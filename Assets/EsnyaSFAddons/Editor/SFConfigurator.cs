#if ESFA
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Udon;

namespace EsnyaAircraftAssets
{
    public class SFConfigurator : EditorWindow
    {
        [MenuItem("EsnyaSFAddons/Configurator")]
        private static void ShowWindow()
        {
            var window = GetWindow<SFConfigurator>();
            window.Show();
        }

        public const string TAG_SEA = "SF_SEA";
        public const string TAG_Respawner = "SF_Respawner";
        public const string LAYER_PREFIX = "SF";

        public Transform sea;
        public float seaLevel = -1080;
        public bool repeatingWorld = true;
        public float repeatingWorldDistance = 20000;

        public Scoreboard_Kills killsBoard;
        public LayerMask hookCableLayer = 23, catapultLayer = 24, aamTargetsLayer = 25, agmTargetsLayer = 26, resupplyLayer = 27;

        public Vector3 respawnerPosition = new Vector3(8, 0, 8);
        public Transform respawnerParent;

        private Vector2 scrollPosition;

        private Transform FindByTagInName(string tag, Transform parent = null)
        {
            var children = ((parent == null) ? SceneManager.GetActiveScene().GetRootGameObjects().Select(o => o.transform) : Enumerable.Range(0, parent.childCount).Select(parent.GetChild)).ToArray();
            foreach (var child in children)
            {
                if (child.gameObject.name.Contains(tag)) return child;
            }

            return children.Select(t => FindByTagInName(tag, t)).FirstOrDefault(t => t != null);
        }

        private static readonly GUILayoutOption[] miniButtonLayouts = {
            GUILayout.ExpandWidth(false),
            GUILayout.Width(80),
        };
        private static void ComponentField<T>(string label, ref T value, System.Func<T> finder) where T : Component
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                value = EditorGUILayout.ObjectField(label, value, typeof(T), true) as T;
                if (GUILayout.Button("Find", EditorStyles.miniButton, miniButtonLayouts))
                {
                    value = finder();
                }
            }
        }

        private static void SetLayerName(int layer, string name)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset"));
            tagManager.Update();

            var layersProperty = tagManager.FindProperty("layers");
            layersProperty.arraySize = Mathf.Max(layersProperty.arraySize, layer);
            layersProperty.GetArrayElementAtIndex(layer).stringValue = name;

            tagManager.ApplyModifiedProperties();
        }

        private static void RenamableLayerField(string layerName, ref LayerMask layer)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                layer = EditorGUILayout.LayerField($"{layerName} Layer", layer);
                if (GUILayout.Button("Rename", EditorStyles.miniButton, miniButtonLayouts)) SetLayerName(layer, $"{LAYER_PREFIX} {layerName}");
            }
        }

        private readonly string[] destinationNames = {
            nameof(EngineController.SeaLevel),
            nameof(EngineController.RepeatingWorld),
            nameof(EngineController.RepeatingWorldDistance),
            nameof(EngineController.KillsBoard),
            nameof(EngineController.HookCableLayer),
            nameof(EngineController.CatapultLayer),
            nameof(EngineController.AAMTargetsLayer),
            nameof(EngineController.AGMTargetsLayer),
            nameof(EngineController.ResupplyLayer),
            nameof(EngineController.VehicleMainObj),
        };
        private readonly string[] sourceNames = {
            nameof(seaLevel),
            nameof(repeatingWorld),
            nameof(repeatingWorldDistance),
            nameof(killsBoard),
            nameof(hookCableLayer),
            nameof(catapultLayer),
            nameof(aamTargetsLayer),
            nameof(agmTargetsLayer),
            nameof(resupplyLayer),
            null,
        };

        private IEnumerable<(string, string)> PropertyNames {
            get => sourceNames.Zip(destinationNames, (sourceName, destinationName) => (sourceName, destinationName));
        }

        private void ForEachProperties<T>(Action<FieldInfo, FieldInfo> Action)
        {
            foreach (var (sourceName, destinationName) in PropertyNames)
            {
                var destinationField = typeof(T).GetField(destinationName);
                if (destinationField != null) Action(sourceName != null ? GetType().GetField(sourceName) : null, destinationField);
            }
        }

        private GameObject GetVehicleMainoOject(UdonSharpBehaviour controller)
        {
            if (controller is AAGunController) return controller.gameObject.GetComponentInParent<Animator>()?.gameObject ?? controller.gameObject;
            return controller.gameObject.GetComponentInParent<Rigidbody>()?.gameObject ?? controller.gameObject;
        }

        private object GetSourceFieldValue<T>(string name, T controller) where T : UdonSharpBehaviour
        {
            var root = GetVehicleMainoOject(controller);
            if (name == nameof(EngineController.VehicleMainObj))
            {
                return root;
            }
            return null;
        }

        private object GetSourceFieldValue(FieldInfo field, object obj)
        {
            if (field.FieldType == typeof(LayerMask))
            {
                return (LayerMask)(1 << ((LayerMask)field.GetValue(obj)).value);
            }
            return field.GetValue(obj);
        }

        private bool IsConfigured<T>(T controller) where T : UdonSharpBehaviour
        {
            bool isConfigured = true;
            ForEachProperties<T>((sourceField, destinationField) => {
                if (!isConfigured) return;
                var sourceValue = sourceField == null ? GetSourceFieldValue(destinationField.Name, controller) : GetSourceFieldValue(sourceField, this);
                isConfigured = sourceValue?.Equals(destinationField.GetValue(controller)) ?? false;
            });

            return isConfigured;
        }

        private void Configure<T>(T controller) where T : UdonSharpBehaviour
        {
            var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(controller);
            Undo.RecordObject(udon, "Configure SF");

            controller.UpdateProxy();
            var root = GetVehicleMainoOject(controller);
            Debug.Log($"Configureing {root.name}");

            ForEachProperties<T>((sourceField, destinationField) => {
                var sourceValue = sourceField == null ? GetSourceFieldValue(destinationField.Name, controller) : GetSourceFieldValue(sourceField, this);
                destinationField.SetValue(controller, sourceValue);
            });

            if (typeof(T) == typeof(EngineController))
            {
                (controller as EngineController).VehicleMainObj = root;
            }

            controller.ApplyProxyModifications();

            EditorUtility.SetDirty(udon);
        }

        private void Configure<T>(IEnumerable<T> controllers) where T : UdonSharpBehaviour
        {
            foreach (var controller in controllers) Configure(controller);
        }

        private void PlaceRespawner(Transform planeRoot)
        {
            var template = Resources.Load<GameObject>("Prefabs/VehicleRespawner");
            var respawner = Instantiate(template, planeRoot.TransformPoint(respawnerPosition), planeRoot.rotation, respawnerParent ?? planeRoot.parent);
            Undo.RegisterCreatedObjectUndo(respawner, "Place Respawner");

            respawner.name = respawner.name.Replace("(Clone)", $"_{planeRoot.gameObject.name}");

            var respawnerUdon = respawner.GetUdonSharpComponent<VehicleRespawnButton>();
            respawnerUdon.EngineControl = planeRoot.GetUdonSharpComponentInChildren<EngineController>();
            respawnerUdon.ApplyProxyModifications();
        }

        private static Scoreboard_Kills FindScoreboard()
        {
            return SceneManager.GetActiveScene().GetRootGameObjects().Select(o => o.GetUdonSharpComponentInChildren<Scoreboard_Kills>()).FirstOrDefault(c => c != null && c.gameObject.activeInHierarchy);
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("SF Configurator");

            var engineController = SceneManager.GetActiveScene().GetRootGameObjects().Select(o => o.GetUdonSharpComponentInChildren<EngineController>()).FirstOrDefault(c => c != null);
            if (engineController != null)
            {
                sea = sea ?? FindByTagInName(TAG_SEA);
                seaLevel = engineController.SeaLevel;
                repeatingWorld = engineController.RepeatingWorld;
                repeatingWorldDistance = engineController.RepeatingWorldDistance;
                killsBoard = FindScoreboard();

                respawnerParent = respawnerParent ?? FindByTagInName(TAG_Respawner);

                // hookCableLayer = engineController.HookCableLayer.value;
                // catapultLayer = engineController.CatapultLayer.value;
                // aamTargetsLayer = engineController.AAMTargetsLayer.value;
                // agmTargetsLayer = engineController.AGMTargetsLayer.value;
                // resupplyLayer = engineController.ResupplyLayer.value;
            }
        }

        private static readonly GUILayoutOption[] miniButotnLayout = {
            GUILayout.ExpandWidth(false),
            GUILayout.Width(120),
        };

        private IEnumerable<T> GetUdonSharpComponentsInScene<T>() where T : UdonSharpBehaviour
        {
            return SceneManager.GetActiveScene()
                .GetRootGameObjects()
                .SelectMany(o => o.GetComponentsInChildren<UdonBehaviour>())
                .Where(UdonSharpEditorUtility.IsUdonSharpBehaviour)
                .Select(UdonSharpEditorUtility.GetProxyBehaviour)
                .Select(u => u as T)
                .Where(u => u != null);
        }

        private void ConfigureButton<T>(T controller, GUIStyle style = null) where T : UdonSharpBehaviour
        {
            ConfigureButton<T, T>(controller, null, style);
        }
        private void ConfigureButton<T, U>(T controller, U effectsController, GUIStyle style = null) where T : UdonSharpBehaviour where U : UdonSharpBehaviour
        {

            var isConfigured = IsConfigured(controller) && (effectsController == null || IsConfigured(effectsController));
            using (new EditorGUI.DisabledGroupScope(isConfigured))
            {
                if (GUILayout.Button(isConfigured ? "Configured" : "Configure", style ?? EditorStyles.miniButton, miniButotnLayout))
                {
                    Configure(controller);
                    if (effectsController != null) Configure(effectsController);
                }
            }
        }

        private static void Validate(EngineController engineController)
        {
            if (engineController == null) return;

            var root = engineController.VehicleMainObj;
            if (root == null)
            {
                EditorGUILayout.LabelField("Vehicle Main Obj is null");
            }
            else
            {
                if (root.GetComponent<VRC.SDK3.Components.VRCObjectSync>() == null) EditorGUILayout.LabelField("Vehicle Main Obj has no VRCObjectSync.");
            }

            var groundDetector = engineController.GroundDetector;
            if (groundDetector == null) EditorGUILayout.LabelField("GroundDetector is null.");
            else
            {
                RaycastHit hitInfo;
                if (Physics.Raycast(groundDetector.position, -groundDetector.up, out hitInfo, 10.44f, 2049, QueryTriggerInteraction.Ignore))
                {
                    if (hitInfo.distance >= 0.44f) EditorGUILayout.LabelField($"GroundDetector is too high (Distance to {hitInfo.collider.gameObject.name} is {hitInfo.distance}).");
                }
                else
                {
                    EditorGUILayout.LabelField("GroundDetector does not detect ground.");
                }
            }

        }

        private void OnGUI()
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                scrollPosition = scroll.scrollPosition;

                EditorGUILayout.Space();

                var scene = SceneManager.GetActiveScene();
                if (scene == null) return;

                var engineControllers = GetUdonSharpComponentsInScene<EngineController>();
                var planeRoots = engineControllers.Select(c => c.GetComponentInParent<Rigidbody>());
                var respawners = GetUdonSharpComponentsInScene<VehicleRespawnButton>().ToArray();

                EditorGUILayout.LabelField("Configurations", EditorStyles.boldLabel);

                ComponentField("Sea", ref sea, () => FindByTagInName(TAG_SEA));
                EditorGUILayout.HelpBox($"Name a GameObject as \"{TAG_SEA}\" to auto configure", MessageType.Info);
                if (sea != null) seaLevel = sea.position.y;
                using (new EditorGUI.DisabledGroupScope(sea != null)) seaLevel = EditorGUILayout.FloatField("Sea Level", seaLevel);

                repeatingWorld = EditorGUILayout.Toggle("Repeating World", repeatingWorld);
                using (new EditorGUI.DisabledGroupScope(!repeatingWorld)) repeatingWorldDistance = EditorGUILayout.FloatField("Repeating World Distance", repeatingWorldDistance);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
                ComponentField(
                    "Kills Board",
                    ref killsBoard,
                    FindScoreboard
                );

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);
                RenamableLayerField("Hook Cable", ref hookCableLayer);
                RenamableLayerField("Catapult", ref catapultLayer);
                RenamableLayerField("AAM Targets", ref aamTargetsLayer);
                RenamableLayerField("AGM Targets", ref agmTargetsLayer);
                RenamableLayerField("Resupply", ref resupplyLayer);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Utility Configurations", EditorStyles.boldLabel);
                respawnerPosition = EditorGUILayout.Vector3Field("Respawner Position", respawnerPosition);
                ComponentField("Respawner Parent", ref respawnerParent, () => FindByTagInName(TAG_Respawner));
                EditorGUILayout.HelpBox($"Name a GameObject as \"{TAG_Respawner}\" to auto configure", MessageType.Info);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Wind Changers");
                var windChangers = GetUdonSharpComponentsInScene<WindChanger>();
                foreach (var windChanger in windChangers)
                {
                    var engines = windChanger.GetProgramVariable("VehicleEngines") as EngineController[];
                    var windChangersConfigured = engines != null && engines.All(e => e != null) && engineControllers.All(e => engines.Contains(e));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.ObjectField(windChanger, typeof(WindChanger), true);
                        using (new EditorGUI.DisabledGroupScope(windChangersConfigured))
                        {
                            if (GUILayout.Button("Configure", EditorStyles.miniButton, miniButotnLayout))
                            {
                                windChanger.SetProgramVariable("VehicleEngines", engineControllers.ToArray());
                                windChanger.ApplyProxyModifications();
                                EditorUtility.SetDirty(UdonSharpEditorUtility.GetBackingUdonBehaviour(windChanger));
                            }
                        }
                    }
                }

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Aircrafts", EditorStyles.boldLabel);
                foreach (var planeRoot in planeRoots)
                {
                    var engineController = planeRoot.GetUdonSharpComponentInChildren<EngineController>();
                    if (engineController == null) continue;

                    var effectsController = planeRoot.GetUdonSharpComponentInChildren<EffectsController>();
                    var respawner = respawners.FirstOrDefault(r => r.EngineControl == engineController);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.ObjectField(planeRoot?.gameObject, typeof(GameObject), true);
                        EditorGUILayout.ObjectField(engineController?.gameObject, typeof(GameObject), true);

                        ConfigureButton(engineController, effectsController, EditorStyles.miniButtonLeft);

                        if (respawner == null)
                        {
                            if (GUILayout.Button("Place Respawner", EditorStyles.miniButtonRight, miniButotnLayout)) PlaceRespawner(planeRoot.transform);
                        }
                        else
                        {
                            if (GUILayout.Button("Remove Respawner", EditorStyles.miniButtonRight, miniButotnLayout)) Undo.DestroyObjectImmediate(respawner.gameObject);
                        }
                    }

                    Validate(engineController);
                }
                if (GUILayout.Button("Configure All")) Configure(engineControllers);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("AA Guns", EditorStyles.boldLabel);
                var aaGuns = GetUdonSharpComponentsInScene<AAGunController>();
                foreach (var aaGun in aaGuns)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.ObjectField(aaGun.gameObject, typeof(GameObject), true);
                        ConfigureButton(aaGun);
                    }
                }

                if (GUILayout.Button("Configure All")) Configure(aaGuns);

                EditorGUILayout.Space();
            }
        }
    }
}
#endif

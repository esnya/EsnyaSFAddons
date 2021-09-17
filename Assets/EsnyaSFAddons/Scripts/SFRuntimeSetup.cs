using System.Collections.ObjectModel;
using InariUdon.UI;
using System;
using UdonSharp;
using UdonToolkit;
using UnityEngine;
using VRC.SDK3.Components;
using System.Net.Mail;
using Ludiq.OdinSerializer;
using VRC.Udon;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System.Linq;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace EsnyaAircraftAssets
{
    [
        DefaultExecutionOrder(100), // After EngineController, WindChanger
        UdonBehaviourSyncMode(BehaviourSyncMode.None),
    ]
    public class SFRuntimeSetup : UdonSharpBehaviour
    {
#if ESFA

        [Header("World Configuration")]
        public Transform sea;
        public bool repeatingWorld = true;
        [HideIf("@!repeatingWorld")] public float repeatingWorldDistance = 20000;

        [Header("SaccSync")]
        public bool enableSaccSync;
        [HideIf("@!enableSaccSync")] public GameObject saccSyncPrefab;

        [Header("Detected Components")]
        public Scoreboard_Kills scoreboard;
        public WindChanger[] windChangers = { };
        public EngineController[] engineControllers;

        private void Start()
        {
            foreach (var engineController in engineControllers)
            {
                engineController.RepeatingWorld = repeatingWorld;
                engineController.RepeatingWorldDistance = repeatingWorldDistance;
                engineController.SeaLevel = sea.position.y;
                engineController.KillsBoard = scoreboard;
                var hudController = engineController.HUDControl;
                if (hudController != null) hudController.gameObject.SetActive(false);
            }

            if (windChangers != null)
            {
                foreach (var changer in windChangers) if (changer) changer.VehicleEngines = engineControllers;
            }

            Log("Info", $"Initialized {engineControllers.Length} vehicles");

            enabled = false;
        }

        [Header("Logger")]
        public UdonLogger logger;
        private void Log(string level, string log)
        {
            if (logger == null) Debug.Log(log);
            else logger.Log(level, gameObject.name, log);
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void Reset()
        {
            sea = GameObject.Find("SF_SEA")?.transform;
            saccSyncPrefab = Resources.Load<GameObject>("SaccSync");
        }

        private static string GetNameWithId(UnityEngine.Object obj)
        {
            return $"{obj}/{obj.GetInstanceID():x}";
        }

        [Button("Editor Setup Now", true)]
        public void EditorSetup()
        {
            var rootObjects = gameObject.scene.GetRootGameObjects();

            engineControllers = rootObjects.SelectMany(o => o.GetUdonSharpComponentsInChildren<EngineController>(true)).ToArray();
            scoreboard = rootObjects.Select(o => o.GetUdonSharpComponentInChildren<Scoreboard_Kills>()).Concat(rootObjects.Select(o => o.GetUdonSharpComponentInChildren<Scoreboard_Kills>(true))).Append(scoreboard).FirstOrDefault();
            windChangers = rootObjects.SelectMany(o => o.GetUdonSharpComponentsInChildren<WindChanger>(true)).ToArray();

            var saccSyncType = UdonSharpEditorUtility.GetUdonSharpBehaviourType(saccSyncPrefab.GetComponent<UdonBehaviour>());

            foreach (var engineController in engineControllers)
            {
                var vehicleMainObj = engineController.VehicleMainObj;
                var hasSaccSync = vehicleMainObj.GetUdonSharpComponentInChildren(saccSyncType, true) != null;
                var objectSync = vehicleMainObj.GetComponent<VRCObjectSync>();
                var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(engineController.gameObject);
                var hudController = engineController.HUDControl;
                if (hudController?.gameObject?.activeSelf ?? false) hudController.gameObject.SetActive(false);


                if (enableSaccSync && saccSyncPrefab)
                {
                    if (!hasSaccSync)
                    {
                        Debug.Log($"[{GetNameWithId(this)}] Adding SaccSync to {GetNameWithId(vehicleMainObj)}");
                        var saccSync = (PrefabUtility.InstantiatePrefab(saccSyncPrefab, vehicleMainObj.transform) as GameObject).GetUdonSharpComponent(saccSyncType);
                        saccSync.SetProgramVariable("EngineControl", engineController);
                        saccSync.SetProgramVariable("VehicleTransform", vehicleMainObj.transform);
                        saccSync.ApplyProxyModifications();
                        saccSync.gameObject.SetActive(false);

                        engineController.SetProgramVariable("SaccSync", saccSync);
                        engineController.ApplyProxyModifications();
                        EditorUtility.SetDirty(UdonSharpEditorUtility.GetBackingUdonBehaviour(engineController));
                    }

                    if (objectSync)
                    {
                        Debug.Log($"[{GetNameWithId(this)}] Removing ObjectSync from {GetNameWithId(vehicleMainObj)}");
                        DestroyImmediate(objectSync);
                    }
                }
                else
                {
                    if (hasSaccSync)
                    {
                        Debug.Log($"[{GetNameWithId(this)}] Removing SaccSync from {GetNameWithId(vehicleMainObj)}");
                        foreach (var saccSync in vehicleMainObj.GetUdonSharpComponentsInChildren(saccSyncType))
                        {
                            DestroyImmediate(saccSync.gameObject);
                        }
                    }

                    if (objectSync == null)
                    {
                        Debug.Log($"[{GetNameWithId(this)}] Adding ObjectSync to {GetNameWithId(vehicleMainObj)}");
                        vehicleMainObj.AddComponent<VRCObjectSync>().AllowCollisionOwnershipTransfer = false;
                    }
                }
            }

            Debug.Log($"[{GetNameWithId(this)}] detected {engineControllers.Length} EngineControllers, {scoreboard?.ToString() ?? "No Scoreboard"} and {windChangers.Length} WindChangers.");
        }

        public static void EditorSetup(Scene scene)
        {
            scene.GetRootGameObjects().SelectMany(o => o.GetUdonSharpComponentsInChildren<SFRuntimeSetup>(true)).ToList().ForEach(target =>
            {
                target.EditorSetup();
                target.ApplyProxyModifications();
                EditorUtility.SetDirty(UdonSharpEditorUtility.GetBackingUdonBehaviour(target));
            });
        }

        [InitializeOnLoadMethod]
        public static void RegisterCallbacks()
        {
            EditorApplication.playModeStateChanged += (c) =>
            {
                if (c == PlayModeStateChange.EnteredPlayMode) EditorSetup(SceneManager.GetActiveScene());
            };
            EditorSceneManager.sceneOpened += (scene, _) => EditorSetup(scene);
            EditorSceneManager.sceneSaving += (scene, _) => EditorSetup(scene);
        }
#endif

#endif
    }
}

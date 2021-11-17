using InariUdon.UI;
using System;
using UdonSharp;
using UdonToolkit;
using UnityEngine;
using VRC.SDK3.Components;
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
        DefaultExecutionOrder(100), // After SaccAirVehicle, SAV_WindChanger
        UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync),
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
        public SaccScoreboard_Kills scoreboard;
        public SAV_WindChanger[] windChangers = { };
        public SaccAirVehicle[] airVehicles;

        private void Start()
        {
            foreach (var airVehicle in airVehicles)
            {
                if (airVehicle == null) continue;

                var entity = airVehicle.GetComponentInParent<SaccEntity>();
                if (entity != null)
                {
                    foreach (var extention in entity.ExtensionUdonBehaviours) SetupExtention(entity, extention);
                    foreach (var extention in entity.Dial_Functions_L) SetupExtention(entity, extention);
                    foreach (var extention in entity.Dial_Functions_R) SetupExtention(entity, extention);
                }

                airVehicle.SetProgramVariable(nameof(SaccAirVehicle.RepeatingWorld), repeatingWorld);
                airVehicle.SetProgramVariable(nameof(SaccAirVehicle.RepeatingWorldDistance), repeatingWorldDistance);
                airVehicle.SetProgramVariable(nameof(SaccAirVehicle.SeaLevel), sea.position.y);

                var killTracker = airVehicle.GetComponentInChildren<SAV_KillTracker>();
                if (killTracker != null) killTracker.SetProgramVariable(nameof(SAV_KillTracker.KillsBoard), scoreboard);

                var hudController = airVehicle.GetComponentInChildren<SAV_HUDController>();
                if (hudController != null) hudController.gameObject.SetActive(false);
            }

            if (windChangers != null)
            {
                foreach (var changer in windChangers) if (changer) changer.SetProgramVariable("SaccAirVehicles", airVehicles);
            }

            Log("Info", $"Initialized {airVehicles.Length} vehicles");

            enabled = false;
        }

        private void SetupExtention(SaccEntity entity, UdonSharpBehaviour extention)
        {
            if (extention == null) return;
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

            airVehicles = rootObjects.SelectMany(o => o.GetUdonSharpComponentsInChildren<SaccAirVehicle>(true)).ToArray();
            scoreboard = rootObjects.Select(o => o.GetUdonSharpComponentInChildren<SaccScoreboard_Kills>()).Concat(rootObjects.Select(o => o.GetUdonSharpComponentInChildren<SaccScoreboard_Kills>(true))).Append(scoreboard).Where(s => s != null).FirstOrDefault();
            windChangers = rootObjects.SelectMany(o => o.GetUdonSharpComponentsInChildren<SAV_WindChanger>(true)).ToArray();

            foreach (var airVehicle in airVehicles)
            {
                var vehicleMainObj = airVehicle.GetComponentInParent<SaccEntity>()?.gameObject;
                if (vehicleMainObj == null) continue;
                var objectSync = vehicleMainObj.GetComponent<VRCObjectSync>();
                var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(airVehicle.gameObject);
                var hudController = airVehicle.GetComponentInChildren<SAV_HUDController>();
                if (hudController?.gameObject?.activeSelf ?? false) hudController.gameObject.SetActive(false);
            }

            Debug.Log($"[{GetNameWithId(this)}] detected {airVehicles.Length} SaccAirVehicles, {scoreboard?.ToString() ?? "No Scoreboard"} and {windChangers.Length} SAV_WindChangers.");
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

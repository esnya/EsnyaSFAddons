using System.Collections.ObjectModel;
using InariUdon.UI;
using System;
using UdonSharp;
using UdonToolkit;
using UnityEngine;
using VRC.SDK3.Components;

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
        UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync),
    ]
    public class SFRuntimeSetup : UdonSharpBehaviour
    {
#if ESFA
        [Header("EngineController Detection")]
        public LayerMask planeLayers = 1 << 17;
        public float searchRadius = 100000;

        [Header("World Configuration")]
        public Transform sea;
        public bool repeatingWorld = true;
        [HideIf("@!repeatingWorld")] public float repeatingWorldDistance = 20000;
        public Scoreboard_Kills scoreboard;
        public WindChanger[] windChangers = {};

        private int targetCount;
        private EngineController[] engineControllers;

        private bool ContainsUntilNull(object[] array, object value)
        {
            foreach (var item in array)
            {
                if (item == null) return false;
                if (item == value) return true;
            }
            return false;
        }
        private object[] Take(object[] array, int length)
        {
            if (array == null) return null;

            length = Mathf.Min(array.Length, length);

            var resized = new object[length];
            Array.Copy(array, resized, length);

            return resized;
        }

        private void Start()
        {
            var colliders = Physics.OverlapSphere(transform.position, 100000, planeLayers, QueryTriggerInteraction.Ignore);

            engineControllers = new EngineController[colliders.Length];
            targetCount = 0;
            foreach (var collider in colliders)
            {
                if (collider == null) continue;

                var rigidbody = collider.GetComponentInParent<Rigidbody>();
                if (rigidbody == null) continue;

                var hitDetector = rigidbody.GetComponent<HitDetector>();
                if (hitDetector == null) continue;

                var engineController = hitDetector.EngineControl;
                if (engineController == null || ContainsUntilNull(engineControllers, engineController)) continue;

                engineControllers[targetCount++] = engineController;
            }
            engineControllers = (EngineController[])Take(engineControllers, targetCount);

            foreach (var engineController in engineControllers)
            {
                engineController.SetProgramVariable(nameof(EngineController.RepeatingWorld), repeatingWorld);
                engineController.SetProgramVariable(nameof(EngineController.RepeatingWorldDistance), repeatingWorldDistance);
                engineController.SetProgramVariable(nameof(EngineController.SeaLevel), sea.position.y);
                engineController.SetProgramVariable(nameof(EngineController.KillsBoard), scoreboard);
                var hudController = (HUDController)engineController.GetProgramVariable(nameof(EngineController.HUDControl));
                if (hudController != null) hudController.gameObject.SetActive(false);
            }

            if (windChangers != null)
            {
                foreach (var changer in windChangers) if (changer) changer.SetProgramVariable("VehicleEngines", engineControllers);
            }

            Log("Info", $"Initialized {targetCount} vehicles");

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
            // scoreboard = SceneManager.GetActiveScene().GetRootGameObjects().Select(o => o.GetUdonSharpComponentInChildren<Scoreboard_Kills>()).FirstOrDefault();
            // windChangers = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(o => o.GetUdonSharpComponentsInChildren<WindChanger>()).ToArray();
        }
#endif

#endif
    }
}

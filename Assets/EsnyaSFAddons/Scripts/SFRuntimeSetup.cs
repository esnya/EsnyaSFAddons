using InariUdon.UI;
using System;
using UdonSharp;
using UdonToolkit;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.Udon;
using VRC.SDKBase;
using System.Collections;

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
        DefaultExecutionOrder(100), // After SaccEntity/SaccAirVehicle/SAV_WindChanger
        UdonBehaviourSyncMode(BehaviourSyncMode.None),
    ]
    public class SFRuntimeSetup : UdonSharpBehaviour
    {
#if ESFA
        [Header("World Configuration")]
        public Transform sea;
        public bool repeatingWorld = true;
        [HideIf("@!repeatingWorld")] public float repeatingWorldDistance = 20000;

        [Header("Inject Extentions")]
        public UdonSharpBehaviour[] injectExtentions = {};

        [Header("Detected Components")]
        [UdonSharpComponentInject] public SAV_WindChanger[] windChangers = { };
        [UdonSharpComponentInject] public SaccAirVehicle[] airVehicles;


        private void Start()
        {
            foreach (var airVehicle in airVehicles)
            {
                if (airVehicle == null) continue;

                var entity = airVehicle.EntityControl;
                if (entity == null) continue;

                InjectExtentions(entity, airVehicle);

                airVehicle.SetProgramVariable(nameof(SaccAirVehicle.RepeatingWorld), repeatingWorld);
                airVehicle.SetProgramVariable(nameof(SaccAirVehicle.RepeatingWorldDistance), repeatingWorldDistance);
                airVehicle.SetProgramVariable(nameof(SaccAirVehicle.SeaLevel), sea.position.y);
            }

            if (windChangers != null)
            {
                foreach (var changer in windChangers) if (changer) changer.SetProgramVariable("SaccAirVehicles", airVehicles);
            }

            Log("Info", $"Initialized {airVehicles.Length} vehicles");

            gameObject.SetActive(false);
        }

        private void SetupExtentionReference(UdonBehaviour extention, string variableName, UdonSharpBehaviour value)
        {
            // if (extention.GetProgramVariableType(variableName) == null) return;
            extention.SetProgramVariable(variableName, value);
        }

        private void InjectExtentions(SaccEntity entity, SaccAirVehicle airVehicle)
        {
            var currentArray = (Component[])entity.GetProgramVariable(nameof(entity.ExtensionUdonBehaviours));
            var currentLength = currentArray.Length;
            var nextLength = currentLength + injectExtentions.Length;
            var nextArray = new UdonSharpBehaviour[nextLength];
            Array.Copy(currentArray, nextArray, currentLength);

            Debug.Log(entity);

            for (var i = 0; i < injectExtentions.Length; i++)
            {
                var obj = VRCInstantiate(injectExtentions[i].gameObject);
                var extention = (UdonBehaviour)obj.GetComponent(typeof(UdonBehaviour));
                obj.transform.SetParent(entity.transform, false);
                nextArray[currentLength + i] = (UdonSharpBehaviour)(Component)extention;

                SetupExtentionReference(extention, "EntityControl", entity);
                SetupExtentionReference(extention, "SAVControl", airVehicle);
            }

            entity.SetProgramVariable(nameof(entity.ExtensionUdonBehaviours), nextArray);
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
        }
#endif

#endif
    }
}

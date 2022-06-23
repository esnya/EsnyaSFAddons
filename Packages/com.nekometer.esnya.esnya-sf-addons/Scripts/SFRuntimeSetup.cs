using System;
using EsnyaSFAddons.Accesory;
using EsnyaSFAddons.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.Udon;

namespace EsnyaSFAddons
{
    [DefaultExecutionOrder(order: 100)] // After SaccEntity/SaccAirVehicle/SAV_WindChanger
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFRuntimeSetup : UdonSharpBehaviour
    {
        [Header("World Configuration")]
        public Transform sea;
        public bool repeatingWorld = true;
        public float repeatingWorldDistance = 20000;


        [Header("Inject Extentions")]
        public UdonSharpBehaviour[] injectExtentions = { };

        [Header("Detected Components")]
        [UdonSharpComponentInject] public SAV_WindChanger[] windChangers = { };
        [UdonSharpComponentInject] public SaccAirVehicle[] airVehicles;
        [UdonSharpComponentInject] public Windsock[] windsocks;

        private void Start()
        {
            foreach (var airVehicle in airVehicles)
            {
                if (!airVehicle) continue;

                var entity = airVehicle.EntityControl;
                if (!entity) continue;

                InjectExtentions(entity, airVehicle);

                airVehicle.SetProgramVariable(nameof(SaccAirVehicle.RepeatingWorld), repeatingWorld);
                airVehicle.SetProgramVariable(nameof(SaccAirVehicle.RepeatingWorldDistance), repeatingWorldDistance);
                airVehicle.SetProgramVariable(nameof(SaccAirVehicle.SeaLevel), sea.position.y);
            }

            if (windChangers != null)
            {
                foreach (SAV_WindChanger changer in windChangers)
                {
                    if (!changer) continue;

                    changer.SetProgramVariable(name: nameof(SAV_WindChanger.SaccAirVehicles), airVehicles);

                }
                if (windChangers.Length > 0 && windsocks != null)
                {
                    foreach (Windsock windsock in windsocks) windsock.windChanger = windChangers[0];
                }
            }

            Debug.Log($"[ESFA] Initialized {airVehicles.Length} vehicles");

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

            for (var i = 0; i < injectExtentions.Length; i++)
            {
                var obj = VRCInstantiate(injectExtentions[i].gameObject);
                obj.name = injectExtentions[i].name;
                var extention = (UdonBehaviour)obj.GetComponent(typeof(UdonBehaviour));
                obj.transform.SetParent(entity.transform, false);
                nextArray[currentLength + i] = (UdonSharpBehaviour)(Component)extention;

                SetupExtentionReference(extention, "EntityControl", entity);
                SetupExtentionReference(extention, "SAVControl", airVehicle);
            }

            entity.SetProgramVariable(nameof(entity.ExtensionUdonBehaviours), nextArray);
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void Reset()
        {
            sea = GameObject.Find("SF_SEA")?.transform;
        }
#endif
    }
}
using System;
using InariUdon.UI;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace EsnyaAircraftAssets
{
    [DefaultExecutionOrder(100)] // After SaccEntity/SaccAirVehicle/SAV_WindChanger
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFRuntimeSetup : UdonSharpBehaviour
    {
        [Header("World Configuration")]
        public Transform sea;
        public bool repeatingWorld = true;
        public float repeatingWorldDistance = 20000;

        public bool randomWind = false;
        public float randomWindStrength = 6.0f;
        public AnimationCurve randomWindCurve = AnimationCurve.Linear(0, 0, 0, 1);
        public float randomGustStrength = 3.0f;
        public AnimationCurve randomGustCurve = AnimationCurve.Linear(0, 0, 0, 1);

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
                foreach (var changer in windChangers)
                {
                    if (!changer) continue;

                    changer.SetProgramVariable(nameof(SAV_WindChanger.SaccAirVehicles), airVehicles);

                }
                if (windChangers.Length > 0 && windsocks != null)
                {
                    foreach (var windsock in windsocks) windsock.windChanger = windChangers[0];
                }
            }

            Log("Info", $"Initialized {airVehicles.Length} vehicles");

            gameObject.SetActive(false);
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (randomWind && player.isLocal && player.isMaster)
            {
                var windStrength = randomWindCurve.Evaluate(UnityEngine.Random.value) * randomWindStrength;
                var wind = Quaternion.AngleAxis(UnityEngine.Random.Range(0, 360), Vector3.up) * Vector3.forward * windStrength;
                var gustStrength = randomGustCurve.Evaluate(UnityEngine.Random.value) * randomGustStrength;

                foreach (var airVehicle in airVehicles)
                {
                    if (!airVehicle) continue;
                    airVehicle.SetProgramVariable(nameof(SaccAirVehicle.Wind), wind);
                    airVehicle.SetProgramVariable(nameof(SaccAirVehicle.WindGustStrength), gustStrength);
                }

                if (windChangers != null)
                {
                    foreach (var changer in windChangers)
                    {
                        if (!changer) continue;

                        var synced = changer.SyncedWind;
                        changer.SyncedWind = false;

                        changer.WindStrenth_3 = wind;
                        changer.WindGustStrength = gustStrength;

                        changer.SyncedWind = synced;

                        changer.UpdateValuesFromOther();

                        changer.transform.rotation = Quaternion.FromToRotation(Vector3.forward, wind.normalized);
                    }
                }
            }
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
    }
}

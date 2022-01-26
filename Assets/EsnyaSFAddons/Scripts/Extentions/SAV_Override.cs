using UdonSharp;
using UnityEngine;

namespace EsnyaAircraftAssets
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SAV_Override : UdonSharpBehaviour
    {
        [Header("Health")]
        public bool overrideHealth;
        public float healthMultiplier = 1;

        [Header("Fuel")]
        public bool overrideFuel;
        public float fuelMultiplier = 1;

        [Header("Brake")]
        public bool overrideBrake;
        public float airbrakeMultiplier = 1;
        public float groundBrakeMultiplier = 1;
        public float waterBrakeMultiplier = 1;

        [Header("Custom Override")]
        public bool customOverride;
        public string udonTypeName;
        public string variableName;
        public float customValueMultiplier;

        private bool initialized = false;

        public void SFEXT_O_PilotEnter()
        {
            if (initialized) return;

            initialized = true;
            SendCustomEventDelayedSeconds(nameof(_LateStart), 3);
        }

        public void _LateStart()
        {
            var entity = GetComponentInParent<SaccEntity>();
            var airVehicle = entity.GetComponentInChildren<SaccAirVehicle>(true);

            if (overrideHealth) airVehicle.SetProgramVariable("FullHealth", ((float)airVehicle.GetProgramVariable("FullHealth")) * healthMultiplier);
            if (overrideFuel) airVehicle.SetProgramVariable("FullFuel", ((float)airVehicle.GetProgramVariable("FullFuel")) * fuelMultiplier);

            if (overrideBrake)
            {
                var brake = entity.GetComponentInChildren<DFUNC_Brake>(true);
                brake.AirbrakeStrength *= airbrakeMultiplier;
                brake.GroundBrakeStrength *= groundBrakeMultiplier;
                brake.WaterBrakeStrength *= waterBrakeMultiplier;
            }

            if (customOverride)
            {
                CustomOverride(entity.ExtensionUdonBehaviours);
                CustomOverride(entity.Dial_Functions_L);
                CustomOverride(entity.Dial_Functions_R);
            }

            gameObject.SetActive(false);
        }

        private void CustomOverride(UdonSharpBehaviour[] extentions)
        {
            if (extentions == null) return;
            foreach (var extention in extentions)
            {
                if (!extention || extention.GetUdonTypeName() != udonTypeName) continue;
                extention.SetProgramVariable(variableName, (float)extention.GetProgramVariable(variableName) * customValueMultiplier);
            }
        }
    }
}

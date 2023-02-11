using SaccFlightAndVehicles;
using UdonSharp;
using VRC.Udon.Common.Interfaces;

namespace EsnyaSFAddons.SFEXT
{
    /// <summary>
    /// Turn off engine if vehicle flooded
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_FloodedEngineKiller : UdonSharpBehaviour
    {
        private SaccAirVehicle airVehicle;
        private DFUNC_ToggleEngine toggleEngine;

        public void SFEXT_L_EntityStart()
        {
            var entity = GetComponentInParent<SaccEntity>();
            airVehicle = (SaccAirVehicle)entity.GetExtention(GetUdonTypeName<SaccAirVehicle>());
            toggleEngine = (DFUNC_ToggleEngine)entity.GetExtention(GetUdonTypeName<DFUNC_ToggleEngine>());
        }

        public void SFEXT_P_PilotEnter() => CheckFlooded();
        public void SFEXT_G_TouchDownWater() => CheckFlooded();
        public void SFEXT_G_EngineOn() => CheckFlooded();
        public void SFEXT_G_EngineStartup() => CheckFloodedOnStartup();
        private void CheckFlooded()
        {
            if (airVehicle && airVehicle.Piloting && airVehicle.Floating && airVehicle.EngineOn)
            {
                airVehicle.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(airVehicle.SetEngineOff));
            }
        }

        private void CheckFloodedOnStartup()
        {
            if (airVehicle && toggleEngine && airVehicle.Piloting && airVehicle.Floating)
            {
                toggleEngine.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(toggleEngine.EngineStartupCancel));
            }
        }
    }
}

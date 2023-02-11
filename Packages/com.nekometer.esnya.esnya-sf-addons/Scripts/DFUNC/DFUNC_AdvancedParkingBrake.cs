using EsnyaSFAddons.SFEXT;
using SaccFlightAndVehicles;
using UdonSharp;
using UnityEngine;

namespace EsnyaSFAddons.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_AdvancedParkingBrake : DFUNC_Base
    {
        public KeyCode desktopControl = KeyCode.N;
        public string parameterName = "parkingbrake";
        public GameObject Dial_Funcon;

        protected override bool ActivateOnSelected => false;

        [System.NonSerialized][UdonSynced][FieldChangeCallback(nameof(State))] private bool _state = false;
        public bool State
        {
            private set
            {
                _state = value;

                if (!initialized) return;
                if (vehicleAnimator) vehicleAnimator.SetBool(parameterName, value);
                if (Dial_Funcon) Dial_Funcon.SetActive(value);
                foreach (var gear in gears) gear.parkingBrake = value;
            }
            get => _state;
        }

        private Animator vehicleAnimator;
        private SFEXT_AdvancedGear[] gears;
        private bool initialized;
        public override void SFEXT_L_EntityStart()
        {
            var entity = GetComponentInParent<SaccEntity>();
            var airVehicle = (SaccAirVehicle)entity.GetExtention(GetUdonTypeName<SaccAirVehicle>());
            vehicleAnimator = airVehicle.VehicleAnimator;
            gears = (SFEXT_AdvancedGear[])entity.GetExtentions(GetUdonTypeName<SFEXT_AdvancedGear>());

            gameObject.SetActive(false);

            initialized = true;

            State = false;
        }

        private void Toggle()
        {
            State = !State;
            RequestSerialization();
        }

        public override void DFUNC_TriggerPressed() => Toggle();
        public void KeyboardInput() => Toggle();

        protected override void DFUNC_Update()
        {
            if (isPilot && Input.GetKeyDown(desktopControl)) Toggle();
        }

        private void ResetStatus()
        {
            State = false;
            RequestSerialization();
        }


        #region SF Utilities
        public void SFEXT_G_Explode() => ResetStatus();
        public void SFEXT_G_RespawnButton() => ResetStatus();

        public void SFEXT_G_PilotEnter() => gameObject.SetActive(true);
        public void SFEXT_G_PilotExit() => gameObject.SetActive(false);


        private bool isPilot;
        public override void SFEXT_O_PilotEnter()
        {
            DFUNC_Deselected();
            isPilot = true;
        }
        public override void SFEXT_O_PilotExit()
        {
            DFUNC_Deselected();
            isPilot = false;
        }

        public void Set()
        {
            State = true;
            RequestSerialization();
        }
        public void Release()
        {
            State = false;
            RequestSerialization();
        }
        #endregion
    }
}
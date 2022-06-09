
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace EsnyaSFAddons
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_WingFold : UdonSharpBehaviour
    {
        public GameObject Dial_Funcon;
        public float ExtraLiftMulti = 0.1f;
        public string animatorBool = "wingfold";
        public bool flapsOff = true;

        private Animator vehicleAnimator;
        private SaccEntity entity;
        private SaccAirVehicle airVehicle;
        private DFUNC_Flaps flapsDFunc;
        private float _extraLift = 0.0f;
        private float ExtraLift {
            set {
                var diff = value - _extraLift;
                if (diff > 0) airVehicle.ExtraLift -= diff;
                _extraLift = value;
            }
        }
        [UdonSynced][FieldChangeCallback(nameof(Fold))] private bool _fold;
        private bool hasPilot;

        public bool Fold {
            get => _fold;
            private set {
                ExtraLift = value ? Mathf.Clamp01(1.0f - ExtraLiftMulti) : 0.0f;
                if (vehicleAnimator) vehicleAnimator.SetBool(animatorBool, value);
                if (Dial_Funcon) Dial_Funcon.SetActive(value);
                if (value && flapsOff && flapsDFunc) flapsDFunc.SetFlapsOff();
                _fold = value;
                if (value && !hasPilot) gameObject.SetActive(false);
            }
        }

        public void SFEXT_L_EntityStart()
        {
            entity = GetComponentInParent<SaccEntity>();
            airVehicle = (SaccAirVehicle)entity.GetExtention(GetUdonTypeName<SaccAirVehicle>());
            flapsDFunc = (DFUNC_Flaps)entity.GetExtention(GetUdonTypeName<DFUNC_Flaps>());
            vehicleAnimator = airVehicle.VehicleAnimator;

            Fold = true;
            gameObject.SetActive(entity.Piloting);
        }

        public void SFEXT_G_Reappear()
        {
            Fold = true;
            gameObject.SetActive(hasPilot);
        }

        public void SFEXT_G_PilotEnter()
        {
            hasPilot = true;
            gameObject.SetActive(true);
        }
        public void SFEXT_G_PilotExit()
        {
            hasPilot = false;
            if (Fold) gameObject.SetActive(false);
        }

        public void SFEXT_O_FlapsOn()
        {
            if (Fold && flapsDFunc) flapsDFunc.ToggleFlaps();
        }

        public void KeyboardInput() => ToggleFold();
        public void DFUNC_TriggerPress() => ToggleFold();

        public void ToggleFold()
        {
            Fold = !Fold;
            RequestSerialization();
        }
    }
}

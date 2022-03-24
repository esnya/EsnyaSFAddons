using UdonSharp;
using UnityEngine;

namespace EsnyaSFAddons
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_AdvancedParkingBrake : UdonSharpBehaviour
    {
        public KeyCode desktopControl = KeyCode.N;
        public string parameterName = "parkingbrake";
        public GameObject Dial_Funcon;

        [System.NonSerialized] [UdonSynced] [FieldChangeCallback(nameof(State))] private bool _state = false;
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
        private string triggerAxis;
        private bool initialized;
        public void SFEXT_L_EntityStart()
        {
            var entity = GetComponentInParent<SaccEntity>();
            var airVehicle = (SaccAirVehicle)GetExtention(entity, GetUdonTypeName<SaccAirVehicle>());
            vehicleAnimator = airVehicle.VehicleAnimator;
            gears = (SFEXT_AdvancedGear[])GetExtentions(entity, GetUdonTypeName<SFEXT_AdvancedGear>());

            gameObject.SetActive(false);

            initialized = true;

            State = false;
        }

        private void OnEnable()
        {
            prevTriggered = isSelected = false;
        }

        private bool prevTriggered;
        private void Update()
        {
            if (isPilot)
            {
                var triggered = Input.GetAxis(triggerAxis) > 0.75f;
                if (Input.GetKeyDown(desktopControl) || isSelected && triggered && !prevTriggered)
                {
                    State = !State;
                    RequestSerialization();
                }
                prevTriggered = triggered;
            }
        }

        private void ResetStatus()
        {
            State = false;
            RequestSerialization();
        }


        #region SF Utilities
        private bool isOwner;
        public void DFUNC_LeftDial() => triggerAxis = "Oculus_CrossPlatform_PrimaryIndexTrigger";
        public void DFUNC_RightDial() => triggerAxis = "Oculus_CrossPlatform_SecondaryIndexTrigger";

        public void SFEXT_O_TakeOwnership() => isOwner = true;
        public void SFEXT_O_LoseOwnership() => isOwner = false;
        public void SFEXT_G_Explode() => ResetStatus();
        public void SFEXT_G_RespawnButton() => ResetStatus();

        public void SFEXT_G_PilotEnter() => gameObject.SetActive(true);
        public void SFEXT_G_PilotExit() => gameObject.SetActive(false);

        private bool isSelected;
        public void DFUNC_Selected() => isSelected = true;
        public void DFUNC_Deselected() => isSelected = false;

        private bool isPilot;
        public void SFEXT_O_PilotEnter() => isOwner = isPilot = true;
        public void SFEXT_O_PilotExit() => isPilot = false;

        private UdonSharpBehaviour GetExtention(SaccEntity entity, string udonTypeName)
        {
            foreach (var extention in entity.ExtensionUdonBehaviours)
            {
                if (extention && extention.GetUdonTypeName() == udonTypeName) return extention;
            }
            foreach (var extention in entity.Dial_Functions_L)
            {
                if (extention && extention.GetUdonTypeName() == udonTypeName) return extention;
            }
            foreach (var extention in entity.Dial_Functions_R)
            {
                if (extention && extention.GetUdonTypeName() == udonTypeName) return extention;
            }
            return null;
        }

        private UdonSharpBehaviour[] GetExtentions(SaccEntity entity, string udonTypeName)
        {
            var result = new UdonSharpBehaviour[entity.ExtensionUdonBehaviours.Length + entity.Dial_Functions_L.Length + entity.Dial_Functions_R.Length];
            var count = 0;
            foreach (var extention in entity.ExtensionUdonBehaviours)
            {
                if (extention && extention.GetUdonTypeName() == udonTypeName)
                {
                    result[count++] = extention;
                }
            }
            foreach (var extention in entity.Dial_Functions_L)
            {
                if (extention && extention.GetUdonTypeName() == udonTypeName)
                {
                    result[count++] = extention;
                }
            }
            foreach (var extention in entity.Dial_Functions_R)
            {
                if (extention && extention.GetUdonTypeName() == udonTypeName)
                {
                    result[count++] = extention;
                }
            }

            var finalResult = new UdonSharpBehaviour[count];
            System.Array.Copy(result, finalResult, count);

            return finalResult;
        }
        #endregion
    }
}
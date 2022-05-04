
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace EsnyaSFAddons
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_AdvancedCatapult : UdonSharpBehaviour
    {
        public DFUNC_Catapult catapult;
        public GameObject Dial_Funcon;
        public string launchHookParameterName = "LaunchHookDown";


        private Animator vehicleAnimator;
        private string triggerAxis;
        private bool triggerLastFrame;
        private bool selected;

        [UdonSynced][FieldChangeCallback(nameof(LaunchHookDown))] private bool _launchHookDown;
        public bool LaunchHookDown
        {
            set
            {
                _launchHookDown = value;
                if (vehicleAnimator) vehicleAnimator.SetBool(launchHookParameterName, value);
                if (Dial_Funcon) Dial_Funcon.SetActive(value);
                if (Networking.IsOwner(gameObject))
                {
                    if (value) catapult.SFEXT_O_PilotEnter();
                    else catapult.SFEXT_O_PilotExit();
                }
            }
            get => _launchHookDown;
        }

        public void DFUNC_LeftDial() { triggerAxis = "Oculus_CrossPlatform_PrimaryIndexTrigger"; }
        public void DFUNC_RightDial() { triggerAxis = "Oculus_CrossPlatform_SecondaryIndexTrigger"; }
        public void DFUNC_Selected()
        {
            triggerLastFrame = true;
            selected = true;
        }
        public void DFUNC_Deselected()
        {
            selected = false;
        }

        public void SFEXT_L_EntityStart()
        {
            var vehicleRigidbody = GetComponentInParent<Rigidbody>();
            vehicleAnimator = vehicleRigidbody.GetComponent<Animator>();

            catapult.SFEXT_L_EntityStart();
            SFEXT_G_ReAppear();
        }

        public void SFEXT_O_TakeOwnership()
        {
            Networking.SetOwner(Networking.LocalPlayer, catapult.gameObject);
            catapult.SFEXT_O_TakeOwnership();
        }
        public void SFEXT_O_LoseOwnership() => catapult.SFEXT_O_LoseOwnership();
        public void SFEXT_O_Explode() => catapult.SFEXT_O_Explode();

        public void SFEXT_G_GearDown()
        {
            gameObject.SetActive(true);
        }

        public void SFEXT_G_GearUp()
        {
            LaunchHookDown = false;
            if (Networking.IsOwner(gameObject)) RequestSerialization();
            gameObject.SetActive(false);
        }

        public void SFEXT_O_PilotExit()
        {
            selected = false;
        }

        public void KeyboardInput()
        {
            ToggleLauchHook();
        }

        public void SFEXT_G_ReAppear()
        {
            LaunchHookDown = false;
        }

        public void Update()
        {
            if (selected)
            {
                var trigger = Input.GetAxisRaw(triggerAxis) > 0.75f;
                if (!triggerLastFrame && trigger)
                {
                    ToggleLauchHook();
                }
                triggerLastFrame = trigger;
            }
        }

        private void ToggleLauchHook()
        {
            LaunchHookDown = !LaunchHookDown;
            RequestSerialization();
        }
    }
}

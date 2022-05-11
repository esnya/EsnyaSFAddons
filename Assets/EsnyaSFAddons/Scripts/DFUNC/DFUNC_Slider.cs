
using UdonSharp;
using UdonToolkit;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace EsnyaSFAddons
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class DFUNC_Slider : UdonSharpBehaviour
    {
        public float defaultValue = 0.0f;
        public bool resetOnPilotExit = false;

        [SectionHeader("VR Input")]
        public float vrSensitivity = 5f;
        public Vector3 vrAxis = Vector3.forward;

        [SectionHeader("Desktop Input")]
        public float desktopStep = 0.2f;
        public KeyCode desktopIncrease, desktopDecrease;

        [SectionHeader("Public Variable")]
        public bool writePublicVariable;
        [HideIf("@!writePublicVariable")] public UdonSharpBehaviour targetBehaviour;
        [HideIf("@!writePublicVariable")][Popup("programVariable", "@targetBehaviour", "float")] public string targetVariableName;

        [SectionHeader("Animator")]
        public bool writeAnimatorParameter;
        [HideIf("@!writeAnimatorParameter")]
        public Animator targetAnimator;
        [HideIf("@!writeAnimatorParameter")][Popup("animatorFloat", "@targetAnimator")] public string targetAnimatorParameterName;

        private string triggerAxis;
        private bool prevTrigger;
        private bool isSelected;
        private float originValue;
        private Vector3 originPosition;
        private Transform controlsRoot;
        private VRCPlayerApi.TrackingDataType trackingHand;

        [UdonSynced(UdonSyncMode.Smooth)][FieldChangeCallback(nameof(Value))] private float _value;
        private bool isPilot;

        private float Value
        {
            set
            {
                _value = value;
                if (writePublicVariable && targetBehaviour) targetBehaviour.SetProgramVariable(targetVariableName, value);
                if (writeAnimatorParameter && targetAnimator) targetAnimator.SetFloat(targetAnimatorParameterName, value);
            }
            get => _value;
        }

        public void DFUNC_LeftDial()
        {
            triggerAxis = "Oculus_CrossPlatform_PrimaryIndexTrigger";
            trackingHand = VRCPlayerApi.TrackingDataType.LeftHand;
        }
        public void DFUNC_RightDial()
        {
            triggerAxis = "Oculus_CrossPlatform_SecondaryIndexTrigger";
            trackingHand = VRCPlayerApi.TrackingDataType.RightHand;
        }

        public void DFUNC_Selected()
        {
            prevTrigger = false;
            isSelected = true;
        }
        public void DFUNC_Deselected()
        {
            isSelected = false;
        }

        public void SFEXT_L_EntityStart()
        {
            var saccEntity = GetComponentInParent<SaccEntity>();
            var airVehicle = (SaccAirVehicle)saccEntity.GetExtention(GetUdonTypeName<SaccAirVehicle>());
            controlsRoot = airVehicle.ControlsRoot ?? saccEntity.transform;

            Value = defaultValue;

            DFUNC_Deselected();
        }

        public void SFEXT_O_PilotEnter() => isPilot = true;
        public void SFEXT_O_PilotExit()
        {
            isPilot = false;
            if (resetOnPilotExit) Value = defaultValue;
            DFUNC_Deselected();
        }

        public void SFEXT_G_PilotEnter() => gameObject.SetActive(true);
        public void SFEXT_G_PilotExit() => gameObject.SetActive(false);

        public void Increase()
        {
            Value = Mathf.Clamp01(Value + desktopStep);
        }

        public void Decrease()
        {
            Value = Mathf.Clamp01(Value + desktopStep);
        }

        private void Update()
        {
            if (isPilot)
            {
                if (Input.GetKeyDown(desktopIncrease)) Increase();
                else if (Input.GetKeyDown(desktopDecrease)) Decrease();
            }
        }

        public override void PostLateUpdate()
        {
            if (isSelected)
            {
                var trigger = Input.GetAxisRaw(triggerAxis) > 0.75f;
                if (trigger)
                {
                    var handPosition = controlsRoot.InverseTransformPoint(Networking.LocalPlayer.GetTrackingData(trackingHand).position);
                    if (!prevTrigger)
                    {
                        originValue = Value;
                        originPosition = handPosition;
                    }
                    else
                    {
                        Value = Mathf.Clamp01(originValue + Vector3.Dot(handPosition - originPosition, vrAxis) * vrSensitivity);
                    }
                }
                prevTrigger = trigger;
            }
        }
    }
}

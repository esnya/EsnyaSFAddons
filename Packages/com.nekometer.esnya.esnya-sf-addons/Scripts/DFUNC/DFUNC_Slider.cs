
using UdonSharp;
using UdonToolkit;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace EsnyaSFAddons.DFUNC
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
        public bool desktopLoop;

        [SectionHeader("Public Variable")]
        public bool writePublicVariable;
        [HideIf("@!writePublicVariable")] public UdonSharpBehaviour targetBehaviour;
        [HideIf("@!writePublicVariable")][Popup("programVariable", "@targetBehaviour", "float")] public string targetVariableName;

        [SectionHeader("Animator")]
        public bool writeAnimatorParameter;
        [HideIf("@!writeAnimatorParameter")]
        public Animator targetAnimator;
        [HideIf("@!writeAnimatorParameter")][Popup("animatorFloat", "@targetAnimator")] public string targetAnimatorParameterName;

        [SectionHeader("Send Events")]
        public bool sendOnChange;
        [HideIf("@!sendOnChange")] public string onChange = "SFEXT_G_SliderValueCange";
        public bool sendOnMin;
        [HideIf("@!sendOnMin")] public string onMin = "SFEXT_G_SliderMin";
        public bool sendOnMax;
        [HideIf("@!sendOnMax")] public string onMax = "SFEXT_G_SliderMax";

        private string triggerAxis;
        private bool prevTrigger;
        private bool isSelected;
        private float originValue;
        private Vector3 originPosition;
        private Transform controlsRoot;
        private VRCPlayerApi.TrackingDataType trackingHand;

        [UdonSynced(UdonSyncMode.Smooth)][FieldChangeCallback(nameof(Value))] private float _value;
        private bool isPilot;
        private SaccEntity entity;

        private float Value
        {
            set
            {
                var clampedValue = Mathf.Clamp01(value);
                if (writePublicVariable && targetBehaviour) targetBehaviour.SetProgramVariable(targetVariableName, clampedValue);
                if (writeAnimatorParameter && targetAnimator) targetAnimator.SetFloat(targetAnimatorParameterName, clampedValue);
                if (clampedValue != _value && entity)
                {
                    if (sendOnChange) entity.SendEventToExtensions(onChange);
                    if (sendOnMin && Mathf.Approximately(clampedValue, 0)) entity.SendEventToExtensions(onMin);
                    if (sendOnMax && Mathf.Approximately(clampedValue, 1)) entity.SendEventToExtensions(onMax);
                }
                _value = clampedValue;
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
            entity = GetComponentInParent<SaccEntity>();
            var airVehicle = (SaccAirVehicle)entity.GetExtention(GetUdonTypeName<SaccAirVehicle>());
            controlsRoot = airVehicle.ControlsRoot ?? entity.transform;

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
            Value += desktopStep;
        }

        public void Decrease()
        {
            Value -= desktopStep;
        }

        private float Loop01(float value)
        {
            return value > 1.0f ? 0.0f : value < 0.0f ? 1.0f : value;
        }

        public void IncreaseLooped()
        {
            Value = Loop01(Value + desktopStep);
        }
        public void DecreaseLooped()
        {
            Value = Loop01(Value - desktopStep);
        }

        private void Update()
        {
            if (isPilot)
            {
                if (Input.GetKeyDown(desktopIncrease))
                {
                    if (desktopLoop) IncreaseLooped();
                    else Increase();
                }
                else if (Input.GetKeyDown(desktopDecrease))
                {
                    if (desktopLoop) DecreaseLooped();
                    else Decrease();
                }
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

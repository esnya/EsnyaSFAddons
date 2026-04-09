
using SaccFlightAndVehicles;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace EsnyaSFAddons.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class DFUNC_Slider : UdonSharpBehaviour
    {
        public float defaultValue = 0.0f;
        public bool resetOnPilotExit = false;

        [Header("VR Input")]
        public float vrSensitivity = 5f;
        public Vector3 vrAxis = Vector3.forward;

        [Header("Desktop Input")]
        public float desktopStep = 0.2f;
        public KeyCode desktopIncrease, desktopDecrease;
        public bool desktopLoop;

        [Header("Public Variable")]
        public bool writePublicVariable;
        public UdonSharpBehaviour targetBehaviour;
        public string targetVariableName;
        public float targetVariableMin = 0.0f;
        public float targetVariableMax = 1.0f;

        [Header("Animator")]
        public bool writeAnimatorParameter;
        public Animator targetAnimator;
        public string targetAnimatorParameterName;

        [Header("Send Events")]
        public bool sendOnChange;
        public string onChange = "SFEXT_G_SliderValueChange";
        public bool sendOnMin;
        public string onMin = "SFEXT_G_SliderMin";
        public bool sendOnMax;
        public string onMax = "SFEXT_G_SliderMax";

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
                if (writePublicVariable && targetBehaviour && !string.IsNullOrEmpty(targetVariableName)) targetBehaviour.SetProgramVariable(targetVariableName, clampedValue * (targetVariableMax - targetVariableMin) + targetVariableMin);
                if (writeAnimatorParameter && targetAnimator && !string.IsNullOrEmpty(targetAnimatorParameterName)) targetAnimator.SetFloat(targetAnimatorParameterName, clampedValue);
                if (clampedValue != _value && entity)
                {
                    if (sendOnChange && !string.IsNullOrEmpty(onChange)) entity.SendEventToExtensions(onChange);
                    if (sendOnMin && Mathf.Approximately(clampedValue, 0) && !string.IsNullOrEmpty(onMin)) entity.SendEventToExtensions(onMin);
                    if (sendOnMax && Mathf.Approximately(clampedValue, 1) && !string.IsNullOrEmpty(onMax)) entity.SendEventToExtensions(onMax);
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
            if (Mathf.Approximately(value, 1.0f)) return 1.0f;
            if (Mathf.Approximately(value, 0.0f)) return 0.0f;
            return value > 1.0f ? 0.0f : value < 0.0f ? 1.0f : value;
        }

        public void IncreaseLooped()
        {
            Value = Value >= 1.0f ? 0.0f : Value + desktopStep;
        }
        public void DecreaseLooped()
        {
            Value = Value <= 0.0f ? 1.0f : Value - desktopStep;
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

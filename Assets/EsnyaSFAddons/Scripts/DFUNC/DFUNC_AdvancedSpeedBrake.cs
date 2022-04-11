
using BestHTTP.SecureProtocol.Org.BouncyCastle.Security;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace EsnyaSFAddons
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class DFUNC_AdvancedSpeedBrake : UdonSharpBehaviour
    {
        public float liftMultiplier = 0.6f;
        public float dragMultiplier = 1.4f;
        public float response = 1.0f;

        [Header("Inputs")]
        public float vrInputDistance = 0.1f;
        public float step = 0.2f;
        public KeyCode desktopKey = KeyCode.B;

        [Header("Animation")]
        public string floatParameterName = "speedbrake";
        public string floatInputParameterName = "speedbrakeinput";
        public GameObject Dial_Funcon;

        private SaccAirVehicle airVehicle;
        private Animator vehicleAnimator;
        private Transform controlsRoot;
        private string triggerAxis;
        private VRCPlayerApi.TrackingDataType trackingTarget;

        [UdonSynced(UdonSyncMode.Smooth)][FieldChangeCallback(nameof(TargetAngle))] private float _targetAngle;
        public float TargetAngle
        {
            private set {
                vehicleAnimator.SetFloat(floatInputParameterName, value);
                _targetAngle = value;
            }
            get => _targetAngle;
        }
        private bool isPilot;
        private bool isSelected;

        private float _angle;
        public float Angle
        {
            private set
            {
                var clamped = Mathf.Clamp01(value);

                var extended = clamped > 0;

                var diff = clamped - _angle;
                airVehicle.ExtraLift += diff * liftMultiplier;
                airVehicle.ExtraDrag += diff * dragMultiplier;

                vehicleAnimator.SetFloat(floatParameterName, clamped);

                if (Dial_Funcon && Dial_Funcon.activeSelf != extended) Dial_Funcon.SetActive(extended);

                _angle = clamped;
            }
            get => _angle;
        }

        private Vector3 prevHandPosition;
        private bool _triggerState;
        private bool TriggerState
        {
            set
            {
                if (value && !_triggerState) OnTriggerDown();
                _triggerState = value;
            }
            get => _triggerState;
        }

        public void DFUNC_LeftDial()
        {
            triggerAxis = "Oculus_CrossPlatform_PrimaryIndexTrigger";
            trackingTarget = VRCPlayerApi.TrackingDataType.LeftHand;
        }
        public void DFUNC_RightDial()
        {
            triggerAxis = "Oculus_CrossPlatform_SecondaryIndexTrigger";
            trackingTarget = VRCPlayerApi.TrackingDataType.RightHand;
        }

        public void DFUNC_Selected()
        {
            isSelected = true;
        }
        public void DFUNC_Deselected()
        {
            isSelected = false;
        }

        public void SFEXT_L_EntityStart()
        {
            var entity = GetComponentInParent<SaccEntity>();
            airVehicle = (SaccAirVehicle)entity.GetExtention(GetUdonTypeName<SaccAirVehicle>());
            vehicleAnimator = entity.GetComponent<Animator>();

            controlsRoot = airVehicle.ControlsRoot ? airVehicle.ControlsRoot : entity.transform;
        }

        public void SFEXT_G_PilotEnter()
        {
            gameObject.SetActive(true);
        }

        public void SFEXT_G_PilotExit()
        {
            gameObject.SetActive(false);
        }

        public void SFEXT_O_PilotEnter()
        {
            isPilot = true;
        }

        public void SFEXT_O_PilotExit()
        {
            isPilot = false;
            isSelected = false;
        }

        public void SFEXT_G_ReAppear()
        {
            TargetAngle = 0;
            Angle = 0;
        }

        private void Update()
        {
            if (isPilot)
            {
                TriggerState = isSelected && Input.GetAxisRaw(triggerAxis) > 0.75f;

                if (Input.GetKeyDown(desktopKey)) TargetAngle = 1.0f;
                else if (Input.GetKeyUp(desktopKey)) TargetAngle = 0.0f;
            }

            if (!Mathf.Approximately(TargetAngle, Angle))
            {
                Angle = Mathf.MoveTowards(Angle, TargetAngle, response * Time.deltaTime);
            }
        }
        private Vector3 GetLocalHandPosition()
        {
            return controlsRoot.InverseTransformPoint(Networking.LocalPlayer.GetTrackingData(trackingTarget).position); ;
        }

        private void OnTriggerDown()
        {
            prevHandPosition = GetLocalHandPosition();
        }

        public override void PostLateUpdate()
        {
            if (isPilot && TriggerState)
            {
                var handPosition = GetLocalHandPosition();
                TargetAngle -= Vector3.Dot(handPosition - prevHandPosition, Vector3.forward) / vrInputDistance;
                prevHandPosition = handPosition;
            }
        }

        public void IncreaseAngle()
        {
            TargetAngle += step;
        }
        public void DecreaseAngle()
        {
            TargetAngle -= step;
        }
    }
}

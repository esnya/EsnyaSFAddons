
using System;
using SaccFlightAndVehicles;
using UdonSharp;
using UnityEngine;

namespace EsnyaSFAddons.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_ThrustReverser : UdonSharpBehaviour
    {
        public float ReversingThrottleMultiplier = -.5f;
        public KeyCode KeyboardControl = KeyCode.R;
        [Tooltip("SAVControl.VehicleAnimator when null")] public Animator ThrustReverserAnimator;
        public string ParameterName = "reverse";

        [NonSerialized] public float ReversingEngineOutput;
        private Rigidbody VehicleRigidbody;
        private SaccEntity EntityControl;
        private SaccAirVehicle SAVControl;
        private float ThrottleStrength, ReversingThrottleStrength, AccelerationResponse, EngineSpoolDownSpeedMulti;
        private bool UseLeftTrigger, Selected, isPilot, lowFuel;
        private bool HasWheelColliders;
        [UdonSynced][FieldChangeCallback(nameof(Reversing))] private bool _reversing;
        public bool Reversing
        {
            private set
            {
                if (value == _reversing) return;

                _reversing = value;

                if (isPilot)
                {
                    SAVControl.ThrottleStrength = value ? -ReversingThrottleStrength : ThrottleStrength;

                    SAVControl.ThrottleOverridden_ += value ? 1 : -1;
                    SAVControl.ThrottleOverride = value ? 1.0f : 0.0f;
                }

                if (value)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_StartReversing");
                }
                else
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_StopReversing");
                }
                ThrustReverserAnimator.SetBool(ParameterName, value);
            }
            get => _reversing;
        }


        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }

        public void DFUNC_Selected() => Selected = true;
        public void DFUNC_Deselected() => Selected = false;

        public void SFEXT_L_EntityStart()
        {
            EntityControl = GetComponentInParent<SaccEntity>();
            SAVControl = EntityControl.GetComponentInChildren<SaccAirVehicle>(true);
            if (ThrustReverserAnimator == null) ThrustReverserAnimator = SAVControl.VehicleAnimator;
            VehicleRigidbody = EntityControl.GetComponent<Rigidbody>();
            AccelerationResponse = SAVControl.AccelerationResponse;
            EngineSpoolDownSpeedMulti = SAVControl.EngineSpoolDownSpeedMulti;
            ThrottleStrength = SAVControl.ThrottleStrength;
            ReversingThrottleStrength = SAVControl.ThrottleStrength * ReversingThrottleMultiplier;

            HasWheelColliders = SAVControl.VehicleMesh.GetComponentInChildren<WheelCollider>(true) != null;
        }

        public void SFEXT_O_PilotEnter()
        {
            isPilot = true;
            Reversing = false;
            ReversingEngineOutput = 0;
            RequestSerialization();
        }

        public void SFEXT_O_PilotExit()
        {
            isPilot = false;
            Reversing = false;
            RequestSerialization();
        }

        public void SFEXT_G_PilotEnter()
        {
            gameObject.SetActive(true);
            ReversingEngineOutput = 0;
        }

        public void SFEXT_G_PilotExit()
        {
            gameObject.SetActive(false);
            ReversingEngineOutput = 0;
        }

        public void SFEXT_G_LowFuel()
        {
            lowFuel = true;
        }
        public void SFEXT_G_NotLowFuel()
        {
            lowFuel = false;
        }

        private float GetInput()
        {
            if (lowFuel) return 0.0f;

            if (Input.GetKey(KeyboardControl)) return 1.0f;

            if (!Selected) return 0.0f;

            if (UseLeftTrigger) return Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
            return Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
        }

        private void Update()
        {
            if (isPilot)
            {
                var trigger = GetInput() > 0.75f && SAVControl.EngineOn;
                if (trigger != Reversing)
                {
                    Reversing = trigger;
                    RequestSerialization();
                }
            }
        }
    }
}

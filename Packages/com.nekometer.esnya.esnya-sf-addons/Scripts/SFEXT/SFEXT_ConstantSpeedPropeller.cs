using System;
using JetBrains.Annotations;
using UnityEngine;
using UdonSharp;
using SaccFlightAndVehicles;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using VRC.Udon;

namespace EsnyaSFAddons
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    [DefaultExecutionOrder(10)] // After SaccAirVehicle
    public class SFEXT_ConstantSpeedPropeller : UdonSharpBehaviour
    {
        /// <summary>
        /// Diameter of the propeller in meters.
        /// </summary>
        [Min(0)] public float diameter = 1.9f;

        // /// <summary>
        // /// Number of blades.
        // /// </summary>
        // [Min(1)] public int blades = 2;

        /// <summary>
        /// Interception points of the kt curve at minimum blade pitch.
        /// </summary>
        public Vector2 kt0 = new Vector2(1.8f, 0.13f);

        /// <summary>
        /// Interception points of the kt curve at maximum blade pitch.
        /// </summary>
        public Vector2 kt1 = new Vector2(2.6f, 0.24f);

        /// <summary>
        /// Interception points of the kq curve at minimum blade pitch.
        /// </summary>
        public Vector2 kq0 = new Vector2(2.0f, 0.02f);

        /// <summary>
        /// Interception points of the kq curve at maximum blade pitch.
        /// </summary>
        public Vector2 kq1 = new Vector2(2.8f, 0.04f);

        /// <summary>
        /// Moment of inertia of power train and propeller in kgm^2.
        /// </summary>
        [Min(0)] public float momentOfInertia = 15f;

        /// <summary>
        /// Gear ratio between propeller and power train.
        /// </summary>
        [Min(0)] public float gearRatio = 1.0f;

        /// <summary>
        /// Response of governor.
        /// </summary>
        [Min(0)] public float governorResponse = 0.01f;

        /// <summary>
        /// Minimum rpm of the power train. If revolution is lower than this value, the propeller will not rotate.
        /// </summary>
        [Min(0)] public float engineMinRpm = 500f;

        /// <summary>
        /// Starter rpm of the power train.
        /// </summary>
        [Min(0)] public float engineStarterRpm = 1000f;

        /// <summary>
        /// Maximum engine power in [W].
        /// </summary>
        public float enginePower = 123.5f * 1000;

        /// <summary>
        /// Idling engine power in [W].
        /// </summary>
        [Min(0)] public float engineIdlePower = 10f * 1000;

        /// <summary>
        /// Normalized engine power curve.
        /// </summary>
        public AnimationCurve enginePowerCurve;

        /// <summary>
        /// Total efficiency of the power train and propeller.
        /// </summary>
        public float efficiency = 0.8f;

        /// <summary>
        /// Reference rpm to normalize egine output.
        /// </summary>
        [Min(0)] public float referenceRpm = 2300f;

        /// <summary>
        /// Visual transform of the propeller.
        /// </summary>
        public Transform propellerVisual;

        /// <summary>
        /// Visual rotation axis of the propeller. Use backward to counter clockwise rotation.
        /// </summary>
        public Vector3 propellerVisualAxis = Vector3.forward;

        /// <summary>
        /// Input for target propeller RPM.
        /// </summary>
        public float targetRpm = 1800;

        /// <summary>
        /// Enable or disable electric propeller RPM control.
        /// </summary>
        public bool enableElectricPropellerRpmControl = true;

        /// <summary>
        /// Propeller RPM target vs throttle input curve.
        /// </summary>
        public AnimationCurve targetRpmCurve;


        [NonSerialized][UdonSynced(UdonSyncMode.Smooth)] public float n = 0;
        [NonSerialized][UdonSynced(UdonSyncMode.Smooth)] public float brakeTorque;
        [NonSerialized] public float bladePitch = 0;

        [NonSerialized] public SaccEntity EntityControl;
        private Rigidbody vehicleRigidbody;
        private SaccAirVehicle airVehicle;
        private bool starter = false;


        public void SFEXT_L_EntityStart()
        {
            Debug.Log(EntityControl, this);
            vehicleRigidbody = EntityControl.GetComponent<Rigidbody>();
            airVehicle = (SaccAirVehicle)EntityControl.GetExtention(GetUdonTypeName<SaccAirVehicle>());
            airVehicle.ThrottleStrength = 0;
        }

        private bool hasPilot;
        public void SFEXT_G_PilotEnter()
        {
            hasPilot = true;
            gameObject.SetActive(true);
        }
        public void SFEXT_G_PilotExit() => hasPilot = false;

        private bool isPiloting = false, isOwner = false;
        public void SFEXT_O_PilotEnter() => isPiloting = isOwner = true;
        public void SFEXT_O_PilotExit() => isPiloting = false;

        private bool engineOn = false;
        public void SFEXT_G_EngineStartup() => starter = engineOn = true;
        public void SFEXT_G_EngineStartupCancel() => starter = false;
        public void SFEXT_G_EngineOn() => starter = false;
        public void SFEXT_G_EngineOff() => engineOn = false;

        private float thrust;
        private void OnEnable()
        {
            thrust = Mathf.Epsilon;
        }

        public void Deactivate()
        {
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (isOwner) PilotUpdate();

            if (propellerVisual)
            {
                propellerVisual.localRotation = Quaternion.AngleAxis(n * 360 * Time.deltaTime, propellerVisualAxis) * propellerVisual.localRotation;
            }
        }

        private void PilotUpdate()
        {
            if (starter)
            {
                n = engineStarterRpm / 60;
                return;
            }

            var airVel = airVehicle.AirVel;
            var v = Vector3.Dot(airVel, transform.forward);

            var powerTrainN = n / gearRatio;
            var powerTrainRpm = powerTrainN * 60;
            if (powerTrainRpm < engineMinRpm)
            {
                n = 0;
                if (!hasPilot && !engineOn) SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Deactivate));
                return;
            }

            var deltaTime = Time.deltaTime;

            var j = v / (n * diameter);
            var rho = airVehicle.Atmosphere * 1.225f;
            var c2 = rho * Mathf.Pow(n, 2) * Mathf.Pow(diameter, 4);
            thrust = CalculateK(kt0, kt1, bladePitch, j) * c2;
            brakeTorque = CalculateK(kq0, kq1, bladePitch, j) * c2 * diameter;

            if (enableElectricPropellerRpmControl)
            {
                targetRpm = targetRpmCurve.Evaluate(airVehicle.ThrottleInput);
            }

            bladePitch = Mathf.Clamp01(bladePitch - (targetRpm - powerTrainRpm * gearRatio) * deltaTime * governorResponse);

            var availableTorque = engineOn ? Mathf.Lerp(engineIdlePower, enginePower, airVehicle.ThrottleInput) * enginePowerCurve.Evaluate(powerTrainRpm) * gearRatio * efficiency / (Mathf.PI * 2 * powerTrainN) : 0.0f;
            n += (availableTorque - brakeTorque) * deltaTime / momentOfInertia;

            airVehicle.EngineOutput = Mathf.Clamp01(n * 60 / referenceRpm);
        }

        private void FixedUpdate()
        {
            if (!isOwner || Mathf.Approximately(thrust, 0)) return;

            vehicleRigidbody.AddForceAtPosition(transform.forward * thrust, transform.position, ForceMode.Force);
        }

        private float CalculateK(Vector2 intercepts0, Vector2 intercepts1, float p, float j)
        {
            var intercepts = Vector2.Lerp(intercepts0, intercepts1, p);
            return (intercepts.y / -intercepts.x) * j + intercepts.y;
        }
#if UNITY_EDITOR && !UDONSHARP_COMPILER
        private void Reset()
        {
            enginePowerCurve = AnimationCurve.Linear(0, 1, 2500, 1);
            targetRpmCurve = AnimationCurve.Linear(0, 1800, 1, 2300);
        }
#endif
    }
}

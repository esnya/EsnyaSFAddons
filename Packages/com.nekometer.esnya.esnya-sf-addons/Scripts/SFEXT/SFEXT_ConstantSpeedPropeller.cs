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


        /// <summary>
        /// Animator parameter name for propeller RPM.
        /// </summary>
        [Header("Animation")]
        public string animatorPropellerRpmParameterName = "propellerrpm";

        /// <summary>
        /// Max propeller RPM to normalize animator parameter.
        /// </summary>
        public float animatorMaxPropellerRpm = 3000;

        /// <summary>
        /// Enable or disable failures.
        /// </summary>
        [Header("Failures")]
        public bool failures = true;

        /// <summary>
        /// Mean time between failures in hours.
        /// </summary>
        public float mtbf = 1000;

        /// <summary>
        /// RPM limitations.
        /// </summary>
        public float[] rpmLimits = new float[] { 2300, 2500 };

        /// <summary>
        /// Limit duration in seconds.
        /// </summary>
        public float[] rpmLimitDurations = new float[] { 5 * 60 , 20 };

        /// <summary>
        /// Load factor limitations.
        /// </summary>
        public float[] loadLimits = new float[] { 1.0f };

        /// <summary>
        /// Limit duration in seconds.
        /// </summary>
        public float[] loadLimitDurations = new float[] { 5 * 60 };

        /// <summary>
        /// Effects to enabled when the propeller is broken. Such as smoke or fire, sound, etc.
        /// </summary>
        public GameObject brokenEffects;

        [UdonSynced][FieldChangeCallback(nameof(Broken))] private bool _broken;
        public bool Broken
        {
            get => _broken;
            set
            {
                if (_broken != value && brokenEffects)
                {
                    brokenEffects.SetActive(value);

                    if (value && Networking.IsOwner(gameObject) && airVehicle)
                    {
                        airVehicle.EngineOn = false;
                    }
                }


                _broken = value;
            }
        }

#if ESFA_DEBUG
        [Header("For Debug")]
        [UdonSynced(UdonSyncMode.Smooth)] public float n = 0;
        public float brakeTorque;
        [UdonSynced(UdonSyncMode.Smooth)] public float load;
        public float bladePitch = 0;
        public float j;
        public float thrust;
#else
        [NonSerialized][UdonSynced(UdonSyncMode.Smooth)] public float n = 0;
        [NonSerialized]public float brakeTorque;
        [NonSerialized][UdonSynced(UdonSyncMode.Smooth)] public float load;
        private float bladePitch = 0;
        private float j;
        private float thrust;
#endif

        [NonSerialized] public SaccEntity EntityControl;
        private Animator vehicleAnimator;
        private Rigidbody vehicleRigidbody;
        private SaccAirVehicle airVehicle;
        private bool starter = false;


        public void SFEXT_L_EntityStart()
        {
            Debug.Log(EntityControl, this);
            vehicleRigidbody = EntityControl.GetComponent<Rigidbody>();
            airVehicle = (SaccAirVehicle)EntityControl.GetExtention(GetUdonTypeName<SaccAirVehicle>());
            airVehicle.ThrottleStrength = 0;
            vehicleAnimator = airVehicle.VehicleAnimator;
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

        public void SFEXT_G_ReAppear() => SFEXT_O_RespawnButton();
        public void SFEXT_O_RespawnButton() => ReAppear();
        public void ReAppear()
        {
            Broken = false;
            n = thrust = load = brakeTorque = bladePitch = 0;
        }

        private void OnEnable()
        {
            thrust = 0;
        }

        public void Deactivate()
        {
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (isOwner) PilotUpdate();

            if (vehicleAnimator)
            {
                vehicleAnimator.SetFloat(animatorPropellerRpmParameterName, Mathf.Clamp01(n * 60 / animatorMaxPropellerRpm));
            }

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
                if (airVehicle && !starter) airVehicle.EngineOn = false;
                if (!hasPilot && !engineOn && !Broken) SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Deactivate));
                return;
            }

            var deltaTime = Time.deltaTime;

            j = v / (n * diameter);
            var rho = airVehicle.Atmosphere * 1.225f;
            var c2 = rho * Mathf.Pow(n, 2) * Mathf.Pow(diameter, 4);
            thrust = CalculateK(kt0, kt1, bladePitch, j) * c2;
            brakeTorque = CalculateK(kq0, kq1, bladePitch, j) * c2 * diameter;

            if (enableElectricPropellerRpmControl)
            {
                targetRpm = targetRpmCurve.Evaluate(airVehicle.ThrottleInput);
            }

            bladePitch = Mathf.Clamp01(bladePitch - (targetRpm - powerTrainRpm * gearRatio) * deltaTime * governorResponse);

            var availableTorque = (engineOn && !Broken) ? Mathf.Lerp(engineIdlePower, enginePower, airVehicle.ThrottleInput) * enginePowerCurve.Evaluate(powerTrainRpm) * gearRatio * efficiency / (Mathf.PI * 2 * powerTrainN) : 0.0f;
            n += (availableTorque - brakeTorque) * deltaTime / momentOfInertia;

            load = brakeTorque * Mathf.PI * 2 * n * gearRatio / (enginePower * efficiency);

            airVehicle.EngineOutput = Mathf.Clamp01(n * 60 / referenceRpm);

            if (failures && CheckForFailure(powerTrainRpm, load, deltaTime))
            {
                Broken = true;
            }
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

        public float CalculateRpmFailureProbability(float currentRpm)
        {
            var mtbf_seconds  = mtbf * 3600;
            var basicProbability = 1 / mtbf_seconds ;

            for (var i = rpmLimits.Length - 1; i >= 0; i--)
            {
                if (currentRpm > rpmLimits[i])
                {
                    return basicProbability * (mtbf_seconds / rpmLimitDurations[i]);
                }
            }

            return basicProbability * (currentRpm / rpmLimits[0]);
        }

        public float CalculateLoadFactorFailureProbability(float currentLoadFactor)
        {
            var mtbf_seconds  = mtbf * 3600;
            float basicProbability = 1 / mtbf_seconds;

            for (var i = loadLimits.Length - 1; i >= 0; i--)
            {
                if (currentLoadFactor > loadLimits[i])
                {
                    return basicProbability * (mtbf_seconds / loadLimitDurations[i]);
                }
            }
            return basicProbability * (currentLoadFactor / loadLimits[0]);
        }

        public bool CheckForFailure(float currentRpm, float currentLoadFactor, float deltaTime)
        {
            var rpmProbability = CalculateRpmFailureProbability(currentRpm);
            var loadFactorProbability = CalculateLoadFactorFailureProbability(currentLoadFactor);

            var combinedProbability = (rpmProbability + loadFactorProbability) / 2;

            return UnityEngine.Random.value < combinedProbability * deltaTime;
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

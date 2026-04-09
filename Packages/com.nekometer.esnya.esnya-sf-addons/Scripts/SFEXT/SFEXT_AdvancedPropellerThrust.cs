using System;
using JetBrains.Annotations;
using SaccFlightAndVehicles;
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common.Interfaces;
using VRC.SDKBase;

namespace EsnyaSFAddons.SFEXT
{

    /// <summary>
    /// Advanced Thrust System for Proppeller Aircrafts.
    ///
    /// Overrides seaLevelThrust of SaccAirVehicle.
    /// </summary>

    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class SFEXT_AdvancedPropellerThrust : UdonSharpBehaviour
    {
        [Header("Specs")]
        /// <summary>
        /// Maximum power at sea-level in hp.
        /// </summary>
        [Tooltip("hp")] public float power = 160.0f;

        /// <summary>
        /// Diameter of propeller in meter.
        /// </summary>
        [Tooltip("m")] public float diameter = 1.9304f;

        /// <summary>
        /// Max RPM per altitude.
        /// </sumamry>
        [NotNull][Tooltip("rpm")] public AnimationCurve maxRPMCurve = AnimationCurve.Linear(0.0f, 2700.0f, 20000.0f, 2500.0f);

        /// <summary>
        /// Idle RPM at sea-level and full-rich.
        /// </summary>
        [Tooltip("rpm")] public float minRPM = 600;

        /// <summary>
        /// How throtle effects RPM.
        ///
        /// time: Throttle (0.0 to 1.0)
        /// value: RPM ratio between minRPM and maxRPM (0.0 to 1.0)
        /// </summary>
        [NotNull][Tooltip("Throttle vs RPM")] public AnimationCurve throttleCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

        /// <summary>
        /// Thrust redused 50% at the altitude
        /// </summary>
        public float halfPowerAltitude = 21000.0f;

        /// <summary>
        /// Best mixture control position vs altitude in feet.
        /// </summary>
        [NotNull][Tooltip("Best mixture control vs altitude")] public AnimationCurve bestMixtureControlCurve = AnimationCurve.Linear(0.0f, 0.8f, 20000.0f, 0.1f);

        /// <summary>
        /// How much effects mixture to RPM.
        /// </summary>
        public float mixtureErrorCoefficient = 0.3f;

        /// <summary>
        /// Response of RPM
        /// </summary>
        public float rpmResponse = 1.0f;

        [Header("Startup")]
        /// <summary>
        /// Delay in seconds to cut off engine.
        /// </summary>
        public float mixtureCutOffDelay = 1.0f;

        /// <summary>
        /// Battery bus object to enable starter.
        /// </summary>
        [CanBeNull] public GameObject batteryBus;

        [Header("Animation")]
        /// <summary>
        /// Animator parameter name.
        ///
        /// 0 to animationMaxRPM will be remapped 0 to 1.
        /// </summary>
        public string rpmFloatParameter = "rpm";
        /// <summary>
        /// Max value
        /// </summary>
        public float animationMaxRPM = 3500;

        /// <summary>
        /// Animator parameter name.
        /// </summary>
        public string oilTempFloatParameter = "oiltemp";

        /// <summary>
        /// Response of oil temperature.
        /// </summary>
        public float oilTempResponse = 0.1f;

        [Header("Failure")]
        /// <summary>
        /// Enable engine stall simulation.
        /// </summary>
        public bool engineStall = true;

        /// <summary>
        /// Negative load factor limitation.
        /// </summary>
        [Tooltip("G")] public float minimumNegativeLoadFactor = -1.72f;

        /// <summary>
        /// Meen time between engine stalll with negative load.
        /// </summary>
        public float mtbEngineStallNegativeLoad = 10.0f;

        /// <summary>
        /// Meen time between engine stalll with under negative load limitation.
        /// </summary>
        public float mtbEngineStallOverNegativeLoad = 1.0f;

        [Header("Environment")]
        /// <summary>
        /// Air density
        /// </summary>
        public float airDensity = 1.2249f;


        [Header("Hazard")]
        /// <summary>
        /// Enable hazard area.
        /// </summary>
        public bool hazardEnabled = true;

        /// <summary>
        /// Hazard range in min RPM in meters.
        /// </summary>
        public float minHazardRange = 1.5f;

        /// <summary>
        /// Hazard range in max RPM in meters.
        /// </summary>
        public float maxHazardRange = 3.0f;

        /// <summary>
        /// Delay in seconds to kill player.
        /// </summary>
        public float hazardKillDelay = 1.0f;

        /// <summary>
        /// Position to teleport killed player.
        /// </summary>
        public Vector3 killedPlayerPosition = new Vector3(0.0f, -10000.0f, 0.0f);

        /// <summary>
        /// Sound to play when propeller striked.
        /// </summary>
        public AudioSource strikedSound;

        [NonSerialized] public float mixture = 1.0f;
        [UdonSynced(UdonSyncMode.Smooth)][FieldChangeCallback(nameof(RPM))] private float _rpm;
        public float RPM
        {
            private set
            {
                _rpm = value;
                if (animator) animator.SetFloat(rpmFloatParameter, value / animationMaxRPM);
            }
            get => _rpm;
        }

        private SaccEntity saccEntity;
        private SaccAirVehicle airVehicle;
        private DFUNC_ToggleEngine toggleEngine;
        private Rigidbody vehicleRigidbody;
        private Transform vehicleTransform;
        private Animator animator;
        private Vector3 prevVelocity;
        private bool isOwner, engineOn;
        private float seaLevelThrust;
        private float mixtureCutOffTimer;
        private float slip, seaLevelThrustScale, smoothedTargetRPM;
        private float thrust;
        private float oilTemp;
        private bool broken;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void Reset()
        {
            throttleCurve = new AnimationCurve(new[] {
                new Keyframe(0.0f, 0.0f, 1.0f, 1.0f),
                new Keyframe(1.0f, 1.0f, 0.0f, 0.0f),
            });
            // mixtureCurve = new AnimationCurve(new [] {
            //     new Keyframe(0.0f, 0.0f, 1.0f, 1.0f),
            //     new Keyframe(0.6f, 1.0f, 0.0f, 0.0f),
            //     new Keyframe(1.0f, 0.9f, 1.0f, 1.0f),
            // });
            bestMixtureControlCurve = new AnimationCurve(new[] {
                new Keyframe(0.0f, 0.8f, 0.0f, 0.0f),
                new Keyframe(2000.0f, 0.8f, 0.0f, 0.0f),
                new Keyframe(20000.0f, 0.1f),
            });

            GetComponent<CapsuleCollider>().radius = diameter / 2.0f;
            GetComponent<CapsuleCollider>().direction = 2;
        }
#endif

        private void UpdatePropeller(float smoothedTargetRPM, float v)
        {
            RPM = smoothedTargetRPM * (1 - 0.1f * slip);
            slip = 1 - 31.5f * v / Mathf.Max(RPM, minRPM);
            seaLevelThrust = 1 / 120.0f * slip * Mathf.Pow(RPM, 2) * seaLevelThrustScale;
        }

        public void SFEXT_L_EntityStart()
        {
            vehicleRigidbody = GetComponentInParent<Rigidbody>();
            vehicleTransform = vehicleRigidbody.transform;
            animator = vehicleRigidbody.GetComponent<Animator>();
            saccEntity = vehicleRigidbody.GetComponent<SaccEntity>();
            airVehicle = (SaccAirVehicle)saccEntity.GetExtention(GetUdonTypeName<SaccAirVehicle>());
            airVehicle.ThrottleStrength = 0;
            toggleEngine = (DFUNC_ToggleEngine)saccEntity.GetExtention(GetUdonTypeName<DFUNC_ToggleEngine>());

            var maxRPM = maxRPMCurve.Evaluate(0.0f);
            seaLevelThrustScale = 1.0f;
            RPM = maxRPM;
            for (var i = 0; i < 10; i++) UpdatePropeller(maxRPM, 0);
            var t0 = seaLevelThrust;
            var ts = Mathf.Pow(2.0f * airDensity * Mathf.PI * Mathf.Pow(diameter / 2.0f, 2.0f) * Mathf.Pow(power * 735.499f, 2.0f), 1.0f / 3.0f);
            seaLevelThrustScale = ts / t0;

            SFEXT_G_ReAppear();

            isOwner = airVehicle.IsOwner;
        }

        public void SFEXT_O_TakeOwnership() => isOwner = true;
        public void SFEXT_O_LoseOwnership() => isOwner = false;

        public void SFEXT_G_EngineStartup()
        {

            if (batteryBus && toggleEngine && !batteryBus.activeInHierarchy && airVehicle.IsOwner)
            {
                toggleEngine.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(DFUNC_ToggleEngine.EngineStartupCancel));
            }
            else
            {
                SFEXT_G_EngineOn();
            }
        }

        public void SFEXT_G_EngineOn()
        {
            engineOn = true;
            gameObject.SetActive(true);
        }

        public void SFEXT_G_EngineOff()
        {
            engineOn = false;
            mixtureCutOffTimer = 0.0f;
        }

        public void SFEXT_G_RespawnButton() => SFEXT_G_ReAppear();
        public void SFEXT_G_ReAppear()
        {
            engineOn = false;
            broken = false;
            seaLevelThrust = 0;
            smoothedTargetRPM = 0;
            slip = 0;
            RPM = 0;
            oilTemp = 0.0f;
            gameObject.SetActive(false);
        }

        private void FixedUpdate()
        {
            if (isOwner && thrust > 0) vehicleRigidbody.AddForceAtPosition(transform.forward * thrust, transform.position);
        }

        private void Update()
        {
            if (isOwner) OwnerUpdate();

            if (!engineOn && Mathf.Approximately(RPM, 0))
            {
                gameObject.SetActive(false);
                return;
            }

            if (!string.IsNullOrEmpty(oilTempFloatParameter))
            {
                var oilTempTarget = engineOn ? Mathf.Lerp(0.2f, 1.0f, airVehicle.ThrottleInput) : 0.0f;
                if (!Mathf.Approximately(oilTemp, oilTempTarget))
                {
                    oilTemp = Mathf.MoveTowards(oilTemp, oilTempTarget, Time.deltaTime * oilTempResponse);
                    animator.SetFloat(oilTempFloatParameter, oilTemp);
                }
            }
        }

        private void OwnerUpdate()
        {
            if (Mathf.Approximately(mixture, 0))
            {
                if (mixtureCutOffTimer > mixtureCutOffDelay)
                {
                    mixtureCutOffTimer = 0;
                    EngineOff();
                    return;
                }
                mixtureCutOffTimer += Time.deltaTime * UnityEngine.Random.Range(0.9f, 1.1f);
            }

            var deltaTime = Time.deltaTime;

            var altitude = (transform.position.y - airVehicle.SeaLevel) * 3.281f;
            var throttleInput = airVehicle.ThrottleInput;

            var bestMixtureControl = bestMixtureControlCurve.Evaluate(altitude);
            var mixtureError = Mathf.Abs(mixture - bestMixtureControl);

            var maxRPM = maxRPMCurve.Evaluate(altitude);

            var targetRPM = (engineOn && ! broken)
                ? Mathf.Lerp(minRPM, maxRPM, throttleCurve.Evaluate(throttleInput)) / (1.0f + mixtureError * mixtureErrorCoefficient)
                : 0;
            smoothedTargetRPM = Mathf.Lerp(smoothedTargetRPM, targetRPM, Time.deltaTime * rpmResponse);

            UpdatePropeller(smoothedTargetRPM, Vector3.Dot(airVehicle.AirVel, transform.forward));

            thrust = seaLevelThrust * Mathf.SmoothStep(1.0f, 0.0f, altitude / (halfPowerAltitude * 2.0f));

            var engineOutput = Mathf.Clamp01(RPM / maxRPM);
            airVehicle.EngineOutput = engineOutput;

            if (Mathf.Approximately(engineOutput, 0.0f) && UnityEngine.Random.value < (1.0f - engineOutput)) EngineOff();

            if (engineStall)
            {
                var velocity = vehicleRigidbody.velocity;
                var acceleration = (velocity - prevVelocity) / deltaTime;
                prevVelocity = velocity;

                var gravity = Physics.gravity;
                var loadFactor = Vector3.Dot(acceleration - Physics.gravity, vehicleTransform.up) / gravity.magnitude;
                if (
                    loadFactor < minimumNegativeLoadFactor && UnityEngine.Random.value < Mathf.Abs((loadFactor - minimumNegativeLoadFactor) * deltaTime / mtbEngineStallOverNegativeLoad)
                    || loadFactor < 0 && UnityEngine.Random.value < Mathf.Clamp01(-loadFactor) * deltaTime / mtbEngineStallNegativeLoad
                )
                {
                    EngineOff();
                }
            }
        }

        private void EngineOff()
        {
            if (toggleEngine) toggleEngine.ToggleEngine(true);
            else airVehicle.SetEngineOff();
        }

        private void PostLateUpdate()
        {
            if (hazardEnabled && PlayerStrike.CheckPlayerStrike(transform, saccEntity, RPM / maxRPMCurve.Evaluate(0), minHazardRange, maxHazardRange, thrust))
            {
                PlayStrikeSound();
                SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(PlayerStriked));
                SendCustomEventDelayedSeconds(nameof(_KillPlayer), hazardKillDelay);
            }
        }

        public void PlayerStriked()
        {
            if (!broken)
            {
                broken = true;
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayStrikeSound));
            }
        }

        public void PlayStrikeSound()
        {
            if (strikedSound && !strikedSound.isPlaying) strikedSound.Play();
        }

        public void _KillPlayer()
        {
            Networking.LocalPlayer.TeleportTo(killedPlayerPosition, Quaternion.identity);
        }
    }
}

using System;
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common.Interfaces;

namespace EsnyaSFAddons
{

    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class SFEXT_AdvancedPropellerThrust : UdonSharpBehaviour
    {
        [Header("Specs")]
        [Tooltip("hp")] public float power = 160.0f;
        [Tooltip("m")] public float diameter = 1.9304f;
        [Tooltip("rpm")] public float maxRPM = 2700;
        [Tooltip("rpm")] public float minRPM = 600;
        public AnimationCurve mixtureCurve;
        public float rpmResponse = 1.0f;

        [Header("Startup")]
        public float mixtureCutOffDelay = 1.0f;
        public GameObject batteryBus;

        [Header("Animation")]
        public string rpmFloatParameter = "rpm";
        public float animationMaxRPM = 3000;

        [Header("Failure")]
        public bool engineStall = true;
        [Tooltip("G")] public float minimumNegativeLoadFactor = -1.72f;
        public float mtbEngineStallNegativeLoad = 10.0f;
        public float mtbEngineStallOverNegativeLoad = 1.0f;

        [Header("Environment")]
        public float airDensity = 1.2249f;

        [NonSerialized] public float mixture = 1.0f;
        [UdonSynced(UdonSyncMode.Smooth)] private float _rpm;
        public float RPM {
            private set {
                _rpm = value;
                if (animator) animator.SetFloat(rpmFloatParameter, value / animationMaxRPM);
            }
            get => _rpm;
        }

        private SaccAirVehicle airVehicle;
        private DFUNC_ToggleEngine toggleEngine;
        private Rigidbody vehicleRigidbody;
        private Transform vehicleTransform;
        private Animator animator;
        private Vector3 prevVelocity;
        private bool isOwner, engineOn;
        private float thrust;
        private float mixtureCutOffTimer;
        private float slip, thrustScale, targetRPM;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void Reset()
        {
            mixtureCurve = new AnimationCurve(new [] {
                new Keyframe(0.0f, 0.0f, 1.0f, 1.0f),
                new Keyframe(0.6f, 1.0f, 0.0f, 0.0f),
                new Keyframe(1.0f, 0.9f, 1.0f, 1.0f),
            });
        }
#endif

        private void UpdatePropeller(float targetRPM, float v) {
            RPM = targetRPM * (1 - 0.1f * slip);
            slip = 1 - 31.5f * v / Mathf.Max(RPM, minRPM);
            thrust = 1 / 120.0f * slip * Mathf.Pow(RPM, 2) * thrustScale;
        }

        public void SFEXT_L_EntityStart()
        {
            vehicleRigidbody = GetComponentInParent<Rigidbody>();
            vehicleTransform = vehicleRigidbody.transform;
            animator = vehicleRigidbody.GetComponent<Animator>();

            var saccEntity = vehicleRigidbody.GetComponent<SaccEntity>();
            airVehicle = (SaccAirVehicle)saccEntity.GetExtention(GetUdonTypeName<SaccAirVehicle>());
            airVehicle.ThrottleStrength = 0;
            toggleEngine = (DFUNC_ToggleEngine)saccEntity.GetExtention(GetUdonTypeName<DFUNC_ToggleEngine>());

            thrustScale = 1.0f;
            RPM = maxRPM;
            for (var i = 0; i < 10; i++) UpdatePropeller(maxRPM, 0);
            var t0 = thrust;
            var ts = Mathf.Pow(2.0f * airDensity * Mathf.PI * Mathf.Pow(diameter / 2.0f, 2.0f) * Mathf.Pow(power * 735.499f, 2.0f), 1.0f / 3.0f);
            thrustScale = ts / t0;

            SFEXT_G_Reappear();

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
        }

        public void SFEXT_G_Reappear()
        {
            engineOn = false;
            thrust = 0;
            targetRPM = 0;
            slip = 0;
            RPM = 0;
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
        }

        private void OwnerUpdate()
        {
            if (Mathf.Approximately(mixture, 0))
            {
                if (mixtureCutOffTimer > mixtureCutOffDelay)
                {
                    EngineOff();
                    return;
                }
                mixtureCutOffTimer += Time.deltaTime * UnityEngine.Random.Range(0.9f, 1.1f);
            }

            targetRPM = Mathf.Lerp(targetRPM, engineOn ? Mathf.Lerp(minRPM, maxRPM, airVehicle.ThrottleInput) * mixtureCurve.Evaluate(mixture) : 0, Time.deltaTime * rpmResponse);
            UpdatePropeller(targetRPM, Vector3.Dot(airVehicle.AirVel, transform.forward));

            airVehicle.EngineOutput = Mathf.Clamp01(RPM / maxRPM);

            if (engineStall)
            {
                var deltaTime = Time.deltaTime;
                var velocity = vehicleRigidbody.velocity;
                var acceleration = (velocity - prevVelocity) / deltaTime;
                prevVelocity = velocity;

                var gravity = Physics.gravity;
                var loadFactor = Vector3.Dot(acceleration - Physics.gravity, vehicleTransform.up) / gravity.magnitude;
                if (
                    loadFactor < minimumNegativeLoadFactor && UnityEngine.Random.value < Mathf.Abs((loadFactor - minimumNegativeLoadFactor) * deltaTime / mtbEngineStallOverNegativeLoad)
                    || loadFactor < 0 && UnityEngine.Random.value <  Mathf.Clamp01(-loadFactor) * deltaTime / mtbEngineStallNegativeLoad
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
    }
}

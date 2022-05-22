using UdonSharp;
using UnityEngine;
using VRC.Udon.Common.Interfaces;

namespace EsnyaSFAddons
{

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_AdvancedPropellerThrust : UdonSharpBehaviour
    {
        [Header("Specs")]
        [Tooltip("hp")] public float power = 160.0f;
        [Tooltip("m")] public float diameter = 1.9304f;
        [Tooltip("rpm")] public float maxRPM = 2450;
        [Tooltip("rpm")] public float minRPM = 700;
        public AnimationCurve propellerEfficiency;
        public float minAirspeed = 20.0f;

        [Header("Startup")]
        public float mixtureCutOffDelay = 1.0f;
        public GameObject batteryBus;

        [Header("Failure")]
        public bool engineStall = true;
        [Tooltip("G")] public float minimumNegativeLoadFactor = -1.72f;
        public float mtbEngineStallNegativeLoad = 10.0f;
        public float mtbEngineStallOverNegativeLoad = 1.0f;

        private SaccAirVehicle airVehicle;
        private DFUNC_ToggleEngine toggleEngine;
        private Rigidbody vehicleRigidbody;
        private Transform vehicleTransform;
        private Vector3 prevVelocity;
        private bool engineOn;
        private float thrust;
        private bool mixture;
        private float mixtureCutOffTimer;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void Reset()
        {
            propellerEfficiency = new AnimationCurve(new [] {
                new Keyframe(0.0f, 0.0f, 1.0f, 1.0f),
                new Keyframe(0.8f, 0.8f, 0.0f, 0.0f),
                new Keyframe(0.9f, 0.0f, 1.0f, 1.0f),
            });
        }
#endif

        public void SFEXT_L_EntityStart()
        {
            vehicleRigidbody = GetComponentInParent<Rigidbody>();
            vehicleTransform = vehicleRigidbody.transform;
            var saccEntity = vehicleRigidbody.GetComponent<SaccEntity>();
            airVehicle = (SaccAirVehicle)saccEntity.GetExtention(GetUdonTypeName<SaccAirVehicle>());
            airVehicle.ThrottleStrength = 0;
            toggleEngine = (DFUNC_ToggleEngine)saccEntity.GetExtention(GetUdonTypeName<DFUNC_ToggleEngine>());
            gameObject.SetActive(saccEntity.IsOwner);
        }

        public void SFEXT_O_TakeOwnership() => gameObject.SetActive(true);
        public void SFEXT_O_LoseOwnership() => gameObject.SetActive(false);

        public void SFEXT_G_EngineStartup()
        {
            if (batteryBus && toggleEngine && !batteryBus.activeInHierarchy && airVehicle.IsOwner)
            {
                toggleEngine.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(DFUNC_ToggleEngine.EngineStartupCancel));
            }
        }

        public void SFEXT_G_EngineOn()
        {
            engineOn = true;
            thrust = 0.0f;
        }

        public void SFEXT_G_EngineOff()
        {
            engineOn = false;
            thrust = 0.0f;
        }

        public void SFEXT_G_MixtureOn()
        {
            mixtureCutOffTimer = 0;
            mixture = true;
        }

        public void SFEXT_G_MixtureCutOff()
        {
            mixture = false;
        }

        private void FixedUpdate()
        {
            if (thrust > 0) vehicleRigidbody.AddForceAtPosition(transform.forward * thrust, transform.position);
        }

        private void Update()
        {
            if (!engineOn) return;

            if (!mixture)
            {
                if (mixtureCutOffTimer > mixtureCutOffDelay)
                {
                    EngineOff();
                    return;
                }
                mixtureCutOffTimer += Time.deltaTime * Random.Range(0.9f, 1.1f);
            }

            var rpm = Mathf.Lerp(minRPM, maxRPM, airVehicle.EngineOutput);
            var v = Mathf.Max(Vector3.Dot(transform.forward, airVehicle.AirVel), minAirspeed);
            var j = v / (rpm / 60.0f * diameter);
            var e = propellerEfficiency.Evaluate(j);
            thrust = 75 * 9.807f * e * power / v;

            if (engineStall)
            {
                var deltaTime = Time.deltaTime;
                var velocity = vehicleRigidbody.velocity;
                var acceleration = (velocity - prevVelocity) / deltaTime;
                prevVelocity = velocity;

                var gravity = Physics.gravity;
                var loadFactor = Vector3.Dot(acceleration - Physics.gravity, vehicleTransform.up) / gravity.magnitude;
                if (
                    loadFactor < minimumNegativeLoadFactor && Random.value < Mathf.Abs((loadFactor - minimumNegativeLoadFactor) * deltaTime / mtbEngineStallOverNegativeLoad)
                    || loadFactor < 0 && Random.value <  Mathf.Clamp01(-loadFactor) * deltaTime / mtbEngineStallNegativeLoad
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

        private float Curve(float x, float a, float b)
        {
            return Mathf.Sin(Mathf.Max(Mathf.Min(x / a, (1 - x / b) / (1 - a / b)), 0.0f) * Mathf.PI * 0.5f);
        }
    }
}

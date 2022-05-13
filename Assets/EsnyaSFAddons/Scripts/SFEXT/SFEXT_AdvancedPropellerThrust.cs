using UdonSharp;
using UnityEngine;
using VRC.Udon.Common.Interfaces;

namespace EsnyaSFAddons
{

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_AdvancedPropellerThrust : UdonSharpBehaviour
    {
        [Tooltip("hp")] public float power = 160.0f;
        [Tooltip("m")] public float diameter = 1.9304f;
        [Tooltip("rpm")] public float maxRPM = 2700;
        public float maxAdvanceRatio = 0.9f;
        public float maxPropellerEfficiencyAdvanceRatio = 0.8f;
        public float maxPropellerEfficiency = 0.82f;
        public float minAirspeed = 10f;
        public bool engineStall = true;
        public float mixtureCutOffDelay = 1.0f;
        public GameObject batteryBus;
        [Tooltip("G")] public float negativeLoadFactorLimit = -1.8f;
        public float mtbEngineStallNegativeLoadFactor = 30.0f;
        public float mtbEngineStallExceedNegativeLoadFactorLimit = 5.0f;
        private SaccAirVehicle airVehicle;
        private DFUNC_ToggleEngine toggleEngine;
        private Rigidbody vehicleRigidbody;
        private Transform vehicleTransform;
        private Vector3 prevVelocity;
        private bool engineOn;
        private bool mixture;
        private float mixtureCutOffTimer;

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
            if (batteryBus && toggleEngine && !batteryBus.activeSelf && airVehicle.IsOwner)
            {
                toggleEngine.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(DFUNC_ToggleEngine.EngineStartupCancel));
            }
        }

        public void SFEXT_G_EngineOn() => engineOn = true;
        public void SFEXT_G_EngineOff() => engineOn = false;

        public void SFEXT_G_MixtureOn()
        {
            mixtureCutOffTimer = 0;
            mixture = true;
        }
        public void SFEXT_G_MixtureCutOff() => mixture = false;

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

            var v = Mathf.Max(Vector3.Dot(vehicleRigidbody.transform.forward, airVehicle.AirVel), minAirspeed);
            var j = v / (maxRPM / 60.0f * diameter);
            var e = Curve(j, maxPropellerEfficiencyAdvanceRatio, maxAdvanceRatio) * maxPropellerEfficiency;
            var t = 75 * e * power * 9.807f / v;
            airVehicle.ThrottleStrength = t;

            if (engineStall)
            {
                var deltaTime = Time.deltaTime;
                var velocity = vehicleRigidbody.velocity;
                var acceleration = (velocity - prevVelocity) / deltaTime;
                prevVelocity = velocity;

                var gravity = Physics.gravity;
                var loadFactor = Vector3.Dot(acceleration - Physics.gravity, vehicleTransform.up) / gravity.magnitude;
                if (
                    loadFactor < negativeLoadFactorLimit && Random.value < Mathf.Abs((loadFactor - negativeLoadFactorLimit) * deltaTime / negativeLoadFactorLimit)
                    || loadFactor < 0 && Random.value <  Mathf.Clamp01(-loadFactor) * deltaTime / mtbEngineStallNegativeLoadFactor
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

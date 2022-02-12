using UdonSharp;
using UnityEngine;

namespace EsnyaAircraftAssets
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_WakeTurbulence : UdonSharpBehaviour
    {
        public float minSpeed = 60;
        public float peakSpeed = 120;
        public float maxSpeed = 300;
        public float curve = 2.0f;

        private Rigidbody vehicleRigidbody;
        private SaccAirVehicle airVehicle;
        private ParticleSystem[] particles;
        private float[] emissionRates;
        private void Start()
        {
            var entity = GetComponentInParent<SaccEntity>();
            vehicleRigidbody = entity.GetComponent<Rigidbody>();
            airVehicle = entity.GetComponentInChildren<SaccAirVehicle>();

            particles = GetComponentsInChildren<ParticleSystem>(true);
            emissionRates = new float[particles.Length];
            for (var i = 0; i < particles.Length; i++)
            {
                emissionRates[i] = particles[i].emission.rateOverTimeMultiplier;
            }

            gameObject.SetActive(false);
        }

        private bool hasPilot = false;
        public void SFEXT_G_PilotEnter()
        {
            hasPilot = true;
            prevPosition = vehicleRigidbody.position;
            gameObject.SetActive(true);
        }
        public void SFEXT_G_PilotExit() => hasPilot = false;

        private Vector3 prevPosition;
        private void Update()
        {
            var position = vehicleRigidbody.position;
            var velocity = (position - prevPosition) / Time.deltaTime;
            prevPosition = position;

            var airspeed = Vector3.Distance(velocity, airVehicle.Wind) * 1.94384f;
            var strength = Mathf.Pow(Lerp3(0, 1, 0, airspeed, minSpeed, peakSpeed, maxSpeed), curve);

            var enabled = !Mathf.Approximately(strength, 0);
            for (var i = 0; i < particles.Length; i++)
            {
                var emission = particles[i].emission;

                if (enabled)
                {
                    emission.rateOverTimeMultiplier = emissionRates[i] * strength;
                }

                if (enabled != emission.enabled) emission.enabled = enabled;
            }

            if (!hasPilot && !enabled) gameObject.SetActive(false);
        }

        private float Remap01(float value, float oldMin, float oldMax)
        {
            return (value - oldMin) / (oldMax - oldMin);
        }
        private float Lerp3(float a, float b, float c, float t, float tMin, float tMid, float tMax)
        {
            return Mathf.Lerp(a, Mathf.Lerp(b, c, Remap01(t, tMid, tMax)), Remap01(t, tMin, tMid));
        }
    }
}

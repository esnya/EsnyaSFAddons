using EsnyaSFAddons.DFUNC;
using UdonSharp;
using UnityEngine;

namespace EsnyaSFAddons.Avionics
{

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AuralWarnings : UdonSharpBehaviour
    {
        [Tooltip("KIAS")] public float defaultVmo = 340;
        [Tooltip("Degree")] public float stickShakerStartAoA = 10;
        [Tooltip("Degree")] public float stickShakerMaxAoA = 24;
        public AudioSource overspeed;
        public AudioSource stickShaker;
        public int updateInterval = 30;
        public float velocitySmooth = 1;

        private Transform origin;
        private SaccAirVehicle airVehicle;
        private DFUNC_AdvancedFlaps advancedFlaps;
        private int updateOffset;
        private void OnEnable()
        {
            updateOffset = Random.Range(0, updateInterval);
            prevTime = Time.time;
            prevPosition = transform.position;
        }
        private void Start()
        {
            var rigidbody = GetComponentInParent<Rigidbody>();
            if (rigidbody) origin = rigidbody.transform;
            if (!origin) origin = transform;

            airVehicle = origin.GetComponentInChildren<SaccAirVehicle>();
            advancedFlaps = origin.GetComponentInChildren<DFUNC_AdvancedFlaps>(true);
        }

        private float prevTime;
        private Vector3 prevPosition, velocity;
        private void Update()
        {
            if ((Time.frameCount + updateOffset) % updateInterval != 0) return;

            var time = Time.time;
            var deltaTime = time - prevTime;
            prevTime = time;

            var position = origin.position;
            velocity = Vector3.Lerp(velocity, (position - prevPosition) / deltaTime, deltaTime / velocitySmooth);
            prevPosition = position;

            var wind = airVehicle ? airVehicle.Wind : Vector3.zero;
            var airVel = velocity - wind;

            var ias = Mathf.Max(Vector3.Dot(origin.forward, airVel), 0) * 1.94384f;

            var vmo = defaultVmo;
            if (advancedFlaps) vmo = Mathf.Min(advancedFlaps.targetSpeedLimit, advancedFlaps.speedLimit);
            var playOverspeed = ias > 1.0f && ias > vmo;
            SetVolume(overspeed, playOverspeed ? 1.0f : 0.0f);

            var aoa = ias > 10f ? Mathf.Atan2(Vector3.Dot(origin.up, airVel), Vector3.Dot(origin.forward, airVel)) * Mathf.Rad2Deg : 0.0f;
            var stickShakerVolume = Mathf.Pow(ClampedRemap01(-aoa, stickShakerStartAoA, stickShakerMaxAoA), 0.1f);
            SetVolume(stickShaker, stickShakerVolume);
        }

        private void SetVolume(AudioSource audioSource, float volume)
        {

            if (!audioSource) return;
            var play = !Mathf.Approximately(volume, 0);
            if (play)
            {
                audioSource.volume = volume;
            }

            if (audioSource.isPlaying != play)
            {
                if (play) audioSource.Play();
                else audioSource.Stop();
            }
        }

        private float Remap01(float value, float oldMin, float oldMax)
        {
            return (value - oldMin) / (oldMax - oldMin);
        }
        private float ClampedRemap01(float value, float oldMin, float oldMax)
        {
            return Mathf.Clamp01(Remap01(value, oldMin, oldMax));
        }

        private float ClampedRemap(float value, float oldMin, float oldMax, float newMin, float newMax)
        {
            return ClampedRemap01(value, oldMin, oldMax) * (newMax - newMin) + newMin;
        }
    }
}
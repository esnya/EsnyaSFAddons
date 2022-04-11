using System.Runtime.CompilerServices;
using UdonSharp;
using UnityEngine;

namespace EsnyaAircraftAssets
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [RequireComponent(typeof(Animator))]
    public class Windsock : UdonSharpBehaviour
    {
        public SAV_WindChanger windChanger;
        public int updateInterval = 10;
        public float maxWindMagnitude = 100.0f;
        public float rotationSmooth = 1f;
        public float rotationOffset = 180.0f;

        public bool cloth = true;
        public bool animation = false;

        private int updateOffset;
        private Animator animator;
        private float heading, targetHeading;
        private Quaternion initialRotation;
        private float finalWindTarget, finalWindSmoothed;

        private void Start()
        {
            updateOffset = Random.Range(0, updateInterval);
            animator = GetComponent<Animator>();
            initialRotation = transform.rotation;
            heading = 0;
        }

        private void Update()
        {
            var prevHeading = heading;
            heading = Mathf.LerpAngle(heading, targetHeading, Time.deltaTime / rotationSmooth);
            if (!Mathf.Approximately(heading, prevHeading)) transform.rotation = Quaternion.AngleAxis(heading + rotationOffset, Vector3.up) * initialRotation;

            if ((Time.frameCount + updateOffset) % updateInterval == 0 && windChanger)
            {
                var wind = ((SaccAirVehicle)windChanger.SaccAirVehicles[0]).Wind;
                var windGustiness = ((SaccAirVehicle)windChanger.SaccAirVehicles[0]).WindGustiness;
                var windTurbulanceScale = ((SaccAirVehicle)windChanger.SaccAirVehicles[0]).WindTurbulanceScale;
                var windGustStrength = ((SaccAirVehicle)windChanger.SaccAirVehicles[0]).WindGustStrength;

                var timeGustiness = Time.time * windGustiness;
                var gustx = timeGustiness + (transform.position.x * windTurbulanceScale);
                var gustz = timeGustiness + (transform.position.z * windTurbulanceScale);
                var finalWind = wind + Vector3.Normalize(new Vector3(Mathf.PerlinNoise(gustx + 9000, gustz) - .5f, 0, Mathf.PerlinNoise(gustx, gustz + 9999) - .5f)) * windGustStrength;

                targetHeading = Vector3.SignedAngle(Vector3.back, Vector3.ProjectOnPlane(finalWind, Vector3.up), Vector3.up);

                if (cloth)
                {
                    animator.SetFloat("x", Mathf.Clamp(finalWind.x / maxWindMagnitude, -1.0f, 1.0f));
                    animator.SetFloat("z", Mathf.Clamp(finalWind.z / maxWindMagnitude, -1.0f, 1.0f));
                }

                finalWindTarget = finalWind.magnitude / maxWindMagnitude;
            }

            if (animation && !Mathf.Approximately(finalWindSmoothed, finalWindTarget))
            {
                finalWindSmoothed = Mathf.LerpAngle(finalWindTarget, finalWindSmoothed, Time.deltaTime / rotationSmooth);
                animator.SetFloat("finalwind", finalWindSmoothed);
            }
        }
    }
}

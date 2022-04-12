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
        public Vector3 windsockAxis = Vector3.up;
        public int updateInterval = 10;
        public float maxWindMagnitude = 100.0f;
        public float rotationResponse =1f;
        public float rotationOffset = 0.0f;

        public bool cloth = true;
        public bool negateClothWind = false;

        private int updateOffset;
        private Animator animator;
        private Vector3 wind;
        private float windGustiness, windTurbulanceScale, windGustStrength;

        private float _windHeading;
        private float WindHeading
        {
            set
            {
                if (!Mathf.Approximately(WindHeading, value))
                {
                    transform.rotation = Quaternion.AngleAxis(value + rotationOffset, windsockAxis);
                }
                _windHeading = value;
            }
            get => _windHeading;
        }


        private Vector3 _finalWind;
        private Vector3 FinalWind
        {
            get => _finalWind;
            set
            {
                if (Vector3.Distance(value, _finalWind) > 0)
                {
                    if (cloth)
                    {
                        var sign = negateClothWind ? -1.0f : 1.0f;
                        animator.SetFloat("x", Mathf.Clamp(value.x / maxWindMagnitude, -1.0f, 1.0f) * sign);
                        animator.SetFloat("z", Mathf.Clamp(value.z / maxWindMagnitude, -1.0f, 1.0f) * sign);
                    }
                    else
                    {
                        animator.SetFloat("finalwind", value.magnitude / maxWindMagnitude);
                    }
                }
                _finalWind = value;
            }
        }

        private void Start()
        {
            updateOffset = Random.Range(0, updateInterval);
            animator = GetComponent<Animator>();
            WindHeading = 0;
            FinalWind = Vector3.zero;
        }

        private void OnAnimatorMove()
        {
            if (!cloth) _Update();
        }

        private void OnRenderObject() => _Update();

        private void _Update()
        {
            if (!windChanger) return;

            if ((Time.frameCount + updateOffset) % updateInterval == 0)
            {
                wind = ((SaccAirVehicle)windChanger.SaccAirVehicles[0]).Wind;
                windGustiness = ((SaccAirVehicle)windChanger.SaccAirVehicles[0]).WindGustiness;
                windTurbulanceScale = ((SaccAirVehicle)windChanger.SaccAirVehicles[0]).WindTurbulanceScale;
                windGustStrength = ((SaccAirVehicle)windChanger.SaccAirVehicles[0]).WindGustStrength;
            }

            var timeGustiness = Time.time * windGustiness;
            var gustx = timeGustiness + (transform.position.x * windTurbulanceScale);
            var gustz = timeGustiness + (transform.position.z * windTurbulanceScale);

            FinalWind = wind + Vector3.Normalize(new Vector3(Mathf.PerlinNoise(gustx + 9000, gustz) - .5f, 0, Mathf.PerlinNoise(gustx, gustz + 9999) - .5f)) * windGustStrength;

            var targetWindHeading = Vector3.SignedAngle(Vector3.back, FinalWind, Vector3.up);
            WindHeading = Mathf.MoveTowardsAngle(WindHeading, targetWindHeading, Time.deltaTime * rotationResponse);
        }
    }
}

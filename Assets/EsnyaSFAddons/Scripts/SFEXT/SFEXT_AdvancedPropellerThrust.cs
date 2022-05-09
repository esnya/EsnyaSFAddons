using UdonSharp;
using UnityEngine;
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
        private SaccAirVehicle airVehicle;
        private Rigidbody vehicleRigidbody;

        public void SFEXT_L_EntityStart()
        {
            vehicleRigidbody = GetComponentInParent<Rigidbody>();
            var saccEntity = vehicleRigidbody.GetComponent<SaccEntity>();
            airVehicle = (SaccAirVehicle)saccEntity.GetExtention(GetUdonTypeName<SaccAirVehicle>());

            gameObject.SetActive(saccEntity.IsOwner);
        }

        public void SFEXT_O_TakeOwnership() => gameObject.SetActive(true);
        public void SFEXT_O_LoseOwnership() => gameObject.SetActive(false);

        private void Update()
        {
            var v = Mathf.Max(Vector3.Dot(vehicleRigidbody.transform.forward, airVehicle.AirVel), minAirspeed);
            var j = v / (maxRPM / 60.0f * diameter);
            var e = Curve(j, maxPropellerEfficiencyAdvanceRatio, maxAdvanceRatio) * maxPropellerEfficiency;
            var t = 75 * e * power * 9.807f / v;
            airVehicle.ThrottleStrength = t;
        }

        private float Curve(float x, float a, float b)
        {
            return Mathf.Sin(Mathf.Max(Mathf.Min(x / a, (1 - x / b) / (1 - a / b)), 0.0f) * Mathf.PI * 0.5f);
        }
    }
}

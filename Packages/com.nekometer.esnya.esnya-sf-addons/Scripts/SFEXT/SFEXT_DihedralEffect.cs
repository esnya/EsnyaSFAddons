using SaccFlightAndVehicles;
using UdonSharp;
using UnityEngine;

namespace EsnyaSFAddons.SFEXT
{

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_DihedralEffect : UdonSharpBehaviour
    {
        public float coefficient = 0.01f;
        [Tooltip("Pa")] public float dynamicPressure = 10;
        [Tooltip("m^2")] public float referenceArea = 1;
        [Tooltip("m")] public float calacteristicLength = 1;
        public float maxSlipAngle = 20;
        public float aoaCurve = 1;
        public float extraDrag = 0;
        private Rigidbody vehicleRigidbody;
        private Transform vehicleTransform;
        private float maxSpeed;
        private bool hasDrag;
        private SaccAirVehicle airVehicle;
        private float dragMultiplier;
        public void SFEXT_L_EntityStart()
        {
            var saccEntity = GetComponentInParent<SaccEntity>();
            airVehicle = (SaccAirVehicle)saccEntity.GetExtention(GetUdonTypeName<SaccAirVehicle>());
            vehicleRigidbody = airVehicle.VehicleRigidbody;
            vehicleTransform = airVehicle.VehicleTransform;

            maxSpeed = airVehicle.RotMultiMaxSpeed;
            hasDrag = !Mathf.Approximately(extraDrag, 0.0f);
            dragMultiplier = hasDrag ? airVehicle.AirFriction * (dragMultiplier - 1) : 0.0f;

            gameObject.SetActive(false);
        }

        private float torqueMultiplier;

        public void SFEXT_O_PilotEnter()
        {
            torqueMultiplier = 0.5f * coefficient * dynamicPressure * referenceArea * calacteristicLength;
            gameObject.SetActive(true);
        }

        public void SFEXT_O_PilotExit()
        {
            gameObject.SetActive(false);
        }

        private void FixedUpdate()
        {
            if (!airVehicle) return;

            var vehicleRight = vehicleTransform.right;
            var vehicleUp = vehicleTransform.up;
            var vehicleForward = vehicleTransform.forward;

            var airVel = airVehicle.AirVel;
            var airSpeed = airVel.magnitude;
            var rotLift = Mathf.Min(airSpeed / maxSpeed, 1);
            var slipAngle = Vector3.SignedAngle(Vector3.Dot(airVel, vehicleForward) >= 0 ? vehicleForward : -vehicleForward, Vector3.ProjectOnPlane(airVel, vehicleUp), vehicleUp);
            var normalizedSlipAngle = Mathf.Clamp(slipAngle / maxSlipAngle, -1, 1);
            var curvedSlipAngle = Mathf.Pow(Mathf.Abs(normalizedSlipAngle), aoaCurve) * Mathf.Sign(normalizedSlipAngle);
            var roll = torqueMultiplier * Mathf.Pow(airSpeed, 2.0f) * curvedSlipAngle * rotLift;
            vehicleRigidbody.AddRelativeTorque(0, 0, roll);
            if (hasDrag)
            {
                var slipSpeed = Vector3.Dot(airVel, vehicleRight);
                var absSlipSpeed = Mathf.Abs(slipSpeed);
                vehicleRigidbody.AddRelativeForce(-Vector3.right * Mathf.Pow(absSlipSpeed, 2.0f) * Mathf.Sign(slipSpeed) * dragMultiplier * airVehicle.Atmosphere, ForceMode.Acceleration);
            }
        }
    }
}

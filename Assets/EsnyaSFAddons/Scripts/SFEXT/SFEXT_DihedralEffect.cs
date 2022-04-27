namespace EsnyaSFAddons
{
    using UdonSharp;
    using UnityEngine;

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_DihedralEffect : UdonSharpBehaviour
    {
        public float coefficient = 0.1f;
        [Tooltip("Pa")] public float dynamicPressure = 10;
        [Tooltip("m^2")] public float referenceArea = 1;
        [Tooltip("m")] public float calacteristicLength = 1;
        public float maxRollAoA = 60;
        private Rigidbody vehicleRigidbody;
        private Transform vehicleTransform;
        private float maxSpeed;
        private SaccAirVehicle airVehicle;
        public void SFEXT_L_EntityStart()
        {
            var saccEntity = GetComponentInParent<SaccEntity>();
            airVehicle = (SaccAirVehicle)saccEntity.GetExtention(GetUdonTypeName<SaccAirVehicle>());
            vehicleRigidbody = airVehicle.VehicleRigidbody;
            vehicleTransform = airVehicle.VehicleTransform;

            maxSpeed = airVehicle.RotMultiMaxSpeed;

            gameObject.SetActive(false);
        }

        private float torqueMultiplier;
        public void SFEXT_O_PilotEnter()
        {
            torqueMultiplier = -0.5f * coefficient * dynamicPressure * referenceArea * calacteristicLength;
            gameObject.SetActive(true);
        }

        public void SFEXT_O_PilotExit()
        {
            gameObject.SetActive(false);
        }

        private void FixedUpdate()
        {
            if (!airVehicle) return;

            var airVel = airVehicle.AirVel;
            var airSpeed = airVel.magnitude;
            var rotLift = Mathf.Min(airSpeed / maxSpeed, 1);
            var slipSpeed = Vector3.Dot(airVel, vehicleTransform.right);

            var absSlipSpeed = Mathf.Abs(slipSpeed);
            var rollAoA = Vector3.SignedAngle(Vector3.up, Vector3.ProjectOnPlane(airVel, Vector3.forward), Vector3.forward);
            var roll = torqueMultiplier * Mathf.Pow(absSlipSpeed, 2.0f) * Mathf.Clamp(rollAoA / maxRollAoA, -1, 1) * rotLift;
            vehicleRigidbody.AddRelativeTorque(0, 0, roll);
        }
    }
}

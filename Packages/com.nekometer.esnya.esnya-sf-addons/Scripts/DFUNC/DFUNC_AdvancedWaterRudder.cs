
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UdonSharpEditor;
#endif

namespace EsnyaSFAddons.DFUNC
{
    /// <summary>
    /// Adavnced Rudder for seaplane.
    ///
    /// Place in center of rudders.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_AdvancedWaterRudder : UdonSharpBehaviour
    {
        public GameObject Dial_Funcon;
        public bool defaultExtracted = false;
        public AnimationCurve liftCoefficientCurve = AnimationCurve.Linear(0, 0, 30, 0.1f);
        public AnimationCurve dragCoefficientCurve = AnimationCurve.Linear(0, 0, 30, 0.01f);
        public float referenceArea = 1.0f;
        public float waterDensity = 999.1026f;
        public float maxRudderAngle = 30.0f;
        public float response = 0.5f;

        private Animator vehicleAnimator;
        private Rigidbody vehicleRigidbody;
        private SaccEntity entity;
        private SaccAirVehicle airVehicle;
        private float rudderAngle;
        private Vector3 localForce;
        private float forceMultiplier;

        [UdonSynced][FieldChangeCallback(nameof(Extracted))] private bool _extracted;
        public bool Extracted
        {
            set {
                if (Dial_Funcon) {
                    Dial_Funcon.SetActive(value);
                }

                if (vehicleAnimator) {
                    vehicleAnimator.SetBool("waterrudder", value);
                }

                _extracted = value;
            }
            get => _extracted;
        }

        private void Start()
        {
            gameObject.SetActive(false);
        }

        public void SFEXT_L_EntityStart()
        {
            vehicleRigidbody = GetComponentInParent<Rigidbody>();
            entity = vehicleRigidbody.GetComponent<SaccEntity>();
            airVehicle = (SaccAirVehicle)entity.GetExtention(GetUdonTypeName<SaccAirVehicle>());
            vehicleAnimator = airVehicle.VehicleAnimator;

            UpdateActive();
            SFEXT_G_Reappear();
        }

        public void SFEXT_O_PilotEnter() => UpdateActive();
        public void SFEXT_O_PilotExit() => UpdateActive();
        public void SFEXT_G_TakeOff() => UpdateActive();
        public void SFEXT_G_TouchDownWater() => UpdateActive();

        public void SFEXT_G_RespawnButton() => SFEXT_G_Reappear();
        public void SFEXT_G_Reappear()
        {
            Extracted = defaultExtracted;
            if (Networking.IsOwner(gameObject)) RequestSerialization();
        }

        private void FixedUpdate()
        {
            if (!(Extracted && vehicleRigidbody)) return;
            vehicleRigidbody.AddForceAtPosition(transform.TransformVector(localForce), transform.position);
        }

        private void Update()
        {
            if (!(Extracted && vehicleRigidbody)) return;

            var velocity = vehicleRigidbody.velocity;
            var speed = velocity.magnitude;

            var rudderTargetAngle = airVehicle.RotationInputs.z * maxRudderAngle;
            rudderAngle = Mathf.Lerp(rudderAngle, rudderTargetAngle, Time.deltaTime * response);

            var rudderAoA = GetRudderAoA(rudderAngle, velocity);
            localForce = (Vector3.right * liftCoefficientCurve.Evaluate(rudderAoA) - Vector3.back * dragCoefficientCurve.Evaluate(rudderAoA)) * Mathf.Pow(speed, 2) * forceMultiplier;
        }

        private void UpdateActive()
        {
            var isActive = entity && airVehicle && entity.Piloting && airVehicle.Floating;

            if (isActive)
            {
                forceMultiplier = 0.5f * waterDensity * referenceArea;
            }
            else
            {
                rudderAngle = 0.0f;
                localForce = Vector3.zero;
            }

            gameObject.SetActive(isActive);
        }

        private float GetRudderAoA(float angle, Vector3 velocity)
        {
            var rotatedVelocity = Quaternion.AngleAxis(angle, transform.up) * velocity;
            return Mathf.Approximately(rotatedVelocity.sqrMagnitude, 0.0f) ? 0.0f : -Mathf.Atan(Vector3.Dot(rotatedVelocity, transform.right) / Vector3.Dot(rotatedVelocity, transform.forward)) * Mathf.Rad2Deg;
        }

        public void KeyboardInput() => Toggle();
        public void DFUNC_TriggerPress() => Toggle();

        /// <summary>
        /// Extract water rudder
        /// </summary>
        public void Extract()
        {
            Extracted = true;
            RequestSerialization();
        }

        /// <summary>
        /// Retract water rudder
        /// </summary>
        public void Retract()
        {
            Extracted = false;
            RequestSerialization();
        }

        /// <summary>
        /// Toggle water rudder
        /// </summary>
        public void Toggle()
        {
            Extracted = !Extracted;
            RequestSerialization();
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!(Extracted && vehicleRigidbody)) return;

            var position = transform.position;
            var forceScale = 1.0f / vehicleRigidbody.mass;

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(position, transform.right * localForce.x * forceScale);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(position, transform.forward * localForce.z * forceScale);

            Gizmos.color = Color.green;
            Gizmos.DrawRay(position, transform.TransformVector(localForce) * forceScale);

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(position, 1);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(position, Mathf.Abs(GetRudderAoA(rudderAngle, vehicleRigidbody.velocity)) / maxRudderAngle);
        }
#endif
    }
}

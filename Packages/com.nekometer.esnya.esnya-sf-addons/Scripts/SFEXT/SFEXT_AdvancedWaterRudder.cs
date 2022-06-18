﻿
using UdonSharp;
using UdonSharpEditor;
using UnityEngine;

namespace EsnyaSFAddons.SFEXT
{
    /// <summary>
    /// Adavnced Rudder for seaplane.
    ///
    /// Place in center of rudders.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_AdvancedWaterRudder : UdonSharpBehaviour
    {
        public AnimationCurve liftCoefficientCurve = AnimationCurve.Linear(0, 0, 30, 0.1f);
        public AnimationCurve dragCoefficientCurve = AnimationCurve.Linear(0, 0, 30, 0.01f);
        public float referenceArea = 1.0f;
        public float waterDensity = 999.1026f;
        public float maxRudderAngle = 30.0f;
        public float response = 0.5f;

        private Rigidbody vehicleRigidbody;
        private SaccEntity entity;
        private SaccAirVehicle airVehicle;
        private float rudderAngle;
        private Vector3 localForce;
        private float forceMultiplier;

        private void Start()
        {
            gameObject.SetActive(false);
        }

        public void SFEXT_L_EntityStart()
        {
            vehicleRigidbody = GetComponentInParent<Rigidbody>();
            entity = vehicleRigidbody.GetComponent<SaccEntity>();
            airVehicle = (SaccAirVehicle)entity.GetExtention(GetUdonTypeName<SaccAirVehicle>());

            UpdateActive();
        }

        public void SFEXT_O_PilotEnter() => UpdateActive();
        public void SFEXT_O_PilotExit() => UpdateActive();
        public void SFEXT_G_TakeOff() => UpdateActive();
        public void SFEXT_G_TouchDownWater() => UpdateActive();

        private void FixedUpdate()
        {
            if (!vehicleRigidbody) return;
            vehicleRigidbody.AddForceAtPosition(transform.TransformVector(localForce), transform.position);
        }

        private void Update()
        {
            if (!vehicleRigidbody) return;

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

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            this.UpdateProxy();

            if (!vehicleRigidbody) return;

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
            Gizmos.DrawWireSphere(position, GetRudderAoA(rudderAngle, vehicleRigidbody.velocity) / 90);
        }
#endif
    }
}

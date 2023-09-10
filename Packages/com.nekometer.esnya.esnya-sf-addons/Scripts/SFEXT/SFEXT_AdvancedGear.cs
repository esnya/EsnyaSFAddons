using SaccFlightAndVehicles;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace EsnyaSFAddons.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class SFEXT_AdvancedGear : UdonSharpBehaviour
    {
        public WheelCollider wheelCollider;
        public Transform suspensionTransform;
        public Transform steerTransform;
        public Transform wheelTransform;
        public Vector3 wheelUp = Vector3.up;
        public Vector3 wheelRight = Vector3.right;

        [Header("Steering")]
        [Tooltip("deg")] public float maxSteerAngle = 0;
        public float steerResponse = 1.0f;

        [Header("Brake")]
        [Tooltip("N/m")] public float brakeTorque = 10000;
        public float brakeResponse = 1.0f;
        public bool autoLimitGroundSpeed;
        public bool autoLimitGroundSpeedOnDesktop = true;
        public float rudderBrake = 0.0f;

        [Header("Indicators")]
        public GameObject transitionIndicator;
        public GameObject downIndicator;


        [Header("Animations")]
        public string gearPositionParameterName = "gearpos";

        [Header("Sounds")]
        public AudioSource transitionSound;
        public AudioClip burstSound, breakSound;

        [Header("Failures")]
        [Tooltip("KIAS")] public float maxExtensionSpeed = 270;
        [Tooltip("KIAS")] public float maxRetractionSpeed = 235;
        [Tooltip("KIAS")] public float maxExtendedSpeed = 320;
        [Tooltip("KGS")] public float brakeMaxGroundSpeed = 60;

        public float mtbTransitionFail = 2 * 60 * 60;
        public float mtbTransitionFailOnOverspeed = 10;
        public float mtbTransitionBraek = 8 * 60 * 60;
        public float mtbTransitionBraekOnOverspeed = 60;
        public float mtbBurstOnOverGroundSpeed = 10;

        [Tooltip("feet/min")] public float verticalSpeedLimit = 600;
        [Tooltip("feet/min")] public float burstVerticalSpeed = 900;

        [Header("Effects")]
        public GameObject burstEffect;

        [Header("Misc")]
        public float timeNosieScale = 0.1f;

        [System.NonSerialized] public float targetPosition;
        [System.NonSerialized][UdonSynced(UdonSyncMode.Smooth)] public float position;
        [System.NonSerialized][UdonSynced] public bool moving, inTransition;
        private bool hasPilot, isOwner;
        private SaccAirVehicle airVehicle;
        private Rigidbody vehicleRigidbody;
        private Animator vehicleAnimator;
        private DFUNC_Brake brakeFunction;
        private Vector3 wheelPositionOffset;
        private Quaternion wheelRotationOffset = Quaternion.identity, steerRotationOffset = Quaternion.identity;
        private float wheelAngle;
        private GameObject burstEffectInstance;
        [System.NonSerialized][UdonSynced] public bool failed, broken, parkingBrake;
        [UdonSynced][FieldChangeCallback(nameof(Bursted))] private bool _bursted;
        private bool Bursted
        {
            set
            {
                if (burstEffect)
                {
                    if (value && !burstEffectInstance)
                    {
                        burstEffectInstance = Instantiate(burstEffect);
                        burstEffectInstance.transform.SetParent(wheelCollider.transform, false);
                    }
                    else if (!value && burstEffectInstance)
                    {
                        Destroy(burstEffectInstance);
                    }
                }

                if (value && !_bursted && transitionSound) transitionSound.PlayOneShot(burstSound);
                wheelCollider.enabled = !value;
                _bursted = value;
            }
            get => _bursted;
        }

        private bool initialized;
        public void SFEXT_L_EntityStart()
        {
            var entity = GetComponentInParent<SaccEntity>();

            airVehicle = entity.GetComponentInChildren<SaccAirVehicle>();
            airVehicle.DisableTaxiRotation_++;
            vehicleAnimator = airVehicle.VehicleAnimator;
            vehicleRigidbody = airVehicle.VehicleRigidbody;

            brakeFunction = entity.GetComponentInChildren<DFUNC_Brake>(true);

            if (suspensionTransform) wheelPositionOffset = suspensionTransform.localPosition - suspensionTransform.parent.InverseTransformPoint(wheelCollider.transform.position);
            if (wheelTransform) wheelRotationOffset = wheelTransform.localRotation;
            if (steerTransform) steerRotationOffset = steerTransform.localRotation;

            gameObject.SetActive(false);
            initialized = true;

            ResetStatus();
        }

        public void SFEXT_O_PilotEnter()
        {
            isOwner = true;
        }

        public void SFEXT_O_TakeOwnership() => isOwner = true;
        public void SFEXT_O_LoseOwnership() => isOwner = false;

        public void SFEXT_G_PilotEnter()
        {
            hasPilot = true;
            gameObject.SetActive(true);
        }
        public void SFEXT_G_PilotExit() => hasPilot = false;
        public void SFEXT_G_Explode() => ResetStatus();
        public void SFEXT_G_RespawnButton() => ResetStatus();

        public void SFEXT_G_GearUp() => targetPosition = 0;
        public void SFEXT_G_GearDown() => targetPosition = 1;

        private Vector3 prevVehiclePosition;
        private bool prevIsGrounded;

        private void Update()
        {
            if (!initialized) return;

            var deltaTime = Time.deltaTime;
            var taxiing = airVehicle.Taxiing;

            var groundVelocity = (isOwner ? vehicleRigidbody.velocity : (vehicleRigidbody.position - prevVehiclePosition) / deltaTime) * 1.94384f;
            prevVehiclePosition = vehicleRigidbody.position;

            var groundSpeed = Vector3.Dot(groundVelocity, vehicleRigidbody.transform.forward);
            if (isOwner)
            {
                inTransition = !Mathf.Approximately(position, targetPosition);
                moving = inTransition && !failed && !broken;
            }
            var retracted = !inTransition && Mathf.Approximately(position, 0);
            var extended = !inTransition && Mathf.Approximately(position, 1);
            var onGround = extended && taxiing;

            if (transitionIndicator && transitionIndicator.activeSelf != inTransition) transitionIndicator.SetActive(inTransition);
            if (downIndicator && downIndicator.activeSelf != extended) downIndicator.SetActive(extended);

            if (transitionSound && transitionSound.isPlaying != moving)
            {
                if (moving) transitionSound.PlayDelayed(Random.value * 0.1f);
                else transitionSound.Stop();
            }

            var targetBrakeStrength = GetTargetBrakeStrength(groundSpeed);
            var targetBrakeTorque = targetBrakeStrength * brakeTorque;
            var currentBrakeTorque = wheelCollider.brakeTorque;
            wheelCollider.brakeTorque = Mathf.MoveTowards(currentBrakeTorque, targetBrakeTorque, brakeTorque * brakeResponse * deltaTime);


            if (inTransition)
            {
                vehicleAnimator.SetFloat(gearPositionParameterName, position);
            }

            if (onGround && maxSteerAngle > 0)
            {
                var normalizedSteerAngle = onGround ? airVehicle.RotationInputs.y : 0;
                var targetSteerAngle = normalizedSteerAngle * maxSteerAngle;
                wheelCollider.steerAngle = Mathf.MoveTowards(wheelCollider.steerAngle, targetSteerAngle, deltaTime * steerResponse * maxSteerAngle);
            }

            if (!retracted)
            {
                if (suspensionTransform)
                {
                    Vector3 wheelPosition; Quaternion _;
                    wheelCollider.GetWorldPose(out wheelPosition, out _);
                    suspensionTransform.localPosition = suspensionTransform.parent.InverseTransformPoint(wheelPosition) * position + wheelPositionOffset;
                }

                if (wheelTransform || steerTransform)
                {
                    var rpm = isOwner ? wheelCollider.rpm : groundSpeed * 0.514444f * 60.0f / (2 * wheelCollider.radius * Mathf.PI);
                    if (taxiing) wheelAngle = (wheelAngle + rpm * 360 / 60 * Time.deltaTime) % 360;
                    var steerRotation = Quaternion.AngleAxis(wheelCollider.steerAngle, wheelUp);
                    if (wheelTransform) wheelTransform.localRotation = wheelRotationOffset * (steerTransform ? Quaternion.identity : steerRotation) * Quaternion.AngleAxis(wheelAngle, wheelRight);
                    if (steerTransform) steerTransform.localRotation = steerRotationOffset * steerRotation;
                }
            }

            if (isOwner)
            {
                if (!retracted)
                {
                    var ias = airVehicle.AirSpeed * 1.94384f;
                    var maxSpeed = _GetMaxSpeed();
                    var overspeed = maxSpeed > 0 && ias > maxSpeed;
                    var mtbfMultiplier = overspeed ? ias / maxSpeed : 1.0f;
                    var isGrounded = wheelCollider.isGrounded;

                    if (!broken && (inTransition || overspeed) && Random.value < deltaTime * mtbfMultiplier / (overspeed ? mtbTransitionBraekOnOverspeed : mtbTransitionBraek)) broken = true;
                    if (!failed && inTransition && Random.value < deltaTime * mtbfMultiplier / (overspeed ? mtbTransitionFailOnOverspeed : mtbTransitionFail)) failed = true;

                    if (brakeTorque > 0 && groundSpeed > brakeMaxGroundSpeed && isGrounded && groundSpeed / brakeMaxGroundSpeed * wheelCollider.brakeTorque / brakeTorque / mtbBurstOnOverGroundSpeed * Time.deltaTime > Random.value)
                    {
                        Burst();
                    }

                    var verticalSpeed = -vehicleRigidbody.velocity.y * 197;
                    if (isGrounded && !prevIsGrounded && verticalSpeed > verticalSpeedLimit && Random.value < (verticalSpeed - verticalSpeedLimit) / (burstVerticalSpeed - verticalSpeedLimit))
                    {
                        Burst();
                    }

                    prevIsGrounded = wheelCollider.isGrounded;
                }

                if (!failed && !broken)
                {
                    var duration = transitionSound ? transitionSound.clip.length : 5.0f;
                    position = Mathf.MoveTowards(position, targetPosition, Time.deltaTime / duration * Random.Range(1.0f - timeNosieScale, 1.0f + timeNosieScale));
                }
            }

            if (!hasPilot && !moving) gameObject.SetActive(false);
        }

        private void Burst()
        {
            Bursted = true;
            RequestSerialization();
        }

        public float _GetMaxSpeed()
        {
            if (Mathf.Approximately(position, 0)) return -1;

            if (Mathf.Approximately(position, 1)) return maxExtendedSpeed;

            return position < 0.5f ? maxExtensionSpeed : maxRetractionSpeed;
        }

        private void ResetStatus()
        {
            if (!initialized) return;

            targetPosition = position = 1.0f;
            if (transitionSound) transitionSound.Stop();
            failed = false;
            broken = false;
            vehicleAnimator.SetFloat(gearPositionParameterName, position);
            Bursted = false;
        }

        private float GetTargetBrakeStrength(float groundSpeed)
        {
            if (Mathf.Approximately(position, 0.0f) || parkingBrake) return 1.0f;
            if (!brakeFunction || (autoLimitGroundSpeed || !Networking.LocalPlayer.IsUserInVR() && autoLimitGroundSpeedOnDesktop) && groundSpeed >= brakeMaxGroundSpeed) return 0;
            var brakeInput = brakeFunction.BrakeInput;
            if (airVehicle.Taxiing && rudderBrake > 0.0f && groundSpeed < brakeMaxGroundSpeed)
            {
                var yawInput = airVehicle.RotationInputs.y;
                return Mathf.Max(Mathf.Clamp01(yawInput / rudderBrake - 1 / Mathf.Abs(rudderBrake) + 1), brakeInput);
            }
            return brakeInput;
        }

        #region Math Utilities
        private float Remap01(float value, float oldMin, float oldMax)
        {
            return (value - oldMin) / (oldMax - oldMin);
        }
        private float ClampedRemap01(float value, float oldMin, float oldMax)
        {
            return Mathf.Clamp01(Remap01(value, oldMin, oldMax));
        }
        #endregion
    }
}

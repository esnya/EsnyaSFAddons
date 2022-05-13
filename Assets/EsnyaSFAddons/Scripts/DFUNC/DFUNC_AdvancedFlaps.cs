
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace EsnyaSFAddons
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class DFUNC_AdvancedFlaps : UdonSharpBehaviour
    {
        [Header("Specs")]
        public float[] detents = {
            0,
            1,
            2,
            5,
            10,
            15,
            25,
            30,
            40,
        };
        [Tooltip("KIAS")]
        public float[] speedLimits = {
            340,
            250,
            250,
            250,
            210,
            200,
            190,
            175,
            162,
        };
        public float dragMultiplier = 1.4f;
        public float liftMultiplier = 1.35f;
        public float response = 1f;
        public GameObject powerSource;

        [Header("Inputs")]
        public float controllerSensitivity = 0.1f;
        public Vector3 vrInputAxis = Vector3.forward;
        public KeyCode desktopKey = KeyCode.F;
        public bool seamless = true;

        [Header("Animator")]
        public string boolParameterName = "flaps";
        public string angleParameterName = "flapsangle", targetAngleParameterName = "flapstarget", brokenParameterName = "flapsbroken";

        [Header("Sounds")]
        public AudioSource[] audioSources = { };
        public float soundResponse = 1;
        public AudioSource[] breakingSounds = { };

        [Header("Faults")]
        public float meanTimeBetweenActuatorBrokenOnOverspeed = 120.0f;
        public float meanTimeBetweenWingBrokenOnOverspeed = 240.0f;
        public float overspeedDamageMultiplier = 10.0f;
        public float brokenDragMultiplier = 2.9f;
        public float brokenLiftMultiplier = 0.3f;

        [Header("Haptics")]
        [Range(0, 1)] public float hapticDuration = 0.2f;
        [Range(0, 1)] public float hapticAmplitude = 0.5f;
        [Range(0, 1)] public float hapticFrequency = 0.1f;

        [HideInInspector] public int targetDetentIndex, detentIndex;
        [HideInInspector] public float detentAngle, targetDetentAngle, speedLimit, targetSpeedLimit, angle, maxAngle;

        private Animator vehicleAnimator;
        [System.NonSerialized] [UdonSynced(UdonSyncMode.Smooth)] public float targetAngle;
        [UdonSynced] bool actuatorBroken;
        [UdonSynced] [FieldChangeCallback(nameof(WingBroken))] bool _wingBroken;
        private bool WingBroken
        {
            set
            {
                if (value == _wingBroken) return;
                _wingBroken = value;

                if (vehicleAnimator)
                {
                    vehicleAnimator.SetBool(brokenParameterName, value);
                }

                if (value)
                {
                    foreach (var audioSource in breakingSounds)
                    {
                        if (audioSource) audioSource.PlayScheduled(Random.value * 0.1f);
                    }
                }
            }
            get => _wingBroken;
        }
        private string triggerAxis;
        private VRCPlayerApi.TrackingDataType trackingTarget;
        private bool hasPilot, isPilot, isOwner, selected;
        private SaccAirVehicle airVehicle;
        private Transform controlsRoot;
        private float[] audioVolumes, audioPitches;
        public void DFUNC_LeftDial()
        {
            triggerAxis = "Oculus_CrossPlatform_PrimaryIndexTrigger";
            trackingTarget = VRCPlayerApi.TrackingDataType.LeftHand;
        }
        public void DFUNC_RightDial()
        {
            triggerAxis = "Oculus_CrossPlatform_SecondaryIndexTrigger";
            trackingTarget = VRCPlayerApi.TrackingDataType.RightHand;
        }

        public void SFEXT_L_EntityStart()
        {
            var entity = GetComponentInParent<SaccEntity>();
            airVehicle = (SaccAirVehicle)entity.GetExtention(GetUdonTypeName<SaccAirVehicle>());

            vehicleAnimator = airVehicle.VehicleAnimator;

            controlsRoot = airVehicle.ControlsRoot;
            if (!controlsRoot) controlsRoot = entity.transform;

            maxAngle = detents[detents.Length - 1];

            audioVolumes = new float[audioSources.Length];
            audioPitches = new float[audioSources.Length];
            for (var i = 0; i < audioSources.Length; i++)
            {
                var audioSource = audioSources[i];
                if (!audioSource) continue;

                audioVolumes[i] = audioSource.volume;
                audioPitches[i] = audioSource.pitch;
            }

            ResetStatus();
        }

        public void SFEXT_O_PilotEnter()
        {
            isPilot = true;
            isOwner = true;
            selected = false;
        }
        public void SFEXT_O_PilotExit() => isPilot = false;

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

        public void DFUNC_Selected() => selected = true;
        public void DFUNC_Deselected() => selected = false;

        private float prevAngle, prevTargetAngle;
        private void Update()
        {
            var deltaTime = Time.deltaTime;

            UpdateDetents();

            if (isOwner) ApplyDamage(deltaTime);

            var actuatorMoving = !actuatorBroken && (!powerSource || powerSource.activeInHierarchy);
            UpdateSounds(deltaTime, actuatorMoving);

            if (actuatorMoving) angle = Mathf.MoveTowards(angle, targetAngle, response * deltaTime);

            var flapsChanged = !Mathf.Approximately(angle, prevAngle);
            prevAngle = angle;

            var targetAngleChanged = !Mathf.Approximately(targetAngle, prevTargetAngle);
            prevTargetAngle = targetAngle;

            if (flapsChanged)
            {
                var flapsDown = !Mathf.Approximately(angle, 0);

                if (vehicleAnimator)
                {
                    vehicleAnimator.SetFloat(angleParameterName, angle / maxAngle);
                    vehicleAnimator.SetBool(boolParameterName, flapsDown);
                }

                ApplyParameters();
            }

            if (targetAngleChanged)
            {
                if (vehicleAnimator) vehicleAnimator.SetFloat(targetAngleParameterName, targetAngle / maxAngle);
            }

            if (!hasPilot && !flapsChanged) gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (isPilot) HandleInput();
        }

        private void ResetStatus()
        {
            angle = targetAngle = 0;
            actuatorBroken = WingBroken = false;

            airVehicle.ExtraDrag -= appliedExtraDrag;
            airVehicle.ExtraLift -= appliedExtraLift;
            appliedExtraDrag = 0;
            appliedExtraLift = 0;

            gameObject.SetActive(false);
        }

        private bool prevTrigger;
        private Vector3 trackingOrigin;
        private float targetAngleOrigin;
        private void HandleInput()
        {
            if (selected)
            {
                var trigger = Input.GetAxis(triggerAxis) > 0.7f;
                var triggerChanged = prevTrigger != trigger;
                prevTrigger = trigger;

                if (trigger)
                {
                    var trackingPosition = controlsRoot.InverseTransformPoint(Networking.LocalPlayer.GetTrackingData(trackingTarget).position);

                    if (triggerChanged)
                    {
                        trackingOrigin = trackingPosition;
                        targetAngleOrigin = targetAngle;
                    }
                    else
                    {
                        targetAngle = Mathf.Clamp(targetAngleOrigin - Vector3.Dot(trackingPosition - trackingOrigin, vrInputAxis) * maxAngle / controllerSensitivity, 0, maxAngle);
                    }
                }

                if (triggerChanged && !trigger && !seamless)
                {
                    UpdateDetents();
                    targetAngle = targetDetentAngle;
                }
            }

            if (Input.GetKeyDown(desktopKey))
            {
                targetAngle = detents[(targetDetentIndex + 1) % detents.Length];
            }
        }

        private void UpdateDetents()
        {
            while (detentIndex > 0 && detents[detentIndex] > angle) detentIndex--;
            while (detentIndex < detents.Length - 1 && detents[detentIndex] < angle) detentIndex++;
            detentAngle = detents[detentIndex];

            var prevTargetDetentIndex = targetDetentIndex;
            while (targetDetentIndex > 0 && detents[targetDetentIndex] > targetAngle) targetDetentIndex--;
            while (targetDetentIndex < detents.Length - 1 && detents[targetDetentIndex] < targetAngle) targetDetentIndex++;

            if (isPilot && targetDetentIndex != prevTargetDetentIndex) PlayHapticEvent();

            targetDetentAngle = detents[targetDetentIndex];

            targetSpeedLimit = speedLimits[targetDetentIndex];
            speedLimit = speedLimits[detentIndex];
        }

        private void UpdateSounds(float deltaTime, bool actuatorAvailable)
        {
            var moving = actuatorAvailable && !Mathf.Approximately(targetAngle, angle);

            for (var i = 0; i < audioSources.Length; i++)
            {
                var audioSource = audioSources[i];
                if (!audioSource) continue;

                var volume = Mathf.Lerp(audioSource.volume, moving ? audioVolumes[i] : 0.0f, soundResponse * deltaTime);
                var stop = Mathf.Approximately(volume, 0);

                if (stop)
                {
                    if (audioSource.isPlaying)
                    {
                        audioSource.Stop();
                        audioSource.volume = 0;
                        audioSource.pitch = 0.8f;
                    }
                }
                else
                {
                    audioSource.volume = volume;
                    audioSource.pitch = Mathf.Lerp(audioSource.volume, (moving ? 1.0f : 0.8f) * audioPitches[i], soundResponse * deltaTime);

                    if (!audioSource.isPlaying)
                    {
                        audioSource.loop = true;
                        audioSource.time = audioSource.clip.length * (Random.value % 1.0f);
                        audioSource.Play();
                    }
                }
            }
        }

        private void ApplyDamage(float deltaTime)
        {
            var airSpeed = airVehicle.AirSpeed * 1.94384f; // KAIS
            var damage = Mathf.Max(airSpeed - speedLimit, 0) / speedLimit * overspeedDamageMultiplier;
            if (damage > 0)
            {
                if (!actuatorBroken && Random.value < damage * deltaTime / meanTimeBetweenActuatorBrokenOnOverspeed)
                {
                    actuatorBroken = true;
                }

                if (!WingBroken && Random.value < damage * deltaTime / meanTimeBetweenWingBrokenOnOverspeed)
                {
                    WingBroken = true;
                    actuatorBroken = true;
                    ApplyParameters();
                }
            }
        }

        private float appliedExtraDrag, appliedExtraLift;
        private void ApplyParameters()
        {
            var normalizedPosition = angle / maxAngle;
            var extraDrag = WingBroken ? brokenDragMultiplier - 1 : (dragMultiplier - 1) * normalizedPosition;
            var extraLift = WingBroken ? brokenLiftMultiplier - 1 : (liftMultiplier - 1) * normalizedPosition;

            airVehicle.ExtraDrag += extraDrag - appliedExtraDrag;
            airVehicle.ExtraLift += extraLift - appliedExtraLift;

            appliedExtraDrag = extraDrag;
            appliedExtraLift = extraLift;
        }

        private void PlayHapticEvent()
        {
            var hand = trackingTarget == VRCPlayerApi.TrackingDataType.LeftHand ? VRC_Pickup.PickupHand.Left : VRC_Pickup.PickupHand.Right;
            Networking.LocalPlayer.PlayHapticEventInHand(hand, hapticDuration, hapticAmplitude, hapticFrequency);
        }


        private void SetTargetDetentIndex(int value)
        {
            targetAngle = detents[Mathf.Clamp(value, 0, detents.Length - 1)];
            UpdateDetents();
        }
        public void NextDetent()
        {
            SetTargetDetentIndex(targetDetentIndex + 1);
        }

        public void PreviousDetent()
        {
            SetTargetDetentIndex(targetDetentIndex - 1);
        }
    }
}

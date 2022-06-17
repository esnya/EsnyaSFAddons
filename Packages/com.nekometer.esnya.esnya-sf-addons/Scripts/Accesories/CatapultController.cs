
using JetBrains.Annotations;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace EsnyaSFAddons.Accesory
{
    /// <summary>
    /// Provide realstic control for catapult such as tension, speed and trigger launch by crew
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CatapultController : UdonSharpBehaviour
    {
        /// <summary>
        /// Target catapult trigger
        /// </summary>
        [NotNull] public Transform catapultTrigger;

        /// <summary>
        /// Detects vehicle in this range
        /// </summary>
        public float planeDetectionRadius = 2.0f;

        /// <summary>
        /// Layer mask for detecting vehicle
        /// </summary>
        public LayerMask planeDetectionLayerMask = 1 << 31 | 1 << 25 | 1 << 17;

        /// <summary>
        /// (Optional) Activate when tension
        /// </summary>
        public GameObject tensionIndicator;

        /// <summary>
        /// (Optional) Displays launch power
        /// </summary>
        public TextMeshPro launchSpeedDisplay;

        /// <summary>
        /// Multiplier for launch speed to speed
        /// </summary>
        public float launchSpeedAnimatroMultiplier = 1.0f;

        /// <summary>
        /// Multiplier for launch speed to display text
        /// </summary>
        public float launchSpeedDisplayMultiplier = 1.0f;

        /// <summary>
        /// Format of display launch speed
        /// </summary>
        public string launchSpeedDisplayFormat = "P0";

        /// <summary>
        /// Increase or decrease step of launch speed
        /// </summary>
        public float launchSpeedStep = 0.1f;


        [UdonSynced][FieldChangeCallback(nameof(Tension))] private bool _tension;
        /// <summary>
        /// Tension
        /// </summary>
        /// <value></value>
        public bool Tension
        {
            private set
            {
                _tension = value;
                if (catapultAnimator) catapultAnimator.SetBool("tension", value);
                if (tensionIndicator) tensionIndicator.SetActive(value);
            }
            get => _tension;
        }

        [UdonSynced][FieldChangeCallback(targetPropertyName: nameof(LaunchSpeed))] private float _launchSpeed;
        /// <summary>
        /// Speed of launch
        /// </summary>
        /// <value></value>
        public float LaunchSpeed
        {
            private set
            {
                _launchSpeed = value;
                if (catapultAnimator) catapultAnimator.SetFloat("launchspeed", value * launchSpeedAnimatroMultiplier);
                if (launchSpeedDisplay) launchSpeedDisplay.text = (value * launchSpeedDisplayMultiplier).ToString(launchSpeedDisplayFormat);
            }
            get => _launchSpeed;
        }

        private Animator catapultAnimator;

        private void Start()
        {
            catapultAnimator = catapultTrigger.GetComponentInParent<Animator>();
            Tension = false;
            LaunchSpeed = 1.0f;
        }

        /// <summary>
        /// Take ownership if not owner
        /// </summary>
        public void _TakeOwnership()
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        /// <summary>
        /// Trigger launch
        ///
        /// Tension before launch
        /// </summary>
        public void _Launch()
        {
            if (!Tension) return;
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PreLaunch));
            SendCustomEventDelayedSeconds(nameof(_DisableTension), 6);
        }

        /// <summary>
        /// Toggle tension
        /// </summary>
        public void _ToggleTension()
        {
            _TakeOwnership();
            Tension = !Tension;
            RequestSerialization();
        }

        /// <summary>
        /// Turn off tension
        /// </summary>
        public void _DisableTension()
        {
            if (Tension) _ToggleTension();
        }

        private DFUNC_Catapult FindDFunc()
        {
            foreach (var collider in Physics.OverlapSphere(catapultTrigger.position, planeDetectionRadius, planeDetectionLayerMask, QueryTriggerInteraction.Collide))
            {
                if (!collider) continue;

                var rigidbody = collider.attachedRigidbody;
                if (!rigidbody) continue;

                var saccEntity = rigidbody.GetComponent<SaccEntity>();
                if (!saccEntity) continue;

                foreach (var dfunc in saccEntity.gameObject.GetComponentsInChildren<DFUNC_Catapult>(true))
                {
                    if (!dfunc || !dfunc.OnCatapult || dfunc.Launching || !dfunc.CatapultTransform) continue;
                    return dfunc;
                }
            }

            return null;
        }

        /// <summary>
        /// Emulator of DFUNC_Catapult.PreLaunch
        /// </summary>
        public void PreLaunch()
        {
            var dfunc = FindDFunc();
            if (!dfunc)
            {
                SendCustomEventDelayedFrames(nameof(_FakeLaunch), 3);
                return;
            }

            dfunc.Launching = true;
            dfunc.PreLaunchCatapult();
        }

        /// <summary>
        /// Internal event used when plane not found
        /// </summary>
        public void _FakeLaunch()
        {
            if (catapultAnimator) catapultAnimator.SetTrigger("launch");
        }

        private void AddLaunchSpeed(float value)
        {
            _TakeOwnership();
            LaunchSpeed = Mathf.Clamp01(LaunchSpeed + value);
            RequestSerialization();
        }
        /// <summary>
        /// Increment launch speed
        /// </summary>
        public void _IncrementLaunchSpeed() => AddLaunchSpeed(launchSpeedStep);
        /// <summary>
        /// Decrement launch speed
        /// </summary>
        public void _DecrementLaunchSpeed() => AddLaunchSpeed(value: -launchSpeedStep);
    }
}

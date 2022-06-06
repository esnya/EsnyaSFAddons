
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace EsnyaSFAddons
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CatapultController : UdonSharpBehaviour
    {
        public Transform catapultTrigger;
        public float planeDetectionRadius = 2.0f;
        public LayerMask planeDetectionLayerMask = 1 << 31 | 1 << 25 | 1 << 17;
        public GameObject tensionIndicator;
        public TextMeshPro launchSpeedDisplay;
        public float launchSpeedAnimatroMultiplier = 1.0f;
        public float launchSpeedDisplayMultiplier = 1.0f;
        public string launchSpeedDisplayFormat = "P0";
        public float launchSpeedStep = 0.1f;


        [UdonSynced][FieldChangeCallback(nameof(Tension))] private bool _tension;
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

        public void _TakeOwnership()
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        public void _Launch()
        {
            if (!Tension) return;
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PreLaunch));
            SendCustomEventDelayedSeconds(nameof(_DisableTension), 6);
        }

        public void _ToggleTension()
        {
            _TakeOwnership();
            Tension = !Tension;
            RequestSerialization();
        }

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
        public void _IncrementLaunchSpeed() => AddLaunchSpeed(launchSpeedStep);
        public void _DecrementLaunchSpeed() => AddLaunchSpeed(value: -launchSpeedStep);
    }
}

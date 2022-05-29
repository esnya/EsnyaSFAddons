
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

        private Animator catapultAnimator;
        [UdonSynced][FieldChangeCallback(nameof(Tension))] private bool _tension;

        public bool Tension {
            private set {
                _tension = value;
                if (catapultAnimator) catapultAnimator.SetBool("tension", value);
            }
            get => _tension;
        }

        private void Start()
        {
            catapultAnimator = catapultTrigger.GetComponentInParent<Animator>();
            Tension = false;
        }

        public void _Launch()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PreLaunch));
            SendCustomEventDelayedSeconds(nameof(_DisableTension), 6);
        }

        public void _ToggleTension()
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
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
                    if (!dfunc || !dfunc.OnCatapult || dfunc.Launching) continue;
                    return dfunc;
                }
            }

            return null;
        }

        public void PreLaunch()
        {
            var dfunc = FindDFunc();
            if (!dfunc) SendCustomEventDelayedFrames(nameof(_FakeLaunch), 3);

            dfunc.OnCatapult = true;
            dfunc.Launching = true;
            dfunc.PreLaunchCatapult();
        }

        public void _FakeLaunch()
        {
            if (catapultAnimator) catapultAnimator.SetTrigger("launch");
        }
    }
}

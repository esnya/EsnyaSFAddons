
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace EsnyaSFAddons
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CatapultController : UdonSharpBehaviour
    {
        public Transform catapultTrigger;
        public float planeDetectionRadius = 2.0f;
        public LayerMask planeDetectionLayerMask = 1 << 31 | 1 << 25 | 1 << 17;

        public override void Interact()
        {
            var dfunc = FindDFunc();
            if (!dfunc) return;
            dfunc.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(DFUNC_Catapult.KeyboardInput));
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

                var dfunc = (DFUNC_Catapult)saccEntity.GetExtention(GetUdonTypeName<DFUNC_Catapult>());
                if (!dfunc || !dfunc.OnCatapult) continue;

                return dfunc;
            }

            return null;
        }
    }
}

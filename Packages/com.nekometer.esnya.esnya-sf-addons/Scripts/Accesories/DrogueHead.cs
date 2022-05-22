using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace EsnyaSFAddons
{
    [RequireComponent(typeof(Rigidbody))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DrogueHead : UdonSharpBehaviour
    {
        public string prefix = "Probe";
        public UdonSharpBehaviour eventTarget;
        public string eventName = "_Contact";
        [System.NonSerialized] public SaccEntity targetEntity;
        [System.NonSerialized] public Collider probeCollider;

        private void OnTriggerEnter(Collider other)
        {
            if (!other || !other.attachedRigidbody || !other.gameObject.name.StartsWith(prefix)) return;
            probeCollider = other;

            var vehicleRigidbody = other.attachedRigidbody;
            if (!Networking.IsOwner(vehicleRigidbody.gameObject)) return;

            targetEntity= vehicleRigidbody.GetComponent<SaccEntity>();
            if (!targetEntity) return;

            eventTarget.SendCustomEvent(eventName);
        }
    }
}


using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace EsnyaAircraftAssets
{
    public class VehicleLock : UdonSharpBehaviour
    {
        public VRCStation pilotSeat;
        public float timeToLock = 60;

        private void Start()
        {
            _Lock();
        }

        public override void Interact()
        {
            if (!enabled || !gameObject.activeInHierarchy) return;
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Unlock));
        }

        public void _Lock()
        {
            SetColliderEnabled(false);
        }

        public void Unlock()
        {
            SetColliderEnabled(true);
            SendCustomEventDelayedSeconds(nameof(_Lock), timeToLock);
        }

        private void SetColliderEnabled(bool value)
        {
            foreach (var c in pilotSeat.GetComponents<Collider>()) c.enabled = value;
        }
    }
}
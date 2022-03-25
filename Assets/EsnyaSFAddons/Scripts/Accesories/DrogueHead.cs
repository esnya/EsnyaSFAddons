
using System.Net;
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
        public float detatchDistance = 2.0f;
        public AudioSource fuelingSound;
        public AudioClip contactSound, detachSound;
        public float udpateInterval = 1f;
        public int resupplylayer = 27;
        private SaccEntity target;
        private Transform probe;

        private void OnDisable()
        {
            target = null;
            probe = null;
            if (fuelingSound) fuelingSound.Stop();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other || !other.attachedRigidbody || other.gameObject.layer != resupplylayer) return;

            var vehicleRigidbody = other.attachedRigidbody;
            if (!Networking.IsOwner(vehicleRigidbody.gameObject)) return;

            target = vehicleRigidbody.GetComponent<SaccEntity>();
            if (!target) return;
            probe = other.transform;

            if (contactSound && fuelingSound) fuelingSound.PlayOneShot(contactSound);
            if (fuelingSound && !fuelingSound.isPlaying) fuelingSound.Play();

            SendCustomEventDelayedSeconds(nameof(_Supply), udpateInterval);
        }

        public void _Supply()
        {
            if (!target || !probe || Vector3.Distance(transform.position, probe.position) > detatchDistance)
            {
                if (detachSound && fuelingSound) fuelingSound.PlayOneShot(detachSound);
                if (fuelingSound) fuelingSound.Stop();
            }
            else
            {
                target.SendEventToExtensions("SFEXT_O_ReSupply");
                SendCustomEventDelayedSeconds(nameof(_Supply), udpateInterval);
            }
        }
    }
}
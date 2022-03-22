
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace EsnyaSFAddons
{
    [RequireComponent(typeof(VRCObjectSync))]
    [RequireComponent(typeof(VRCPickup))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PickupChock : UdonSharpBehaviour
    {
        [Tooltip("Respawn if placed outside of this range. [m]")] public float maxDistance = 5;
        [Tooltip("[s]")] public float respawnTimeout = 5;
        public LayerMask groundLayerMask = 0x0801;

        private VRCObjectSync objectSync;
        private VRCPickup pickup;
        private Collider[] colliders;
        private bool[] colliderTriggerFlags;
        private Vector3 respawnPosition;
        private Quaternion respawnRotation;
        private float lastDropTime;
        private void Start()
        {
            objectSync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));
            pickup = (VRCPickup)GetComponent(typeof(VRCPickup));
            colliders = GetComponentsInChildren<Collider>(true);

            colliderTriggerFlags = new bool[colliders.Length];
            for (var i = 0; i < colliders.Length; i++)
            {
                colliderTriggerFlags[i] = colliders[i].isTrigger;
            }

            respawnPosition = transform.localPosition;
            respawnRotation = transform.localRotation;
        }

        public override void OnPickup()
        {
            SetTriggerFlags(true);
        }

        public override void OnDrop()
        {
            lastDropTime = Time.time;
            SendCustomEventDelayedSeconds(nameof(_CheckDistance), respawnTimeout);

            RaycastHit hitInfo;
            if (Physics.Raycast(transform.position, Vector3.down, out hitInfo, maxDistance, groundLayerMask, QueryTriggerInteraction.Ignore))
            {
                transform.position = hitInfo.point;
                transform.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(Vector3.forward, transform.forward, hitInfo.normal), hitInfo.normal);
            }

            SetTriggerFlags(false);
        }

        public void _CheckDistance()
        {
            if (!pickup.IsHeld && Time.time - (lastDropTime + respawnTimeout) < 1 && Vector3.Distance(transform.localPosition, respawnPosition) * transform.lossyScale.x > maxDistance)
            {
                objectSync.FlagDiscontinuity();
                transform.localPosition = respawnPosition;
                transform.localRotation = respawnRotation;
            }
        }

        private void SetTriggerFlags(bool value)
        {
            for (var i = 0; i < colliders.Length; i++)
            {
                if (colliderTriggerFlags[i]) continue;
                colliders[i].isTrigger = value;
            }
        }
    }
}

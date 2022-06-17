
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace EsnyaSFAddons.Accesory
{
    /// <summary>
    /// Wheel Chock
    ///
    /// Synced transform and snapped on ground.
    /// </summary>
    [RequireComponent(typeof(VRCPickup))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class PickupChock : UdonSharpBehaviour
    {
        /// <summary>
        /// Layers of grounds
        /// </summary>
        public LayerMask groundLayerMask = 0x0801;

        /// <summary>
        /// Maximum distance to snap ground
        /// </summary>
        public float raycastDistance = 3.0f;

        /// <summary>
        /// Offset to detect ground adove chock
        /// </summary>
        public float raycastOffset = 1.0f;

        /// <summary>
        /// Delay to sleep after snapping on ground
        /// </summary>
        public float sleepTimeout = 3.0f;

        private VRCPickup pickup;
        private Collider[] colliders;
        private bool[] colliderTriggerFlags;

        private float wakeUpTime;
        private bool _moving;
        private bool Moving {
            set {
                if (value) wakeUpTime = Time.time;
                SetTriggerFlags(value);
                _moving = value;
            }
            get => _moving;
        }

        [UdonSynced(UdonSyncMode.Smooth)][FieldChangeCallback(nameof(Position))] private Vector3 _position;
        private Vector3 Position {
            set {
                Moving = true;
                transform.position = value;

                _position = value;
            }
            get => _position;
        }
        [UdonSynced(UdonSyncMode.Smooth)] private float _angle;
        private float Angle {
            set {
                Moving = true;

                transform.rotation = Quaternion.AngleAxis(value, Vector3.up);

                _angle = value;
            }
            get => _angle;
        }

        private void Start()
        {
            pickup = (VRCPickup)GetComponent(typeof(VRCPickup));
            colliders = GetComponentsInChildren<Collider>(true);

            colliderTriggerFlags = new bool[colliders.Length];
            for (var i = 0; i < colliders.Length; i++)
            {
                colliderTriggerFlags[i] = colliders[i].isTrigger;
            }

            Moving = false;
        }

        private void Update()
        {
            if (Moving && Time.time > wakeUpTime + sleepTimeout)
            {
                Moving = false;
            }
        }

        public override void OnPickup()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(RemoteOnPickup));
        }

        public override void OnDrop()
        {
            RaycastHit hitInfo;
            if (Physics.Raycast(transform.position + Vector3.up * raycastOffset, Vector3.down, out hitInfo, raycastDistance, groundLayerMask, QueryTriggerInteraction.Ignore))
            {
                Position = hitInfo.point;
            }
            else
            {
                Position = transform.position;
            }
            Angle = Vector3.SignedAngle(Vector3.forward, transform.forward, Vector3.up);
        }

        public void RemoteOnPickup()
        {
            Moving = true;
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

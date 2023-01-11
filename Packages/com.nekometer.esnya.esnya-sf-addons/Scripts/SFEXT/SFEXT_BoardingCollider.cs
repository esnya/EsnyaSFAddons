
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UdonSharpEditor;
#endif

namespace EsnyaSFAddons.SFEXT
{
    /// <summary>
    /// Attach collider to enables walk inside plane without getting blown away.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_BoardingCollider : UdonSharpBehaviour
    {
        /// <summary>
        /// Enables on water. If plane will be not floating, set false to better performance.
        /// </summary>
        public bool enableOnWater = true;

        // public float fakeFriction = 0.1f;

        private Quaternion localRotation;
        private SaccEntity entity;
        private Transform entityTransform;
        private Vector3 localPosition;

        private bool _onBoarding;
        private bool OnBoarding
        {
            set
            {
                if (value)
                {
                    PlayerEnterCount = 0;
                }
                _onBoarding = value;
                CheckState();
            }
            get => _onBoarding;
        }

        private bool _onGround;
        private Vector3 prevPosition;
        private Quaternion prevRotation;
        private int _playerEnterCount;

        private int PlayerEnterCount
        {
            get => _playerEnterCount;
            set
            {
                var prevStay = _playerEnterCount > 0;
                var nextStay = value > 0;

                _playerEnterCount = Mathf.Max(value, 0);
                if (prevStay != nextStay)
                {
                    entity.SendEventToExtensions(nextStay ? "SFEXT_L_BoardingEnter" : "SFEXT_L_BoardingExit");
                    CheckState();
                }

            }
        }

        private bool OnGround
        {
            set
            {
                _onGround = value;
                CheckState();
            }
            get => _onGround;
        }

        public void SFEXT_L_EntityStart()
        {
            entity = GetComponentInParent<SaccEntity>();
            entityTransform = entity.transform;
            localPosition = entityTransform.InverseTransformPoint(transform.position);
            localRotation = Quaternion.Inverse(entityTransform.rotation) * transform.rotation;

            OnBoarding = false;
            OnGround = true;

            SendCustomEventDelayedSeconds(nameof(_LateStart), 1.0f);
        }

        public void _LateStart()
        {
            gameObject.name = $"{entityTransform.gameObject.name}_{gameObject.name}";
            transform.SetParent(entityTransform.parent, true);
        }

        public void SFEXT_O_PilotEnter()
        {
            OnBoarding = true;
        }
        public void SFEXT_O_PilotExit()
        {
            OnBoarding = false;
        }

        public void SFEXT_P_PassengerEnter()
        {
            OnBoarding = true;
        }
        public void SFEXT_P_PassengerExit()
        {
            OnBoarding = false;
        }

        public void SFEXT_G_TakeOff()
        {
            OnGround = false;
        }
        public void SFEXT_G_TouchDown()
        {
            OnGround = true;
        }
        public void SFEXT_G_TouchDownWater()
        {
            OnGround = enableOnWater;
        }

        public void SFEXT_G_ReAppear()
        {
            PlayerEnterCount = 0;
        }

        public override void PostLateUpdate()
        {
            if (!entityTransform) return;

            var position = entityTransform.TransformPoint(localPosition);
            var rotation = entityTransform.rotation * localRotation;
            transform.position = position;
            transform.rotation = rotation;

            var playerStay = PlayerEnterCount > 0;

            if (playerStay)
            {
                var localPlayer = Networking.LocalPlayer;
                var playerPosition = localPlayer.GetPosition();
                var playerRotation = localPlayer.GetRotation();

                var positionDiff = position - prevPosition;
                var rotationDiff = Quaternion.Inverse(prevRotation) * rotation;

                var nextPlayerPosition = rotationDiff * (playerPosition - position) + position + positionDiff;
                if (!Mathf.Approximately(Vector3.Distance(nextPlayerPosition, playerPosition), 0.0f))
                {
                    localPlayer.TeleportTo(nextPlayerPosition, playerRotation * Quaternion.Slerp(rotationDiff, Quaternion.identity, 0.5f));
                }
            }

            prevPosition = position;
            prevRotation = rotation;
        }

        private void CheckState()
        {
            var active = !OnBoarding && OnGround || PlayerEnterCount > 0;
            if (active != gameObject.activeSelf)
            {
                gameObject.SetActive(active);
                if (!active) PlayerEnterCount = 0;
            }
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (player.isLocal) _PlayerEnter();
        }
        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (player.isLocal) _PlayerExit();
        }

        public void _PlayerEnter()
        {
            PlayerEnterCount++;
        }

        public void _PlayerExit()
        {
            PlayerEnterCount--;
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (PlayerEnterCount > 0)
            {
                Gizmos.color = Color.red;
                foreach (var floorCollider in GetComponentsInChildren<Collider>())
                {
                    var bounds = floorCollider.bounds;
                    Gizmos.DrawWireCube(bounds.center, bounds.size);
                }
            }
        }
#endif
    }
}

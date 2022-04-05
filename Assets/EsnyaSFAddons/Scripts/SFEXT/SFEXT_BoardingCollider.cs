
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace EsnyaSFAddons
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SFEXT_BoardingCollider : UdonSharpBehaviour
    {
        private Collider[] colliders;
        private Quaternion localRotation;
        private Transform entityTransform;
        private Vector3 localPosition;

        [UdonSynced][FieldChangeCallback(nameof(DoorsOpened))] private bool _doorsOpened;
        private bool DoorsOpened
        {
            set
            {
                _doorsOpened = value;
                UpdateCollidersEnabled();
            }
            get => _doorsOpened;
        }
        private bool _onBoarding;
        private bool OnBoarding {
            set {
                _onBoarding = value;
                UpdateCollidersEnabled();
            }
            get => _onBoarding;
        }
        private bool _collidersEnabled;
        private bool CollidersEnabled {
            set {
                _collidersEnabled = value;
                foreach (var collider in colliders) collider.enabled = value;
            }
            get => _collidersEnabled;
        }

        public void SFEXT_L_EntityStart()
        {
            colliders = GetComponentsInChildren<Collider>(true);

            entityTransform = GetComponentInParent<SaccEntity>().transform;
            localPosition = entityTransform.InverseTransformPoint(transform.position);
            localRotation = Quaternion.Inverse(entityTransform.rotation) * transform.rotation;

            transform.SetParent(entityTransform.parent, true);

            gameObject.name = $"{entityTransform.gameObject.name}_{gameObject.name}";

            DoorsOpened = true;
            OnBoarding = false;
        }

        public void SFEXT_O_DoorsClosed()
        {
            DoorsOpened = false;
            RequestSerialization();
        }

        public void SFEXT_O_DoorsOpened()
        {
            DoorsOpened = true;
            RequestSerialization();
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

        public override void PostLateUpdate()
        {
            if (!DoorsOpened) return;
            transform.position = entityTransform.TransformPoint(localPosition);
            transform.rotation = entityTransform.rotation * localRotation;
        }

        private void UpdateCollidersEnabled()
        {
            var value = DoorsOpened && !OnBoarding;
            if (value != CollidersEnabled) CollidersEnabled = true;
        }
    }
}

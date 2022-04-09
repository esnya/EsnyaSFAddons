
using System;
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
        private Quaternion localRotation;
        private Transform entityTransform;
        private Vector3 localPosition;

        private bool _onBoarding;
        private bool OnBoarding {
            set {
                _onBoarding = value;
                CheckState();
            }
            get => _onBoarding;
        }

        private bool _onGround;
        private bool OnGround {
            set {
                _onGround = value;
                CheckState();
            }
            get => _onGround;
        }

        public void SFEXT_L_EntityStart()
        {
            entityTransform = GetComponentInParent<SaccEntity>().transform;
            localPosition = entityTransform.InverseTransformPoint(transform.position);
            localRotation = Quaternion.Inverse(entityTransform.rotation) * transform.rotation;

            transform.SetParent(entityTransform.parent, true);

            gameObject.name = $"{entityTransform.gameObject.name}_{gameObject.name}";

            OnBoarding = false;
            OnGround = true;
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

        public override void PostLateUpdate()
        {
            transform.position = entityTransform.TransformPoint(localPosition);
            transform.rotation = entityTransform.rotation * localRotation;
        }

        private void CheckState()
        {
            var active = !OnBoarding && OnGround;
            if (active != gameObject.activeSelf) gameObject.SetActive(active);
        }
    }
}

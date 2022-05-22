using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace EsnyaSFAddons
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class WheelDriver : UdonSharpBehaviour
    {
        public GameObject ownerDetector;

        public WheelCollider wheelCollider;
        public Transform[] wheelTransforms = {};
        public Vector3 axis = Vector3.up;

        private Quaternion[] initialAngles;

        [UdonSynced(UdonSyncMode.Linear), FieldChangeCallback(nameof(Angle))] private float _angle;
        private float Angle
        {
            set
            {
                _angle = value;

                var q = Quaternion.AngleAxis(value, axis);
                for (var i = 0; i < wheelTransforms.Length; i++)
                {
                    var wheel = wheelTransforms[i];
                    if (wheel == null) continue;
                    wheel.localRotation = initialAngles[i] * q;
                }
            }
            get => _angle;
        }

        private void Start()
        {
            if (ownerDetector == null) ownerDetector = gameObject;

            initialAngles = new Quaternion[wheelTransforms.Length];
            for (var i = 0; i < wheelTransforms.Length; i++) initialAngles[i] = wheelTransforms[i].localRotation;
        }

        private void Update()
        {
            if (!Networking.IsOwner(gameObject)) return;

            if (!Networking.IsOwner(ownerDetector))
            {
                Networking.SetOwner(Networking.GetOwner(ownerDetector), gameObject);
                return;
            }

            Angle += wheelCollider.rpm * Time.deltaTime * 360 / 60;
        }
    }
}

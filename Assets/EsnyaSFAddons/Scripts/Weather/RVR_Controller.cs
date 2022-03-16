
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace EsnyaSFAddons
{
    [RequireComponent(typeof(VRCPickup))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class RVR_Controller : UdonSharpBehaviour
    {
        [Header("Scale")]
        public float handleDistance = 0.5f;

        [Header("Fog Control")]
        public AnimationCurve fogCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Material Control (Skybox)")]
        public Material material;
        public string parameterName;
        public AnimationCurve parameterCurve = AnimationCurve.Linear(0, 0, 1, 1);

        private VRCPickup pickup;
        private Quaternion localRotation;

        [UdonSynced(UdonSyncMode.Smooth)][FieldChangeCallback(nameof(Value))] public float _value;
        private float Value
        {
            set
            {
                _value = Mathf.Clamp01(value);
                if (!Networking.IsOwner(gameObject)) SetHandlePosition();

                RenderSettings.fogDensity = fogCurve.Evaluate(_value);
                if (material) material.SetFloat(parameterName, parameterCurve.Evaluate(_value));
            }
            get => _value;
        }

        private void Start()
        {
            localRotation = transform.localRotation;
            pickup = (VRCPickup)GetComponent(typeof(VRCPickup));
            SetHandlePosition();
        }

        private void Update()
        {
            if (!pickup.IsHeld) return;
            Value = Vector3.Dot(transform.localPosition, Vector3.forward) / handleDistance;
        }

        public override void OnDrop()
        {
            SetHandlePosition();
        }

        private void SetHandlePosition()
        {
            transform.localPosition = Vector3.forward * Value * handleDistance;
            transform.localRotation = localRotation;
        }
    }
}

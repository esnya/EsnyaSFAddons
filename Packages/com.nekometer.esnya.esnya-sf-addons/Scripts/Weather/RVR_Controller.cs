
using UdonSharp;
using UdonToolkit;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace EsnyaSFAddons
{
    /// <summary>
    /// Controls scene fog and skybox parameters.
    ///
    /// Attach to pickup object as handle like controller.
    /// </summary>
    [RequireComponent(typeof(VRCPickup))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class RVR_Controller : UdonSharpBehaviour
    {
        [Header("Scale")]
        /// <summary>
        /// Max moving distance of handle.
        /// </summary>
        public float handleDistance = 0.5f;

        [Header("Fog Control")]
        /// <summary>
        /// Curve of fog value.
        ///
        /// At t = 0 with no fog, at t = 1 with maximum fog.
        /// </summary>
        public AnimationCurve fogCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Material Control (Skybox)")]

        /// <summary>
        /// Control skybox material parameter.
        /// </summary>
        public bool controlSkybox = false;

        /// <summary>
        /// Name of material parameter
        /// </summary>
        [HideIf("@!controlSkybox")] public string parameterName;

        /// <summary>
        /// Curve of parameter value.
        ///
        /// At t = 0 with no fog, at t = 1 with maximum fog.
        /// </summary>
        [HideIf("@!controlSkybox")] public AnimationCurve parameterCurve = AnimationCurve.Linear(0, 0, 1, 1);

        private VRCPickup pickup;
        private Quaternion localRotation;
        private Material skyboxMaterial;

        [Header("Initial Values")]
        /// <summary>
        /// Initial value of fog density.
        /// </summary>
        [UdonSynced(UdonSyncMode.Smooth)][FieldChangeCallback(nameof(Value))] public float _value;
        private float Value
        {
            set
            {
                _value = Mathf.Clamp01(value);
                if (!Networking.IsOwner(gameObject)) SetHandlePosition();

                RenderSettings.fogDensity = fogCurve.Evaluate(_value);
                if (skyboxMaterial) skyboxMaterial.SetFloat(parameterName, parameterCurve.Evaluate(_value));
            }
            get => _value;
        }

        private void Start()
        {
            localRotation = transform.localRotation;
            pickup = (VRCPickup)GetComponent(typeof(VRCPickup));
            skyboxMaterial = controlSkybox ? RenderSettings.skybox : null;
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

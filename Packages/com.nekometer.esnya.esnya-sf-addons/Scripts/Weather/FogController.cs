using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace EsnyaSFAddons.Weather
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class FogController : UdonSharpBehaviour
    {
        public Slider slider;
        public Renderer fogRenderer;
        public string propertyName = "_FogStrength";

        [UdonSynced(UdonSyncMode.Linear), FieldChangeCallback(nameof(FogStrength))] private float _fogStrength;
        public float FogStrength
        {
            private set
            {
                _fogStrength = value;
                slider.value = value;

                fogRenderer.GetPropertyBlock(properties);
                properties.SetFloat(propertyName, Mathf.Exp(value));
                fogRenderer.SetPropertyBlock(properties);
            }
            get => _fogStrength;
        }

        public float MinStrength => slider.minValue;
        public float MaxStrength => slider.maxValue;
        public float NormalizedStrength => (FogStrength - MinStrength) / (MaxStrength - MinStrength);

        private MaterialPropertyBlock properties;
        void Start()
        {
            properties = new MaterialPropertyBlock();
            FogStrength = slider.value;
        }

        public void _Apply()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            FogStrength = slider.value;
        }
    }
}

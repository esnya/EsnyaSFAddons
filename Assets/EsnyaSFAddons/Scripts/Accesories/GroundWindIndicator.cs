using EsnyaAircraftAssets;
using TMPro;
using UdonSharp;
using UnityEngine;

namespace EsnyaSFAddons
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GroundWindIndicator : UdonSharpBehaviour
    {
        public float updateInterval = 10.0f;

        [UdonSharpComponentInject] public SAV_WindChanger windChanger;
        public Transform directionIndicator;
        public TextMeshProUGUI speedText;

        private void OnEnable()
        {
            SendCustomEventDelayedSeconds(nameof(_ThinUpdate), Random.Range(0, updateInterval));
        }

        public void _ThinUpdate()
        {
            if (!gameObject.activeInHierarchy || !windChanger) return;

            var strength = windChanger.WindStrength * 1.944f;
            var gust = windChanger.WindGustStrength * 1.944f;
            var angle = Vector3.SignedAngle(windChanger.transform.forward, Vector3.forward, Vector3.up);

            if (directionIndicator)
            {
                directionIndicator.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }

            if (speedText)
            {
                speedText.text = $"{strength:0}\n{strength + gust:0}";
            }

            SendCustomEventDelayedSeconds(nameof(_ThinUpdate), updateInterval);
        }
    }
}

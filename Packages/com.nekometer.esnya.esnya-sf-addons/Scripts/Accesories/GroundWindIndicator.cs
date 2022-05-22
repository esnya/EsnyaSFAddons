using EsnyaAircraftAssets;
using TMPro;
using UdonSharp;
using UnityEngine;

namespace EsnyaSFAddons
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GroundWindIndicator : UdonSharpBehaviour
    {
        public int updateIntervalFrames = 90;

        [UdonSharpComponentInject] public SAV_WindChanger windChanger;
        public Transform directionIndicator;
        public TextMeshProUGUI speedText;

        private int updateOffset;
        private void Start()
        {
            updateOffset = Random.Range(0, updateIntervalFrames);
        }

        public void Update()
        {
            if ((Time.renderedFrameCount + updateOffset) % updateIntervalFrames != 0 || !gameObject.activeInHierarchy || !windChanger) return;

            var windVector = windChanger.WindStrenth_3;
            var strength = windVector.magnitude * 1.944f;
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
        }
    }
}

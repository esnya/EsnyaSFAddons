using EsnyaAircraftAssets;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace EsnyaSFAddons
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GroundWindIndicator : UdonSharpBehaviour
    {
        public const int UPDATE_STEP_UPDATE = 0;
        public const int UPDATE_STEP_DIRECTION = 1;
        public const int UPDATE_STEP_CURRENT_DIRECTION = 2;
        public const int UPDATE_STEP_DIRECTION_TEXT = 3;
        public const int UPDATE_STEP_DIRECTION_RANGE = 4;
        public const int UPDATE_STEP_SPEED = 5;
        public const int UPDATE_STEP_CURRENT_SPEED = 6;
        public const int UPDATE_STEP_MIN_SPEED = 7;
        public const int UPDATE_STEP_MAX_SPEED = 8;
        public const int UPDATE_STEP_COUNT = 9;
        public int updateIntervalFrames = 10;

        [UdonSharpComponentInject] public SAV_WindChanger windChanger;
        public Transform directionIndicator;
        public Transform currentDirectionIndicator;
        public Image directionRangeImage;
        public TextMeshProUGUI directionText;
        public TextMeshProUGUI speedText, currentSpeedText, minSpeedText, maxSpeedText;

        private int updateOffset;
        private int updateStep;
        private Vector3 wind;
        private float gust;

        private Vector3 GetFinalWind()
        {
            var t = Time.time * windChanger.WindGustiness;
            var turbulanceScale = windChanger.WindTurbulanceScale;
            var gustx = t + transform.position.x * turbulanceScale;
            var gustz = t + transform.position.z * turbulanceScale;
            return wind + Vector3.Normalize(new Vector3(Mathf.PerlinNoise(gustx + 9000, gustz) - .5f, 0, Mathf.PerlinNoise(gustx, gustz + 9999) - .5f)) * gust;
        }

        private float GetHeding(Vector3 vector)
        {
            return (Vector3.SignedAngle(Vector3.forward, vector.normalized, Vector3.up) + 180.0f) % 360.0f;
        }

        private void OnEnable()
        {
            updateOffset = Random.Range(0, updateIntervalFrames);
        }

        public void Update()
        {
            if ((Time.renderedFrameCount + updateOffset) % updateIntervalFrames != 0 || !gameObject.activeInHierarchy || !windChanger) return;

            switch (updateStep)
            {
                case UPDATE_STEP_UPDATE:
                    wind = windChanger.WindStrenth_3 * 1.944f;
                    gust = windChanger.WindGustStrength * 1.944f;
                    break;
                case UPDATE_STEP_DIRECTION:
                    if (directionIndicator)
                    {
                        directionIndicator.localRotation = Quaternion.AngleAxis(GetHeding(wind), Vector3.back);
                    }
                    break;
                case UPDATE_STEP_CURRENT_DIRECTION:
                    if (currentDirectionIndicator)
                    {
                        if (gust < 0.5f)
                        {
                            currentDirectionIndicator.gameObject.SetActive(false);
                        }
                        else
                        {
                            currentDirectionIndicator.gameObject.SetActive(true);
                            currentDirectionIndicator.localRotation = Quaternion.AngleAxis(GetHeding(GetFinalWind()), Vector3.back);
                        }
                    }
                    break;
                case UPDATE_STEP_DIRECTION_TEXT:
                    if (directionText)
                    {
                        var heading = Mathf.RoundToInt(GetHeding(wind));
                        directionText.text = heading == 0 ? "360" : $"{heading:000}";
                    }
                    break;
                case UPDATE_STEP_DIRECTION_RANGE:
                    if (directionRangeImage)
                    {
                        var strength = wind.magnitude;
                        var range = Vector3.Angle(Vector3.forward, (Vector3.forward * strength + Vector3.right * gust).normalized);
                        directionRangeImage.transform.localRotation = Quaternion.AngleAxis(range, Vector3.forward);
                        directionRangeImage.fillAmount = range / 180.0f;
                    }
                    break;
                case UPDATE_STEP_SPEED:
                    if (speedText)
                    {
                        speedText.text = $"{wind.magnitude:0}";
                    }
                    break;
                case UPDATE_STEP_CURRENT_SPEED:
                    if (currentSpeedText)
                    {
                        currentSpeedText.text = $"{GetFinalWind().magnitude:0}";
                    }
                    break;
                case UPDATE_STEP_MIN_SPEED:
                    if (minSpeedText)
                    {
                        minSpeedText.text = $"{Mathf.Max(wind.magnitude - gust, 0.0f):0}";
                    }
                    break;
                case UPDATE_STEP_MAX_SPEED:

                    if (maxSpeedText)
                    {
                        maxSpeedText.text = $"{wind.magnitude + gust:0}";
                    }
                    break;
            }

            updateStep = (updateStep + 1) % UPDATE_STEP_COUNT;
        }
    }
}

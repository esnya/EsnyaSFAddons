using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace EsnyaSFAddons.Weather
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class RandomWindChanger : UdonSharpBehaviour
    {
        public bool randomWindOnStart = true;
        public float randomWindStrength = 5.0f;
        [NotNull] public AnimationCurve randomWindCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public float randomGustStrength = 2.0f;
        [NotNull] public AnimationCurve randomGustCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [NotNull] public SAV_WindChanger windChanger;

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (randomWindOnStart && player.isLocal && player.isMaster) SendCustomEventDelayedSeconds(nameof(_RandomWind), 20);
        }

        public override void Interact()
        {
            windChanger.SendCustomEvent("_onPickup");
            _RandomWind();
        }

        public void _RandomWind()
        {
            Networking.SetOwner(Networking.LocalPlayer, windChanger.gameObject);

            if (windChanger.SyncedWind) windChanger.SyncedWind = true;

            windChanger.UpdateValues();

            var windStrength = randomWindCurve.Evaluate(Random.value) * randomWindStrength;
            var gustStrength = randomGustCurve.Evaluate(Random.value) * randomGustStrength;
            var windDirection = Random.Range(0, 360);
            var wind = Quaternion.AngleAxis(windDirection, Vector3.up) * Vector3.forward * windStrength;

            Debug.Log($"[ESFA] Random wind: {windDirection:000} at {windStrength * 1.9444f} knots, gust at {gustStrength * 1.9444f} knots");
            windChanger.transform.rotation = Quaternion.FromToRotation(Vector3.forward, wind.normalized);
            windChanger.WindStrength = windStrength;
            windChanger.WindGustStrength = gustStrength;
            windChanger.RequestSerialization();
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void Reset()
        {
            randomWindCurve = new AnimationCurve(new [] {
                new Keyframe(0.0f, 0.0f, 3.0f, 3.0f),
                new Keyframe(1.0f, 1.0f, 3.0f, 3.0f),
            });
            randomGustCurve = new AnimationCurve(new [] {
                new Keyframe(0.0f, 0.0f, 3.0f, 3.0f),
                new Keyframe(1.0f, 1.0f, 3.0f, 3.0f),
            });
            windChanger = GetComponentInChildren<SAV_WindChanger>();
        }
#endif
    }
}

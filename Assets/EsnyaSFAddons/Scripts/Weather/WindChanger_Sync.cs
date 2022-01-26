using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace EsnyaAircraftAssets
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class WindChanger_Sync : UdonSharpBehaviour
    {
        public bool allowMaster = true, allowInstanceOwner = true, allowEveryone;

        public bool randomInitialWind;
        public float maxStrength = 6.0f, maxGustStrength = 1.5f;
        public float strengthCurve = 2, gustStrengthCurve = 0.5f;

        [UdonSynced] private float WindStrength, WindGustStrength, WindGustiness, WindTurbulanceScale;

        private SAV_WindChanger windChanger;

        private void Start()
        {
            windChanger = GetComponentInParent<SAV_WindChanger>();

            if (Networking.LocalPlayer.isMaster)
            {
                WindStrength = Mathf.Pow(UnityEngine.Random.value, 1.0f / strengthCurve) * maxStrength;
                WindGustStrength = Mathf.Pow(UnityEngine.Random.value, 1.0f / gustStrengthCurve) * maxGustStrength;
                windChanger.transform.rotation = Quaternion.AngleAxis(UnityEngine.Random.Range(0.0f, 360.0f), Vector3.up);
                OnDeserialization();
                RequestSerialization();
            }
        }

        public override void OnDeserialization()
        {
            windChanger.WindStrengthSlider.value = WindStrength;
            windChanger.WindGustStrengthSlider.value = WindGustStrength;
            windChanger.WindGustinessSlider.value = WindGustiness;
            windChanger.WindTurbulanceScaleSlider.value = WindTurbulanceScale;

            windChanger.WindApplySound.Play();
            Vector3 NewWind = windChanger.transform.rotation * Vector3.forward * WindStrength;
            foreach (UdonSharpBehaviour vehicle in windChanger.SaccAirVehicles)
            {
                if (vehicle)
                {
                    vehicle.SetProgramVariable("Wind", NewWind);
                    vehicle.SetProgramVariable("WindGustStrength", WindGustStrength);
                    vehicle.SetProgramVariable("WindGustiness", WindGustiness);
                    vehicle.SetProgramVariable("WindTurbulanceScale", WindTurbulanceScale);
                }
            }
        }

        private bool IsGranted()
        {
            var localPlayer = Networking.LocalPlayer;
            return allowEveryone || allowMaster && localPlayer.isMaster || allowInstanceOwner && localPlayer.isInstanceOwner;
        }

        public void _Sync()
        {
            if (!IsGranted()) return;

            var localPlayer = Networking.LocalPlayer;
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(localPlayer, gameObject);

            WindStrength = windChanger.WindStrengthSlider.value;
            WindGustStrength = windChanger.WindGustStrengthSlider.value;
            WindGustiness = windChanger.WindGustinessSlider.value;
            WindTurbulanceScale = windChanger.WindTurbulanceScaleSlider.value;

            RequestSerialization();
        }
    }
}

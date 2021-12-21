
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace EsnyaFactory
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class WindChanger_Sync : UdonSharpBehaviour
    {
        public bool allowMaster = true, allowInstanceOwner = true, allowEveryone;

        [UdonSynced] private float WindStrength, WindGustStrength, WindGustiness, WindTurbulanceScale;

        private SAV_WindChanger windChanger;

        private void Start()
        {
            windChanger = GetComponentInParent<SAV_WindChanger>();
        }

        public override void OnDeserialization()
        {
            windChanger.WindStrengthSlider.value = WindStrength;
            windChanger.WindGustStrengthSlider.value = WindGustStrength;
            windChanger.WindGustinessSlider.value = WindGustiness;
            windChanger.WindTurbulanceScaleSlider.value = WindTurbulanceScale;

            windChanger.WindApplySound.Play();
            Vector3 NewWindDir = windChanger.transform.rotation * Vector3.forward * WindStrength;
            foreach (UdonSharpBehaviour vehicle in windChanger.SaccAirVehicles)
            {
                if (vehicle)
                {
                    vehicle.SetProgramVariable("Wind", NewWindDir);
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

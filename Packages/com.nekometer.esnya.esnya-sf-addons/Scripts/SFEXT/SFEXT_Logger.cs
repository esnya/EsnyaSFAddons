using EsnyaSFAddons.Annotations;
using InariUdon.UI;
using UdonSharp;
using UnityEngine;

namespace EsnyaSFAddons.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_Logger : UdonSharpBehaviour
    {
        [UdonSharpComponentInject] public UdonLogger logger;

        private string vehicleName;
        private string pilotName;

        private void Start()
        {
            gameObject.SetActive(false);

        }

        public void SFEXT_G_PilotEnter()
        {
            var entity = GetComponentInParent<SaccEntity>();
            vehicleName = entity.gameObject.name;
            pilotName = entity.UsersName;
        }

        public void SFEXT_G_TakeOff() => Log("info", "Take Off");
        public void SFEXT_G_TouchDown() => Log("info", "Touch Down");
        public void SFEXT_G_TouchDownWater() => Log("info", "Touch Down Water");
        public void SFEXT_G_Explode()
        {
            Log("info", "Dead");
            pilotName = null;
        }

        public void SFEXT_G_LaunchFromCatapult() => Log("info", "Launch from Catapult");
        public void SFEXT_G_EngineOn() => Log("info", "Engine On");
        public void SFEXT_G_EngineOff() => Log("info", "Engine Off");
        public void SFEXT_G_NoFuel() => Log("info", "Bingo Fuel");

        private void Log(string level, string message)
        {
            if (pilotName == null) return;
            logger.Log(level, $"{pilotName}@{vehicleName}", message);
        }
    }
}

using UdonSharp;
using UnityEngine;

namespace EsnyaSFAddons.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_Warning : UdonSharpBehaviour
    {
        [Header("Master Caution")]
        public GameObject[] masterCautionLights = { };
        public GameObject[] engineCautionLights = { };
        public GameObject[] hydroCautionLights = { };
        public GameObject[] fuelCautionLihts = { };
        public GameObject[] engineOverheatLights = { };
        public GameObject[] apuCautionLight = { };


        [Header("Fire")]
        public GameObject[] engine1OverheatLights = { };
        public GameObject[] engine2OverheatLights = { };
        public GameObject[] engineFireLights = { };
        public GameObject[] engine1FireLights = { };
        public GameObject[] engine2FireLights = { };
        public AudioSource engineFireAlarm;

        private SaccEntity entity;
        private SaccAirVehicle airVehicle;
        private SFEXT_AdvancedEngine engine1, engine2;
        private SFEXT_AuxiliaryPowerUnit apu;
        private bool initialized, engine1Overheat, engine2Overheat, engine1Fire, engine2Fire, engine1Stall, engine2Stall, hydro1Low, hydro2Low;

        public void SFEXT_L_EntityStart()
        {
            entity = GetComponentInParent<SaccEntity>();
            airVehicle = entity.GetComponentInChildren<SaccAirVehicle>();
            apu = entity.GetComponentInChildren<SFEXT_AuxiliaryPowerUnit>(true);
            var engines = entity.gameObject.GetComponentsInChildren<SFEXT_AdvancedEngine>(true);
            if (engines.Length > 0) engine1 = engines[0];
            if (engines.Length > 1) engine2 = engines[1];

            gameObject.SetActive(false);

            initialized = true;
        }

        public void SFEXT_G_PilotEnter()
        {
            gameObject.SetActive(true);
        }
        public void SFEXT_G_PilotExit()
        {
            gameObject.SetActive(false);
            StopAlarm(engineFireAlarm);
        }

        // private bool isPilot;
        // public void SFEXT_O_PilotEnter()
        // {
        //     isPilot = true;
        // }
        // public void SFEXT_O_PilotExit()
        // {
        //     isPilot = false;
        // }

        private void Update()
        {
            if (!initialized) return;

            if (engine1)
            {
                engine1Overheat = engine1.overheat;
                UpdateWarning(engine1Overheat, engine1OverheatLights, null);

                engine1Fire = engine1.ect > engine1.fireECT;
                UpdateWarning(engine1Fire, engine1FireLights, null);

                engine1Stall = engine1.n1 < engine1.idleN1 * 0.9f;
                hydro1Low = engine1.n1 < engine1.idleN1 * 0.8f;
            }

            if (engine2)
            {
                engine2Overheat = engine2.overheat;
                UpdateWarning(engine2Overheat, engine2OverheatLights, null);

                engine2Fire = engine2.ect > engine2.fireECT;
                UpdateWarning(engine2Fire, engine2FireLights, null);

                engine2Stall = engine2.n1 < engine2.idleN1 * 0.9f;
                hydro2Low = engine2.n1 < engine2.idleN1 * 0.8f;
            }

            var engine = engine1Stall || engine2Stall;
            UpdateWarning(engine, engineCautionLights, null);

            var hydro = hydro1Low || hydro2Low;
            UpdateWarning(hydro, hydroCautionLights, null);

            var overheat = engine1Overheat || engine2Overheat;
            UpdateWarning(overheat, engineOverheatLights, null);

            var fire = engine1Fire || engine2Fire;
            UpdateWarning(fire, engineFireLights, engineFireAlarm);

            var fuelLow = airVehicle.Fuel / airVehicle.FullFuel < 0.3f;
            UpdateWarning(fuelLow, fuelCautionLihts, null);

            var apuOperating = apu != null && !apu.terminated;
            UpdateWarning(apuOperating, apuCautionLight, null);

            UpdateWarning(engine || hydro || overheat || fire || fuelLow || apuOperating, masterCautionLights, null);
        }

        private void UpdateWarning(bool state, GameObject[] lights, AudioSource alarm)
        {
            if (lights != null)
            {
                foreach (var light in lights)
                {
                    if (light && light.activeSelf != state) light.SetActive(state);
                }
            }

            if (alarm != null && alarm.isPlaying != state)
            {
                if (state) alarm.Play();
                else alarm.Stop();
            }
        }

        private void StopAlarm(AudioSource alarm)
        {
            if (alarm == null) return;
            alarm.Stop();
        }
    }
}

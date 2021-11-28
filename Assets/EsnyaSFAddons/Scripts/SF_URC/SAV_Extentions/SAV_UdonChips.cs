using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using InariUdon.UI;
using VRC.SDK3.Components;

#if ESFA_UCS
using UCS;
#endif

namespace EsnyaAircraftAssets
{
    [DefaultExecutionOrder(200)] // After SaccEntity/SaccAirVehicle/SFRuntimeSetup
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SAV_UdonChips : UdonSharpBehaviour
    {
        [Header("Weather Sources")]
        public FogController fogController;
        public Transform sunSource;

        [Header("Configurations")]
        public float altitudeThreshold = 60.0f;

        [Header("Rwards")]
        public float onKill = 2000.0f;
        public float onLanded = 500.0f, onResupplyed = 50.0f;
        public float onEscaped = -500.0f, onDead = -1000.0f;
        public float darkBonus = 500, fogBonus = 3000, fogBonusCurve = 2, fogMaxValue = 140;

#if ESFA_UCS
        private bool initialized = false;
        private UdonChips udonChips;
        private float maxAltitude;
        private SaccEntity entity;
        private SaccAirVehicle airVehicle;
        private Rigidbody vehicleRigidbody;
        private bool takeOff = false;

        private void Start()
        {
            udonChips = GameObject.Find("UdonChips").GetComponent<UdonChips>();

            entity = GetComponentInParent<SaccEntity>();
            airVehicle = entity.GetComponentInChildren<SaccAirVehicle>();
            vehicleRigidbody = entity.GetComponent<Rigidbody>();

            maxAltitude = vehicleRigidbody.position.y;

            initialized = true;
        }

        private void Update()
        {
            if (!initialized) return;
            var position = vehicleRigidbody.position;
            maxAltitude = Mathf.Max(maxAltitude, position.y);
        }

        public void SFEXT_O_PilotEnter()
        {
            gameObject.SetActive(true);
            if (initialized) maxAltitude = vehicleRigidbody.position.y;
        }

        public void SFEXT_O_PilotExit()
        {
            if (!initialized) return;
            if (!entity.dead && takeOff)
            {
                AddMoney(onEscaped, "Escaped");
            }

            gameObject.SetActive(false);
        }

        public void SFEXT_G_Dead()
        {
            if (!initialized) return;
            if (airVehicle.Piloting) AddMoney(onDead, "Dead");
        }

        public void SFEXT_G_TakeOff()
        {
            takeOff = true;
            if (initialized) maxAltitude = vehicleRigidbody.position.y;
        }

        public void SFEXT_G_TouchDown()
        {
            if (!initialized) return;
            if (airVehicle.Piloting)
            {
                if (maxAltitude - airVehicle.SeaLevel > altitudeThreshold)
                {
                    AddMoney(onLanded * Mathf.Pow(airVehicle.Health / airVehicle.FullHealth, 2.0f), "Landed");
                    if (GetIsDark()) AddMoney(darkBonus, "Night Bonus");
                    if (fogController)
                    {
                        var normalizedFogStrength = (fogController.FogStrength - fogController.MinStrength) / (fogController.MaxStrength - fogController.MinStrength);
                        var calculatedFogBonus = Mathf.Floor(Mathf.Pow(normalizedFogStrength, fogBonusCurve) * fogBonus);
                        if (calculatedFogBonus > 0) AddMoney(fogBonus, "Weather Bonus");
                    }
                }
            }

            takeOff = false;
        }

        public void SFEXT_G_TouchDownWater() => SFEXT_G_TouchDown();

        public void SFEXT_O_GotAKill()
        {
            if (!initialized) return;
            AddMoney(onKill, "Got a Kill");
        }

        private void AddMoney(float value, string reason)
        {
            Log("Info", $"{value:#.##} ({reason})");
            udonChips.money += value;
        }

        private bool GetIsDark()
        {
            if (sunSource == null) return false;
            return Vector3.Dot(sunSource.forward, Vector3.up) > 0;
        }
#endif

        [Header("Logger")]
        public UdonLogger logger;
        private void Log(string level, string log)
        {
            if (logger == null) Debug.Log(log);
            else logger.Log(level, entity.name, log);
        }
    }
}

using System;
using EsnyaSFAddons.Annotations;
using EsnyaSFAddons.Weather;
using InariUdon.UI;
using SaccFlightAndVehicles;
using UCS;
using UdonSharp;
using UnityEngine;

namespace EsnyaSFAddons.UCS
{
    [DefaultExecutionOrder(200)] // After SaccEntity/SaccAirVehicle/SFRuntimeSetup
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_UdonChips : UdonSharpBehaviour
    {
        [Header("Killed")]
        public float onKilled = 2000.0f;

        [Header("Dead")]
        public float onEscaped = -500.0f, onDead = -1000.0f;

        [Header("Maintenance")]
        public float onResupply = 50.0f;

        [Header("Landing")]
        public float onLanded = 500.0f, healthCurve = 2;
        public float darkBonus = 500;
        public float fogBonus = 3000, fogBonusCurve = 2, fogMaxValue = 140;
        public float windBonus = 3000, windBonusCurve = 2, windMaxStrength = 50;
        public float gustBonus = 6000, gustBonusCurve = 2, gustMaxStrength = 50;
        public float altitudeThreshold = 60.0f;

        [NonSerialized] public SaccEntity EntityControl;
        [HideInInspector][UdonSharpComponentInject] public FogController fogController;

        private SaccAirVehicle airVehicle;
        private float maxAltitude;
        private Rigidbody vehicleRigidbody;
        private UdonChips ucs;
        private UdonLogger logger;

        private void Start()
        {
            gameObject.SetActive(false);
            ucs = UdonChips.GetInstance();
            logger = UdonLogger.GetInstance();
        }

        private void Update()
        {
            if (!vehicleRigidbody) return;
            var position = vehicleRigidbody.position;
            maxAltitude = Mathf.Max(maxAltitude, position.y);
        }

        public void SFEXT_L_EntityStart()
        {
            vehicleRigidbody = EntityControl.GetComponent<Rigidbody>();
            airVehicle = (SaccAirVehicle)EntityControl.GetExtention(GetUdonTypeName<SaccAirVehicle>());
            maxAltitude = vehicleRigidbody.position.y;
        }

        public void SFEXT_G_Dead()
        {
            if (airVehicle && airVehicle.Piloting) AddMoney(onDead, "Dead");
        }

        public void SFEXT_G_Explode()
        {
            if (IsKilled())
            {
                AddMoney(onKilled, $"Kill");
            }
        }


        public void SFEXT_O_PilotEnter()
        {
            gameObject.SetActive(true);
            maxAltitude = vehicleRigidbody.position.y;
        }

        public void SFEXT_O_PilotExit()
        {
            gameObject.SetActive(false);
            if (!EntityControl.dead && !airVehicle.Taxiing) AddMoney(onEscaped, "Escaped");
        }

        public void SFEXT_G_TakeOff()
        {
            maxAltitude = vehicleRigidbody.position.y;
        }

        private bool GetIsDark()
        {
            var sun = RenderSettings.sun;
            if (!sun) return false;
            return Vector3.Dot(sun.transform.forward, Vector3.up) > 0;
        }

        private float GetFog(float curve)
        {
            if (!fogController) return 0;
            return Mathf.Pow(fogController.NormalizedStrength, curve);
        }

        private float CurvedRemap(float value, float max, float curve)
        {
            return Mathf.Pow(Mathf.Clamp01(value / max), curve);
        }


        public void SFEXT_G_TouchDown()
        {
            if (!airVehicle.Piloting || maxAltitude - airVehicle.SeaLevel < altitudeThreshold) return;

            var baseReward = onLanded * Mathf.Pow(airVehicle.Health / airVehicle.FullHealth, healthCurve);
            if (baseReward < 1.0f) return;
            AddMoney(baseReward, "Landed");

            if (GetIsDark()) AddMoney(darkBonus, "Night Bonus");

            var fogBonus = GetFog(fogBonusCurve);
            if (fogBonus >= 1.0f) AddMoney(fogBonus, "Fog Bonus");

            var remappedWindBonus = CurvedRemap(airVehicle.Wind.sqrMagnitude, windMaxStrength, windBonusCurve * 0.5f) * windBonus;
            if (remappedWindBonus > 1.0f) AddMoney(remappedWindBonus, "Wind Bonus");

            var remappedGustBonus = CurvedRemap(airVehicle.WindGustStrength, gustMaxStrength, gustBonusCurve) * gustBonus;
            if (remappedGustBonus > 1.0f) AddMoney(remappedGustBonus, "Gust Bonus");
        }

        public void SFEXT_O_ReSupply()
        {
            AddMoney(onResupply, "Resupply");
        }


        private bool IsKilled()
        {
            return EntityControl.LastAttacker && EntityControl.LastAttacker.Using && !airVehicle.Taxiing && (airVehicle.Occupied || Time.time - Mathf.Min(airVehicle.LastHitTime, EntityControl.PilotExitTime) < 5);
        }

        public void AddMoney(float value, string reason)
        {
            if (ucs)
            {
                ucs.money += value;
            }

            Log("Info", $"{value:#.##} ({reason})");
        }

        public void Log(string level, string log)
        {

            if (logger) logger.Log(level, EntityControl.gameObject.name, log);
        }
    }
}

using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using InariUdon.UI;
using VRC.SDK3.Components;

#if ESFA && ESFA_UCS
using UCS;
#endif

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UdonSharpEditor;
#endif

namespace EsnyaAircraftAssets
{
    [
        DefaultExecutionOrder(100), // After Engine Controller
        UdonBehaviourSyncMode(/*BehaviourSyncMode.None*/ BehaviourSyncMode.NoVariableSync),
    ]
    public class SFUdonChips : UdonSharpBehaviour
    {
#if ESFA && ESFA_UCS
        [Header("EngineController Detection")]
        public LayerMask planeLayers = 1 << 17;
        public float searchRadius = 100000;

        [Header("Weather Sources")]
        public UdonSharpBehaviour fogSource;
        public string fogVariableName;
        public Transform sunSource;


        [Header("Configurations")]
        public float altitudeThreshold = 60.0f;

        [Header("Rwards")]
        public float onKill = 2000.0f;
        public float onLanded = 500.0f, onResupplyed = 50.0f;
        public float onEscaped = -500.0f, onDead = -1000.0f;
        public float darkBonus = 500, fogBonus = 3000, fogBonusCurve = 2, fogMaxValue = 140;

        private EngineController[] engineControllers;
        private EngineController pilotingController;
        private Scoreboard_Kills scoreboard;
        private UdonChips udonChips;
        private float seaLevel, fullFuel, landedFogLevel;
        private int prevFuel;
        private bool landedInDark, prevTaxiing;

        private int targetCount;
        private float maxAltitude;
        private bool landed;

        private bool ContainsUntilNull(object[] array, object value)
        {
            foreach (var item in array)
            {
                if (item == null) return false;
                if (item == value) return true;
            }
            return false;
        }

        private object[] Take(object[] array, int length)
        {
            if (array == null) return null;

            length = Mathf.Min(array.Length, length);

            var resized = new object[length];
            Array.Copy(array, resized, length);

            return resized;
        }

        private void Start()
        {
            udonChips = GameObject.Find("UdonChips").GetComponent<UdonChips>();

            var colliders = Physics.OverlapSphere(transform.position, 100000, planeLayers, QueryTriggerInteraction.Ignore);

            engineControllers = new EngineController[colliders.Length];
            targetCount = 0;
            foreach (var collider in colliders)
            {
                if (collider == null) continue;

                var sync = collider.GetComponentInParent(typeof(VRCObjectSync));
                if (sync == null) continue;

                var engineController = sync.GetComponentInChildren<EngineController>();
                if (engineController == null || ContainsUntilNull(engineControllers, engineController)) continue;

                engineControllers[targetCount++] = engineController;
            }
            engineControllers = (EngineController[])Take(engineControllers, targetCount);

            Log("Info", $"Initialized (tracking {targetCount} vehicles)");
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

        private float GetFogLevel()
        {
            if (fogSource == null) return 0;
            var value = (float)fogSource.GetProgramVariable(fogVariableName);
            return Mathf.Clamp01(value / fogMaxValue);
        }

        private bool GetIsFullHealth()
        {
            var health = pilotingController.Health;
            var fullHealth = pilotingController.FullHealth;
            return health == fullHealth;
        }

        private void OnEntered(EngineController engineController)
        {
            pilotingController = engineController;

            fullFuel = pilotingController.FullFuel;
            seaLevel = pilotingController.SeaLevel;
            scoreboard = pilotingController.KillsBoard;
        }
        private void OnExited()
        {
            if (scoreboard != null)
            {
                var kills = scoreboard.MyKills;
                for (int i = 0; i < kills; i++) AddMoney(onKill, "Kill");
            }

            if (pilotingController.dead)
            {
                AddMoney(onDead, "Dead");
            }
            else if (!pilotingController.Taxiing && pilotingController.HasGear)
            {
                AddMoney(onEscaped, "Escaped");
            }

            pilotingController = null;
            maxAltitude = 0;
        }

        private void OnLanded()
        {
            if (maxAltitude - seaLevel < altitudeThreshold) return;

            landed = true;
            maxAltitude = 0;

            if (GetIsFullHealth())
            {
                landedInDark = GetIsDark();
                landedFogLevel = GetFogLevel();
                Log("Debug", $"landed with full health (dark: {landedInDark}, fog: {landedFogLevel})");
            }
        }

        private void OnResupplied()
        {
            AddMoney(onResupplyed, "Resupplied");
            if (landed)
            {
                AddMoney(onLanded, "Landed");
                if (landedInDark) AddMoney(darkBonus, "Night Bonus");
                var calculatedFogBonus = Mathf.Floor(Mathf.Pow(landedFogLevel, fogBonusCurve) * fogBonus);
                if (calculatedFogBonus > 0) AddMoney(fogBonus, "Weather Bonus");

                landedInDark = false;
                landedFogLevel = 0;
                landed = false;
                maxAltitude = 0;
            }
        }

        private void Update()
        {
            if (pilotingController == null)
            {
                var engineController = engineControllers[Time.frameCount % targetCount];
                if (engineController.Piloting) OnEntered(engineController);
            }

            if (pilotingController != null)
            {
                if (!pilotingController.Piloting) OnExited();
                else
                {
                    maxAltitude = Mathf.Max(maxAltitude, Networking.LocalPlayer.GetPosition().y);

                    var fuel = Mathf.CeilToInt(pilotingController.Fuel);
                    if (fuel == fullFuel && prevFuel < fuel) OnResupplied();
                    prevFuel = fuel;

                    var taxiing = pilotingController.Taxiing;
                    if (!prevTaxiing && taxiing) OnLanded();
                    prevTaxiing = taxiing;
                }
            }
        }

        [Header("Logger")]
        public UdonLogger logger;
        private void Log(string level, string log)
        {
            if (logger == null) Debug.Log(log);
            else logger.Log(level, gameObject.name, log);
        }
#endif
    }
}
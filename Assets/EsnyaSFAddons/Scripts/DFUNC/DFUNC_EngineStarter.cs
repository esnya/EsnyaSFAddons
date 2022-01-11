
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace EsnyaAircraftAssets
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_EngineStarter : UdonSharpBehaviour
    {
        public const byte EngineStarting = 1;
        public const byte EngineStarted = 2;
        public const byte EngineStopping = 3;
        public const byte EngineStopped = 4;

        public KeyCode engineStartKey = KeyCode.LeftShift;
        public KeyCode engineStopKey = KeyCode.None;
        public Animator engineStarterAnimator;
        public string parameterName = "enginestarted";
        public GameObject Dial_Funcon;
        public AudioSource audioSource;
        public AudioClip engineStart, engineStop;
        public float engineStartDuration = 4.0f, engineStopDuration = 4.0f;
        public float throttleStrengthTransitionDuration = 1.0f;

        private SaccEntity entity;
        private SaccAirVehicle airVehicle;
        private SAV_SoundController soundController;
        private float throttleStrength;
        private AudioSource[] engineSounds;
        private bool useLeftTrigger, previousTrigger, selected, isPilot, isOccupied;
        private float stateChangedTime;
        private bool disableTaxiRotation;
        [UdonSynced] [FieldChangeCallback(nameof(EngineState))] private byte _engineState;
        public byte EngineState
        {
            private set
            {
                var startingOrStarted = value == EngineStarted || value == EngineStarting;
                if (value != _engineState)
                {
                    MuteEngineSounds(value != EngineStarted);
                    if (value == EngineStarting) PlayOneShot(engineStart);
                    else if (value == EngineStopping) PlayOneShot(engineStop);

                    airVehicle.ThrottleStrength = value == EngineStarted || value == EngineStopping ? throttleStrength : 0;

                    stateChangedTime = Time.time;
                }

                if (!disableTaxiRotation)
                {
                    if (value == EngineStopped) airVehicle.DisableTaxiRotation = 1;
                    else if (value == EngineStarting) airVehicle.DisableTaxiRotation = 0;
                }

                if (Dial_Funcon) Dial_Funcon.SetActive(startingOrStarted);

                if (engineStarterAnimator) engineStarterAnimator.SetBool(parameterName, startingOrStarted);

                if (value == EngineStopped && !isOccupied) gameObject.SetActive(false);

                _engineState = value;
            }
            get => _engineState;
        }

        public void DFUNC_LeftDial() { useLeftTrigger = true; }
        public void DFUNC_RightDial() { useLeftTrigger = false; }

        public void SFEXT_L_EntityStart()
        {
            entity = GetComponentInParent<SaccEntity>();
            airVehicle = entity.GetComponentInChildren<SaccAirVehicle>(true);
            soundController = entity.GetComponentInChildren<SAV_SoundController>(true);

            if (engineStarterAnimator == null) engineStarterAnimator = airVehicle.VehicleAnimator;

            engineSounds = new AudioSource[soundController.Thrust.Length + soundController.PlaneIdle.Length + 4];
            engineSounds[0] = soundController.PlaneInside;
            engineSounds[1] = soundController.PlaneDistant;
            engineSounds[2] = soundController.ABOnInside;
            engineSounds[3] = soundController.ABOnOutside;
            for (var i = 0; i < soundController.Thrust.Length; i++) engineSounds[i + 4] = soundController.Thrust[i];
            for (var i = 0; i < soundController.PlaneIdle.Length; i++) engineSounds[i + 4 + soundController.Thrust.Length] = soundController.PlaneIdle[i];

            throttleStrength = airVehicle.ThrottleStrength;

            disableTaxiRotation = airVehicle.DisableTaxiRotation != 0;

            EngineState = EngineStopped;
        }

        public void SFEXT_G_Explode()
        {
            EngineState = EngineStopped;
        }

        public void SFEXT_O_RespawnButton()
        {
            EngineState = EngineStopped;
            RequestSerialization();
        }

        public void DFUNC_Selected()
        {
            previousTrigger = true;
            selected = true;
        }

        public void DFUNC_Deselected()
        {
            selected = false;
        }

        public void SFEXT_G_PilotEnter()
        {
            isOccupied = true;
            gameObject.SetActive(true);
        }

        public void SFEXT_G_PilotExit()
        {
            isOccupied = false;
        }

        public void SFEXT_O_PilotEnter()
        {
            EngineState = EngineStopped;
            isPilot = true;
            RequestSerialization();
        }

        public void SFEXT_O_PilotExit()
        {
            if (EngineState != EngineStopped)
            {
                EngineState = EngineStopping;
                RequestSerialization();
            }

            isPilot = false;
        }

        private float GetInput()
        {
            if (EngineState == EngineStarting || EngineState == EngineStopping) return 0.0f;
            if (Input.GetKey(engineStartKey) && EngineState == EngineStopped) return 1.0f;
            if (Input.GetKeyDown(engineStopKey) && EngineState == EngineStarted && airVehicle.ThrottleInput < 0.001f) return 1.0f;

            if (!selected) return 0;
            if (useLeftTrigger) return Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
            return Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
        }

        private void Update()
        {
            var time = Time.time;
            var t = time - stateChangedTime;
            if (EngineState == EngineStarting)
            {
                var throttleStrengthMulti = Mathf.Clamp01((t - engineStartDuration + throttleStrengthTransitionDuration) / throttleStrengthTransitionDuration);
                airVehicle.ThrottleStrength = throttleStrength * throttleStrengthMulti;
                if (t >= engineStartDuration && isPilot)
                {
                    EngineState = EngineStarted;
                    RequestSerialization();
                }
            }
            else if (EngineState == EngineStopping)
            {
                var throttleStrengthMulti = Mathf.Clamp01((throttleStrengthTransitionDuration - t) / throttleStrengthTransitionDuration);
                airVehicle.ThrottleStrength = throttleStrength * throttleStrengthMulti;
                if (t >= engineStopDuration && isPilot)
                {
                    EngineState = EngineStopped;
                    RequestSerialization();
                }
            }

            if (isPilot)
            {
                var trigger = GetInput() > 0.75f;
                if (trigger && !previousTrigger) ToggleEngine();
                previousTrigger = trigger;

                if (Input.GetKeyDown(engineStartKey)) StartEngine();
                if (Input.GetKeyDown(engineStopKey) && airVehicle.ThrottleInput < 0.001f) StopEngine();
            }
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (audioSource && clip) audioSource.PlayOneShot(clip);
        }

        private void MuteEngineSounds(bool value)
        {
            foreach (var engineSound in engineSounds)
            {
                if (engineSound) engineSound.mute = value;
            }
        }

        private void StartEngine()
        {
            if (EngineState != EngineStopped) return;
            EngineState = EngineStarting;
            RequestSerialization();
        }

        private void StopEngine()
        {
            if (EngineState != EngineStarted) return;
            EngineState = EngineStopping;
            RequestSerialization();
        }

        private void ToggleEngine()
        {
            if (EngineState == EngineStopped) StartEngine();
            else if (EngineState == EngineStarted) StopEngine();
        }
    }
}

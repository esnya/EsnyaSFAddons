
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
        public KeyCode keyboardControl = KeyCode.LeftShift;
        [Tooltip("SAVControl.VehicleAnimator when null")] public Animator engineStarterAnimator;
        public string parameterName = "enginestarted";
        public GameObject Dial_Funcon;
        // public AudioSource engineStart;

        private SaccEntity entity;
        private SaccAirVehicle airVehicle;
        private SAV_SoundController soundController;
        private float throttleStrength;
        private AudioSource[] engineSounds;
        private bool useLeftTrigger, previousTrigger, selected, isOwner, isPilot;
        [UdonSynced] [FieldChangeCallback(nameof(EngineStarted))] private bool _engineStarted;
        public bool EngineStarted
        {
            private set
            {
                _engineStarted = value;

                if (value)
                {
                    if (Networking.IsOwner(gameObject)) entity.SendEventToExtensions("SFEXT_O_StartEngine");
                    airVehicle.ThrottleStrength = throttleStrength;
                }
                else
                {
                    if (Networking.IsOwner(gameObject)) entity.SendEventToExtensions("SFEXT_O_StopEngine");
                    airVehicle.ThrottleStrength = 0.0f;
                }

                if (Dial_Funcon) Dial_Funcon.SetActive(value);

                foreach (var engineSound in engineSounds) engineSound.mute = !value;

                engineStarterAnimator.SetBool(parameterName, value);
            }
            get => _engineStarted;
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

        public void SFEXT_G_PilotEnter() => gameObject.SetActive(true);

        public void SFEXT_G_PilotExit()
        {
            EngineStarted = false;
            gameObject.SetActive(false);
        }

        public void SFEXT_O_PilotEnter()
        {
            EngineStarted = false;
            RequestSerialization();
            previousTrigger = true;
            isPilot = true;
        }

        public void SFEXT_O_PilotExit()
        {
            EngineStarted = false;
            RequestSerialization();
            isPilot = false;
        }

        private float GetInput()
        {
            if (!EngineStarted && Input.GetKey(keyboardControl)) return 1.0f;

            if (!selected) return 0;
            if (useLeftTrigger) return Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
            return Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
        }

        private void Update()
        {
            if (!isPilot) return;

            var trigger = GetInput() > 0.75f;
            if (trigger && !previousTrigger)
            {
                EngineStarted = !EngineStarted;
                RequestSerialization();
            }
            previousTrigger = trigger;
        }
    }
}

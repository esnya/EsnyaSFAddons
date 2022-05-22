using System;
using System.Threading;
using UdonSharp;
using UnityEngine;

namespace EsnyaSFAddons
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SFEXT_AuxiliaryPowerUnit : UdonSharpBehaviour
    {
        public AudioSource apuAudioSource;
        public AudioClip apuStart, apuLoop, apuStop;
        [Tooltip("[s]")] public float crossFadeDuration = 3.0f;
        [Tooltip("[s]")] public float defaultApuStartDuration = 30.0f;
        [Tooltip("[s]")] public float defaultApuStopDuration = 10.0f;

        public ParticleSystem exhaustEffect;

        [NonSerialized] public bool started, terminated;
        [UdonSynced] private bool run;

        private AudioSource apuOneshotAudioSource;
        private bool initialized;
        public void SFEXT_L_EntityStart()
        {
            if (apuAudioSource)
            {
                apuAudioSource.playOnAwake = false;
                apuAudioSource.loop = true;
                apuAudioSource.clip = apuLoop;

                apuOneshotAudioSource = VRCInstantiate(apuAudioSource.gameObject).GetComponent<AudioSource>();
                apuOneshotAudioSource.transform.SetParent(apuAudioSource.transform);
                apuOneshotAudioSource.transform.position = apuAudioSource.transform.position;
            }

            LoadAudioClip(apuStart);
            LoadAudioClip(apuLoop);
            LoadAudioClip(apuStop);

            ResetStatus();

            gameObject.SetActive(false);
            initialized = true;
        }

        private bool isOwner;
        public void SFEXT_O_PilotEnter() => isOwner = true;
        public bool SFEXT_O_TakeOwnership() => isOwner = true;
        public bool SFEXT_O_LoseOwnership() => isOwner = false;

        private bool hasPilot;
        public void SFEXT_G_PilotEnter()
        {
            hasPilot = true;
            gameObject.SetActive(true);
        }
        public void SFEXT_G_PilotExit() => hasPilot = false;
        public void SFEXT_G_RespawnButton() => ResetStatus();
        public void SFEXT_G_Explode() => ResetStatus();

        private bool prevRun;
        private float stateChangedTime;
        private void Update()
        {
            if (!initialized) return;

            if (run != prevRun)
            {
                prevRun = run;
                stateChangedTime = Time.time;
                if (run) OnStart();
                else OnShutdown();
            }

            var stateTime = Time.time - stateChangedTime;
            if (run)
            {
                if (!started) OnStarting(stateTime);
            }
            else OnShuttingDown(stateTime);

            if (!hasPilot && terminated) gameObject.SetActive(false);
        }

        public void StartAPU()
        {
            if (!run) ToggleAPU();
        }
        public void StopAPU()
        {
            if (run) ToggleAPU();
        }
        public void ToggleAPU()
        {
            run = !run;
            RequestSerialization();
        }

        private void ResetStatus()
        {
            run = false;
            started = false;
            terminated = true;
        }

        private void OnStart()
        {
            terminated = false;
            started = false;
            PlayOneShot(apuOneshotAudioSource, apuStart);
            SetParticleEmission(exhaustEffect, true);
        }

        private void OnStarting(float stateTime)
        {
            var loopVolume = Mathf.Clamp01(stateTime - GetAudioClipLength(apuStart, defaultApuStartDuration) + crossFadeDuration);
            SetVolume(apuAudioSource, loopVolume);
            if (isOwner && Mathf.Approximately(loopVolume, 1.0f)) OnStarted();
        }

        private void OnStarted()
        {
            started = true;
            terminated = false;
        }

        private void OnShutdown()
        {
            terminated = false;
            started = false;
            PlayOneShot(apuOneshotAudioSource, apuStop);
        }

        private void OnShuttingDown(float stateTime)
        {
            var loopVolume = Mathf.Clamp01(crossFadeDuration - stateTime);
            SetVolume(apuAudioSource, loopVolume);
            if (Mathf.Approximately(loopVolume, 0.0f)) OnTerminated();
        }

        private void OnTerminated()
        {
            started = false;
            terminated = true;

            SetParticleEmission(exhaustEffect, false);
        }

        #region Utilities
        private void LoadAudioClip(AudioClip clip)
        {
            if (clip) clip.LoadAudioData();
        }
        private float GetAudioClipLength(AudioClip clip, float defaultLength)
        {
            return clip ? clip.length : defaultLength;
        }
        private void PlayOneShot(AudioSource audioSource, AudioClip clip)
        {
            if (audioSource && clip)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void SetVolume(AudioSource audioSource, float volume)
        {
            if (audioSource)
            {
                var play = !Mathf.Approximately(volume, 0);
                if (audioSource.isPlaying != play)
                {
                    if (play) audioSource.Play();
                    else audioSource.Stop();
                }

                if (audioSource.volume != volume) audioSource.volume = volume;
            }
        }

        private void SetParticleEmission(ParticleSystem system, bool value)
        {
            if (!system) return;
            var emission = system.emission;
            if (emission.enabled != value) emission.enabled = value;
        }
        #endregion
    }
}

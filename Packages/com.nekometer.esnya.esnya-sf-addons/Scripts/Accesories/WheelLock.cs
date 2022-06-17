
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace EsnyaSFAddons
{
    /// <summary>
    /// Lock vehicle
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [RequireComponent(typeof(MeshRenderer))]
    [DefaultExecutionOrder(1000)] // After EngineController
    public class WheelLock : UdonSharpBehaviour
    {
        public Animator animator;
        public bool defaultLocked = true;
        public string parameterName = "locked";

        private MeshRenderer visual;

        [UdonSynced, FieldChangeCallback(nameof(Locked))] private bool _locked;
        private bool Locked {
            set {
                _locked = value;
                visual.enabled = value;
                animator.SetBool(parameterName, value);
            }
            get => _locked;
        }

        private void Start()
        {
            visual = GetComponent<MeshRenderer>();
            Locked = defaultLocked;
        }

        public override void Interact() => _ToggleLock();

        public void _SetLocked(bool value)
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            Locked = value;
            RequestSerialization();
        }

        public void _Lock() => _SetLocked(false);
        public void _Unlock() => _SetLocked(false);
        public void _ToggleLock() => _SetLocked(!Locked);
    }
}

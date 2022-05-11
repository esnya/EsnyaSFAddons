
using UdonSharp;
using UdonToolkit;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace EsnyaSFAddons
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_CustomEvent : UdonSharpBehaviour
    {
        public UdonSharpBehaviour target;
        [Popup("@target", "behaviour")] public string eventName;
        public bool networked;
        [HideIf("@!networked")] public NetworkEventTarget networkEventTarget;
        // private string triggerAxis;
        // private bool triggerLastFrame;

        // public void DFUNC_LeftDial() => triggerAxis = "Oculus_CrossPlatform_PrimaryIndexTrigger";
        // public void DFUNC_RightDial() => triggerAxis = "Oculus_CrossPlatform_SecondaryIndexTrigger";
        public void DFUNC_Selected()
        {
            // triggerLastFrame = true;
            gameObject.SetActive(true);
        }
        public void DFUNC_Deselected()
        {
            gameObject.SetActive(false);
        }
        public void DFUNC_TriggerPress() => _SendEvent();

        public void SFEXT_L_EntityStart()
        {
            gameObject.SetActive(false);
        }

        public void SFEXT_O_PilotExit() => gameObject.SetActive(false);

        public void KeyboardInput() => _SendEvent();

        public void _SendEvent()
        {
            if (!target) return;

            if (networked) target.SendCustomNetworkEvent(networkEventTarget, eventName);
            else target.SendCustomEvent(eventName);
        }

        // private void Update()
        // {
        //     var trigger = Input.GetAxisRaw(triggerAxis) > 0.75f;
        //     if (trigger && !triggerLastFrame) _SendEvent();
        //     triggerLastFrame = trigger;
        // }

    }
}

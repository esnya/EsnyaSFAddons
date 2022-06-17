
using UdonSharp;
using UdonToolkit;
using VRC.Udon.Common.Interfaces;

namespace EsnyaSFAddons.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_SendCustomEvent : UdonSharpBehaviour
    {
        public UdonSharpBehaviour target;
        public bool networked;
        [HideIf("@!networked")] public NetworkEventTarget networkEventTarget;
        public bool sendOnTriggerPress;
        [HideIf("@!sendOnTriggerPress")][Popup("behaviour", "@target")] public string onTriggerPress;
        public bool sendOnTriggerRelease;
        [HideIf("@!sendOnTriggerRelease")][Popup("behaviour", "@target")] public string onTriggerRelease;
        public bool sendOnKeyboardInput;
        [HideIf("@!sendOnKeyboardInput")][Popup("behaviour", "@target")] public string onKeyboardInput;

        public void DFUNC_Selected()
        {
            gameObject.SetActive(true);
        }
        public void DFUNC_Deselected()
        {
            gameObject.SetActive(false);
        }
        public void DFUNC_TriggerPress()
        {
            if (sendOnTriggerPress) _SendEvent(onTriggerPress);
        }
        public void DFUNC_TriggerRelease()
        {
            if (sendOnTriggerRelease) _SendEvent(onTriggerRelease);
        }
        public void KeyboardInput()
        {
            if (sendOnKeyboardInput) _SendEvent(onKeyboardInput);
        }


        public void SFEXT_L_EntityStart()
        {
            gameObject.SetActive(false);
        }

        public void SFEXT_O_PilotExit() => gameObject.SetActive(false);
        public void _SendEvent(string eventName)
        {
            if (!target) return;

            if (networked) target.SendCustomNetworkEvent(networkEventTarget, eventName);
            else target.SendCustomEvent(eventName);
        }
    }
}


using UdonSharp;
using UdonToolkit;
using VRC.Udon.Common.Interfaces;

namespace EsnyaSFAddons.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_SendCustomEvent : DFUNC_Base
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

        public override void DFUNC_TriggerPressed()
        {
            if (sendOnTriggerPress) _SendEvent(onTriggerPress);
        }
        public override void DFUNC_TriggerReleased()
        {
            if (sendOnTriggerRelease) _SendEvent(onTriggerRelease);
        }
        public void KeyboardInput()
        {
            if (sendOnKeyboardInput) _SendEvent(onKeyboardInput);
        }

        public void _SendEvent(string eventName)
        {
            if (!target) return;

            if (networked) target.SendCustomNetworkEvent(networkEventTarget, eventName);
            else target.SendCustomEvent(eventName);
        }
    }
}

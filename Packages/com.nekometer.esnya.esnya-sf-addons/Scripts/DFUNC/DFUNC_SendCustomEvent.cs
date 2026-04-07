
using UdonSharp;
using VRC.Udon.Common.Interfaces;

namespace EsnyaSFAddons.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_SendCustomEvent : DFUNC_Base
    {
        public UdonSharpBehaviour target;
        public bool networked;
        public NetworkEventTarget networkEventTarget;
        public bool sendOnTriggerPress;
        public string onTriggerPress;
        public bool sendOnTriggerRelease;
        public string onTriggerRelease;
        public bool sendOnKeyboardInput;
        public string onKeyboardInput;

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

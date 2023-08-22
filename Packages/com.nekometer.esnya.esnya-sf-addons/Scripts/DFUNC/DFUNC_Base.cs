using UdonSharp;
using VRC.SDKBase;
using UnityEngine;

namespace EsnyaSFAddons
{
    public class DFUNC_Base : UdonSharpBehaviour
    {
        public virtual void DFUNC_TriggerPressed() {}
        public virtual void DFUNC_TriggerReleased() {}

        private string triggerAxis;
        private bool triggerBoolLastFrame = false;
        protected bool isSelected;
        protected virtual bool ActivateOnSelected => true;

        public void DFUNC_LeftDial()
        {
            triggerAxis = "Oculus_CrossPlatform_PrimaryIndexTrigger";
        }

        public void DFUNC_RightDial()
        {
            triggerAxis = "Oculus_CrossPlatform_SecondaryIndexTrigger";
        }

        public virtual void SFEXT_L_EntityStart() => DFUNC_Deselected();
        public virtual void SFEXT_O_PilotEnter() => DFUNC_Deselected();
        public virtual void SFEXT_O_PilotExit() => DFUNC_Deselected();

        public virtual void DFUNC_Selected()
        {
            isSelected = true;
            if (ActivateOnSelected) gameObject.SetActive(true);
        }

        public virtual void DFUNC_Deselected()
        {
            isSelected = false;
            if (ActivateOnSelected) gameObject.SetActive(false);
        }

        protected virtual void DFUNC_Update() {}

        private void Update()
        {
            if (isSelected && Networking.LocalPlayer.IsUserInVR())
            {
                var trigger = Input.GetAxisRaw(triggerAxis);
                var triggerBool = trigger > 0.75f;
                if (triggerBool != triggerBoolLastFrame)
                {
                    if (triggerBool) DFUNC_TriggerPressed();
                    else DFUNC_TriggerReleased();
                }
                triggerBoolLastFrame = triggerBool;
            }
            DFUNC_Update();
        }

    }
}

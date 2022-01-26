
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace EsnyaAircraftAssets
{

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_AdvancedThrustReverser : UdonSharpBehaviour
    {
        public KeyCode keyboardControl = KeyCode.R;

        private SFEXT_AdvancedEngine[] engines;
        private bool selected, isPilot, prevTrigger;
        private string triggerAxis;

        public void SFEXT_L_EntityStart()
        {
            var entity = GetComponentInParent<SaccEntity>();
            engines = entity.gameObject.GetComponentsInChildren<SFEXT_AdvancedEngine>(true);

            gameObject.SetActive(false);
        }

        public void DFUNC_LeftDial() => triggerAxis =  "Oculus_CrossPlatform_PrimaryIndexTrigger";
        public void DFUNC_RightDial() => triggerAxis = "Oculus_CrossPlatform_SecondaryIndexTrigger";
        public void DFUNC_Selected() => selected = true;
        public void DFUNC_Deselected() => selected = false;
        public void SFEXT_O_PilotEnter()
        {
            isPilot = true;
            prevTrigger = true;
            gameObject.SetActive(true);
        }

        public void SFEXT_O_PilotExit()
        {
            isPilot = false;
            selected = false;
            gameObject.SetActive(false);
        }

        private bool GetInput()
        {
            return Input.GetKey(keyboardControl) || selected && Input.GetAxisRaw(triggerAxis) > 0.75f;
        }

        private void Update()
        {
            if (isPilot)
            {
                var trigger = GetInput();
                if (trigger != prevTrigger)
                {
                    prevTrigger = trigger;
                    foreach (var engine in engines)
                    {
                        if (!engine) continue;
                        engine.reversing = trigger;
                    }
                }
            }
        }
    }
}

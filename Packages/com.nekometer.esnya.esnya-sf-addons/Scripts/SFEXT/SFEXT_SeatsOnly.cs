using UdonSharp;
using UnityEngine;
namespace EsnyaSFAddons.SFEXT
{

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_SeatsOnly : UdonSharpBehaviour
    {
        public SaccVehicleSeat[] seats = { };
        public bool excludeMode;
        public GameObject[] objects = { };

        public void SFEXT_L_EntityStart()
        {
            SetActive(false);
        }

        public void SFEXT_O_PilotEnter() => OnEnter();
        public void SFEXT_O_PilotExit() => OnExit();
        public void SFEXT_P_PassengerEnter() => OnEnter();
        public void SFEXT_P_PassengerExit() => OnExit();

        private void OnEnter()
        {
            foreach (var seat in seats)
            {
                if (seat && seat.EntityControl.MySeat == seat.ThisStationID)
                {
                    if (excludeMode) return;
                    SetActive(true);
                    break;
                }
            }

            if (excludeMode) SetActive(true);
        }

        private void OnExit()
        {
            SetActive(false);
        }

        private void SetActive(bool value)
        {
            gameObject.SetActive(value);
            if (objects != null)
            {
                foreach (var obj in objects)
                {
                    if (!obj) continue;
                    obj.SetActive(value);
                }
            }
        }
    }
}

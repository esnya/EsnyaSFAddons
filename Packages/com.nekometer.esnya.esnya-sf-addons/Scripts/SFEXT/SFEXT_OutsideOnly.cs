
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace EsnyaSFAddons.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_OutsideOnly : UdonSharpBehaviour
    {
        public GameObject[] outsideOnly;
        public void SFEXT_L_EntityStart() => SetActive(true);
        public void SFEXT_O_PilotEnter() => SetActive(false);
        public void SFEXT_P_PassengerEnter() => SetActive(false);
        public void SFEXT_O_PilotExit() => SetActive(true);
        public void SFEXT_P_PassengerExit() => SetActive(true);

        private void SetActive(bool value)
        {
            if (outsideOnly == null) return;
            foreach (var o in outsideOnly)
            {
                if (!o) continue;
                o.SetActive(value);
            }
        }
    }
}
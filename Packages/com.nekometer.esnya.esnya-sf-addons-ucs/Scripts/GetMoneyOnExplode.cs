using UdonSharp;
using UnityEngine;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using UCS;
using SaccFlightAndVehicles;
using System.Runtime.InteropServices;

namespace EsnyaSFAddons.UCS
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GetMoneyOnExplode : UdonSharpBehaviour
    {
        public float money = 50.0f;
        private UdonChips udonChips;

        private void Start()
        {
            udonChips = UdonChips.GetInstance();
        }

        public void Explode()
        {
            udonChips.money += money;
        }
    }
}

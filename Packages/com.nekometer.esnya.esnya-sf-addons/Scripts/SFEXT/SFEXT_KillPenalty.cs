using System;
using UdonSharp;
using UnityEngine;
using SaccFlightAndVehicles;
using VRC.SDKBase;

namespace EsnyaSFAddons
{
    /// <summary>
    /// Decrease kill score when vehicle killed. For protection of commercial aircraft, etc.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_KillPenalty : UdonSharpBehaviour
    {
        public SaccScoreboard_Kills KillsBoard;
        public ushort penalty = 100;
        public bool applyToBestKills = true;

        [NonSerialized] public SaccEntity EntityControl;
        private SaccAirVehicle airVehicle;

        public void SFEXT_L_EntityStart()
        {
            airVehicle = (SaccAirVehicle)EntityControl.GetExtention(GetUdonTypeName<SaccAirVehicle>());
        }

        public void SFEXT_G_Explode()
        {
            if (!KillsBoard || !airVehicle || !EntityControl.LastAttacker || !EntityControl.LastAttacker.Using) return;

            var time = Time.time;
            if (!(airVehicle.Occupied || (time - airVehicle.LastHitTime < 5 && ((time - EntityControl.PilotExitTime) < 5)))) return;

            KillsBoard.MyKills = (ushort)Mathf.Clamp(KillsBoard.MyKills - penalty, ushort.MinValue, ushort.MaxValue);
            if (applyToBestKills) {
                KillsBoard.MyBestKills = (ushort)Mathf.Clamp(KillsBoard.MyBestKills - penalty, ushort.MinValue, ushort.MaxValue);
                var localPlayer = Networking.LocalPlayer;

                if (KillsBoard.TopKiller == localPlayer.displayName)
                {
                    Networking.SetOwner(localPlayer, KillsBoard.gameObject);
                    KillsBoard.TopKiller = "Nobody";
                    KillsBoard.TopKills = 0;
                    KillsBoard.RequestSerialization();
                }
            }
        }
    }
}

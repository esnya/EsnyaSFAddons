
using System;
using System.Data;
using System.Linq;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace EsnyaAircraftAssets
{
    [DefaultExecutionOrder(1000)]
    public class VehicleLockAuthorizer : UdonSharpBehaviour
    {
        public string[] authorizedUserNames = {
            "ESNYA／エスニヤ",
        };
        public bool master = false;
        public bool instanceOwner = true;

        private VehicleLock[] vehicleLocks;

        private void Start()
        {
            vehicleLocks = GetComponentsInChildren<VehicleLock>();
            UpdateLocks();
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            UpdateLocks();
        }

        private void UpdateLocks()
        {
            var autohrized = IsAuthorized();
            foreach (var vehicleLock in vehicleLocks) vehicleLock.gameObject.SetActive(autohrized);
        }

        private bool IsAuthorized()
        {
            var player = Networking.LocalPlayer;
            if (master && player.isMaster || instanceOwner && player.isInstanceOwner) return true;

            foreach (var name in authorizedUserNames)
            {
                if (player.displayName == name) return true;
            }

            return false;
        }
    }
}

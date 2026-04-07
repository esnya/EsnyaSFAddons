using UnityEngine;
using SaccFlightAndVehicles;
using VRC.SDKBase;

namespace EsnyaSFAddons
{
    public class PlayerStrike
    {
        public static bool CheckPlayerStrike(Transform origin, SaccEntity saccEntity, float normalizedRpm, float minHazardRange, float maxHazardRange, float thrust)
        {
            var striked = false;
            var localPlayer = Networking.LocalPlayer;

            if (!Utilities.IsValid(localPlayer) || !saccEntity || saccEntity.InVehicle || Mathf.Approximately(normalizedRpm, 0)) return striked;

            var playerPosition = localPlayer.GetPosition();

            var relative = origin.InverseTransformPoint(playerPosition);
            var distance = relative.magnitude;

            if (distance > maxHazardRange) return striked;

            var hazardRange = Mathf.Lerp(minHazardRange, maxHazardRange, normalizedRpm);

            if (distance > hazardRange) return striked;

            var forceScale = 1 - distance / hazardRange;

            if (relative.z >= 0)
            {
                striked = true;
                AddPlayerForce(localPlayer, forceScale * thrust * (origin.position - playerPosition).normalized);
            }
            else
            {
                AddPlayerForce(localPlayer, - forceScale * thrust * origin.forward);
            }

            return striked;
        }

        private static void AddPlayerForce(VRCPlayerApi player, Vector3 force)
        {
            player.SetVelocity(player.GetVelocity() + (force + (player.IsPlayerGrounded() ? Vector3.up * 0.5f : Vector3.zero)) * Time.deltaTime);
        }
    }
}

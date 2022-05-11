
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace EsnyaSFAddons
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class RemoteThrottle : UdonSharpBehaviour
    {
        public float step = 0.1f;
        private SaccAirVehicle airVehicle;

        private void Start()
        {
            var saccEntity = GetComponentInParent<SaccEntity>();
            airVehicle = (SaccAirVehicle)saccEntity.GetExtention(GetUdonTypeName<SaccAirVehicle>());
            gameObject.SetActive(false);
        }

        public void Increase()
        {
            if (airVehicle && airVehicle.IsOwner)
            {
                airVehicle.PlayerThrottle = Mathf.Clamp01(airVehicle.PlayerThrottle + step);
            }
        }

        public void Decrease()
        {
            if (airVehicle && airVehicle.IsOwner)
            {
                airVehicle.PlayerThrottle = Mathf.Clamp01(airVehicle.PlayerThrottle - step);
            }
        }
    }
}

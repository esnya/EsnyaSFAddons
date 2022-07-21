
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using SaccFlightAndVehicles;

namespace EsnyaSFAddons.Accesory
{
    /// <summary>
    /// Control throttle by remote players such as C/O or PM
    /// </summary>
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

        /// <summary>
        /// Increase throttle
        /// </summary>
        public void Increase()
        {
            if (airVehicle && airVehicle.IsOwner)
            {
                airVehicle.PlayerThrottle = Mathf.Clamp01(airVehicle.PlayerThrottle + step);
            }
        }

        /// <summary>
        /// Decrease throttle
        /// </summary>
        public void Decrease()
        {
            if (airVehicle && airVehicle.IsOwner)
            {
                airVehicle.PlayerThrottle = Mathf.Clamp01(airVehicle.PlayerThrottle - step);
            }
        }
    }
}

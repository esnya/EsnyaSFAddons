using UdonSharp;
using UnityEngine;

namespace EsnyaSFAddons
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_PassengerOnly : UdonSharpBehaviour
    {
        public bool moveToSeat = true;
        public SaccVehicleSeat[] excludes = { };

        private SaccEntity entity;
        public void SFEXT_L_EntityStart()
        {
            entity = GetComponentInParent<SaccEntity>();
            gameObject.SetActive(false);
        }

        public void SFEXT_P_PassengerEnter()
        {
            var mySeat = entity.MySeat;
            if (mySeat < 0) return;

            var station = entity.VehicleStations[mySeat];
            if (!station) return;

            var seat = station.GetComponent<SaccVehicleSeat>();
            if (seat.IsPilotSeat || excludes != null && System.Array.IndexOf(excludes, seat) >= 0) return;

            if (moveToSeat) transform.SetPositionAndRotation(seat.transform.position, seat.transform.rotation);

            gameObject.SetActive(true);
        }

        public void SFEXT_P_PassengerExit()
        {
            gameObject.SetActive(false);
        }
    }
}

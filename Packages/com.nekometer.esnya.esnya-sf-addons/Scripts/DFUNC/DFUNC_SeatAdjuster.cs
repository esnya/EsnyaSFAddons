using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace EsnyaSFAddons.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_SeatAdjuster : UdonSharpBehaviour
    {
        public KeyCode desktopUp = KeyCode.Home, desktopDown = KeyCode.End, desktopForward = KeyCode.Insert, desktopBack = KeyCode.Delete;
        public float desktopStep = 0.05f;

        [Header("Debug")]
        public Transform debugTransform;

        private string triggerAxis;
        private VRCPlayerApi.TrackingDataType trackingTarget;
        private bool isSelected, triggered, prevTriggered;
        private SaccEntity entity;
        private SaccAirVehicle airVehicle;
        private Transform controlsRoot;
        private SaccVehicleSeat seat;

        public void DFUNC_LeftDial()
        {
            triggerAxis = "Oculus_CrossPlatform_PrimaryIndexTrigger";
            trackingTarget = VRCPlayerApi.TrackingDataType.LeftHand;
        }
        public void DFUNC_RightDial()
        {
            triggerAxis = "Oculus_CrossPlatform_SecondaryIndexTrigger";
            trackingTarget = VRCPlayerApi.TrackingDataType.RightHand;
        }
        public void DFUNC_Selected()
        {
            isSelected = true;
            prevTriggered = false;
        }
        public void DFUNC_Deselected() => isSelected = false;

        public void SFEXTP_L_EntityStart() => SFEXT_L_EntityStart();
        public void SFEXT_L_EntityStart()
        {
            entity = GetComponentInParent<SaccEntity>();
            airVehicle = entity.GetComponentInChildren<SaccAirVehicle>(true);
            controlsRoot = airVehicle.ControlsRoot;
            if (!controlsRoot) controlsRoot = entity.transform;

            Deactivate();
        }

        public void SFEXT_O_PilotEnter() => Activate();
        public void SFEXT_O_PilotExit() => Deactivate();
        public void SFEXTP_O_UserEnter() => Activate();
        public void SFEXTP_O_UserExit() => Deactivate();

        private Vector3 sliderOrigin;
        private Vector2 adjustedOrigin;
        public override void PostLateUpdate()
        {
            if (!seat) return;

            prevTriggered = triggered;
            triggered = isSelected && Input.GetAxis(triggerAxis) > 0.75f || debugTransform;

            if (triggered)
            {
                var trackingPosition = controlsRoot.InverseTransformPoint(debugTransform ? debugTransform.position : Networking.LocalPlayer.GetTrackingData(trackingTarget).position);
                if (!prevTriggered)
                {
                    sliderOrigin = trackingPosition;
                    adjustedOrigin = seat.AdjustedPos;
                }
                else
                {
                    seat.AdjustedPos = adjustedOrigin - XYZtoYZ(trackingPosition - sliderOrigin);
                }
            }
            else
            {
                var up = Input.GetKeyDown(desktopUp);
                var down = Input.GetKeyDown(desktopDown);
                var forward = Input.GetKeyDown(desktopForward);
                var back = Input.GetKeyDown(desktopBack);
                var keyDown = up || down || forward || back;
                seat.AdjustedPos += (
                    (up ? Vector2.right : Vector2.zero)
                    + (down ? Vector2.left : Vector2.zero)
                    + (forward ? Vector2.up : Vector2.zero)
                    + (back ? Vector2.down : Vector2.zero)
                ) * desktopStep;
                if (prevTriggered || keyDown) seat.RequestSerialization();
            }
        }

        private void Activate()
        {
            isSelected = false;
            prevTriggered = false;

            var mySeatId = entity.MySeat;
            var station = mySeatId >= 0 ? entity.VehicleStations[mySeatId] : null;
            if (!station)
            {
                Deactivate();
                return;
            }

            seat = station.GetComponent<SaccVehicleSeat>();

            gameObject.SetActive(true);
        }
        private void Deactivate()
        {
            seat = null;
            gameObject.SetActive(false);
        }

        private Vector2 XYZtoYZ(Vector3 xyz)
        {
            return Vector2.right * xyz.y + Vector2.up * xyz.z;
        }
        // private Vector3 YZtoXYZ(Vector3 yz)
        // {
        //     return Vector3.up * yz.x + Vector3.forward * yz.z;
        // }
    }
}

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace EsnyaAircraftAssets
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DFUNCP_IHaveControl : UdonSharpBehaviour
    {
        public KeyCode desktopControl = KeyCode.F8;
        public GameObject Dial_Funcon;
        public float pressTime = 3.0f;
        public bool inverseSwitchHand = true;

        [Header("Haptics")]
        [Range(0, 1)] public float hapticDuration = 0.2f;
        [Range(0, 1)] public float hapticAmplitude = 0.5f;
        [Range(0, 1)] public float hapticFrequency = 0.1f;

        private string triggerAxis;
        private VRC_Pickup.PickupHand hand;
        private SaccEntity entity;
        private SaccAirVehicle airVehicle;
        private SaccVehicleSeat pilotSeat, passengerSeat;
        private bool pilotSeatAdjust, passengerSeatAdjust;
        private Vector3 captainSeatPosition, firstOfficerSeatPosition;
        private Vector3[] pilotSeatChildPositions, passengerSeatChildPositions;
        private Transform[] seatChildExcludes;
        private bool initialized, isUser, isSelected, hasPilot, isPilot, enterAsPilot, enterAsPassenger;
        private float pressingTime;
        private bool swapped;

        public void DFUNC_LeftDial()
        {
            triggerAxis = "Oculus_CrossPlatform_PrimaryIndexTrigger";
            hand = VRC_Pickup.PickupHand.Left;
        }
        public void DFUNC_RightDial()
        {
            triggerAxis = "Oculus_CrossPlatform_SecondaryIndexTrigger";
            hand = VRC_Pickup.PickupHand.Right;
        }
        public void DFUNC_Selected()
        {
            isSelected = true;
        }
        public void DFUNC_Deselected() => isSelected = false;

        public void SFEXTP_L_EntityStart()
        {
            ResetStatus();
        }

        public void SFEXTP_G_PilotEnter()
        {
            hasPilot = true;
            isPilot = entity.IsOwner;
            SendCustomEventDelayedFrames(nameof(_EnterAsPassenger), 2);
        }
        public void SFEXTP_G_PilotExit()
        {
            hasPilot = false;
            isPilot = false;
            SendCustomEventDelayedFrames(nameof(_EnterAsPilot), 2);
        }
        public void SFEXTP_O_UserEnter()
        {
            isUser = true;
            gameObject.SetActive(true);
        }
        public void SFEXTP_O_UserExit()
        {
            isUser = false;
            gameObject.SetActive(false);
        }
        public void SFEXTP_O_PlayerJoined() => Sync();
        public void SFEXTP_G_Explode() => ResetStatus();
        public void SFEXTP_G_RespawnButton() => ResetStatus();

        private void Update()
        {
            var deltaTime = Time.deltaTime;

            if (isUser)
            {
                if (Input.GetKey(desktopControl) || isSelected && Input.GetAxis(triggerAxis) > 0.75f)
                {
                    pressingTime += deltaTime;
                }
                else
                {
                    pressingTime = 0;
                }

                var progress = pressingTime / pressTime;
                if (pressingTime > 0)
                {
                    PlayHapticEvent(progress);
                }

                if (Dial_Funcon)
                {
                    var active = pressingTime > 0 && Time.time / Mathf.Lerp(10.0f, 1f, progress) % 2.0f > 1.0f || pressingTime >= pressTime;
                    if (active != Dial_Funcon.activeSelf) Dial_Funcon.SetActive(active);
                }

                if (pressingTime >= pressTime)
                {
                    pressingTime = 0;
                    Toggle();
                }
            }
        }

        private bool switchHandsJoyThrottle;
        private bool engineOffOnExit;
        private void Initialize()
        {
            if (initialized) return;

            entity = GetComponentInParent<SaccEntity>();
            airVehicle = entity.GetComponentInChildren<SaccAirVehicle>();
            switchHandsJoyThrottle = airVehicle.SwitchHandsJoyThrottle;

            foreach (var station in entity.VehicleStations)
            {
                var seat = station.GetComponent<SaccVehicleSeat>();
                if (seat.IsPilotSeat)
                {
                    pilotSeat = seat;
                    break;
                }
            }
            passengerSeat = GetComponentInParent<SaccVehicleSeat>();

            pilotSeatAdjust = pilotSeat.AdjustSeat;
            passengerSeatAdjust = passengerSeat.AdjustSeat;

            captainSeatPosition = entity.transform.InverseTransformPoint(pilotSeat.transform.position);
            firstOfficerSeatPosition = entity.transform.InverseTransformPoint(passengerSeat.transform.position);

            pilotSeatChildPositions = new Vector3[pilotSeat.transform.childCount];
            for (var i = 0; i < pilotSeatChildPositions.Length; i++)
            {
                pilotSeatChildPositions[i] = entity.transform.InverseTransformPoint(pilotSeat.transform.GetChild(i).position);
            }

            passengerSeatChildPositions = new Vector3[passengerSeat.transform.childCount];
            for (var i = 0; i < passengerSeatChildPositions.Length; i++)
            {
                passengerSeatChildPositions[i] = entity.transform.InverseTransformPoint(passengerSeat.transform.GetChild(i).position);
            }

            var pilotStation = (VRCStation)pilotSeat.gameObject.GetComponent(typeof(VRCStation));
            var passengerStation = (VRCStation)passengerSeat.gameObject.GetComponent(typeof(VRCStation));
            seatChildExcludes = new Transform[6];
            seatChildExcludes[0] = pilotStation.stationEnterPlayerLocation;
            seatChildExcludes[1] = pilotStation.stationExitPlayerLocation;
            seatChildExcludes[2] = pilotSeat.TargetEyePosition;
            seatChildExcludes[3] = passengerStation.stationEnterPlayerLocation;
            seatChildExcludes[4] = passengerStation.stationExitPlayerLocation;
            seatChildExcludes[5] = passengerSeat.TargetEyePosition;

            engineOffOnExit = airVehicle.EngineOffOnExit;

            initialized = true;
        }

        private void ResetStatus()
        {
            Initialize();
            if (swapped) G_RevertControl();
        }

        private void Toggle()
        {
            if (!swapped) SendCustomNetworkEvent(NetworkEventTarget.All, nameof(G_SwapControl));
            else SendCustomNetworkEvent(NetworkEventTarget.All, nameof(G_RevertControl));
        }
        private void Sync()
        {
            if (swapped) SendCustomNetworkEvent(NetworkEventTarget.All, nameof(G_SwapControl));
            else SendCustomNetworkEvent(NetworkEventTarget.All, nameof(G_RevertControl));
        }

        public void G_SwapControl()
        {
            Initialize();

            SetSeatTransforms(true);
            if (!swapped && (isUser || entity.IsOwner)) SwapPlayers();
            swapped = true;
        }

        public void G_RevertControl()
        {
            Initialize();

            SetSeatTransforms(false);
            if (swapped && (isUser || entity.IsOwner)) SwapPlayers();
            swapped = false;
        }

        private void SetSeatTransforms(bool swapped)
        {
            SetSeatTransform(swapped ? passengerSeat : pilotSeat, captainSeatPosition);
            SetSeatTransform(swapped ? pilotSeat : passengerSeat, firstOfficerSeatPosition);
            SetSeatChildTransforms(pilotSeat, pilotSeatChildPositions);
            SetSeatChildTransforms(passengerSeat, passengerSeatChildPositions);

            if (inverseSwitchHand) airVehicle.SwitchHandsJoyThrottle = swapped ? !switchHandsJoyThrottle : switchHandsJoyThrottle;
        }
        private void SetSeatTransform(SaccVehicleSeat seat, Vector3 targetPosition)
        {
            seat.transform.position = entity.transform.TransformPoint(targetPosition);
        }

        private void SetSeatChildTransforms(SaccVehicleSeat seat, Vector3[] targetPositions)
        {
            for (var i = 0; i < targetPositions.Length; i++)
            {
                var child = seat.transform.GetChild(i);
                if (System.Array.IndexOf(seatChildExcludes, child) >= 0) continue;
                child.position = entity.transform.TransformPoint(targetPositions[i]);
            }
        }

        private Vector2 seatAdjustedPos;
        private void SwapPlayers()
        {
            enterAsPilot = isUser;
            enterAsPassenger = !isUser && entity.IsOwner && hasPilot;

            SaveAdjustedPos(isUser ? passengerSeat : pilotSeat);

            if (isUser && !hasPilot)
            {
                SendCustomEventDelayedFrames(nameof(_EnterAsPilot), 2);
            }
            entity.ExitStation();
        }

        private void SaveAdjustedPos(SaccVehicleSeat seat)
        {
            seatAdjustedPos = seat.AdjustedPos;
            pilotSeat.AdjustSeat = false;
            passengerSeat.AdjustSeat = false;

            airVehicle.EngineOffOnExit = false;
        }

        private void LoadAdjustedPos(SaccVehicleSeat seat)
        {
            seat.AdjustedPos = seatAdjustedPos;
            seat.RequestSerialization();

            pilotSeat.AdjustSeat = pilotSeatAdjust;
            passengerSeat.AdjustSeat = passengerSeatAdjust;

            airVehicle.EngineOffOnExit = engineOffOnExit;
        }

        public void _EnterAsPilot()
        {
            if (enterAsPilot)
            {
                enterAsPilot = false;
                pilotSeat.SendCustomEvent("_interact");
                LoadAdjustedPos(pilotSeat);
            }
        }

        public void _EnterAsPassenger()
        {
            if (enterAsPassenger)
            {
                enterAsPassenger = false;
                passengerSeat.SendCustomEvent("_interact");
                LoadAdjustedPos(passengerSeat);
            }
        }

        private void PlayHapticEvent(float amplitude)
        {
            Networking.LocalPlayer.PlayHapticEventInHand(hand, Time.deltaTime, amplitude * hapticAmplitude, hapticFrequency);
        }
    }
}

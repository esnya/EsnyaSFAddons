using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

/*
@startuml(timing)
box "Client on Lema"
actor Lema
participant PilotSeat as R_PilotSeat
participant PassengerSeat as R_PassengerSeat
participant SaccEntity as R_SaccEntity
participant IHaveControl as R_IHaveControl
end box

participant CaptainSeatPosition
participant FirstOfficerSeatPosition

box "Client on Romeo"
actor Romeo
participant PilotSeat as L_PilotSeat
participant PassengerSeat as L_PassengerSeat
participant SaccEntity as L_SaccEntity
participant IHaveControl as L_IHaveControl
end box

== Initialize ==
R_PilotSeat -> CaptainSeatPosition: Inital Placed
R_PassengerSeat -> FirstOfficerSeatPosition: Initial Placed
L_PilotSeat -> CaptainSeatPosition: Inital Placed
L_PassengerSeat -> FirstOfficerSeatPosition: Initial Placed

== Pilot Enter ==
Lema -> R_PilotSeat: Interact
activate R_PilotSeat
    R_PilotSeat -> R_SaccEntity: EnterAsPilot
    activate R_SaccEntity
        R_SaccEntity --> R_SaccEntity: SFEXT_O_PilotEnter
        R_SaccEntity --> R_SaccEntity: SFEXT_G_PilotEnter
        R_SaccEntity --> L_SaccEntity: SFEXT_G_PilotEnter
        activate L_SaccEntity
            L_SaccEntity --> L_IHaveControl: SFEXT_G_PilotEnter
            activate L_IHaveControl
                L_IHaveControl -> L_IHaveControl: hasPilot = true
                L_IHaveControl -> L_SaccEntity: is pilot ?
                activate L_SaccEntity
                return no
                L_IHaveControl -> L_IHaveControl: isPilot = false
            return
        deactivate
        R_SaccEntity --> R_IHaveControl: SFEXT_G_PilotEnter
        activate R_IHaveControl
            R_IHaveControl -> R_IHaveControl: hasPilot = true
            R_IHaveControl -> R_SaccEntity: is pilot ?
            activate R_SaccEntity
            return yes
            R_IHaveControl -> R_IHaveControl: isPilot = true
        deactivate
    return
deactivate

== Passenger Enter ==
Romeo -> L_PassengerSeat: Interact
activate L_PassengerSeat
    L_PassengerSeat -> L_SaccEntity: EnterAsPassenger
    activate L_SaccEntity
        L_SaccEntity --> L_IHaveControl: SFEXTP_O_UserEnter
        activate L_IHaveControl
            L_IHaveControl -> L_IHaveControl: isUser = true
        deactivate
    return
deactivate

== Swap Control ==
Romeo -> L_IHaveControl: Take Over Control
activate L_IHaveControl
    L_IHaveControl --> L_IHaveControl: SwapControl
    activate L_IHaveControl
        L_IHaveControl -> L_PassengerSeat: Move
        activate L_PassengerSeat
            L_PassengerSeat -> CaptainSeatPosition: Placed
        return
        L_IHaveControl -> L_PilotSeat: Move
        activate L_PilotSeat
            L_PilotSeat -> FirstOfficerSeatPosition: Placed
        return
        L_IHaveControl -> L_SaccEntity: ExitStation
        activate L_SaccEntity
            L_SaccEntity -> L_PassengerSeat: ExitStation
            activate L_PassengerSeat
                L_PassengerSeat --> L_SaccEntity: SFEXT_P_PassengerExit
                activate L_SaccEntity
                    L_SaccEntity --> L_IHaveControl: SFEXTP_O_UserExit
                deactivate
                activate L_IHaveControl
                    L_IHaveControl -> L_IHaveControl: isUser = false
                deactivate
            return
        return
    deactivate
    L_IHaveControl --> R_IHaveControl: SwapControl
    activate R_IHaveControl
        R_IHaveControl -> R_PassengerSeat: Move
        activate R_PassengerSeat
            R_PassengerSeat -> CaptainSeatPosition: Placed
        return
        R_IHaveControl -> R_PilotSeat: Move
        activate R_PilotSeat
            R_PilotSeat -> FirstOfficerSeatPosition: Placed
        return
        R_IHaveControl -> R_SaccEntity: ExitStation
        activate R_SaccEntity
            R_SaccEntity -> R_PilotSeat: ExitStation
            activate R_PilotSeat
            return
            R_SaccEntity --> R_IHaveControl: SFEXT_G_PilotExit
            activate R_IHaveControl
            deactivate
            R_SaccEntity --> L_IHaveControl: SFEXT_G_PilotExit
            activate L_IHaveControl
                L_IHaveControl -> L_PilotSeat: Interact
                activate L_PilotSeat
                    L_PilotSeat -> L_SaccEntity: EnterAsPilot
                    activate L_SaccEntity
                        L_SaccEntity --> L_IHaveControl: SFEXT_G_PilotEnter
                        activate L_IHaveControl
                            L_IHaveControl -> L_IHaveControl: hasPilot = true
                            L_IHaveControl -> L_SaccEntity: is pilot ?
                            activate L_SaccEntity
                            return yes
                            L_IHaveControl -> L_IHaveControl: isPilot = true
                        deactivate
                        L_SaccEntity --> R_SaccEntity: SFEXT_G_PilotEnter
                        activate R_SaccEntity
                            R_SaccEntity --> R_IHaveControl: SFEXT_G_PilotEnter
                            activate R_IHaveControl
                                R_IHaveControl -> R_IHaveControl: hasPilot = true
                                R_IHaveControl -> R_SaccEntity: is pilot ?
                                activate R_SaccEntity
                                return no
                                R_IHaveControl -> R_IHaveControl: isPilot = false
                                R_IHaveControl --> R_PassengerSeat: Interact
                                activate R_PassengerSeat
                                    R_PassengerSeat -> R_SaccEntity: EnterAsPassenger
                                    R_SaccEntity --> R_IHaveControl: SFEXTP_O_UserEnter
                                    activate R_IHaveControl
                                        R_IHaveControl -> R_IHaveControl: isUser = true
                                    deactivate
                                deactivate
                            deactivate
                        deactivate
                    return
                return
            deactivate
        return
    deactivate
deactivate
@enduml
*/

namespace EsnyaSFAddons.DFUNC
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

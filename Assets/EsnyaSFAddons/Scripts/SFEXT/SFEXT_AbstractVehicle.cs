
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace EsnyaSFAddons
{

    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    [DefaultExecutionOrder(5)]//after dfuncs that can set values used by this
    public class SFEXT_AbstractVehicle : UdonSharpBehaviour
    {
        [Tooltip("Base object reference")]
        public SaccEntity EntityControl;
        [Tooltip("The object containing all non-trigger colliders for the vehicle, their layers are changed when entering and exiting")]
        public Transform VehicleMesh;
        [Tooltip("Layer to set the VehicleMesh and it's children to when entering vehicle")]
        public int OnboardVehicleLayer = 31;
        [Tooltip("Change all children of VehicleMesh, or just the objects with colliders?")]
        public bool OnlyChangeColliders = false;
        [Tooltip("Position used to raycast from in order to calculate ground effect")]
        public Transform GroundDetector;
        [Tooltip("Position traced down from to detect whether the vehicle is currently on the ground. Trace distance is 44cm. Place between the back wheels around 20cm above the height where the wheels touch the ground")]
        public float GroundDetectorRayDistance = .44f;
        [Tooltip("Distance traced down from the ground detector's position to see if the ground is there, in order to determine if the vehicle is grounded")]
        public LayerMask GroundDetectorLayers = 2049;
        [Tooltip("HP of the vehicle")]
        [UdonSynced(UdonSyncMode.None)] public float Health = 23f;
        [Tooltip("Teleport the vehicle to the oposite side of the map when flying too far in one direction?")]
        public bool RepeatingWorld = true;
        [Tooltip("Distance you can travel away from world origin before being teleported to the other side of the map. Not recommended to increase, floating point innacuracy and game freezing issues may occur if larger than default")]
        public float RepeatingWorldDistance = 20000;
        [Tooltip("Use the left hand to control the joystick and the right hand to control the throttle?")]
        public bool SwitchHandsJoyThrottle = false;

        [Header("Response:")]
        [Tooltip("Vehicle thrust at max throttle without afterburner")]
        public float ThrottleStrength = 20f;
        [Tooltip("Make VR Throttle motion controls use the Y axis instead of the Z axis for adjustment (Helicopter collective)")]
        public bool VerticalThrottle = false;
        [Tooltip("Multiply how much the VR throttle moves relative to hand movement")]
        public float ThrottleSensitivity = 6f;
        [Tooltip("Joystick sensitivity. Angle at which joystick will reach maximum deflection in VR")]
        public Vector3 MaxJoyAngles = new Vector3(45, 45, 45);
        [Tooltip("How far down you have to push the grip button to grab the joystick and throttle")]
        public float GripSensitivity = .75f;
        [Tooltip("How quickly the vehicle throttles up after throttle is increased (Lerp)")]
        public float AccelerationResponse = 4.5f;
        [Tooltip("How quickly the vehicle throttles down relative to how fast it throttles up after throttle is decreased")]
        public float EngineSpoolDownSpeedMulti = .5f;

        public float MaxGs = 40f;
        [Tooltip("Damage taken Per G above maxGs, per second.\n(Gs - MaxGs) * GDamage = damage/second")]
        public float GDamage = 10f;
        [Tooltip("Length of the trace that looks for the ground to calculate ground effect")]

        public Transform ControlsRoot;
        [Tooltip("Wind speed on each axis")]
        public Vector3 Wind;
        [Tooltip("Strength of noise-based changes in wind strength")]
        public float WindGustStrength = 15;
        [Tooltip("How often wind gust changes strength")]
        public float WindGustiness = 0.03f;
        [Tooltip("Scale of world space gust cells, smaller number = larger cells")]
        public float WindTurbulanceScale = 0.0001f;
        [Tooltip("Extra drag added when airspeed approaches the speed of sound")]

        [UdonSynced(UdonSyncMode.None)] public float Fuel = 900;
        [Tooltip("Amount of fuel at which throttle will start reducing")]
        public float LowFuel = 125;
        [Tooltip("Fuel consumed per second at max throttle, scales with throttle")]
        public float FuelConsumption = 1;
        [Tooltip("Fuel consumed per second at max throttle, scales with throttle")]
        public float MinFuelConsumption = .25f;
        [Tooltip("Multiply FuelConsumption by this number when at full afterburner Scales with afterburner level")]
        public float FuelConsumptionABMulti = 3f;
        [Tooltip("Number of resupply ticks it takes to refuel fully from zero")]
        public float RefuelTime = 25;
        [Tooltip("Number of resupply ticks it takes to repair fully from zero")]
        public float RepairTime = 30;
        [Tooltip("Time until vehicle reappears after exploding")]
        public float RespawnDelay = 10;
        [Tooltip("Time after reappearing the vehicle is invincible for")]
        public float InvincibleAfterSpawn = 2.5f;
        [Tooltip("Damage taken when hit by a bullet")]
        public float BulletDamageTaken = 10f;
        [Tooltip("Locally destroy target if prediction thinks you killed them, should only ever cause problems if you have a system that repairs vehicles during a fight")]
        public bool PredictDamage = true;
        [Tooltip("Multiply how much damage is done by missiles")]
        public float MissileDamageTakenMultiplier = 1f;
        [Tooltip("Strength of force that pushes the vehicle when a missile hits it")]
        public float MissilePushForce = 1f;
        [Tooltip("Zero height of the calculation of atmosphere thickness and HUD altitude display")]
        public float SeaLevel = -10f;
        [Tooltip("Altitude above 'Sea Level' at which the atmosphere starts thinning, In meters. 12192 = 40,000~ feet")]
        public float AtmosphereThinningStart = 12192f; //40,000 feet
        [Tooltip("Altitude above 'Sea Level' at which the atmosphere reaches zero thickness. In meters. 19812 = 65,000~ feet")]
        public float AtmosphereThinningEnd = 19812; //65,000 feet
        [Tooltip("When in desktop mode, make the joystick input square? (for game controllers, disable for actual joysticks")]
        public bool SquareJoyInput = true;
        [Tooltip("Set Engine On when entering the vehicle?")]
        public bool EngineOnOnEnter = true;
        [Tooltip("Set Engine Off when entering the vehicle?")]
        public bool EngineOffOnExit = true;
        [FieldChangeCallback(nameof(EngineOn))] public bool _EngineOn = false;
        public bool EngineOn
        {
            set
            {
                //disable thrust vectoring if engine off
                if (value)
                {
                    if (!_EngineOn)
                    {
                        EntityControl.SendEventToExtensions("SFEXT_G_EngineOn");
                        VehicleAnimator.SetBool("EngineOn", true);
                    }

                    //replaces StickyWheelWorkaround
                    if (HasWheelColliders)
                    {
                        foreach (WheelCollider wheel in VehicleWheelColliders)
                        { wheel.motorTorque = 0.00000000000000000000000000000000001f; }
                    }
                }
                else
                {
                    if (_EngineOn)
                    {
                        EntityControl.SendEventToExtensions("SFEXT_G_EngineOff");
                        Taxiinglerper = 0;
                        VehicleAnimator.SetBool("EngineOn", false);
                    }

                    if (HasWheelColliders)
                    {
                        foreach (WheelCollider wheel in VehicleWheelColliders)
                        { wheel.motorTorque = 0; }
                    }
                }
                _EngineOn = value;
            }
            get => _EngineOn;
        }
        public void SetEngineOn()
        {
            EngineOn = true;
        }
        public void SetEngineOff()
        {
            EngineOn = false;
        }
        [System.NonSerializedAttribute] public float AllGs;
        [System.NonSerializedAttribute][UdonSynced(UdonSyncMode.Linear)] public float EngineOutput = 0f;
        [System.NonSerializedAttribute] public Vector3 CurrentVel = Vector3.zero;
        [System.NonSerializedAttribute][UdonSynced(UdonSyncMode.Linear)] public float VertGs = 1f;
        [System.NonSerializedAttribute] public float AngleOfAttackPitch;
        [System.NonSerializedAttribute] public float AngleOfAttackYaw;
        [System.NonSerializedAttribute][UdonSynced(UdonSyncMode.Linear)] public float AngleOfAttack;//MAX of yaw & pitch aoa //used by effectscontroller and hudcontroller
        [System.NonSerializedAttribute] public bool Occupied = false; //this is true if someone is sitting in pilot seat
        [System.NonSerializedAttribute] public float VTOLAngle;

        [System.NonSerializedAttribute] public Animator VehicleAnimator;
        [System.NonSerializedAttribute] public ConstantForce VehicleConstantForce;
        [System.NonSerializedAttribute] public Rigidbody VehicleRigidbody;
        [System.NonSerializedAttribute] public Transform VehicleTransform;
        [System.NonSerializedAttribute] public float VTOLAngleForward90;//dot converted to angle, 0=0 90=1 max 1, for adjusting values that change with engine angle
        [System.NonSerializedAttribute] public float VTOLAngleForwardDot;
        [System.NonSerializedAttribute] public bool VTOLAngleForward = true;
        private VRC.SDK3.Components.VRCObjectSync VehicleObjectSync;
        private GameObject VehicleGameObj;
        [System.NonSerializedAttribute] public Transform CenterOfMass;
        [System.NonSerializedAttribute] public bool ThrottleGripLastFrame = false;
        [System.NonSerializedAttribute] public bool JoystickGripLastFrame = false;
        Quaternion JoystickZeroPoint;
        Quaternion VehicleRotLastFrame;
        [System.NonSerializedAttribute] public float PlayerThrottle;
        private float TempThrottle;
        private float ThrottleZeroPoint;
        [System.NonSerializedAttribute] public float ThrottleInput = 0f;
        [System.NonSerializedAttribute] public float FullHealth;
        [System.NonSerializedAttribute] public bool Taxiing = false;
        [System.NonSerializedAttribute] public bool Floating = false;
        [System.NonSerializedAttribute][UdonSynced(UdonSyncMode.Linear)] public Vector3 RotationInputs;
        [System.NonSerializedAttribute] public bool Piloting = false;
        [System.NonSerializedAttribute] public bool Passenger = false;
        [System.NonSerializedAttribute] public bool InEditor = true;
        [System.NonSerializedAttribute] public bool InVR = false;
        [System.NonSerializedAttribute] public Vector3 LastFrameVel = Vector3.zero;
        [System.NonSerializedAttribute] public VRCPlayerApi localPlayer;
        [System.NonSerializedAttribute] public float AtmoshpereFadeDistance;
        [System.NonSerializedAttribute] public float AtmosphereHeightThing;
        [System.NonSerializedAttribute] public float Atmosphere = 1;
        private Vector3 Pitching;
        private Vector3 Yawing;
        [System.NonSerializedAttribute] public float Taxiinglerper;
        [System.NonSerializedAttribute] public float ExtraDrag = 1;
        [System.NonSerializedAttribute] public float ExtraLift = 1;
        [System.NonSerializedAttribute] public float Speed;
        [System.NonSerializedAttribute] public float AirSpeed;
        [System.NonSerializedAttribute] public bool IsOwner = false;
        private Vector3 FinalWind;//includes Gusts
        [System.NonSerializedAttribute] public Vector3 AirVel;
        private float StillWindMulti;//multiplies the speed of the wind by the speed of the plane when taxiing to prevent still planes flying away
        [System.NonSerializedAttribute] public float FullFuel;
        private float LowFuelDivider;
        private float LastResupplyTime = 0;
        private bool Initialized;
        [System.NonSerializedAttribute] public bool IsAirVehicle = true;//could be checked by any script targeting/checking this vehicle to see if it is the kind of vehicle they're looking for
        [System.NonSerializedAttribute] public bool dead = false;
        [System.NonSerializedAttribute] public float FullGunAmmo;
        //use these for whatever, Only MissilesIncomingHeat is used by the prefab
        [System.NonSerializedAttribute] public int MissilesIncomingHeat = 0;
        [System.NonSerializedAttribute] public int MissilesIncomingRadar = 0;
        [System.NonSerializedAttribute] public int MissilesIncomingOther = 0;
        [System.NonSerializedAttribute] public Vector3 Spawnposition;
        [System.NonSerializedAttribute] public Quaternion Spawnrotation;
        [System.NonSerializedAttribute] public int OutsideVehicleLayer;
        [System.NonSerializedAttribute] public bool DoAAMTargeting;
        [System.NonSerializedAttribute] public Rigidbody GDHitRigidbody;
        [System.NonSerializedAttribute] public bool UsingManualSync;
        private bool RepeatingWorldCheckAxis;
        bool FloatingLastFrame = false;
        bool GroundedLastFrame = false;
        [System.NonSerializedAttribute] public float VTOLAngleDegrees;
        private int VehicleLayer;
        private float HandDistanceZLastFrame;
        private float EngineOutputLastFrame;
        bool HasWheelColliders = false;
        [System.NonSerializedAttribute] public WheelCollider[] VehicleWheelColliders;
        [System.NonSerializedAttribute] public bool LowFuelLastFrame;
        [System.NonSerializedAttribute] public bool NoFuelLastFrame;
        [System.NonSerializedAttribute] public float ThrottleStrengthAB;
        [System.NonSerializedAttribute] public float FuelConsumptionAB;
        [System.NonSerializedAttribute] public bool AfterburnerOn;
        [System.NonSerializedAttribute] public bool PitchDown;//air is hitting plane from the top
        private float GDamageToTake;
        [System.NonSerializedAttribute] public float LastHitTime = -100;
        [System.NonSerializedAttribute] public float PredictedHealth;


        [System.NonSerializedAttribute] public int NumActiveFlares;
        [System.NonSerializedAttribute] public int NumActiveChaff;
        [System.NonSerializedAttribute] public int NumActiveOtherCM;
        [System.NonSerializedAttribute] public bool UseAtmospherePositionOffset = false;
        [System.NonSerializedAttribute] public float AtmospherePositionOffset = 0;//set UseAtmospherePositionOffset true to use this for floating origin system
        [System.NonSerializedAttribute] public float Limits = 1;//specially used by limits function
                                                                //this stuff can be used by DFUNCs
                                                                //if these == 0 then they are not disabled. Being an int allows more than one extension to disable it at a time
                                                                //the bools exists to save externs every frame
        [System.NonSerializedAttribute] public bool _DisablePhysicsAndInputs;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(DisablePhysicsAndInputs_))] public int DisablePhysicsAndInputs = 0;
        public int DisablePhysicsAndInputs_
        {
            set
            {
                if (value > 0 && DisablePhysicsAndInputs == 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_DisablePhysicsAndInputs_Activated");
                }
                else if (value == 0 && DisablePhysicsAndInputs > 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_DisablePhysicsAndInputs_Deactivated");
                }
                _DisablePhysicsAndInputs = value > 0;
                DisablePhysicsAndInputs = value;
            }
            get => DisablePhysicsAndInputs;
        }
        [System.NonSerializedAttribute] public bool _DisableGroundDetection;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(DisableGroundDetection_))] public int DisableGroundDetection = 0;
        public int DisableGroundDetection_
        {
            set
            {
                if (value > 0 && DisableGroundDetection == 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_DisableGroundDetection_Activated");
                }
                else if (value == 0 && DisableGroundDetection > 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_DisableGroundDetection_Deactivated");
                }
                _DisableGroundDetection = value > 0;
                DisableGroundDetection = value;
            }
            get => DisableGroundDetection;
        }
        [System.NonSerializedAttribute] public bool _ThrottleOverridden;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(ThrottleOverridden_))] public int ThrottleOverridden = 0;
        public int ThrottleOverridden_
        {
            set
            {
                if (value > 0 && ThrottleOverridden == 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_ThrottleOverridden_Activated");
                }
                else if (value == 0 && ThrottleOverridden > 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_ThrottleOverridden_Deactivated");
                }
                _ThrottleOverridden = value > 0;
                ThrottleOverridden = value;
            }
            get => ThrottleOverridden;
        }
        [System.NonSerializedAttribute] public float ThrottleOverride;
        [System.NonSerializedAttribute] public bool _JoystickOverridden;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(JoystickOverridden_))] public int JoystickOverridden = 0;
        public int JoystickOverridden_
        {
            set
            {
                if (value > 0 && JoystickOverridden == 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_JoystickOverridden_Activated");
                }
                else if (value == 0 && JoystickOverridden > 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_JoystickOverridden_Deactivated");
                }
                _JoystickOverridden = value > 0;
                JoystickOverridden = value;
            }
            get => JoystickOverridden;
        }
        [System.NonSerializedAttribute] public Vector3 JoystickOverride;


        [System.NonSerializedAttribute] public int ReSupplied = 0;
        public void SFEXT_L_EntityStart()
        {
            Initialized = true;
            VehicleGameObj = EntityControl.gameObject;
            VehicleTransform = EntityControl.transform;
            VehicleRigidbody = EntityControl.GetComponent<Rigidbody>();
            VehicleObjectSync = (VRC.SDK3.Components.VRCObjectSync)VehicleGameObj.GetComponent(typeof(VRC.SDK3.Components.VRCObjectSync));
            if (VehicleObjectSync == null)
            {
                UsingManualSync = true;
            }

            localPlayer = Networking.LocalPlayer;
            if (localPlayer == null)
            {
                Piloting = true;
                Occupied = true;
                if (!UsingManualSync)
                {
                    VehicleRigidbody.drag = 0;
                    VehicleRigidbody.angularDrag = 0;
                }
            }
            else
            {
                InVR = localPlayer.IsUserInVR();
                if (localPlayer.isMaster)
                {
                    if (!UsingManualSync)
                    {
                        VehicleRigidbody.drag = 0;
                        VehicleRigidbody.angularDrag = 0;
                    }
                }
                else
                {
                    if (!UsingManualSync)
                    {
                        VehicleRigidbody.drag = 9999;
                        VehicleRigidbody.angularDrag = 9999;
                    }
                }
            }
            InEditor = EntityControl.InEditor;
            IsOwner = EntityControl.IsOwner;

            VehicleWheelColliders = VehicleMesh.GetComponentsInChildren<WheelCollider>(true);
            if (VehicleWheelColliders.Length != 0) { HasWheelColliders = true; }

            VehicleLayer = VehicleMesh.gameObject.layer;//get the layer of the vehicle as set by the world creator
            OutsideVehicleLayer = VehicleMesh.gameObject.layer;
            VehicleAnimator = EntityControl.GetComponent<Animator>();

            FullHealth = Health;
            FullFuel = Fuel;

            CenterOfMass = EntityControl.CenterOfMass;
            SetCoMMeshOffset();

            if (AtmosphereThinningStart > AtmosphereThinningEnd) { AtmosphereThinningEnd = (AtmosphereThinningStart + 1); }
            AtmoshpereFadeDistance = (AtmosphereThinningEnd + SeaLevel) - (AtmosphereThinningStart + SeaLevel); //for finding atmosphere thinning gradient
            AtmosphereHeightThing = (AtmosphereThinningStart + SeaLevel) / (AtmoshpereFadeDistance); //used to add back the height to the atmosphere after finding gradient

            if (GroundDetectorRayDistance == 0 || !GroundDetector)
            { DisableGroundDetection_++; }

            LowFuelDivider = 1 / LowFuel;

            if (!ControlsRoot)
            { ControlsRoot = VehicleTransform; }
        }
        private void LateUpdate()
        {
            float DeltaTime = Time.deltaTime;
            if (IsOwner)//works in editor or ingame
            {
                if (!EntityControl.dead)
                {
                    //G/crash Damage
                    Health -= Mathf.Max((GDamageToTake) * DeltaTime * GDamage, 0f);//take damage of GDamage per second per G above MaxGs
                    GDamageToTake = 0;
                    if (Health <= 0f)//vehicle is ded
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Explode));
                        return;
                    }
                }
                else { GDamageToTake = 0; }

                if (Floating)
                {
                    if (!FloatingLastFrame)
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(TouchDownWater));
                    }
                }
                else
                { FloatingLastFrame = false; }
                if (!_DisableGroundDetection)
                {
                    RaycastHit GDHit;
                    if ((Physics.Raycast(GroundDetector.position, -GroundDetector.up, out GDHit, GroundDetectorRayDistance, GroundDetectorLayers, QueryTriggerInteraction.Ignore)))
                    {
                        if (!GroundedLastFrame)
                        {
                            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(TouchDown));
                        }
                    }
                    else
                    {
                        GroundedLastFrame = false;
                        GDHitRigidbody = null;
                    }
                }
                if (Taxiing && !GroundedLastFrame && !FloatingLastFrame)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(TakeOff));
                }

                //synced variables because rigidbody values aren't accessable by non-owner players
                CurrentVel = VehicleRigidbody.velocity;//CurrentVel is set by SAV_SyncScript for non owners
                Speed = CurrentVel.magnitude;
                bool VehicleMoving = false;
                if (Speed > .1f)//don't bother doing all this for vehicles that arent moving and it therefore wont even effect
                {
                    VehicleMoving = true;//check this bool later for more optimizations
                    WindAndAoA();
                }

                if (Piloting)
                {
                    //gotta do these this if we're piloting but it didn't get done(specifically, hovering extremely slowly in a VTOL craft will cause control issues we don't)
                    if (!VehicleMoving)
                    { WindAndAoA(); VehicleMoving = true; }
                    DoRepeatingWorld();

                    if (!_DisablePhysicsAndInputs)
                    {
                        //collect inputs
                        int Wi = Input.GetKey(KeyCode.W) ? 1 : 0; //inputs as ints
                        int Si = Input.GetKey(KeyCode.S) ? -1 : 0;
                        int Ai = Input.GetKey(KeyCode.A) ? -1 : 0;
                        int Di = Input.GetKey(KeyCode.D) ? 1 : 0;
                        int Qi = Input.GetKey(KeyCode.Q) ? -1 : 0;
                        int Ei = Input.GetKey(KeyCode.E) ? 1 : 0;
                        int upi = Input.GetKey(KeyCode.UpArrow) ? 1 : 0;
                        int downi = Input.GetKey(KeyCode.DownArrow) ? -1 : 0;
                        int lefti = Input.GetKey(KeyCode.LeftArrow) ? -1 : 0;
                        int righti = Input.GetKey(KeyCode.RightArrow) ? 1 : 0;
                        bool Shift = Input.GetKey(KeyCode.LeftShift);
                        bool Ctrl = Input.GetKey(KeyCode.LeftControl);
                        int Shifti = Shift ? 1 : 0;
                        int LeftControli = Ctrl ? 1 : 0;
                        float LGrip = 0;
                        float RGrip = 0;
                        if (!InEditor)
                        {
                            LGrip = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryHandTrigger");
                            RGrip = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
                        }
                        //MouseX = Input.GetAxisRaw("Mouse X");
                        //MouseY = Input.GetAxisRaw("Mouse Y");
                        Vector3 VRJoystickPos = Vector3.zero;

                        float ThrottleGrip;
                        float JoyStickGrip;
                        if (SwitchHandsJoyThrottle)
                        {
                            JoyStickGrip = LGrip;
                            ThrottleGrip = RGrip;
                        }
                        else
                        {
                            ThrottleGrip = LGrip;
                            JoyStickGrip = RGrip;
                        }
                        //VR Joystick
                        if (JoyStickGrip > GripSensitivity)
                        {
                            Quaternion VehicleRotDif = ControlsRoot.rotation * Quaternion.Inverse(VehicleRotLastFrame);//difference in vehicle's rotation since last frame
                            VehicleRotLastFrame = ControlsRoot.rotation;
                            JoystickZeroPoint = VehicleRotDif * JoystickZeroPoint;//zero point rotates with the vehicle so it appears still to the pilot
                            if (!JoystickGripLastFrame)//first frame you gripped joystick
                            {
                                EntityControl.SendEventToExtensions("SFEXT_O_JoystickGrabbed");
                                VehicleRotDif = Quaternion.identity;
                                if (SwitchHandsJoyThrottle)
                                {
                                    JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation;
                                    localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, .05f, .07f, 35);
                                }//rotation of the controller relative to the plane when it was pressed
                                else
                                {
                                    JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;
                                    localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, .05f, .07f, 35);
                                }
                            }
                            JoystickGripLastFrame = true;
                            //difference between the vehicle and the hand's rotation, and then the difference between that and the JoystickZeroPoint, finally rotated by the vehicles rotation to turn it back to vehicle space
                            Quaternion JoystickDifference;
                            JoystickDifference = Quaternion.Inverse(ControlsRoot.rotation) *
                                (SwitchHandsJoyThrottle ? localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation
                                                        : localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation)
                            * Quaternion.Inverse(JoystickZeroPoint)
                             * ControlsRoot.rotation;

                            //create normalized vectors facing towards the 'forward' and 'up' directions of the joystick
                            Vector3 JoystickPosYaw = (JoystickDifference * Vector3.forward);
                            Vector3 JoystickPos = (JoystickDifference * Vector3.up);
                            //use acos to convert the relevant elements of the array into radians, re-center around zero, then normalize between -1 and 1 and dovide for desired deflection
                            //the clamp is there because rotating a vector3 can cause it to go a miniscule amount beyond length 1, resulting in NaN (crashes vrc)
                            VRJoystickPos.x = -((Mathf.Acos(Mathf.Clamp(JoystickPos.z, -1, 1)) - 1.5707963268f) * Mathf.Rad2Deg) / MaxJoyAngles.x;
                            VRJoystickPos.y = -((Mathf.Acos(Mathf.Clamp(JoystickPosYaw.x, -1, 1)) - 1.5707963268f) * Mathf.Rad2Deg) / MaxJoyAngles.y;
                            VRJoystickPos.z = -((Mathf.Acos(Mathf.Clamp(JoystickPos.x, -1, 1)) - 1.5707963268f) * Mathf.Rad2Deg) / MaxJoyAngles.z;
                        }
                        else
                        {
                            VRJoystickPos = Vector3.zero;
                            if (JoystickGripLastFrame)//first frame you let go of joystick
                            {
                                EntityControl.SendEventToExtensions("SFEXT_O_JoystickDropped");
                                if (SwitchHandsJoyThrottle)
                                { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, .05f, .07f, 35); }
                                else
                                { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, .05f, .07f, 35); }
                            }
                            JoystickGripLastFrame = false;
                        }
                        PlayerThrottle = Mathf.Clamp(PlayerThrottle + ((Shifti - LeftControli) * .5f * DeltaTime), 0, 1f);
                        //VR Throttle
                        if (ThrottleGrip > GripSensitivity)
                        {
                            Vector3 handdistance;
                            if (SwitchHandsJoyThrottle)
                            {
                                handdistance = ControlsRoot.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
                            }
                            else
                            {
                                handdistance = ControlsRoot.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
                            }
                            handdistance = ControlsRoot.InverseTransformDirection(handdistance);

                            float HandThrottleAxis;
                            if (VerticalThrottle)
                            {
                                HandThrottleAxis = handdistance.y;
                            }
                            else
                            {
                                HandThrottleAxis = handdistance.z;
                            }

                            if (!ThrottleGripLastFrame)
                            {
                                if (SwitchHandsJoyThrottle)
                                { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, .05f, .07f, 35); }
                                else
                                { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, .05f, .07f, 35); }
                                EntityControl.SendEventToExtensions("SFEXT_O_ThrottleGrabbed");
                                ThrottleZeroPoint = HandThrottleAxis;
                                TempThrottle = PlayerThrottle;
                                HandDistanceZLastFrame = 0;
                            }
                            float ThrottleDifference = ThrottleZeroPoint - HandThrottleAxis;
                            ThrottleDifference *= ThrottleSensitivity;

                            //Detent function to prevent you going into afterburner by accident (bit of extra force required to turn on AB (actually hand speed))
                            if (((HandDistanceZLastFrame - HandThrottleAxis) * ThrottleSensitivity > .05f)/*detent overcome*/ && Fuel > LowFuel)
                            {
                                PlayerThrottle = Mathf.Clamp(TempThrottle + ThrottleDifference, 0, 1);
                            }
                            HandDistanceZLastFrame = HandThrottleAxis;
                            ThrottleGripLastFrame = true;
                        }
                        else
                        {
                            if (ThrottleGripLastFrame)
                            {
                                EntityControl.SendEventToExtensions("SFEXT_O_ThrottleDropped");
                                if (SwitchHandsJoyThrottle)
                                { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, .05f, .07f, 35); }
                                else
                                { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, .05f, .07f, 35); }
                            }
                            ThrottleGripLastFrame = false;
                        }
                        if (_ThrottleOverridden)
                        {
                            ThrottleInput = PlayerThrottle = ThrottleOverride;
                        }
                        else//if cruise control disabled, use inputs
                        {
                            if (!InVR)
                            {
                                float LTrigger = 0;
                                float RTrigger = 0;
                                if (!InEditor)
                                {
                                    LTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
                                    RTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
                                }
                                if (LTrigger > .05f)//axis throttle input for people who wish to use it //.05 deadzone so it doesn't take effect for keyboard users with something plugged in
                                { ThrottleInput = LTrigger; }
                                else { ThrottleInput = PlayerThrottle; }
                            }
                            else { ThrottleInput = PlayerThrottle; }
                        }
                        FuelEvents();

                        if (_JoystickOverridden)//joystick override enabled, and player not holding joystick
                        {
                            RotationInputs = JoystickOverride;
                        }
                        else//joystick override disabled, player has control
                        {
                            if (!InVR)
                            {
                                //allow stick flight in desktop mode
                                Vector2 LStickPos = new Vector2(0, 0);
                                Vector2 RStickPos = new Vector2(0, 0);
                                if (!InEditor)
                                {
                                    LStickPos.x = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
                                    LStickPos.y = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
                                    RStickPos.x = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
                                    //RStickPos.y = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
                                }
                                VRJoystickPos.x = LStickPos.y;
                                VRJoystickPos.y = RStickPos.x;
                                VRJoystickPos.z = LStickPos.x;
                                //make stick input square
                                if (SquareJoyInput)
                                {
                                    Vector2 LeftStick = new Vector2(VRJoystickPos.z, VRJoystickPos.x);
                                    if (Mathf.Abs(LeftStick.x) > Mathf.Abs(LeftStick.y))
                                    {
                                        if (Mathf.Abs(LeftStick.x) > 0)
                                        {
                                            float temp = LeftStick.magnitude / Mathf.Abs(LeftStick.x);
                                            LeftStick *= temp;
                                        }
                                    }
                                    else if (Mathf.Abs(LeftStick.y) > 0)
                                    {
                                        float temp = LeftStick.magnitude / Mathf.Abs(LeftStick.y);
                                        LeftStick *= temp;
                                    }
                                    VRJoystickPos.z = LeftStick.x;
                                    VRJoystickPos.x = LeftStick.y;
                                }
                            }

                            RotationInputs.x = Mathf.Clamp(VRJoystickPos.x + Wi + Si + downi + upi, -1, 1) * Limits;
                            RotationInputs.y = Mathf.Clamp(Qi + Ei + VRJoystickPos.y, -1, 1) * Limits;
                            //roll isn't subject to flight limits
                            RotationInputs.z = Mathf.Clamp(((VRJoystickPos.z + Ai + Di + lefti + righti) * -1), -1, 1);
                        }

                    }
                }
                else
                {
                    if (Taxiing)
                    {
                        StillWindMulti = Mathf.Min(Speed * .1f, 1);
                    }
                    else { StillWindMulti = 1; }
                    if (_EngineOn)
                    {
                        //allow remote piloting using extensions?
                        if (_ThrottleOverridden)
                        { ThrottleInput = PlayerThrottle = ThrottleOverride; }
                        FuelEvents();
                    }
                    DoRepeatingWorld();
                }

                if (!_DisablePhysicsAndInputs)
                {
                    //Lerp the inputs for 'engine response', throttle decrease response is slower than increase (EngineSpoolDownSpeedMulti)
                    if (_EngineOn)
                    {
                        if (EngineOutput < ThrottleInput)
                        { EngineOutput = Mathf.Lerp(EngineOutput, ThrottleInput, AccelerationResponse * DeltaTime); }
                        else
                        { EngineOutput = Mathf.Lerp(EngineOutput, ThrottleInput, AccelerationResponse * EngineSpoolDownSpeedMulti * DeltaTime); }
                    }
                    else
                    {
                        EngineOutput = Mathf.Lerp(EngineOutput, 0, AccelerationResponse * EngineSpoolDownSpeedMulti * DeltaTime);
                    }
                    float sidespeed = 0;
                    float downspeed = 0;

                    if (VehicleMoving)//optimization
                    {
                        //used to create air resistance for updown and sideways if your movement direction is in those directions
                        //to add physics to plane's yaw and pitch, accel angvel towards velocity, and add force to the plane
                        //and add wind
                        sidespeed = Vector3.Dot(AirVel, VehicleTransform.right);
                        downspeed = -Vector3.Dot(AirVel, VehicleTransform.up);
                        PitchDown = downspeed < 0;//air is hitting plane from above?
                    }
                }

            }
            else//non-owners need to know these values
            {
                Speed = AirSpeed = CurrentVel.magnitude;//wind speed is local anyway, so just use ground speed for non-owners
                                                        //AirVel = VehicleRigidbody.velocity - Wind;//wind isn't synced so this will be wrong
                                                        //AirSpeed = AirVel.magnitude;
            }
        }
        private void FixedUpdate()
        {
            if (IsOwner)
            {
                float DeltaTime = Time.fixedDeltaTime;
                //lerp velocity toward 0 to simulate air friction
                Vector3 VehicleVel = VehicleRigidbody.velocity;

                //calc Gs
                float gravity = 9.81f * DeltaTime;
                LastFrameVel.y -= gravity; //add gravity
                AllGs = Vector3.Distance(LastFrameVel, VehicleVel) / gravity;
                GDamageToTake += Mathf.Max((AllGs - MaxGs), 0);

                Vector3 Gs3 = VehicleTransform.InverseTransformDirection(VehicleVel - LastFrameVel);
                VertGs = Gs3.y / gravity;
                LastFrameVel = VehicleVel;
            }
        }
        public void Explode()//all the things players see happen when the vehicle explodes
        {
            if (EntityControl.dead) { return; }//can happen with prediction enabled if two people kill something at the same time
            EntityControl.dead = true;
            EngineOn = false;
            PlayerThrottle = 0;
            ThrottleInput = 0;
            EngineOutput = 0;
            MissilesIncomingHeat = 0;
            MissilesIncomingRadar = 0;
            MissilesIncomingOther = 0;
            Fuel = FullFuel;
            Atmosphere = 1;//vehiclemoving optimization requires this to be here

            EntityControl.SendEventToExtensions("SFEXT_G_Explode");

            SendCustomEventDelayedSeconds(nameof(ReAppear), RespawnDelay);
            SendCustomEventDelayedSeconds(nameof(NotDead), RespawnDelay + InvincibleAfterSpawn);

            if (IsOwner)
            {
                VehicleConstantForce.relativeForce = Vector3.zero;
                VehicleConstantForce.relativeTorque = Vector3.zero;
                VehicleRigidbody.velocity = Vector3.zero;
                VehicleRigidbody.angularVelocity = Vector3.zero;
                if (!UsingManualSync)
                {
                    VehicleRigidbody.drag = 9999;
                    VehicleRigidbody.angularDrag = 9999;
                }
                Health = FullHealth;//turns off low health smoke
                Fuel = FullFuel;
                SendCustomEventDelayedSeconds(nameof(MoveToSpawn), RespawnDelay - 3);
                EntityControl.SendEventToExtensions("SFEXT_O_Explode");
            }

            //pilot and passengers are dropped out of the vehicle
            if ((Piloting || Passenger) && !InEditor)
            {
                EntityControl.ExitStation();
            }
            if (LowFuelLastFrame)
            { SendNotLowFuel(); }
            if (NoFuelLastFrame)
            { SendNotNoFuel(); }
        }
        public void SFEXT_O_OnPlayerJoined()
        {
            if (GroundedLastFrame)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(TouchDown));
            }
            if (FloatingLastFrame)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(TouchDownWater));
            }
            //sync engine status
            if (_EngineOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetEngineOn)); }
        }
        public void ReAppear()
        {
            EntityControl.SendEventToExtensions("SFEXT_G_ReAppear");
            if (IsOwner)
            {
                if (!UsingManualSync)
                {
                    VehicleRigidbody.drag = 0;
                    VehicleRigidbody.angularDrag = 0;
                }
                VehicleConstantForce.relativeForce = Vector3.zero;
                VehicleConstantForce.relativeTorque = Vector3.zero;
            }
        }
        public void NotDead()
        {
            Health = FullHealth;
            EntityControl.dead = false;
        }
        public void MoveToSpawn()
        {
            PlayerThrottle = 0;//for editor test mode
            EngineOutput = 0;//^
            VehicleRigidbody.angularVelocity = Vector3.zero;
            VehicleRigidbody.velocity = Vector3.zero;
            //these could get set after death by lag, probably
            MissilesIncomingHeat = 0;
            MissilesIncomingRadar = 0;
            MissilesIncomingOther = 0;
            Health = FullHealth;
            if (InEditor || UsingManualSync)
            {
                VehicleTransform.localPosition = Spawnposition;
                VehicleTransform.localRotation = Spawnrotation;
            }
            else
            {
                VehicleObjectSync.Respawn();
            }
            EntityControl.SendEventToExtensions("SFEXT_O_MoveToSpawn");
        }
        public void SFEXT_L_CoMSet()
        {
            if (Initialized)
            { SetCoMMeshOffset(); }
        }
        public void SetCoMMeshOffset()
        {
            //move objects to so that the vehicle's main pivot is at the CoM so that syncscript's rotation is smoother
            Vector3 CoMOffset = CenterOfMass.position - VehicleTransform.position;
            int c = VehicleTransform.childCount;
            Transform[] MainObjChildren = new Transform[c];
            for (int i = 0; i < c; i++)
            {
                VehicleTransform.GetChild(i).position -= CoMOffset;
            }
            VehicleTransform.position += CoMOffset;
            SendCustomEventDelayedSeconds(nameof(SetCoM_ITR), Time.fixedDeltaTime);//this has to be delayed because ?
            Spawnposition = VehicleTransform.localPosition;
            Spawnrotation = VehicleTransform.localRotation;
        }
        public void SetCoM_ITR()
        {
            VehicleRigidbody.centerOfMass = VehicleTransform.InverseTransformDirection(CenterOfMass.position - VehicleTransform.position);//correct position if scaled
        }
        public void FuelEvents()
        {
            if (_EngineOn)
            {
                Vector2 Throttles = UnpackThrottles(ThrottleInput);
                Fuel = Mathf.Max(Fuel -
                        ((Mathf.Max(Throttles.x, MinFuelConsumption) * FuelConsumption)
                            + (Throttles.y * FuelConsumptionAB))
                                * Time.deltaTime, 0);
            }
            if (Fuel < LowFuel)
            {
                //max throttle scales down with amount of fuel below LowFuel
                ThrottleInput = Mathf.Min(ThrottleInput, Fuel * LowFuelDivider);
                if (!LowFuelLastFrame)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendLowFuel));
                }
                if (Fuel == 0 && !NoFuelLastFrame)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendNoFuel));
                }
            }
            else
            {
                if (LowFuelLastFrame)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendNotLowFuel));
                }
            }
            if (NoFuelLastFrame)
            {
                if (Fuel > 0)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendNotNoFuel));
                }
            }
        }
        public void DoRepeatingWorld()
        {
            if (RepeatingWorld)
            {
                if (RepeatingWorldCheckAxis)
                {
                    if (Mathf.Abs(CenterOfMass.position.z) > RepeatingWorldDistance)
                    {
                        if (CenterOfMass.position.z > 0)
                        {
                            Vector3 vehpos = VehicleTransform.position;
                            vehpos.z -= RepeatingWorldDistance * 2;
                            VehicleTransform.position = vehpos;
                        }
                        else
                        {
                            Vector3 vehpos = VehicleTransform.position;
                            vehpos.z += RepeatingWorldDistance * 2;
                            VehicleTransform.position = vehpos;
                        }
                    }
                }
                else
                {
                    if (Mathf.Abs(CenterOfMass.position.x) > RepeatingWorldDistance)
                    {
                        if (CenterOfMass.position.x > 0)
                        {
                            Vector3 vehpos = VehicleTransform.position;
                            vehpos.x -= RepeatingWorldDistance * 2;
                            VehicleTransform.position = vehpos;
                        }
                        else
                        {
                            Vector3 vehpos = VehicleTransform.position;
                            vehpos.x += RepeatingWorldDistance * 2;
                            VehicleTransform.position = vehpos;
                        }
                    }
                }
                RepeatingWorldCheckAxis = !RepeatingWorldCheckAxis;//Check one axis per frame
            }
        }
        public void TouchDown()
        {
            //Debug.Log("TouchDown");
            if (GroundedLastFrame) { return; }
            GroundedLastFrame = true;
            Taxiing = true;
            EntityControl.SendEventToExtensions("SFEXT_G_TouchDown");
        }
        public void TouchDownWater()
        {
            //Debug.Log("TouchDownWater");
            if (FloatingLastFrame) { return; }
            FloatingLastFrame = true;
            Taxiing = true;
            EntityControl.SendEventToExtensions("SFEXT_G_TouchDownWater");
        }
        public void TakeOff()
        {
            //Debug.Log("TakeOff");
            Taxiing = false;
            FloatingLastFrame = false;
            GroundedLastFrame = false;
            EntityControl.SendEventToExtensions("SFEXT_G_TakeOff");
        }
        public void SetAfterburnerOn()
        {
            if (!AfterburnerOn)
            {
                AfterburnerOn = true;
                EntityControl.SendEventToExtensions("SFEXT_G_AfterburnerOn");
            }
        }
        public void SetAfterburnerOff()
        {
            if (AfterburnerOn)
            {
                AfterburnerOn = false;
                EntityControl.SendEventToExtensions("SFEXT_G_AfterburnerOff");
            }
        }
        public void SendLowFuel()
        {
            LowFuelLastFrame = true;
            EntityControl.SendEventToExtensions("SFEXT_G_LowFuel");
        }
        public void SendNotLowFuel()
        {
            LowFuelLastFrame = false;
            EntityControl.SendEventToExtensions("SFEXT_G_NotLowFuel");
        }
        public void SendNoFuel()
        {
            NoFuelLastFrame = true;
            EntityControl.SendEventToExtensions("SFEXT_G_NoFuel");
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetEngineOff));
        }
        public void SendNotNoFuel()
        {
            NoFuelLastFrame = false;
            EntityControl.SendEventToExtensions("SFEXT_G_NotNoFuel");
            if (EngineOnOnEnter && Occupied)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetEngineOn));
            }
        }
        public void SFEXT_O_ReSupply()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ReSupply));
        }
        public void ReSupply()
        {
            ReSupplied = 0;//used to know if other scripts resupplied
            if ((Fuel < FullFuel - 10 || Health != FullHealth))
            {
                ReSupplied++;//used to only play the sound if we're actually repairing/getting ammo/fuel
            }
            EntityControl.SendEventToExtensions("SFEXT_G_ReSupply");//extensions increase the ReSupplied value too

            LastResupplyTime = Time.time;

            if (IsOwner)
            {
                Fuel = Mathf.Min(Fuel + (FullFuel / RefuelTime), FullFuel);
                Health = Mathf.Min(Health + (FullHealth / RepairTime), FullHealth);
                if (LowFuelLastFrame && Fuel > LowFuel)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendNotLowFuel));
                }
                if (NoFuelLastFrame && Fuel > 0)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendNotNoFuel));
                }
            }
        }
        public void SFEXT_O_RespawnButton()//called when using respawn button
        {
            if (!Occupied && !EntityControl.dead)
            {
                Networking.SetOwner(localPlayer, EntityControl.gameObject);
                EntityControl.TakeOwnerShipOfExtensions();
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ResetStatus));
                IsOwner = true;
                Atmosphere = 1;//vehiclemoving optimization requires this to be here
                               //synced variables
                Health = FullHealth;
                Fuel = FullFuel;

                if (InEditor || UsingManualSync)
                {
                    VehicleTransform.localPosition = Spawnposition;
                    VehicleTransform.localRotation = Spawnrotation;
                    VehicleRigidbody.velocity = Vector3.zero;
                }
                else
                {
                    VehicleObjectSync.Respawn();
                }
                VehicleRigidbody.angularVelocity = Vector3.zero;//editor needs this
            }
        }
        public void ResetStatus()//called globally when using respawn button
        {
            if (_EngineOn)
            {
                EngineOn = false;
                PlayerThrottle = ThrottleInput = EngineOutputLastFrame = EngineOutput = 0;
            }
            //these two make it invincible and unable to be respawned again for 5s
            EntityControl.dead = true;
            SendCustomEventDelayedSeconds(nameof(NotDead), InvincibleAfterSpawn);
            if (LowFuelLastFrame)
            { SendNotLowFuel(); }
            if (NoFuelLastFrame)
            { SendNotNoFuel(); }
            EntityControl.SendEventToExtensions("SFEXT_G_RespawnButton");
            VehicleConstantForce.relativeForce = Vector3.zero;
            VehicleConstantForce.relativeTorque = Vector3.zero;
        }
        public void SFEXT_L_BulletHit()
        {
            if (PredictDamage)
            {
                if (Time.time - LastHitTime > 2)
                {
                    PredictedHealth = Health - (BulletDamageTaken * EntityControl.LastHitBulletDamageMulti);
                    if (PredictedHealth <= 0)
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Explode));
                    }
                }
                else
                {
                    PredictedHealth -= BulletDamageTaken * EntityControl.LastHitBulletDamageMulti;
                    if (PredictedHealth <= 0)
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Explode));
                    }
                }
                LastHitTime = Time.time;
            }
        }
        public void SFEXT_G_BulletHit()
        {
            if (!EntityControl.dead)
            {
                LastHitTime = Time.time;
                if (IsOwner)
                {
                    Health -= BulletDamageTaken * EntityControl.LastHitBulletDamageMulti;
                    if (PredictDamage && Health <= 0)//the attacker calls the explode function in this case
                    {
                        Health = 0.0911f;
                        //if two people attacked us, and neither predicted they killed us but we took enough damage to die, we must still die.
                        SendCustomEventDelayedSeconds(nameof(CheckLaggyKilled), .25f);//give enough time for the explode event to happen if they did predict we died, otherwise do it ourself
                    }
                }
            }
        }
        public void CheckLaggyKilled()
        {
            if (!EntityControl.dead)
            {
                //Check if we still have the amount of health set to not send explode when killed, and if we do send explode
                if (Health == 0.0911f)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Explode));
                }
            }
        }
        //Add .001 to each value of damage taken to prevent float comparison bullshit
        public void SFEXT_L_MissileHit25()
        {
            if (PredictDamage && !EntityControl.dead)
            { MissileDamagePrediction(.251f); }
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendMissileHit25));
        }
        public void SendMissileHit25()
        {
            EntityControl.SendEventToExtensions("SFEXT_G_MissileHit25");
        }
        public void SFEXT_G_MissileHit25()
        {
            if (IsOwner)
            { TakeMissileDamage(.251f); }
            LastHitTime = Time.time;
        }
        public void SFEXT_L_MissileHit50()
        {
            if (PredictDamage && !EntityControl.dead)
            { MissileDamagePrediction(.501f); }
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendMissileHit50));
        }
        public void SendMissileHit50()
        {
            EntityControl.SendEventToExtensions("SFEXT_G_MissileHit50");
        }
        public void SFEXT_G_MissileHit50()
        {
            if (IsOwner)
            { TakeMissileDamage(.501f); }
            LastHitTime = Time.time;
        }
        public void SFEXT_L_MissileHit75()
        {
            if (PredictDamage && !EntityControl.dead)
            { MissileDamagePrediction(.751f); }
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendMissileHit75));
        }
        public void SendMissileHit75()
        {
            EntityControl.SendEventToExtensions("SFEXT_G_MissileHit75");
        }
        public void SFEXT_G_MissileHit75()
        {
            if (IsOwner)
            { TakeMissileDamage(.751f); }
            LastHitTime = Time.time;
        }
        public void SFEXT_L_MissileHit100()
        {
            if (PredictDamage && !EntityControl.dead)
            { MissileDamagePrediction(1.001f); }
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendMissileHit100));
        }
        public void SendMissileHit100()
        {
            EntityControl.SendEventToExtensions("SFEXT_G_MissileHit100");
        }
        public void SFEXT_G_MissileHit100()
        {
            if (IsOwner)
            { TakeMissileDamage(1.001f); }
            LastHitTime = Time.time;
        }
        public void TakeMissileDamage(float damage)
        {
            Health -= ((FullHealth * damage) * MissileDamageTakenMultiplier);
            if (PredictDamage && !EntityControl.dead && Health <= 0)
            { Health = 0.1f; }//the attacker calls the explode function in this case
            Vector3 explosionforce = new Vector3(Random.Range(-MissilePushForce, MissilePushForce), Random.Range(-MissilePushForce, MissilePushForce), Random.Range(-MissilePushForce, MissilePushForce)) * damage;
            VehicleRigidbody.AddTorque(explosionforce, ForceMode.VelocityChange);
        }
        private void MissileDamagePrediction(float Damage)
        {
            if (Time.time - LastHitTime > 2)
            {
                PredictedHealth = Health - ((FullHealth * Damage) * MissileDamageTakenMultiplier);
                if (PredictedHealth <= 0)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Explode));
                }
            }
            else
            {
                PredictedHealth -= ((FullHealth * Damage) * MissileDamageTakenMultiplier);
                if (PredictedHealth <= 0)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Explode));
                }
            }
        }
        public void SFEXT_P_PassengerEnter()
        {
            Passenger = true;
            SetCollidersLayer(OnboardVehicleLayer);
        }
        public void SFEXT_P_PassengerExit()
        {
            Passenger = false;
            localPlayer.SetVelocity(CurrentVel);
            MissilesIncomingHeat = 0;
            MissilesIncomingRadar = 0;
            MissilesIncomingOther = 0;
            SetCollidersLayer(VehicleLayer);
        }
        public void SFEXT_O_TakeOwnership()
        {
            IsOwner = true;
            VehicleRigidbody.velocity = CurrentVel;
            if (_EngineOn && EntityControl.Piloting)
            { PlayerThrottle = ThrottleInput = EngineOutputLastFrame = EngineOutput; }
            else
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetEngineOff));
                PlayerThrottle = ThrottleInput = EngineOutputLastFrame = EngineOutput = 0;
            }
            if (!UsingManualSync)
            {
                VehicleRigidbody.drag = 0;
                VehicleRigidbody.angularDrag = 0;
            }
        }
        public void SFEXT_O_LoseOwnership()
        {
            IsOwner = false;
            if (!UsingManualSync)
            {
                VehicleRigidbody.drag = 9999;
                VehicleRigidbody.angularDrag = 9999;
            }
        }
        public void SFEXT_O_PilotEnter()
        {
            //setting this as a workaround because it doesnt work reliably in Start()
            if (!InEditor)
            {
                InVR = localPlayer.IsUserInVR();
            }
            GDHitRigidbody = null;
            if (_EngineOn)
            { PlayerThrottle = ThrottleInput = EngineOutputLastFrame = EngineOutput; }
            else
            { PlayerThrottle = ThrottleInput = EngineOutputLastFrame = EngineOutput = 0; }

            Piloting = true;
            if (EntityControl.dead) { Health = FullHealth; }//dead is true for the first 5 seconds after spawn, this might help with spontaneous explosions

            //hopefully prevents explosions when you enter the vehicle
            VehicleRigidbody.velocity = CurrentVel;
            VertGs = 0;
            AllGs = 0;
            LastFrameVel = CurrentVel;
            if (EngineOnOnEnter && Fuel > 0)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetEngineOn));
            }

            SetCollidersLayer(OnboardVehicleLayer);
        }
        public void SFEXT_G_PilotEnter()
        {
            Occupied = true;
            EntityControl.dead = false;//vehicle stops being invincible if someone gets in, also acts as redundancy incase someone missed the notdead event
        }
        public void SFEXT_G_PilotExit()
        {
            Occupied = false;
            RotationInputs = Vector3.zero;
        }
        public void SFEXT_G_NotDead()
        { dead = false; }
        public void SFEXT_G_Dead()
        { dead = true; }
        public void SFEXT_O_PilotExit()
        {
            //reset everything
            Piloting = false;
            Taxiinglerper = 0;
            ThrottleGripLastFrame = false;
            JoystickGripLastFrame = false;
            DoAAMTargeting = false;
            MissilesIncomingHeat = 0;
            MissilesIncomingRadar = 0;
            MissilesIncomingOther = 0;
            localPlayer.SetVelocity(CurrentVel);
            if (EngineOffOnExit)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetEngineOff));
                ThrottleInput = 0; ;
            }
            //set vehicle's collider's layers back
            SetCollidersLayer(VehicleLayer);
        }
        public void SetCollidersLayer(int NewLayer)
        {
            if (VehicleMesh)
            {
                if (OnlyChangeColliders)
                {
                    Collider[] children = VehicleMesh.GetComponentsInChildren<Collider>(true);
                    foreach (Collider child in children)
                    {
                        child.gameObject.layer = NewLayer;
                    }
                }
                else
                {
                    Transform[] children = VehicleMesh.GetComponentsInChildren<Transform>(true);
                    foreach (Transform child in children)
                    {
                        child.gameObject.layer = NewLayer;
                    }
                }
            }
        }
        private void WindAndAoA()
        {
            if (_DisablePhysicsAndInputs) { return; }
            float AtmosPos = CenterOfMass.position.y;
            if (UseAtmospherePositionOffset) { AtmosPos += AtmospherePositionOffset; }//saves one extern if not using it
            Atmosphere = Mathf.Clamp((1 - (AtmosPos / AtmoshpereFadeDistance)) + AtmosphereHeightThing, 0, 1);
            float TimeGustiness = Time.time * WindGustiness;
            float gustx = TimeGustiness + (VehicleTransform.position.x * WindTurbulanceScale);
            float gustz = TimeGustiness + (VehicleTransform.position.z * WindTurbulanceScale);
            FinalWind = Vector3.Normalize(new Vector3((Mathf.PerlinNoise(gustx + 9000, gustz) - .5f), /* (Mathf.PerlinNoise(gustx - 9000, gustz - 9000) - .5f) */0, (Mathf.PerlinNoise(gustx, gustz + 9999) - .5f))) * WindGustStrength;
            FinalWind = (FinalWind + Wind) * Atmosphere;
            AirVel = VehicleRigidbody.velocity - (FinalWind * StillWindMulti);
            AirSpeed = AirVel.magnitude;
        }
        public Vector2 UnpackThrottles(float Throttle)
        {
            //x = throttle amount (0-1), y = afterburner amount (0-1)
            return Vector2.right * Throttle;
        }
    }
}

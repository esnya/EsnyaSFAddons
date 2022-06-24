
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UdonToolkit;
using JetBrains.Annotations;
using System.Runtime.CompilerServices;

namespace EsnyaSFAddons.SFEXT
{
    /// <summary>
    /// Drive animation parameters of flight instruments.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_InstrumentsAnimationDriver : UdonSharpBehaviour
    {
        /// <summary>
        /// Animator to drive.
        ///
        /// Use airVehicle.VehicleAnimator if null
        /// </summary>
        [CanBeNull] public Animator instrumentsAnimator;

        /// <summary>
        /// Response of vacuum power witch driven by engine
        /// </summary>
        public float vacuumPowerResponse = 1.0f;

        /// <summary>
        /// Battery bus state.
        ///
        /// Always on if null.
        /// </summary>
        [CanBeNull] public GameObject batteryBus;

        /// <summary>
        /// Response of battery voltage.
        /// </summary>
        public float batteryVoltageResponse = 1.0f;

        /// <summary>
        /// Magnetic declination.
        /// </summary>
        public float magneticDeclination;

        /// <summary>
        /// Response of velocity indicators.
        /// </summary>
        public float smoothedVelocityResponse = 0.25f;

        [Header("ADI")]
        /// <summary>
        /// Set true to enable Attitude Indicator.
        /// </summary>
        public bool hasADI = true;

        /// <summary>
        /// Is electirc driven.
        /// </summary>
        [HideIf("@!hasADI")] public bool adiElectric = false;

        /// <summary>
        /// Max pitch angle in degrees.
        /// </summary>
        [HideIf("@!hasADI")] public float maxPitch = 30;

        /// <summary>
        /// Name of parameter in animator.
        ///
        /// -maxPitch to maxPitch will be remapped to 0.0 to 1.0.
        /// </summary>
        [NotNull][HideIf("@!hasADI")] public string pithFloatParameter = "pitch";


        /// <summary>
        /// Name of parameter in animator.
        ///
        /// -180 to 180 degrees will be remapped to 0.0 to 1.0.
        /// </summary>
        [NotNull][HideIf("@!hasADI")] public string rollFloatParameter = "roll";

        [Header("HI")]
        /// <summary>
        /// Set true to enable Heading Indicator.
        /// </summary>
        public bool hasHI = true;

        /// <summary>
        /// Is electirc driven.
        /// </summary>
        [NotNull][HideIf("@!hasHI")] public bool hiElectric = false;

        /// <summary>
        /// Name of parameter in animator.
        ///
        /// 0 to 360 degrees will be remapped to 0.0 to 1.0.
        /// </summary>
        [NotNull][HideIf("@!hasHI")] public string headingFloatParameter = "heading";

        [Header("ASI")]
        /// <summary>
        /// Set true to enable Airspeed Indicator.
        /// </summary>
        public bool hasASI = true;

        /// <summary>
        /// Max indicated airspeed in knots.
        /// </summary>
        [HideIf("@!hasASI")] public float maxAirspeed = 180.0f;

        /// <summary>
        /// Response of airspeed indicator.
        /// </summary>
        [HideIf("@!hasASI")] public float asiResponse = 0.25f;

        /// <summary>
        /// Name of parameter in animator.
        ///
        /// 0 to maxAirspeed will be remapped to 0.0 to 1.0.
        /// </summary>
        [NotNull][HideIf("@!hasASI")] public string airspeedFloatParameter = "airspeed";

        [Header("Altimeter")]
        /// <summary>
        /// Set true to enable barometric Altimeter.
        /// </summary>
        public bool hasAltimeter = true;
        /// <summary>
        /// Max indicated altitude in feet.
        /// </summary>
        [HideIf("@!hasAltimeter")] public float maxAltitude = 20000;

        /// <summary>
        /// Response of altimeter.
        /// </summary>
        [HideIf("@!hasAltimeter")] public float altimeterResponse = 0.25f;

        /// <summary>
        /// Name of parameter in animator.
        ///
        /// 0 to maxAltitude will be remapped to 0.0 to 1.0.
        /// </summary>
        [NotNull][HideIf("@!hasAltimeter")] public string altitudeFloatParameter = "altitude";

        [Header("TC")]
        /// <summary>
        /// Set true to enable Turn Coordinator
        /// </summary>
        public bool hasTC = true;

        /// <summary>
        /// Is turn coordinator electric.
        /// </summary>
        [HideIf("@!hasTC")] public bool tcElectric = true;

        /// <summary>
        /// Max turn rate.
        /// </summary>
        [HideIf("@!hasTC")] public float maxTurn = 360.0f / 60.0f * 2.0f;

        /// <summary>
        /// Response of turn coordinator.
        /// </summary>
        [HideIf("@!hasTC")] public float turnResponse = 1.0f;

        /// <summary>
        /// Name of parameter in animator.
        ///
        /// -maxTurn to maxTurn will be remapped to 0.0 to 1.0.
        /// </summary>
        [NotNull][HideIf("@!hasTC")] public string turnRateFloatParameter = "turnrate";

        [Header("SI")]
        /// <summary>
        /// Set true to enable Slip Indicator ("Ball").
        /// </summary>
        public bool hasSI = true;

        /// <summary>
        /// Max slip angle in degrees.
        /// </summary>
        [HideIf("@!hasSI")] public float maxSlip = 12.0f;

        /// <summary>
        /// Response of slip indicator.
        /// </summary>
        [HideIf("@!hasSI")] public float slipResponse = 0.2f;

        /// <summary>
        /// Name of parameter in animator.
        ///
        /// -maxSlip to maxSlip will be remapped to 0.0 to 1.0.
        /// </summary>
        [NotNull][HideIf("@!hasSI")] public string slipAngleFloatParameter = "slipangle";

        [Header("VSI")]
        /// <summary>
        /// Set true to enable Vertical Speed Indicator.
        /// </summary>
        public bool hasVSI = true;

        /// <summary>
        /// Max indicated vertical speed in feet per minute.
        /// </summary>
        [HideIf("@!hasVSI")] public float maxVerticalSpeed = 2000;

        /// <summary>
        /// Response of vertical speed indicator.
        /// </summary>
        [HideIf("@!hasVSI")] public float vsiResponse = 0.25f;

        /// <summary>
        /// Name of parameter in animator.
        ///
        /// -maxVerticalSpeed to maxVerticalSpeed will be remapped to 0.0 to 1.0.
        /// </summary>
        [NotNull][HideIf("@!hasVSI")] public string verticalSpeedFloatParameter = "vs";

        [Header("Magnetic Compass")]
        /// <summary>
        /// Set true to enable standby magnetic compass.
        /// </summary>
        public bool hasMagneticCompass = true;

        /// <summary>
        /// Response of magnetic compass
        /// </summary>
        [HideIf("@hasMagneticCompass")] public float compassResponse = 0.5f;

        /// <summary>
        /// Name of parameter inanimator
        ///
        /// 0 to 360 degrees will be remapped to 0.0 to 1.0.
        /// </summary>
        [NotNull][HideIf("@!hasMagneticCompass")] public string magneticCompassFloatParameter = "compass";

        private SaccAirVehicle airVehicle;
        private Rigidbody vehicleRigidbody;
        private bool vacuum;
        private bool initaialized;
        private float vacuumPower;
        private float batteryVoltage;
        private Vector3 prevPosition;
        private float turnRate;
        private float slipAngle;
        private Vector3 position;
        private float deltaTime;
        private float roll;
        private float heading;
        private Vector3 velocity;
        private Vector3 acceleration;
        private Vector3 smoothedVelocity;
        private float prevRoll;
        private float prevHeading;
        private Vector3 prevVelocity;
        private Vector3 forward;
        private Vector3 up;

        private bool Battery
        {
            get => !batteryBus || batteryBus.activeInHierarchy;
        }

        private bool _inVehicle;
        private float compassHeading;

        private bool InVehicle
        {
            get => _inVehicle;
            set {
                _inVehicle = value;
                if (value)
                {
                    vacuum = airVehicle.EngineOn;
                    vacuumPower = vacuum ? 1.0f : 0.0f;
                    batteryVoltage = Battery ? 1.0f : 0.0f;
                    gameObject.SetActive(true);
                }
            }
        }

        public void SFEXT_L_EntityStart()
        {
            var entity = GetComponentInParent<SaccEntity>();
            airVehicle = (SaccAirVehicle)entity.GetExtention(GetUdonTypeName<SaccAirVehicle>());
            vehicleRigidbody = airVehicle.VehicleRigidbody;
            if (instrumentsAnimator == null) instrumentsAnimator = airVehicle.VehicleAnimator;

            var navaidDatabaseObj = GameObject.Find("NavaidDatabase");
            if (navaidDatabaseObj) magneticDeclination = (float)((UdonBehaviour)navaidDatabaseObj.GetComponent(typeof(UdonBehaviour))).GetProgramVariable("magneticDeclination");

            vacuum = airVehicle.EngineOn;

            initaialized = true;
        }

        public void SFEXT_G_EngineOn() => vacuum = true;
        public void SFEXT_G_EngineOff() => vacuum = false;

        public void SFEXT_O_PilotEnter() => InVehicle = true;
        public void SFEXT_O_PilotExit() => InVehicle = false;
        public void SFEXT_P_PassengerEnter() => InVehicle = true;
        public void SFEXT_P_PassengerExit() => InVehicle = false;

        public override void PostLateUpdate()
        {
            if (!initaialized) return;

            forward = transform.forward;
            up = transform.up;

            position = transform.position;
            deltaTime = Time.deltaTime;
            roll = Vector3.SignedAngle(up, Vector3.ProjectOnPlane(Vector3.up, forward).normalized, forward);
            heading = transform.eulerAngles.y;
            velocity = (position - prevPosition) / deltaTime;
            acceleration = (velocity - prevVelocity) / deltaTime;

            smoothedVelocity = Vector3.Lerp(smoothedVelocity, velocity, deltaTime * smoothedVelocityResponse);

            var batteryVoltageTarget = Battery ? 1.0f : 0.0f;
            var batteryVoltageUpdate = !Mathf.Approximately(batteryVoltageTarget, batteryVoltage);
            if (batteryVoltageUpdate) batteryVoltage = Mathf.MoveTowards(batteryVoltage, batteryVoltageTarget, deltaTime * batteryVoltageResponse);

            var vacuumPowerTarget = vacuum ? 1.0f : 0.0f;
            var vacuumPowerUpdate = !Mathf.Approximately(vacuumPower, vacuumPowerTarget);
            if (vacuumPowerUpdate) vacuumPower = Mathf.Lerp(vacuumPower, vacuumPowerTarget, deltaTime * vacuumPowerResponse);

            if (hasADI) ADI_Update(adiElectric ? batteryVoltage : vacuumPower);
            if (hasHI) HI_Update(hiElectric ? batteryVoltage : vacuumPower);
            if (hasASI) ASI_Update();
            if (hasAltimeter) Altimeter_Update();
            if (hasTC) TC_Update(tcElectric ? batteryVoltage : vacuumPower);
            if (hasSI) SI_Update();
            if (hasVSI) VSI_Update();
            if (hasMagneticCompass) MC_Update();

            prevPosition = position;
            prevRoll = roll;
            prevHeading = heading;
            prevVelocity = velocity;

            if (!(InVehicle || vacuumPowerUpdate || batteryVoltageUpdate))
            {
                gameObject.SetActive(false);
            }
        }

        private void ADI_Update(float power)
        {
            var pitch = Mathf.DeltaAngle(vehicleRigidbody.transform.localEulerAngles.x, 0);

            instrumentsAnimator.SetFloat(pithFloatParameter, Remap01(pitch, -maxPitch, maxPitch) * power);
            instrumentsAnimator.SetFloat(rollFloatParameter, Remap01(Mathf.Lerp(30.0f, roll, power), -180.0f, 180.0f));
        }

        private void HI_Update(float power)
        {
            var magneticHeading = (heading + magneticDeclination + 360) % 360;
            instrumentsAnimator.SetFloat(headingFloatParameter, Mathf.Lerp(33.0f, magneticHeading, power) / 360.0f);
        }

        private void ASI_Update()
        {
            var TimeGustiness = Time.time * airVehicle.WindGustiness;
            var gustx = TimeGustiness + (position.x * airVehicle.WindTurbulanceScale);
            var gustz = TimeGustiness + (position.z * airVehicle.WindTurbulanceScale);
            var finalWind = (airVehicle.Wind + Vector3.Normalize(new Vector3(Mathf.PerlinNoise(gustx + 9000, gustz) - .5f, /* (Mathf.PerlinNoise(gustx - 9000, gustz - 9000) - .5f) */0, Mathf.PerlinNoise(gustx, gustz + 9999) - .5f)) * airVehicle.WindGustStrength) * airVehicle.Atmosphere;;

            var airspeed = Mathf.Max(Vector3.Dot(smoothedVelocity - finalWind, forward), 0);
            instrumentsAnimator.SetFloat(airspeedFloatParameter, airspeed * 1.9438445f / maxAirspeed);
        }

        private void Altimeter_Update()
        {
            var altitude = (position.y - airVehicle.SeaLevel) * 3.28084f;
            instrumentsAnimator.SetFloat(altitudeFloatParameter, Mathf.Clamp01(altitude / maxAltitude));
        }

        private void TC_Update(float power)
        {
            turnRate = Mathf.Lerp(turnRate, (Mathf.DeltaAngle(heading, prevHeading) + Mathf.DeltaAngle(roll, prevRoll) * 0.5f) / deltaTime, deltaTime * turnResponse);
            instrumentsAnimator.SetFloat(turnRateFloatParameter, Remap01(turnRate, -maxTurn, maxTurn) * power);
        }

        private void SI_Update()
        {
            slipAngle = Mathf.Lerp(slipAngle, Mathf.Clamp(Vector3.SignedAngle(-up, Vector3.ProjectOnPlane(Physics.gravity - acceleration, forward), forward), -maxSlip, maxSlip), deltaTime * slipResponse);
            instrumentsAnimator.SetFloat(slipAngleFloatParameter, Remap01(slipAngle, -maxSlip, maxSlip));
        }

        private void VSI_Update()
        {
            var verticalSpeed = smoothedVelocity.y * 3.28084f * 60;
            instrumentsAnimator.SetFloat(verticalSpeedFloatParameter, Remap01(verticalSpeed, -maxVerticalSpeed, maxVerticalSpeed));
        }

        private void MC_Update()
        {
            compassHeading = (Mathf.LerpAngle(compassHeading, heading + magneticDeclination, deltaTime * compassResponse) + 360.0f) % 360.0f;
            instrumentsAnimator.SetFloat(magneticCompassFloatParameter, compassHeading / 360.0f);
        }

        private float Remap01(float value, float oldMin, float oldMax)
        {
            return (value - oldMin) / (oldMax - oldMin);
        }
    }
}

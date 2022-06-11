
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UdonToolkit;

namespace EsnyaSFAddons
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_InstrumentsAnimationDriver : UdonSharpBehaviour
    {
        public Animator instrumentsAnimator;

        public float vacuumPowerResponse = 1.0f;
        public GameObject batteryBus;
        public float batteryVoltageResponse = 1.0f;
        public float magneticDeclination;
        private float velocityResponse = 0.25f;

        [Header("ADI")]
        public bool hasADI = true;
        [HideIf("@!hasADI")] public bool adiElectric = false;
        [HideIf("@!hasADI")] public float maxPitch = 30;
        [HideIf("@!hasADI")] public string pithFloatParameter = "pitch";
        [HideIf("@!hasADI")] public string rollFloatParameter = "roll";

        [Header("HI")]
        public bool hasHI = true;
        [HideIf("@!hasHI")] public bool hiElectric = false;
        [HideIf("@!hasHI")] public string headingFloatParameter = "heading";

        [Header("ASI")]
        public bool hasASI = true;
        [HideIf("@!hasASI")] public float maxAirspeed = 180.0f;
        [HideIf("@!hasASI")] public string airspeedFloatParameter = "airspeed";
        [HideIf("@!hasASI")] public float asiResponse = 0.25f;

        [Header("Altimeter")]
        public bool hasAltimeter = true;
        [HideIf("@!hasAltimeter")] public float maxAltitude = 20000;
        [HideIf("@!hasAltimeter")] public string altitudeFloatParameter = "altitude";
        [HideIf("@!hasAltimeter")] public float altimeterResponse = 0.25f;

        [Header("TC")]
        public bool hasTC = true;
        [HideIf("@!hasTC")] public bool tcElectric = true;
        [HideIf("@!hasTC")] public float maxTurn = 360.0f / 60.0f * 2.0f;
        [HideIf("@!hasTC")] public float maxSlip = 9.0f;
        [HideIf("@!hasTC")] public string turnRateFloatParameter = "turnrate";
        [HideIf("@!hasTC")] public string slipAngleFloatParameter = "slipangle";
        [HideIf("@!hasTC")] public float turnResponse = 1.0f;
        [HideIf("@!hasTC")] public float slipResponse = 1.0f;

        [Header("VSI")]
        public bool hasVSI = true;
        [HideIf("@!hasVSI")] public float maxVerticalSpeed = 2000;
        [HideIf("@!hasVSI")] public string verticalSpeedFloatParameter = "vs";
        [HideIf("@!hasVSI")] public float vsiResponse = 0.25f;

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
        private float prevRoll;
        private float prevHeading;
        private Vector3 prevVelocity;

        private bool Battery
        {
            get => !batteryBus || batteryBus.activeInHierarchy;
        }

        private bool _inVehicle;
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

            var forward = transform.forward;

            position = transform.position;
            deltaTime = Time.deltaTime;
            roll = Vector3.SignedAngle(transform.up, Vector3.ProjectOnPlane(Vector3.up, forward).normalized, forward);
            heading = transform.eulerAngles.y;
            velocity = Vector3.Lerp(velocity, (position - prevPosition) / deltaTime, deltaTime * velocityResponse);

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
            if (hasVSI) VSI_Update();

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

            var airspeed = Mathf.Max(Vector3.Dot(velocity - finalWind, transform.forward), 0);
            instrumentsAnimator.SetFloat(airspeedFloatParameter, airspeed * 1.9438445f / maxAirspeed);
        }

        private void Altimeter_Update()
        {
            var altitude = (position.y - airVehicle.SeaLevel) * 3.28084f;
            instrumentsAnimator.SetFloat(altitudeFloatParameter, Mathf.Clamp01(altitude / maxAltitude));
        }

        private void TC_Update(float power)
        {
            var forward = transform.forward;

            var acceleration = (velocity - prevVelocity) / deltaTime;

            turnRate = Mathf.Lerp(turnRate, (Mathf.DeltaAngle(heading, prevHeading) + Mathf.DeltaAngle(roll, prevRoll) * 0.5f) / deltaTime, deltaTime * turnResponse);
            slipAngle = Mathf.Lerp(slipAngle, Mathf.Clamp(Vector3.SignedAngle(-transform.up, Vector3.ProjectOnPlane(Physics.gravity - acceleration, forward), forward), -maxSlip, maxSlip), deltaTime * slipResponse);

            instrumentsAnimator.SetFloat(turnRateFloatParameter, Remap01(turnRate, -maxTurn, maxTurn) * power);
            instrumentsAnimator.SetFloat(slipAngleFloatParameter, Remap01(slipAngle, -maxSlip, maxSlip));
        }

        private void VSI_Update()
        {
            var verticalSpeed = velocity.y * 3.28084f * 60;
            instrumentsAnimator.SetFloat(verticalSpeedFloatParameter, Remap01(verticalSpeed, -maxVerticalSpeed, maxVerticalSpeed));
        }

        private float Remap01(float value, float oldMin, float oldMax)
        {
            return (value - oldMin) / (oldMax - oldMin);
        }
    }
}

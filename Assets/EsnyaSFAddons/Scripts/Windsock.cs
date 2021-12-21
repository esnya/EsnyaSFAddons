﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace EsnyaAircraftAssets
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [RequireComponent(typeof(Cloth))]
    [RequireComponent(typeof(Animator))]
    public class Windsock : UdonSharpBehaviour
    {
        public SAV_WindChanger windChanger;
        public int updateInterval = 90;
        public float maxWindMagnitude = 100.0f;
        public float rotationSmooth = 100.0f;

        private int updateOffset;
        private Animator animator;
        private float heading;
        private Quaternion initialRotation;

        private void Start()
        {
            updateOffset = Random.Range(0, updateInterval);
            animator = GetComponent<Animator>();
            initialRotation = transform.rotation;
            heading = 0;
        }

        private void Update()
        {
            if ((Time.frameCount + updateOffset) % updateInterval != 0 || windChanger == null) return;

            var wind = ((SaccAirVehicle)windChanger.SaccAirVehicles[0]).Wind;

            heading = Mathf.LerpAngle(heading, Vector3.SignedAngle(Vector3.forward, Vector3.ProjectOnPlane(wind, Vector3.up), Vector3.up), rotationSmooth * Mathf.Clamp01(wind.magnitude) / (Time.deltaTime * updateInterval));

            transform.rotation = Quaternion.AngleAxis(heading, Vector3.up) * initialRotation;

            animator.SetFloat("x", Mathf.Clamp(wind.x / maxWindMagnitude, -1.0f, 1.0f));
            animator.SetFloat("z", Mathf.Clamp(wind.z / maxWindMagnitude, -1.0f, 1.0f));
        }
    }
}

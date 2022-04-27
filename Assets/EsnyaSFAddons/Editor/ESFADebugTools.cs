using System;
using System.Configuration;
using System.Linq;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace EsnyaSFAddons
{
    public class ESFADebugTools : EditorWindow
    {

        [MenuItem("SaccFlight/EsnyaSFAddons/Debug Tools")]
        private static void ShowWindow()
        {
            var window = GetWindow<ESFADebugTools>();
            window.titleContent = new GUIContent("ESFA Debug Tools");
            window.Show();
        }

        private void FrameCamera(SaccEntity entity, bool lockCamera)
        {
            var vehicleMesh = entity.GetUdonSharpComponentInChildren<SaccAirVehicle>().VehicleMesh;
            Selection.activeGameObject = vehicleMesh.GetComponentsInChildren<SkinnedMeshRenderer>().Select(c => c as Component).Concat(vehicleMesh.GetComponentsInChildren<MeshRenderer>()).Select(r => r.gameObject).FirstOrDefault();
            SceneView.lastActiveSceneView.FrameSelected(lockCamera);
        }

        private void OnGUI()
        {
            var entity = (Selection.activeGameObject?.GetComponentInParent<Rigidbody>() ?? Selection.activeGameObject?.GetComponentInChildren<Rigidbody>())?.GetUdonSharpComponent<SaccEntity>();

            if (!entity)
            {
                EditorGUILayout.LabelField("Select a GameObject which part of vehicle.");
                return;
            }

            EditorGUILayout.LabelField("View");
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Frame Camera")) FrameCamera(entity, false);
                if (GUILayout.Button("Frame Camera & Lock")) FrameCamera(entity, true);
            }

            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Startup");
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Pilot"))
                {
                    var seat = entity.GetUdonSharpComponentInChildren<SaccVehicleSeat>();
                    if (seat) UdonSharpEditorUtility.GetBackingUdonBehaviour(seat).SendCustomEvent("_interact");
                }

                if (GUILayout.Button("Quick Start"))
                {
                    foreach (var canopyFunc in entity.GetUdonSharpComponentsInChildren<DFUNC_Canopy>(true)) UdonSharpEditorUtility.GetBackingUdonBehaviour(canopyFunc).SendCustomEvent(nameof(canopyFunc.CanopyClosing));
                    foreach (var engine in entity.GetUdonSharpComponentsInChildren<SFEXT_AdvancedEngine>(true)) UdonSharpEditorUtility.GetBackingUdonBehaviour(engine).SendCustomEvent(nameof(engine._InstantStart));

                    var airVehicle = entity.GetExtention(UdonSharpBehaviour.GetUdonTypeName<SaccAirVehicle>());
                    var engineToggle = entity.GetExtention(UdonSharpBehaviour.GetUdonTypeName<DFUNC_ToggleEngine>());
                    if (engineToggle && (bool)airVehicle.GetProgramVariable(nameof(SaccAirVehicle._EngineOn)) != true)
                    {
                        engineToggle.SendCustomEvent(nameof(DFUNC_ToggleEngine.ToggleEngine));
                    }
                }

                if (GUILayout.Button("Explode"))
                {
                    foreach (var airVehicle in entity.GetUdonSharpComponentsInChildren<SaccAirVehicle>(true)) UdonSharpEditorUtility.GetBackingUdonBehaviour(airVehicle).SendCustomEvent(nameof(SaccAirVehicle.Explode));
                }

                if (GUILayout.Button("Respawn"))
                {
                    foreach (var airVehicle in entity.GetUdonSharpComponentsInChildren<SaccAirVehicle>(true)) UdonSharpEditorUtility.GetBackingUdonBehaviour(airVehicle).SendCustomEvent(nameof(SaccAirVehicle.SFEXT_O_RespawnButton));
                }
            }

            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Flaps & Gears Cnfiguration");
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("TakeOff"))
                {
                    SetGearDown(entity, true);
                    SetFlaps(entity, 0.5f);
                    SetTrim(entity, 0.0f);
                }

                if (GUILayout.Button("Clean"))
                {
                    SetGearDown(entity, false);
                    SetFlaps(entity, 0.0f);
                    SetTrim(entity, 0.0f);
                }

                if (GUILayout.Button("Landing"))
                {
                    SetGearDown(entity, true);
                    SetFlaps(entity, 1.0f);
                }
            }

            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Throttle");
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Max")) SetPower(entity, 1.0f);
                if (GUILayout.Button("Climb")) SetPower(entity, 0.7f);
                if (GUILayout.Button("Cruise")) SetPower(entity, 0.3f);
                if (GUILayout.Button("Idle")) SetPower(entity, 0.0f);
            }

        }

        private static void SetPower(SaccEntity entity, float power)
        {
            foreach (var airVehicle in entity.GetUdonSharpComponentsInChildren<SaccAirVehicle>(true)) UdonSharpEditorUtility.GetBackingUdonBehaviour(airVehicle).SetProgramVariable(nameof(SaccAirVehicle.PlayerThrottle), power);
        }

        private static void SetFlaps(SaccEntity entity, float angle)
        {
            foreach (var flapsFunc in entity.GetUdonSharpComponentsInChildren<DFUNC_Flaps>(true)) UdonSharpEditorUtility.GetBackingUdonBehaviour(flapsFunc).SendCustomEvent(angle > 0 ? nameof(DFUNC_Flaps.SetFlapsOn) : nameof(flapsFunc.SetFlapsOff));
            foreach (var flapsFunc in entity.GetUdonSharpComponentsInChildren<DFUNC_AdvancedFlaps>(true)) UdonSharpEditorUtility.GetBackingUdonBehaviour(flapsFunc).SetProgramVariable(nameof(DFUNC_AdvancedFlaps.targetAngle), flapsFunc.detents[Mathf.Min(Mathf.FloorToInt(flapsFunc.detents.Length * angle), flapsFunc.detents.Length - 1)]);
        }

        private static void SetGearDown(SaccEntity entity, bool gearDown)
        {
            foreach (var gearFunc in entity.GetUdonSharpComponentsInChildren<DFUNC_Gear>(true)) UdonSharpEditorUtility.GetBackingUdonBehaviour(gearFunc).SendCustomEvent(gearDown ? nameof(DFUNC_Gear.SetGearDown) : nameof(DFUNC_Gear.SetGearUp));
        }

        private static void SetTrim(SaccEntity entity, float trim)
        {
            foreach (var trimFunc in entity.GetUdonSharpComponentsInChildren<DFUNC_ElevatorTrim>(true)) UdonSharpEditorUtility.GetBackingUdonBehaviour(trimFunc).SetProgramVariable(nameof(DFUNC_ElevatorTrim.trim), trim);
        }
    }
}

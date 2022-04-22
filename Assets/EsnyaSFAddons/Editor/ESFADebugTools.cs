using System;
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
            if (GUILayout.Button("Frame Camera")) FrameCamera(entity, false);
            if (GUILayout.Button("Frame Camera & Lock")) FrameCamera(entity, true);

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


            if (GUILayout.Button("TakeOff Cnfiguration"))
            {
                foreach (var flapsFunc in entity.GetUdonSharpComponentsInChildren<DFUNC_Flaps>(true)) UdonSharpEditorUtility.GetBackingUdonBehaviour(flapsFunc).SendCustomEvent(nameof(flapsFunc.SetFlapsOn));
                foreach (var trimFunc in entity.GetUdonSharpComponentsInChildren<DFUNC_ElevatorTrim>(true)) UdonSharpEditorUtility.GetBackingUdonBehaviour(trimFunc).SetProgramVariable(nameof(trimFunc.trim), 0.0f);
                foreach (var flapsFunc in entity.GetUdonSharpComponentsInChildren<DFUNC_AdvancedFlaps>(true)) UdonSharpEditorUtility.GetBackingUdonBehaviour(flapsFunc).SetProgramVariable(nameof(flapsFunc.targetAngle), flapsFunc.detents[flapsFunc.detents.Length / 2]);
            }

            if (GUILayout.Button("Clean Configuration"))
            {
                foreach (var flapsFunc in entity.GetUdonSharpComponentsInChildren<DFUNC_Flaps>(true)) UdonSharpEditorUtility.GetBackingUdonBehaviour(flapsFunc).SendCustomEvent(nameof(flapsFunc.SetFlapsOff));
                foreach (var gearFunc in entity.GetUdonSharpComponentsInChildren<DFUNC_Gear>(true)) UdonSharpEditorUtility.GetBackingUdonBehaviour(gearFunc).SendCustomEvent(nameof(gearFunc.SetGearUp));
                foreach (var flapsFunc in entity.GetUdonSharpComponentsInChildren<DFUNC_AdvancedFlaps>(true)) UdonSharpEditorUtility.GetBackingUdonBehaviour(flapsFunc).SetProgramVariable(nameof(flapsFunc.targetAngle), flapsFunc.detents[0]);
            }

            if (GUILayout.Button("Landing Configuration"))
            {
                foreach (var flapsFunc in entity.GetUdonSharpComponentsInChildren<DFUNC_Flaps>(true)) UdonSharpEditorUtility.GetBackingUdonBehaviour(flapsFunc).SendCustomEvent(nameof(flapsFunc.SetFlapsOn));
                foreach (var gearFunc in entity.GetUdonSharpComponentsInChildren<DFUNC_Gear>(true)) UdonSharpEditorUtility.GetBackingUdonBehaviour(gearFunc).SendCustomEvent(nameof(gearFunc.SetGearDown));
                foreach (var flapsFunc in entity.GetUdonSharpComponentsInChildren<DFUNC_AdvancedFlaps>(true)) UdonSharpEditorUtility.GetBackingUdonBehaviour(flapsFunc).SetProgramVariable(nameof(flapsFunc.targetAngle), flapsFunc.detents[flapsFunc.detents.Length - 1]);
            }

            if (GUILayout.Button("Respawn"))
            {
                entity.SendEventToExtensions("SFEXT_O_RespawnButton");
            }
        }
    }
}

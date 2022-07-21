using System.Linq;
using SaccFlightAndVehicles;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.Udon;

namespace EsnyaSFAddons.Editor
{
    /// <summary>
    /// Draw Gizmos of SaccFlight components
    ///
    /// Now available for SaccEntity, SaccVehicleSeat, SaccAirVehicle and SAV_FloatScript
    /// </summary>
    public class SFGizmoDrawer
    {
        /// <summary>
        /// Gizmo color configuration
        /// </summary>
        private class GizmoColors
        {
            public static Color eyePosition = Color.white;
            public static Color floatPoint = Color.cyan;
            public static Color centerOfMass = Color.white;
            public static Color groundDetector = Color.white;
            public static Color pitchMount = Color.white;
            public static Color yawMount = Color.white;
        }

        private static void DrawSaccEntityGizmos(SaccEntity entity)
        {
            var centerOfMass = entity.CenterOfMass;
            if (centerOfMass)
            {
                Gizmos.color = GizmoColors.centerOfMass;
                Gizmos.DrawWireSphere(centerOfMass.position, 0.1f);
            }

            foreach (var floatScript in entity.ExtensionUdonBehaviours.Where(udon => udon is SAV_FloatScript).Select(udon => udon as SAV_FloatScript))
            {
                DrawSAV_FloatScriptGizmos(floatScript);
            }
        }

        private static void DrawSaccVehicleSeatGizmos(SaccVehicleSeat saccVehicleSeat)
        {
            var targetEyePosition = saccVehicleSeat.GetProgramVariable("TargetEyePosition") as Transform;
            if (!(bool)saccVehicleSeat.GetProgramVariable("AdjustSeat") || targetEyePosition == null) return;

            Gizmos.color = GizmoColors.eyePosition;
            Gizmos.DrawWireSphere(targetEyePosition.position, 0.1f);
        }

        private static void DrawSaccAirVehicleGizmos(SaccAirVehicle airVehicle)
        {
            var entity = airVehicle.EntityControl;

            var entityTransform = entity?.transform ?? airVehicle.transform;

            var pitchMount = airVehicle.PitchMoment;
            if (pitchMount)
            {
                Gizmos.color = GizmoColors.pitchMount;
                Gizmos.DrawWireSphere(pitchMount.position, 0.1f);
                Gizmos.DrawLine(pitchMount.position - entityTransform.up, pitchMount.position + entityTransform.up);
            }

            var yawMount = airVehicle.YawMoment;
            if (yawMount)
            {
                Gizmos.color = GizmoColors.yawMount;
                Gizmos.DrawWireSphere(yawMount.position, 0.1f);
                Gizmos.DrawLine(yawMount.position - entityTransform.right, yawMount.position + entityTransform.right);
            }

            var groundDetector = airVehicle.GroundDetector;
            if (groundDetector)
            {
                Gizmos.color = GizmoColors.groundDetector;
                Gizmos.DrawWireSphere(groundDetector.position, 0.1f);
                Gizmos.DrawRay(groundDetector.position, Vector3.down * airVehicle.GroundDetectorRayDistance);
            }
        }

        private static void DrawSAV_FloatScriptGizmos(SAV_FloatScript floatScript)
        {
            if (floatScript.GetProgramVariable("FloatPoints") is object[] floatPoints)
            {
                var radius = (float)floatScript.GetProgramVariable("FloatRadius");
                Gizmos.color = GizmoColors.floatPoint;
                foreach (var floatPoint in floatPoints.Select(p => p as Transform))
                {
                    Gizmos.DrawWireSphere(floatPoint.position, radius);
                }
            }
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy)]
        public static void DrawGizmos(UdonBehaviour udonBehaviour, GizmoType gizmoType)
        {
            if (!UdonSharpEditorUtility.IsUdonSharpBehaviour(udonBehaviour)) return;

            try
            {
                var udonSharpBehaviour = UdonSharpEditorUtility.GetProxyBehaviour(udonBehaviour);

                if (udonSharpBehaviour is SaccEntity @entity) DrawSaccEntityGizmos(@entity);
                if (udonSharpBehaviour is SaccVehicleSeat @seat) DrawSaccVehicleSeatGizmos(@seat);
                if (udonSharpBehaviour is SaccAirVehicle @airVehicle) DrawSaccAirVehicleGizmos(@airVehicle);
                if (udonSharpBehaviour is SAV_FloatScript @floatScript) DrawSAV_FloatScriptGizmos(@floatScript);
            }
            finally
            {

            }
        }
    }
}

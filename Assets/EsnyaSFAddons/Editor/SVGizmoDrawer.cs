using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.Udon;

namespace EsnyaAircraftAssets
{
    public class VehicleSeat
    {
        public static class GizmoColors
        {
            public static readonly Color eyePosition = Color.white;
            public static readonly Color floatPoint = Color.cyan;
        }

        private static void DrawSaccVehicleSeatGizmos(SaccVehicleSeat saccVehicleSeat)
        {
            var targetEyePosition = saccVehicleSeat.GetProgramVariable("TargetEyePosition") as Transform;
            if (!(bool)saccVehicleSeat.GetProgramVariable("AdjustSeat") || targetEyePosition == null) return;

            Gizmos.color = GizmoColors.eyePosition;
            Gizmos.DrawWireSphere(targetEyePosition.position, 0.1f);
        }

        private static void DrawSAV_FloatScriptGizmos(SAV_FloatScript floatScript)
        {
            var floatPoints = floatScript.GetProgramVariable("FloatPoints") as object[];
            if (floatPoints != null)
            {
                var radius = (float)floatScript.GetProgramVariable("FloatRadius");
                Gizmos.color = GizmoColors.floatPoint;
                foreach (var floatPoint in floatPoints.Select(p => p as Transform))
                {
                    Gizmos.DrawWireSphere(floatPoint.position, radius);
                }
            }
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        public static void SVGizmoDrawer(UdonBehaviour udonBehaviour, GizmoType gizmoType)
        {
            if (!UdonSharpEditorUtility.IsUdonSharpBehaviour(udonBehaviour)) return;

            var udonSharpBehaviour = UdonSharpEditorUtility.GetProxyBehaviour(udonBehaviour);

            if (udonSharpBehaviour is SaccVehicleSeat) DrawSaccVehicleSeatGizmos(udonSharpBehaviour as SaccVehicleSeat);
            if (udonSharpBehaviour is SAV_FloatScript) DrawSAV_FloatScriptGizmos(udonSharpBehaviour as SAV_FloatScript);
        }
    }
}

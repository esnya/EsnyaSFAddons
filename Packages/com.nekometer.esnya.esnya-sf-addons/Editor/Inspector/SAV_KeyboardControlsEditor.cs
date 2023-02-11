using System;
using System.Collections.Generic;
using System.Linq;
using SaccFlightAndVehicles;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace EsnyaSFAddons.Editor.Inspector
{
    [CustomEditor(typeof(SAV_KeyboardControls))]
    public class SAV_KeyboardControlsEditor : UnityEditor.Editor
    {
        private static readonly Dictionary<Type, KeyCode> defaultKeyCodes = new Dictionary<Type, KeyCode>() {
            { typeof(DFUNC_Limits), KeyCode.F1 },
            { typeof(DFUNC_Flares), KeyCode.X },
            { typeof(DFUNC_Catapult), KeyCode.C },
            { typeof(DFUNC_Brake), KeyCode.B },
            { typeof(DFUNC_AltHold), KeyCode.F3 },
            { typeof(DFUNC_Canopy), KeyCode.Z },
            { typeof(DFUNC_Cruise), KeyCode.F2 },
            { typeof(DFUNC_Gun), KeyCode.Alpha1 },
            { typeof(DFUNC_AAM), KeyCode.Alpha2 },
            { typeof(DFUNC_AGM), KeyCode.Alpha3 },
            { typeof(DFUNC_Bomb), KeyCode.Alpha4 },
            { typeof(DFUNC_Gear), KeyCode.G },
            { typeof(DFUNC_Flaps), KeyCode.F },
            { typeof(DFUNC_Hook), KeyCode.H },
            { typeof(DFUNC_Smoke), KeyCode.Alpha5 },
            { typeof(DFUNC_ToggleEngine), KeyCode.Backspace },
        };

        private static void FindDFUNC<T>(UdonSharpBehaviour target, string variableName, IEnumerable<UdonSharpBehaviour> dialFunctions) where T : UdonSharpBehaviour
        {
            target.SetProgramVariable(variableName, dialFunctions.FirstOrDefault(f => f is T) ?? target.GetProgramVariable(variableName));
        }

        private static void SetDefaultKey(SAV_KeyboardControls target, string dstVariableName, string srcVariableName)
        {
            var dfunc = target.GetProgramVariable(srcVariableName) as UdonSharpBehaviour;
            if (dfunc == null) return;
            var type = dfunc.GetType();
            if (defaultKeyCodes.ContainsKey(type)) target.SetProgramVariable(dstVariableName, defaultKeyCodes[type]);
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(this.target)) return;

            var target = this.target as SAV_KeyboardControls;

            serializedObject.Update();

            var property = serializedObject.GetIterator();
            property.NextVisible(true);

            EditorGUILayout.BeginHorizontal();

            while (property.NextVisible(false))
            {
                if (property.name.StartsWith("Lfunc") || property.name.StartsWith("Rfunc"))
                {
                    if (!property.name.EndsWith("key")) EditorGUILayout.LabelField(property.displayName, GUILayout.ExpandWidth(false));
                    EditorGUILayout.PropertyField(property, GUIContent.none, true);
                }
                else EditorGUILayout.PropertyField(property, true);

                if (property.name.EndsWith("key"))
                {
                    EditorGUILayout.EndHorizontal();
                    if (property.name != "Rfunc8key") EditorGUILayout.BeginHorizontal();
                }
            }


            if (GUILayout.Button("Find Default DFUNCs"))
            {
                Undo.RecordObject(target, "Find Default DFUNCs");
                var entity = target.gameObject.GetComponentInParent<SaccEntity>();

                FindDFUNC<DFUNC_Limits>(target, "Lfunc2", entity.Dial_Functions_L);
                FindDFUNC<DFUNC_Flares>(target, "Lfunc3", entity.Dial_Functions_L);
                FindDFUNC<DFUNC_Catapult>(target, "Lfunc4", entity.Dial_Functions_L);
                FindDFUNC<DFUNC_Brake>(target, "Lfunc5", entity.Dial_Functions_L);
                FindDFUNC<DFUNC_AltHold>(target, "Lfunc6", entity.Dial_Functions_L);
                FindDFUNC<DFUNC_Canopy>(target, "Lfunc7", entity.Dial_Functions_L);
                FindDFUNC<DFUNC_Cruise>(target, "Lfunc8", entity.Dial_Functions_L);

                FindDFUNC<DFUNC_Gun>(target, "Rfunc1", entity.Dial_Functions_R);
                FindDFUNC<DFUNC_AAM>(target, "Rfunc2", entity.Dial_Functions_R);
                FindDFUNC<DFUNC_AGM>(target, "Rfunc3", entity.Dial_Functions_R);
                FindDFUNC<DFUNC_Bomb>(target, "Rfunc4", entity.Dial_Functions_R);
                FindDFUNC<DFUNC_Gear>(target, "Rfunc5", entity.Dial_Functions_R);
                FindDFUNC<DFUNC_Flaps>(target, "Rfunc6", entity.Dial_Functions_R);
                FindDFUNC<DFUNC_Hook>(target, "Rfunc7", entity.Dial_Functions_R);
                FindDFUNC<DFUNC_Smoke>(target, "Rfunc8", entity.Dial_Functions_R);
                EditorUtility.SetDirty(target);
            }

            if (GUILayout.Button("Set Default Key Bindings"))
            {
                Undo.RecordObject(target, "Set Default Key Bindings");
                SetDefaultKey(target, "Lfunc1key", "Lfunc1");
                SetDefaultKey(target, "Lfunc2key", "Lfunc2");
                SetDefaultKey(target, "Lfunc3key", "Lfunc3");
                SetDefaultKey(target, "Lfunc4key", "Lfunc4");
                SetDefaultKey(target, "Lfunc5key", "Lfunc5");
                SetDefaultKey(target, "Lfunc6key", "Lfunc6");
                SetDefaultKey(target, "Lfunc7key", "Lfunc7");
                SetDefaultKey(target, "Lfunc8key", "Lfunc8");
                SetDefaultKey(target, "Rfunc1key", "Rfunc1");
                SetDefaultKey(target, "Rfunc2key", "Rfunc2");
                SetDefaultKey(target, "Rfunc3key", "Rfunc3");
                SetDefaultKey(target, "Rfunc4key", "Rfunc4");
                SetDefaultKey(target, "Rfunc5key", "Rfunc5");
                SetDefaultKey(target, "Rfunc6key", "Rfunc6");
                SetDefaultKey(target, "Rfunc7key", "Rfunc7");
                SetDefaultKey(target, "Rfunc8key", "Rfunc8");
                EditorUtility.SetDirty(target);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
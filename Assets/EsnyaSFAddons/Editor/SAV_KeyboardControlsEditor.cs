using System;
using System.Collections.Generic;
using System.Linq;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SAV_KeyboardControls))]
public class SAV_KeyboardControlsEditor : Editor
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
    };

    private static void FindDFUNC<T>(ref UdonSharpBehaviour target, IEnumerable<UdonSharpBehaviour> dialFunctions) where T : UdonSharpBehaviour
    {
        target = dialFunctions.FirstOrDefault(f => f is T) ?? target;
    }

    private static void SetDefaultKey(ref KeyCode target, UdonSharpBehaviour dfunc)
    {
        if (dfunc == null) return;
        var type = dfunc.GetType();
        if (defaultKeyCodes.ContainsKey(type)) target = defaultKeyCodes[type];
    }

    public override void OnInspectorGUI()
    {
        if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(this.target)) return;

        var target = this.target as SAV_KeyboardControls;
        var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(target);

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
            Undo.RecordObject(udon, "Find Default DFUNCs");
            var entity = target.gameObject.GetUdonSharpComponentInParent<SaccEntity>();

            FindDFUNC<DFUNC_Limits>(ref target.Lfunc2, entity.Dial_Functions_L);
            FindDFUNC<DFUNC_Flares>(ref target.Lfunc3, entity.Dial_Functions_L);
            FindDFUNC<DFUNC_Catapult>(ref target.Lfunc4, entity.Dial_Functions_L);
            FindDFUNC<DFUNC_Brake>(ref target.Lfunc5, entity.Dial_Functions_L);
            FindDFUNC<DFUNC_AltHold>(ref target.Lfunc6, entity.Dial_Functions_L);
            FindDFUNC<DFUNC_Canopy>(ref target.Lfunc7, entity.Dial_Functions_L);
            FindDFUNC<DFUNC_Cruise>(ref target.Lfunc8, entity.Dial_Functions_L);

            FindDFUNC<DFUNC_Gun>(ref target.Rfunc1, entity.Dial_Functions_R);
            FindDFUNC<DFUNC_AAM>(ref target.Rfunc2, entity.Dial_Functions_R);
            FindDFUNC<DFUNC_AGM>(ref target.Rfunc3, entity.Dial_Functions_R);
            FindDFUNC<DFUNC_Bomb>(ref target.Rfunc4, entity.Dial_Functions_R);
            FindDFUNC<DFUNC_Gear>(ref target.Rfunc5, entity.Dial_Functions_R);
            FindDFUNC<DFUNC_Flaps>(ref target.Rfunc6, entity.Dial_Functions_R);
            FindDFUNC<DFUNC_Hook>(ref target.Rfunc7, entity.Dial_Functions_R);
            FindDFUNC<DFUNC_Smoke>(ref target.Rfunc8, entity.Dial_Functions_R);
            target.ApplyProxyModifications();
            EditorUtility.SetDirty(udon);
        }

        if (GUILayout.Button("Set Default Key Bindings"))
        {
            Undo.RecordObject(udon, "Set Default Key Bindings");
            SetDefaultKey(ref target.Lfunc1key, target.Lfunc1);
            SetDefaultKey(ref target.Lfunc2key, target.Lfunc2);
            SetDefaultKey(ref target.Lfunc3key, target.Lfunc3);
            SetDefaultKey(ref target.Lfunc4key, target.Lfunc4);
            SetDefaultKey(ref target.Lfunc5key, target.Lfunc5);
            SetDefaultKey(ref target.Lfunc6key, target.Lfunc6);
            SetDefaultKey(ref target.Lfunc7key, target.Lfunc7);
            SetDefaultKey(ref target.Lfunc8key, target.Lfunc8);
            SetDefaultKey(ref target.Rfunc1key, target.Rfunc1);
            SetDefaultKey(ref target.Rfunc2key, target.Rfunc2);
            SetDefaultKey(ref target.Rfunc3key, target.Rfunc3);
            SetDefaultKey(ref target.Rfunc4key, target.Rfunc4);
            SetDefaultKey(ref target.Rfunc5key, target.Rfunc5);
            SetDefaultKey(ref target.Rfunc6key, target.Rfunc6);
            SetDefaultKey(ref target.Rfunc7key, target.Rfunc7);
            SetDefaultKey(ref target.Rfunc8key, target.Rfunc8);
            target.ApplyProxyModifications();
            EditorUtility.SetDirty(udon);
        }

        serializedObject.ApplyModifiedProperties();

    }
}

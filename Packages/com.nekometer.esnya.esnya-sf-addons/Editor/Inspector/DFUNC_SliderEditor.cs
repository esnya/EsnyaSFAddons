using System;
using System.Linq;
using EsnyaSFAddons.DFUNC;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace EsnyaSFAddons.Editor.Inspector
{
    [CustomEditor(typeof(DFUNC_Slider))]
    public class DFUNC_SliderEditor : UnityEditor.Editor
    {
        private static string[] GetFloatVariableNames(UdonSharpBehaviour behaviour)
        {
            if (behaviour == null) return Array.Empty<string>();
            return SFEditorUtility.ListPublicVariables(behaviour.GetType())
                .Where(f => f.FieldType == typeof(float))
                .Select(f => f.Name)
                .ToArray();
        }

        private static string[] GetAnimatorFloatParameterNames(Animator animator)
        {
            if (animator == null) return Array.Empty<string>();
            var controller = animator.runtimeAnimatorController as AnimatorController;
            if (controller == null) return Array.Empty<string>();
            return controller.parameters
                .Where(p => p.type == AnimatorControllerParameterType.Float)
                .Select(p => p.name)
                .ToArray();
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultValue"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("resetOnPilotExit"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("VR Input", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("vrSensitivity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("vrAxis"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Desktop Input", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("desktopStep"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("desktopIncrease"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("desktopDecrease"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("desktopLoop"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Public Variable", EditorStyles.boldLabel);
            var writePublicVariableProp = serializedObject.FindProperty("writePublicVariable");
            EditorGUILayout.PropertyField(writePublicVariableProp);
            if (writePublicVariableProp.boolValue)
            {
                EditorGUI.indentLevel++;
                var targetBehaviourProp = serializedObject.FindProperty("targetBehaviour");
                EditorGUILayout.PropertyField(targetBehaviourProp);
                var targetBehaviour = targetBehaviourProp.objectReferenceValue as UdonSharpBehaviour;
                var floatVars = GetFloatVariableNames(targetBehaviour);
                ESFAUI.StringPopupOrField(serializedObject.FindProperty("targetVariableName"), new GUIContent("Target Variable Name"), floatVars);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("targetVariableMin"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("targetVariableMax"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animator", EditorStyles.boldLabel);
            var writeAnimatorParameterProp = serializedObject.FindProperty("writeAnimatorParameter");
            EditorGUILayout.PropertyField(writeAnimatorParameterProp);
            if (writeAnimatorParameterProp.boolValue)
            {
                EditorGUI.indentLevel++;
                var targetAnimatorProp = serializedObject.FindProperty("targetAnimator");
                EditorGUILayout.PropertyField(targetAnimatorProp);
                var targetAnimator = targetAnimatorProp.objectReferenceValue as Animator;
                var floatParams = GetAnimatorFloatParameterNames(targetAnimator);
                ESFAUI.StringPopupOrField(serializedObject.FindProperty("targetAnimatorParameterName"), new GUIContent("Target Animator Parameter Name"), floatParams);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Send Events", EditorStyles.boldLabel);

            var sendOnChangeProp = serializedObject.FindProperty("sendOnChange");
            EditorGUILayout.PropertyField(sendOnChangeProp);
            if (sendOnChangeProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onChange"));
                EditorGUI.indentLevel--;
            }

            var sendOnMinProp = serializedObject.FindProperty("sendOnMin");
            EditorGUILayout.PropertyField(sendOnMinProp);
            if (sendOnMinProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onMin"));
                EditorGUI.indentLevel--;
            }

            var sendOnMaxProp = serializedObject.FindProperty("sendOnMax");
            EditorGUILayout.PropertyField(sendOnMaxProp);
            if (sendOnMaxProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onMax"));
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

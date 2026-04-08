using System;
using System.Linq;
using EsnyaSFAddons.DFUNC;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace EsnyaSFAddons.Editor.Inspector
{
    [CustomEditor(typeof(DFUNC_SendCustomEvent))]
    public class DFUNC_SendCustomEventEditor : UnityEditor.Editor
    {
        private static string[] GetEventNames(UdonSharpBehaviour behaviour)
        {
            if (behaviour == null) return Array.Empty<string>();
            return SFEditorUtility.ListCustomEvents(behaviour.GetType())
                .Select(m => m.Name)
                .ToArray();
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();

            var targetProp = serializedObject.FindProperty("target");
            EditorGUILayout.PropertyField(targetProp);
            var targetBehaviour = targetProp.objectReferenceValue as UdonSharpBehaviour;
            var eventNames = GetEventNames(targetBehaviour);

            var networkedProp = serializedObject.FindProperty("networked");
            EditorGUILayout.PropertyField(networkedProp);
            if (networkedProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("networkEventTarget"));
                EditorGUI.indentLevel--;
            }

            var sendOnTriggerPressProp = serializedObject.FindProperty("sendOnTriggerPress");
            EditorGUILayout.PropertyField(sendOnTriggerPressProp);
            if (sendOnTriggerPressProp.boolValue)
            {
                EditorGUI.indentLevel++;
                ESFAUI.StringPopupOrField(serializedObject.FindProperty("onTriggerPress"), new GUIContent("On Trigger Press"), eventNames);
                EditorGUI.indentLevel--;
            }

            var sendOnTriggerReleaseProp = serializedObject.FindProperty("sendOnTriggerRelease");
            EditorGUILayout.PropertyField(sendOnTriggerReleaseProp);
            if (sendOnTriggerReleaseProp.boolValue)
            {
                EditorGUI.indentLevel++;
                ESFAUI.StringPopupOrField(serializedObject.FindProperty("onTriggerRelease"), new GUIContent("On Trigger Release"), eventNames);
                EditorGUI.indentLevel--;
            }

            var sendOnKeyboardInputProp = serializedObject.FindProperty("sendOnKeyboardInput");
            EditorGUILayout.PropertyField(sendOnKeyboardInputProp);
            if (sendOnKeyboardInputProp.boolValue)
            {
                EditorGUI.indentLevel++;
                ESFAUI.StringPopupOrField(serializedObject.FindProperty("onKeyboardInput"), new GUIContent("On Keyboard Input"), eventNames);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

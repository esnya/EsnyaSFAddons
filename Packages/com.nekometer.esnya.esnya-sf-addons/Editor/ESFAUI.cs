using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SaccFlightAndVehicles;

namespace EsnyaSFAddons.Editor
{
    public static class ESFAUI
    {
        /// <summary>
        /// Draws a dropdown popup when <paramref name="options"/> is non-empty, otherwise falls back to a plain text field.
        /// Handles the case where the current serialized value is not present in <paramref name="options"/> by
        /// prepending a read-only placeholder entry so the existing value is not silently overwritten.
        /// </summary>
        public static void StringPopupOrField(SerializedProperty property, GUIContent label, string[] options)
        {
            if (options == null || options.Length == 0)
            {
                EditorGUILayout.PropertyField(property, label);
                return;
            }

            var current = property.stringValue ?? string.Empty;
            var idx = Array.IndexOf(options, current);

            string[] displayOptions;
            int displayIdx;
            if (idx >= 0)
            {
                displayOptions = options;
                displayIdx = idx;
            }
            else
            {
                var placeholder = string.IsNullOrEmpty(current) ? "(none)" : $"(current) {current}";
                displayOptions = new[] { placeholder }.Concat(options).ToArray();
                displayIdx = 0;
            }

            EditorGUI.BeginChangeCheck();
            var newIdx = EditorGUILayout.Popup(label, displayIdx, displayOptions.Select(s => new GUIContent(s)).ToArray());
            if (EditorGUI.EndChangeCheck() && newIdx != displayIdx)
            {
                if (idx >= 0)
                {
                    property.stringValue = options[newIdx];
                }
                else if (newIdx > 0)
                {
                    // User selected an actual option (skip placeholder at index 0)
                    property.stringValue = options[newIdx - 1];
                }
            }
        }


        public static bool MiniButton(string label)
        {
            return GUILayout.Button(label, EditorStyles.miniButton, GUILayout.ExpandWidth(false));
        }
        public static bool BigButton(string label)
        {
            return GUILayout.Button(label, new[] { GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true) });
        }

        public static bool HelpBoxWithAutoFix(string message, MessageType messageType)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.HelpBox(message, messageType);
                return BigButton("Auto Fix");
            }
        }

    }
}
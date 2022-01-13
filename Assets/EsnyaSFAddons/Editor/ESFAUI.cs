using UnityEditor;
using UnityEngine;

namespace EsnyaAircraftAssets
{
    public static class ESFAUI
    {
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
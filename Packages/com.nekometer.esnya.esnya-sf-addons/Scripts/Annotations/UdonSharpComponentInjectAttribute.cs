using System;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Reflection;
using UdonSharp;

#if UNITY_EDITOR
using UnityEditor;
using VRC.SDKBase.Editor.BuildPipeline;
#endif

namespace EsnyaSFAddons.Annotations
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
#if UNITY_EDITOR
    public class UdonSharpComponentInjectAttribute : PropertyAttribute
#else
    public class UdonSharpComponentInjectAttribute : System.Attribute
#endif
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public static void AutoSetup(Scene scene)
        {
            var rootGameObjects = scene.GetRootGameObjects();
            var usharpComponents = rootGameObjects
                .SelectMany(o => o.GetComponentsInChildren<UdonSharpBehaviour>(true))
                .Where(c => c != null)
                .GroupBy(component => component.GetType())
                .SelectMany(group =>
                {
                    var type = group.Key;
                    var fields = type.GetFields().Where(f => f.GetCustomAttribute<UdonSharpComponentInjectAttribute>() != null).ToArray();
                    return group.SelectMany(component => fields.Select(field => (component, field)));
                });

            foreach (var (component, field) in usharpComponents)
            {
                var isArray = field.FieldType.IsArray;
                var valueType = isArray ? field.FieldType.GetElementType() : field.FieldType;
                var isComponent = valueType.IsSubclassOf(typeof(UdonSharpBehaviour));
                var variableName = field.Name;

                if (isArray)
                {
                    var components = isComponent
                        ? rootGameObjects.SelectMany(o => o.GetComponentsInChildren(valueType)).ToArray()
                        : rootGameObjects.SelectMany(o => o.GetComponentsInChildren(valueType)).ToArray();
                    var value = Array.CreateInstance(valueType, components.Length);
                    Array.Copy(components, value, components.Length);
                    field.SetValue(component, value);
                }
                else
                {
                    if (isComponent) field.SetValue(component, rootGameObjects.SelectMany(o => o.GetComponentsInChildren(valueType)).FirstOrDefault());
                    else field.SetValue(component, rootGameObjects.SelectMany(o => o.GetComponentsInChildren(valueType)).FirstOrDefault());
                }

                EditorUtility.SetDirty(component);
            }
        }

        [InitializeOnLoadMethod]
        public static void InitializeOnLoad()
        {
            EditorApplication.playModeStateChanged += (PlayModeStateChange e) =>
            {
                if (e == PlayModeStateChange.EnteredPlayMode) AutoSetup(SceneManager.GetActiveScene());
            };
        }

        public class BuildCallback : IVRCSDKBuildRequestedCallback
        {
            public int callbackOrder => 10;

            public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
            {
                AutoSetup(SceneManager.GetActiveScene());
                return true;
            }
        }

        [CustomPropertyDrawer(typeof(UdonSharpComponentInjectAttribute))]
        public class Drawer : PropertyDrawer
        {
            private const float ButtonWidth = 90f;
            private const float HelpBoxHeight = 38f;
            private const float Padding = 2f;

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                var propertyHeight = EditorGUI.GetPropertyHeight(property, label, true);
                var isArray = property.isArray;
                var buttonHeight = isArray ? EditorGUIUtility.singleLineHeight + Padding : 0f;
                return propertyHeight + buttonHeight + HelpBoxHeight + Padding;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                var propertyHeight = EditorGUI.GetPropertyHeight(property, label, true);
                var isArray = property.isArray;

                var fieldRect = isArray
                    ? new Rect(position.x, position.y, position.width, propertyHeight)
                    : new Rect(position.x, position.y, position.width - ButtonWidth - Padding, propertyHeight);

                EditorGUI.PropertyField(fieldRect, property, label, true);

                float afterFieldY = position.y + propertyHeight + Padding;

                if (isArray)
                {
                    var buttonRect = new Rect(position.x, afterFieldY, ButtonWidth, EditorGUIUtility.singleLineHeight);
                    if (GUI.Button(buttonRect, "Force Update"))
                    {
                        AutoSetup((property.serializedObject.targetObject as Component).gameObject.scene);
                    }
                    afterFieldY += EditorGUIUtility.singleLineHeight + Padding;
                }
                else
                {
                    var buttonRect = new Rect(position.x + position.width - ButtonWidth, position.y, ButtonWidth, EditorGUIUtility.singleLineHeight);
                    if (GUI.Button(buttonRect, "Force Update"))
                    {
                        AutoSetup((property.serializedObject.targetObject as Component).gameObject.scene);
                    }
                }

                var helpRect = new Rect(position.x, afterFieldY, position.width, HelpBoxHeight);
                EditorGUI.HelpBox(helpRect, "Auto injected by script.", MessageType.Info);
            }
        }
#endif
    }
}

using System;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Reflection;
using UdonToolkit;
using UdonSharp;

#if UNITY_EDITOR
using UnityEditor;
using VRC.SDKBase.Editor.BuildPipeline;
#endif

namespace EsnyaSFAddons.Annotations
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
#if UNITY_EDITOR
    public class UdonSharpComponentInjectAttribute : UTPropertyAttribute
#else
    public class UdonSharpComponentInjectAttribute : System.Attribute
#endif
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public override void BeforeGUI(SerializedProperty property)
        {
            if (!property.isArray) EditorGUILayout.BeginHorizontal();
        }
        public override void AfterGUI(SerializedProperty property)
        {
            if (GUILayout.Button("Force Update", GUILayout.ExpandWidth(false)))
            {
                AutoSetup((property.serializedObject.targetObject as Component).gameObject.scene);
            }
            if (!property.isArray) EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("Auto injected by script.", MessageType.Info);
        }

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
                    var value = field.FieldType.GetConstructor(new[] { typeof(int) }).Invoke(new object[] { components.Length });
                    Array.Copy(components, value as Array, components.Length);
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
#endif
    }
}

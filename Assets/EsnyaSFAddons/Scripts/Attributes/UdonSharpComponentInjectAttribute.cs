using System;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Reflection;
using UdonToolkit;
using UdonSharp;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
using UdonSharpEditor;
#endif

namespace EsnyaAircraftAssets
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
#if UNITY_EDITOR
    public class UdonSharpComponentInjectAttribute : UTPropertyAttribute
#else
    public class UdonSharpComponentInjectAttribute : Attribute
#endif
    {
#if UNITY_EDITOR
        public override void BeforeGUI(SerializedProperty property)
        {
            EditorGUI.BeginDisabledGroup(true);
        }
        public override void AfterGUI(SerializedProperty property)
        {
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.HelpBox("Auto injected by script.", MessageType.Info);
        }

        private static void AutoSetup(Scene scene)
        {
            var rootGameObjects = scene.GetRootGameObjects();
            var usharpComponents = rootGameObjects
                .SelectMany(o => o.GetUdonSharpComponentsInChildren<UdonSharpBehaviour>())
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
                        ? rootGameObjects.SelectMany(o => o.GetUdonSharpComponentsInChildren(valueType)).ToArray()
                        : rootGameObjects.SelectMany(o => o.GetUdonSharpComponentsInChildren(valueType)).ToArray();
                    var value = field.FieldType.GetConstructor(new[] { typeof(int) }).Invoke(new object[] { components.Length });
                    Array.Copy(components, value as Array, components.Length);
                    field.SetValue(component, value);
                }
                else
                {
                    if (isComponent) field.SetValue(component, rootGameObjects.SelectMany(o => o.GetUdonSharpComponentsInChildren(valueType)).FirstOrDefault());
                    else field.SetValue(component, rootGameObjects.SelectMany(o => o.GetUdonSharpComponentsInChildren(valueType)).FirstOrDefault());
                }
            }
        }

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            EditorSceneManager.sceneSaving += (scene, _) => AutoSetup(scene);
            SceneManager.activeSceneChanged += (_, next) => AutoSetup(next);
        }
#endif
    }
}

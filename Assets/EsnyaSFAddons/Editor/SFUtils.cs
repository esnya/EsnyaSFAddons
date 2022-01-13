using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace EsnyaAircraftAssets
{
    public static class SFUtils
    {
        public static IEnumerable<FieldInfo> ListPublicVariables(Type type)
        {
            return type
                .GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(field => field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
                .Where(field => field.GetCustomAttribute<NonSerializedAttribute>() == null);
        }

        public static MethodInfo[] ListCustomEvents(Type type)
        {
            return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        }

        public static bool IsExtention(Type type)
        {
            return ListCustomEvents(type).Any(m => m.Name.StartsWith("SFEXT_"));
        }

        public static bool IsDFUNC(Type type)
        {
            return ListCustomEvents(type).Any(m => m.Name.StartsWith("DFUNC_"));
        }

        public static IEnumerable<UdonSharpBehaviour> FindExtentions(GameObject root)
        {
            return root.GetUdonSharpComponentsInChildren<UdonSharpBehaviour>(true).Where(udon => IsExtention(udon.GetType()) && !IsDFUNC(udon.GetType()));
        }

        public static IEnumerable<UdonSharpBehaviour> FindDFUNCs(GameObject root)
        {
            return root.GetUdonSharpComponentsInChildren<UdonSharpBehaviour>(true).Where(udon => IsDFUNC(udon.GetType()));
        }

        public static void SetObjectArrayProperty<T>(SerializedProperty property, IEnumerable<T> enumerable) where T : UnityEngine.Object
        {
            var array = enumerable.ToArray();
            property.arraySize = array.Length;

            for (var i = 0; i < array.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = array[i];
            }
        }

        public static void UndoRecordUdonSharpBehaviour(UdonSharpBehaviour udonSharpBehaviour, string name)
        {
            Undo.RecordObject(udonSharpBehaviour, name);
        }

        public static bool ValidateReference<T>(UdonSharpBehaviour extention, string variableName, T expectedValue, MessageType messageType) where T : class
        {
            if (expectedValue == null || extention.GetProgramVariable(variableName) != null) return false;

            if (ESFAUI.HelpBoxWithAutoFix($"{extention}.{variableName} is not set.", messageType))
            {
                UndoRecordUdonSharpBehaviour(extention, "Auto Fix");
                extention.SetProgramVariable(variableName, expectedValue);
                return true;
            }

            return false;
        }

        public static void AlignMFDFunctions(this SaccEntity entity, VRC_Pickup.PickupHand side)
        {
            var parent = entity.InVehicleOnly?.transform ?? entity.transform;
            var display = FindByName(parent, $"StickDisplay{side.ToString()[0]}")?.transform;
            if (!display) return;

            var functions = Enumerable
                .Range(0, display.childCount)
                .Select(display.GetChild)
                .Where(t => t.gameObject.name.StartsWith("MFD_"))
                .Select((transform, index) => (transform, index))
                .ToArray();

            var count = functions.Length;
            var dialFunctions = side == VRC_Pickup.PickupHand.Left ? entity.Dial_Functions_L : entity.Dial_Functions_R;
            foreach (var (transform, index) in functions)
            {
                var localRotation = Quaternion.AngleAxis(360.0f * index / count, Vector3.back);
                var localPosition = localRotation * Vector3.up * 0.14f;

                Undo.RecordObject(transform, "Align MFD Function");
                transform.localPosition = localPosition;
                transform.localScale = Vector3.one;

                var dialFunction = dialFunctions != null && index < dialFunctions.Length ? dialFunctions[index] : null;

                var displayHighlighter = transform.Find("MFD_display_funcon")?.gameObject;
                if (displayHighlighter)
                {
                    Undo.RecordObject(displayHighlighter.transform, "Align MFD Function");
                    displayHighlighter.transform.position = transform.parent.position;
                    displayHighlighter.transform.localRotation = localRotation;

                    if ((UnityEngine.Object)dialFunction.GetProgramVariable("Dial_Funcon") != displayHighlighter)
                    {
                        var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(dialFunction);
                        Undo.RecordObject(udon, "Align MFD Function");
                        dialFunction.SetProgramVariable("Dial_Funcon", displayHighlighter);
                        dialFunction.ApplyProxyModifications();
                        EditorUtility.SetDirty(udon);
                    }
                }
            }
        }

        public static IEnumerable<GameObject> ListByName(this Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true).OrderBy(t => t.GetHierarchyPath().Count(c => c == '/')).Select(t => t.gameObject).Where(o => o.name == name);
        }

        public static GameObject FindByName(this Transform root, string name)
        {
            return ListByName(root, name).FirstOrDefault();
        }
    }
}

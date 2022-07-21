using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;

namespace EsnyaSFAddons.Editor
{
    /// <summary>
    /// Unityty set on Editor
    /// </summary>
    public static class SFEditorUtility
    {
        /// <summary>
        /// Workaround of UdonSharp editor API
        /// </summary>
        /// <param name="o"></param>
        /// <param name="includeInactive"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<UdonSharpBehaviour> SafeGetUdonSharpComponentsInChildren<T>(this UnityEngine.Object o, bool includeInactive = false) where T : UdonSharpBehaviour
        {
            try
            {
                if (o is GameObject @gameObject) return @gameObject.GetUdonSharpComponentsInChildren<T>(includeInactive);
                if (o is Component @component) return @component.GetUdonSharpComponentsInChildren<T>(includeInactive);
            }
            catch (Exception e)
            {
                Debug.LogException(e, o);
            }
            return Enumerable.Empty<UdonSharpBehaviour>();
        }

        /// <summary>
        /// Workaround of UdonSharp editor API
        /// </summary>
        /// <param name="o"></param>
        /// <param name="includeInactive"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static UdonSharpBehaviour SafeGetUdonSharpComponentInParent<T>(this UnityEngine.Object o) where T : UdonSharpBehaviour
        {
            try
            {
                if (o is GameObject @gameObject) return @gameObject.GetUdonSharpComponentInParent<T>();
                if (o is Component @component) return @component.GetUdonSharpComponentInParent<T>();
            }
            catch (Exception e)
            {
                Debug.LogException(e, o);
            }
            return null;
        }

        /// <summary>
        /// List all infomation of  serializable fields of the given type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEnumerable<FieldInfo> ListPublicVariables(Type type)
        {
            return type
                .GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(field => field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
                .Where(field => field.GetCustomAttribute<NonSerializedAttribute>() == null);
        }

        /// <summary>
        /// List all public method of the given type to find Udon custom events
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static MethodInfo[] ListCustomEvents(Type type)
        {
            return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        }

        /// <summary>
        /// Check given type is SaccFlight Extention
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsExtention(Type type)
        {
            return ListCustomEvents(type).Any(m => m.Name.StartsWith("SFEXT_"));
        }

        /// <summary>
        /// Check given type is SaccFlight Dial Function
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsDFUNC(Type type)
        {
            return ListCustomEvents(type).Any(m => m.Name.StartsWith("DFUNC_"));
        }

        /// <summary>
        /// Find nearest SaccEntity of SAV_PassengerFunctionController
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static UdonSharpBehaviour GetNearestController(GameObject o)
        {
            var controller = o.SafeGetUdonSharpComponentInParent<SAV_PassengerFunctionsController>() ?? o.SafeGetUdonSharpComponentInParent<SaccEntity>();
            if (controller is SAV_PassengerFunctionsController && controller.gameObject == o) return o.SafeGetUdonSharpComponentInParent<SaccEntity>();
            return controller;
        }

        /// <summary>
        /// Is child of controller
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="extention"></param>
        /// <returns></returns>
        public static bool IsChildExtention(UdonSharpBehaviour controller, UdonSharpBehaviour extention)
        {
            return GetNearestController(extention.transform.parent.gameObject) == controller;
        }

        /// <summary>
        /// Find all extentions under given controller
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public static IEnumerable<UdonSharpBehaviour> FindExtentions(UdonSharpBehaviour root)
        {
            return root.SafeGetUdonSharpComponentsInChildren<UdonSharpBehaviour>(true).Where(udon => udon.gameObject != root && IsExtention(udon.GetType()) && !IsDFUNC(udon.GetType()) && IsChildExtention(root, udon));
        }

        /// <summary>
        /// Find all dial functions under given controller
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public static IEnumerable<UdonSharpBehaviour> FindDFUNCs(UdonSharpBehaviour root)
        {
            try
            {
                return root.SafeGetUdonSharpComponentsInChildren<UdonSharpBehaviour>(true).Where(udon => udon.gameObject != root && IsDFUNC(udon.GetType()) && GetNearestController(udon.gameObject) == root);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return Enumerable.Empty<UdonSharpBehaviour>();
            }
        }

        /// <summary>
        /// Find all dial functions under given controller
        /// </summary>
        public static IEnumerable<UdonSharpBehaviour> FindDFUNCs(UdonSharpBehaviour root, string name)
        {
            try
            {
                return FindDFUNCs(root).Where(f => IsChildOfNameRecursive(f.transform, name));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return Enumerable.Empty<UdonSharpBehaviour>();
            }
        }

        /// <summary>
        /// Returns true if name of the parent of child equals name
        /// </summary>
        public static bool IsChildOfNameRecursive(Transform child, string name)
        {
            if (!child?.parent) return false;
            return child.parent.gameObject.name == name || IsChildOfNameRecursive(child.parent, name);
        }

        /// <summary>
        /// Assign array property
        /// </summary>
        /// <param name="property"></param>
        /// <param name="enumerable"></param>
        /// <typeparam name="T"></typeparam>
        public static void SetObjectArrayProperty<T>(SerializedProperty property, IEnumerable<T> enumerable) where T : UnityEngine.Object
        {
            var array = enumerable.ToArray();
            property.arraySize = array.Length;

            for (var i = 0; i < array.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = array[i];
            }
        }

        /// <summary>
        /// Shortcut for Undo.RecordObject
        /// </summary>
        /// <param name="udonSharpBehaviour"></param>
        /// <param name="name"></param>
        public static void UndoRecordUdonSharpBehaviour(UdonSharpBehaviour udonSharpBehaviour, string name)
        {
            Undo.RecordObject(udonSharpBehaviour, name);
        }

        public static bool ValidateReference<T>(UdonSharpBehaviour extention, string variableName, T expectedValue, MessageType messageType, bool forceFix = false) where T : class
        {
            if (expectedValue == null || extention.GetProgramVariable(variableName) != null) return false;

            if (forceFix || ESFAUI.HelpBoxWithAutoFix($"{extention}.{variableName} is not set.", messageType))
            {
                UndoRecordUdonSharpBehaviour(extention, "Auto Fix");
                extention.SetProgramVariable(variableName, expectedValue);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Auto algin mfd functions
        ///
        /// Align positions, rotations and scales. Also sets Dial_Funcon for each DFUNCs.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="side"></param>
        public static void AlignMFDFunctions(this UdonSharpBehaviour entity, VRC_Pickup.PickupHand side)
        {
            var parent = (entity as SaccEntity)?.InVehicleOnly?.transform ?? entity.transform;
            var display = FindByName(parent, $"StickDisplay{side.ToString()[0]}")?.transform;
            if (!display) return;

            var mfds = Enumerable
                .Range(0, display.childCount)
                .Select(display.GetChild)
                .Where(t => t.gameObject.name.StartsWith("MFD_"))
                .Select((transform, index) => (transform, index))
                .ToArray();

            var count = mfds.Length;
            var dialFunctions = (side == VRC_Pickup.PickupHand.Left ? entity.GetProgramVariable(nameof(SaccEntity.Dial_Functions_L)) : entity.GetProgramVariable(nameof(SaccEntity.Dial_Functions_R))) as UdonSharpBehaviour[];
            foreach (var (transform, index) in mfds)
            {
                try
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

                        if (!displayHighlighter.Equals(dialFunction?.GetProgramVariable("Dial_Funcon")))
                        {
                            var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(dialFunction);
                            Undo.RecordObject(udon, "Align MFD Function");
                            if (udon.publicVariables.TryGetVariableType("Dial_Funcon", out var type) && type.IsSubclassOf(typeof(Array)))
                            {
                                dialFunction.SetProgramVariable("Dial_Funcon", new[] { displayHighlighter });
                            }
                            else
                            {
                                dialFunction.SetProgramVariable("Dial_Funcon", displayHighlighter);
                            }
                            dialFunction.ApplyProxyModifications();
                            EditorUtility.SetDirty(udon);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            var background = display.GetComponentsInChildren<MeshFilter>(true)
                .FirstOrDefault(f => f.sharedMesh && f.sharedMesh.name.StartsWith("StickDisplay") && char.IsDigit(f.sharedMesh.name.Last()) || f.sharedMesh.name == "StickDisplay");
            if (background)
            {
                var expectedName = count == 8 ? "StkickDisplay" : $"StickDisplay{count}";
                var expectedMesh = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(background.sharedMesh)).Select(o => o as Mesh).FirstOrDefault(m => m && m.name == expectedName);
                if (expectedMesh && background.sharedMesh != expectedMesh)
                {
                    Undo.RecordObject(background, "Align MFD Function");
                    background.sharedMesh = expectedMesh;
                    EditorUtility.SetDirty(background);
                }
            }
        }

        /// <summary>
        /// List object by name
        /// </summary>
        /// <param name="root"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IEnumerable<GameObject> ListByName(this Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true).OrderBy(t => t.GetHierarchyPath().Count(c => c == '/')).Select(t => t.gameObject).Where(o => o.name == name);
        }

        /// <summary>
        /// Find object by name under root
        /// </summary>
        /// <param name="root"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static GameObject FindByName(this Transform root, string name)
        {
            return ListByName(root, name).FirstOrDefault();
        }

        /// <summary>
        /// Animator parameters
        /// </summary>
        public static (string, AnimatorControllerParameterType)[] AnimatorParameters => new[]{
            // DFUNC_Brake
            ("brake", AnimatorControllerParameterType.Float),

            // DFUNC_Catapult
            ("launch", AnimatorControllerParameterType.Trigger),
            ("oncatapult", AnimatorControllerParameterType.Bool),

            // DFUNC_Gear
            ("instantgeardown", AnimatorControllerParameterType.Trigger),
            ("gearup", AnimatorControllerParameterType.Bool),

            // DFUNC_Hook
            ("hooked", AnimatorControllerParameterType.Trigger),
            ("hookdown", AnimatorControllerParameterType.Bool),

            // SaccAirVehicle
            ("EngineOn", AnimatorControllerParameterType.Bool),

            // SAV_EffectsController
            ("pitchinput", AnimatorControllerParameterType.Float),
            ("yawinput", AnimatorControllerParameterType.Float),
            ("rollinput", AnimatorControllerParameterType.Float),
            ("throttle", AnimatorControllerParameterType.Float),
            ("engineoutput", AnimatorControllerParameterType.Float),
            ("vtolangle", AnimatorControllerParameterType.Float),
            ("health", AnimatorControllerParameterType.Float),
            ("AoA", AnimatorControllerParameterType.Float),
            ("mach10", AnimatorControllerParameterType.Float),
            ("Gs", AnimatorControllerParameterType.Float),
            ("fuel", AnimatorControllerParameterType.Float),
            ("occupied", AnimatorControllerParameterType.Bool),
            ("missilesincoming", AnimatorControllerParameterType.Int),
            ("localpilot", AnimatorControllerParameterType.Bool),
            ("localpassenger", AnimatorControllerParameterType.Bool),
            ("reappear", AnimatorControllerParameterType.Trigger),
            ("dead", AnimatorControllerParameterType.Bool),
            ("afterburneron", AnimatorControllerParameterType.Bool),
            ("resupply", AnimatorControllerParameterType.Trigger),
            ("bullethit", AnimatorControllerParameterType.Trigger),
            ("underwater", AnimatorControllerParameterType.Bool),
            ("onground", AnimatorControllerParameterType.Bool),
            ("onwater", AnimatorControllerParameterType.Bool),
            ("explode", AnimatorControllerParameterType.Trigger),
            ("locked_aam", AnimatorControllerParameterType.Trigger),
        };
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UdonSharp;
using UdonSharp.Serialization;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Graph.NodeRegistries;

namespace EsnyaAircraftAssets
{
    [CustomEditor(typeof(SaccEntity))]
    public class SaccEntityEditor : Editor
    {
        private static bool MiniButton(string label)
        {
            return GUILayout.Button(label, EditorStyles.miniButton, GUILayout.ExpandWidth(false));
        }
        private static bool BigButton(string label)
        {
            return GUILayout.Button(label, new[] { GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true) });
        }

        private static void SetObjectArrayProperty<T>(SerializedProperty property, IEnumerable<T> enumerable) where T : UnityEngine.Object
        {
            var array = enumerable.ToArray();
            property.arraySize = array.Length;

            for (var i = 0; i < array.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = array[i];
            }
        }

        private static bool HelpBoxWithAutoFix(string message, MessageType messageType)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.HelpBox(message, messageType);
                return BigButton("Auto Fix");
            }
        }

        private static void UndoRecordUdonSharpBehaviour(UdonSharpBehaviour udonSharpBehaviour, string name)
        {
            Undo.RecordObject(udonSharpBehaviour, name);
        }

        private static bool ValidateReference<T>(UdonSharpBehaviour extention, string variableName, T expectedValue, MessageType messageType) where T : class
        {
            if (expectedValue == null || extention.GetProgramVariable(variableName) != null) return false;

            if (HelpBoxWithAutoFix($"{extention}.{variableName} is not set.", messageType))
            {
                UndoRecordUdonSharpBehaviour(extention, "Auto Fix");
                extention.SetProgramVariable(variableName, expectedValue);
                return true;
            }

            return false;
        }

        private static void ValidationGUI(SaccEntity entity)
        {
            var extentions = SFUtils.FindExtentions(entity.gameObject);
            var dfuncs = SFUtils.FindDFUNCs(entity.gameObject);
            var seats = entity.GetUdonSharpComponentsInChildren<SaccVehicleSeat>(true);
            var animator = entity.GetComponent<Animator>();
            var airVehicle = extentions.FirstOrDefault(e => e is SaccAirVehicle) as SaccAirVehicle;
            var savSoundController = extentions.FirstOrDefault(e => e is SAV_SoundController);

            var others = entity.GetUdonSharpComponentsInChildren<SaccResupplyTrigger>(true).Select(t => t as UdonSharpBehaviour)
                .Concat(entity.GetUdonSharpComponentsInChildren<SAV_AAMController>(true))
                .Concat(entity.GetUdonSharpComponentsInChildren<SAV_AGMController>(true));

            foreach (var extention in extentions.Concat(dfuncs).Concat(seats).Concat(others))
            {
                var isDirty = false;

                var fields = SFUtils.ListPublicVariables(extention.GetType());
                foreach (var field in fields)
                {
                    var value = extention.GetProgramVariable(field.Name);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (value == null)
                        {
                            if (field.FieldType == typeof(SaccEntity))
                            {
                                isDirty = isDirty || ValidateReference(extention, field.Name, entity, MessageType.Warning);
                            }
                            else if (field.FieldType == typeof(Animator))
                            {
                                isDirty = isDirty || ValidateReference(extention, field.Name, animator, MessageType.Info);
                            }
                            else if (field.FieldType == typeof(UdonSharpBehaviour))
                            {
                                if (field.Name == "SAVControl") isDirty = isDirty || ValidateReference(extention, field.Name, airVehicle, MessageType.Warning);
                                else if (field.Name == "SoundControl") isDirty = isDirty || ValidateReference(extention, field.Name, savSoundController, MessageType.Warning);
                            }
                            else if (extention is SaccAirVehicle && field.Name == "VehicleMesh" || extention is SAV_SyncScript && field.Name == "VehicleTransform")
                            {
                                isDirty = isDirty || ValidateReference(extention, field.Name, entity.transform, MessageType.Warning);
                            }
                        }
                    }
                }

                if (isDirty)
                {
                    extention.ApplyProxyModifications();
                    EditorUtility.SetDirty(UdonSharpEditorUtility.GetBackingUdonBehaviour(extention));
                }
            }

            if (airVehicle && airVehicle.VehicleMesh && airVehicle.VehicleMesh.gameObject.layer != LayerMask.NameToLayer("Walkthrough") && HelpBoxWithAutoFix($"VehicleMesh must be on the layer Walkthrough.", MessageType.Error))
            {
                Undo.RecordObject(airVehicle.VehicleMesh.gameObject, "Auto Fix");
                airVehicle.VehicleMesh.gameObject.layer = LayerMask.NameToLayer("Walkthrough");
            }

            if (entity.InVehicleOnly != null && entity.InVehicleOnly.activeSelf && HelpBoxWithAutoFix($"InVehicleOnly should be deactivated.", MessageType.Warning))
            {
                Undo.RecordObject(entity.InVehicleOnly, "Auto Fix");
                entity.InVehicleOnly.SetActive(false);
            }
        }

        private static void AlignMFDFunctions(SaccEntity entity, VRC_Pickup.PickupHand side)
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

        private static IEnumerable<GameObject> ListByName(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true).OrderBy(t => t.GetHierarchyPath().Count(c => c == '/')).Select(t => t.gameObject).Where(o => o.name == name);
        }
        private static GameObject FindByName(Transform root, string name)
        {
            return ListByName(root, name).FirstOrDefault();
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            var entity = target as SaccEntity;

            serializedObject.Update();

            ValidationGUI(entity);

            var property = serializedObject.GetIterator();
            property.NextVisible(true);

            while (property.NextVisible(false))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(property, true);
                    if (property.name == nameof(SaccEntity.ExtensionUdonBehaviours))
                    {
                        if (MiniButton("Find")) SetObjectArrayProperty(property, SFUtils.FindExtentions(entity.gameObject));
                    }
                    else if (property.name == nameof(SaccEntity.Dial_Functions_L))
                    {
                        if (MiniButton("Find")) SetObjectArrayProperty(property, SFUtils.FindDFUNCs(entity.gameObject).Where(dfunc => dfunc.transform.parent.gameObject.name.EndsWith("L")));
                        if (MiniButton("Align")) AlignMFDFunctions(entity, VRC_Pickup.PickupHand.Left);
                    }
                    else if (property.name == nameof(SaccEntity.Dial_Functions_R))
                    {
                        if (MiniButton("Find")) SetObjectArrayProperty(property, SFUtils.FindDFUNCs(entity.gameObject).Where(dfunc => dfunc.transform.parent.gameObject.name.EndsWith("R")));
                        if (MiniButton("Align")) AlignMFDFunctions(entity, VRC_Pickup.PickupHand.Right);
                    }
                    else if (property.name == nameof(SaccEntity.InVehicleOnly) || property.name == nameof(SaccEntity.HoldingOnly))
                    {
                        if (MiniButton("Find")) property.objectReferenceValue = FindByName(entity.transform, property.name);
                    }
                    else if (property.name == nameof(SaccEntity.CenterOfMass) || property.name == nameof(SaccEntity.LStickDisplayHighlighter) || property.name == nameof(SaccEntity.RStickDisplayHighlighter))
                    {
                        if (MiniButton("Find")) property.objectReferenceValue = FindByName(entity.transform, property.name)?.transform;
                    }
                    else if (property.name == nameof(SaccEntity.SwitchFunctionSound))
                    {
                        if (MiniButton("Find")) property.objectReferenceValue = ListByName(entity.transform, property.name).Select(o => o.GetComponent<AudioSource>()).FirstOrDefault();
                    }
                    else if (property.name == nameof(SaccEntity.DisableAfter10Seconds))
                    {
                        if (MiniButton("Find")) SetObjectArrayProperty(property, ListByName(entity.transform, property.name));
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

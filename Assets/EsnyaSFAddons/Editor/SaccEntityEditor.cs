using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UdonSharp;
using UdonSharp.Serialization;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
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
            return GUILayout.Button(label, new [] { GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true) });
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
            EditorGUILayout.HelpBox(message, messageType);
            return BigButton("Auto Fix");
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
            var airVehicle = extentions.FirstOrDefault(e => e is SaccAirVehicle);
            var savSoundController = extentions.FirstOrDefault(e => e is SAV_SoundController);
            var resupplyTriggers = entity.GetUdonSharpComponentsInChildren<SaccResupplyTrigger>(true);

            foreach (var extention in extentions.Concat(dfuncs).Concat(seats).Concat(resupplyTriggers))
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

            if (entity.InVehicleOnly != null && entity.InVehicleOnly.activeSelf && HelpBoxWithAutoFix($"InVehicleOnly should be deactivated.", MessageType.Warning))
            {
                Undo.RecordObject(entity.InVehicleOnly, "Auto Fix");
                entity.InVehicleOnly.SetActive(false);
            }
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
                    }
                    else if (property.name == nameof(SaccEntity.Dial_Functions_R))
                    {
                        if (MiniButton("Find")) SetObjectArrayProperty(property, SFUtils.FindDFUNCs(entity.gameObject).Where(dfunc => dfunc.transform.parent.gameObject.name.EndsWith("R")));
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

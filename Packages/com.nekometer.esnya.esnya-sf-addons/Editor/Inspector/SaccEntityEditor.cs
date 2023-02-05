using System.Collections.Generic;
using System.Linq;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;

namespace EsnyaSFAddons.Editor.Inspector
{
    [CustomEditor(typeof(SaccEntity))]
    public class SaccEntityEditor : UnityEditor.Editor
    {
        private static void ValidationGUI(SaccEntity entity)
        {
            EditorGUILayout.LabelField("SaccEntity Validator");
            var fixAll = GUILayout.Button("Fix All");

            var extentions = SFEditorUtility.FindExtentions(entity);
            var dfuncs = SFEditorUtility.FindDFUNCs(entity);
            var seats = entity.GetComponentsInChildren<SaccVehicleSeat>(true);
            var animator = entity.GetComponent<Animator>();
            var airVehicle = extentions.FirstOrDefault(e => e is SaccAirVehicle) as SaccAirVehicle;
            var savSoundController = extentions.FirstOrDefault(e => e is SAV_SoundController);

            var others = entity.GetComponentsInChildren<SaccResupplyTrigger>(true).Select(t => t as UdonSharpBehaviour)
                .Concat(entity.GetComponentsInChildren<SAV_AAMController>(true))
                .Concat(entity.GetComponentsInChildren<SAV_AGMController>(true));

            foreach (var extention in extentions.Concat(dfuncs).Concat(seats).Concat(others))
            {
                var isDirty = false;

                var fields = SFEditorUtility.ListPublicVariables(extention.GetType());
                foreach (var field in fields)
                {
                    var value = extention.GetProgramVariable(field.Name);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (value == null)
                        {
                            if (field.FieldType == typeof(SaccEntity))
                            {
                                isDirty = isDirty || SFEditorUtility.ValidateReference(extention, field.Name, entity, MessageType.Warning, fixAll);
                            }
                            else if (field.FieldType == typeof(Animator))
                            {
                                isDirty = isDirty || SFEditorUtility.ValidateReference(extention, field.Name, animator, MessageType.Info, fixAll);
                            }
                            else if (field.FieldType == typeof(UdonSharpBehaviour))
                            {
                                if (field.Name == "SAVControl") isDirty = isDirty || SFEditorUtility.ValidateReference(extention, field.Name, airVehicle, MessageType.Warning, fixAll);
                                else if (field.Name == "SoundControl") isDirty = isDirty || SFEditorUtility.ValidateReference(extention, field.Name, savSoundController, MessageType.Warning, fixAll);
                            }
                            else if (extention is SaccAirVehicle && field.Name == "VehicleMesh" || extention is SAV_SyncScript && field.Name == "VehicleTransform")
                            {
                                isDirty = isDirty || SFEditorUtility.ValidateReference(extention, field.Name, entity.transform, MessageType.Warning, fixAll);
                            }
                        }
                    }
                }

                if (isDirty)
                {
                    EditorUtility.SetDirty(extention);
                }
            }

            if (airVehicle && airVehicle.VehicleMesh && airVehicle.VehicleMesh.gameObject.layer != LayerMask.NameToLayer("Walkthrough") && ESFAUI.HelpBoxWithAutoFix($"VehicleMesh must be on the layer Walkthrough.", MessageType.Error))
            {
                Undo.RecordObject(airVehicle.VehicleMesh.gameObject, "Auto Fix");
                airVehicle.VehicleMesh.gameObject.layer = LayerMask.NameToLayer("Walkthrough");
            }

            if (entity.InVehicleOnly != null && entity.InVehicleOnly.activeSelf && ESFAUI.HelpBoxWithAutoFix($"InVehicleOnly should be deactivated.", MessageType.Warning))
            {
                Undo.RecordObject(entity.InVehicleOnly, "Auto Fix");
                entity.InVehicleOnly.SetActive(false);
            }

            var animatorController = animator.runtimeAnimatorController as AnimatorController;
            if (animatorController)
            {
                var existingParameters = animatorController.parameters;
                foreach (var (name, type) in SFEditorUtility.AnimatorParameters)
                {
                    if (!existingParameters.Any(p => p.name == name))
                    {
                        if (ESFAUI.HelpBoxWithAutoFix($"VehicleAnimator does not have parameter \"{name}\"", MessageType.Warning))
                        {
                            (animator.runtimeAnimatorController as AnimatorController).AddParameter(name, type);
                            EditorUtility.SetDirty(animator);
                        }
                    }
                }
            }

            if (entity.InVehicleOnly)
            {
                if (fixAll || ESFAUI.HelpBoxWithAutoFix($"InVehicleOnly is deprecated. Use EnableInVehicle", MessageType.Warning))
                {
                    Undo.RecordObjects(new Object[] { entity.InVehicleOnly, entity }, "Auto Fix");
                    entity.InVehicleOnly.name = "EnableInVehicle";
                    entity.InVehicleOnly.tag = "EnableInVehicle";
                    EditorUtility.SetDirty(entity);
                    entity.EnableInVehicle = (entity.EnableInVehicle ?? Enumerable.Empty<GameObject>()).Append(entity.InVehicleOnly).Where(o => o != null).ToArray();
                    entity.InVehicleOnly = null;
                    EditorUtility.SetDirty(entity);
                }
            }
        }

        private void OnDisable()
        {
            DisableAllPreview();
        }

        private readonly Dictionary<string, bool> previewStatus = new Dictionary<string, bool>();
        private void SetPreview(SerializedProperty property, bool value)
        {
            var gameObject = property.objectReferenceValue as GameObject;
            if (!gameObject) return;

            var key = property.propertyPath;
            previewStatus.TryGetValue(key, out var currentState);

            if (currentState == value) return;

            Undo.RecordObject(gameObject, "Preview");
            previewStatus[key] = value;
            gameObject.SetActive(value);
        }
        private void ArraySetPreview(SerializedProperty property, bool value)
        {
            for (var i = 0; i < property.arraySize; i++) SetPreview(property.GetArrayElementAtIndex(i), value);
        }

        private void DisableAllPreview()
        {
            foreach (var path in previewStatus.Keys.ToArray())
            {
                SetPreview(serializedObject.FindProperty(path), false);
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
                    if (property.name == nameof(SaccEntity.InVehicleOnly) && !property.objectReferenceValue) continue;

                    EditorGUILayout.PropertyField(property, true);
                    if (property.name == nameof(SaccEntity.ExtensionUdonBehaviours))
                    {
                        if (ESFAUI.MiniButton("Find")) SFEditorUtility.SetObjectArrayProperty(property, SFEditorUtility.FindExtentions(entity));
                    }
                    else if (property.name == nameof(SaccEntity.Dial_Functions_L))
                    {
                        if (ESFAUI.MiniButton("Find")) SFEditorUtility.SetObjectArrayProperty(property, SFEditorUtility.FindDFUNCs(entity, "DialFunctions_L").Concat(SFEditorUtility.FindDFUNCs(entity, "L")));
                        if (ESFAUI.MiniButton("Align")) SFEditorUtility.AlignMFDFunctions(entity, VRC_Pickup.PickupHand.Left);
                    }
                    else if (property.name == nameof(SaccEntity.Dial_Functions_R))
                    {
                        if (ESFAUI.MiniButton("Find")) SFEditorUtility.SetObjectArrayProperty(property, SFEditorUtility.FindDFUNCs(entity, "DialFunctions_R").Concat(SFEditorUtility.FindDFUNCs(entity, "L")));
                        if (ESFAUI.MiniButton("Align")) SFEditorUtility.AlignMFDFunctions(entity, VRC_Pickup.PickupHand.Right);
                    }
                    else if (property.name == nameof(SaccEntity.InVehicleOnly) || property.name == nameof(SaccEntity.HoldingOnly))
                    {
                        if (ESFAUI.MiniButton("Preview")) SetPreview(property, true);
                        if (ESFAUI.MiniButton("Find by Name")) property.objectReferenceValue = entity.transform.FindByName(property.name);
                        if (ESFAUI.MiniButton("Find by Tag")) property.objectReferenceValue = entity.transform.FindByTag(property.name);
                    }
                    else if (property.name == nameof(SaccEntity.CenterOfMass) || property.name == nameof(SaccEntity.LStickDisplayHighlighter) || property.name == nameof(SaccEntity.RStickDisplayHighlighter))
                    {
                        if (ESFAUI.MiniButton("Find by Name")) property.objectReferenceValue = entity.transform.FindByName(property.name)?.transform;
                        if (ESFAUI.MiniButton("Find by Tag")) property.objectReferenceValue = entity.transform.FindByTag(property.name)?.transform;
                    }
                    else if (property.name == nameof(SaccEntity.SwitchFunctionSound))
                    {
                        if (ESFAUI.MiniButton("Find")) property.objectReferenceValue = entity.transform.ListByName(property.name).Select(o => o.GetComponent<AudioSource>()).FirstOrDefault();
                    }
                    else if (property.name == nameof(SaccEntity.EnableInVehicle))
                    {
                        if (ESFAUI.MiniButton("Preview")) ArraySetPreview(property, true);
                        if (ESFAUI.MiniButton("Find by Name")) {
                            SFEditorUtility.SetObjectArrayProperty(property, entity.transform.ListByName(property.name));
                        }
                        if (ESFAUI.MiniButton("Find by Tag")) {
                            SFEditorUtility.SetObjectArrayProperty(property, entity.transform.ListByTag(property.name));
                        }
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

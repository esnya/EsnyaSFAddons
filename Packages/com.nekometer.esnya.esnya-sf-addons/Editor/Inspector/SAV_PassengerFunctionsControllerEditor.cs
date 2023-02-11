using System.Linq;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;

namespace EsnyaSFAddons.Editor.Inspector
{
    [CustomEditor(typeof(SAV_PassengerFunctionsController))]
    public class SAV_PassengerFunctionsControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            var controller = target as SAV_PassengerFunctionsController;

            serializedObject.Update();

            var property = serializedObject.GetIterator();
            property.NextVisible(true);

            while (property.NextVisible(false))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(property, true);
                    if (property.name == nameof(SAV_PassengerFunctionsController.PassengerExtensions))
                    {
                        if (ESFAUI.MiniButton("Find")) SFEditorUtility.SetObjectArrayProperty(property, SFEditorUtility.FindExtentions(controller));
                    }
                    else if (property.name == nameof(SAV_PassengerFunctionsController.Dial_Functions_L))
                    {
                        if (ESFAUI.MiniButton("Find")) SFEditorUtility.SetObjectArrayProperty(property, SFEditorUtility.FindDFUNCs(controller).Where(dfunc => dfunc.transform.parent.gameObject.name.EndsWith("L")));
                        if (ESFAUI.MiniButton("Align")) SFEditorUtility.AlignMFDFunctions(controller, VRC_Pickup.PickupHand.Left);
                    }
                    else if (property.name == nameof(SAV_PassengerFunctionsController.Dial_Functions_R))
                    {
                        if (ESFAUI.MiniButton("Find")) SFEditorUtility.SetObjectArrayProperty(property, SFEditorUtility.FindDFUNCs(controller).Where(dfunc => dfunc.transform.parent.gameObject.name.EndsWith("R")));
                        if (ESFAUI.MiniButton("Align")) SFEditorUtility.AlignMFDFunctions(controller, VRC_Pickup.PickupHand.Right);
                    }
                    else if (property.name == nameof(SAV_PassengerFunctionsController.SwitchFunctionSound))
                    {
                        if (ESFAUI.MiniButton("Find")) property.objectReferenceValue = controller.transform.ListByName(property.name).Select(o => o.GetComponent<AudioSource>()).FirstOrDefault();
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

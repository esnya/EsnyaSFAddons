using System;
using UnityEngine;
using System.Linq;
using UdonSharp;
using System.Reflection;
using UnityEngine.SceneManagement;
using VRC.Udon;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
using UdonSharpEditor;
#endif

namespace EsnyaAircraftAssets
{
    public class MFD_Function : MonoBehaviour
    {
        public string dfuncType = nameof(DFUNC_Gun);
        public GameObject displayHighlighter;


#if UNITY_EDITOR
        private void Setup()
        {
            hideFlags = HideFlags.DontSaveInBuild;

            var entity = gameObject.GetUdonSharpComponentInParent<SaccEntity>();
            if (entity == null) return;
            var dfunc = entity.GetUdonSharpComponentsInChildren<UdonSharpBehaviour>(true).FirstOrDefault(u => u.GetType().Name == dfuncType);
            if (dfunc == null) return;

            var mfds = transform.parent.GetComponentsInChildren<MFD_Function>(true).ToList();
            var index = mfds.IndexOf(this);
            if (index >= 0)
            {
                var count = mfds.Count;
                var localRotation = Quaternion.AngleAxis(360.0f * index / count, Vector3.back);
                var localPosition = localRotation * Vector3.up * 0.14f;
                if (Vector3.Distance(transform.localPosition, localPosition) > 0.0001f) transform.localPosition = localPosition;
                if (displayHighlighter)
                {
                    if (Vector3.Distance(displayHighlighter.transform.position, transform.parent.position) > 0.0001f) displayHighlighter.transform.position = transform.parent.position;
                    if (Quaternion.Angle(displayHighlighter.transform.localRotation, localRotation) > 0.001f) displayHighlighter.transform.localRotation = localRotation;
                }
            }

            if (displayHighlighter)
            {
                try
                {
                    dfunc.SetProgramVariable("Dial_Funcon", displayHighlighter);
                    dfunc.ApplyProxyModifications();
                    EditorUtility.SetDirty(UdonSharpEditorUtility.GetBackingUdonBehaviour(dfunc));
                }
                catch (Exception e) { } // ToDo: i.e. DFUNC_ToggleBool
            }
        }

        private void OnValidate() => Setup();

        public static void SetupAll(Scene scene)
        {
            foreach (var c in scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<MFD_Function>())) c.Setup();
        }

        [InitializeOnLoadMethod]
        public static void RegisterCallbacks()
        {
            EditorApplication.playModeStateChanged += (_) => SetupAll(EditorSceneManager.GetActiveScene());
            EditorSceneManager.sceneSaving += (scene, _) => SetupAll(scene);
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MFD_Function))]
    public class MFD_FunctionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var dfuncTypes = SceneManager.GetActiveScene()
                .GetRootGameObjects()
                .SelectMany(o => o.GetComponentsInChildren<UdonBehaviour>(true))
                .Where(u => u.programSource != null && UdonSharpEditorUtility.IsUdonSharpBehaviour(u))
                .Select(u => UdonSharpEditorUtility.GetUdonSharpBehaviourType(u))
                .Where(u => u != null)
                .OrderBy(u => u.Name)
                .Distinct()
                .Where(u => u.GetMethods(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public).Any(m => m.Name.StartsWith("DFUNC_")))
                .Select(u => u.Name)
                .ToList();

            var property = serializedObject.GetIterator();
            property.NextVisible(true);

            do
            {
                if (property.name == nameof(MFD_Function.dfuncType))
                {
                    var index = Mathf.Max(dfuncTypes.IndexOf(property.stringValue), 0);
                    index = EditorGUILayout.Popup("DFUNC", index, dfuncTypes.ToArray());
                    property.stringValue = dfuncTypes[index];
                }
                else EditorGUILayout.PropertyField(property, true);
            }
            while (property.NextVisible(false));

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Align")) MFD_Function.SetupAll((target as Component).gameObject.scene);
        }
    }
#endif
}

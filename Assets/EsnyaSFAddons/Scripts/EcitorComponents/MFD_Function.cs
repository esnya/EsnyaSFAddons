using System;
using UnityEngine;
using System.Linq;
using UdonSharp;
using System.Reflection;
using UnityEngine.SceneManagement;
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

            var indexL = entity.Dial_Functions_L.ToList().IndexOf(dfunc);
            var indexR = entity.Dial_Functions_R.ToList().IndexOf(dfunc);
            if (indexL >= 0 || indexR >= 0)
            {
                var count = indexL >= 0 ? entity.Dial_Functions_L.Length : entity.Dial_Functions_R.Length;
                var dfuncIndex = Mathf.Max(indexL, indexR);
                var localRotation = Quaternion.AngleAxis(360.0f * dfuncIndex / count, Vector3.back);
                transform.localPosition = localRotation * Vector3.up * 0.15f;
                if (displayHighlighter)
                {
                    displayHighlighter.transform.position = transform.parent.position;
                    displayHighlighter.transform.localRotation = localRotation;
                }
            }

            if (displayHighlighter)
            {
                dfunc.SetProgramVariable("Dial_Funcon", displayHighlighter);
                dfunc.ApplyProxyModifications();
                EditorUtility.SetDirty(UdonSharpEditorUtility.GetBackingUdonBehaviour(dfunc));
            }
        }

        private void OnValidate() => Setup();

        private static void SetupAll(Scene scene)
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

            var dfuncTypes = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(o => o.GetUdonSharpComponentsInChildren<UdonSharpBehaviour>(true))
                .Select(u => u.GetType())
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
        }
    }
#endif
}

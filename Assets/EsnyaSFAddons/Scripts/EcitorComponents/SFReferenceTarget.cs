using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UdonSharpEditor;
#endif

namespace EsnyaAircraftAssets
{
    public class SFReferenceTarget : MonoBehaviour
    {
        public enum ReferenceType
        {
            GameObject,
            Transform,
            AudioSource,
        }

        public ReferenceType referenceType;
        public string variableName;
        public bool forceDeactivate;

#if UNITY_EDITOR
        private void Setup(bool deactivate = true)
        {
            hideFlags = HideFlags.DontSaveInBuild;

            if (string.IsNullOrEmpty(variableName)) return;

            var entity = this.GetUdonSharpComponentInParent<SaccEntity>();
            if (!entity) return;

            switch (referenceType)
            {
                case ReferenceType.GameObject: entity.SetProgramVariable(variableName, gameObject); break;
                case ReferenceType.Transform: entity.SetProgramVariable(variableName, transform); break;
                case ReferenceType.AudioSource: entity.SetProgramVariable(variableName, GetComponent<AudioSource>()); break;
            }

            entity.ApplyProxyModifications();
            EditorUtility.SetDirty(UdonSharpEditorUtility.GetBackingUdonBehaviour(entity));

            if (deactivate && forceDeactivate) gameObject.SetActive(false);
        }

        private void OnValidate() => Setup(false);

        private static void SetupAll(Scene scene)
        {
            foreach (var c in scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<SFReferenceTarget>())) c.Setup();
        }

        [InitializeOnLoadMethod]
        public static void RegisterCallbacks()
        {
            EditorApplication.playModeStateChanged += (_) => SetupAll(EditorSceneManager.GetActiveScene());
            EditorSceneManager.sceneSaving += (scene, _) => SetupAll(scene);
        }
#endif
    }
}

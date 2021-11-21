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
            GameObjectArray,
        }

        public ReferenceType referenceType;
        public string variableName;
        public bool forceDeactivate;

#if UNITY_EDITOR
        private object GetValue(SaccEntity entity)
        {
            switch (referenceType)
            {
                case ReferenceType.GameObject: return gameObject;
                case ReferenceType.Transform: return transform;
                case ReferenceType.AudioSource: return GetComponent<AudioSource>();
                case ReferenceType.GameObjectArray:
                    return (entity.GetProgramVariable(variableName) as GameObject[] ?? new GameObject[] {}).Append(gameObject).Distinct().ToArray();
            }

            return null;
        }

        private void Setup(bool deactivate = true)
        {
            hideFlags = HideFlags.DontSaveInBuild;

            if (string.IsNullOrEmpty(variableName)) return;

            var entity = this.GetUdonSharpComponentInParent<SaccEntity>();
            if (!entity) return;

            var value = GetValue(entity);
            if (!value.Equals(entity.GetProgramVariable(variableName)))
            {
                entity.SetProgramVariable(variableName, value);

                entity.ApplyProxyModifications();
                EditorUtility.SetDirty(UdonSharpEditorUtility.GetBackingUdonBehaviour(entity));
            }

            if (deactivate && forceDeactivate && gameObject.activeSelf) gameObject.SetActive(false);
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

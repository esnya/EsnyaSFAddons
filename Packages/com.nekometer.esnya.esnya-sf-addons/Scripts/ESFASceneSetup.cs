using EsnyaSFAddons.Accesory;
using UdonSharp;
using UnityEngine;
using SaccFlightAndVehicles;

#if UNITY_EDITOR
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEditor;
using VRC.SDKBase.Editor.BuildPipeline;
#endif

namespace EsnyaSFAddons
{
    public class ESFASceneSetup : MonoBehaviour
    {
        [Header("World Configuration")]
        public Transform sea;
        public bool repeatingWorld = true;
        public float repeatingWorldDistance = 20000;

        [Header("Inject Extentions")]
        public UdonSharpBehaviour[] injectExtentions = { };

#if UNITY_EDITOR
        private void Reset()
        {
            sea = GameObject.Find("SF_SEA")?.transform;
        }

        public static void SetupAll()
        {
            foreach (var setup in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<ESFASceneSetup>()))
            {
                setup.Setup();
            }
        }

        public void Setup()
        {
            var rootObjects = gameObject.scene.GetRootGameObjects();

            foreach (var entity in rootObjects.SelectMany(o => o.GetComponentsInChildren<SaccEntity>(true)))
            {
                var airVehicle = entity.ExtensionUdonBehaviours.FirstOrDefault(e => e is SaccAirVehicle);
                var extensions = injectExtentions.Select(template =>
                {
                    var extension = Instantiate(template, entity.transform);
                    extension.name = template.name;

                    extension.SetProgramVariable("EntityControl", entity);
                    extension.SetProgramVariable("AirVehicle", airVehicle);

                    return extension;
                });

                entity.ExtensionUdonBehaviours = entity.ExtensionUdonBehaviours.Concat(extensions).ToArray();
            }

            foreach (var airVehicle in rootObjects.SelectMany(o => o.GetComponentsInChildren<SaccAirVehicle>(true)))
            {
                if (sea)
                {
                    airVehicle.SeaLevel = sea.position.y;
                }

                airVehicle.RepeatingWorld = repeatingWorld;
                airVehicle.RepeatingWorldDistance = repeatingWorldDistance;
            }

            var windchanger = rootObjects.Select(o => o.GetComponentInChildren<SAV_WindChanger>()).Where(c => c).FirstOrDefault();
            foreach (var windsock in rootObjects.SelectMany(o => o.GetComponentsInChildren<Windsock>(true)))
            {
                windsock.windChanger = windchanger;
            }

            Destroy(this);
        }

        [InitializeOnLoadMethod]
        public static void InitializeOnLoad()
        {
            EditorApplication.playModeStateChanged += (PlayModeStateChange e) => {
                if (e == PlayModeStateChange.EnteredPlayMode) SetupAll();
            };
        }

        public class BuildCallback : IVRCSDKBuildRequestedCallback
        {
            public int callbackOrder => 11;

            public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
            {
                SetupAll();
                return true;
            }
        }
#endif
    }
}

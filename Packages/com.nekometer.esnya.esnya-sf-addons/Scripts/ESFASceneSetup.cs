using EsnyaSFAddons.Accesory;
using UdonSharp;
using UnityEngine;
using SaccFlightAndVehicles;

#if UNITY_EDITOR
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEditor;
using UdonSharpEditor;
using VRC.SDKBase.Editor.BuildPipeline;
#endif

namespace EsnyaSFAddons
{
    [DefaultExecutionOrder(-20)]
    public class ESFASceneSetup : MonoBehaviour
    {
        [Header("World Configuration")]
        public Transform sea;
        public bool repeatingWorld = true;
        public float repeatingWorldDistance = 20000;
        public SaccScoreboard_Kills killsBoard;

        [Header("Inject Extentions")]
        public UdonSharpBehaviour[] injectExtentions = { };

#if UNITY_EDITOR
        private void Awake()
        {
            Setup();
        }

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
                    var obj = Instantiate(template.gameObject, entity.transform);
                    var extension = obj.GetComponent(template.GetType()) as UdonSharpBehaviour;
                    obj.name = template.gameObject.name;

                    extension.SetProgramVariable("EntityControl", entity);
                    extension.SetProgramVariable("AirVehicle", airVehicle);

                    UdonSharpEditorUtility.CopyProxyToUdon(extension);

                    return extension;
                });

                if (killsBoard)
                {
                    var killTracker = entity.GetExtention(UdonSharpBehaviour.GetUdonTypeName<SAV_KillTracker>()) as SAV_KillTracker;
                    if (killTracker)
                    {
                        killTracker.KillsBoard = killsBoard;
                        UdonSharpEditorUtility.CopyProxyToUdon(killTracker);
                    }

                    var killPenalty = entity.GetExtention(UdonSharpBehaviour.GetUdonTypeName<SFEXT_KillPenalty>()) as SFEXT_KillPenalty;
                    if (killPenalty)
                    {
                        killPenalty.KillsBoard = killsBoard;
                        UdonSharpEditorUtility.CopyProxyToUdon(killPenalty);
                    }
                }

                if (injectExtentions.Length > 0) entity.ExtensionUdonBehaviours = entity.ExtensionUdonBehaviours.Concat(extensions).ToArray();
                UdonSharpEditorUtility.CopyProxyToUdon(entity);
            }

            foreach (var airVehicle in rootObjects.SelectMany(o => o.GetComponentsInChildren<SaccAirVehicle>(true)))
            {
                if (sea)
                {
                    airVehicle.SeaLevel = sea.position.y;
                }

                airVehicle.RepeatingWorld = repeatingWorld;
                airVehicle.RepeatingWorldDistance = repeatingWorldDistance;

                UdonSharpEditorUtility.CopyProxyToUdon(airVehicle);
            }

            var windchanger = rootObjects.Select(o => o.GetComponentInChildren<SAV_WindChanger>()).Where(c => c).FirstOrDefault();
            foreach (var windsock in rootObjects.SelectMany(o => o.GetComponentsInChildren<Windsock>(true)))
            {
                windsock.windChanger = windchanger;
                UdonSharpEditorUtility.CopyProxyToUdon(windsock);
            }

            Destroy(this);
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

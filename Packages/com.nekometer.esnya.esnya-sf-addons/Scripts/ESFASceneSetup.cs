using EsnyaSFAddons.Accesory;
using UdonSharp;
using UnityEngine;
using SaccFlightAndVehicles;

namespace EsnyaSFAddons
{
    [DefaultExecutionOrder(-20)]
    public partial class ESFASceneSetup : MonoBehaviour
    {
        [Header("World Configuration")]
        public float seaLevel = -10;
        public bool repeatingWorld = true;
        public float repeatingWorldDistance = 20000;

        [Header("Inject Extentions")]
        public UdonSharpBehaviour[] injectExtentions = { };
    }
}

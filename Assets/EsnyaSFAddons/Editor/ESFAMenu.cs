using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EsnyaSFAddons
{
    public class ESFAMenu
    {
        private static readonly BuildTargetGroup[] buildTargetGroups = {
            BuildTargetGroup.Standalone,
            BuildTargetGroup.Android,
        };
        private static void AddDefinition(string symbol)
        {
            foreach (var buildTargetGroup in buildTargetGroups)
            {
                var syms = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, $"{symbol};{syms}");
            }
        }

        private static void RemoveDefinition(string symbol)
        {
            foreach (var buildTargetGroup in buildTargetGroups)
            {
                var syms = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, syms.Replace(symbol, "").Replace(";;", ";"));
            }
        }

        private static void SetLayerName(int layer, string name)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset"));
            tagManager.Update();

            var layersProperty = tagManager.FindProperty("layers");
            layersProperty.arraySize = Mathf.Max(layersProperty.arraySize, layer);
            layersProperty.GetArrayElementAtIndex(layer).stringValue = name;

            tagManager.ApplyModifiedProperties();
        }

        public static readonly int[] addonLayers = {
            29,
        };
        public static readonly string[] addonLayerNames = {
            "BoardingCollider"
        };
        public static readonly LayerMask[] addonLayerCollisions = {
            0b0101_1111_1111_1101_1010_1111_1101_1111,
        };

        [MenuItem("SaccFlight/EsnyaSFAddons/Setup Addon Layers")]
        public static void SetupAddonLayers()
        {
            var zippedAddonLayers = addonLayers
                .Zip(addonLayerNames, (layer, name) => (layer, name))
                .Zip(addonLayerCollisions, (t, collision) => (t.layer, t.name, collision));
            foreach (var (layer, name, collision) in zippedAddonLayers)
            {
                SetLayerName(layer, name);
                for (var i = 0; i < 32; i++)
                {
                    Physics.IgnoreLayerCollision(layer, i, ((1 << i) & collision) == 0);
                }
            }
        }

#if !ESFA_UCS
        [MenuItem("SaccFlight/EsnyaSFAddons/Install UdonChips")]
        public static void EnableUCS()
        {
            AddDefinition("ESFA_UCS");
        }
#else
        [MenuItem("SaccFlight/EsnyaSFAddons/Uninstall UdonChips")]
        public static void EnableUCS()
        {
            RemoveDefinition("ESFA_UCS");
        }
#endif
    }
}

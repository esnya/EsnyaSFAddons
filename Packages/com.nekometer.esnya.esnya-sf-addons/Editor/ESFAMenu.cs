using System.Linq;
using System.Reflection.Emit;
using SaccFlightAndVehicles;
using UnityEditor;
using UnityEngine;

namespace EsnyaSFAddons.Editor
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

        private static SerializedObject GetTagManager()
        {
            return new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        }

        private static void SetLayerName(int layer, string name)
        {
            var tagManager = GetTagManager();
            tagManager.Update();

            var layersProperty = tagManager.FindProperty("layers");
            layersProperty.arraySize = Mathf.Max(layersProperty.arraySize, layer);
            layersProperty.GetArrayElementAtIndex(layer).stringValue = name;

            tagManager.ApplyModifiedProperties();
        }

        private static bool IsTagExisits(string tag)
        {
            var tagsProperty = GetTagManager().FindProperty("tags");
            return Enumerable.Range(0, tagsProperty.arraySize).Select(i => tagsProperty.GetArrayElementAtIndex(i).stringValue).Contains(tag);
        }

        private static void AddTag(string tag)
        {
            var tagManager = GetTagManager();
            var tagsProperty = tagManager.FindProperty("tags");
            tagsProperty.arraySize++;
            tagsProperty.GetArrayElementAtIndex(tagsProperty.arraySize - 1).stringValue = tag;
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

        public static readonly string[] addonTags = {
            "SFEXT",
            "SFEXTP",
            "DFUNC",
            "DFUNCP",
            "MFD",
            "LStickDisplay",
            "RStickDisplay",
            "DialFuncOn",
            nameof(SaccEntity.EnableInVehicle),
            nameof(SaccEntity.SwitchFunctionSound),
            nameof(SaccEntity.CenterOfMass),
            nameof(SaccEntity.LStickDisplayHighlighter),
            nameof(SaccEntity.RStickDisplayHighlighter),
        };

        [MenuItem("SaccFlight/EsnyaSFAddons/Setup Layers and Tags")]
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

            foreach (var tag in addonTags)
            {
                if (IsTagExisits(tag)) continue;
                AddTag(tag);
            }
        }
    }
}

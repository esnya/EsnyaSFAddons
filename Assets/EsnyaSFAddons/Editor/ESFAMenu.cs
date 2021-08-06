using UnityEditor;

namespace EsnyaAircraftAssets
{
    public class ESFAMenu
    {
        private static void AddDefinition(string symbol)
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var syms = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, $"{symbol};{syms}");
        }

        private static void RemoveDefinition(string symbol)
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var syms = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, syms.Replace(symbol, "").Replace(";;", ";"));
        }

#if !ESFA
        [MenuItem("EsnyaSFAddons/Install")]
        public static void EnableESFA()
        {
            AddDefinition("ESFA");
        }
#else
        [MenuItem("EsnyaSFAddons/Features/Uninstall All")]
        public static void DisableESFA()
        {
            RemoveDefinition("ESFA");
        }
#endif

#if ESFA && !ESFA_UCS
        [MenuItem("EsnyaSFAddons/Features/Install UdonChips")]
        public static void EnableUCS()
        {
            AddDefinition("ESFA_UCS");
        }
#elif ESFA
        [MenuItem("EsnyaSFAddons/Features/Uninstall UdonChips")]
        public static void EnableUCS()
        {
            RemoveDefinition("ESFA_UCS");
        }
#endif
    }
}

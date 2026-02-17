using HexWords.Core;
using UnityEditor;
using UnityEngine;

namespace HexWords.EditorTools
{
    public static class PreviewPlayContext
    {
        private const string ConfigFolder = "Assets/Resources";
        private const string ConfigAssetPath = "Assets/Resources/HexWordsPreviewConfig.asset";

        public static void SetLevel(LevelDefinition level)
        {
            var config = GetOrCreateConfig();
            config.previewLevel = level;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        private static RuntimePreviewConfig GetOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<RuntimePreviewConfig>(ConfigAssetPath);
            if (config != null)
            {
                return config;
            }

            if (!AssetDatabase.IsValidFolder(ConfigFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            config = ScriptableObject.CreateInstance<RuntimePreviewConfig>();
            AssetDatabase.CreateAsset(config, ConfigAssetPath);
            return config;
        }
    }
}

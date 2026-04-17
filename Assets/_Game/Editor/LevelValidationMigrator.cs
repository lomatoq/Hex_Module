using System.Linq;
using HexWords.Core;
using UnityEditor;
using UnityEngine;

namespace HexWords.EditorTools
{
    /// <summary>
    /// One-shot migration for existing <see cref="LevelDefinition"/> assets:
    /// switches them to <see cref="ValidationMode.Dictionary"/> and clears
    /// <c>bonusRequiresEmbeddedInLevelOnly</c> so any real word on the board
    /// scores as a bonus (e.g. "GAY" on a level whose targets are different
    /// words).
    ///
    /// Also tweaks <see cref="GenerationProfile"/> assets so future batches
    /// inherit the same behavior.
    /// </summary>
    public static class LevelValidationMigrator
    {
        [MenuItem("HexWords/Level/Migrate Existing Levels → Dictionary Mode")]
        public static void Migrate()
        {
            if (!EditorUtility.DisplayDialog(
                "Migrate levels to Dictionary mode?",
                "All LevelDefinition assets in the project will be set to:\n" +
                "  • validationMode = Dictionary\n" +
                "  • bonusRequiresEmbeddedInLevelOnly = false\n\n" +
                "This lets the player score bonus points for any real dictionary word " +
                "on the board, not just substrings of the target list.\n\n" +
                "GenerationProfile assets will be updated to the same defaults so future " +
                "batches stay consistent.\n\nProceed?",
                "Migrate", "Cancel"))
            {
                return;
            }

            var levelsChanged = MigrateLevels();
            var profilesChanged = MigrateProfiles();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Migration complete",
                $"Levels updated: {levelsChanged}\nGeneration profiles updated: {profilesChanged}",
                "OK");
        }

        private static int MigrateLevels()
        {
            var guids = AssetDatabase.FindAssets("t:LevelDefinition");
            var changed = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                if (level == null) continue;

                var touched = false;
                if (level.validationMode != ValidationMode.Dictionary)
                {
                    level.validationMode = ValidationMode.Dictionary;
                    touched = true;
                }
                if (level.bonusRequiresEmbeddedInLevelOnly)
                {
                    level.bonusRequiresEmbeddedInLevelOnly = false;
                    touched = true;
                }
                if (!level.allowBonusWords)
                {
                    level.allowBonusWords = true;
                    touched = true;
                }

                if (touched)
                {
                    EditorUtility.SetDirty(level);
                    changed++;
                }
            }
            Debug.Log($"[LevelValidationMigrator] Updated {changed}/{guids.Length} LevelDefinition assets.");
            return changed;
        }

        private static int MigrateProfiles()
        {
            var guids = AssetDatabase.FindAssets("t:GenerationProfile");
            var changed = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<GenerationProfile>(path);
                if (profile == null) continue;

                var touched = false;
                if (profile.bonusRequiresEmbeddedInLevelOnly)
                {
                    profile.bonusRequiresEmbeddedInLevelOnly = false;
                    touched = true;
                }
                if (!profile.allowBonusWords)
                {
                    profile.allowBonusWords = true;
                    touched = true;
                }

                if (touched)
                {
                    EditorUtility.SetDirty(profile);
                    changed++;
                }
            }
            Debug.Log($"[LevelValidationMigrator] Updated {changed}/{guids.Length} GenerationProfile assets.");
            return changed;
        }
    }
}

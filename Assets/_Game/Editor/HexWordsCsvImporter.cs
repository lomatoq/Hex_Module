using System;
using System.Collections.Generic;
using System.IO;
using HexWords.Core;
using UnityEditor;
using UnityEngine;

namespace HexWords.EditorTools
{
    public static class HexWordsCsvImporter
    {
        private const string SourceRoot = "Assets/_Game/Data/Source";
        private const string DictionaryOutRoot = "Assets/_Game/Data/Generated/Dictionaries";
        private const string LevelsOutRoot = "Assets/_Game/Data/Generated/Levels";

        [MenuItem("Tools/HexWords/Import CSV")]
        public static void ImportAll()
        {
            ImportDictionaries();
            ImportLevels();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("HexWords CSV import complete.");
        }

        private static void ImportDictionaries()
        {
            EnsureFolder(DictionaryOutRoot);
            ImportDictionaryFile(Path.Combine(SourceRoot, "dictionary_ru.csv"), Language.RU, "DictionaryRU.asset");
            ImportDictionaryFile(Path.Combine(SourceRoot, "dictionary_en.csv"), Language.EN, "DictionaryEN.asset");
        }

        private static void ImportDictionaryFile(string path, Language language, string outFile)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"Dictionary CSV not found: {path}");
                return;
            }

            var rows = CsvUtility.Parse(File.ReadAllText(path));
            if (rows.Count <= 1)
            {
                Debug.LogWarning($"Dictionary CSV is empty: {path}");
                return;
            }

            var idx = CsvUtility.HeaderIndex(rows[0]);
            var seen = new HashSet<string>();
            var entries = new List<DictionaryEntry>();

            for (var i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                var word = WordNormalizer.Normalize(CsvUtility.Get(row, idx, "word"));
                if (string.IsNullOrEmpty(word) || !WordNormalizer.IsAsciiOrCyrillicLetterString(word))
                {
                    continue;
                }

                if (!seen.Add(word))
                {
                    Debug.LogWarning($"Duplicate word skipped: {word}");
                    continue;
                }

                entries.Add(new DictionaryEntry
                {
                    word = word,
                    language = language,
                    category = CsvUtility.Get(row, idx, "category"),
                    minLevel = ParseInt(CsvUtility.Get(row, idx, "minLevel"), 1),
                    maxLevel = ParseInt(CsvUtility.Get(row, idx, "maxLevel"), 999),
                    difficultyBand = ParseInt(CsvUtility.Get(row, idx, "difficultyBand"), 0)
                });
            }

            var outPath = $"{DictionaryOutRoot}/{outFile}";
            var asset = AssetDatabase.LoadAssetAtPath<DictionaryDatabase>(outPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<DictionaryDatabase>();
                AssetDatabase.CreateAsset(asset, outPath);
            }

            asset.entries = entries;
            EditorUtility.SetDirty(asset);
        }

        private static void ImportLevels()
        {
            EnsureFolder(LevelsOutRoot);
            var levelsPath = Path.Combine(SourceRoot, "levels.csv");
            var cellsPath = Path.Combine(SourceRoot, "level_cells.csv");
            if (!File.Exists(levelsPath) || !File.Exists(cellsPath))
            {
                Debug.LogWarning("Level CSV source files are missing.");
                return;
            }

            var levelRows = CsvUtility.Parse(File.ReadAllText(levelsPath));
            var cellRows = CsvUtility.Parse(File.ReadAllText(cellsPath));
            if (levelRows.Count <= 1 || cellRows.Count <= 1)
            {
                Debug.LogWarning("Level CSV source files do not contain data.");
                return;
            }

            var levelIdx = CsvUtility.HeaderIndex(levelRows[0]);
            var cellIdx = CsvUtility.HeaderIndex(cellRows[0]);

            var cellsByLevel = new Dictionary<string, List<CellDefinition>>();
            for (var i = 1; i < cellRows.Count; i++)
            {
                var row = cellRows[i];
                var levelId = CsvUtility.Get(row, cellIdx, "levelId");
                if (string.IsNullOrWhiteSpace(levelId))
                {
                    continue;
                }

                if (!cellsByLevel.TryGetValue(levelId, out var list))
                {
                    list = new List<CellDefinition>();
                    cellsByLevel[levelId] = list;
                }

                list.Add(new CellDefinition
                {
                    cellId = CsvUtility.Get(row, cellIdx, "cellId"),
                    letter = WordNormalizer.Normalize(CsvUtility.Get(row, cellIdx, "letter")),
                    q = ParseInt(CsvUtility.Get(row, cellIdx, "q"), 0),
                    r = ParseInt(CsvUtility.Get(row, cellIdx, "r"), 0)
                });
            }

            for (var i = 1; i < levelRows.Count; i++)
            {
                var row = levelRows[i];
                var levelId = CsvUtility.Get(row, levelIdx, "levelId");
                if (string.IsNullOrWhiteSpace(levelId))
                {
                    continue;
                }

                if (!cellsByLevel.TryGetValue(levelId, out var cells) || cells.Count == 0)
                {
                    Debug.LogWarning($"No cells for level: {levelId}");
                    continue;
                }

                var wordsRaw = CsvUtility.Get(row, levelIdx, "targetWords");
                var targetWords = SplitPipe(wordsRaw);
                var level = CreateOrLoadLevel(levelId);
                level.levelId = levelId;
                level.targetScore = ParseInt(CsvUtility.Get(row, levelIdx, "targetScore"), 10);
                level.validationMode = ParseValidationMode(CsvUtility.Get(row, levelIdx, "validationMode"));
                level.language = ParseLanguage(CsvUtility.Get(row, levelIdx, "language"));
                level.targetWords = targetWords;
                level.shape.cells = cells;

                var valid = true;
                for (var w = 0; w < level.targetWords.Length; w++)
                {
                    if (!LevelPathValidator.CanBuildWord(level, level.targetWords[w]))
                    {
                        valid = false;
                        Debug.LogWarning($"Word '{level.targetWords[w]}' is not buildable in level {level.levelId}");
                    }
                }

                if (!valid)
                {
                    Debug.LogWarning($"Level {level.levelId} has unreachable target words.");
                }

                EditorUtility.SetDirty(level);
            }
        }

        private static LevelDefinition CreateOrLoadLevel(string levelId)
        {
            var outPath = $"{LevelsOutRoot}/{levelId}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<LevelDefinition>(outPath);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<LevelDefinition>();
            AssetDatabase.CreateAsset(asset, outPath);
            return asset;
        }

        private static ValidationMode ParseValidationMode(string raw)
        {
            if (Enum.TryParse(raw, true, out ValidationMode mode))
            {
                return mode;
            }

            return ValidationMode.LevelOnly;
        }

        private static Language ParseLanguage(string raw)
        {
            if (Enum.TryParse(raw, true, out Language language))
            {
                return language;
            }

            return Language.EN;
        }

        private static int ParseInt(string raw, int fallback)
        {
            return int.TryParse(raw, out var value) ? value : fallback;
        }

        private static string[] SplitPipe(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return Array.Empty<string>();
            }

            var parts = raw.Split('|');
            for (var i = 0; i < parts.Length; i++)
            {
                parts[i] = WordNormalizer.Normalize(parts[i]);
            }

            return parts;
        }

        private static void EnsureFolder(string path)
        {
            var split = path.Split('/');
            var current = split[0];
            for (var i = 1; i < split.Length; i++)
            {
                var next = $"{current}/{split[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, split[i]);
                }

                current = next;
            }
        }
    }
}

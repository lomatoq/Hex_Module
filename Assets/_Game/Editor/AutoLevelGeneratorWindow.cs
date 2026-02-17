using System;
using System.Collections.Generic;
using System.Linq;
using HexWords.Core;
using UnityEditor;
using UnityEngine;

namespace HexWords.EditorTools
{
    public class AutoLevelGeneratorWindow : EditorWindow
    {
        private static readonly (int dq, int dr)[] Directions =
        {
            (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1)
        };

        private DictionaryDatabase _dictionary;
        private GenerationProfile _profile;
        private string _outputFolder = "Assets/_Game/Data/Generated/Levels";
        private int _startLevelId = 1001;
        private int _levelsCount = 20;
        private ValidationMode _validationMode = ValidationMode.LevelOnly;
        private int _targetWordsPerLevel = 3;
        private int _maxWordReuseAcrossLevels = 1;
        private bool _overwriteExisting;
        private bool _shuffleCandidates = true;
        private bool _preferNaturalEnglishWords = true;

        [MenuItem("Tools/HexWords/Generate Levels (Auto)")]
        public static void Open()
        {
            GetWindow<AutoLevelGeneratorWindow>("Auto Level Generator");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Inputs", EditorStyles.boldLabel);
            _dictionary = (DictionaryDatabase)EditorGUILayout.ObjectField("Dictionary", _dictionary, typeof(DictionaryDatabase), false);
            _profile = (GenerationProfile)EditorGUILayout.ObjectField("Generation Profile", _profile, typeof(GenerationProfile), false);
            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);
            _startLevelId = EditorGUILayout.IntField("Start Level ID", _startLevelId);
            _levelsCount = Mathf.Clamp(EditorGUILayout.IntField("Levels Count", _levelsCount), 1, 500);
            _validationMode = (ValidationMode)EditorGUILayout.EnumPopup("Validation Mode", _validationMode);
            _targetWordsPerLevel = Mathf.Clamp(EditorGUILayout.IntField("Target Words Per Level", _targetWordsPerLevel), 1, 6);
            _maxWordReuseAcrossLevels = Mathf.Clamp(EditorGUILayout.IntField("Max Word Reuse (global)", _maxWordReuseAcrossLevels), 1, 50);
            _overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing Assets", _overwriteExisting);
            _shuffleCandidates = EditorGUILayout.Toggle("Shuffle Candidates", _shuffleCandidates);
            _preferNaturalEnglishWords = EditorGUILayout.Toggle("Prefer Natural EN Words", _preferNaturalEnglishWords);

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate Levels"))
            {
                Generate();
            }
        }

        private void Generate()
        {
            if (_dictionary == null || _profile == null)
            {
                Debug.LogWarning("Set Dictionary and Generation Profile.");
                return;
            }

            EnsureFolder(_outputFolder);

            var candidates = LevelGenerator.FilterWords(_dictionary, _profile)
                .Select(e => WordNormalizer.Normalize(e.word))
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Where(w => w.Length >= 2 && w.Length <= _profile.cellCount)
                .Where(w => !_preferNaturalEnglishWords || _profile.language != Language.EN || LooksNaturalEnglishWord(w))
                .Where(w => !_profile.avoidDuplicateLetters || IsUniqueLetters(w))
                .Distinct()
                .ToList();

            if (candidates.Count == 0)
            {
                Debug.LogWarning("No candidate words matched profile. Levels were not generated.");
                return;
            }

            if (_shuffleCandidates)
            {
                var rng = new System.Random(_startLevelId);
                for (var i = candidates.Count - 1; i > 0; i--)
                {
                    var j = rng.Next(i + 1);
                    (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
                }
            }

            var globalWordUse = new Dictionary<string, int>(StringComparer.Ordinal);
            var generated = 0;

            for (var levelOffset = 0; levelOffset < _levelsCount; levelOffset++)
            {
                var levelIdNumber = _startLevelId + levelOffset;
                var levelId = levelIdNumber.ToString();
                var assetPath = $"{_outputFolder}/{levelId}.asset";

                var existing = AssetDatabase.LoadAssetAtPath<LevelDefinition>(assetPath);
                if (existing != null && !_overwriteExisting)
                {
                    continue;
                }

                var startIndex = levelIdNumber % candidates.Count;
                var boardLetters = BuildBoardLetters(candidates, startIndex, _profile.cellCount, _profile.language, _profile.avoidDuplicateLetters);
                if (boardLetters.Count == 0)
                {
                    continue;
                }

                var boardText = new string(boardLetters.ToArray());
                var targetWords = PickBuildableTargetWords(candidates, boardText, _targetWordsPerLevel, globalWordUse, _maxWordReuseAcrossLevels);
                if (targetWords.Count == 0)
                {
                    continue;
                }

                var coords = BuildCompactPathCoords(_profile.cellCount, seed: levelIdNumber);
                var cells = new List<CellDefinition>(_profile.cellCount);
                for (var i = 0; i < _profile.cellCount; i++)
                {
                    cells.Add(new CellDefinition
                    {
                        cellId = $"c{i + 1}",
                        letter = boardLetters[i].ToString(),
                        q = coords[i].q,
                        r = coords[i].r
                    });
                }

                for (var i = 0; i < targetWords.Count; i++)
                {
                    globalWordUse.TryGetValue(targetWords[i], out var current);
                    globalWordUse[targetWords[i]] = current + 1;
                }

                var level = existing ?? ScriptableObject.CreateInstance<LevelDefinition>();
                level.levelId = levelId;
                level.language = _profile.language;
                level.validationMode = _validationMode;
                level.targetWords = targetWords.ToArray();
                level.targetScore = targetWords.Sum(w => w.Length);
                level.shape = new GridShape { cells = cells };

                if (existing == null)
                {
                    AssetDatabase.CreateAsset(level, assetPath);
                }

                EditorUtility.SetDirty(level);
                generated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Auto generation complete. Generated levels: {generated}. Candidates pool: {candidates.Count}");
        }

        private static List<char> BuildBoardLetters(List<string> candidates, int startIndex, int cellCount, Language language, bool uniqueOnly)
        {
            var letters = new List<char>(cellCount);
            var used = new HashSet<char>();

            // Seed board with up to two words to increase chance of multiple target words.
            var seedsPlaced = 0;
            for (var i = 0; i < candidates.Count && seedsPlaced < 2; i++)
            {
                var word = candidates[(startIndex + i) % candidates.Count];
                if (word.Length > cellCount - letters.Count)
                {
                    continue;
                }

                if (uniqueOnly)
                {
                    var fits = true;
                    for (var c = 0; c < word.Length; c++)
                    {
                        if (used.Contains(word[c]))
                        {
                            fits = false;
                            break;
                        }
                    }

                    if (!fits)
                    {
                        continue;
                    }
                }

                for (var c = 0; c < word.Length; c++)
                {
                    var ch = word[c];
                    letters.Add(ch);
                    used.Add(ch);
                }

                seedsPlaced++;
            }

            if (letters.Count == 0)
            {
                var fallback = candidates[startIndex % candidates.Count];
                for (var c = 0; c < fallback.Length && letters.Count < cellCount; c++)
                {
                    var ch = fallback[c];
                    if (!uniqueOnly || used.Add(ch))
                    {
                        letters.Add(ch);
                    }
                }
            }

            var alphabet = language == Language.RU
                ? "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ"
                : "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            var rng = new System.Random(startIndex + cellCount);
            while (letters.Count < cellCount)
            {
                char next;
                if (uniqueOnly)
                {
                    next = alphabet.FirstOrDefault(ch => !used.Contains(ch));
                    if (next == default)
                    {
                        break;
                    }
                    used.Add(next);
                }
                else
                {
                    next = alphabet[rng.Next(alphabet.Length)];
                }

                letters.Add(next);
            }

            return letters;
        }

        private static List<string> PickBuildableTargetWords(
            List<string> candidates,
            string board,
            int targetCount,
            Dictionary<string, int> globalWordUse,
            int maxReuse)
        {
            var reverseBoard = new string(board.Reverse().ToArray());
            var buildable = candidates
                .Where(w => board.Contains(w, StringComparison.Ordinal) || reverseBoard.Contains(w, StringComparison.Ordinal))
                .Distinct()
                .OrderBy(w => globalWordUse.TryGetValue(w, out var used) ? used : 0)
                .ThenByDescending(w => w.Length)
                .ThenBy(w => w, StringComparer.Ordinal)
                .ToList();

            var selected = new List<string>();
            for (var i = 0; i < buildable.Count && selected.Count < targetCount; i++)
            {
                var word = buildable[i];
                globalWordUse.TryGetValue(word, out var used);
                if (used >= maxReuse)
                {
                    continue;
                }

                selected.Add(word);
            }

            if (selected.Count == 0 && buildable.Count > 0)
            {
                selected.Add(buildable[0]);
            }

            return selected;
        }

        private static List<(int q, int r)> BuildCompactPathCoords(int count, int seed)
        {
            var path = new List<(int q, int r)>(count) { (0, 0) };
            if (count == 1)
            {
                return path;
            }

            var visited = new HashSet<(int q, int r)> { (0, 0) };
            var rng = new System.Random(seed);

            TryExtendPath(path, visited, count, rng);
            return path;
        }

        private static bool TryExtendPath(List<(int q, int r)> path, HashSet<(int q, int r)> visited, int targetCount, System.Random rng)
        {
            if (path.Count >= targetCount)
            {
                return true;
            }

            var current = path[path.Count - 1];
            var candidates = new List<(int q, int r)>();
            for (var i = 0; i < Directions.Length; i++)
            {
                var next = (current.q + Directions[i].dq, current.r + Directions[i].dr);
                if (!visited.Contains(next))
                {
                    candidates.Add(next);
                }
            }

            // Keep path compact by preferring coordinates near center.
            candidates = candidates
                .OrderBy(c => HexDistance(c.q, c.r, 0, 0))
                .ThenBy(_ => rng.Next())
                .ToList();

            for (var i = 0; i < candidates.Count; i++)
            {
                var next = candidates[i];
                visited.Add(next);
                path.Add(next);

                if (TryExtendPath(path, visited, targetCount, rng))
                {
                    return true;
                }

                path.RemoveAt(path.Count - 1);
                visited.Remove(next);
            }

            return false;
        }

        private static int HexDistance(int q1, int r1, int q2, int r2)
        {
            var s1 = -q1 - r1;
            var s2 = -q2 - r2;
            return (Math.Abs(q1 - q2) + Math.Abs(r1 - r2) + Math.Abs(s1 - s2)) / 2;
        }

        private static bool IsUniqueLetters(string word)
        {
            var seen = new HashSet<char>();
            for (var i = 0; i < word.Length; i++)
            {
                if (!seen.Add(word[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool LooksNaturalEnglishWord(string word)
        {
            if (word.Length < 3)
            {
                return false;
            }

            var vowels = 0;
            var maxConsonantRun = 0;
            var currentConsonantRun = 0;
            var repeatedRun = 1;

            for (var i = 0; i < word.Length; i++)
            {
                var c = word[i];
                var isVowel = c is 'A' or 'E' or 'I' or 'O' or 'U' or 'Y';
                if (isVowel)
                {
                    vowels++;
                    currentConsonantRun = 0;
                }
                else
                {
                    currentConsonantRun++;
                    if (currentConsonantRun > maxConsonantRun)
                    {
                        maxConsonantRun = currentConsonantRun;
                    }
                }

                if (i > 0 && word[i - 1] == c)
                {
                    repeatedRun++;
                    if (repeatedRun >= 3)
                    {
                        return false;
                    }
                }
                else
                {
                    repeatedRun = 1;
                }
            }

            var vowelRatio = (float)vowels / word.Length;
            if (vowelRatio < 0.18f || vowelRatio > 0.75f)
            {
                return false;
            }

            if (maxConsonantRun >= 4)
            {
                return false;
            }

            return true;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}

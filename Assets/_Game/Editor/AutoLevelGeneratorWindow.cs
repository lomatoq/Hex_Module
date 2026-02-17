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
            _targetWordsPerLevel = Mathf.Clamp(EditorGUILayout.IntField("Target Words Per Level", _targetWordsPerLevel), 1, 5);
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
                .Distinct()
                .ToList();

            if (candidates.Count == 0)
            {
                Debug.LogWarning("No candidate words matched profile. Levels were not generated.");
                return;
            }

            // Long words first by default, then optional deterministic shuffle for variety.
            candidates = candidates.OrderByDescending(w => w.Length).ToList();
            if (_shuffleCandidates)
            {
                var rng = new System.Random(_startLevelId);
                for (var i = candidates.Count - 1; i > 0; i--)
                {
                    var j = rng.Next(i + 1);
                    (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
                }
            }

            var generated = 0;
            var wordUseCount = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < _levelsCount; i++)
            {
                var levelId = (_startLevelId + i).ToString();
                var assetPath = $"{_outputFolder}/{levelId}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<LevelDefinition>(assetPath);
                if (existing != null && !_overwriteExisting)
                {
                    continue;
                }

                var level = existing ?? ScriptableObject.CreateInstance<LevelDefinition>();
                var startIndex = (_startLevelId + i) % candidates.Count;
                var selectedWords = PickWordsForLevel(
                    candidates,
                    startIndex,
                    _targetWordsPerLevel,
                    _profile.cellCount,
                    wordUseCount,
                    _maxWordReuseAcrossLevels);
                if (selectedWords.Count == 0)
                {
                    break;
                }

                var seed = _startLevelId + i;
                level.levelId = levelId;
                level.language = _profile.language;
                level.validationMode = _validationMode;
                level.targetWords = selectedWords.ToArray();
                level.targetScore = selectedWords.Sum(w => w.Length);
                level.shape = new GridShape
                {
                    cells = BuildCellsFromWords(selectedWords, _profile, seed)
                };

                for (var w = 0; w < selectedWords.Count; w++)
                {
                    var word = selectedWords[w];
                    wordUseCount.TryGetValue(word, out var current);
                    wordUseCount[word] = current + 1;
                }

                if (existing == null)
                {
                    AssetDatabase.CreateAsset(level, assetPath);
                }

                EditorUtility.SetDirty(level);
                generated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Auto generation complete. Generated levels: {generated}. Candidates: {candidates.Count}. Output folder: {_outputFolder}");
        }

        private static List<string> PickWordsForLevel(
            List<string> candidates,
            int startIndex,
            int maxWords,
            int cellCount,
            Dictionary<string, int> wordUseCount,
            int maxReuse)
        {
            var selected = new List<string>();
            var totalLen = 0;
            var ordered = new List<string>(candidates.Count);
            for (var i = 0; i < candidates.Count; i++)
            {
                ordered.Add(candidates[(startIndex + i) % candidates.Count]);
            }

            // Prefer shorter words first to actually hit target word count per level.
            ordered = ordered.OrderBy(w => w.Length).ThenBy(w => w, StringComparer.Ordinal).ToList();

            for (var i = 0; i < ordered.Count && selected.Count < maxWords; i++)
            {
                var word = ordered[i];
                if (word.Length > cellCount)
                {
                    continue;
                }

                wordUseCount.TryGetValue(word, out var used);
                if (used >= maxReuse)
                {
                    continue;
                }

                if (totalLen + word.Length > cellCount)
                {
                    continue;
                }

                if (selected.Contains(word))
                {
                    continue;
                }

                selected.Add(word);
                totalLen += word.Length;
            }

            // If strict global reuse did not fill target count, relax it for this level.
            if (selected.Count < maxWords)
            {
                for (var i = 0; i < ordered.Count && selected.Count < maxWords; i++)
                {
                    var word = ordered[i];
                    if (word.Length > cellCount)
                    {
                        continue;
                    }

                    if (totalLen + word.Length > cellCount || selected.Contains(word))
                    {
                        continue;
                    }

                    selected.Add(word);
                    totalLen += word.Length;
                }
            }

            if (selected.Count == 0)
            {
                for (var i = 0; i < ordered.Count; i++)
                {
                    var fallback = ordered[i];
                    if (fallback.Length <= cellCount)
                    {
                        selected.Add(fallback);
                        break;
                    }
                }
            }

            return selected;
        }

        private static List<CellDefinition> BuildCellsFromWords(List<string> words, GenerationProfile profile, int seed)
        {
            var cells = new List<CellDefinition>(profile.cellCount);
            var combined = string.Concat(words).ToCharArray().ToList();
            var unique = new HashSet<char>(combined);
            var alphabet = profile.language == Language.RU
                ? "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ"
                : "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            var rng = new System.Random(seed);
            while (combined.Count < profile.cellCount)
            {
                char next;
                if (profile.avoidDuplicateLetters)
                {
                    next = alphabet.FirstOrDefault(c => !unique.Contains(c));
                    if (next == default)
                    {
                        next = alphabet[rng.Next(alphabet.Length)];
                    }
                    unique.Add(next);
                }
                else
                {
                    next = alphabet[rng.Next(alphabet.Length)];
                }

                combined.Add(next);
            }

            // Layout in a straight axial line: every next cell is a hex neighbor by (dq=1, dr=0).
            for (var i = 0; i < profile.cellCount; i++)
            {
                cells.Add(new CellDefinition
                {
                    cellId = $"c{i + 1}",
                    letter = combined[i].ToString(),
                    q = i,
                    r = 0
                });
            }

            return cells;
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

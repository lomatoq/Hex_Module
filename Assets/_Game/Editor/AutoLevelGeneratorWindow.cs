using System;
using System.Collections.Generic;
using System.IO;
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
        private string _catalogPath = "Assets/Resources/LevelCatalog.asset";
        private int _startLevelId = 1001;
        private int _levelsCount = 20;
        private ValidationMode _validationMode = ValidationMode.LevelOnly;
        private int _targetWordsPerLevel = 3;
        private int _maxWordReuseAcrossLevels = 1;
        private bool _overwriteExisting;
        private bool _shuffleCandidates;
        private bool _preferNaturalEnglishWords = true;
        private bool _preferPopularWords = true;
        private bool _strictPopularOnly = true;
        private int _popularPoolLimit = 500;
        private bool _useMinimalHexes = true;
        private int _minCells = 5;
        private int _extraBufferCells;
        private bool _strictTargetWordCount = true;

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
            _catalogPath = EditorGUILayout.TextField("Catalog Asset Path", _catalogPath);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);
            _startLevelId = EditorGUILayout.IntField("Start Level ID", _startLevelId);
            _levelsCount = Mathf.Clamp(EditorGUILayout.IntField("Levels Count", _levelsCount), 1, 500);
            _validationMode = (ValidationMode)EditorGUILayout.EnumPopup("Validation Mode", _validationMode);
            _targetWordsPerLevel = Mathf.Clamp(EditorGUILayout.IntField("Target Words Per Level", _targetWordsPerLevel), 1, 8);
            _maxWordReuseAcrossLevels = Mathf.Clamp(EditorGUILayout.IntField("Max Word Reuse (global)", _maxWordReuseAcrossLevels), 1, 100);
            _overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing Assets", _overwriteExisting);
            _shuffleCandidates = EditorGUILayout.Toggle("Shuffle Candidates", _shuffleCandidates);
            _preferNaturalEnglishWords = EditorGUILayout.Toggle("Prefer Natural EN Words", _preferNaturalEnglishWords);
            _preferPopularWords = EditorGUILayout.Toggle("Prefer Popular Words", _preferPopularWords);
            _strictPopularOnly = EditorGUILayout.Toggle("Strict Popular Only", _strictPopularOnly);
            _popularPoolLimit = Mathf.Clamp(EditorGUILayout.IntField("Popular Pool Limit", _popularPoolLimit), 50, 5000);
            _strictTargetWordCount = EditorGUILayout.Toggle("Strict Target Word Count", _strictTargetWordCount);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Board Size", EditorStyles.boldLabel);
            _useMinimalHexes = EditorGUILayout.Toggle("Use Minimal Hexes", _useMinimalHexes);
            _minCells = Mathf.Clamp(EditorGUILayout.IntField("Min Cells", _minCells), 3, _profile != null ? _profile.cellCount : 64);
            _extraBufferCells = Mathf.Clamp(EditorGUILayout.IntField("Extra Buffer Cells", _extraBufferCells), 0, 10);

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
            EnsureFolder(Path.GetDirectoryName(_catalogPath)?.Replace('\\', '/') ?? "Assets/_Game/Data/Generated");

            var popularityMap = _preferPopularWords && _profile.language == Language.EN
                ? LoadPopularityMap("Assets/_Game/Data/Source/frequency_en.txt")
                : new Dictionary<string, int>(StringComparer.Ordinal);

            var candidates = LevelGenerator.FilterWords(_dictionary, _profile)
                .Select(e => WordNormalizer.Normalize(e.word))
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Where(w => w.Length >= _profile.minLength && w.Length <= _profile.maxLength)
                .Where(w => !_preferNaturalEnglishWords || _profile.language != Language.EN || LooksNaturalEnglishWord(w))
                .Where(w => !_profile.avoidDuplicateLetters || IsUniqueLetters(w))
                .Distinct()
                .ToList();

            if (candidates.Count == 0)
            {
                Debug.LogWarning("No candidate words matched profile. Levels were not generated.");
                return;
            }

            if (_preferPopularWords && _profile.language == Language.EN && _strictPopularOnly && popularityMap.Count > 0)
            {
                var strict = candidates.Where(w => popularityMap.ContainsKey(w)).ToList();
                if (strict.Count >= 20)
                {
                    candidates = strict;
                }
                else
                {
                    Debug.LogWarning("Strict popular filter found too few words; fallback to weighted popular ranking.");
                }
            }

            candidates = candidates
                .OrderByDescending(w => ScoreWord(w, _profile.language, popularityMap, _preferPopularWords))
                .ThenByDescending(w => w.Length)
                .ThenBy(w => w, StringComparer.Ordinal)
                .ToList();

            if (_preferPopularWords && _profile.language == Language.EN)
            {
                var cap = Mathf.Min(_popularPoolLimit, candidates.Count);
                candidates = candidates.Take(cap).ToList();
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
                var targetWords = PickTargetWords(candidates, startIndex, _targetWordsPerLevel, globalWordUse, _maxWordReuseAcrossLevels);
                if (targetWords.Count == 0)
                {
                    continue;
                }

                var boardSequence = BuildBoardSequence(targetWords, _profile.avoidDuplicateLetters);
                if (string.IsNullOrWhiteSpace(boardSequence))
                {
                    continue;
                }

                var effectiveCellCount = ResolveCellCount(boardSequence, _profile.cellCount);
                var boardLetters = BuildBoardLetters(boardSequence, effectiveCellCount, _profile.language, _profile.avoidDuplicateLetters, levelIdNumber);
                if (boardLetters.Count < effectiveCellCount)
                {
                    continue;
                }

                var boardText = new string(boardLetters.ToArray());
                var validTargetWords = EnsureBuildableWords(targetWords, candidates, boardText, _targetWordsPerLevel);
                if (validTargetWords.Count == 0)
                {
                    continue;
                }

                if (_strictTargetWordCount && validTargetWords.Count < _targetWordsPerLevel)
                {
                    continue;
                }

                var coords = BuildCompactPathCoords(effectiveCellCount, levelIdNumber);
                var cells = new List<CellDefinition>(effectiveCellCount);
                for (var i = 0; i < effectiveCellCount; i++)
                {
                    cells.Add(new CellDefinition
                    {
                        cellId = $"c{i + 1}",
                        letter = boardLetters[i].ToString(),
                        q = coords[i].q,
                        r = coords[i].r
                    });
                }

                for (var i = 0; i < validTargetWords.Count; i++)
                {
                    globalWordUse.TryGetValue(validTargetWords[i], out var current);
                    globalWordUse[validTargetWords[i]] = current + 1;
                }

                var level = existing ?? ScriptableObject.CreateInstance<LevelDefinition>();
                level.levelId = levelId;
                level.language = _profile.language;
                level.validationMode = _validationMode;
                level.targetWords = validTargetWords.ToArray();
                level.targetScore = validTargetWords.Sum(w => w.Length);
                level.shape = new GridShape { cells = cells };

                if (existing == null)
                {
                    AssetDatabase.CreateAsset(level, assetPath);
                }

                EditorUtility.SetDirty(level);
                generated++;
            }

            BuildOrUpdateCatalog();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Auto generation complete. Generated levels: {generated}. Candidates pool: {candidates.Count}");
        }

        private int ResolveCellCount(string boardSequence, int maxCells)
        {
            if (!_useMinimalHexes)
            {
                return maxCells;
            }

            var required = boardSequence.Length + _extraBufferCells;
            return Mathf.Clamp(required, _minCells, maxCells);
        }

        private static List<string> PickTargetWords(
            List<string> candidates,
            int startIndex,
            int targetCount,
            Dictionary<string, int> globalWordUse,
            int maxReuse)
        {
            var ordered = new List<string>(candidates.Count);
            for (var i = 0; i < candidates.Count; i++)
            {
                ordered.Add(candidates[(startIndex + i) % candidates.Count]);
            }

            var picked = new List<string>();
            for (var i = 0; i < ordered.Count && picked.Count < targetCount; i++)
            {
                var word = ordered[i];
                globalWordUse.TryGetValue(word, out var used);
                if (used >= maxReuse)
                {
                    continue;
                }

                if (picked.Contains(word))
                {
                    continue;
                }

                picked.Add(word);
            }

            if (picked.Count == 0)
            {
                picked.Add(ordered[0]);
            }

            return picked;
        }

        private static string BuildBoardSequence(List<string> targetWords, bool uniqueOnly)
        {
            if (targetWords == null || targetWords.Count == 0)
            {
                return string.Empty;
            }

            var sequence = targetWords[0];
            for (var i = 1; i < targetWords.Count; i++)
            {
                sequence = MergeByOverlap(sequence, targetWords[i]);
            }

            if (!uniqueOnly)
            {
                return sequence;
            }

            var seen = new HashSet<char>();
            var chars = new List<char>(sequence.Length);
            for (var i = 0; i < sequence.Length; i++)
            {
                if (seen.Add(sequence[i]))
                {
                    chars.Add(sequence[i]);
                }
            }

            return new string(chars.ToArray());
        }

        private static string MergeByOverlap(string a, string b)
        {
            var maxOverlap = Math.Min(a.Length, b.Length);
            for (var overlap = maxOverlap; overlap > 0; overlap--)
            {
                var suffix = a.Substring(a.Length - overlap, overlap);
                var prefix = b.Substring(0, overlap);
                if (string.Equals(suffix, prefix, StringComparison.Ordinal))
                {
                    return a + b.Substring(overlap);
                }
            }

            return a + b;
        }

        private static List<char> BuildBoardLetters(string boardSequence, int cellCount, Language language, bool uniqueOnly, int seed)
        {
            var letters = new List<char>(cellCount);
            var used = new HashSet<char>();

            for (var i = 0; i < boardSequence.Length && letters.Count < cellCount; i++)
            {
                var ch = boardSequence[i];
                if (!uniqueOnly || used.Add(ch))
                {
                    letters.Add(ch);
                }
            }

            var alphabet = language == Language.RU
                ? "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ"
                : "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var common = language == Language.RU
                ? "ОЕАИНТСРВЛКМДПУЯЫЬГЗБЧЙХЖШЮЦЩЭФЪЁ"
                : "ETAOINSHRDLCUMWFGYPBVKJXQZ";

            var rng = new System.Random(seed);
            while (letters.Count < cellCount)
            {
                char next;
                if (uniqueOnly)
                {
                    next = common.FirstOrDefault(ch => !used.Contains(ch));
                    if (next == default)
                    {
                        next = alphabet.FirstOrDefault(ch => !used.Contains(ch));
                    }
                    if (next == default)
                    {
                        break;
                    }
                    used.Add(next);
                }
                else
                {
                    // Non-unique mode still prefers frequent letters over random noise.
                    var source = rng.NextDouble() < 0.85 ? common : alphabet;
                    next = source[rng.Next(source.Length)];
                }

                letters.Add(next);
            }

            return letters;
        }

        private static List<string> EnsureBuildableWords(List<string> selected, List<string> candidates, string boardText, int targetCount)
        {
            var result = new List<string>();
            for (var i = 0; i < selected.Count && result.Count < targetCount; i++)
            {
                if (IsBuildableOnPath(boardText, selected[i]))
                {
                    result.Add(selected[i]);
                }
            }

            if (result.Count >= targetCount)
            {
                return result;
            }

            for (var i = 0; i < candidates.Count && result.Count < targetCount; i++)
            {
                var word = candidates[i];
                if (result.Contains(word))
                {
                    continue;
                }

                if (IsBuildableOnPath(boardText, word))
                {
                    result.Add(word);
                }
            }

            return result;
        }

        private static bool IsBuildableOnPath(string boardText, string word)
        {
            if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(boardText))
            {
                return false;
            }

            if (boardText.IndexOf(word, StringComparison.Ordinal) >= 0)
            {
                return true;
            }

            var reverse = new string(boardText.Reverse().ToArray());
            return reverse.IndexOf(word, StringComparison.Ordinal) >= 0;
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
            }

            var vowelRatio = (float)vowels / word.Length;
            return vowelRatio is >= 0.18f and <= 0.75f && maxConsonantRun < 4;
        }

        private static double ScoreWord(string word, Language language, Dictionary<string, int> popularityMap, bool preferPopular)
        {
            var score = 0.0;

            if (preferPopular && popularityMap.TryGetValue(word, out var rank))
            {
                score += Math.Max(0, 200000 - rank);
            }

            if (language == Language.EN)
            {
                if (word.Length is >= 4 and <= 7)
                {
                    score += 120;
                }
                else if (word.Length == 3 || word.Length == 8)
                {
                    score += 50;
                }

                var rareLetters = 0;
                for (var i = 0; i < word.Length; i++)
                {
                    if (word[i] is 'J' or 'Q' or 'X' or 'Z')
                    {
                        rareLetters++;
                    }
                }

                score -= rareLetters * 20;
            }

            return score;
        }

        private static Dictionary<string, int> LoadPopularityMap(string projectPath)
        {
            var map = new Dictionary<string, int>(StringComparer.Ordinal);
            var absPath = Path.Combine(Directory.GetCurrentDirectory(), projectPath);
            if (!File.Exists(absPath))
            {
                return map;
            }

            var lines = File.ReadAllLines(absPath);
            var rank = 1;
            for (var i = 0; i < lines.Length; i++)
            {
                var word = lines[i].Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(word))
                {
                    continue;
                }

                if (!map.ContainsKey(word))
                {
                    map[word] = rank++;
                }
            }

            return map;
        }

        private void BuildOrUpdateCatalog()
        {
            var guids = AssetDatabase.FindAssets("t:LevelDefinition", new[] { _outputFolder });
            var levels = new List<LevelDefinition>();
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                if (level != null)
                {
                    levels.Add(level);
                }
            }

            levels = levels
                .OrderBy(l => int.TryParse(l.levelId, out var id) ? id : int.MaxValue)
                .ThenBy(l => l.levelId)
                .ToList();

            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(_catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<LevelCatalog>();
                AssetDatabase.CreateAsset(catalog, _catalogPath);
            }

            catalog.levels = levels;
            EditorUtility.SetDirty(catalog);
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var normalized = path.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            var parts = normalized.Split('/');
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

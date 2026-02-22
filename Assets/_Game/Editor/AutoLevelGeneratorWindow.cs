using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HexWords.Core;
using HexWords.EditorTools.GenerationV2;
using UnityEditor;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace HexWords.EditorTools
{
    public class AutoLevelGeneratorWindow : EditorWindow
    {
        private static readonly (int dq, int dr)[] Directions =
        {
            (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1)
        };

        private const string EnAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string RuAlphabet = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
        private const string EnCommon = "ETAOINSHRDLCUMWFGYPBVKJXQZ";
        private const string RuCommon = "ОЕАИНТСРВЛКМДПУЯЫЬГЗБЧЙХЖШЮЦЩЭФЪЁ";

        private DictionaryDatabase _dictionary;
        private GenerationProfile _profile;
        private string _outputFolder = "Assets/_Game/Data/Generated/Levels";
        private string _catalogPath = "Assets/Resources/LevelCatalog.asset";

        private int _startLevelId = 1001;
        private int _levelsCount = 20;
        private ValidationMode _validationMode = ValidationMode.LevelOnly;
        private int _maxWordReuseAcrossLevels = 3;
        private bool _overwriteExisting;

        private bool _preferNaturalEnglishWords = false;
        private bool _preferPopularWords = true;
        private bool _strictPopularOnly;
        private int _popularPoolLimit = 800;

        private bool _strictTargetWordCount;
        private int _attemptsPerLevel = 180;
        private int _candidateWindow = 600;
        private bool _useLegacyGenerator;
        private bool _showAdvanced;

        private bool _useMinimalHexes = true;
        private int _minCells = 6;

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
            EditorGUILayout.LabelField("Run", EditorStyles.boldLabel);
            _startLevelId = EditorGUILayout.IntField("Start Level ID", _startLevelId);
            _levelsCount = Mathf.Clamp(EditorGUILayout.IntField("Levels Count", _levelsCount), 1, 500);
            _validationMode = (ValidationMode)EditorGUILayout.EnumPopup("Validation Mode", _validationMode);
            _maxWordReuseAcrossLevels = Mathf.Clamp(EditorGUILayout.IntField("Max Word Reuse (global)", _maxWordReuseAcrossLevels), 1, 100);
            _strictTargetWordCount = EditorGUILayout.Toggle("Strict Target Word Count", _strictTargetWordCount);
            _overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing Assets", _overwriteExisting);

            _showAdvanced = EditorGUILayout.Toggle("Show Advanced Settings", _showAdvanced);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Solver", EditorStyles.boldLabel);
            _useLegacyGenerator = EditorGUILayout.Toggle("Use Legacy Generator (override)", _useLegacyGenerator);
            _attemptsPerLevel = Mathf.Clamp(EditorGUILayout.IntField("Attempts Per Level", _attemptsPerLevel), 20, 3000);

            var useLegacyForUi = _useLegacyGenerator || (_profile != null && _profile.generationAlgorithm == GenerationAlgorithm.Legacy);
            if (_showAdvanced && useLegacyForUi)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Legacy Candidate Quality", EditorStyles.boldLabel);
                _preferNaturalEnglishWords = EditorGUILayout.Toggle("Prefer Natural EN Words", _preferNaturalEnglishWords);
                _preferPopularWords = EditorGUILayout.Toggle("Prefer Popular Words", _preferPopularWords);
                _strictPopularOnly = EditorGUILayout.Toggle("Strict Popular Only", _strictPopularOnly);
                _popularPoolLimit = Mathf.Clamp(EditorGUILayout.IntField("Popular Pool Limit", _popularPoolLimit), 50, 5000);
                _candidateWindow = Mathf.Clamp(EditorGUILayout.IntField("Candidate Window", _candidateWindow), 50, 5000);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Board", EditorStyles.boldLabel);
            _useMinimalHexes = EditorGUILayout.Toggle("Use Minimal Hexes", _useMinimalHexes);
            _minCells = Mathf.Clamp(EditorGUILayout.IntField("Min Cells", _minCells), 3, _profile != null ? _profile.cellCount : 64);

            if (_profile != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Profile Constraints", EditorStyles.boldLabel);
                _profile.targetWordsMin = Mathf.Clamp(EditorGUILayout.IntField("Target Words Min", _profile.targetWordsMin), 1, 20);
                _profile.targetWordsMax = Mathf.Clamp(EditorGUILayout.IntField("Target Words Max", _profile.targetWordsMax), _profile.targetWordsMin, 40);
                _profile.minLength = Mathf.Clamp(EditorGUILayout.IntField("Word Length Min", _profile.minLength), 2, _profile.maxLength);
                _profile.maxLength = Mathf.Clamp(EditorGUILayout.IntField("Word Length Max", _profile.maxLength), _profile.minLength, 32);
                _profile.maxLetterRepeats = Mathf.Clamp(EditorGUILayout.IntField("Max Letter Repeats", _profile.maxLetterRepeats), 0, 8);
                _profile.allowSingleRepeatFallback = EditorGUILayout.Toggle("Allow +1 Repeat Fallback", _profile.allowSingleRepeatFallback);
                _profile.fillerLettersMax = Mathf.Clamp(EditorGUILayout.IntField("Filler Letters Max", _profile.fillerLettersMax), 0, 16);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Generation V2", EditorStyles.boldLabel);
                _profile.generationAlgorithm = (GenerationAlgorithm)EditorGUILayout.EnumPopup("Algorithm", _profile.generationAlgorithm);
                _profile.objective = (GenerationObjective)EditorGUILayout.EnumPopup("Objective", _profile.objective);
                _profile.hexBudgetMin = Mathf.Clamp(EditorGUILayout.IntField("Hex Budget Min", _profile.hexBudgetMin), 0, 128);
                _profile.hexBudgetMax = Mathf.Clamp(EditorGUILayout.IntField("Hex Budget Max", _profile.hexBudgetMax), 0, 128);
                _profile.maxResampleAttempts = Mathf.Clamp(EditorGUILayout.IntField("Max Resample Attempts", _profile.maxResampleAttempts), 1, 500);
                _profile.requireAllTargetsSolvable = EditorGUILayout.Toggle("Require All Targets Solvable", _profile.requireAllTargetsSolvable);
                _profile.useLegacyFallback = EditorGUILayout.Toggle("Use Legacy Fallback", _profile.useLegacyFallback);

                if (_showAdvanced)
                {
                    _profile.greedyRestarts = Mathf.Clamp(EditorGUILayout.IntField("Greedy Restarts", _profile.greedyRestarts), 1, 200);
                    _profile.beamWidth = Mathf.Clamp(EditorGUILayout.IntField("Beam Width", _profile.beamWidth), 2, 200);
                    _profile.overlapWeight = EditorGUILayout.Slider("Overlap Weight", _profile.overlapWeight, 0f, 250f);
                    _profile.diversityWeight = EditorGUILayout.Slider("Diversity Weight", _profile.diversityWeight, 0f, 250f);
                    _candidateWindow = Mathf.Clamp(EditorGUILayout.IntField("V2 Candidate Pool", _candidateWindow), 200, 4000);
                }
            }

            if (GUI.changed && _profile != null)
            {
                EditorUtility.SetDirty(_profile);
            }

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
            EnsureFolder(Path.GetDirectoryName(_catalogPath)?.Replace('\\', '/') ?? "Assets/Resources");

            var useLegacy = _useLegacyGenerator || _profile.generationAlgorithm == GenerationAlgorithm.Legacy;
            var popularityMap = _profile.language == Language.EN
                ? LoadPopularityMap("Assets/_Game/Data/Source/frequency_en.txt")
                : new Dictionary<string, int>(StringComparer.Ordinal);

            var candidates = BuildCandidates(popularityMap);
            if (candidates.Count == 0)
            {
                Debug.LogWarning(
                    $"No candidates after filtering. mode={(useLegacy ? "Legacy" : "V2")}, " +
                    $"language={_profile.language}, category='{_profile.category}', " +
                    $"length={_profile.minLength}-{_profile.maxLength}, difficulty={_profile.minDifficultyBand}-{_profile.maxDifficultyBand}.");
                return;
            }

            var globalWordUse = new Dictionary<string, int>(StringComparer.Ordinal);
            var generated = 0;
            var canceled = false;

            try
            {
                for (var levelOffset = 0; levelOffset < _levelsCount; levelOffset++)
                {
                    var levelIdNumber = _startLevelId + levelOffset;
                    var levelId = levelIdNumber.ToString();
                    var progress = (float)levelOffset / Math.Max(1, _levelsCount);
                    if (EditorUtility.DisplayCancelableProgressBar("HexWords Auto Generation", $"Building level {levelId} ({levelOffset + 1}/{_levelsCount})", progress))
                    {
                        canceled = true;
                        break;
                    }
                    var assetPath = $"{_outputFolder}/{levelId}.asset";

                    var existing = AssetDatabase.LoadAssetAtPath<LevelDefinition>(assetPath);
                    if (existing != null && !_overwriteExisting)
                    {
                        continue;
                    }

                    List<string> targetWords;
                    List<CellDefinition> cells = null;
                    List<char> boardLetters = null;
                    string v2FailureReason = null;

                    var built = useLegacy
                        ? TryBuildLevel(levelIdNumber, candidates, popularityMap, globalWordUse, out targetWords, out boardLetters)
                        : TryBuildLevelV2(levelIdNumber, candidates, globalWordUse, out targetWords, out cells, out v2FailureReason);

                    if (!built && !useLegacy && _profile.useLegacyFallback)
                    {
                        built = TryBuildLevel(levelIdNumber, candidates, popularityMap, globalWordUse, out targetWords, out boardLetters);
                        if (built)
                        {
                            Debug.Log($"Level {levelId}: V2 failed ({v2FailureReason}), fallback to legacy succeeded.");
                        }
                    }

                    if (!built)
                    {
                        if (!useLegacy && !string.IsNullOrWhiteSpace(v2FailureReason))
                        {
                            Debug.LogWarning($"Skipped level {levelId}: cannot satisfy constraints. {v2FailureReason}");
                        }
                        else
                        {
                            Debug.LogWarning($"Skipped level {levelId}: cannot satisfy constraints.");
                        }

                        continue;
                    }

                    if (cells == null)
                    {
                        var coords = BuildCompactPathCoords(boardLetters.Count, levelIdNumber);
                        cells = new List<CellDefinition>(boardLetters.Count);
                        for (var i = 0; i < boardLetters.Count; i++)
                        {
                            cells.Add(new CellDefinition
                            {
                                cellId = $"c{i + 1}",
                                letter = boardLetters[i].ToString(),
                                q = coords[i].q,
                                r = coords[i].r
                            });
                        }
                    }

                    for (var i = 0; i < targetWords.Count; i++)
                    {
                        globalWordUse.TryGetValue(targetWords[i], out var used);
                        globalWordUse[targetWords[i]] = used + 1;
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
                    Debug.Log($"Generated level {levelId}. Words={targetWords.Count}, Cells={cells.Count}, Mode={(useLegacy ? "Legacy" : "V2")}");
                    generated++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            BuildOrUpdateCatalog();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Generation complete. Generated={generated}, Candidates={candidates.Count}, WordsRange={_profile.targetWordsMin}-{_profile.targetWordsMax}, Canceled={canceled}");
        }

        private List<string> BuildCandidates(Dictionary<string, int> popularityMap)
        {
            var useLegacy = _useLegacyGenerator || _profile.generationAlgorithm == GenerationAlgorithm.Legacy;
            var enforceCommonEnOnly = _profile.language == Language.EN && popularityMap.Count > 0;
            var repeatCap = _profile.allowSingleRepeatFallback
                ? Mathf.Max(_profile.maxLetterRepeats, 1)
                : _profile.maxLetterRepeats;

            var filteredEntries = LevelGenerator.FilterWords(_dictionary, _profile);
            var appliedCategoryFallback = false;
            if (!useLegacy && filteredEntries.Count < Mathf.Max(30, _profile.targetWordsMin * 12) && !string.IsNullOrWhiteSpace(_profile.category))
            {
                var tempProfile = ScriptableObject.CreateInstance<GenerationProfile>();
                CopyProfile(_profile, tempProfile);
                tempProfile.category = string.Empty;
                filteredEntries = LevelGenerator.FilterWords(_dictionary, tempProfile);
                UnityEngine.Object.DestroyImmediate(tempProfile);
                appliedCategoryFallback = true;
            }

            var all = filteredEntries
                .Select(e => WordNormalizer.Normalize(e.word))
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Where(w => w.Length >= _profile.minLength && w.Length <= _profile.maxLength)
                .Where(w => useLegacy == false || !_preferNaturalEnglishWords || _profile.language != Language.EN || LooksNaturalEnglishWord(w))
                .Where(w => !useLegacy || CountRepeats(w) <= repeatCap)
                .Distinct()
                .ToList();

            if (enforceCommonEnOnly)
            {
                all = all
                    .Where(popularityMap.ContainsKey)
                    .OrderBy(w => popularityMap.TryGetValue(w, out var rank) ? rank : int.MaxValue)
                    .ThenBy(w => w.Length)
                    .ThenBy(w => w, StringComparer.Ordinal)
                    .ToList();
            }
            else
            {
                all = all
                    .OrderByDescending(w => ScoreWord(w, _profile.language, popularityMap, false))
                    .ThenBy(w => w, StringComparer.Ordinal)
                    .ToList();
            }

            if (appliedCategoryFallback)
            {
                Debug.Log($"Candidates: category fallback enabled for V2 (category='{_profile.category}') -> {all.Count} words. CommonENOnly={enforceCommonEnOnly}.");
            }
            else
            {
                Debug.Log($"Candidates: {all.Count} words (language={_profile.language}, category='{_profile.category}', mode={(useLegacy ? "Legacy" : "V2")}, CommonENOnly={enforceCommonEnOnly}).");
            }

            return all;
        }

        private bool TryBuildLevel(
            int seed,
            List<string> candidates,
            Dictionary<string, int> popularityMap,
            Dictionary<string, int> globalWordUse,
            out List<string> targetWords,
            out List<char> boardLetters)
        {
            targetWords = null;
            boardLetters = null;

            var minTargets = Mathf.Clamp(_profile.targetWordsMin, 1, 40);
            var maxTargets = Mathf.Clamp(_profile.targetWordsMax, minTargets, 40);
            var repeatBudgets = BuildRepeatBudgets();

            for (var rb = 0; rb < repeatBudgets.Count; rb++)
            {
                var repeatBudget = repeatBudgets[rb];
                for (var desired = maxTargets; desired >= minTargets; desired--)
                {
                    if (TryBuildWithDesired(seed, desired, repeatBudget, candidates, popularityMap, globalWordUse, out targetWords, out boardLetters))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryBuildLevelV2(
            int seed,
            List<string> candidates,
            Dictionary<string, int> globalWordUse,
            out List<string> targetWords,
            out List<CellDefinition> cells,
            out string failureReason)
        {
            targetWords = null;
            cells = null;
            failureReason = string.Empty;

            var available = candidates
                .Where(c => CanUseWord(c, globalWordUse))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var minTargets = Mathf.Clamp(_profile.targetWordsMin, 1, 40);
            var maxTargets = Mathf.Clamp(_profile.targetWordsMax, minTargets, 40);
            if (available.Count < minTargets)
            {
                failureReason = $"V2 failed for level {seed}. Available={available.Count}, requiredMin={minTargets}.";
                return false;
            }

            var targetScoreGoal = Mathf.Max(minTargets * _profile.minLength, minTargets * 3);
            var resampleAttempts = Mathf.Max(1, _profile.maxResampleAttempts);
            var placementAttempts = Mathf.Clamp(_attemptsPerLevel / resampleAttempts, 1, 300);
            var minCells = _useMinimalHexes ? _minCells : _profile.cellCount;
            var selectionFails = 0;
            var placementFails = 0;
            var solvabilityFails = 0;
            var desiredCounts = GenerationPlanUtility.BuildDesiredWordCounts(minTargets, maxTargets, _strictTargetWordCount);
            var levelSolveTimer = Stopwatch.StartNew();
            const int levelSolveBudgetMs = 2500;

            for (var d = 0; d < desiredCounts.Count; d++)
            {
                if (levelSolveTimer.ElapsedMilliseconds >= levelSolveBudgetMs)
                {
                    break;
                }

                var desiredCount = desiredCounts[d];
                for (var attempt = 0; attempt < resampleAttempts; attempt++)
                {
                    if (levelSolveTimer.ElapsedMilliseconds >= levelSolveBudgetMs)
                    {
                        break;
                    }

                    var selectionOptions = new WordSetSelectionOptions
                    {
                        language = _profile.language,
                        objective = _profile.objective,
                        avoidDuplicateLetters = _profile.avoidDuplicateLetters,
                        minWords = _strictTargetWordCount ? desiredCount : minTargets,
                        maxWords = _strictTargetWordCount ? desiredCount : maxTargets,
                        hexBudgetMin = _profile.hexBudgetMin,
                        hexBudgetMax = _profile.hexBudgetMax > 0 ? _profile.hexBudgetMax : _profile.cellCount,
                        targetScore = targetScoreGoal,
                        greedyRestarts = Mathf.Max(1, _profile.greedyRestarts),
                        beamWidth = Mathf.Max(2, _profile.beamWidth),
                        overlapWeight = _profile.overlapWeight,
                        diversityWeight = _profile.diversityWeight,
                        seed = seed + attempt * 97 + desiredCount * 17,
                        candidatePoolLimit = Mathf.Clamp(_candidateWindow, 200, 4000),
                        maxSolverMilliseconds = 220,
                        beamInputLimit = 700,
                        beamExpansionLimit = 120000
                    };

                    if (!WordSetSelector.TrySelect(available, selectionOptions, out var selection))
                    {
                        selectionFails++;
                        continue;
                    }

                    if (_strictTargetWordCount && selection.words.Count != desiredCount)
                    {
                        selectionFails++;
                        continue;
                    }

                    var placementOptions = new BoardPlacementOptions
                    {
                        language = _profile.language,
                        minCells = minCells,
                        maxCells = _profile.cellCount,
                        fillerLettersMax = Mathf.Max(_profile.fillerLettersMax, Mathf.Max(0, minCells - selection.hexCount)),
                        avoidDuplicateLetters = _profile.avoidDuplicateLetters,
                        maxLetterRepeats = Mathf.Max(0, _profile.maxLetterRepeats),
                        hexBudgetMin = _profile.hexBudgetMin,
                        hexBudgetMax = _profile.hexBudgetMax,
                        attempts = placementAttempts,
                        seed = seed + attempt * 193 + desiredCount * 31,
                        requireAllTargetsSolvable = _profile.requireAllTargetsSolvable
                    };

                    if (!BoardPlacer.TryPlace(selection.words, placementOptions, out var placement))
                    {
                        placementFails++;
                        continue;
                    }

                    if (_profile.requireAllTargetsSolvable &&
                        !SolvabilityValidator.ValidateAll(placement.cells, selection.words, out var failed))
                    {
                        solvabilityFails++;
                        Debug.Log($"Level {seed}: resample due to unsolved targets [{string.Join(", ", failed)}]");
                        continue;
                    }

                    targetWords = selection.words.ToList();
                    cells = placement.cells;
                    return true;
                }
            }

            failureReason =
                $"V2 failed for level {seed}. Available={available.Count}, " +
                $"WordsRange={minTargets}-{maxTargets}, ReuseLimit={_maxWordReuseAcrossLevels}, " +
                $"selectionFails={selectionFails}, placementFails={placementFails}, solvabilityFails={solvabilityFails}, " +
                $"Category='{_profile.category}', CellCount={_profile.cellCount}, ElapsedMs={levelSolveTimer.ElapsedMilliseconds}.";
            return false;
        }

        private bool TryBuildWithDesired(
            int seed,
            int desiredCount,
            int repeatBudget,
            List<string> candidates,
            Dictionary<string, int> popularityMap,
            Dictionary<string, int> globalWordUse,
            out List<string> targetWords,
            out List<char> boardLetters)
        {
            targetWords = null;
            boardLetters = null;
            var rng = new System.Random(seed + desiredCount + repeatBudget * 1000);

            for (var attempt = 0; attempt < _attemptsPerLevel; attempt++)
            {
                var start = (seed * 37 + attempt * 23) % candidates.Count;
                var selected = new List<string>();
                string merged = null;

                for (var offset = 0; offset < candidates.Count; offset++)
                {
                    var candidate = candidates[(start + offset) % candidates.Count];
                    if (!CanUseWord(candidate, globalWordUse))
                    {
                        continue;
                    }

                    if (CountRepeats(candidate) > repeatBudget)
                    {
                        continue;
                    }

                    merged = candidate;
                    selected.Add(candidate);
                    break;
                }

                if (selected.Count == 0)
                {
                    continue;
                }

                while (selected.Count < desiredCount)
                {
                    string bestWord = null;
                    string bestMerged = null;
                    var bestScore = double.MinValue;

                    var scan = Mathf.Min(_candidateWindow, candidates.Count);
                    for (var i = 0; i < scan; i++)
                    {
                        var word = candidates[(start + i + attempt) % candidates.Count];
                        if (selected.Contains(word) || !CanUseWord(word, globalWordUse))
                        {
                            continue;
                        }

                        if (!TryBestMerge(merged, word, out var mergedCandidate, out var overlap, out var addedLen))
                        {
                            continue;
                        }

                        if (mergedCandidate.Length > _profile.cellCount)
                        {
                            continue;
                        }

                        if (CountRepeats(mergedCandidate) > repeatBudget)
                        {
                            continue;
                        }

                        var score = ScoreWord(word, _profile.language, popularityMap, _preferPopularWords)
                                    + overlap * 90
                                    - addedLen * 14
                                    - (globalWordUse.TryGetValue(word, out var used) ? used * 60 : 0)
                                    + rng.NextDouble();

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestWord = word;
                            bestMerged = mergedCandidate;
                        }
                    }

                    if (bestWord == null)
                    {
                        break;
                    }

                    selected.Add(bestWord);
                    merged = bestMerged;
                }

                if (_strictTargetWordCount && selected.Count < desiredCount)
                {
                    continue;
                }

                var minAllowed = _strictTargetWordCount ? desiredCount : _profile.targetWordsMin;
                if (selected.Count < minAllowed)
                {
                    continue;
                }

                var cellCount = ResolveCellCount(merged.Length, _profile.cellCount, _profile.fillerLettersMax);
                if (cellCount < merged.Length)
                {
                    continue;
                }

                var letters = BuildBoardLettersFromMerged(merged, cellCount, _profile.language, repeatBudget, seed + attempt);
                if (letters.Count < cellCount)
                {
                    continue;
                }

                if (CountRepeats(letters) > repeatBudget)
                {
                    continue;
                }

                var board = new string(letters.ToArray());
                var validWords = selected.Where(w => IsBuildableOnPath(board, w)).ToList();
                if (_strictTargetWordCount && validWords.Count < desiredCount)
                {
                    continue;
                }

                if (validWords.Count < minAllowed)
                {
                    continue;
                }

                var take = _strictTargetWordCount ? desiredCount : Mathf.Clamp(validWords.Count, _profile.targetWordsMin, _profile.targetWordsMax);
                targetWords = validWords.Take(take).ToList();
                boardLetters = letters;
                return true;
            }

            return false;
        }

        private List<int> BuildRepeatBudgets()
        {
            var list = new List<int> { Mathf.Max(0, _profile.maxLetterRepeats) };
            if (_profile.allowSingleRepeatFallback && !list.Contains(1))
            {
                list.Add(1);
            }

            list.Sort();
            return list;
        }

        private bool CanUseWord(string word, Dictionary<string, int> globalWordUse)
        {
            globalWordUse.TryGetValue(word, out var used);
            return used < _maxWordReuseAcrossLevels;
        }

        private int ResolveCellCount(int mergedLength, int maxCells, int fillerMax)
        {
            if (!_useMinimalHexes)
            {
                return maxCells;
            }

            var required = mergedLength;
            if (fillerMax <= 0)
            {
                return Mathf.Clamp(required, 1, maxCells);
            }

            var target = Mathf.Max(required, _minCells);
            target = Mathf.Min(target, required + fillerMax);
            target = Mathf.Min(target, maxCells);
            if (target < required)
            {
                target = required;
            }

            return target;
        }

        private static bool TryBestMerge(string a, string b, out string merged, out int overlap, out int addedLength)
        {
            merged = a + b;
            overlap = 0;
            addedLength = b.Length;

            var best = merged;
            var bestOverlap = 0;
            var maxOverlap = Math.Min(a.Length, b.Length);

            for (var k = maxOverlap; k > 0; k--)
            {
                if (a.EndsWith(b.Substring(0, k), StringComparison.Ordinal))
                {
                    var candidate = a + b.Substring(k);
                    if (k > bestOverlap)
                    {
                        bestOverlap = k;
                        best = candidate;
                    }
                    break;
                }
            }

            for (var k = maxOverlap; k > 0; k--)
            {
                if (a.StartsWith(b.Substring(b.Length - k, k), StringComparison.Ordinal))
                {
                    var candidate = b + a.Substring(k);
                    if (k > bestOverlap)
                    {
                        bestOverlap = k;
                        best = candidate;
                    }
                    break;
                }
            }

            merged = best;
            overlap = bestOverlap;
            addedLength = merged.Length - a.Length;
            return true;
        }

        private static List<char> BuildBoardLettersFromMerged(string merged, int cellCount, Language language, int repeatBudget, int seed)
        {
            var letters = new List<char>(cellCount);
            var counts = new Dictionary<char, int>();

            for (var i = 0; i < merged.Length && letters.Count < cellCount; i++)
            {
                var ch = merged[i];
                AddChar(letters, counts, ch);
            }

            var alphabet = language == Language.RU ? RuAlphabet : EnAlphabet;
            var common = language == Language.RU ? RuCommon : EnCommon;
            var rng = new System.Random(seed);

            while (letters.Count < cellCount)
            {
                var source = rng.NextDouble() < 0.9 ? common : alphabet;
                var added = false;
                for (var i = 0; i < source.Length; i++)
                {
                    var ch = source[(i + rng.Next(source.Length)) % source.Length];
                    var nextRepeats = PredictRepeatsAfterAdd(counts, ch);
                    if (nextRepeats <= repeatBudget)
                    {
                        AddChar(letters, counts, ch);
                        added = true;
                        break;
                    }
                }

                if (!added)
                {
                    break;
                }
            }

            return letters;
        }

        private static void AddChar(List<char> letters, Dictionary<char, int> counts, char ch)
        {
            letters.Add(ch);
            counts.TryGetValue(ch, out var count);
            counts[ch] = count + 1;
        }

        private static int PredictRepeatsAfterAdd(Dictionary<char, int> counts, char ch)
        {
            var repeats = CountRepeats(counts);
            counts.TryGetValue(ch, out var count);
            if (count >= 1)
            {
                repeats += 1;
            }

            return repeats;
        }

        private static int CountRepeats(string word)
        {
            var counts = new Dictionary<char, int>();
            for (var i = 0; i < word.Length; i++)
            {
                var ch = word[i];
                counts.TryGetValue(ch, out var count);
                counts[ch] = count + 1;
            }

            return CountRepeats(counts);
        }

        private static int CountRepeats(List<char> letters)
        {
            var counts = new Dictionary<char, int>();
            for (var i = 0; i < letters.Count; i++)
            {
                var ch = letters[i];
                counts.TryGetValue(ch, out var count);
                counts[ch] = count + 1;
            }

            return CountRepeats(counts);
        }

        private static int CountRepeats(Dictionary<char, int> counts)
        {
            var repeats = 0;
            foreach (var pair in counts)
            {
                if (pair.Value > 1)
                {
                    repeats += pair.Value - 1;
                }
            }

            return repeats;
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
            var nexts = new List<(int q, int r)>();
            for (var i = 0; i < Directions.Length; i++)
            {
                var n = (current.q + Directions[i].dq, current.r + Directions[i].dr);
                if (!visited.Contains(n))
                {
                    nexts.Add(n);
                }
            }

            nexts = nexts
                .OrderBy(v => HexDistance(v.q, v.r, 0, 0))
                .ThenBy(_ => rng.Next())
                .ToList();

            for (var i = 0; i < nexts.Count; i++)
            {
                var next = nexts[i];
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

        private static bool IsBuildableOnPath(string boardText, string word)
        {
            if (string.IsNullOrWhiteSpace(boardText) || string.IsNullOrWhiteSpace(word))
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

        private static bool LooksNaturalEnglishWord(string word)
        {
            if (word.Length < 3)
            {
                return false;
            }

            var vowels = 0;
            var maxConsonantRun = 0;
            var consonantRun = 0;
            for (var i = 0; i < word.Length; i++)
            {
                var isVowel = word[i] is 'A' or 'E' or 'I' or 'O' or 'U' or 'Y';
                if (isVowel)
                {
                    vowels++;
                    consonantRun = 0;
                }
                else
                {
                    consonantRun++;
                    if (consonantRun > maxConsonantRun)
                    {
                        maxConsonantRun = consonantRun;
                    }
                }
            }

            var ratio = (float)vowels / word.Length;
            return ratio is >= 0.18f and <= 0.75f && maxConsonantRun < 4;
        }

        private static double ScoreWord(string word, Language language, Dictionary<string, int> popularityMap, bool preferPopular)
        {
            var score = 0.0;

            if (preferPopular && popularityMap.TryGetValue(word, out var rank))
            {
                score += Math.Max(0, 200000 - rank);
            }
            else if (preferPopular && language == Language.EN)
            {
                score -= 5000;
            }

            if (language == Language.EN)
            {
                if (word.Length is >= 3 and <= 7)
                {
                    score += 120;
                }

                var rare = 0;
                for (var i = 0; i < word.Length; i++)
                {
                    if (word[i] is 'J' or 'Q' or 'X' or 'Z')
                    {
                        rare++;
                    }
                }

                score -= rare * 25;
            }

            return score;
        }

        private static Dictionary<string, int> LoadPopularityMap(string projectPath)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            var abs = Path.Combine(Directory.GetCurrentDirectory(), projectPath);
            if (!File.Exists(abs))
            {
                return result;
            }

            var lines = File.ReadAllLines(abs);
            var rank = 1;
            for (var i = 0; i < lines.Length; i++)
            {
                var word = lines[i].Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(word))
                {
                    continue;
                }

                if (!result.ContainsKey(word))
                {
                    result[word] = rank++;
                }
            }

            return result;
        }

        private static void CopyProfile(GenerationProfile from, GenerationProfile to)
        {
            to.language = from.language;
            to.category = from.category;
            to.minLength = from.minLength;
            to.maxLength = from.maxLength;
            to.cellCount = from.cellCount;
            to.targetWordsMin = from.targetWordsMin;
            to.targetWordsMax = from.targetWordsMax;
            to.maxLetterRepeats = from.maxLetterRepeats;
            to.allowSingleRepeatFallback = from.allowSingleRepeatFallback;
            to.fillerLettersMax = from.fillerLettersMax;
            to.avoidDuplicateLetters = from.avoidDuplicateLetters;
            to.includeLetters = from.includeLetters;
            to.excludeLetters = from.excludeLetters;
            to.minDifficultyBand = from.minDifficultyBand;
            to.maxDifficultyBand = from.maxDifficultyBand;

            to.generationAlgorithm = from.generationAlgorithm;
            to.objective = from.objective;
            to.hexBudgetMin = from.hexBudgetMin;
            to.hexBudgetMax = from.hexBudgetMax;
            to.beamWidth = from.beamWidth;
            to.greedyRestarts = from.greedyRestarts;
            to.maxResampleAttempts = from.maxResampleAttempts;
            to.overlapWeight = from.overlapWeight;
            to.diversityWeight = from.diversityWeight;
            to.requireAllTargetsSolvable = from.requireAllTargetsSolvable;
            to.useLegacyFallback = from.useLegacyFallback;
        }

        private void BuildOrUpdateCatalog()
        {
            var guids = AssetDatabase.FindAssets("t:LevelDefinition", new[] { _outputFolder });
            var levels = new List<LevelDefinition>(guids.Length);
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

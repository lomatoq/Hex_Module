using System.Collections.Generic;
using HexWords.Core;
using UnityEditor;
using UnityEngine;

namespace HexWords.EditorTools
{
    public class HexWordsLevelEditorWindow : EditorWindow
    {
        private LevelDefinition _level;
        private DictionaryDatabase _dictionary;
        private GenerationProfile _generationProfile;
        private Vector2 _scroll;

        [MenuItem("Tools/HexWords/Level Editor")]
        public static void Open()
        {
            GetWindow<HexWordsLevelEditorWindow>("HexWords Level Editor");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            _level = (LevelDefinition)EditorGUILayout.ObjectField("Level", _level, typeof(LevelDefinition), false);
            _dictionary = (DictionaryDatabase)EditorGUILayout.ObjectField("Dictionary", _dictionary, typeof(DictionaryDatabase), false);
            _generationProfile = (GenerationProfile)EditorGUILayout.ObjectField("Generation Profile", _generationProfile, typeof(GenerationProfile), false);

            if (_level == null)
            {
                if (GUILayout.Button("Create New Level Asset"))
                {
                    CreateLevelAsset();
                }

                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawLevelFields();
            DrawCells();
            DrawTargetWords();
            DrawActions();
            EditorGUILayout.EndScrollView();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_level);
            }
        }

        private void DrawLevelFields()
        {
            _level.levelId = EditorGUILayout.TextField("Level ID", _level.levelId);
            _level.language = (Language)EditorGUILayout.EnumPopup("Language", _level.language);
            _level.validationMode = (ValidationMode)EditorGUILayout.EnumPopup("Validation Mode", _level.validationMode);
            _level.boardLayoutMode = (BoardLayoutMode)EditorGUILayout.EnumPopup("Board Layout", _level.boardLayoutMode);
            _level.targetScore = EditorGUILayout.IntField("Target Score", _level.targetScore);
            _level.minTargetWordsToComplete = Mathf.Max(0, EditorGUILayout.IntField("Min Targets To Complete", _level.minTargetWordsToComplete));
            _level.allowBonusWords = EditorGUILayout.Toggle("Allow Bonus Words", _level.allowBonusWords);
            _level.allowBonusInLevelOnly = EditorGUILayout.Toggle("Allow Bonus In LevelOnly", _level.allowBonusInLevelOnly);
            _level.bonusRequiresEmbeddedInLevelOnly = EditorGUILayout.Toggle("Bonus Embedded In LevelOnly", _level.bonusRequiresEmbeddedInLevelOnly);
        }

        private void DrawCells()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Cells", EditorStyles.boldLabel);

            if (_level.shape.cells == null)
            {
                _level.shape.cells = new List<CellDefinition>();
            }

            for (var i = 0; i < _level.shape.cells.Count; i++)
            {
                var cell = _level.shape.cells[i];
                EditorGUILayout.BeginHorizontal();
                cell.cellId = EditorGUILayout.TextField(cell.cellId, GUILayout.Width(120));
                cell.letter = EditorGUILayout.TextField(cell.letter, GUILayout.Width(40));
                cell.q = EditorGUILayout.IntField(cell.q, GUILayout.Width(50));
                cell.r = EditorGUILayout.IntField(cell.r, GUILayout.Width(50));
                if (GUILayout.Button("X", GUILayout.Width(24)))
                {
                    _level.shape.cells.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }

                EditorGUILayout.EndHorizontal();
                _level.shape.cells[i] = cell;
            }

            if (GUILayout.Button("Add Cell"))
            {
                _level.shape.cells.Add(new CellDefinition
                {
                    cellId = $"c{_level.shape.cells.Count + 1}",
                    letter = "A"
                });
            }
        }

        private void DrawTargetWords()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Target Words", EditorStyles.boldLabel);

            if (_level.targetWords == null)
            {
                _level.targetWords = new string[0];
            }

            var size = Mathf.Max(0, EditorGUILayout.IntField("Count", _level.targetWords.Length));
            if (size != _level.targetWords.Length)
            {
                var resized = new string[size];
                for (var i = 0; i < resized.Length && i < _level.targetWords.Length; i++)
                {
                    resized[i] = _level.targetWords[i];
                }

                _level.targetWords = resized;
            }

            for (var i = 0; i < _level.targetWords.Length; i++)
            {
                _level.targetWords[i] = EditorGUILayout.TextField($"Word {i + 1}", _level.targetWords[i]);
            }
        }

        private void DrawActions()
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Validate"))
            {
                ValidateLevel();
            }

            if (GUILayout.Button("Generate Draft Cells From Profile"))
            {
                GenerateDraft();
            }

            if (GUILayout.Button("Make Letters Unique"))
            {
                MakeLettersUnique();
            }

            if (GUILayout.Button("Repack Cells (Compact)"))
            {
                RepackCellsCompact();
            }

            if (GUILayout.Button("Preview Play"))
            {
                PreviewPlayContext.SetLevel(_level);
                EditorApplication.EnterPlaymode();
            }

            if (GUILayout.Button("Save"))
            {
                EditorUtility.SetDirty(_level);
                AssetDatabase.SaveAssets();
            }
        }

        private void ValidateLevel()
        {
            if (_level.targetWords == null || _level.targetWords.Length == 0)
            {
                Debug.LogWarning("No target words to validate.");
                return;
            }

            var failed = 0;
            for (var i = 0; i < _level.targetWords.Length; i++)
            {
                var word = _level.targetWords[i];
                var ok = LevelPathValidator.CanBuildWord(_level, word);
                if (!ok)
                {
                    failed++;
                    Debug.LogWarning($"Unreachable word: {word}");
                }
            }

            if (failed == 0)
            {
                Debug.Log("Level validation passed.");
            }
            else
            {
                Debug.LogWarning($"Level validation failed for {failed} words.");
            }
        }

        private void GenerateDraft()
        {
            if (_dictionary == null || _generationProfile == null)
            {
                Debug.LogWarning("Set Dictionary and Generation Profile first.");
                return;
            }

            var candidates = LevelGenerator.FilterWords(_dictionary, _generationProfile);
            var cells = LevelGenerator.GenerateCells(_generationProfile, candidates);
            _level.shape.cells = cells;
            _level.boardLayoutMode = _generationProfile.boardLayoutMode;
            _level.minTargetWordsToComplete = _generationProfile.minTargetWordsToComplete;
            _level.allowBonusWords = _generationProfile.allowBonusWords;
            _level.allowBonusInLevelOnly = _generationProfile.allowBonusInLevelOnly;
            _level.bonusRequiresEmbeddedInLevelOnly = _generationProfile.bonusRequiresEmbeddedInLevelOnly;

            var words = new List<string>();
            for (var i = 0; i < Mathf.Min(5, candidates.Count); i++)
            {
                words.Add(candidates[i].word);
            }

            _level.targetWords = words.ToArray();
            Debug.Log($"Generated draft with {cells.Count} cells and {words.Count} words. Avoid duplicates: {_generationProfile.avoidDuplicateLetters}");
        }

        private void MakeLettersUnique()
        {
            if (_level.shape.cells == null || _level.shape.cells.Count == 0)
            {
                Debug.LogWarning("No cells to update.");
                return;
            }

            LevelGenerator.EnsureUniqueLetters(_level.shape.cells, _level.language);
            EditorUtility.SetDirty(_level);
            Debug.Log("Updated level letters to unique set where possible.");
        }

        private void RepackCellsCompact()
        {
            if (_level.shape.cells == null || _level.shape.cells.Count == 0)
            {
                Debug.LogWarning("No cells to repack.");
                return;
            }

            LevelGenerator.RepackCellsCompact(_level.shape.cells);
            EditorUtility.SetDirty(_level);
            Debug.Log(_level.shape.cells.Count == HexBoardTemplate16.CellCount
                ? "Applied fixed16 canonical hex layout."
                : "Repacked cell coordinates to compact hex layout.");
        }

        private void CreateLevelAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject("Create Level", "LevelDefinition", "asset", "Pick location for LevelDefinition asset");
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var asset = CreateInstance<LevelDefinition>();
            asset.levelId = "1";
            asset.targetScore = 10;
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            _level = asset;
        }
    }
}

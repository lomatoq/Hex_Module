using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HexWords.Core;
using UnityEditor;
using UnityEngine;

namespace HexWords.EditorTools
{
    public class DictionaryImportWindow : EditorWindow
    {
        private Language _language = Language.RU;
        private string _category = "general";
        private int _minLevel = 1;
        private int _maxLevel = 999;
        private int _difficultyBand;
        private int _minLength = 3;
        private int _maxLength = 12;
        private string _inputPath = string.Empty;
        private string _outputCsvPath = "Assets/_Game/Data/Source/dictionary_ru.csv";
        private bool _appendToFile;
        private Vector2 _scroll;

        [MenuItem("Tools/HexWords/Dictionary Importer")]
        public static void Open()
        {
            GetWindow<DictionaryImportWindow>("Dictionary Importer");
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
            DrawPathPicker("Input file (.txt/.csv)", ref _inputPath, false);
            DrawPathPicker("Output CSV (project path)", ref _outputCsvPath, true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Defaults for imported words", EditorStyles.boldLabel);
            _language = (Language)EditorGUILayout.EnumPopup("Language", _language);
            _category = EditorGUILayout.TextField("Category", _category);
            _minLevel = EditorGUILayout.IntField("Min Level", _minLevel);
            _maxLevel = EditorGUILayout.IntField("Max Level", _maxLevel);
            _difficultyBand = EditorGUILayout.IntField("Difficulty Band", _difficultyBand);
            _minLength = EditorGUILayout.IntField("Min Length", _minLength);
            _maxLength = EditorGUILayout.IntField("Max Length", _maxLength);
            _appendToFile = EditorGUILayout.Toggle("Append to existing CSV", _appendToFile);

            EditorGUILayout.Space();
            if (GUILayout.Button("Import Word List"))
            {
                Import();
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawPathPicker(string label, ref string path, bool projectPath)
        {
            EditorGUILayout.BeginHorizontal();
            path = EditorGUILayout.TextField(label, path);
            if (GUILayout.Button("Browse", GUILayout.Width(90)))
            {
                path = projectPath
                    ? EditorUtility.SaveFilePanelInProject("Select output CSV", Path.GetFileName(path), "csv", "Choose destination csv")
                    : EditorUtility.OpenFilePanel("Select word list file", string.Empty, "txt,csv");
            }

            EditorGUILayout.EndHorizontal();
        }

        private void Import()
        {
            if (string.IsNullOrWhiteSpace(_inputPath) || !File.Exists(_inputPath))
            {
                Debug.LogWarning("Input file not found.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_outputCsvPath) || !_outputCsvPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                Debug.LogWarning("Output path must be a project-relative path under Assets/.");
                return;
            }

            var targetAbsPath = Path.Combine(Directory.GetCurrentDirectory(), _outputCsvPath);
            var targetDir = Path.GetDirectoryName(targetAbsPath);
            if (!string.IsNullOrWhiteSpace(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            var importedWords = ReadWords(_inputPath);
            var filteredWords = FilterWords(importedWords);
            var existing = _appendToFile && File.Exists(targetAbsPath) ? ReadWordsFromCsv(targetAbsPath) : new HashSet<string>();

            var added = 0;
            using (var writer = new StreamWriter(targetAbsPath, _appendToFile && File.Exists(targetAbsPath), Encoding.UTF8))
            {
                if (!_appendToFile || !File.Exists(targetAbsPath) || new FileInfo(targetAbsPath).Length == 0)
                {
                    writer.WriteLine("word,category,minLevel,maxLevel,difficultyBand");
                }

                foreach (var word in filteredWords)
                {
                    if (!existing.Add(word))
                    {
                        continue;
                    }

                    writer.WriteLine($"{word},{_category},{_minLevel},{_maxLevel},{_difficultyBand}");
                    added++;
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"Dictionary import completed. Added words: {added}. Output: {_outputCsvPath}");
        }

        private static HashSet<string> ReadWords(string path)
        {
            var content = File.ReadAllText(path);
            var rows = CsvUtility.Parse(content);
            var words = new HashSet<string>();

            if (rows.Count > 0)
            {
                var header = rows[0];
                var hasWordColumn = false;
                for (var i = 0; i < header.Length; i++)
                {
                    if (string.Equals(header[i], "word", StringComparison.OrdinalIgnoreCase))
                    {
                        hasWordColumn = true;
                        break;
                    }
                }

                if (hasWordColumn)
                {
                    var idx = CsvUtility.HeaderIndex(header);
                    for (var i = 1; i < rows.Count; i++)
                    {
                        var value = WordNormalizer.Normalize(CsvUtility.Get(rows[i], idx, "word"));
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            words.Add(value);
                        }
                    }

                    return words;
                }
            }

            var split = content.Split(new[] { '\r', '\n', '\t', ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < split.Length; i++)
            {
                words.Add(WordNormalizer.Normalize(split[i]));
            }

            return words;
        }

        private HashSet<string> FilterWords(HashSet<string> words)
        {
            var result = new HashSet<string>();
            foreach (var word in words)
            {
                if (word.Length < _minLength || word.Length > _maxLength)
                {
                    continue;
                }

                if (!WordNormalizer.IsAsciiOrCyrillicLetterString(word))
                {
                    continue;
                }

                if (_language == Language.RU && ContainsLatin(word))
                {
                    continue;
                }

                if (_language == Language.EN && ContainsCyrillic(word))
                {
                    continue;
                }

                result.Add(word);
            }

            return result;
        }

        private static HashSet<string> ReadWordsFromCsv(string path)
        {
            var rows = CsvUtility.Parse(File.ReadAllText(path));
            var words = new HashSet<string>();
            if (rows.Count <= 1)
            {
                return words;
            }

            var idx = CsvUtility.HeaderIndex(rows[0]);
            for (var i = 1; i < rows.Count; i++)
            {
                var word = WordNormalizer.Normalize(CsvUtility.Get(rows[i], idx, "word"));
                if (!string.IsNullOrWhiteSpace(word))
                {
                    words.Add(word);
                }
            }

            return words;
        }

        private static bool ContainsLatin(string value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] is >= 'A' and <= 'Z')
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsCyrillic(string value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c is >= 'А' and <= 'Я' || c == 'Ё')
                {
                    return true;
                }
            }

            return false;
        }
    }
}

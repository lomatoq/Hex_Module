using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using HexWords.Core;
using UnityEditor;
using UnityEngine;

namespace HexWords.EditorTools
{
    public class DictionaryImportWindow : EditorWindow
    {
        private struct CategoryRule
        {
            public string pattern;
            public string type;
            public string category;
            public int priority;
            public Language? language;
        }

        private Language _language = Language.EN;
        private string _category = "general";
        private int _minLevel = 1;
        private int _maxLevel = 999;
        private int _difficultyBand;
        private int _minLength = 3;
        private int _maxLength = 12;
        private string _inputPath = string.Empty;
        private string _outputCsvPath = "Assets/_Game/Data/Source/dictionary_en.csv";
        private bool _appendToFile = true;

        private bool _useCategoryRules = true;
        private string _categoryRulesPath = "Assets/_Game/Data/Source/category_rules_en.csv";
        private bool _useBlacklist = true;
        private string _blacklistPath = "Assets/_Game/Data/Source/blacklist_en.txt";
        private int _previewCount = 20;

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
            _category = EditorGUILayout.TextField("Fallback Category", _category);
            _minLevel = EditorGUILayout.IntField("Min Level", _minLevel);
            _maxLevel = EditorGUILayout.IntField("Max Level", _maxLevel);
            _difficultyBand = EditorGUILayout.IntField("Difficulty Band", _difficultyBand);
            _minLength = EditorGUILayout.IntField("Min Length", _minLength);
            _maxLength = EditorGUILayout.IntField("Max Length", _maxLength);
            _appendToFile = EditorGUILayout.Toggle("Append to existing CSV", _appendToFile);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Auto categorization", EditorStyles.boldLabel);
            _useCategoryRules = EditorGUILayout.Toggle("Use category rules", _useCategoryRules);
            if (_useCategoryRules)
            {
                DrawPathPicker("Category rules CSV", ref _categoryRulesPath, true);
            }

            _useBlacklist = EditorGUILayout.Toggle("Use blacklist", _useBlacklist);
            if (_useBlacklist)
            {
                DrawPathPicker("Blacklist file (.txt)", ref _blacklistPath, true);
            }

            _previewCount = Mathf.Clamp(EditorGUILayout.IntField("Preview rows", _previewCount), 1, 200);

            EditorGUILayout.Space();
            if (GUILayout.Button("Preview Categorization"))
            {
                Preview();
            }

            if (GUILayout.Button("Import Word List"))
            {
                Import();
            }

            if (GUILayout.Button("Create Sample EN Rule Files"))
            {
                CreateSampleRuleFiles();
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
                    ? EditorUtility.SaveFilePanelInProject("Select file", Path.GetFileName(path), "csv", "Choose destination file")
                    : EditorUtility.OpenFilePanel("Select input file", string.Empty, "txt,csv");
            }

            EditorGUILayout.EndHorizontal();
        }

        private void Preview()
        {
            var context = BuildImportContext(includeExisting: false);
            if (!context.ok)
            {
                return;
            }

            var shown = 0;
            foreach (var word in context.filteredWords)
            {
                if (context.blacklist.Contains(word))
                {
                    continue;
                }

                var category = ResolveCategory(word, context.rules);
                Debug.Log($"Preview: {word} -> {category}");
                shown++;
                if (shown >= _previewCount)
                {
                    break;
                }
            }

            Debug.Log($"Preview finished. Showing {shown} words.");
        }

        private void Import()
        {
            var context = BuildImportContext(includeExisting: true);
            if (!context.ok)
            {
                return;
            }

            var targetAbsPath = context.targetAbsPath;
            var append = _appendToFile && File.Exists(targetAbsPath);
            var existingCount = context.existing.Count;
            var added = 0;
            var skippedBlacklisted = 0;

            using (var writer = new StreamWriter(targetAbsPath, append, Encoding.UTF8))
            {
                if (!append || new FileInfo(targetAbsPath).Length == 0)
                {
                    writer.WriteLine("word,category,minLevel,maxLevel,difficultyBand");
                }

                foreach (var word in context.filteredWords)
                {
                    if (context.blacklist.Contains(word))
                    {
                        skippedBlacklisted++;
                        continue;
                    }

                    if (!context.existing.Add(word))
                    {
                        continue;
                    }

                    var category = ResolveCategory(word, context.rules);
                    writer.WriteLine($"{word},{category},{_minLevel},{_maxLevel},{_difficultyBand}");
                    added++;
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"Dictionary import completed. Added={added}, skipped_blacklist={skippedBlacklisted}, existing_before={existingCount}, output={_outputCsvPath}");
        }

        private (bool ok, string targetAbsPath, List<string> filteredWords, HashSet<string> existing, HashSet<string> blacklist, List<CategoryRule> rules) BuildImportContext(bool includeExisting)
        {
            if (string.IsNullOrWhiteSpace(_inputPath) || !File.Exists(_inputPath))
            {
                Debug.LogWarning("Input file not found.");
                return (false, string.Empty, null, null, null, null);
            }

            if (string.IsNullOrWhiteSpace(_outputCsvPath) || !_outputCsvPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                Debug.LogWarning("Output path must be a project-relative path under Assets/.");
                return (false, string.Empty, null, null, null, null);
            }

            var targetAbsPath = Path.Combine(Directory.GetCurrentDirectory(), _outputCsvPath);
            var targetDir = Path.GetDirectoryName(targetAbsPath);
            if (!string.IsNullOrWhiteSpace(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            var importedWords = ReadWords(_inputPath);
            var filteredWords = FilterWords(importedWords);
            Debug.Log($"Dictionary importer: read={importedWords.Count}, after_filters={filteredWords.Count}, language={_language}, len={_minLength}-{_maxLength}");

            if (filteredWords.Count == 0)
            {
                LogLikelyLanguageMismatch(importedWords);
                Debug.LogWarning("No words passed filters. Output file was not modified.");
                return (false, string.Empty, null, null, null, null);
            }

            var rules = _useCategoryRules ? LoadCategoryRules(_categoryRulesPath) : new List<CategoryRule>();
            var blacklist = _useBlacklist ? LoadBlacklist(_blacklistPath) : new HashSet<string>();
            var existing = includeExisting && _appendToFile && File.Exists(targetAbsPath) ? ReadWordsFromCsv(targetAbsPath) : new HashSet<string>();

            return (true, targetAbsPath, filteredWords, existing, blacklist, rules);
        }

        private void LogLikelyLanguageMismatch(HashSet<string> importedWords)
        {
            var latinWords = 0;
            var cyrWords = 0;
            foreach (var word in importedWords)
            {
                if (ContainsLatin(word))
                {
                    latinWords++;
                }

                if (ContainsCyrillic(word))
                {
                    cyrWords++;
                }
            }

            if (_language == Language.RU && latinWords > 0 && cyrWords == 0)
            {
                Debug.LogWarning("No words passed filters: source looks like EN, but Language is RU.");
            }
            else if (_language == Language.EN && cyrWords > 0 && latinWords == 0)
            {
                Debug.LogWarning("No words passed filters: source looks like RU, but Language is EN.");
            }
        }

        private string ResolveCategory(string word, List<CategoryRule> rules)
        {
            if (rules == null || rules.Count == 0)
            {
                return _category;
            }

            CategoryRule? best = null;
            for (var i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (rule.language.HasValue && rule.language.Value != _language)
                {
                    continue;
                }

                if (!RuleMatches(word, rule))
                {
                    continue;
                }

                if (!best.HasValue || rule.priority > best.Value.priority)
                {
                    best = rule;
                }
            }

            return best.HasValue ? best.Value.category : _category;
        }

        private static bool RuleMatches(string word, CategoryRule rule)
        {
            switch (rule.type)
            {
                case "exact":
                    return string.Equals(word, rule.pattern, StringComparison.Ordinal);
                case "startswith":
                    return word.StartsWith(rule.pattern, StringComparison.Ordinal);
                case "endswith":
                    return word.EndsWith(rule.pattern, StringComparison.Ordinal);
                case "contains":
                    return word.Contains(rule.pattern);
                case "regex":
                    return Regex.IsMatch(word, rule.pattern, RegexOptions.CultureInvariant);
                default:
                    return false;
            }
        }

        private List<CategoryRule> LoadCategoryRules(string projectPath)
        {
            var rules = new List<CategoryRule>();
            if (string.IsNullOrWhiteSpace(projectPath) || !projectPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                Debug.LogWarning("Category rules path must be under Assets/. Rules disabled.");
                return rules;
            }

            var absPath = Path.Combine(Directory.GetCurrentDirectory(), projectPath);
            if (!File.Exists(absPath))
            {
                Debug.LogWarning($"Category rules file not found: {projectPath}. Rules disabled.");
                return rules;
            }

            var rows = CsvUtility.Parse(File.ReadAllText(absPath));
            if (rows.Count <= 1)
            {
                return rules;
            }

            var header = rows[0];
            for (var i = 0; i < header.Length; i++)
            {
                header[i] = header[i]?.TrimStart('\uFEFF');
            }

            var idx = CsvUtility.HeaderIndex(header);
            for (var i = 1; i < rows.Count; i++)
            {
                var pattern = NormalizeToken(CsvUtility.Get(rows[i], idx, "pattern"));
                var type = NormalizeToken(CsvUtility.Get(rows[i], idx, "type")).ToLowerInvariant();
                var category = NormalizeToken(CsvUtility.Get(rows[i], idx, "category")).ToLowerInvariant();
                var priorityRaw = CsvUtility.Get(rows[i], idx, "priority");
                var langRaw = NormalizeToken(CsvUtility.Get(rows[i], idx, "language"));

                if (string.IsNullOrWhiteSpace(pattern) || string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(category))
                {
                    continue;
                }

                if (!int.TryParse(priorityRaw, out var priority))
                {
                    priority = 0;
                }

                Language? lang = null;
                if (Enum.TryParse(langRaw, true, out Language parsedLang))
                {
                    lang = parsedLang;
                }

                rules.Add(new CategoryRule
                {
                    pattern = pattern,
                    type = type,
                    category = category,
                    priority = priority,
                    language = lang
                });
            }

            Debug.Log($"Loaded category rules: {rules.Count} from {projectPath}");
            return rules;
        }

        private HashSet<string> LoadBlacklist(string projectPath)
        {
            var blacklist = new HashSet<string>();
            if (string.IsNullOrWhiteSpace(projectPath) || !projectPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                Debug.LogWarning("Blacklist path must be under Assets/. Blacklist disabled.");
                return blacklist;
            }

            var absPath = Path.Combine(Directory.GetCurrentDirectory(), projectPath);
            if (!File.Exists(absPath))
            {
                Debug.LogWarning($"Blacklist file not found: {projectPath}. Blacklist disabled.");
                return blacklist;
            }

            var lines = File.ReadAllLines(absPath);
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                var normalized = NormalizeToken(trimmed);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    blacklist.Add(normalized);
                }
            }

            Debug.Log($"Loaded blacklist words: {blacklist.Count} from {projectPath}");
            return blacklist;
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
                    header[i] = header[i]?.TrimStart('\uFEFF');
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
                        var value = NormalizeToken(CsvUtility.Get(rows[i], idx, "word"));
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            words.Add(value);
                        }
                    }

                    return words;
                }
            }

            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < lines.Length; i++)
            {
                var token = ExtractTokenFromLine(lines[i]);
                var normalized = NormalizeToken(token);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    words.Add(normalized);
                }
            }

            return words;
        }

        private List<string> FilterWords(HashSet<string> words)
        {
            var result = new List<string>();
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

            result.Sort(StringComparer.Ordinal);
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

            var header = rows[0];
            for (var i = 0; i < header.Length; i++)
            {
                header[i] = header[i]?.TrimStart('\uFEFF');
            }

            var idx = CsvUtility.HeaderIndex(header);
            for (var i = 1; i < rows.Count; i++)
            {
                var word = NormalizeToken(CsvUtility.Get(rows[i], idx, "word"));
                if (!string.IsNullOrWhiteSpace(word))
                {
                    words.Add(word);
                }
            }

            return words;
        }

        private void CreateSampleRuleFiles()
        {
            var rulesPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets/_Game/Data/Source/category_rules_en.csv");
            var blacklistPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets/_Game/Data/Source/blacklist_en.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(rulesPath) ?? "Assets/_Game/Data/Source");

            if (!File.Exists(rulesPath))
            {
                File.WriteAllText(rulesPath,
                    "pattern,type,category,priority,language\n" +
                    "CAT,exact,animals,100,EN\n" +
                    "DOG,exact,animals,100,EN\n" +
                    "FISH,exact,animals,100,EN\n" +
                    "^UN.*,regex,general,10,EN\n" +
                    ".*ING$,regex,verbs,20,EN\n");
            }

            if (!File.Exists(blacklistPath))
            {
                File.WriteAllText(blacklistPath,
                    "# one word per line\n" +
                    "AARDVARKS\n");
            }

            AssetDatabase.Refresh();
            _categoryRulesPath = "Assets/_Game/Data/Source/category_rules_en.csv";
            _blacklistPath = "Assets/_Game/Data/Source/blacklist_en.txt";
            Debug.Log("Sample EN rule files created (if missing).\n- Assets/_Game/Data/Source/category_rules_en.csv\n- Assets/_Game/Data/Source/blacklist_en.txt");
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

        private static string ExtractTokenFromLine(string line)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return string.Empty;
            }

            var commaIdx = trimmed.IndexOf(',');
            if (commaIdx > 0)
            {
                return trimmed.Substring(0, commaIdx);
            }

            return trimmed;
        }

        private static string NormalizeToken(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            var token = raw.Trim();
            token = token.TrimStart('\uFEFF');
            token = token.Trim().Trim('"', '\'');
            token = token.Trim().Trim(',', ';');
            token = token.Trim().Trim('"', '\'');
            return WordNormalizer.Normalize(token);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HexWords.Editor.Theming
{
    /// <summary>
    /// Editor-only cache of every slot id the collector has ever seen. Lets
    /// custom inspectors show a dropdown when picking a slot for a themed
    /// component or a theme entry.
    /// </summary>
    public static class ThemeSlotRegistry
    {
        private const string PrefsKey = "HexWords.Theming.KnownSlotIds";

        private static List<string> _cached;

        public static IReadOnlyList<string> All
        {
            get
            {
                if (_cached == null) Load();
                return _cached;
            }
        }

        public static void Add(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (_cached == null) Load();
            if (_cached.Contains(id)) return;
            _cached.Add(id);
        }

        public static void Replace(IEnumerable<string> ids)
        {
            _cached = ids.Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToList();
            Save();
        }

        public static void Save()
        {
            if (_cached == null) return;
            EditorPrefs.SetString(PrefsKey, string.Join("\n", _cached));
        }

        private static void Load()
        {
            var raw = EditorPrefs.GetString(PrefsKey, string.Empty);
            if (string.IsNullOrEmpty(raw)) { _cached = new List<string>(); return; }
            _cached = raw.Split('\n').Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToList();
        }
    }
}

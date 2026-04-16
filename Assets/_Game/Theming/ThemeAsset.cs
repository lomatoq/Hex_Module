using System;
using System.Collections.Generic;
using HexWords.Core;
using UnityEngine;

namespace HexWords.Theming
{
    /// <summary>
    /// A single "skin" for the game. Contains per-slot overrides for sprites,
    /// colors, and visibility. Leave a flag off to inherit the original
    /// design-time value from the scene/prefab.
    ///
    /// Workflow:
    ///   1. Run HexWords → Theme → Collect Themeable Elements (editor window).
    ///   2. It creates/updates "DefaultTheme" with every discovered slot and
    ///      a snapshot of the current sprites/colors.
    ///   3. Duplicate DefaultTheme → "DarkTheme" (or whatever) and override
    ///      the slots you want different.
    ///   4. At runtime call ThemeManager.Instance.SetTheme(darkTheme).
    /// </summary>
    [CreateAssetMenu(menuName = "HexWords/Theming/Theme Asset", fileName = "Theme")]
    public class ThemeAsset : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("Unique identifier — typically hierarchy-derived, e.g. 'HUD/Header/CoinIcon'.")]
            public string slotId;

            [Header("Overrides (leave off to keep original)")]
            [Tooltip("Replace the sprite on the bound Image.")]
            public bool useSprite;
            public Sprite sprite;

            [Tooltip("Replace the color on the bound Graphic (Image / TMP_Text / etc).")]
            public bool useColor;
            public Color color = Color.white;

            [Tooltip("Override visibility. When false, the graphic is hidden (Image.enabled = false).")]
            public bool useVisibility;
            public bool visible = true;

            public bool HasAnyOverride => useSprite || useColor || useVisibility;
        }

        [Tooltip("Human-readable theme name shown in debug UIs / picker lists.")]
        public string displayName = "Theme";

        [Tooltip("Optional — replaces the gameplay FeedbackPalette while this theme is active (HUD, cell flashes, trail). Leave empty to keep the default palette assigned in the scene.")]
        public FeedbackPalette paletteOverride;

        [Tooltip("Every slot known to the game. Flags decide whether to override or inherit.")]
        public List<Entry> entries = new List<Entry>();

        // ── Lookup ────────────────────────────────────────────────────────────

        private Dictionary<string, Entry> _cache;

        public Entry GetEntry(string slotId)
        {
            if (string.IsNullOrEmpty(slotId)) return null;
            if (_cache == null) RebuildCache();
            return _cache.TryGetValue(slotId, out var e) ? e : null;
        }

        public void RebuildCache()
        {
            _cache = new Dictionary<string, Entry>(entries.Count);
            for (var i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null || string.IsNullOrEmpty(e.slotId)) continue;
                _cache[e.slotId] = e; // last wins on duplicate
            }
        }

        public Entry GetOrCreateEntry(string slotId)
        {
            var e = GetEntry(slotId);
            if (e != null) return e;
            e = new Entry { slotId = slotId };
            entries.Add(e);
            _cache = null; // invalidate
            return e;
        }

        private void OnEnable()   => _cache = null;
        private void OnValidate() => _cache = null;
    }
}

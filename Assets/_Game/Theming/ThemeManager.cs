using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexWords.Theming
{
    /// <summary>
    /// Runtime hub for the theme system. Keeps a registry of every enabled
    /// <see cref="IThemedElement"/> and a reference to the current
    /// <see cref="ThemeAsset"/>. Changing the theme (or registering a new
    /// element) triggers an immediate apply.
    ///
    /// Execution order is negative so themed components' OnEnable (which
    /// pulls the current theme) sees a ready manager.
    /// </summary>
    [DefaultExecutionOrder(-5000)]
    [DisallowMultipleComponent]
    public class ThemeManager : MonoBehaviour
    {
        private static ThemeManager _instance;
        public static  ThemeManager  Instance => _instance;

        /// <summary>Fallback for components that enable before a manager exists in the scene.</summary>
        private static readonly HashSet<IThemedElement> _pendingRegistry = new HashSet<IThemedElement>();

        [SerializeField] private ThemeAsset startupTheme;
        [Tooltip("If true, this manager survives scene loads.")]
        [SerializeField] private bool persistAcrossScenes = false;

        public ThemeAsset CurrentTheme { get; private set; }

        /// <summary>Fired after a theme was applied (or cleared). Argument may be null.</summary>
        public event Action<ThemeAsset> ThemeApplied;

        private readonly HashSet<IThemedElement> _registry = new HashSet<IThemedElement>();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            if (persistAcrossScenes) DontDestroyOnLoad(gameObject);

            // Adopt anything that tried to register before we existed.
            foreach (var el in _pendingRegistry) _registry.Add(el);
            _pendingRegistry.Clear();
        }

        private void Start()
        {
            if (startupTheme != null) SetTheme(startupTheme);
            else                      ApplyToAll(null); // force restore-originals pass
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // ── Registry ──────────────────────────────────────────────────────────

        public static void Register(IThemedElement element)
        {
            if (element == null) return;
            if (_instance != null)
            {
                if (_instance._registry.Add(element) && _instance.CurrentTheme != null)
                    element.ApplyTheme(_instance.CurrentTheme);
            }
            else
            {
                _pendingRegistry.Add(element);
            }
        }

        public static void Unregister(IThemedElement element)
        {
            if (element == null) return;
            if (_instance != null) _instance._registry.Remove(element);
            else                   _pendingRegistry.Remove(element);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void SetTheme(ThemeAsset theme)
        {
            CurrentTheme = theme;
            ApplyToAll(theme);
            ThemeApplied?.Invoke(theme);
        }

        public void ReapplyCurrent() => ApplyToAll(CurrentTheme);

        // ── Internal ──────────────────────────────────────────────────────────

        private void ApplyToAll(ThemeAsset theme)
        {
            // Snapshot to guard against mutations during iteration.
            if (_registry.Count == 0) return;
            var buf = new IThemedElement[_registry.Count];
            _registry.CopyTo(buf);
            for (var i = 0; i < buf.Length; i++)
            {
                try { buf[i]?.ApplyTheme(theme); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }
    }
}

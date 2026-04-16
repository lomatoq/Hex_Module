using System;
using HexWords.Core;

namespace HexWords.Theming
{
    /// <summary>
    /// Runtime lookup for <see cref="FeedbackPalette"/>. The active theme can
    /// install an override; consumers resolve their serialized palette through
    /// <see cref="Resolve"/> so the override wins when present.
    ///
    /// <para>Usage in a consumer:</para>
    /// <code>
    /// [SerializeField] private FeedbackPalette feedbackPalette;
    /// private FeedbackPalette Palette =&gt; FeedbackPaletteProvider.Resolve(feedbackPalette);
    /// // then read: Palette.cellAcceptedBackground, etc.
    /// </code>
    ///
    /// Subscribe to <see cref="Changed"/> if you have already-applied colors
    /// that must refresh when a theme swaps (e.g. a currently-selected cell).
    /// </summary>
    public static class FeedbackPaletteProvider
    {
        private static FeedbackPalette _override;

        public static FeedbackPalette Override => _override;

        /// <summary>Fires whenever the override is installed or cleared.</summary>
        public static event Action Changed;

        public static FeedbackPalette Resolve(FeedbackPalette fallback)
        {
            return _override != null ? _override : fallback;
        }

        public static void SetOverride(FeedbackPalette palette)
        {
            if (_override == palette) return;
            _override = palette;
            Changed?.Invoke();
        }
    }
}

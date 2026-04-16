namespace HexWords.Theming
{
    /// <summary>
    /// Implemented by any component that can be re-skinned by a <see cref="ThemeAsset"/>.
    /// Implementations register themselves with <see cref="ThemeManager"/> in OnEnable
    /// and unregister in OnDisable.
    /// </summary>
    public interface IThemedElement
    {
        /// <summary>Slot id this element binds to, e.g. "HUD/Header/CoinIcon".</summary>
        string SlotId { get; }

        /// <summary>
        /// Apply the given theme. Passing null means "restore design-time defaults".
        /// </summary>
        void ApplyTheme(ThemeAsset theme);
    }
}

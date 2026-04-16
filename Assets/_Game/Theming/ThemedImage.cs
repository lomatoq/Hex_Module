using UnityEngine;
using UnityEngine.UI;

namespace HexWords.Theming
{
    /// <summary>
    /// Binds a <see cref="Image"/> to a theme slot. On Awake it snapshots the
    /// design-time sprite / color / enabled state — so switching away from a
    /// customized theme restores the original look, not whatever the last
    /// theme left behind.
    ///
    /// Gameplay scripts that mutate image.color at runtime (e.g. HexCellView)
    /// should re-read their base color from the Image in Start() or listen to
    /// <see cref="ThemeManager.ThemeApplied"/> — by then the theme has
    /// already applied.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public class ThemedImage : MonoBehaviour, IThemedElement
    {
        [SerializeField] private string slotId;
        public string SlotId => slotId;

#if UNITY_EDITOR
        // Editor-only setter used by the collector.
        public void EditorSetSlotId(string id) => slotId = id;
#endif

        private Image   _image;
        private Sprite  _originalSprite;
        private Color   _originalColor;
        private bool    _originalEnabled;
        private bool    _snapshotTaken;

        private void Awake()
        {
            _image = GetComponent<Image>();
            TakeSnapshot();
        }

        private void OnEnable()
        {
            if (!_snapshotTaken) TakeSnapshot();
            ThemeManager.Register(this);
        }

        private void OnDisable()
        {
            ThemeManager.Unregister(this);
        }

        private void TakeSnapshot()
        {
            if (_image == null) _image = GetComponent<Image>();
            if (_image == null) return;
            _originalSprite  = _image.sprite;
            _originalColor   = _image.color;
            _originalEnabled = _image.enabled;
            _snapshotTaken   = true;
        }

        public void ApplyTheme(ThemeAsset theme)
        {
            if (_image == null) return;
            var entry = theme != null ? theme.GetEntry(slotId) : null;

            // Sprite
            if (entry != null && entry.useSprite && entry.sprite != null)
                _image.sprite = entry.sprite;
            else
                _image.sprite = _originalSprite;

            // Color
            if (entry != null && entry.useColor)
                _image.color = entry.color;
            else
                _image.color = _originalColor;

            // Visibility (uses Image.enabled — keeps GameObject active so we can switch back)
            if (entry != null && entry.useVisibility)
                _image.enabled = entry.visible;
            else
                _image.enabled = _originalEnabled;
        }
    }
}

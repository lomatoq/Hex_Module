using UnityEngine;
using UnityEngine.UI;

namespace HexWords.Theming
{
    /// <summary>
    /// Binds any <see cref="Graphic"/> (RawImage, TextMeshProUGUI, legacy Text,
    /// etc.) to a theme slot for COLOR ONLY. Use <see cref="ThemedImage"/> when
    /// you also need sprite override.
    /// </summary>
    [DisallowMultipleComponent]
    public class ThemedGraphicColor : MonoBehaviour, IThemedElement
    {
        [SerializeField] private string  slotId;
        [SerializeField] private Graphic target;

        public string SlotId => slotId;

#if UNITY_EDITOR
        public void EditorSetSlotId(string id) => slotId = id;
        public void EditorSetTarget(Graphic g) => target = g;
#endif

        private Color _originalColor;
        private bool  _originalEnabled;
        private bool  _snapshotTaken;

        private void Awake()
        {
            if (target == null) target = GetComponent<Graphic>();
            TakeSnapshot();
        }

        private void OnEnable()
        {
            if (!_snapshotTaken) TakeSnapshot();
            ThemeManager.Register(this);
        }

        private void OnDisable() => ThemeManager.Unregister(this);

        private void TakeSnapshot()
        {
            if (target == null) return;
            _originalColor   = target.color;
            _originalEnabled = target.enabled;
            _snapshotTaken   = true;
        }

        public void ApplyTheme(ThemeAsset theme)
        {
            if (target == null) return;
            var entry = theme != null ? theme.GetEntry(slotId) : null;

            if (entry != null && entry.useColor) target.color   = entry.color;
            else                                 target.color   = _originalColor;

            if (entry != null && entry.useVisibility) target.enabled = entry.visible;
            else                                      target.enabled = _originalEnabled;
        }
    }
}

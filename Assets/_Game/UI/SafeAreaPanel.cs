using UnityEngine;

namespace HexWords.UI
{
    /// <summary>
    /// Attach to a full-screen RectTransform that should respect the device safe area
    /// (notch, home indicator, status bar). Works at runtime on any resolution / device.
    /// The parent must be a full-screen Canvas RectTransform.
    /// </summary>
    [ExecuteAlways]
    public class SafeAreaPanel : MonoBehaviour
    {
        [Tooltip("Simulate safe area insets in the Editor for testing (pixels at 1080x1920).")]
        [SerializeField] private bool simulateInEditor = false;
        [SerializeField] private RectOffset simulatedInset = new RectOffset(0, 0, 88, 34); // top notch / bottom bar

        private RectTransform _rt;
        private Rect          _lastSafeArea = Rect.zero;
        private Vector2Int    _lastScreenSize;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
        }

        private void Update()
        {
            var safeArea   = GetSafeArea();
            var screenSize = new Vector2Int(Screen.width, Screen.height);

            if (safeArea != _lastSafeArea || screenSize != _lastScreenSize)
            {
                Apply(safeArea);
                _lastSafeArea   = safeArea;
                _lastScreenSize = screenSize;
            }
        }

        private Rect GetSafeArea()
        {
#if UNITY_EDITOR
            if (simulateInEditor && Screen.width > 0 && Screen.height > 0)
            {
                return new Rect(
                    simulatedInset.left,
                    simulatedInset.bottom,
                    Screen.width  - simulatedInset.left - simulatedInset.right,
                    Screen.height - simulatedInset.top  - simulatedInset.bottom);
            }
#endif
            return Screen.safeArea;
        }

        private void Apply(Rect area)
        {
            if (_rt == null) return;
            var scrnW = (float)Screen.width;
            var scrnH = (float)Screen.height;
            if (scrnW <= 0 || scrnH <= 0) return;

            _rt.anchorMin = new Vector2(area.x / scrnW, area.y / scrnH);
            _rt.anchorMax = new Vector2((area.x + area.width) / scrnW,
                                        (area.y + area.height) / scrnH);
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}

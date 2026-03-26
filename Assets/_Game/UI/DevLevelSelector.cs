using HexWords.Core;
using HexWords.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace HexWords.UI
{
    /// <summary>
    /// Hidden dev panel for manual level selection during testing.
    ///
    /// HOW TO ACTIVATE:
    ///   Tap the invisible trigger zone (top-left corner) 5 times quickly → panel appears.
    ///
    /// SETUP IN SCENE:
    ///   1. Add this script to a GameObject inside Canvas.
    ///   2. Assign the serialized fields.
    ///   3. Set TriggerZone to a transparent Button in a screen corner.
    ///   4. Make sure panel root is inactive by default.
    /// </summary>
    public class DevLevelSelector : MonoBehaviour
    {
        [Header("Activation")]
        [Tooltip("Invisible button in a corner — tap 5× quickly to open dev panel")]
        [SerializeField] private Button triggerZone;
        [SerializeField] private int    tapsRequired  = 5;
        [SerializeField] private float  tapResetTime  = 2f;   // reset counter after this many seconds

        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button     closeButton;

        [Header("Level selector")]
        [SerializeField] private Text       currentLevelText;
        [SerializeField] private Button     prevButton;        // ←
        [SerializeField] private Button     nextButton;        // →
        [SerializeField] private Button     goButton;          // PLAY THIS LEVEL

        [Header("References")]
        [SerializeField] private GameBootstrap gameBootstrap;

        // ── State ──────────────────────────────────────────────────────────
        private int   _tapCount;
        private float _lastTapTime;
        private int   _selectedLevel;   // 0-based index

        private const string PrefKey = "HexWords.CurrentLevelIndex";

        private void Awake()
        {
            if (triggerZone != null)
                triggerZone.onClick.AddListener(OnTriggerTapped);

            if (closeButton != null)
                closeButton.onClick.AddListener(ClosePanel);

            if (prevButton != null)
                prevButton.onClick.AddListener(() => ChangeLevel(-1));

            if (nextButton != null)
                nextButton.onClick.AddListener(() => ChangeLevel(+1));

            if (goButton != null)
                goButton.onClick.AddListener(OnGoClicked);

            // Make sure panel is hidden at start
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        // ── Tap trigger logic ──────────────────────────────────────────────

        private void OnTriggerTapped()
        {
            float now = Time.realtimeSinceStartup;

            if (now - _lastTapTime > tapResetTime)
                _tapCount = 0;

            _tapCount++;
            _lastTapTime = now;

            if (_tapCount >= tapsRequired)
            {
                _tapCount = 0;
                OpenPanel();
            }
        }

        // ── Panel ──────────────────────────────────────────────────────────

        private void OpenPanel()
        {
            _selectedLevel = PlayerPrefs.GetInt(PrefKey, 0);
            RefreshLabel();

            if (panelRoot != null)
                panelRoot.SetActive(true);

            Debug.Log($"[DevLevelSelector] Panel opened. Current level index: {_selectedLevel}");
        }

        private void ClosePanel()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void ChangeLevel(int delta)
        {
            _selectedLevel = Mathf.Max(0, _selectedLevel + delta);
            RefreshLabel();
        }

        private void OnGoClicked()
        {
            ClosePanel();

            if (gameBootstrap != null)
            {
                // JumpToLevel sets _currentLevelIndex in memory AND saves to PlayerPrefs
                gameBootstrap.JumpToLevel(_selectedLevel);
            }
            else
            {
                // Fallback: write to PlayerPrefs and reload scene
                PlayerPrefs.SetInt(PrefKey, _selectedLevel);
                PlayerPrefs.Save();
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                UnityEngine.SceneManagement.SceneManager.LoadScene(scene.name);
            }

            Debug.Log($"[DevLevelSelector] Jumping to level index {_selectedLevel} (Level {_selectedLevel + 1})");
        }

        private void RefreshLabel()
        {
            if (currentLevelText != null)
                currentLevelText.text = $"Level {_selectedLevel + 1}";
        }

#if UNITY_EDITOR
        // ── Editor shortcut: press L to open panel in Play Mode ───────────
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.L))
                OpenPanel();
        }
#endif
    }
}

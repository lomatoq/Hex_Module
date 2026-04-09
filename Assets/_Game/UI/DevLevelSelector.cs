using HexWords.Core;
using HexWords.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HexWords.UI
{
    public class DevLevelSelector : MonoBehaviour
    {
        [Header("Activation")]
        [Tooltip("Invisible button in a corner — tap 5× quickly to open dev panel")]
        [SerializeField] private Button triggerZone;
        [SerializeField] private int    tapsRequired  = 5;
        [SerializeField] private float  tapResetTime  = 2f;

        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button     closeButton;

        [Header("Level selector")]
        [SerializeField] private TMP_Text   currentLevelText;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button goButton;
        [SerializeField] private Button restartButton;   // ← новая кнопка

        [Header("References")]
        [SerializeField] private GameBootstrap gameBootstrap;

        private int   _tapCount;
        private float _lastTapTime;
        private int   _selectedLevel;

        private const string PrefKey = "HexWords.CurrentLevelIndex";

        private void Awake()
        {
            if (triggerZone  != null) triggerZone.onClick.AddListener(OnTriggerTapped);
            if (closeButton  != null) closeButton.onClick.AddListener(ClosePanel);
            if (prevButton   != null) prevButton.onClick.AddListener(() => ChangeLevel(-1));
            if (nextButton   != null) nextButton.onClick.AddListener(() => ChangeLevel(+1));
            if (goButton     != null) goButton.onClick.AddListener(OnGoClicked);
            if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);

            if (panelRoot != null) panelRoot.SetActive(false);
        }

        // ── Tap trigger ────────────────────────────────────────────────────

        private void OnTriggerTapped()
        {
            float now = Time.realtimeSinceStartup;
            if (now - _lastTapTime > tapResetTime) _tapCount = 0;
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
            if (panelRoot != null) panelRoot.SetActive(true);
        }

        private void ClosePanel()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void ChangeLevel(int delta)
        {
            _selectedLevel = Mathf.Max(0, _selectedLevel + delta);
            RefreshLabel();
        }

        // ── Actions ────────────────────────────────────────────────────────

        private void OnGoClicked()
        {
            ClosePanel();

            if (gameBootstrap != null)
            {
                gameBootstrap.JumpToLevel(_selectedLevel);
            }
            else
            {
                PlayerPrefs.SetInt(PrefKey, _selectedLevel);
                PlayerPrefs.Save();
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                UnityEngine.SceneManagement.SceneManager.LoadScene(scene.name);
            }

            Debug.Log($"[DevLevelSelector] Jump → level index {_selectedLevel} (Level {_selectedLevel + 1})");
        }

        private void OnRestartClicked()
        {
            ClosePanel();

            // Restart = JumpToLevel з бягучым індэксам
            int current = PlayerPrefs.GetInt(PrefKey, 0);

            if (gameBootstrap != null)
            {
                gameBootstrap.JumpToLevel(current);
            }
            else
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                UnityEngine.SceneManagement.SceneManager.LoadScene(scene.name);
            }

            Debug.Log($"[DevLevelSelector] Restart → level index {current} (Level {current + 1})");
        }

        private void RefreshLabel()
        {
            if (currentLevelText != null)
                currentLevelText.text = $"Level {_selectedLevel + 1}";
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.L))
                OpenPanel();
        }
#endif
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;

namespace HexWords.UI
{
    /// <summary>
    /// Home screen: logo, coin balance, Play button, Settings, Daily Challenge entry.
    /// </summary>
    public class HomeScreenView : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private Text coinText;
        [SerializeField] private Button settingsButton;

        [Header("Play")]
        [SerializeField] private Button playButton;
        [SerializeField] private Text playButtonText; // "Play (Level X)"

        [Header("Daily Challenge")]
        [SerializeField] private Button dailyChallengeButton; // calendar icon

        [Header("Root")]
        [SerializeField] private GameObject root;

        // ── Events ─────────────────────────────────────────────────────────
        public event Action OnPlayClicked;
        public event Action OnSettingsClicked;
        public event Action OnDailyChallengeClicked;

        private void Awake()
        {
            if (playButton != null)
                playButton.onClick.AddListener(() => OnPlayClicked?.Invoke());

            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => OnSettingsClicked?.Invoke());

            if (dailyChallengeButton != null)
                dailyChallengeButton.onClick.AddListener(() => OnDailyChallengeClicked?.Invoke());
        }

        // ── Public API ─────────────────────────────────────────────────────

        public void Show()
        {
            SetRootVisible(true);
        }

        public void Hide()
        {
            SetRootVisible(false);
        }

        public void SetCurrentLevel(int levelNumber)
        {
            if (playButtonText != null)
                playButtonText.text = $"Play (Level {levelNumber})";
        }

        public void SetCoins(int amount)
        {
            if (coinText != null)
                coinText.text = amount.ToString();
        }

        private void SetRootVisible(bool visible)
        {
            if (root != null)
                root.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }
    }
}

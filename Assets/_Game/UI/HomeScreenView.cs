#define DOTWEEN

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if DOTWEEN
using DG.Tweening;
#endif

namespace HexWords.UI
{
    public class HomeScreenView : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private Button   settingsButton;

        [Header("Play")]
        [SerializeField] private Button   playButton;
        [SerializeField] private TMP_Text playButtonText;

        [Header("Progress")]
        [SerializeField] private Slider   progressBar;
        [SerializeField] private TMP_Text progressText; // e.g. "12 / 60 levels"

        [Header("Daily Challenge")]
        [SerializeField] private Button dailyChallengeButton;

        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("Progress Animation")]
        [SerializeField] private float         progressDuration = 0.6f;
        [SerializeField] private AnimationCurve progressCurve   = AnimationCurve.EaseInOut(0, 0, 1, 1);

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

        public void Show() => SetRootVisible(true);
        public void Hide() => SetRootVisible(false);

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

        /// <summary>Updates and animates the levels-completed progress bar.</summary>
        /// <param name="completed">Levels completed so far (0-based index = levels done).</param>
        /// <param name="total">Total levels in the catalog.</param>
        /// <param name="animate">Play fill animation (true when returning from level complete).</param>
        public void SetProgress(int completed, int total, bool animate = false)
        {
            if (progressBar == null) return;

            progressBar.minValue = 0;
            progressBar.maxValue = Mathf.Max(1, total);

            if (progressText != null)
                progressText.text = $"{completed} / {total}";

#if DOTWEEN
            DOTween.Kill(progressBar);
            if (animate)
            {
                DOTween.To(() => progressBar.value,
                           v  => progressBar.value = v,
                           completed,
                           progressDuration)
                       .SetEase(progressCurve)
                       .SetId(progressBar);
            }
            else
            {
                progressBar.value = completed;
            }
#else
            progressBar.value = completed;
#endif
        }

        // ── Internal ───────────────────────────────────────────────────────

        private void SetRootVisible(bool visible)
        {
            if (root != null) root.SetActive(visible);
            else gameObject.SetActive(visible);
        }
    }
}

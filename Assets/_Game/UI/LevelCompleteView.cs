using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HexWords.UI
{
    /// <summary>
    /// Win-screen popup shown after a level is completed.
    /// Tap the coin icon to trigger the reward animation, then the
    /// Next Level / Main Menu buttons become interactive.
    /// </summary>
    public class LevelCompleteView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("Status")]
        [SerializeField] private TMP_Text levelCompleteText; // "Level X completed!"

        [Header("Coin Reward")]
        [SerializeField] private Button coinRewardButton; // tap to collect
        [SerializeField] private TMP_Text coinRewardText;     // coin amount label
        [SerializeField] private RectTransform coinIcon;  // animated to header balance

        [Header("Actions")]
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private TMP_Text nextLevelButtonText; // "Next Level X"
        [SerializeField] private Button mainMenuButton;

        [Header("Ad Banner")]
        [SerializeField] private GameObject adBannerSlot; // bottom banner placeholder

        // ── Public events ──────────────────────────────────────────────────
        public event Action OnNextLevelClicked;
        public event Action OnMainMenuClicked;
        /// <summary>Fired when the coin-collect animation finishes; passes the reward amount.</summary>
        public event Action<int> OnCoinRewardCollected;

        private int _pendingCoinReward;
        private bool _rewardCollected;

        private void Awake()
        {
            if (coinRewardButton != null)
                coinRewardButton.onClick.AddListener(OnCoinIconTapped);

            if (nextLevelButton != null)
                nextLevelButton.onClick.AddListener(() => OnNextLevelClicked?.Invoke());

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(() => OnMainMenuClicked?.Invoke());
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>Shows the win screen for the completed level with a coin reward.</summary>
        public void Show(int levelNumber, int coinReward)
        {
            transform.SetAsLastSibling();
            _pendingCoinReward = coinReward;
            _rewardCollected   = false;

            if (levelCompleteText != null)
                levelCompleteText.text = $"Level {levelNumber} completed!";

            if (coinRewardText != null)
                coinRewardText.text = $"+{coinReward}";

            if (nextLevelButtonText != null)
                nextLevelButtonText.text = $"Next Level {levelNumber + 1}";

            // If no coin button assigned — unlock immediately; otherwise wait for tap
            bool hasCoinButton = coinRewardButton != null;
            SetActionButtonsInteractable(!hasCoinButton);

            if (hasCoinButton)
                coinRewardButton.interactable = true;

            SetRootVisible(true);
        }

        public void Hide()
        {
            SetRootVisible(false);
        }

        // ── Internal ───────────────────────────────────────────────────────

        private void OnCoinIconTapped()
        {
            if (_rewardCollected) return;
            _rewardCollected = true;

            if (coinRewardButton != null)
                coinRewardButton.interactable = false;

            StartCoroutine(CoinCollectRoutine());
        }

        private IEnumerator CoinCollectRoutine()
        {
            // Simple scale-up + fade-out animation on the coin icon
            if (coinIcon != null)
            {
                float t = 0f;
                Vector3 startScale = coinIcon.localScale;
                while (t < 0.3f)
                {
                    t += Time.deltaTime;
                    float progress = Mathf.Clamp01(t / 0.3f);
                    coinIcon.localScale = Vector3.Lerp(startScale, startScale * 1.4f, progress);
                    yield return null;
                }

                t = 0f;
                while (t < 0.25f)
                {
                    t += Time.deltaTime;
                    float progress = Mathf.Clamp01(t / 0.25f);
                    coinIcon.localScale = Vector3.Lerp(startScale * 1.4f, Vector3.zero, progress);
                    yield return null;
                }

                coinIcon.gameObject.SetActive(false);
            }
            else
            {
                yield return new WaitForSeconds(0.3f);
            }

            OnCoinRewardCollected?.Invoke(_pendingCoinReward);
            SetActionButtonsInteractable(true);
        }

        private void SetActionButtonsInteractable(bool interactable)
        {
            if (nextLevelButton != null) nextLevelButton.interactable = interactable;
            if (mainMenuButton != null)  mainMenuButton.interactable  = interactable;
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

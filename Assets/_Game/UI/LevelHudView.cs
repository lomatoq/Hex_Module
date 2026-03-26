using HexWords.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HexWords.UI
{
    public class LevelHudView : MonoBehaviour
    {
        // ── Header ─────────────────────────────────────────────────────────
        [Header("Header")]
        [SerializeField] private Text levelText;
        [SerializeField] private Text coinText;           // top-left balance
        [SerializeField] private Button settingsButton;   // top-right gear
        [SerializeField] private Button foundWordsButton; // book icon

        // ── Progress ───────────────────────────────────────────────────────
        [Header("Progress")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Slider progressBar;

        // ── Word feedback (last submitted word) ───────────────────────────
        [Header("Word Feedback")]
        [SerializeField] private Text lastWordText;
        [SerializeField] private FeedbackPalette feedbackPalette;

        // ── Word preview (floating above hex field during swipe) ───────────
        [Header("Word Preview")]
        [SerializeField] private GameObject wordPreviewRoot;
        [SerializeField] private Text wordPreviewText;
        [SerializeField] private GameObject scoreBadgeRoot;
        [SerializeField] private Text scoreBadgeText;

        // ── Booster – Hint ─────────────────────────────────────────────────
        [Header("Booster – Hint")]
        [SerializeField] private Button hintButton;
        [SerializeField] private Text hintChargeText;
        [SerializeField] private GameObject hintRvIcon;    // charges=0, RV available
        [SerializeField] private GameObject hintEmptyIcon; // charges=0, no RV

        // ── Events ─────────────────────────────────────────────────────────
        public event System.Action OnHintClicked;
        public event System.Action OnSettingsClicked;
        public event System.Action OnFoundWordsClicked;

        private void Awake()
        {
            if (hintButton != null)
                hintButton.onClick.AddListener(() => OnHintClicked?.Invoke());

            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => OnSettingsClicked?.Invoke());

            if (foundWordsButton != null)
                foundWordsButton.onClick.AddListener(() => OnFoundWordsClicked?.Invoke());

            if (wordPreviewRoot != null)
                wordPreviewRoot.SetActive(false);
        }

        // ── Header ─────────────────────────────────────────────────────────

        public void SetLevel(string levelId)
        {
            if (levelText != null)
                levelText.text = $"Level {levelId}";
        }

        public void SetCoins(int amount)
        {
            if (coinText != null)
                coinText.text = amount.ToString();
        }

        // ── Progress ───────────────────────────────────────────────────────

        public void SetScore(int current, int target)
        {
            if (scoreText != null)
                scoreText.text = $"{current}/{target}";

            if (progressBar != null)
            {
                progressBar.maxValue = target;
                progressBar.value    = current;
            }
        }

        // ── Word feedback ──────────────────────────────────────────────────

        public void SetLastWord(string text, bool accepted)
        {
            SetLastWord(text, accepted ? WordSubmitOutcome.TargetAccepted : WordSubmitOutcome.Rejected);
        }

        public void SetLastWord(string text, WordSubmitOutcome outcome)
        {
            if (lastWordText == null) return;
            lastWordText.text  = text;
            lastWordText.color = GetHudColor(outcome);
        }

        public void SetCurrentWord(string text)
        {
            if (lastWordText == null) return;
            lastWordText.text  = text;
            lastWordText.color = feedbackPalette != null
                ? feedbackPalette.hudCurrentWordColor
                : new Color(0.2f, 0.2f, 0.2f);
        }

        // ── Word preview ───────────────────────────────────────────────────

        public void ShowWordPreview(string word, int score, bool isValid)
        {
            if (wordPreviewRoot == null) return;
            wordPreviewRoot.SetActive(true);

            if (wordPreviewText != null)
                wordPreviewText.text = word;

            bool showBadge = isValid && score > 0;
            if (scoreBadgeRoot != null)
                scoreBadgeRoot.SetActive(showBadge);

            if (scoreBadgeText != null && showBadge)
                scoreBadgeText.text = $"+{score}";
        }

        public void HideWordPreview()
        {
            if (wordPreviewRoot != null)
                wordPreviewRoot.SetActive(false);
        }

        // ── Booster – Hint ─────────────────────────────────────────────────

        /// <summary>
        /// Updates the hint button visual state.
        /// charges > 0     → shows charge count.<br/>
        /// charges == 0, rvAvailable  → shows RV icon.<br/>
        /// charges == 0, !rvAvailable → shows empty icon, disables button.
        /// </summary>
        public void SetHintCharges(int charges, bool rvAvailable)
        {
            if (hintChargeText != null)
            {
                hintChargeText.gameObject.SetActive(charges > 0);
                hintChargeText.text = charges.ToString();
            }

            if (hintRvIcon != null)
                hintRvIcon.SetActive(charges == 0 && rvAvailable);

            if (hintEmptyIcon != null)
                hintEmptyIcon.SetActive(charges == 0 && !rvAvailable);

            if (hintButton != null)
                hintButton.interactable = charges > 0 || rvAvailable;
        }

        // ── Colour helper ──────────────────────────────────────────────────

        private Color GetHudColor(WordSubmitOutcome outcome)
        {
            if (feedbackPalette == null)
            {
                return outcome switch
                {
                    WordSubmitOutcome.TargetAccepted  => new Color(0.2f, 0.5f, 0.2f),
                    WordSubmitOutcome.BonusAccepted   => new Color(0.1f, 0.55f, 0.65f),
                    WordSubmitOutcome.AlreadyAccepted => new Color(0.2f, 0.35f, 0.8f),
                    _                                 => new Color(0.7f, 0.2f, 0.2f)
                };
            }

            return outcome switch
            {
                WordSubmitOutcome.TargetAccepted  => feedbackPalette.hudTargetAcceptedColor,
                WordSubmitOutcome.BonusAccepted   => feedbackPalette.hudBonusAcceptedColor,
                WordSubmitOutcome.AlreadyAccepted => feedbackPalette.hudAlreadyAcceptedColor,
                _                                 => feedbackPalette.hudRejectedColor
            };
        }
    }
}

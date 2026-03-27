using HexWords.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HexWords.UI
{
    public class LevelHudView : MonoBehaviour
    {
        // ── Header ─────────────────────────────────────────────────────────
        [Header("Header")]
        [SerializeField] private Text   levelText;
        [SerializeField] private Text   coinText;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button foundWordsButton;

        // ── Progress ───────────────────────────────────────────────────────
        [Header("Progress")]
        [SerializeField] private Text   scoreText;
        [SerializeField] private Slider progressBar;

        // ── Word display ───────────────────────────────────────────────────
        // Адзін тэкставы поле для ўсяго: бягучае слова падчас свайпу
        // і апошняе слова пасля сабміту. Word Preview выкарыстоўвае яго ж.
        [Header("Word Display")]
        [SerializeField] private Text          lastWordText;
        [SerializeField] private FeedbackPalette feedbackPalette;

        // Апцыянальны бэдж з балімі ("+7") — з'яўляецца над словам калі слова валіднае.
        // Калі не прысвоены — проста не паказваецца.
        [Header("Score Badge (optional)")]
        [SerializeField] private GameObject scoreBadgeRoot;
        [SerializeField] private Text       scoreBadgeText;

        // ── Booster – Hint ─────────────────────────────────────────────────
        [Header("Booster – Hint")]
        [SerializeField] private Button     hintButton;
        [SerializeField] private Text       hintChargeText;
        [SerializeField] private GameObject hintRvIcon;
        [SerializeField] private GameObject hintEmptyIcon;

        // ── Events ─────────────────────────────────────────────────────────
        public event System.Action OnHintClicked;
        public event System.Action OnSettingsClicked;
        public event System.Action OnFoundWordsClicked;

        private void Awake()
        {
            if (hintButton      != null) hintButton.onClick.AddListener(() => OnHintClicked?.Invoke());
            if (settingsButton  != null) settingsButton.onClick.AddListener(() => OnSettingsClicked?.Invoke());
            if (foundWordsButton != null) foundWordsButton.onClick.AddListener(() => OnFoundWordsClicked?.Invoke());

            if (scoreBadgeRoot != null) scoreBadgeRoot.SetActive(false);
        }

        // ── Header ─────────────────────────────────────────────────────────

        public void SetLevel(string levelId)
        {
            if (levelText != null) levelText.text = $"Level {levelId}";
        }

        public void SetCoins(int amount)
        {
            if (coinText != null) coinText.text = amount.ToString();
        }

        // ── Progress ───────────────────────────────────────────────────────

        public void SetScore(int current, int target)
        {
            if (scoreText != null) scoreText.text = $"{current}/{target}";

            if (progressBar != null)
            {
                progressBar.maxValue = target;
                progressBar.value    = current;
            }
        }

        // ── Word display ───────────────────────────────────────────────────

        /// <summary>Паказвае слова падчас свайпу (бягучае).</summary>
        public void SetCurrentWord(string text)
        {
            if (lastWordText == null) return;
            lastWordText.text  = text;
            lastWordText.color = feedbackPalette != null
                ? feedbackPalette.hudCurrentWordColor
                : new Color(0.6f, 0.6f, 0.6f);

            // Схаваць бэдж пакуль яшчэ не валідавана
            if (scoreBadgeRoot != null) scoreBadgeRoot.SetActive(false);
        }

        /// <summary>Паказвае слова падчас свайпу + бал калі слова валіднае.</summary>
        public void ShowWordPreview(string word, int score, bool isValid)
        {
            if (lastWordText == null) return;
            lastWordText.text  = word;
            lastWordText.color = isValid
                ? (feedbackPalette != null ? feedbackPalette.hudCurrentWordColor : new Color(0.2f, 0.8f, 0.2f))
                : (feedbackPalette != null ? feedbackPalette.hudRejectedColor    : new Color(0.6f, 0.6f, 0.6f));

            bool showBadge = isValid && score > 0;
            if (scoreBadgeRoot != null) scoreBadgeRoot.SetActive(showBadge);
            if (scoreBadgeText != null && showBadge) scoreBadgeText.text = $"+{score}";
        }

        /// <summary>Хавае прэвью (схавае бэдж, зачышчае тэкст).</summary>
        public void HideWordPreview()
        {
            if (scoreBadgeRoot != null) scoreBadgeRoot.SetActive(false);
            if (lastWordText   != null) lastWordText.text = string.Empty;
        }

        /// <summary>Паказвае выніковае слова пасля сабміту.</summary>
        public void SetLastWord(string text, bool accepted)
        {
            SetLastWord(text, accepted ? WordSubmitOutcome.TargetAccepted : WordSubmitOutcome.Rejected);
        }

        public void SetLastWord(string text, WordSubmitOutcome outcome)
        {
            if (lastWordText == null) return;
            if (scoreBadgeRoot != null) scoreBadgeRoot.SetActive(false);
            lastWordText.text  = text;
            lastWordText.color = GetHudColor(outcome);
        }

        // ── Hint ───────────────────────────────────────────────────────────

        public void SetHintCharges(int charges, bool rvAvailable)
        {
            if (hintChargeText != null)
            {
                hintChargeText.gameObject.SetActive(charges > 0);
                hintChargeText.text = charges.ToString();
            }

            if (hintRvIcon   != null) hintRvIcon.SetActive(charges == 0 && rvAvailable);
            if (hintEmptyIcon != null) hintEmptyIcon.SetActive(charges == 0 && !rvAvailable);
            if (hintButton   != null) hintButton.interactable = charges > 0 || rvAvailable;
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

// #define DOTWEEN — uncomment if DOTween is installed (already enabled if you followed setup)
#define DOTWEEN

using HexWords.Core;
using UnityEngine;
using UnityEngine.UI;
#if DOTWEEN
using DG.Tweening;
#endif

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
        [Header("Word Display")]
        [SerializeField] private Text            lastWordText;
        [SerializeField] private FeedbackPalette feedbackPalette;

        // Bubble that resizes with the word length.
        // Assign the RectTransform of the background Image behind lastWordText.
        // It must use a 9-sliced sprite so it stretches cleanly.
        [SerializeField] private RectTransform wordBubble;
        [SerializeField] private float         bubbleMinWidth   = 80f;   // width for 1 letter (square-ish)
        [SerializeField] private float         bubbleLetterWidth = 28f;  // extra px per letter
        [SerializeField] private float         bubbleResizeDuration = 0.12f;

        // ── Streak ─────────────────────────────────────────────────────────
        // Паказваецца калі серыя >= 2. Хаваецца пры серыі 0 або 1.
        [Header("Streak (optional)")]
        [SerializeField] private GameObject streakRoot;
        [SerializeField] private Text       streakText;

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
            if (hintButton       != null) hintButton.onClick.AddListener(() => OnHintClicked?.Invoke());
            if (settingsButton   != null) settingsButton.onClick.AddListener(() => OnSettingsClicked?.Invoke());
            if (foundWordsButton != null) foundWordsButton.onClick.AddListener(() => OnFoundWordsClicked?.Invoke());

            if (scoreBadgeRoot != null) scoreBadgeRoot.SetActive(false);
            if (streakRoot     != null) streakRoot.SetActive(false);
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
            if (scoreBadgeRoot != null) scoreBadgeRoot.SetActive(false);
            ResizeBubble(text);
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
            ResizeBubble(word);
        }

        /// <summary>Хавае прэвью (схавае бэдж, зачышчае тэкст).</summary>
        public void HideWordPreview()
        {
            if (scoreBadgeRoot != null) scoreBadgeRoot.SetActive(false);
            if (lastWordText   != null) lastWordText.text = string.Empty;
            ResizeBubble(string.Empty);
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
            ResizeBubble(text);
        }

        // ── Streak ─────────────────────────────────────────────────────────

        /// <summary>Shows the streak badge when streak >= 2, hides it otherwise.</summary>
        public void SetStreak(int streak)
        {
            bool show = streak >= 2;
            if (streakRoot != null) streakRoot.SetActive(show);
            if (streakText != null && show) streakText.text = $"x{streak}";
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

        // ── Bubble resize ──────────────────────────────────────────────────

        private void ResizeBubble(string word)
        {
            if (wordBubble == null) return;
            var letters     = string.IsNullOrEmpty(word) ? 0 : word.Length;
            var currentSize = wordBubble.sizeDelta;
            var height      = currentSize.y;

            // 0 letters → minWidth (hidden state), 1 letter → square (width = height),
            // each extra letter adds bubbleLetterWidth.
            var targetWidth = letters == 0
                ? bubbleMinWidth
                : height + bubbleLetterWidth * (letters - 1);

#if DOTWEEN
            DOTween.Kill(wordBubble);
            wordBubble.DOSizeDelta(new Vector2(targetWidth, height), bubbleResizeDuration)
                      .SetEase(Ease.OutBack)
                      .SetId(wordBubble);
#else
            wordBubble.sizeDelta = new Vector2(targetWidth, height);
#endif
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

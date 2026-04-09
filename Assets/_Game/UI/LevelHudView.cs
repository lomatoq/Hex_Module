// #define DOTWEEN — uncomment if DOTween is installed (already enabled if you followed setup)
#define DOTWEEN

using HexWords.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
        // wordBubble       — the RectTransform that gets resized (parent of both layers)
        // bubbleCanvasGroup — controls alpha (invisible when no word selected)
        // bubbleColorImage  — the bottom/stroke Image whose color changes with word state
        [SerializeField] private RectTransform wordBubble;
        [SerializeField] private CanvasGroup   wordBubbleCanvasGroup;
        [SerializeField] private Image         bubbleColorImage;
        [SerializeField] private Color         bubbleNeutralColor   = new Color(0.85f, 0.85f, 0.85f, 1f);
        [SerializeField] private float         bubblePadding        = 32f;
        [SerializeField] private float         bubbleResizeDuration = 0.12f;
        [SerializeField] private float         bubbleColorDuration  = 0.10f;

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

        // ── Bubble animation ───────────────────────────────────────────────
        [Header("Bubble Animation – Bounce (accepted)")]
        [SerializeField] private float          bubbleBounceScale       = 0.15f;
        [SerializeField] private float          bubbleBounceDuration    = 0.28f;
        [SerializeField] private AnimationCurve bubbleBounceScaleCurve  = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float          bubbleAcceptedDismissDelay = 0.35f;

        [Header("Bubble Animation – Dismiss (fly up)")]
        [SerializeField] private float          bubbleDismissRise       = 60f;
        [SerializeField] private float          bubbleDismissDuration   = 0.25f;
        [SerializeField] private AnimationCurve bubbleDismissMoveCurve  = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve bubbleDismissAlphaCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // ── Events ─────────────────────────────────────────────────────────
        public event System.Action OnHintClicked;
        public event System.Action OnSettingsClicked;
        public event System.Action OnFoundWordsClicked;

        private Vector2 _bubbleBasePos;

        private void Awake()
        {
            if (hintButton       != null) hintButton.onClick.AddListener(() => OnHintClicked?.Invoke());
            if (settingsButton   != null) settingsButton.onClick.AddListener(() => OnSettingsClicked?.Invoke());
            if (foundWordsButton != null) foundWordsButton.onClick.AddListener(() => OnFoundWordsClicked?.Invoke());

            if (scoreBadgeRoot != null) scoreBadgeRoot.SetActive(false);
            if (streakRoot     != null) streakRoot.SetActive(false);

            // Start fully invisible — becomes visible when first letter is selected
            if (wordBubbleCanvasGroup != null) wordBubbleCanvasGroup.alpha = 0f;
            if (wordBubble            != null)
            {
                var sd = wordBubble.sizeDelta;
                wordBubble.sizeDelta = new Vector2(0f, sd.y);
                _bubbleBasePos = wordBubble.anchoredPosition;
            }
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
            lastWordText.color = Color.black;
            if (scoreBadgeRoot != null) scoreBadgeRoot.SetActive(false);
            SetBubbleColor(bubbleNeutralColor);
            ShowBubble(text);
        }

        /// <summary>Паказвае слова падчас свайпу + бал калі слова валіднае.</summary>
        public void ShowWordPreview(string word, int score, bool isValid)
        {
            if (lastWordText == null) return;
            lastWordText.text  = word;
            lastWordText.color = Color.black;

            var bubbleCol = isValid
                ? (feedbackPalette != null ? feedbackPalette.hudCurrentWordColor : new Color(0.2f, 0.8f, 0.2f))
                : bubbleNeutralColor;
            SetBubbleColor(bubbleCol);

            bool showBadge = isValid && score > 0;
            if (scoreBadgeRoot != null) scoreBadgeRoot.SetActive(showBadge);
            if (scoreBadgeText != null && showBadge) scoreBadgeText.text = $"+{score}";
            ShowBubble(word);
        }

        /// <summary>Хавае прэвью анімацыяй вылету ўверх.</summary>
        public void HideWordPreview()
        {
            if (scoreBadgeRoot != null) scoreBadgeRoot.SetActive(false);
            PlayBubbleDismiss();
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
            lastWordText.color = Color.black;
            SetBubbleColor(GetHudColor(outcome));
            ShowBubble(text);
        }

        // ── Bubble animations ──────────────────────────────────────────────

        /// <summary>Bounces the bubble container, then flies it up after a delay.</summary>
        public void PlayBubbleAccepted()
        {
            if (wordBubble == null) return;
#if DOTWEEN
            int id = wordBubble.GetInstanceID() + 1;
            DOTween.Kill(id);
            wordBubble.transform.localScale = Vector3.one;

            DOTween.Sequence().SetId(id)
                .Append(wordBubble.transform
                    .DOScale(1f + bubbleBounceScale, bubbleBounceDuration * 0.45f)
                    .SetEase(bubbleBounceScaleCurve))
                .Append(wordBubble.transform
                    .DOScale(1f, bubbleBounceDuration * 0.55f)
                    .SetEase(bubbleBounceScaleCurve))
                .AppendInterval(bubbleAcceptedDismissDelay)
                .AppendCallback(PlayBubbleDismiss);
#endif
        }

        /// <summary>Flies the bubble upward and fades it out — call when swipe released with no valid word.</summary>
        public void PlayBubbleDismiss()
        {
            if (lastWordText != null) lastWordText.text = string.Empty;
#if DOTWEEN
            if (wordBubble == null) return;
            int tweenId = wordBubble.GetInstanceID() + 2;

            // Kill all competing bubble tweens
            DOTween.Kill(wordBubble.GetInstanceID() + 1); // bounce+dismiss sequence
            DOTween.Kill(tweenId);
            DOTween.Kill(wordBubble);          // resize tween
            DOTween.Kill(bubbleColorImage);    // color tween

            // Snap scale back and color to white before flying up
            wordBubble.transform.localScale = Vector3.one;
            if (bubbleColorImage != null) bubbleColorImage.color = Color.white;

            var cg      = wordBubbleCanvasGroup;
            var rt      = wordBubble;
            var basePos = _bubbleBasePos;

            var seq = DOTween.Sequence().SetId(tweenId);
            seq.Append(rt.DOAnchorPosY(basePos.y + bubbleDismissRise, bubbleDismissDuration)
                         .SetEase(bubbleDismissMoveCurve));
            if (cg != null)
                seq.Join(cg.DOFade(0f, bubbleDismissDuration)
                           .SetEase(bubbleDismissAlphaCurve));
            seq.AppendCallback(() =>
            {
                rt.anchoredPosition = basePos;
                var sd = rt.sizeDelta; rt.sizeDelta = new Vector2(0f, sd.y);
                if (cg != null) cg.alpha = 0f;
            });
#else
            if (bubbleColorImage != null) bubbleColorImage.color = bubbleNeutralColor;
            if (wordBubbleCanvasGroup != null) wordBubbleCanvasGroup.alpha = 0f;
            if (wordBubble != null)
            {
                wordBubble.anchoredPosition = _bubbleBasePos;
                var sd = wordBubble.sizeDelta;
                wordBubble.sizeDelta = new Vector2(0f, sd.y);
            }
#endif
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

        // ── Bubble visibility + resize ─────────────────────────────────────

        private void ShowBubble(string word)
        {
            if (wordBubble == null) return;
#if DOTWEEN
            // Kill all pending bubble animations, snap back to base state
            DOTween.Kill(wordBubble.GetInstanceID() + 1); // bounce sequence
            DOTween.Kill(wordBubble.GetInstanceID() + 2); // dismiss sequence
            wordBubble.transform.localScale = Vector3.one;
            wordBubble.anchoredPosition = _bubbleBasePos;
#endif
            ResizeBubble(word);

            bool hasContent = !string.IsNullOrEmpty(word);
            if (wordBubbleCanvasGroup != null)
            {
#if DOTWEEN
                DOTween.Kill(wordBubbleCanvasGroup);
                // Snap alpha: visible immediately when content appears, hidden when empty
                wordBubbleCanvasGroup.alpha = hasContent ? 1f : 0f;
#else
                wordBubbleCanvasGroup.alpha = hasContent ? 1f : 0f;
#endif
            }
        }

        private void SetBubbleColor(Color color)
        {
            if (bubbleColorImage == null) return;
#if DOTWEEN
            DOTween.Kill(bubbleColorImage);
            bubbleColorImage.DOColor(color, bubbleColorDuration).SetId(bubbleColorImage);
#else
            bubbleColorImage.color = color;
#endif
        }

        private void ResizeBubble(string word)
        {
            if (wordBubble == null || lastWordText == null) return;
            var height = wordBubble.sizeDelta.y;

            float targetWidth;
            if (string.IsNullOrEmpty(word))
            {
                targetWidth = 0f;
            }
            else
            {
                targetWidth = Mathf.Max(height, lastWordText.preferredWidth + bubblePadding);
            }

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

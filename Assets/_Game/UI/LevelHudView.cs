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
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private Button   settingsButton;
        [SerializeField] private Button   foundWordsButton;

        // ── Progress ───────────────────────────────────────────────────────
        [Header("Progress")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private Slider   progressBar;
        [SerializeField] private Image    progressBarFill; // Fill image inside the Slider — for color sync

        [Header("Progress Animation")]
        [SerializeField] private float          scoreFillDuration      = 0.5f;
        [SerializeField] private AnimationCurve scoreFillCurve         = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float          scoreBarBounceScale    = 1.06f;
        [SerializeField] private float          scoreBarBounceDuration = 0.5f;
        // Curve shape: 0→0 at t=0, peak at ~t=0.25, back to 0 at t=1
        // Inspector default set in Reset() below
        [SerializeField] private AnimationCurve scoreBarBounceCurve    = AnimationCurve.EaseInOut(0, 0, 1, 0);
        [SerializeField] private float          scoreBarAnticipation   = 0.07f; // sec before drop arrives

        // ── Word display ───────────────────────────────────────────────────
        [Header("Word Display")]
        [SerializeField] private TMP_Text      lastWordText;
        [SerializeField] private FeedbackPalette feedbackPalette;

        // wordBubble       — the RectTransform that gets resized (parent of all bubble layers)
        // wordBubbleCanvasGroup — controls alpha for the WHOLE bubble (must be on wordBubble or a parent that covers everything)
        // bubbleColorImage  — background Image whose color changes with word state
        [SerializeField] private RectTransform wordBubble;
        [SerializeField] private CanvasGroup   wordBubbleCanvasGroup;
        [SerializeField] private Image         bubbleColorImage;
        [SerializeField] private Color         bubbleNeutralColor   = new Color(0.85f, 0.85f, 0.85f, 1f);
        [SerializeField] private float         bubblePadding        = 32f;
        [SerializeField] private float         bubbleResizeDuration = 0.12f;
        [SerializeField] private float         bubbleColorDuration  = 0.10f;

        // ── Streak ─────────────────────────────────────────────────────────
        [Header("Streak (optional)")]
        [SerializeField] private GameObject streakRoot;
        [SerializeField] private TMP_Text   streakText;

        // ── Score Badge ────────────────────────────────────────────────────
        // Must be a child of wordBubble so it fades with the bubble CanvasGroup.
        [Header("Score Badge (optional)")]
        [SerializeField] private GameObject scoreBadgeRoot;
        [SerializeField] private TMP_Text   scoreBadgeText;
        [SerializeField] private Image      scoreBadgeImage; // background Image — receives accent color

        // ── Score Drop ─────────────────────────────────────────────────────
        [Header("Score Drop")]
        [SerializeField] private RectTransform  scoreDropTemplate;                              // inactive circle Image on HUD canvas
        [SerializeField] private RectTransform  scoreDropTarget;                                // destination RT (progressBar area)
        [SerializeField] private Vector2        scoreDropControlOffset = new Vector2(0f, 180f); // bezier arc control offset from midpoint
        [SerializeField] private float          scoreDropDuration      = 0.45f;
        [SerializeField] private AnimationCurve scoreDropCurve         = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private int            scoreDropCount         = 9;
        [SerializeField] private float          scoreDropSpacing       = 0.035f;
        [SerializeField] private float          scoreDropTailScale     = 0.18f;

        /// <summary>Head arrival time — used as delay for SetScore so bounce syncs with drop.</summary>
        public float ScoreDropDuration => (scoreDropTemplate != null && scoreDropTarget != null) ? scoreDropDuration : 0f;

        // ── Booster – Hint ─────────────────────────────────────────────────
        [Header("Booster – Hint")]
        [SerializeField] private Button     hintButton;
        [SerializeField] private TMP_Text   hintChargeText;
        [SerializeField] private GameObject hintRvIcon;
        [SerializeField] private GameObject hintEmptyIcon;

        // ── Bubble animation ───────────────────────────────────────────────
        [Header("Bubble Animation – Bounce (accepted)")]
        [SerializeField] private float          bubbleBounceScale          = 0.15f;
        [SerializeField] private float          bubbleBounceDuration       = 0.28f;
        [SerializeField] private AnimationCurve bubbleBounceScaleCurve     = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float          bubbleAcceptedDismissDelay = 0.35f;

        [Header("Bubble Animation – Dismiss (fly up)")]
        [SerializeField] private float          bubbleDismissRise       = 60f;
        [SerializeField] private float          bubbleDismissDuration   = 0.25f;
        [SerializeField] private AnimationCurve bubbleDismissMoveCurve  = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve bubbleDismissAlphaCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // ── Accent color (shared across badge, drops, bar fill) ────────────
        [Header("Accent Color")]
        [SerializeField] private float accentColorDuration = 0.15f;

        private Color _currentAccentColor;
        private int   _lastWordScore;

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

            // Progress bar is display-only — block user interaction
            if (progressBar != null) progressBar.interactable = false;

            if (scoreBadgeRoot != null) scoreBadgeRoot.SetActive(false);
            if (streakRoot     != null) streakRoot.SetActive(false);

            _currentAccentColor = bubbleNeutralColor;

            // Start fully invisible — becomes visible when first letter is selected
            if (wordBubbleCanvasGroup != null) wordBubbleCanvasGroup.alpha = 0f;
            if (wordBubble != null)
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

        public void SetScore(int current, int target, bool animate = false, float delay = 0f)
        {
            if (progressBar == null) return;
            progressBar.maxValue = target;

#if DOTWEEN
            if (animate && current > progressBar.value)
            {
                DOTween.Kill(progressBar);
                var barRT    = progressBar.GetComponent<RectTransform>();
                var scoreTRT = scoreText != null ? scoreText.GetComponent<RectTransform>() : null;

                // Fill bar (delayed so it starts when drop arrives)
                DOTween.To(() => progressBar.value,
                           v  => progressBar.value = v,
                           current,
                           scoreFillDuration)
                       .SetEase(scoreFillCurve)
                       .SetDelay(delay)
                       .SetId(progressBar);

                // Text update at drop arrival
                var seq = DOTween.Sequence();
                if (delay > 0f) seq.AppendInterval(delay);
                seq.AppendCallback(() =>
                {
                    if (scoreText != null) scoreText.text = $"{current}/{target}";
                });

                // Bar + score label bounce: starts slightly before drop arrives (anticipation)
                float bounceDelay = Mathf.Max(0f, delay - scoreBarAnticipation);
                if (barRT != null)
                {
                    DOTween.Kill(barRT);
                    float bScale = scoreBarBounceScale;
                    float bDur   = scoreBarBounceDuration;
                    var   bCurve = scoreBarBounceCurve;
                    float bT     = 0f;
                    DOTween.To(() => bT, v =>
                    {
                        bT = v;
                        float factor = bCurve.Evaluate(bT); // curve: 0→peak→0
                        barRT.localScale = Vector3.one * Mathf.LerpUnclamped(1f, bScale, factor);
                    }, 1f, bDur)
                    .SetDelay(bounceDelay)
                    .SetId(barRT)
                    .OnComplete(() => barRT.localScale = Vector3.one);
                }
                if (scoreTRT != null)
                {
                    DOTween.Kill(scoreTRT);
                    float bScale = scoreBarBounceScale;
                    float bDur   = scoreBarBounceDuration;
                    var   bCurve = scoreBarBounceCurve;
                    float bT     = 0f;
                    DOTween.To(() => bT, v =>
                    {
                        bT = v;
                        float factor = bCurve.Evaluate(bT);
                        scoreTRT.localScale = Vector3.one * Mathf.LerpUnclamped(1f, bScale, factor);
                    }, 1f, bDur)
                    .SetDelay(bounceDelay)
                    .SetId(scoreTRT)
                    .OnComplete(() => scoreTRT.localScale = Vector3.one);
                }
            }
            else
            {
                DOTween.Kill(progressBar);
                progressBar.value = current;
                if (scoreText != null) scoreText.text = $"{current}/{target}";
            }
#else
            progressBar.value = current;
            if (scoreText != null) scoreText.text = $"{current}/{target}";
#endif
        }

        // ── Word display ───────────────────────────────────────────────────

        /// <summary>Паказвае слова падчас свайпу (нейтральны стан).</summary>
        public void SetCurrentWord(string text)
        {
            if (lastWordText == null) return;
            lastWordText.text  = text;
            lastWordText.color = BubbleTextColor();
            if (scoreBadgeRoot != null) scoreBadgeRoot.SetActive(false);
            ApplyAccentColor(bubbleNeutralColor);
            ShowBubble(text);
        }

        /// <summary>Паказвае слова падчас свайпу + бал калі валіднае.</summary>
        public void ShowWordPreview(string word, int score, bool isValid)
        {
            if (lastWordText == null) return;
            lastWordText.text  = word;
            lastWordText.color = BubbleTextColor();
            _lastWordScore     = isValid ? score : 0;

            var col = isValid
                ? (feedbackPalette != null ? feedbackPalette.hudCurrentWordColor : new Color(0.2f, 0.8f, 0.2f))
                : bubbleNeutralColor;
            ApplyAccentColor(col);

            bool showBadge = isValid && score > 0;
            if (scoreBadgeRoot != null) scoreBadgeRoot.SetActive(showBadge);
            if (scoreBadgeText != null && showBadge) scoreBadgeText.text = $"{score}";
            ShowBubble(word);
        }

        /// <summary>Хавае прэвью — усё фэйдзіцца разам праз CanvasGroup.</summary>
        public void HideWordPreview()
        {
            // Don't SetActive(false) badge here — PlayBubbleDismiss fades everything
            // together via CanvasGroup and cleans up in OnComplete.
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
            lastWordText.color = outcome == WordSubmitOutcome.AlreadyAccepted
                ? BubbleTextColorAlreadyFound()
                : BubbleTextColor();
            ApplyAccentColor(GetHudColor(outcome));
            ShowBubble(text);
        }

        // ── Bubble animations ──────────────────────────────────────────────

        /// <summary>Bounces the bubble, optionally fires score drop, then dismisses after delay.</summary>
        public void PlayBubbleAccepted(bool withDrop = true)
        {
            if (withDrop && _lastWordScore > 0) PlayScoreDrop(); // only when word actually gives points

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

        /// <summary>
        /// Flies the bubble upward and fades ALL its contents (text, badge, background)
        /// together via CanvasGroup. Text and badge are cleaned up only after the animation.
        /// </summary>
        public void PlayBubbleDismiss()
        {
#if DOTWEEN
            if (wordBubble == null) return;
            int tweenId = wordBubble.GetInstanceID() + 2;

            // Kill all competing bubble tweens
            DOTween.Kill(wordBubble.GetInstanceID() + 1);
            DOTween.Kill(tweenId);
            DOTween.Kill(wordBubble);
            DOTween.Kill(bubbleColorImage);

            // Snap scale only — don't touch color or text yet
            wordBubble.transform.localScale = Vector3.one;

            var cg      = wordBubbleCanvasGroup;
            var rt      = wordBubble;
            var basePos = _bubbleBasePos;

            var seq = DOTween.Sequence().SetId(tweenId);
            seq.Append(rt.DOAnchorPosY(basePos.y + bubbleDismissRise, bubbleDismissDuration)
                         .SetEase(bubbleDismissMoveCurve));
            if (cg != null)
                seq.Join(cg.DOFade(0f, bubbleDismissDuration)
                           .SetEase(bubbleDismissAlphaCurve));

            // After fade: reset position, clear text, hide badge
            seq.AppendCallback(() =>
            {
                rt.anchoredPosition = basePos;
                var sd = rt.sizeDelta;
                rt.sizeDelta = new Vector2(0f, sd.y);
                if (cg != null) cg.alpha = 0f;
                if (lastWordText  != null) lastWordText.text = string.Empty;
                if (scoreBadgeRoot != null) scoreBadgeRoot.SetActive(false);
            });
#else
            if (bubbleColorImage    != null) bubbleColorImage.color = bubbleNeutralColor;
            if (wordBubbleCanvasGroup != null) wordBubbleCanvasGroup.alpha = 0f;
            if (lastWordText        != null) lastWordText.text = string.Empty;
            if (scoreBadgeRoot      != null) scoreBadgeRoot.SetActive(false);
            if (wordBubble != null)
            {
                wordBubble.anchoredPosition = _bubbleBasePos;
                var sd = wordBubble.sizeDelta;
                wordBubble.sizeDelta = new Vector2(0f, sd.y);
            }
#endif
        }

        /// <summary>
        /// Launches a chain of circles along a quadratic bezier arc, each tinted
        /// in the current accent color. Head = full size, tail = scoreDropTailScale.
        /// </summary>
        public void PlayScoreDrop()
        {
#if DOTWEEN
            if (scoreDropTemplate == null || scoreBadgeRoot == null || scoreDropTarget == null) return;

            var parentRT = scoreDropTemplate.parent as RectTransform;
            var cam      = GetComponentInParent<Canvas>()?.worldCamera;

            Vector2 p0 = ToCanvasLocal(parentRT, scoreBadgeRoot.transform.position, cam);
            Vector2 p2 = ToCanvasLocal(parentRT, scoreDropTarget.position, cam);
            Vector2 p1 = (p0 + p2) * 0.5f + scoreDropControlOffset;

            int   count       = Mathf.Max(1, scoreDropCount);
            Color accentColor = _currentAccentColor;

            for (int i = 0; i < count; i++)
            {
                float delay = i * scoreDropSpacing;
                float scale = Mathf.Lerp(1f, scoreDropTailScale, count > 1 ? (float)i / (count - 1) : 0f);

                var dropGO = Instantiate(scoreDropTemplate.gameObject, scoreDropTemplate.parent);
                var drop   = dropGO.GetComponent<RectTransform>();
                drop.gameObject.SetActive(true);
                drop.anchoredPosition = p0;
                drop.localScale       = Vector3.one * scale;

                // Tint drop to match accent color
                var dropImg = dropGO.GetComponent<Image>();
                if (dropImg != null) dropImg.color = accentColor;

                float t = 0f;
                DOTween.To(() => t, v =>
                {
                    t = v;
                    float mt = 1f - t;
                    drop.anchoredPosition = mt * mt * p0
                                          + 2f * mt * t * p1
                                          + t * t * p2;
                }, 1f, scoreDropDuration)
                .SetEase(scoreDropCurve)
                .SetDelay(delay)
                .OnComplete(() => Destroy(drop.gameObject));
            }
#endif
        }

        private static Vector2 ToCanvasLocal(RectTransform parent, Vector3 worldPos, Camera cam)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, cam, out var local);
            return local;
        }

        // ── Streak ─────────────────────────────────────────────────────────

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
            if (hintRvIcon    != null) hintRvIcon.SetActive(charges == 0 && rvAvailable);
            if (hintEmptyIcon != null) hintEmptyIcon.SetActive(charges == 0 && !rvAvailable);
            if (hintButton    != null) hintButton.interactable = charges > 0 || rvAvailable;
        }

        // ── Bubble visibility + resize ─────────────────────────────────────

        private void ShowBubble(string word)
        {
            if (wordBubble == null) return;
#if DOTWEEN
            DOTween.Kill(wordBubble.GetInstanceID() + 1);
            DOTween.Kill(wordBubble.GetInstanceID() + 2);
            wordBubble.transform.localScale = Vector3.one;
            wordBubble.anchoredPosition = _bubbleBasePos;
#endif
            ResizeBubble(word);

            if (wordBubbleCanvasGroup != null)
            {
                bool hasContent = !string.IsNullOrEmpty(word);
#if DOTWEEN
                DOTween.Kill(wordBubbleCanvasGroup);
                wordBubbleCanvasGroup.alpha = hasContent ? 1f : 0f;
#else
                wordBubbleCanvasGroup.alpha = hasContent ? 1f : 0f;
#endif
            }
        }

        /// <summary>
        /// Sets accent color: animates bubble background, score badge image.
        /// Drops and progress bar fill receive the color separately at their own moments.
        /// </summary>
        private void ApplyAccentColor(Color color)
        {
            _currentAccentColor = color;

#if DOTWEEN
            if (bubbleColorImage != null)
            {
                DOTween.Kill(bubbleColorImage);
                bubbleColorImage.DOColor(color, bubbleColorDuration).SetId(bubbleColorImage);
            }
            if (scoreBadgeImage != null)
            {
                DOTween.Kill(scoreBadgeImage);
                scoreBadgeImage.DOColor(color, accentColorDuration).SetId(scoreBadgeImage);
            }
#else
            if (bubbleColorImage != null) bubbleColorImage.color = color;
            if (scoreBadgeImage  != null) scoreBadgeImage.color  = color;
#endif
        }

        private void ResizeBubble(string word)
        {
            if (wordBubble == null || lastWordText == null) return;
            var height = wordBubble.sizeDelta.y;

            float targetWidth = string.IsNullOrEmpty(word)
                ? 0f
                : Mathf.Max(height, lastWordText.preferredWidth + bubblePadding);

#if DOTWEEN
            DOTween.Kill(wordBubble);
            wordBubble.DOSizeDelta(new Vector2(targetWidth, height), bubbleResizeDuration)
                      .SetEase(Ease.OutBack)
                      .SetId(wordBubble);
#else
            wordBubble.sizeDelta = new Vector2(targetWidth, height);
#endif
        }

        // ── Colour helpers ─────────────────────────────────────────────────

        private Color BubbleTextColor()
            => feedbackPalette != null ? feedbackPalette.hudBubbleTextDefault : Color.black;

        private Color BubbleTextColorAlreadyFound()
            => feedbackPalette != null ? feedbackPalette.hudBubbleTextAlreadyFound : new Color(0.2f, 0.35f, 0.8f);

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

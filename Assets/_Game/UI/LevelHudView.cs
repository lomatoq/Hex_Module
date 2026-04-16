// #define DOTWEEN — uncomment if DOTween is installed (already enabled if you followed setup)
#define DOTWEEN

using HexWords.Core;
using HexWords.Theming;
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
        [SerializeField] private float          scoreBarBounceScale    = 1.06f;  // peak scale (1.06 = +6%)
        [SerializeField] private float          scoreBarBounceDuration = 0.45f;
        [SerializeField] private float          scoreBarAnticipation   = 0.07f;  // sec before drop arrives

        // ── Word display ───────────────────────────────────────────────────
        [Header("Word Display")]
        [SerializeField] private TMP_Text      lastWordText;
        [SerializeField] private FeedbackPalette feedbackPalette;
        private FeedbackPalette Palette => FeedbackPaletteProvider.Resolve(feedbackPalette);

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
        [SerializeField] private float          bubbleAcceptedDismissDelay = 0.35f; // kept for reference, no longer used in sequence
        [Tooltip("Delay between drops launching and bubble dismiss starting.")]
        [SerializeField] private float          bubbleDropDismissDelay     = 0.12f;

        [Header("Bubble Animation – Shake (already found)")]
        [SerializeField] private float bubbleShakeDuration  = 0.4f;
        [SerializeField] private float bubbleShakeStrength  = 14f;
        [SerializeField] private int   bubbleShakeVibrato   = 18;

        [Header("Bubble Animation – Dismiss (fly up)")]
        [SerializeField] private float          bubbleDismissRise       = 60f;
        [SerializeField] private float          bubbleDismissDuration   = 0.25f;
        [SerializeField] private AnimationCurve bubbleDismissMoveCurve  = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve bubbleDismissAlphaCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Bubble Text")]
        [Tooltip("Duration of the letter fade-in when the bubble word changes (0 = instant).")]
        [SerializeField] private float bubbleTextFadeIn = 0.10f;

        // ── Accent color (shared across badge, drops, bar fill) ────────────
        [Header("Accent Color")]
        [SerializeField] private float accentColorDuration = 0.15f;

        private Color _currentAccentColor;
        private int   _lastWordScore;
        private int   _bubbleVisibleCharCount; // how many chars were visible before last SetBubbleText

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
                float punch       = scoreBarBounceScale - 1f; // e.g. 0.06 for 6% overshoot
                if (barRT != null)
                {
                    DOTween.Kill(barRT);
                    barRT.localScale = Vector3.one;
                    barRT.DOPunchScale(Vector3.one * punch, scoreBarBounceDuration, 1, 0.3f)
                         .SetDelay(bounceDelay)
                         .SetId(barRT);
                }
                if (scoreTRT != null)
                {
                    DOTween.Kill(scoreTRT);
                    scoreTRT.localScale = Vector3.one;
                    scoreTRT.DOPunchScale(Vector3.one * punch, scoreBarBounceDuration, 1, 0.3f)
                            .SetDelay(bounceDelay)
                            .SetId(scoreTRT);
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
            _lastWordScore = isValid ? score : 0;
            SetBubbleText(word, BubbleTextColor());

            var col = isValid
                ? (feedbackPalette != null ? Palette.hudCurrentWordColor : new Color(0.2f, 0.8f, 0.2f))
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
            var textCol = outcome == WordSubmitOutcome.AlreadyAccepted
                ? BubbleTextColorAlreadyFound()
                : BubbleTextColor();
            SetBubbleText(text, textCol);
            ApplyAccentColor(GetHudColor(outcome));
            ShowBubble(text);
        }

        // ── Bubble animations ──────────────────────────────────────────────

        /// <summary>
        /// Bounces or shakes the bubble depending on outcome, optionally fires score drop.
        /// withDrop=true for new words, shake=true for already-found words.
        /// </summary>
        public void PlayBubbleAccepted(bool withDrop = true, bool shake = false)
        {
            if (withDrop && _lastWordScore > 0) PlayScoreDrop();

            if (wordBubble == null) return;
#if DOTWEEN
            int tweenId = wordBubble.GetInstanceID() + 2;
            DOTween.Kill(tweenId);

            if (shake)
            {
                // Shake left/right, then fly-up dismiss exactly as for new words
                DOTween.Sequence()
                    .SetId(tweenId)
                    .Append(wordBubble.DOShakeAnchorPos(
                        bubbleShakeDuration,
                        new Vector2(bubbleShakeStrength, 0f),
                        bubbleShakeVibrato,
                        randomness: 0f,
                        snapping:   false,
                        fadeOut:    true))
                    .AppendCallback(PlayBubbleDismiss);
            }
            else
            {
                // Fly-up dismiss — with a short delay so drops are visible first
                if (bubbleDropDismissDelay > 0f)
                    DOTween.Sequence()
                        .SetId(tweenId)
                        .AppendInterval(bubbleDropDismissDelay)
                        .AppendCallback(PlayBubbleDismiss);
                else
                    PlayBubbleDismiss();
            }
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
                _bubbleVisibleCharCount = 0;
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
#endif
                wordBubbleCanvasGroup.alpha = hasContent ? 1f : 0f;
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

        /// <summary>
        /// Sets bubble text. Only newly-added characters fade in from alpha=0;
        /// existing characters stay at full opacity.
        /// </summary>
        private void SetBubbleText(string text, Color color)
        {
            if (lastWordText == null) return;

            int prevCount = _bubbleVisibleCharCount;
            lastWordText.text  = text;
            lastWordText.color = color;
            _bubbleVisibleCharCount = string.IsNullOrEmpty(text) ? 0 : text.Length;

#if DOTWEEN
            DOTween.Kill(lastWordText);

            // No new characters or fade disabled — nothing to animate
            int newCount = _bubbleVisibleCharCount;
            if (bubbleTextFadeIn <= 0f || newCount <= prevCount || prevCount < 0) return;

            // Force TMP to rebuild mesh so characterInfo is valid
            lastWordText.ForceMeshUpdate();
            var textInfo = lastWordText.textInfo;

            // Set alpha=0 on vertex colors for new characters
            for (int i = prevCount; i < newCount; i++)
            {
                if (i >= textInfo.characterCount) break;
                var ci = textInfo.characterInfo[i];
                if (!ci.isVisible) continue;
                var cols = textInfo.meshInfo[ci.materialReferenceIndex].colors32;
                for (int v = 0; v < 4; v++)
                    cols[ci.vertexIndex + v].a = 0;
            }
            lastWordText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            // Animate those new characters to alpha=255
            float t = 0f;
            DOTween.To(() => t, val =>
            {
                t = val;
                byte a = (byte)(val * 255f);
                lastWordText.ForceMeshUpdate();
                var ti = lastWordText.textInfo;
                for (int i = prevCount; i < newCount; i++)
                {
                    if (i >= ti.characterCount) break;
                    var ci = ti.characterInfo[i];
                    if (!ci.isVisible) continue;
                    var cols = ti.meshInfo[ci.materialReferenceIndex].colors32;
                    for (int v = 0; v < 4; v++)
                        cols[ci.vertexIndex + v].a = a;
                }
                lastWordText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            }, 1f, bubbleTextFadeIn).SetId(lastWordText);
#endif
        }

        // ── Colour helpers ─────────────────────────────────────────────────

        private Color BubbleTextColor()
            => feedbackPalette != null ? Palette.hudBubbleTextDefault : Color.black;

        private Color BubbleTextColorAlreadyFound()
            => feedbackPalette != null ? Palette.hudBubbleTextAlreadyFound : new Color(0.2f, 0.35f, 0.8f);

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
                WordSubmitOutcome.TargetAccepted  => Palette.hudTargetAcceptedColor,
                WordSubmitOutcome.BonusAccepted   => Palette.hudBonusAcceptedColor,
                WordSubmitOutcome.AlreadyAccepted => Palette.hudAlreadyAcceptedColor,
                _                                 => Palette.hudRejectedColor
            };
        }
    }
}

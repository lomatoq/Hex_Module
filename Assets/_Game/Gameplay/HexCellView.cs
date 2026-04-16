// Remove this comment after installing DOTween via Asset Store
#define DOTWEEN

using System.Collections;
using HexWords.Core;
using HexWords.Theming;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
#if DOTWEEN
using DG.Tweening;
#endif

namespace HexWords.Gameplay
{
    public class HexCellView : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler, ICellFxPlayer
    {
        [SerializeField] private TMP_Text        letterText;
        [SerializeField] private Image           background;
        [SerializeField] private FeedbackPalette feedbackPalette;
        private FeedbackPalette Palette => FeedbackPaletteProvider.Resolve(feedbackPalette);
        [SerializeField] private HexCellAnimConfig animConfig;

        [Header("Ink Effect (optional)")]
        [SerializeField] private Image inkSplatOverlay;

        [Header("Circle Fill (optional)")]
        [Tooltip("The circle Image inside HexCellMask — scaled 0→fillFinalScale on select")]
        [SerializeField] private RectTransform circleFill;

        public string    CellId     { get; private set; }
        public TMP_Text  LetterText => letterText;

        public delegate void CellEvent(HexCellView cell);
        public event CellEvent PointerDownOnCell;
        public event CellEvent PointerEnterOnCell;
        public event CellEvent PointerUpOnCell;

        private Color   _baseColor       = Color.white;
        private Color   _baseLetterColor = Color.black;
        private Vector2 _baseAnchoredPos;

        private Coroutine _fxRoutine;
        private Coroutine _hintRoutine;

#if DOTWEEN
        // Integer IDs — used ONLY for tweens on this specific cell.
        // IMPORTANT: never use offsets here; int IDs are global in DOTween,
        // so GetInstanceID()+N of cell A could equal GetInstanceID() of cell B,
        // causing cross-cell kills. WordBounce and Idle use SetTarget(this) instead.
        private int TweenId       => GetInstanceID();
        private int CircleTweenId => GetInstanceID() + 100;
#endif

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (inkSplatOverlay != null)
                SetAlpha(inkSplatOverlay, 0f);

            // Circle fill: hidden via scale=0; prefab alpha is preserved
            if (circleFill != null)
            {
                circleFill.anchorMin        = new Vector2(0.5f, 0.5f);
                circleFill.anchorMax        = new Vector2(0.5f, 0.5f);
                circleFill.pivot            = new Vector2(0.5f, 0.5f);
                circleFill.anchoredPosition = Vector2.zero;
                circleFill.localScale       = Vector3.zero;
            }

        }

        private void Start()
        {
            // anchoredPosition is set by GridView after Bind(), so cache it here.
            _baseAnchoredPos = ((RectTransform)transform).anchoredPosition;
        }

        private void OnDisable()
        {
#if DOTWEEN
            DOTween.Kill(TweenId);
            DOTween.Kill(CircleTweenId);
            DOTween.Kill(this);   // word bounce + idle (SetTarget-based, no int collision)
#endif
        }

        // ── Bind ───────────────────────────────────────────────────────────

        public void Bind(CellDefinition cellDefinition)
        {
            CellId = cellDefinition.cellId;
            if (letterText != null)
            {
                EnsureLetterCentered();
                letterText.text = WordNormalizer.Normalize(cellDefinition.letter);
                _baseLetterColor = feedbackPalette != null
                    ? Palette.cellLetterDefault
                    : letterText.color;
                letterText.color = _baseLetterColor;
            }
            if (background != null)
                _baseColor = background.color;

        }

        private void EnsureLetterCentered()
        {
            letterText.alignment = TextAlignmentOptions.Center;
            var r                = letterText.rectTransform;
            r.anchorMin          = new Vector2(0.5f, 0.5f);
            r.anchorMax          = new Vector2(0.5f, 0.5f);
            r.pivot              = new Vector2(0.5f, 0.5f);
            r.anchoredPosition   = Vector2.zero;
            r.sizeDelta          = background != null
                ? background.rectTransform.rect.size
                : new Vector2(120f, 120f);
        }

        // ── Pointer ────────────────────────────────────────────────────────

        public void OnPointerDown(PointerEventData e)  => PointerDownOnCell?.Invoke(this);
        public void OnPointerEnter(PointerEventData e) => PointerEnterOnCell?.Invoke(this);
        public void OnPointerUp(PointerEventData e)    => PointerUpOnCell?.Invoke(this);

        // ── ICellFxPlayer ──────────────────────────────────────────────────

        public void OnSelected()
        {
            KillAll();

            // Explicitly reset background — KillAll may leave it mid-animation (e.g. red from rejection)
            if (background != null) background.color = _baseColor;

            var selColor = feedbackPalette != null ? Palette.cellSelectedBackground : new Color(0.85f, 0.95f, 1f);
            var letColor = feedbackPalette != null ? Palette.cellLetterSelected : Color.white;

            if (letterText != null) letterText.color = letColor;

#if DOTWEEN
            // Scale in and hold
            float holdScale = animConfig != null ? animConfig.selectHoldScale    : 1.10f;
            float holdDur   = animConfig != null ? animConfig.selectHoldDuration : 0.15f;
            var   holdCurve = animConfig != null ? animConfig.selectHoldCurve    : AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

            // Snap to neutral first (idle may have left scale/rotation mid-animation)
            transform.localScale    = Vector3.one;
            transform.localRotation = Quaternion.identity;
            transform.DOScale(holdScale, holdDur).SetEase(holdCurve).SetId(TweenId);

            // Schedule idle animation to start after hold scale completes
            if (animConfig != null && animConfig.enableAdvancedCellAnim &&
                (animConfig.idleScaleEnabled || animConfig.idleRotationEnabled))
                DOVirtual.DelayedCall(holdDur + 0.02f, StartIdleAnim).SetId(TweenId);

            // Circle fill: scale from 0 → fillFinalScale
            PlayCircleFill(selColor);

            PlayInkSplat(selColor);
#else
            float holdScale = animConfig != null ? animConfig.selectHoldScale : 1.10f;
            transform.localScale = Vector3.one * holdScale;
#endif
        }

        public void OnPathAccepted()
        {
            var color = feedbackPalette != null ? Palette.cellAcceptedBackground : new Color(0.75f, 1f, 0.75f);
            FlashAndReturn(color, AcceptFlashDuration());
        }

        public void OnPathBonusAccepted()
        {
            var color = feedbackPalette != null ? Palette.cellBonusBackground : new Color(0.65f, 0.95f, 1f);
            FlashAndReturn(color, AcceptFlashDuration());
        }

        public void OnPathAlreadyAccepted()
        {
            // No color flash — snap to neutral then shake horizontally
            KillAll();
            if (letterText != null) letterText.color = _baseLetterColor;
            ResetCircleFill();
            ReturnToNeutralImmediate();
#if DOTWEEN
            float shakeStr = animConfig != null ? animConfig.shakePositionStrength : 5f;
            float shakeDur = animConfig != null ? animConfig.shakeDuration         : 0.25f;
            int   shakeVib = animConfig != null ? animConfig.shakeVibrato          : 18;
            transform.DOShakePosition(shakeDur, new Vector3(shakeStr, 0f, 0f), shakeVib, 0f, false, true).SetId(TweenId);
#endif
        }

        public void OnPathRejected()
        {
            KillAll();
            if (letterText != null) letterText.color = _baseLetterColor;
            ResetCircleFill();
            ReturnToNeutralImmediate();

            var color = feedbackPalette != null ? Palette.cellRejectedBackground : new Color(1f, 0.8f, 0.8f);
#if DOTWEEN
            float flashDur  = animConfig != null ? animConfig.rejectFlashDuration   : 0.3f;
            float shakeStr  = animConfig != null ? animConfig.shakePositionStrength : 5f;
            float shakeDur  = animConfig != null ? animConfig.shakeDuration         : 0.25f;
            int   shakeVib  = animConfig != null ? animConfig.shakeVibrato          : 18;

            if (background != null)
                background.DOColor(_baseColor, flashDur).From(color).SetEase(Ease.OutCubic).SetId(TweenId);
            transform.DOShakePosition(shakeDur, new Vector3(shakeStr, 0f, 0f), shakeVib, 0f, false, true).SetId(TweenId);
#else
            PlayFlashAndReturn(color, 0.2f);
#endif
        }

        public void ResetFx()
        {
            KillAll();
            ReturnToNeutralImmediate();
            ((RectTransform)transform).anchoredPosition = _baseAnchoredPos;
            if (background  != null) background.color = _baseColor;
            if (letterText  != null) letterText.color = _baseLetterColor;
            if (inkSplatOverlay != null) SetAlpha(inkSplatOverlay, 0f);
            ResetCircleFill();
        }

        // ── Hint pulse ─────────────────────────────────────────────────────

        public void PlayHintPulse(float delay = 0f, HintAnimationConfig config = null)
        {
#if DOTWEEN
            DOTween.Kill(TweenId);
            int   pulseCount = config != null ? config.pulseCount         : 3;
            float fadeIn     = config != null ? config.pulseFadeIn        : 0.22f;
            float fadeOut    = config != null ? config.pulseFadeOut       : 0.22f;
            float pause      = config != null ? config.pauseBetweenPulses : 0.10f;
            float peakScale  = config != null ? config.peakScale          : 1.12f;
            var targetColor  = feedbackPalette != null ? Palette.cellSelectedBackground : new Color(0.85f, 0.95f, 1f);

            var seq = DOTween.Sequence().SetId(TweenId);
            if (delay > 0f) seq.AppendInterval(delay);
            for (var i = 0; i < pulseCount; i++)
            {
                seq.Append(background.DOColor(targetColor, fadeIn).SetEase(Ease.InOutSine));
                seq.Join(transform.DOScale(peakScale, fadeIn).SetEase(Ease.InOutSine));
                seq.Append(background.DOColor(_baseColor, fadeOut).SetEase(Ease.InOutSine));
                seq.Join(transform.DOScale(1f, fadeOut).SetEase(Ease.InOutSine));
                if (i < pulseCount - 1 && pause > 0f) seq.AppendInterval(pause);
            }
#else
            StopHintRoutine();
            _hintRoutine = StartCoroutine(HintPulseRoutine(delay, config));
#endif
        }

        // ── Circle Fill ────────────────────────────────────────────────────

        private void PlayCircleFill(Color color)
        {
#if DOTWEEN
            if (circleFill == null) return;

            float dur        = animConfig != null ? animConfig.fillDuration   : 0.14f;
            float finalScale = animConfig != null ? animConfig.fillFinalScale : 1.0f;
            var   curve      = animConfig != null ? animConfig.fillCurve      : AnimationCurve.EaseInOut(0, 0, 1, 1);

            var img = circleFill.GetComponent<Image>();
            if (img != null)
            {
                float a = img.color.a; // preserve prefab alpha (e.g. 40%)
                img.color = new Color(color.r, color.g, color.b, a);
            }

            DOTween.Kill(CircleTweenId);
            circleFill.localScale = Vector3.zero;

            float t = 0f;
            DOTween.To(() => t, v =>
            {
                t = v;
                float s = Mathf.LerpUnclamped(0f, finalScale, curve.Evaluate(t));
                circleFill.localScale = Vector3.one * s;
            }, 1f, dur).SetId(CircleTweenId);
#endif
        }

        private void ResetCircleFill()
        {
#if DOTWEEN
            DOTween.Kill(CircleTweenId);
#endif
            if (circleFill != null)
                circleFill.localScale = Vector3.zero;
        }


        // ── Helpers ────────────────────────────────────────────────────────

        private float AcceptFlashDuration() => animConfig != null ? animConfig.acceptFlashDuration : 0.25f;

        private void FlashAndReturn(Color flashColor, float duration)
        {
            KillAll();
            // Instantly snap scale + rotation back to neutral before new animations start.
            // ApplyWordStateColor (called immediately after) kills TweenId, so smooth return
            // would be cut short — instant snap is the only reliable approach here.
            ReturnToNeutralImmediate();
            // Restore letter color and circle fill immediately
            if (letterText != null) letterText.color = _baseLetterColor;
            ResetCircleFill();
#if DOTWEEN
            float punchScale = animConfig != null ? animConfig.acceptPunchScale    : 0.08f;
            float punchDur   = animConfig != null ? animConfig.acceptPunchDuration : duration * 0.7f;
            int   punchVib   = animConfig != null ? animConfig.acceptPunchVibrato  : 3;
            float punchEla   = animConfig != null ? animConfig.acceptElasticity    : 0.5f;

            if (background != null)
                background.DOColor(_baseColor, duration).From(flashColor).SetEase(Ease.OutCubic).SetId(TweenId);
            transform.DOPunchScale(Vector3.one * punchScale, punchDur, punchVib, punchEla).SetId(TweenId);
#else
            PlayFlashAndReturn(flashColor, duration);
#endif
        }

        /// <summary>Instantly snaps scale to 1 and rotation to identity. Call after KillAll.</summary>
        private void ReturnToNeutralImmediate()
        {
            transform.localScale    = Vector3.one;
            transform.localRotation = Quaternion.identity;
        }

        private void KillAll()
        {
#if DOTWEEN
            DOTween.Kill(TweenId);            // hold scale, flash, shake, delayed-call
            DOTween.Kill(CircleTweenId);      // circle fill
            DOTween.Kill(this);               // word bounce + idle (SetTarget-based)
            if (background      != null) DOTween.Kill(background);        // state color sequence
            if (letterText      != null) DOTween.Kill(letterText);        // letter color tween
            if (letterText      != null) DOTween.Kill(letterText.rectTransform); // letter bounce
            if (inkSplatOverlay != null) DOTween.Kill(inkSplatOverlay);   // ink splat
#else
            StopFxRoutine();
#endif
        }

        /// <summary>
        /// Immediately tints background + circle fill to stateColor.
        /// After holdDuration, smoothly returns to the original base color.
        /// </summary>
        public void ApplyWordStateColor(Color stateColor, float holdDuration, float returnDuration)
        {
            if (background == null) return;
#if DOTWEEN
            // Kill both ID-tagged tweens (FlashAndReturn, Reject) and target-based (previous state color)
            DOTween.Kill(TweenId);
            DOTween.Kill(background);
            if (letterText != null) DOTween.Kill(letterText);

            background.color = stateColor;

            // Keep letter at selected color during the hold phase
            var selLetterCol  = feedbackPalette != null ? Palette.cellLetterSelected : Color.white;
            if (letterText != null) letterText.color = selLetterCol;

            var baseCol       = _baseColor;
            var baseLetterCol = _baseLetterColor;

            var seq = DOTween.Sequence()
                .SetTarget(background) // SetTarget allows DOTween.Kill(background) to kill this sequence
                .AppendInterval(holdDuration)
                .Append(background.DOColor(baseCol, returnDuration).SetEase(Ease.OutCubic));

            if (letterText != null)
                seq.Join(letterText.DOColor(baseLetterCol, returnDuration).SetEase(Ease.OutCubic));

            seq.OnKill(() =>
            {
                if (background != null) background.color = baseCol;
                if (letterText != null) letterText.color = baseLetterCol;
            });
#else
            background.color = stateColor;
#endif
        }

        /// <summary>Plays a punch-scale bounce after delay (for sequential word-accepted FX).</summary>
        public void PlayWordBounce(float delay, HexCellAnimConfig config)
        {
#if DOTWEEN
            float punch        = config != null ? config.wordBouncePunchScale   : 0.12f;
            float dur          = config != null ? config.wordBounceDuration     : 0.20f;
            var   curve        = config != null ? config.wordBounceCurve        : AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            float letterOffset = config != null ? config.wordLetterBounceOffset : 0.04f;

            DOTween.Kill(this);   // kill previous bounce/idle (target-based, no int collision)
            var seq = DOTween.Sequence().SetTarget(this);
            if (delay > 0f) seq.AppendInterval(delay);
            seq.Append(transform.DOPunchScale(Vector3.one * punch, dur, 3, 0.4f).SetEase(curve));

            // Letter bounce: independent sequence, starts slightly after cell bounce for organic feel
            if (letterText != null)
            {
                var letterRt = letterText.rectTransform;
                DOTween.Kill(letterRt);
                float letterDelay = delay + letterOffset;
                var letterSeq = DOTween.Sequence().SetTarget(letterRt);
                if (letterDelay > 0f) letterSeq.AppendInterval(letterDelay);
                letterSeq.Append(letterRt.DOPunchScale(Vector3.one * punch, dur, 3, 0.4f).SetEase(curve));
            }
#endif
        }

#if DOTWEEN
        /// <summary>Starts looping idle scale / rotation. Call after hold-scale animation completes.</summary>
        private void StartIdleAnim()
        {
            DOTween.Kill(this);   // kill previous idle (SetTarget-based, no int collision)
            if (animConfig == null || !animConfig.enableAdvancedCellAnim) return;

            float holdScale = animConfig.selectHoldScale;

            if (animConfig.idleScaleEnabled)
            {
                // Breathe: holdScale → idleScalePeak → holdScale → … (Yoyo loop)
                transform.localScale = Vector3.one * holdScale;
                transform.DOScale(animConfig.idleScalePeak, animConfig.idleScaleHalfPeriod)
                    .SetEase(animConfig.idleScaleCurve)
                    .SetTarget(this)      // SetTarget avoids int ID collision between cells
                    .SetLoops(-1, LoopType.Yoyo);
            }

            if (animConfig.idleRotationEnabled)
            {
                // Pendulum: 0° → +angle → 0° → -angle → 0° (smooth continuous loop)
                float hp = animConfig.idleRotationHalfPeriod;
                float a  = animConfig.idleRotationAngle;
                transform.localRotation = Quaternion.identity;
                DOTween.Sequence().SetTarget(this).SetLoops(-1)
                    .Append(transform.DOLocalRotate(new Vector3(0f, 0f,  a), hp * 0.5f, RotateMode.Fast).SetEase(animConfig.idleRotationCurve))
                    .Append(transform.DOLocalRotate(new Vector3(0f, 0f, -a), hp,         RotateMode.Fast).SetEase(animConfig.idleRotationCurve))
                    .Append(transform.DOLocalRotate(Vector3.zero,             hp * 0.5f, RotateMode.Fast).SetEase(animConfig.idleRotationCurve));
            }
        }
#endif

#if DOTWEEN
        private void PlayInkSplat(Color color)
        {
            if (inkSplatOverlay == null) return;
            DOTween.Kill(inkSplatOverlay);
            inkSplatOverlay.color                = new Color(color.r, color.g, color.b, 0.55f);
            inkSplatOverlay.transform.localScale = Vector3.zero;
            DOTween.Sequence().SetId(inkSplatOverlay)
                   .Append(inkSplatOverlay.transform.DOScale(1.2f, 0.12f).SetEase(Ease.OutBack))
                   .Join(DOTween.To(() => inkSplatOverlay.color.a, a => SetAlpha(inkSplatOverlay, a), 0f, 0.35f).SetEase(Ease.InCubic))
                   .AppendCallback(() => inkSplatOverlay.transform.localScale = Vector3.zero);
        }
#endif

        // ── Coroutine fallback (no DOTween) ────────────────────────────────

        private void PlayFlashAndReturn(Color flashColor, float duration)
        {
            StopFxRoutine();
            _fxRoutine = StartCoroutine(FlashRoutine(flashColor, duration));
        }

        private IEnumerator FlashRoutine(Color flashColor, float duration)
        {
            if (background == null) yield break;
            background.color = flashColor;
            var scale = transform.localScale;
            var t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                var k = Mathf.Clamp01(t / duration);
                background.color     = Color.Lerp(flashColor, _baseColor, k);
                transform.localScale = Vector3.Lerp(scale, Vector3.one, k);
                yield return null;
            }
            background.color     = _baseColor;
            transform.localScale = Vector3.one;
            _fxRoutine = null;
        }

        private IEnumerator HintPulseRoutine(float delay, HintAnimationConfig cfg)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (background == null) yield break;

            var targetColor = feedbackPalette != null ? Palette.cellSelectedBackground : new Color(0.85f, 0.95f, 1f);
            int   pulseCount = cfg != null ? cfg.pulseCount          : 3;
            float fadeIn     = cfg != null ? cfg.pulseFadeIn         : 0.22f;
            float fadeOut    = cfg != null ? cfg.pulseFadeOut        : 0.22f;
            float pause      = cfg != null ? cfg.pauseBetweenPulses  : 0.10f;
            float peakScale  = cfg != null ? cfg.peakScale           : 1.12f;
            var   targetScale = Vector3.one * peakScale;

            for (int i = 0; i < pulseCount; i++)
            {
                for (float t = 0f; t < fadeIn; t += Time.deltaTime)
                {
                    float k = Smooth(t / fadeIn);
                    background.color     = Color.Lerp(_baseColor, targetColor, k);
                    transform.localScale = Vector3.Lerp(Vector3.one, targetScale, k);
                    yield return null;
                }
                background.color     = targetColor;
                transform.localScale = targetScale;

                for (float t = 0f; t < fadeOut; t += Time.deltaTime)
                {
                    float k = Smooth(t / fadeOut);
                    background.color     = Color.Lerp(targetColor, _baseColor, k);
                    transform.localScale = Vector3.Lerp(targetScale, Vector3.one, k);
                    yield return null;
                }
                background.color     = _baseColor;
                transform.localScale = Vector3.one;

                if (i < pulseCount - 1 && pause > 0f)
                    yield return new WaitForSeconds(pause);
            }
            _hintRoutine = null;
        }

        private void StopFxRoutine()
        {
            if (_fxRoutine != null) { StopCoroutine(_fxRoutine); _fxRoutine = null; }
        }

        private void StopHintRoutine()
        {
            if (_hintRoutine != null) { StopCoroutine(_hintRoutine); _hintRoutine = null; }
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);

        private static void SetAlpha(Image img, float a)
        {
            var c = img.color; c.a = a; img.color = c;
        }
    }
}

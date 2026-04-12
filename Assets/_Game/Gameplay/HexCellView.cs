// Remove this comment after installing DOTween via Asset Store
#define DOTWEEN

using System.Collections;
using HexWords.Core;
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
        [SerializeField] private HexCellAnimConfig animConfig;

        [Header("Ink Effect (optional)")]
        [SerializeField] private Image inkSplatOverlay;

        [Header("Circle Fill (optional)")]
        [Tooltip("The circle Image inside HexCellMask — scaled 0→fillFinalScale on select")]
        [SerializeField] private RectTransform circleFill;

        public string CellId { get; private set; }

        public delegate void CellEvent(HexCellView cell);
        public event CellEvent PointerDownOnCell;
        public event CellEvent PointerEnterOnCell;
        public event CellEvent PointerUpOnCell;

        private Color   _baseColor       = Color.white;
        private Color   _baseLetterColor = Color.black;
        private Vector2 _baseAnchoredPos;
        private Canvas  _letterCanvas;          // overrideSorting canvas on letterText GO

        private Coroutine _fxRoutine;
        private Coroutine _hintRoutine;

#if DOTWEEN
        private int TweenId      => GetInstanceID();
        private int CircleTweenId => GetInstanceID() + 100;
#endif

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (inkSplatOverlay != null)
                SetAlpha(inkSplatOverlay, 0f);

            // Reset circle fill to invisible
            if (circleFill != null)
                circleFill.localScale = Vector3.zero;

            // Apply letter sorting order via a Canvas on the letterText GameObject
            ApplyLetterSorting();
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
                    ? feedbackPalette.cellLetterDefault
                    : letterText.color;
                letterText.color = _baseLetterColor;
            }
            if (background != null)
                _baseColor = background.color;

            // Re-apply sorting in case config changed since Awake
            ApplyLetterSorting();
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

            var selColor = feedbackPalette != null ? feedbackPalette.selectedCellColor : new Color(0.85f, 0.95f, 1f);
            var letColor = feedbackPalette != null ? feedbackPalette.cellLetterSelected : Color.white;

            if (background != null) background.color = selColor;
            if (letterText != null) letterText.color  = letColor;

#if DOTWEEN
            // Punch scale
            float punchMag = animConfig != null ? animConfig.selectPunchScale    : 0.13f;
            float punchDur = animConfig != null ? animConfig.selectPunchDuration : 0.18f;
            int   punchVib = animConfig != null ? animConfig.selectPunchVibrato  : 5;
            float punchEla = animConfig != null ? animConfig.selectElasticity    : 0.5f;

            transform.localScale = Vector3.one;
            transform.DOPunchScale(Vector3.one * punchMag, punchDur, punchVib, punchEla).SetId(TweenId);

            // Circle fill: scale from 0 → fillFinalScale
            PlayCircleFill(selColor);

            PlayInkSplat(selColor);
#else
            transform.localScale = Vector3.one * 1.05f;
            if (circleFill != null) circleFill.localScale = Vector3.one;
#endif
        }

        public void OnPathAccepted()
        {
            var color = feedbackPalette != null ? feedbackPalette.targetAcceptedCellColor : new Color(0.75f, 1f, 0.75f);
            FlashAndReturn(color, AcceptFlashDuration());
        }

        public void OnPathBonusAccepted()
        {
            var color = feedbackPalette != null ? feedbackPalette.bonusAcceptedCellColor : new Color(0.65f, 0.95f, 1f);
            FlashAndReturn(color, AcceptFlashDuration());
        }

        public void OnPathAlreadyAccepted()
        {
            var color = feedbackPalette != null ? feedbackPalette.alreadyAcceptedCellColor : new Color(0.55f, 0.7f, 1f);
            FlashAndReturn(color, 0.2f);
        }

        public void OnPathRejected()
        {
            KillAll();
            if (letterText != null) letterText.color = _baseLetterColor;
            ResetCircleFill();

            var color = feedbackPalette != null ? feedbackPalette.rejectedCellColor : new Color(1f, 0.8f, 0.8f);
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
            transform.localScale = Vector3.one;
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
            var targetColor  = feedbackPalette != null ? feedbackPalette.selectedCellColor : new Color(0.85f, 0.95f, 1f);

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
            if (img != null) img.color = color;

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

        // ── Letter sorting ─────────────────────────────────────────────────

        private void ApplyLetterSorting()
        {
            if (letterText == null) return;

            bool  above = animConfig == null || animConfig.letterAboveTrail;
            int   order = animConfig != null  ? animConfig.letterSortingOrder : 10;

            if (above)
            {
                if (_letterCanvas == null)
                    _letterCanvas = letterText.gameObject.GetComponent<Canvas>()
                                ?? letterText.gameObject.AddComponent<Canvas>();
                _letterCanvas.overrideSorting = true;
                _letterCanvas.sortingOrder    = order;
                // No GraphicRaycaster needed — nested Canvas used for sorting only
            }
            else if (_letterCanvas != null)
            {
                _letterCanvas.overrideSorting = false;
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private float AcceptFlashDuration() => animConfig != null ? animConfig.acceptFlashDuration : 0.25f;

        private void FlashAndReturn(Color flashColor, float duration)
        {
            KillAll();
            // Restore letter color and circle fill immediately
            if (letterText != null) letterText.color = _baseLetterColor;
            ResetCircleFill();
#if DOTWEEN
            float punchScale = animConfig != null ? animConfig.acceptPunchScale    : 0.08f;
            float punchDur   = animConfig != null ? animConfig.acceptPunchDuration : duration * 0.7f;
            int   punchVib   = animConfig != null ? animConfig.acceptPunchVibrato  : 3;
            float punchEla   = animConfig != null ? animConfig.acceptElasticity    : 0.5f;

            transform.localScale = Vector3.one;
            if (background != null)
                background.DOColor(_baseColor, duration).From(flashColor).SetEase(Ease.OutCubic).SetId(TweenId);
            transform.DOPunchScale(Vector3.one * punchScale, punchDur, punchVib, punchEla).SetId(TweenId);
#else
            PlayFlashAndReturn(flashColor, duration);
#endif
        }

        private void KillAll()
        {
#if DOTWEEN
            DOTween.Kill(TweenId);
            DOTween.Kill(CircleTweenId);
            if (inkSplatOverlay != null) DOTween.Kill(inkSplatOverlay);
#else
            StopFxRoutine();
#endif
        }

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

            var targetColor = feedbackPalette != null ? feedbackPalette.selectedCellColor : new Color(0.85f, 0.95f, 1f);
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

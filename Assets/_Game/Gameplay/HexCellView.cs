// Uncomment the line below after installing DOTween (Asset Store or unitypackage)
//#define DOTWEEN

using System.Collections;
using HexWords.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if DOTWEEN
using DG.Tweening;
#endif

namespace HexWords.Gameplay
{
    public class HexCellView : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler, ICellFxPlayer
    {
        [SerializeField] private Text            letterText;
        [SerializeField] private Image           background;
        [SerializeField] private FeedbackPalette feedbackPalette;

        [Header("Ink Effect (optional)")]
        [SerializeField] private Image inkSplatOverlay;

        public string CellId { get; private set; }

        public delegate void CellEvent(HexCellView cell);
        public event CellEvent PointerDownOnCell;
        public event CellEvent PointerEnterOnCell;
        public event CellEvent PointerUpOnCell;

        private Color     _baseColor  = Color.white;
        private Coroutine _fxRoutine;
        private Coroutine _hintRoutine;

#if DOTWEEN
        private int TweenId => GetInstanceID();
#endif

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (inkSplatOverlay != null)
                SetAlpha(inkSplatOverlay, 0f);
        }

        private void OnDisable()
        {
#if DOTWEEN
            DOTween.Kill(TweenId);
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
            }
            if (background != null)
                _baseColor = background.color;
        }

        private void EnsureLetterCentered()
        {
            letterText.alignment      = TextAnchor.MiddleCenter;
            var r                     = letterText.rectTransform;
            r.anchorMin               = new Vector2(0.5f, 0.5f);
            r.anchorMax               = new Vector2(0.5f, 0.5f);
            r.pivot                   = new Vector2(0.5f, 0.5f);
            r.anchoredPosition        = Vector2.zero;
            r.sizeDelta               = background != null
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
#if DOTWEEN
            var selColor = feedbackPalette != null ? feedbackPalette.selectedCellColor : new Color(0.85f, 0.95f, 1f);
            if (background != null) background.color = selColor;
            transform.localScale = Vector3.one;
            transform.DOPunchScale(Vector3.one * 0.13f, 0.18f, 5, 0.5f).SetId(TweenId);
            PlayInkSplat(selColor);
#else
            if (background != null)
                background.color = feedbackPalette != null
                    ? feedbackPalette.selectedCellColor
                    : new Color(0.85f, 0.95f, 1f, 1f);
            transform.localScale = Vector3.one * 1.05f;
#endif
        }

        public void OnPathAccepted()
        {
            var color = feedbackPalette != null ? feedbackPalette.targetAcceptedCellColor : new Color(0.75f, 1f, 0.75f);
            FlashAndReturn(color, 0.25f);
        }

        public void OnPathBonusAccepted()
        {
            var color = feedbackPalette != null ? feedbackPalette.bonusAcceptedCellColor : new Color(0.65f, 0.95f, 1f);
            FlashAndReturn(color, 0.25f);
        }

        public void OnPathAlreadyAccepted()
        {
            var color = feedbackPalette != null ? feedbackPalette.alreadyAcceptedCellColor : new Color(0.55f, 0.7f, 1f);
            FlashAndReturn(color, 0.2f);
        }

        public void OnPathRejected()
        {
            KillAll();
            var color = feedbackPalette != null ? feedbackPalette.rejectedCellColor : new Color(1f, 0.8f, 0.8f);
#if DOTWEEN
            if (background != null)
                background.DOColor(_baseColor, 0.3f).From(color).SetEase(Ease.OutCubic).SetId(TweenId);
            transform.DOShakePosition(0.25f, new Vector3(5f, 0f, 0f), 18, 0f, false, true).SetId(TweenId);
#else
            PlayFlashAndReturn(color, 0.2f);
#endif
        }

        public void ResetFx()
        {
            KillAll();
            transform.localScale    = Vector3.one;
            transform.localPosition = Vector3.zero;
            if (background != null)      background.color = _baseColor;
            if (inkSplatOverlay != null) SetAlpha(inkSplatOverlay, 0f);
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

        // ── Helpers ────────────────────────────────────────────────────────

        private void FlashAndReturn(Color flashColor, float duration)
        {
            KillAll();
#if DOTWEEN
            transform.localScale = Vector3.one;
            if (background != null)
                background.DOColor(_baseColor, duration).From(flashColor).SetEase(Ease.OutCubic).SetId(TweenId);
            transform.DOPunchScale(Vector3.one * 0.08f, duration * 0.7f, 3, 0.5f).SetId(TweenId);
#else
            PlayFlashAndReturn(flashColor, duration);
#endif
        }

        private void KillAll()
        {
#if DOTWEEN
            DOTween.Kill(TweenId);
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
            inkSplatOverlay.color           = new Color(color.r, color.g, color.b, 0.55f);
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

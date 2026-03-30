using DG.Tweening;
using HexWords.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HexWords.Gameplay
{
    public class HexCellView : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler, ICellFxPlayer
    {
        [SerializeField] private Text          letterText;
        [SerializeField] private Image         background;
        [SerializeField] private FeedbackPalette feedbackPalette;

        // ── Optional ink-splat overlay (assign a circular/blob sprite) ──────
        // If set, plays a scale-punch "ink drop" whenever this cell is selected.
        [Header("Ink Effect (optional)")]
        [SerializeField] private Image inkSplatOverlay;

        public string CellId { get; private set; }

        public delegate void CellEvent(HexCellView cell);
        public event CellEvent PointerDownOnCell;
        public event CellEvent PointerEnterOnCell;
        public event CellEvent PointerUpOnCell;

        private Color _baseColor = Color.white;

        // ── Tween IDs (used for DOTween.Kill) ──────────────────────────────
        // Using GetInstanceID() as the shared id for all tweens on this cell
        // so DOTween.Kill(id) stops every tween belonging to it.
        private int TweenId => GetInstanceID();

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (inkSplatOverlay != null)
                inkSplatOverlay.SetAlphaImmediate(0f);
        }

        private void OnDisable()
        {
            DOTween.Kill(TweenId);
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
            letterText.alignment = TextAnchor.MiddleCenter;
            var textRect = letterText.rectTransform;
            textRect.anchorMin        = new Vector2(0.5f, 0.5f);
            textRect.anchorMax        = new Vector2(0.5f, 0.5f);
            textRect.pivot            = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta        = background != null
                ? background.rectTransform.rect.size
                : new Vector2(120f, 120f);
        }

        // ── Pointer events ─────────────────────────────────────────────────

        public void OnPointerDown(PointerEventData eventData)  => PointerDownOnCell?.Invoke(this);
        public void OnPointerEnter(PointerEventData eventData) => PointerEnterOnCell?.Invoke(this);
        public void OnPointerUp(PointerEventData eventData)    => PointerUpOnCell?.Invoke(this);

        // ── ICellFxPlayer ──────────────────────────────────────────────────

        public void OnSelected()
        {
            KillAll();
            var selColor = feedbackPalette != null
                ? feedbackPalette.selectedCellColor
                : new Color(0.85f, 0.95f, 1f, 1f);

            if (background != null)
                background.color = selColor;

            // Quick scale-punch: feels like pressing a button
            transform.localScale = Vector3.one;
            transform.DOPunchScale(Vector3.one * 0.13f, 0.18f, 5, 0.5f)
                      .SetId(TweenId);

            // Ink splat: small blob appears and fades out
            PlayInkSplat(selColor);
        }

        public void OnPathAccepted()
        {
            var color = feedbackPalette != null
                ? feedbackPalette.targetAcceptedCellColor
                : new Color(0.75f, 1f, 0.75f, 1f);
            FlashAndReturn(color, 0.25f, punch: 0.08f);
        }

        public void OnPathBonusAccepted()
        {
            var color = feedbackPalette != null
                ? feedbackPalette.bonusAcceptedCellColor
                : new Color(0.65f, 0.95f, 1f, 1f);
            FlashAndReturn(color, 0.25f, punch: 0.08f);
        }

        public void OnPathAlreadyAccepted()
        {
            var color = feedbackPalette != null
                ? feedbackPalette.alreadyAcceptedCellColor
                : new Color(0.55f, 0.7f, 1f, 1f);
            FlashAndReturn(color, 0.2f, punch: 0f);
        }

        public void OnPathRejected()
        {
            KillAll();
            var color = feedbackPalette != null
                ? feedbackPalette.rejectedCellColor
                : new Color(1f, 0.8f, 0.8f, 1f);

            if (background != null)
                background.DOColor(_baseColor, 0.3f).From(color)
                          .SetEase(Ease.OutCubic).SetId(TweenId);

            // Horizontal shake — "wrong answer" feel
            transform.DOShakePosition(0.25f, strength: new Vector3(5f, 0f, 0f),
                                      vibrato: 18, randomness: 0f,
                                      snapping: false, fadeOut: true)
                     .SetId(TweenId);
        }

        public void ResetFx()
        {
            KillAll();
            transform.localScale    = Vector3.one;
            transform.localPosition = Vector3.zero; // restore after shake
            if (background != null)
                background.color = _baseColor;
            if (inkSplatOverlay != null)
                inkSplatOverlay.SetAlphaImmediate(0f);
        }

        // ── Hint pulse (DOTween Sequence) ──────────────────────────────────

        public void PlayHintPulse(float delay = 0f, HintAnimationConfig config = null)
        {
            DOTween.Kill(TweenId);

            int   pulseCount = config != null ? config.pulseCount         : 3;
            float fadeIn     = config != null ? config.pulseFadeIn        : 0.22f;
            float fadeOut    = config != null ? config.pulseFadeOut       : 0.22f;
            float pause      = config != null ? config.pauseBetweenPulses : 0.10f;
            float peakScale  = config != null ? config.peakScale          : 1.12f;

            var targetColor  = feedbackPalette != null
                ? feedbackPalette.selectedCellColor
                : new Color(0.85f, 0.95f, 1f, 1f);

            var seq = DOTween.Sequence().SetId(TweenId);

            if (delay > 0f)
                seq.AppendInterval(delay);

            for (var i = 0; i < pulseCount; i++)
            {
                // Ease in
                seq.Append(background.DOColor(targetColor, fadeIn).SetEase(Ease.InOutSine));
                seq.Join(transform.DOScale(peakScale, fadeIn).SetEase(Ease.InOutSine));
                // Ease out
                seq.Append(background.DOColor(_baseColor, fadeOut).SetEase(Ease.InOutSine));
                seq.Join(transform.DOScale(1f, fadeOut).SetEase(Ease.InOutSine));

                if (i < pulseCount - 1 && pause > 0f)
                    seq.AppendInterval(pause);
            }
        }

        // ── Private helpers ────────────────────────────────────────────────

        private void FlashAndReturn(Color flashColor, float duration, float punch)
        {
            KillAll();
            transform.localScale = Vector3.one;

            if (background != null)
                background.DOColor(_baseColor, duration)
                          .From(flashColor)
                          .SetEase(Ease.OutCubic)
                          .SetId(TweenId);

            if (punch > 0f)
                transform.DOPunchScale(Vector3.one * punch, duration * 0.7f, 3, 0.5f)
                         .SetId(TweenId);
        }

        private void PlayInkSplat(Color color)
        {
            if (inkSplatOverlay == null) return;

            DOTween.Kill(inkSplatOverlay);
            inkSplatOverlay.color = new Color(color.r, color.g, color.b, 0.55f);
            inkSplatOverlay.transform.localScale = Vector3.zero;

            DOTween.Sequence().SetId(inkSplatOverlay)
                   .Append(inkSplatOverlay.transform.DOScale(1.2f, 0.12f).SetEase(Ease.OutBack))
                   .Join(DOTween.To(() => inkSplatOverlay.color.a,
                                   a => inkSplatOverlay.SetAlpha(a),
                                   0f, 0.35f).SetEase(Ease.InCubic))
                   .AppendCallback(() => inkSplatOverlay.transform.localScale = Vector3.zero);
        }

        private void KillAll()
        {
            DOTween.Kill(TweenId);
            if (inkSplatOverlay != null) DOTween.Kill(inkSplatOverlay);
        }
    }

    // ── Extension helper ───────────────────────────────────────────────────
    internal static class ImageExtensions
    {
        internal static void SetAlphaImmediate(this Image img, float a)
        {
            var c = img.color; c.a = a; img.color = c;
        }
        internal static void SetAlpha(this Image img, float a)
        {
            var c = img.color; c.a = a; img.color = c;
        }
    }
}

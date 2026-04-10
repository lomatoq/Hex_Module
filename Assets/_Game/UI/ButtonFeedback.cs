#define DOTWEEN

using UnityEngine;
using UnityEngine.EventSystems;
#if DOTWEEN
using DG.Tweening;
#endif

namespace HexWords.UI
{
    /// <summary>
    /// Scales a button down on press and bounces back on release.
    /// Add to any GameObject with a Button component.
    /// Pick a preset or customise parameters directly in the Inspector.
    /// </summary>
    public class ButtonFeedback : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        // ── Preset ────────────────────────────────────────────────────────
        public enum Preset
        {
            Custom,
            Default,    // regular play / action buttons
            Small,      // icon buttons (hint, settings, close)
            Soft,       // subtle — coin counters, badges
            Strong,     // main CTA — Play, Collect
        }

        [Header("Preset")]
        [SerializeField] private Preset preset = Preset.Default;

        [Header("Custom parameters (used when Preset = Custom)")]
        [SerializeField] private float pressedScale   = 0.90f;
        [SerializeField] private float pressDuration  = 0.08f;
        [SerializeField] private float bounceScale    = 1.08f;
        [SerializeField] private float bounceDuration = 0.12f;
        [SerializeField] private float settleDuration = 0.08f;
        [SerializeField] private AnimationCurve bounceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // ── State ─────────────────────────────────────────────────────────
        private bool _isPressed;

        private void Awake() => ApplyPreset();

        private void ApplyPreset()
        {
            switch (preset)
            {
                case Preset.Default:
                    pressedScale = 0.90f; pressDuration = 0.08f;
                    bounceScale  = 1.06f; bounceDuration = 0.12f; settleDuration = 0.08f;
                    break;
                case Preset.Small:
                    pressedScale = 0.85f; pressDuration = 0.06f;
                    bounceScale  = 1.10f; bounceDuration = 0.10f; settleDuration = 0.07f;
                    break;
                case Preset.Soft:
                    pressedScale = 0.94f; pressDuration = 0.10f;
                    bounceScale  = 1.03f; bounceDuration = 0.10f; settleDuration = 0.08f;
                    break;
                case Preset.Strong:
                    pressedScale = 0.88f; pressDuration = 0.07f;
                    bounceScale  = 1.12f; bounceDuration = 0.14f; settleDuration = 0.09f;
                    break;
                // Custom: use Inspector values as-is
            }
        }

        public void OnPointerDown(PointerEventData _)
        {
            _isPressed = true;
#if DOTWEEN
            DOTween.Kill(transform);
            transform.DOScale(pressedScale, pressDuration).SetEase(Ease.OutQuad).SetId(transform);
#else
            transform.localScale = Vector3.one * pressedScale;
#endif
        }

        public void OnPointerUp(PointerEventData _)
        {
            if (!_isPressed) return;
            _isPressed = false;
            Release();
        }

        public void OnPointerExit(PointerEventData _)
        {
            if (!_isPressed) return;
            _isPressed = false;
            Release();
        }

        private void Release()
        {
#if DOTWEEN
            DOTween.Kill(transform);
            DOTween.Sequence().SetId(transform)
                .Append(transform.DOScale(bounceScale, bounceDuration).SetEase(bounceCurve))
                .Append(transform.DOScale(1f,          settleDuration).SetEase(Ease.OutQuad));
#else
            transform.localScale = Vector3.one;
#endif
        }

        private void OnDisable()
        {
#if DOTWEEN
            DOTween.Kill(transform);
#endif
            transform.localScale = Vector3.one;
        }
    }
}

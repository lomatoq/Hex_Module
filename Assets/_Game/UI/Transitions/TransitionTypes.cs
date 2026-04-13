// Remove this comment after installing DOTween via Asset Store
#define DOTWEEN

using System;
using UnityEngine;
using UnityEngine.Events;
#if DOTWEEN
using DG.Tweening;
#endif

namespace HexWords.UI.Transitions
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Enums
    // ─────────────────────────────────────────────────────────────────────────

    public enum BlockPlayMode
    {
        StaggeredParallel,
        Sequential,
    }

    public enum InterruptStrategy
    {
        FinishCurrent,
        CancelAndSnap,
        CancelAndReverse,
    }

    public enum TransitionStyle
    {
        Full,
        Overlap,
        Quick,
        Instant,
    }

    public enum WaveOrigin
    {
        TopToBottom,
        BottomToTop,
        LeftToRight,
        RightToLeft,
        CenterOutward,
        OutwardToCenter,
        DirectionalAngle,
        ExplicitOrder,
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  EaseSetting — DOTween Ease preset OR custom AnimationCurve
    //  Custom PropertyDrawer: EaseSettingDrawer shows only the active option.
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class EaseSetting
    {
        [Tooltip("Use a hand-drawn AnimationCurve instead of a DOTween preset.")]
        public bool useCustomCurve = false;

#if DOTWEEN
        public Ease ease = Ease.OutCubic;
#endif
        [Tooltip("Custom curve (X = normalised time 0→1, Y = normalised value 0→1).")]
        public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

#if DOTWEEN
        public T Apply<T>(T tween) where T : Tween
        {
            if (useCustomCurve && curve != null && curve.length >= 2)
                tween.SetEase(curve);
            else
                tween.SetEase(ease);
            return tween;
        }
#endif
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  TransitionElementConfig — per-element animation settings
    //
    //  Timing:
    //    useGlobalTiming = true  → all properties share Duration / Extra Delay
    //    useGlobalTiming = false → each property has its own Duration / Delay
    //
    //  Alpha range: 0 = fully transparent, 1 = fully opaque.
    //  NOTE: nested CanvasGroups multiply  (parent.alpha × child.alpha).
    //  If animation looks wrong, check for a parent CG with alpha < 1.
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class TransitionElementConfig
    {
        // ── Global timing ──────────────────────────────────────────────────
        [Tooltip("When ON: all properties share Duration and Extra Delay.\nWhen OFF: each property gets its own timing below.")]
        public bool  useGlobalTiming = true;
        [Tooltip("Duration for all properties (when Use Global Timing is ON).")]
        public float duration        = 0.35f;
        [Tooltip("Extra delay for all properties on top of the block stagger (when Use Global Timing is ON).")]
        public float extraDelay      = 0f;

        // ── Alpha ──────────────────────────────────────────────────────────
        public bool  alphaEnabled    = true;
        public float alphaFrom       = 0f;
        public float alphaTo         = 1f;
        // per-property timing (active when useGlobalTiming = false)
        public float alphaDuration   = 0.35f;
        public float alphaDelay      = 0f;
        public EaseSetting alphaEase = new EaseSetting
        {
#if DOTWEEN
            ease = Ease.OutCubic
#endif
        };

        // ── Scale ──────────────────────────────────────────────────────────
        public bool    scaleEnabled   = false;
        public Vector3 scaleFrom      = new Vector3(0.85f, 0.85f, 1f);
        public Vector3 scaleTo        = Vector3.one;
        public float   scaleDuration  = 0.35f;
        public float   scaleDelay     = 0f;
        public EaseSetting scaleEase  = new EaseSetting
        {
#if DOTWEEN
            ease = Ease.OutBack
#endif
        };

        // ── Position (anchored delta from rest) ────────────────────────────
        public bool    positionEnabled   = false;
        [Tooltip("Offset from resting anchored position at animation START.")]
        public Vector2 positionFrom      = new Vector2(0f, -60f);
        [Tooltip("Offset from resting anchored position at animation END  (0,0 = rest).")]
        public Vector2 positionTo        = Vector2.zero;
        public float   positionDuration  = 0.35f;
        public float   positionDelay     = 0f;
        public EaseSetting positionEase  = new EaseSetting
        {
#if DOTWEEN
            ease = Ease.OutCubic
#endif
        };

        // ── Rotation (Euler Z) ─────────────────────────────────────────────
        public bool  rotationEnabled    = false;
        public float rotationFrom       = -6f;
        public float rotationTo         = 0f;
        public float rotationDuration   = 0.35f;
        public float rotationDelay      = 0f;
        public EaseSetting rotationEase = new EaseSetting
        {
#if DOTWEEN
            ease = Ease.OutCubic
#endif
        };

        // ── Per-property timing accessors ──────────────────────────────────

        public float GetAlphaDuration()    => useGlobalTiming ? duration   : alphaDuration;
        public float GetAlphaDelay()       => useGlobalTiming ? extraDelay : alphaDelay;
        public float GetScaleDuration()    => useGlobalTiming ? duration   : scaleDuration;
        public float GetScaleDelay()       => useGlobalTiming ? extraDelay : scaleDelay;
        public float GetPositionDuration() => useGlobalTiming ? duration   : positionDuration;
        public float GetPositionDelay()    => useGlobalTiming ? extraDelay : positionDelay;
        public float GetRotationDuration() => useGlobalTiming ? duration   : rotationDuration;
        public float GetRotationDelay()    => useGlobalTiming ? extraDelay : rotationDelay;

        /// <summary>
        /// Returns the duration of the longest-running property.
        /// Used by Sequential block mode to advance accumulatedDelay.
        /// </summary>
        public float GetMaxPropertySpan()
        {
            float max = 0f;
            if (alphaEnabled)    max = Mathf.Max(max, GetAlphaDelay()    + GetAlphaDuration());
            if (scaleEnabled)    max = Mathf.Max(max, GetScaleDelay()    + GetScaleDuration());
            if (positionEnabled) max = Mathf.Max(max, GetPositionDelay() + GetPositionDuration());
            if (rotationEnabled) max = Mathf.Max(max, GetRotationDelay() + GetRotationDuration());
            return max > 0f ? max : GetAlphaDelay() + GetAlphaDuration(); // fallback
        }

        // ──────────────────────────────────────────────────────────────────

        /// <summary>Returns a copy with every from↔to swapped (for auto-disappear config).</summary>
        public TransitionElementConfig Mirrored()
        {
            return new TransitionElementConfig
            {
                useGlobalTiming  = useGlobalTiming,
                duration         = duration,
                extraDelay       = extraDelay,

                alphaEnabled     = alphaEnabled,
                alphaFrom        = alphaTo,
                alphaTo          = alphaFrom,
                alphaDuration    = alphaDuration,
                alphaDelay       = alphaDelay,
                alphaEase        = alphaEase,

                scaleEnabled     = scaleEnabled,
                scaleFrom        = scaleTo,
                scaleTo          = scaleFrom,
                scaleDuration    = scaleDuration,
                scaleDelay       = scaleDelay,
                scaleEase        = scaleEase,

                positionEnabled  = positionEnabled,
                positionFrom     = positionTo,
                positionTo       = positionFrom,
                positionDuration = positionDuration,
                positionDelay    = positionDelay,
                positionEase     = positionEase,

                rotationEnabled  = rotationEnabled,
                rotationFrom     = rotationTo,
                rotationTo       = rotationFrom,
                rotationDuration = rotationDuration,
                rotationDelay    = rotationDelay,
                rotationEase     = rotationEase,
            };
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  BlockSettings
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class BlockSettings
    {
        public bool          enabled              = true;
        public BlockPlayMode playMode             = BlockPlayMode.StaggeredParallel;
        [Tooltip("Delay between successive elements' start times (seconds).")]
        public float         stagger              = 0.055f;
        [Tooltip("Max random jitter added to each element stagger (± jitter/2).")]
        public float         staggerRandomJitter  = 0f;
        [Space]
        public UnityEvent    onStart              = new UnityEvent();
        public UnityEvent    onComplete           = new UnityEvent();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ScreenAnimElement — entry in ScreenAnimator.elements list
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class ScreenAnimElement
    {
        [Tooltip("The UI RectTransform to animate.")]
        public RectTransform target;

        // Appear
        [Tooltip("Use the preset's appear config instead of the one below.")]
        public bool usePresetForAppear = true;
        public TransitionElementConfig appearConfig = new TransitionElementConfig();

        // Disappear
        [Tooltip("Auto-flip the appear config from↔to for disappear.")]
        public bool mirrorAppearForDisappear = true;
        [Tooltip("Use the preset's disappear config (takes priority over mirrorAppear).")]
        public bool usePresetForDisappear = false;
        public TransitionElementConfig disappearConfig = new TransitionElementConfig
        {
            alphaEnabled = true, alphaFrom = 1f, alphaTo = 0f,
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  IdleAnimConfig — looping visual effect while screen is visible
    //  (structure ready; runtime playback coming in a future update)
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class IdleAnimConfig
    {
        public RectTransform target;
        public bool          playOnVisible = true;

        [Header("Breathing  (scale pulse)")]
        public bool        breathingEnabled = false;
        public float       breathingScale   = 1.05f;
        public float       breathingPeriod  = 2.0f;
        public EaseSetting breathingEase    = new EaseSetting();

        [Header("Floating  (position bob)")]
        public bool        floatingEnabled  = false;
        public Vector2     floatOffset      = new Vector2(0f, 8f);
        public float       floatPeriod      = 2.5f;
        public EaseSetting floatEase        = new EaseSetting();

        [Header("Rotation Rock  (pendulum)")]
        public bool        rotationEnabled  = false;
        public float       rotationAngle    = 5f;
        public float       rotationPeriod   = 2.0f;
        public EaseSetting rotationEase     = new EaseSetting();

        [Header("Color Pulse  (alpha)")]
        public bool        colorPulseEnabled  = false;
        public float       colorPulseMinAlpha = 0.7f;
        public float       colorPulsePeriod   = 1.5f;
        public EaseSetting colorPulseEase     = new EaseSetting();
    }
}

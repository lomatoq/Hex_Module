using UnityEngine;

namespace HexWords.Gameplay
{
    [CreateAssetMenu(menuName = "HexWords/Hex Cell Anim Config", fileName = "HexCellAnimConfig")]
    public class HexCellAnimConfig : ScriptableObject
    {
        // ── On Selected (hold scale) ───────────────────────────────────────
        [Header("Selected — Hold Scale")]
        [Tooltip("Target scale when cell is selected and held (e.g. 1.1 = +10%)")]
        public float selectHoldScale    = 1.10f;
        [Tooltip("Duration of the scale-in animation")]
        public float selectHoldDuration = 0.15f;
        [Tooltip("Ease curve for the scale-in (x=time 0-1, y=scale 0-1)")]
        public AnimationCurve selectHoldCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // ── On Selected — Circle Fill ──────────────────────────────────────
        [Header("Selected — Circle Fill")]
        [Tooltip("Duration of the circle fill scale animation")]
        public float fillDuration        = 0.14f;
        [Tooltip("Final local scale of CircleFill (size it at 1.0 to fully cover the mask)")]
        public float fillFinalScale      = 1.0f;
        [Tooltip("Ease curve for the fill scale (start at 0, end at 1)")]
        public AnimationCurve fillCurve  = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // ── On Accepted (flash + punch) ────────────────────────────────────
        [Header("Accepted — Flash & Punch")]
        public float acceptFlashDuration  = 0.25f;
        public float acceptPunchScale     = 0.08f;
        public float acceptPunchDuration  = 0.175f; // acceptFlashDuration * 0.7
        public int   acceptPunchVibrato   = 3;
        [Range(0f, 1f)]
        public float acceptElasticity     = 0.5f;

        // ── On Rejected (shake) ────────────────────────────────────────────
        [Header("Rejected — Shake")]
        public float rejectFlashDuration  = 0.3f;
        public float shakePositionStrength = 5f;
        public float shakeDuration         = 0.25f;
        public int   shakeVibrato          = 18;

        // ── Word accepted — state color flash ─────────────────────────────
        [Header("Word Accepted — State Color")]
        [Tooltip("Extra seconds the state color holds AFTER drops land before fading.")]
        public float wordColorHoldExtra      = 0.25f;
        [Tooltip("Duration of the fade-back from state color to neutral.")]
        public float wordColorReturnDuration = 0.35f;

        // ── Word accepted — sequential bounce ────────────────────────────
        [Header("Word Accepted — Sequential Bounce")]
        [Tooltip("Punch scale magnitude per cell (e.g. 0.20 = +20%)")]
        public float         wordBouncePunchScale     = 0.20f;
        [Tooltip("Duration of each cell's punch animation")]
        public float         wordBounceDuration       = 0.20f;
        [Tooltip("Delay between successive cells' bounces (stagger)")]
        public float         wordBounceStagger        = 0.06f;
        [Tooltip("Ease curve for the punch — typically a quick ease-out")]
        public AnimationCurve wordBounceCurve         = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Delay between cell bounce start and letter bounce start (organic offset).")]
        public float          wordLetterBounceOffset  = 0.04f;

        // ════════════════════════════════════════════════════════════════
        //  Advanced Cell Animation
        //  Master toggle must be ON for any sub-feature to activate.
        // ════════════════════════════════════════════════════════════════

        [Header("═══ Advanced Cell Animation ═══")]
        [Tooltip("Master toggle — enables path ripple and idle animations on selected cells.")]
        public bool enableAdvancedCellAnim = false;

        // ── Idle scale ────────────────────────────────────────────────────
        [Header("  Idle Scale  (breathing while selected)")]
        [Tooltip("Slow scale oscillation while a cell is part of the active path.")]
        public bool idleScaleEnabled = true;
        [Tooltip("Peak scale during idle breathing (should be >= selectHoldScale).")]
        public float idleScalePeak         = 1.14f;
        [Tooltip("Half-period: time to go from holdScale → peak (then same time back, Yoyo).")]
        public float idleScaleHalfPeriod   = 0.70f;
        [Tooltip("Ease curve for the scale-up / scale-down half of the breathing cycle.")]
        public AnimationCurve idleScaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // ── Idle rotation ─────────────────────────────────────────────────
        [Header("  Idle Rotation  (pendulum while selected)")]
        [Tooltip("Slow pendulum rotation while a cell is part of the active path.")]
        public bool idleRotationEnabled = false;
        [Tooltip("Max rotation angle (degrees) to either side of zero.")]
        public float idleRotationAngle      = 3.5f;
        [Tooltip("Half-period for the full pendulum swing (0 → +angle takes this long).")]
        public float idleRotationHalfPeriod = 1.0f;
        [Tooltip("Ease curve for each quarter of the pendulum swing.")]
        public AnimationCurve idleRotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }
}

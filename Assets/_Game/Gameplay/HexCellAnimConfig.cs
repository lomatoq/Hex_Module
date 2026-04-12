using UnityEngine;

namespace HexWords.Gameplay
{
    [CreateAssetMenu(menuName = "HexWords/Hex Cell Anim Config", fileName = "HexCellAnimConfig")]
    public class HexCellAnimConfig : ScriptableObject
    {
        // ── On Selected (punch scale) ──────────────────────────────────────
        [Header("Selected — Punch Scale")]
        [Tooltip("Punch magnitude (fraction of original scale, e.g. 0.13 = +13%)")]
        public float selectPunchScale    = 0.13f;
        [Tooltip("Duration of the punch animation")]
        public float selectPunchDuration = 0.18f;
        [Tooltip("Number of vibrations")]
        public int   selectPunchVibrato  = 5;
        [Tooltip("Elasticity of the punch (0 = no overshoot, 1 = max)")]
        [Range(0f, 1f)]
        public float selectElasticity    = 0.5f;

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

        // ── Letter Sorting Order ───────────────────────────────────────────
        [Header("Letter Sorting")]
        [Tooltip("Enable overrideSorting on the letter Canvas so letters render above the swipe trail")]
        public bool  letterAboveTrail      = true;
        [Tooltip("Sorting order for the letter Canvas when letterAboveTrail is enabled")]
        public int   letterSortingOrder    = 10;
    }
}

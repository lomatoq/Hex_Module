// Remove this comment after installing DOTween via Asset Store
#define DOTWEEN

#if UNITY_EDITOR
using UnityEngine;
#if DOTWEEN
using DG.Tweening;
#endif

namespace HexWords.UI.Transitions
{
    /// <summary>
    /// Pure-math ease evaluator for edit-mode animation preview (no DOTween runtime required).
    /// Mirrors DOTween's ease curves so preview looks identical to runtime playback.
    /// </summary>
    public static class EaseEvaluator
    {
#if DOTWEEN
        public static float Evaluate(Ease ease, float t)
        {
            t = Mathf.Clamp01(t);
            return ease switch
            {
                Ease.Linear      => t,
                Ease.InSine      => 1f - Mathf.Cos(t * Mathf.PI * 0.5f),
                Ease.OutSine     => Mathf.Sin(t * Mathf.PI * 0.5f),
                Ease.InOutSine   => -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f,
                Ease.InQuad      => t * t,
                Ease.OutQuad     => t * (2f - t),
                Ease.InOutQuad   => t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t,
                Ease.InCubic     => t * t * t,
                Ease.OutCubic    => OutCubic(t),
                Ease.InOutCubic  => t < 0.5f
                    ? 4f * t * t * t
                    : (t - 1f) * (2f * t - 2f) * (2f * t - 2f) + 1f,
                Ease.InQuart     => t * t * t * t,
                Ease.OutQuart    => 1f - Pow4(t - 1f),
                Ease.InOutQuart  => t < 0.5f ? 8f * t * t * t * t : 1f - 8f * Pow4(t - 1f),
                Ease.InQuint     => t * t * t * t * t,
                Ease.OutQuint    => 1f + Pow5(t - 1f),
                Ease.InOutQuint  => t < 0.5f ? 16f * Pow5(t) : 1f + 16f * Pow5(t - 1f),
                Ease.InExpo      => t == 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f),
                Ease.OutExpo     => t == 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t),
                Ease.InOutExpo   => t == 0f ? 0f : t == 1f ? 1f
                    : t < 0.5f
                        ? Mathf.Pow(2f, 20f * t - 10f) * 0.5f
                        : (2f - Mathf.Pow(2f, -20f * t + 10f)) * 0.5f,
                Ease.InBack      => InBack(t),
                Ease.OutBack     => OutBack(t),
                Ease.InOutBack   => InOutBack(t),
                Ease.InElastic   => InElastic(t),
                Ease.OutElastic  => OutElastic(t),
                Ease.InBounce    => 1f - OutBounce(1f - t),
                Ease.OutBounce   => OutBounce(t),
                Ease.InOutBounce => t < 0.5f
                    ? (1f - OutBounce(1f - 2f * t)) * 0.5f
                    : (1f + OutBounce(2f * t - 1f)) * 0.5f,
                _                => t,   // Flash, INTERNAL_Zero, Unset → linear
            };
        }
#endif

        // ── Private helpers ────────────────────────────────────────────────

        private static float Pow4(float x) => x * x * x * x;
        private static float Pow5(float x) => x * x * x * x * x;
        private static float OutCubic(float t) { float f = t - 1f; return f * f * f + 1f; }

        private const float C1 = 1.70158f;
        private const float C2 = C1 * 1.525f;
        private const float C3 = C1 + 1f;

        private static float InBack(float t)    => C3 * t * t * t - C1 * t * t;
        private static float OutBack(float t)   { float f = t - 1f; return 1f + C3 * f * f * f + C1 * f * f; }
        private static float InOutBack(float t) =>
            t < 0.5f
                ? Pow2(2f * t)   * ((C2 + 1f) * 2f * t - C2) * 0.5f
                : (Pow2(2f * t - 2f) * ((C2 + 1f) * (t * 2f - 2f) + C2) + 2f) * 0.5f;
        private static float Pow2(float x) => x * x;

        private const float ElasticP  = 2f * Mathf.PI / 3f;
        private const float ElasticPi = 2f * Mathf.PI / 4.5f;

        private static float OutElastic(float t)
        {
            if (t == 0f) return 0f;
            if (t == 1f) return 1f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * ElasticP) + 1f;
        }

        private static float InElastic(float t)
        {
            if (t == 0f) return 0f;
            if (t == 1f) return 1f;
            return -Mathf.Pow(2f, 10f * t - 10f) * Mathf.Sin((t * 10f - 10.75f) * ElasticP);
        }

        private static float OutBounce(float t)
        {
            const float n = 7.5625f, d = 2.75f;
            if (t < 1f / d)   return n * t * t;
            if (t < 2f / d) { t -= 1.5f   / d; return n * t * t + 0.75f; }
            if (t < 2.5f / d) { t -= 2.25f / d; return n * t * t + 0.9375f; }
            t -= 2.625f / d; return n * t * t + 0.984375f;
        }
    }
}
#endif

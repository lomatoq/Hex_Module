// Remove this comment after installing DOTween via Asset Store
#define DOTWEEN

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using HexWords.Gameplay;
using UnityEngine;
#if DOTWEEN
using DG.Tweening;
#endif

namespace HexWords.UI.Transitions
{
    /// <summary>
    /// Animates the hex cells of a <see cref="GridView"/> in a wave pattern.
    ///
    /// Attach to the same GameObject as GridView (or reference it via Inspector).
    /// Call <see cref="WaveInAsync"/> / <see cref="WaveOutAsync"/> to animate,
    /// or hook them into <see cref="ScreenAnimator"/> UnityEvents.
    /// </summary>
    public class CellGridAnimator : MonoBehaviour
    {
        [SerializeField] private GridView               gridView;
        [SerializeField] private CellGridTransitionConfig config;

        [Header("Speed")]
        [Tooltip("Local speed multiplier (stacked with ScreenTransitionManager.GlobalSpeedMultiplier).")]
        [SerializeField] private float localSpeedMultiplier = 1f;

        public float GlobalSpeedMultiplier { get; set; } = 1f;
        private float SpeedMul => Mathf.Max(0.01f, GlobalSpeedMultiplier * localSpeedMultiplier);

        private CancellationTokenSource _cts;

        // ── Public async API ───────────────────────────────────────────────

        /// <summary>Cells wave in using the appear config.</summary>
        public async UniTask WaveInAsync(CancellationToken ct = default)
        {
            if (gridView == null || config == null) return;
            var order = BuildOrder(config.waveOrigin, reverse: false);
            await PlayWave(order, config.cellAppearConfig, appear: true, ct);
        }

        /// <summary>Cells wave out using the disappear config.</summary>
        public async UniTask WaveOutAsync(CancellationToken ct = default)
        {
            if (gridView == null || config == null) return;
            bool rev    = config.mirrorOriginForDisappear;
            var  order  = BuildOrder(config.waveOrigin, reverse: rev);
            var  cfg    = config.GetDisappearConfig();
            await PlayWave(order, cfg, appear: false, ct);
        }

        // ── Immediate (no animation) ───────────────────────────────────────

        public void WaveInImmediate()
        {
            if (gridView == null || config == null) return;
            SnapAll(config.cellAppearConfig, appear: true);
        }

        public void WaveOutImmediate()
        {
            if (gridView == null || config == null) return;
            SnapAll(config.GetDisappearConfig(), appear: false);
        }

        // ── UnityEvent-compatible wrappers (no async, fire-and-forget) ─────

        public void WaveIn()  => WaveInAsync(destroyCancellationToken).Forget();
        public void WaveOut() => WaveOutAsync(destroyCancellationToken).Forget();

        // ── Core ───────────────────────────────────────────────────────────

        private async UniTask PlayWave(
            List<RectTransform>    order,
            TransitionElementConfig cfg,
            bool                   appear,
            CancellationToken      externalCt)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
            var ct = _cts.Token;

            float speedMul   = SpeedMul;
            float stagger    = config.cellStagger / speedMul;
            float jitterAmp  = config.cellStaggerRandomJitter;
            float dur        = cfg.duration / speedMul;
            float maxEndTime = 0f;

            for (int i = 0; i < order.Count; i++)
            {
                var rt = order[i];
                if (rt == null) continue;

                float jitter     = jitterAmp > 0f
                    ? UnityEngine.Random.Range(-jitterAmp * 0.5f, jitterAmp * 0.5f)
                    : 0f;
                float startDelay = i * stagger + jitter + cfg.extraDelay / speedMul;
                float endTime    = startDelay + dur;
                if (endTime > maxEndTime) maxEndTime = endTime;

                PlayCellTween(rt, cfg, startDelay, dur);
            }

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(maxEndTime), cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                // Caller cancelled — leave cells wherever they are
            }
        }

        // ── Per-cell tween ─────────────────────────────────────────────────

        private void PlayCellTween(RectTransform rt, TransitionElementConfig cfg, float delay, float dur)
        {
#if DOTWEEN
            DOTween.Kill(rt, complete: false);

            if (cfg.alphaEnabled)
            {
                var cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = cfg.alphaFrom;
                cfg.alphaEase.Apply(
                    cg.DOFade(cfg.alphaTo, dur)
                      .SetDelay(delay)
                      .SetTarget(rt));
            }

            if (cfg.scaleEnabled)
            {
                rt.localScale = cfg.scaleFrom;
                cfg.scaleEase.Apply(
                    rt.DOScale(cfg.scaleTo, dur)
                      .SetDelay(delay)
                      .SetTarget(rt));
            }

            if (cfg.positionEnabled)
            {
                var rest = rt.anchoredPosition;
                rt.anchoredPosition = rest + cfg.positionFrom;
                cfg.positionEase.Apply(
                    rt.DOAnchorPos(rest + cfg.positionTo, dur)
                      .SetDelay(delay)
                      .SetTarget(rt));
            }

            if (cfg.rotationEnabled)
            {
                rt.localRotation = Quaternion.Euler(0f, 0f, cfg.rotationFrom);
                cfg.rotationEase.Apply(
                    rt.DOLocalRotate(new Vector3(0f, 0f, cfg.rotationTo), dur)
                      .SetDelay(delay)
                      .SetTarget(rt));
            }
#endif
        }

        // ── Snap all ──────────────────────────────────────────────────────

        private void SnapAll(TransitionElementConfig cfg, bool appear)
        {
            foreach (var pair in gridView.CellViews)
            {
                var view = pair.Value;
                if (view == null) continue;
                var rt = (RectTransform)view.transform;
#if DOTWEEN
                DOTween.Kill(rt, complete: false);
#endif
                if (cfg.alphaEnabled)
                {
                    var cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
                    cg.alpha = cfg.alphaTo;
                }
                if (cfg.scaleEnabled)    rt.localScale    = cfg.scaleTo;
                if (cfg.rotationEnabled) rt.localRotation = Quaternion.Euler(0f, 0f, cfg.rotationTo);
            }
        }

        // ── Wave order builders ────────────────────────────────────────────

        private List<RectTransform> BuildOrder(WaveOrigin origin, bool reverse)
        {
            var views = gridView.CellViews;
            var list  = new List<(RectTransform rt, float sortKey)>(views.Count);

            foreach (var pair in views)
            {
                var view = pair.Value;
                if (view == null) continue;
                var rt  = (RectTransform)view.transform;
                var pos = rt.anchoredPosition;

                float key = origin switch
                {
                    WaveOrigin.TopToBottom      =>  pos.y,           // highest y first
                    WaveOrigin.BottomToTop      => -pos.y,
                    WaveOrigin.LeftToRight      =>  pos.x,
                    WaveOrigin.RightToLeft      => -pos.x,
                    WaveOrigin.CenterOutward    =>  pos.magnitude,   // centre first (near=low key)
                    WaveOrigin.OutwardToCenter  => -pos.magnitude,
                    WaveOrigin.DirectionalAngle => ProjectOnAngle(pos, config.customAngle),
                    WaveOrigin.ExplicitOrder    =>  0f,              // order unchanged
                    _                           =>  0f,
                };

                list.Add((rt, key));
            }

            // Sort ascending on key (then optionally reverse for disappear)
            list.Sort((a, b) => a.sortKey.CompareTo(b.sortKey));

            if (reverse) list.Reverse();

            var result = new List<RectTransform>(list.Count);
            foreach (var (rt, _) in list) result.Add(rt);
            return result;
        }

        /// <summary>
        /// Projects <paramref name="pos"/> onto a unit vector at <paramref name="angleDeg"/> degrees.
        /// Cells with a lower projection value will animate first.
        /// </summary>
        private static float ProjectOnAngle(Vector2 pos, float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return pos.x * Mathf.Cos(rad) + pos.y * Mathf.Sin(rad);
        }

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}

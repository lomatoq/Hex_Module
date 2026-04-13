// Remove this comment after installing DOTween via Asset Store
#define DOTWEEN

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if DOTWEEN
using DG.Tweening;
#endif

namespace HexWords.UI.Transitions
{
    /// <summary>
    /// Placed on a screen or popup root.  Animates a list of child UI elements
    /// when the screen appears or disappears.
    ///
    /// Quick start:
    ///   1. Add this component to your screen/popup root GameObject.
    ///   2. Assign a TransitionPreset SO to the Preset field.
    ///   3. Add child RectTransforms to the Animated Elements list.
    ///   4. Enable "Play On Enable" — animation fires whenever the object is activated.
    ///      OR drive it explicitly via ScreenTransitionManager.ShowScreen().
    /// </summary>
    public class ScreenAnimator : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────

        [Header("Preset  (provides defaults for each element)")]
        [SerializeField] private TransitionPreset preset;

        [Header("Animated Elements  (ordered — stagger follows this order)")]
        [SerializeField] private List<ScreenAnimElement> elements = new List<ScreenAnimElement>();

        [Header("Appear Block")]
        [SerializeField] private BlockSettings appearSettings = new BlockSettings();

        [Header("Disappear Block")]
        [SerializeField] private BlockSettings disappearSettings = new BlockSettings { stagger = 0.04f };

        [Header("Quick Block  (tab switches, lightweight)")]
        [SerializeField] private BlockSettings quickSettings = new BlockSettings { stagger = 0.03f };

        [Header("Source-based Delay")]
        [Tooltip("Delay applied the very FIRST time this screen appears (e.g. after SplashScreen).\nSet 0 to disable.")]
        [SerializeField] private float firstAppearDelay = 0f;

        [Tooltip("If the previously-shown screen's GameObject is in this list, comingFromDelay is applied.\nWorks only when both screens go through ScreenTransitionManager.\nAccepts ANY GameObject — no ScreenAnimator required on the source.")]
        [SerializeField] private List<GameObject> delayWhenComingFrom = new List<GameObject>();
        [Tooltip("Seconds to wait before appearing when coming from one of the GameObjects above.")]
        [SerializeField] private float comingFromDelay = 0.5f;

        [Header("Options")]
        [Tooltip("Automatically manage a CanvasGroup on this GameObject to block raycasts during transitions.")]
        [SerializeField] private bool manageCanvasGroup = true;

        [Tooltip("What to do when a new transition starts while one is already running.")]
        [SerializeField] private InterruptStrategy interruptStrategy = InterruptStrategy.CancelAndSnap;

        [Tooltip("Local speed multiplier (multiplied with ScreenTransitionManager.GlobalSpeedMultiplier).")]
        [SerializeField] private float localSpeedMultiplier = 1f;

        [Header("Auto-Play")]
        [Tooltip("Snap elements to disappeared state on Awake — prevents flicker on first frame.")]
        [SerializeField] private bool startHidden = true;

        [Tooltip("Run Appear animation automatically every time this object is enabled (SetActive true).")]
        [SerializeField] private bool playOnEnable = true;

        [Tooltip("Snap elements to disappeared state when this object is disabled.")]
        [SerializeField] private bool resetOnDisable = true;

        [Header("Idle Animations  (coming soon)")]
        [Tooltip("Looping visual effects active while the screen is visible.\nImplementation will be added in a future update.")]
        [SerializeField] private List<IdleAnimConfig> idleAnims = new List<IdleAnimConfig>();

        // ── Runtime state ──────────────────────────────────────────────────

        private CanvasGroup _rootCanvasGroup;
        private CancellationTokenSource _cts;
        private bool _isAnimating;
        private bool _awoken;

        // Set by ScreenTransitionManager before calling AppearAsync
        private GameObject _pendingSource;
        // Set by ScreenTransitionManager to prevent double-play when it drives the animation
        private bool _suppressNextOnEnable;
        // Tracks whether Appear has ever been called (for firstAppearDelay)
        private bool _hasEverAppeared;

        private readonly Dictionary<RectTransform, Vector2>    _restPositions = new();
        private readonly Dictionary<RectTransform, CanvasGroup> _elementCGs    = new();

        // ── Public API ─────────────────────────────────────────────────────

        public float GlobalSpeedMultiplier { get; set; } = 1f;
        public bool  IsAnimating           => _isAnimating;
        public bool  IsVisible             { get; private set; }

        // Exposed for editor preview
        public IReadOnlyList<ScreenAnimElement> Elements         => elements;
        public TransitionPreset                 Preset           => preset;
        public BlockSettings                    AppearSettings   => appearSettings;
        public BlockSettings                    DisappearSettings => disappearSettings;

        // ── Manager handshake ──────────────────────────────────────────────

        /// <summary>
        /// Call before AppearAsync to tell this animator which screen was previously shown.
        /// Pass the source's GameObject — no ScreenAnimator required on the source.
        /// </summary>
        public void SetPreviousScreen(ScreenAnimator prev)
            => _pendingSource = prev != null ? prev.gameObject : null;

        /// <summary>
        /// Call before SetActive(true) when the manager is about to drive AppearAsync manually.
        /// Prevents OnEnable from triggering a second parallel animation.
        /// </summary>
        public void SuppressNextOnEnable() => _suppressNextOnEnable = true;

        // ── Async transitions ──────────────────────────────────────────────

        public async UniTask AppearAsync(CancellationToken ct = default)
        {
            float preDelay = ComputePreDelay();
            if (preDelay > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(preDelay), cancellationToken: ct);
                if (ct.IsCancellationRequested) return;
            }

            await RunBlock(appearSettings, GetAppearConfig, appear: true, ct);
            if (!ct.IsCancellationRequested)
            {
                IsVisible        = true;
                _hasEverAppeared = true;
            }
        }

        public async UniTask DisappearAsync(CancellationToken ct = default)
        {
            await RunBlock(disappearSettings, GetDisappearConfig, appear: false, ct);
        }

        public async UniTask QuickAppearAsync(CancellationToken ct = default)
        {
            await RunBlock(quickSettings, GetQuickConfig, appear: true, ct);
            if (!ct.IsCancellationRequested) IsVisible = true;
        }

        public async UniTask QuickDisappearAsync(CancellationToken ct = default)
        {
            await RunBlock(quickSettings, i => GetQuickConfig(i).Mirrored(), appear: false, ct);
        }

        // ── Immediate (no animation) ───────────────────────────────────────

        public void AppearImmediate()
        {
            CancelCurrent(snap: false);
            SnapAll(appear: true);
            SetRootInteractable(true);
            IsVisible = true;
        }

        public void DisappearImmediate()
        {
            CancelCurrent(snap: false);
            SnapAll(appear: false);
            SetRootInteractable(false);
            IsVisible = false;
        }

        // ── Unity lifecycle ────────────────────────────────────────────────

        private void Awake()
        {
            _awoken = true;
            EnsureRootCanvasGroup();
            CacheRestPositions();

            if (startHidden)
            {
                SnapAll(appear: false);
                SetRootInteractable(false);
            }
        }

        private void OnEnable()
        {
            if (!_awoken) return;

            // Manager called SuppressNextOnEnable() — it will drive AppearAsync itself
            if (_suppressNextOnEnable)
            {
                _suppressNextOnEnable = false;
                return;
            }

            if (playOnEnable)
                AppearAsync(destroyCancellationToken).Forget();
        }

        private void OnDisable()
        {
            CancelCurrent(snap: false);

            if (resetOnDisable)
            {
                SnapAll(appear: false);
                IsVisible = false;
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
#if UNITY_EDITOR
            EditorStopAnimatedPreview();
#endif
        }

        // ── Pre-appear delay ───────────────────────────────────────────────

        /// <summary>
        /// Returns the delay to apply before the appear animation starts.
        /// Priority: firstAppearDelay (first run ever) > source-based delay > 0.
        /// </summary>
        private float ComputePreDelay()
        {
            // ── First-ever appear (e.g. after SplashScreen) ────────────────
            if (!_hasEverAppeared && firstAppearDelay > 0f)
            {
                _pendingSource = null; // consume source too, not needed
                return firstAppearDelay;
            }

            // ── Source-based (manager told us who was previous) ────────────
            var src = _pendingSource;
            _pendingSource = null;

            if (src == null) return 0f;
            if (delayWhenComingFrom == null || delayWhenComingFrom.Count == 0) return 0f;
            return delayWhenComingFrom.Contains(src) ? comingFromDelay : 0f;
        }

        // ── Core block runner ──────────────────────────────────────────────

        private async UniTask RunBlock(
            BlockSettings                      settings,
            Func<int, TransitionElementConfig> getConfig,
            bool                               appear,
            CancellationToken                  externalCt)
        {
            if (!settings.enabled)
            {
                SnapAll(appear);
                if (appear) SetRootInteractable(true);
                return;
            }

            // ── Handle interrupt ───────────────────────────────────────────
            if (_isAnimating)
            {
                switch (interruptStrategy)
                {
                    case InterruptStrategy.FinishCurrent:
                        await UniTask.WaitUntil(() => !_isAnimating, cancellationToken: externalCt);
                        break;
                    case InterruptStrategy.CancelAndSnap:
                        CancelCurrent(snap: true);
                        break;
                    case InterruptStrategy.CancelAndReverse:
                        CancelCurrent(snap: false);
                        break;
                }
            }

            if (externalCt.IsCancellationRequested) return;

            // ── Setup ──────────────────────────────────────────────────────
            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
            var ct = _cts.Token;
            _isAnimating = true;

            if (!appear) SetRootInteractable(false);
            settings.onStart?.Invoke();

            float speedMul         = Mathf.Max(0.01f, GlobalSpeedMultiplier * localSpeedMultiplier);
            float accumulatedDelay = 0f;
            float maxEndTime       = 0f;

            for (int i = 0; i < elements.Count; i++)
            {
                var elem = elements[i];
                if (elem == null || elem.target == null) continue;

                var   config = getConfig(i);
                float span   = config.GetMaxPropertySpan() / speedMul;

                float jitter = settings.staggerRandomJitter > 0f
                    ? UnityEngine.Random.Range(
                        -settings.staggerRandomJitter * 0.5f,
                         settings.staggerRandomJitter * 0.5f)
                    : 0f;

                float startDelay;
                if (settings.playMode == BlockPlayMode.Sequential)
                {
                    startDelay       = accumulatedDelay + config.extraDelay / speedMul;
                    accumulatedDelay = startDelay + span;
                }
                else
                {
                    startDelay = i * settings.stagger / speedMul
                               + config.extraDelay    / speedMul
                               + jitter;
                }

                float endTime = startDelay + span;
                if (endTime > maxEndTime) maxEndTime = endTime;

                PlayElementTween(elem, config, startDelay, speedMul);
            }

            // No elements → snap and complete immediately
            if (maxEndTime <= 0f)
            {
                SnapAll(appear);
                _isAnimating = false;
                if (appear) SetRootInteractable(true);
                settings.onComplete?.Invoke();
                return;
            }

            // ── Await ──────────────────────────────────────────────────────
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(maxEndTime), cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                _isAnimating = false;
                return;
            }

            _isAnimating = false;
            if (appear) SetRootInteractable(true);
            settings.onComplete?.Invoke();
        }

        // ── Per-element tween ──────────────────────────────────────────────

        /// <summary>
        /// Fires all DOTween tweens for one element.
        /// Each property uses its own duration/delay from the config
        /// (respecting <see cref="TransitionElementConfig.useGlobalTiming"/>).
        /// </summary>
        private void PlayElementTween(
            ScreenAnimElement       elem,
            TransitionElementConfig config,
            float                   startDelay,   // stagger offset for this element (speed-adjusted, seconds)
            float                   speedMul)
        {
            var rt = elem.target;
            if (rt == null || !rt.gameObject.activeInHierarchy) return;

#if DOTWEEN
            DOTween.Kill(rt, complete: false);

            Vector2 restPos = GetRestPosition(rt);

            if (config.alphaEnabled)
            {
                float dur   = config.GetAlphaDuration() / speedMul;
                float delay = startDelay + config.GetAlphaDelay() / speedMul;
                var cg = GetOrAddCanvasGroup(rt);
                if (cg != null)
                {
                    cg.alpha = config.alphaFrom;
                    config.alphaEase.Apply(
                        cg.DOFade(config.alphaTo, dur)
                          .SetDelay(delay)
                          .SetTarget(rt));
                }
            }

            if (config.scaleEnabled)
            {
                float dur   = config.GetScaleDuration() / speedMul;
                float delay = startDelay + config.GetScaleDelay() / speedMul;
                rt.localScale = config.scaleFrom;
                config.scaleEase.Apply(
                    rt.DOScale(config.scaleTo, dur)
                      .SetDelay(delay)
                      .SetTarget(rt));
            }

            if (config.positionEnabled)
            {
                float dur   = config.GetPositionDuration() / speedMul;
                float delay = startDelay + config.GetPositionDelay() / speedMul;
                rt.anchoredPosition = restPos + config.positionFrom;
                config.positionEase.Apply(
                    rt.DOAnchorPos(restPos + config.positionTo, dur)
                      .SetDelay(delay)
                      .SetTarget(rt));
            }

            if (config.rotationEnabled)
            {
                float dur   = config.GetRotationDuration() / speedMul;
                float delay = startDelay + config.GetRotationDelay() / speedMul;
                rt.localRotation = Quaternion.Euler(0f, 0f, config.rotationFrom);
                config.rotationEase.Apply(
                    rt.DOLocalRotate(new Vector3(0f, 0f, config.rotationTo), dur)
                      .SetDelay(delay)
                      .SetTarget(rt));
            }
#endif
        }

        // ── Snap (immediate state) ─────────────────────────────────────────

        private void SnapAll(bool appear)
        {
            for (int i = 0; i < elements.Count; i++)
            {
                var elem = elements[i];
                if (elem == null || elem.target == null) continue;
                SnapElement(elem.target, appear ? GetAppearConfig(i) : GetDisappearConfig(i));
            }
        }

        private void SnapElement(RectTransform rt, TransitionElementConfig config)
        {
            if (rt == null) return;
#if DOTWEEN
            DOTween.Kill(rt, complete: false);
#endif
            Vector2 restPos = GetRestPosition(rt);

            if (config.alphaEnabled)
            {
                var cg = GetOrAddCanvasGroup(rt);
                if (cg != null) cg.alpha = config.alphaTo;
            }
            if (config.scaleEnabled)
                rt.localScale = config.scaleTo;
            if (config.positionEnabled)
                rt.anchoredPosition = restPos + config.positionTo;
            if (config.rotationEnabled)
                rt.localRotation = Quaternion.Euler(0f, 0f, config.rotationTo);
        }

        // ── Config selectors ───────────────────────────────────────────────

        public TransitionElementConfig GetAppearConfig(int i)
        {
            var elem = elements[i];
            if (elem.usePresetForAppear && preset != null)
                return preset.appearDefaultConfig;
            return elem.appearConfig;
        }

        public TransitionElementConfig GetDisappearConfig(int i)
        {
            var elem = elements[i];
            if (elem.mirrorAppearForDisappear)
                return GetAppearConfig(i).Mirrored();
            if (elem.usePresetForDisappear && preset != null)
                return preset.disappearDefaultConfig;
            return elem.disappearConfig;
        }

        private TransitionElementConfig GetQuickConfig(int i)
        {
            if (preset != null) return preset.quickDefaultConfig;
            var c = GetAppearConfig(i);
            return new TransitionElementConfig
            {
                duration     = c.duration * 0.5f,
                alphaEnabled = c.alphaEnabled,
                alphaFrom    = c.alphaFrom,
                alphaTo      = c.alphaTo,
                alphaEase    = c.alphaEase,
            };
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private void CancelCurrent(bool snap)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            if (!snap) return;
            foreach (var elem in elements)
            {
                if (elem?.target == null) continue;
#if DOTWEEN
                DOTween.Kill(elem.target, complete: false);
#endif
            }
        }

        private void EnsureRootCanvasGroup()
        {
            if (!manageCanvasGroup) return;
            _rootCanvasGroup = GetComponent<CanvasGroup>();
            if (_rootCanvasGroup == null)
                _rootCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void SetRootInteractable(bool interactive)
        {
            if (_rootCanvasGroup == null) return;
            _rootCanvasGroup.interactable   = interactive;
            _rootCanvasGroup.blocksRaycasts = interactive;
        }

        private void CacheRestPositions()
        {
            _restPositions.Clear();
            foreach (var elem in elements)
            {
                if (elem?.target != null && !_restPositions.ContainsKey(elem.target))
                    _restPositions[elem.target] = elem.target.anchoredPosition;
            }
        }

        private Vector2 GetRestPosition(RectTransform rt)
        {
            if (_restPositions.TryGetValue(rt, out var pos)) return pos;
            _restPositions[rt] = rt.anchoredPosition;
            return _restPositions[rt];
        }

        private CanvasGroup GetOrAddCanvasGroup(RectTransform rt)
        {
            if (_elementCGs.TryGetValue(rt, out var cg) && cg != null) return cg;

            cg = rt.GetComponent<CanvasGroup>();
            if (cg == null && Application.isPlaying)
                cg = rt.gameObject.AddComponent<CanvasGroup>();

            if (cg != null) _elementCGs[rt] = cg;
            return cg;
        }

        // ── Editor helpers ─────────────────────────────────────────────────

#if UNITY_EDITOR

        // ── Snap preview (edit-mode, no animation) ─────────────────────────

        public void EditorPreviewAppear()
        {
            CacheRestPositions();
            for (int i = 0; i < elements.Count; i++)
            {
                var elem = elements[i];
                if (elem?.target == null) continue;
                var config = GetAppearConfig(i);
                var rt = elem.target;
                if (config.scaleEnabled)    rt.localScale       = config.scaleTo;
                if (config.positionEnabled) rt.anchoredPosition = GetRestPosition(rt) + config.positionTo;
                if (config.rotationEnabled) rt.localRotation    = Quaternion.Euler(0f, 0f, config.rotationTo);
                if (config.alphaEnabled)
                {
                    var cg = rt.GetComponent<CanvasGroup>();
                    if (cg != null) cg.alpha = config.alphaTo;
                }
            }
        }

        public void EditorPreviewDisappear()
        {
            CacheRestPositions();
            for (int i = 0; i < elements.Count; i++)
            {
                var elem = elements[i];
                if (elem?.target == null) continue;
                var config = GetDisappearConfig(i);
                var rt = elem.target;
                if (config.scaleEnabled)    rt.localScale       = config.scaleTo;
                if (config.positionEnabled) rt.anchoredPosition = GetRestPosition(rt) + config.positionTo;
                if (config.rotationEnabled) rt.localRotation    = Quaternion.Euler(0f, 0f, config.rotationTo);
                if (config.alphaEnabled)
                {
                    var cg = rt.GetComponent<CanvasGroup>();
                    if (cg != null) cg.alpha = config.alphaTo;
                }
            }
        }

        public void EditorResetPositions()
        {
            CacheRestPositions();
            foreach (var elem in elements)
            {
                if (elem?.target == null) continue;
                var rt = elem.target;
                rt.anchoredPosition = GetRestPosition(rt);
                rt.localScale       = Vector3.one;
                rt.localRotation    = Quaternion.identity;
                var cg = rt.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 1f;
            }
        }

        // ── Animated preview (edit-mode, real-time interpolation) ──────────

        private struct PreviewTrack
        {
            public RectTransform           rt;
            public TransitionElementConfig config;
            public Vector2                 restPos;
            public CanvasGroup             cg;
            public float                   staggerDelay; // raw stagger (seconds), before extraDelay
        }

        private bool               _previewRunning;
        private bool               _previewIsAppear;
        private double             _previewStartTime;
        private List<PreviewTrack> _previewTracks;
        private float              _previewTotalDuration;

        public bool PreviewRunning => _previewRunning;

        /// <summary>Starts an animated edit-mode preview using EditorApplication.update.</summary>
        public void EditorStartAnimatedPreview(bool appear)
        {
            EditorStopAnimatedPreview();
            CacheRestPositions();

            var blockSettings  = appear ? appearSettings : disappearSettings;
            float stagger      = blockSettings.stagger;

            _previewTracks        = new List<PreviewTrack>(elements.Count);
            _previewTotalDuration = 0f;

            for (int i = 0; i < elements.Count; i++)
            {
                var elem = elements[i];
                if (elem?.target == null) continue;

                var config  = appear ? GetAppearConfig(i) : GetDisappearConfig(i);
                var restPos = GetRestPosition(elem.target);
                var cg      = elem.target.GetComponent<CanvasGroup>();

                float rawStagger = i * stagger;
                float endTime    = rawStagger + config.extraDelay + config.GetMaxPropertySpan();
                if (endTime > _previewTotalDuration) _previewTotalDuration = endTime;

                _previewTracks.Add(new PreviewTrack
                {
                    rt           = elem.target,
                    config       = config,
                    restPos      = restPos,
                    cg           = cg,
                    staggerDelay = rawStagger,
                });
            }

            // Snap all elements to their starting FROM state
            foreach (var track in _previewTracks)
            {
                var cfg = track.config;
                if (cfg.alphaEnabled && track.cg != null)
                    track.cg.alpha = cfg.alphaFrom;
                if (cfg.scaleEnabled)
                    track.rt.localScale = cfg.scaleFrom;
                if (cfg.positionEnabled)
                    track.rt.anchoredPosition = track.restPos + cfg.positionFrom;
                if (cfg.rotationEnabled)
                    track.rt.localRotation = Quaternion.Euler(0f, 0f, cfg.rotationFrom);
            }

            _previewIsAppear  = appear;
            _previewStartTime = UnityEditor.EditorApplication.timeSinceStartup;
            _previewRunning   = true;
            UnityEditor.EditorApplication.update += OnPreviewUpdate;
            UnityEditor.SceneView.RepaintAll();
        }

        /// <summary>Stops the animated preview immediately (leaves elements at current interpolated state).</summary>
        public void EditorStopAnimatedPreview()
        {
            if (!_previewRunning) return;
            _previewRunning = false;
            UnityEditor.EditorApplication.update -= OnPreviewUpdate;
            UnityEditor.SceneView.RepaintAll();
        }

        private void OnPreviewUpdate()
        {
            if (!_previewRunning || this == null || _previewTracks == null)
            {
                EditorStopAnimatedPreview();
                return;
            }

            float elapsed = (float)(UnityEditor.EditorApplication.timeSinceStartup - _previewStartTime);

            foreach (var track in _previewTracks)
            {
                if (track.rt == null) continue;
                var cfg = track.config;

                float baseDelay = track.staggerDelay + cfg.extraDelay;

                if (cfg.alphaEnabled && track.cg != null)
                {
                    float t  = CalcT(elapsed, baseDelay + cfg.GetAlphaDelay(), cfg.GetAlphaDuration());
                    float ev = EvalEase(cfg.alphaEase, t);
                    track.cg.alpha = Mathf.LerpUnclamped(cfg.alphaFrom, cfg.alphaTo, ev);
                }

                if (cfg.scaleEnabled)
                {
                    float t  = CalcT(elapsed, baseDelay + cfg.GetScaleDelay(), cfg.GetScaleDuration());
                    float ev = EvalEase(cfg.scaleEase, t);
                    track.rt.localScale = Vector3.LerpUnclamped(cfg.scaleFrom, cfg.scaleTo, ev);
                }

                if (cfg.positionEnabled)
                {
                    float t  = CalcT(elapsed, baseDelay + cfg.GetPositionDelay(), cfg.GetPositionDuration());
                    float ev = EvalEase(cfg.positionEase, t);
                    track.rt.anchoredPosition = Vector2.LerpUnclamped(
                        track.restPos + cfg.positionFrom,
                        track.restPos + cfg.positionTo, ev);
                }

                if (cfg.rotationEnabled)
                {
                    float t   = CalcT(elapsed, baseDelay + cfg.GetRotationDelay(), cfg.GetRotationDuration());
                    float ev  = EvalEase(cfg.rotationEase, t);
                    float rot = Mathf.LerpUnclamped(cfg.rotationFrom, cfg.rotationTo, ev);
                    track.rt.localRotation = Quaternion.Euler(0f, 0f, rot);
                }
            }

            UnityEditor.SceneView.RepaintAll();

            // Stop a little after the last property finishes
            if (elapsed >= _previewTotalDuration + 0.1f)
                EditorStopAnimatedPreview();
        }

        private static float CalcT(float elapsed, float delay, float duration)
        {
            if (duration <= 0f) return elapsed >= delay ? 1f : 0f;
            return Mathf.Clamp01((elapsed - delay) / duration);
        }

        private static float EvalEase(EaseSetting ease, float t)
        {
            if (ease == null) return t;
            if (ease.useCustomCurve && ease.curve != null && ease.curve.length >= 2)
                return ease.curve.Evaluate(t);
#if DOTWEEN
            return EaseEvaluator.Evaluate(ease.ease, t);
#else
            return t;
#endif
        }

#endif // UNITY_EDITOR
    }
}

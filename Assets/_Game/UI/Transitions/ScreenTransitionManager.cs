using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HexWords.UI.Transitions
{
    /// <summary>
    /// Singleton that orchestrates transitions between screens and popups.
    ///
    /// Screens — full-page views; only one active at a time (by default).
    /// Popups  — layered on top of screens; multiple can stack.
    ///
    /// Usage:
    ///   await ScreenTransitionManager.Instance.ShowScreen(myScreen);
    ///   await ScreenTransitionManager.Instance.ShowPopup(myPopup);
    ///   await ScreenTransitionManager.Instance.HidePopup(myPopup);
    ///   await ScreenTransitionManager.Instance.SwitchTab(oldTab, newTab);
    /// </summary>
    public class ScreenTransitionManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────

        public static ScreenTransitionManager Instance { get; private set; }

        // ── Inspector ──────────────────────────────────────────────────────

        [Header("Global Settings")]
        [Tooltip("Multiplied with each ScreenAnimator's localSpeedMultiplier. 2 = twice as fast.")]
        [SerializeField] private float globalSpeedMultiplier = 1f;

        [Tooltip("Default transition style used when TransitionStyle.Full is passed (or no style specified).")]
        [SerializeField] private TransitionStyle defaultStyle = TransitionStyle.Full;

        // ── State ──────────────────────────────────────────────────────────

        private ScreenAnimator _currentScreen;
        private readonly Stack<ScreenAnimator> _popupStack = new Stack<ScreenAnimator>();

        public float GlobalSpeedMultiplier
        {
            get => globalSpeedMultiplier;
            set => globalSpeedMultiplier = Mathf.Max(0.01f, value);
        }

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Screen API ─────────────────────────────────────────────────────

        /// <summary>
        /// Transitions from the currently active screen to <paramref name="next"/>.
        /// </summary>
        public async UniTask ShowScreen(
            ScreenAnimator    next,
            TransitionStyle   style = TransitionStyle.Full,
            CancellationToken ct    = default)
        {
            if (next == null) return;

            var  prev          = _currentScreen;
            var  effectivStyle = style == TransitionStyle.Full ? defaultStyle : style;
            _currentScreen = next;

            Prepare(next, prev);  // pass source screen + suppress OnEnable

            switch (effectivStyle)
            {
                // ── Full: hide old, then show new ─────────────────────────
                case TransitionStyle.Full:
                    if (prev != null && prev != next)
                    {
                        ApplyGlobalSpeed(prev);
                        await prev.DisappearAsync(ct);
                    }
                    next.gameObject.SetActive(true);
                    await next.AppearAsync(ct);
                    if (prev != null && prev != next)
                        prev.gameObject.SetActive(false);
                    break;

                // ── Overlap: new appears over old, then old hides ─────────
                case TransitionStyle.Overlap:
                    next.gameObject.SetActive(true);
                    if (prev != null && prev != next)
                    {
                        ApplyGlobalSpeed(prev);
                        await UniTask.WhenAll(
                            next.AppearAsync(ct),
                            prev.DisappearAsync(ct));
                        prev.gameObject.SetActive(false);
                    }
                    else
                    {
                        await next.AppearAsync(ct);
                    }
                    break;

                // ── Quick: both animate simultaneously ────────────────────
                case TransitionStyle.Quick:
                    next.gameObject.SetActive(true);
                    if (prev != null && prev != next)
                    {
                        ApplyGlobalSpeed(prev);
                        await UniTask.WhenAll(
                            next.QuickAppearAsync(ct),
                            prev.QuickDisappearAsync(ct));
                        prev.gameObject.SetActive(false);
                    }
                    else
                    {
                        await next.QuickAppearAsync(ct);
                    }
                    break;

                // ── Instant: no animation ─────────────────────────────────
                case TransitionStyle.Instant:
                    if (prev != null && prev != next)
                    {
                        prev.DisappearImmediate();
                        prev.gameObject.SetActive(false);
                    }
                    next.gameObject.SetActive(true);
                    next.AppearImmediate();
                    break;
            }
        }

        /// <summary>
        /// Hides the current screen (or a specific screen) without showing another.
        /// </summary>
        public async UniTask HideScreen(
            ScreenAnimator    screen = null,
            TransitionStyle   style  = TransitionStyle.Full,
            CancellationToken ct     = default)
        {
            var target = screen ?? _currentScreen;
            if (target == null) return;
            ApplyGlobalSpeed(target);

            if (style == TransitionStyle.Instant)
            {
                target.DisappearImmediate();
                target.gameObject.SetActive(false);
            }
            else
            {
                await target.DisappearAsync(ct);
                target.gameObject.SetActive(false);
            }

            if (target == _currentScreen) _currentScreen = null;
        }

        // ── Tab switching ──────────────────────────────────────────────────

        /// <summary>
        /// Switches between two tab-level screens simultaneously using the Quick block.
        /// </summary>
        public async UniTask SwitchTab(
            ScreenAnimator    from,
            ScreenAnimator    to,
            TransitionStyle   style = TransitionStyle.Quick,
            CancellationToken ct    = default)
        {
            if (from == null && to == null) return;
            _currentScreen = to;

            if (from == null)
            {
                if (to == null) return;
                Prepare(to, null);
                ApplyGlobalSpeed(to);
                to.gameObject.SetActive(true);
                await to.QuickAppearAsync(ct);
                return;
            }

            if (to == null)
            {
                ApplyGlobalSpeed(from);
                await from.QuickDisappearAsync(ct);
                from.gameObject.SetActive(false);
                return;
            }

            Prepare(to, from);
            ApplyGlobalSpeed(from);
            ApplyGlobalSpeed(to);
            to.gameObject.SetActive(true);

            if (style == TransitionStyle.Instant)
            {
                from.DisappearImmediate();
                from.gameObject.SetActive(false);
                to.AppearImmediate();
                return;
            }

            await UniTask.WhenAll(
                from.QuickDisappearAsync(ct),
                to.QuickAppearAsync(ct));

            from.gameObject.SetActive(false);
        }

        // ── Popup API ──────────────────────────────────────────────────────

        /// <summary>
        /// Shows a popup on top of the current screen.
        /// </summary>
        public async UniTask ShowPopup(
            ScreenAnimator    popup,
            CancellationToken ct = default)
        {
            if (popup == null) return;
            Prepare(popup, _currentScreen);
            ApplyGlobalSpeed(popup);
            _popupStack.Push(popup);
            popup.gameObject.SetActive(true);
            await popup.AppearAsync(ct);
        }

        /// <summary>
        /// Hides a specific popup (or the top-most one if null).
        /// </summary>
        public async UniTask HidePopup(
            ScreenAnimator    popup = null,
            CancellationToken ct   = default)
        {
            ScreenAnimator target;

            if (popup != null)
            {
                target = popup;
                var tmp = new List<ScreenAnimator>(_popupStack);
                _popupStack.Clear();
                foreach (var p in tmp)
                    if (p != popup) _popupStack.Push(p);
            }
            else
            {
                if (_popupStack.Count == 0) return;
                target = _popupStack.Pop();
            }

            ApplyGlobalSpeed(target);
            await target.DisappearAsync(ct);
            target.gameObject.SetActive(false);
        }

        /// <summary>Hides every open popup immediately (no animation).</summary>
        public void HideAllPopupsImmediate()
        {
            while (_popupStack.Count > 0)
            {
                var p = _popupStack.Pop();
                if (p == null) continue;
                p.DisappearImmediate();
                p.gameObject.SetActive(false);
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────

        /// <summary>
        /// Informs <paramref name="next"/> who the previous screen was (for source-based delay),
        /// and suppresses its OnEnable auto-play so the manager drives the animation.
        /// Must be called BEFORE SetActive(true).
        /// </summary>
        private static void Prepare(ScreenAnimator next, ScreenAnimator prev)
        {
            if (next == null) return;
            next.SetPreviousScreen(prev);   // source for delay whitelist
            next.SuppressNextOnEnable();    // manager drives AppearAsync, not OnEnable
        }

        private void ApplyGlobalSpeed(ScreenAnimator animator)
        {
            if (animator != null)
                animator.GlobalSpeedMultiplier = globalSpeedMultiplier;
        }
    }
}

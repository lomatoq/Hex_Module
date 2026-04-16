// Remove this comment after installing DOTween via Asset Store
#define DOTWEEN

using System.Collections;
using System.Collections.Generic;
using HexWords.Core;
using HexWords.Theming;
using UnityEngine;
using UnityEngine.UI;
#if DOTWEEN
using DG.Tweening;
#endif

namespace HexWords.Gameplay
{
    public enum TrailBounceMode
    {
        /// <summary>Only newly spawned elements animate; existing elements stay still.</summary>
        CurrentOnly,
        /// <summary>New element triggers a ripple wave backward: each step back bounces with decaying amplitude.</summary>
        RippleWave,
    }

    public class SwipeTrailView : MonoBehaviour
    {
        [Header("Container")]
        [SerializeField] private RectTransform trailRoot;
        [SerializeField] private FeedbackPalette feedbackPalette;
        private FeedbackPalette Palette => FeedbackPaletteProvider.Resolve(feedbackPalette);

        [Header("Visuals")]
        [SerializeField] private Sprite segmentSprite;  // pill/stadium shape (9-sliced)
        [SerializeField] private Sprite dotSprite;      // circle blob
        [SerializeField] private Color  lineColor    = new Color(0.30f, 0.18f, 0.10f, 0.80f);
        [SerializeField] private float  thickness    = 22f;
        [SerializeField] private float  dotScale     = 1.4f;
        [SerializeField] private float  fadeOutTime  = 0.25f;
        [Tooltip("Duration of the trail appear animation (scaleY 0→1). Match HexCellAnimConfig.fillDuration.")]
        [SerializeField] private float  appearDuration = 0.14f;

        [Header("Bounce Mode")]
        [Tooltip("CurrentOnly: only newly added elements animate. RippleWave: wave propagates backward with decaying amplitude.")]
        [SerializeField] private TrailBounceMode bounceMode = TrailBounceMode.CurrentOnly;

        [Header("Ripple Wave (bounceMode = RippleWave)")]
        [Tooltip("ScaleY overshoot amplitude of the element directly behind the new one (e.g. 0.18 = +18%).")]
        [SerializeField] private float rippleAmplitude = 0.18f;
        [Tooltip("Amplitude multiplier per step back. 0.5 = halves each step (jello decay).")]
        [Range(0.1f, 0.9f)]
        [SerializeField] private float rippleDecay     = 0.50f;
        [Tooltip("Additional delay per step back (seconds). Creates the propagation feel.")]
        [SerializeField] private float rippleStepDelay = 0.025f;
        [Tooltip("Total duration of each ripple bounce (split 40% up / 60% down).")]
        [SerializeField] private float rippleDuration  = 0.18f;

        // Keyed elements: "dot:cellId" or "seg:fromId:toId" → Image
        private readonly Dictionary<string, Image> _trailElements = new Dictionary<string, Image>();
        private Coroutine _fadeRoutine;

#if DOTWEEN
        private int TweenId => GetInstanceID();
#endif

        // ── Public API ─────────────────────────────────────────────────────

        public void DrawPath(IReadOnlyList<string> cellPath,
                             IReadOnlyDictionary<string, HexCellView> cellViews)
        {
#if DOTWEEN
            DOTween.Kill(TweenId);
#endif
            if (trailRoot == null || cellPath == null || cellPath.Count < 1)
            {
                ClearImmediate();
                return;
            }

            // Build required element keys for this path
            var required = new HashSet<string>(cellPath.Count * 2);
            for (int i = 0; i < cellPath.Count; i++)
                required.Add("dot:" + cellPath[i]);
            for (int i = 0; i < cellPath.Count - 1; i++)
                required.Add("seg:" + cellPath[i] + ":" + cellPath[i + 1]);

            // Remove elements no longer in path (backtracking)
            var toRemove = new List<string>();
            foreach (var key in _trailElements.Keys)
                if (!required.Contains(key)) toRemove.Add(key);
            bool isBacktracking = toRemove.Count > 0;
            foreach (var key in toRemove)
            {
                if (_trailElements[key] != null) Destroy(_trailElements[key].gameObject);
                _trailElements.Remove(key);
            }

            // Spawn only new elements (existing ones keep their current state)
            var newKeys = new HashSet<string>();

            foreach (var id in cellPath)
            {
                var key = "dot:" + id;
                if (!_trailElements.ContainsKey(key) && cellViews.TryGetValue(id, out var v))
                {
                    _trailElements[key] = SpawnDot(GetLocalCenter(v));
                    newKeys.Add(key);
                }
            }

            for (int i = 0; i < cellPath.Count - 1; i++)
            {
                var key = "seg:" + cellPath[i] + ":" + cellPath[i + 1];
                if (!_trailElements.ContainsKey(key) &&
                    cellViews.TryGetValue(cellPath[i], out var f) &&
                    cellViews.TryGetValue(cellPath[i + 1], out var t))
                {
                    _trailElements[key] = SpawnSegment(GetLocalCenter(f), GetLocalCenter(t));
                    newKeys.Add(key);
                }
            }

#if DOTWEEN
            if (bounceMode == TrailBounceMode.RippleWave)
            {
                if (newKeys.Count > 0)
                    PlayRipple(cellPath, newKeys);         // forward: new element added
                else if (isBacktracking && cellPath.Count >= 1)
                    PlayBacktrackRipple(cellPath);         // backward: tip removed
            }
#endif
        }

        /// <summary>Recolors the trail to stateColor, holds for holdDuration, then fades out.</summary>
        public void SetColorAndFadeAfter(Color stateColor, float holdDuration, float fadeDuration = -1f)
        {
            if (_trailElements.Count == 0) return;
#if DOTWEEN
            // Do NOT kill TweenId here — the appear animation (scaleY 0→1) must keep running
            foreach (var img in _trailElements.Values)
                if (img != null)
                {
                    var c = img.color;
                    img.color = new Color(stateColor.r, stateColor.g, stateColor.b, c.a);
                }
            // Schedule fade-out; will be killed by DrawPath/ClearImmediate if new swipe starts
            DOVirtual.DelayedCall(holdDuration, () => FadeOutAndClear(fadeDuration)).SetId(TweenId);
#endif
        }

        public void FadeOutAndClear(float duration = -1f)
        {
            if (_trailElements.Count == 0) return;
            var dur = duration >= 0f ? duration : fadeOutTime;

#if DOTWEEN
            DOTween.Kill(TweenId);
            foreach (var img in _trailElements.Values)
            {
                if (img == null) continue;
                img.DOFade(0f, dur).SetEase(Ease.OutCubic).SetId(TweenId);
                // Narrow the trail (local Y = thickness direction, works for rotated segments too)
                img.rectTransform.DOScaleY(0f, dur).SetEase(Ease.OutCubic).SetId(TweenId);
            }
            DOVirtual.DelayedCall(dur + 0.02f, ClearImmediate).SetId(TweenId);
#else
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeRoutine(dur));
#endif
        }

        public void ClearImmediate()
        {
#if DOTWEEN
            DOTween.Kill(TweenId);
#else
            if (_fadeRoutine != null) { StopCoroutine(_fadeRoutine); _fadeRoutine = null; }
#endif
            foreach (var img in _trailElements.Values)
                if (img != null) Destroy(img.gameObject);
            _trailElements.Clear();
        }

        // ── Ripple wave ────────────────────────────────────────────────────

#if DOTWEEN
        /// <summary>Forward ripple: fires when a new element is appended to the path.</summary>
        private void PlayRipple(IReadOnlyList<string> cellPath, HashSet<string> newKeys)
        {
            var ordered = BuildOrderedKeys(cellPath);

            int newIdx = -1;
            for (int i = ordered.Count - 1; i >= 0; i--)
                if (newKeys.Contains(ordered[i])) { newIdx = i; break; }
            if (newIdx <= 0) return;

            RippleBackward(ordered, newIdx, skipKeys: newKeys);
        }

        /// <summary>Backtrack ripple: fires when the last cell is removed from the path.
        /// The new tip gets a retraction bounce, then the wave continues backward.</summary>
        private void PlayBacktrackRipple(IReadOnlyList<string> cellPath)
        {
            if (cellPath.Count < 1) return;
            var ordered = BuildOrderedKeys(cellPath);
            if (ordered.Count == 0) return;

            int sourceIdx = ordered.Count - 1;   // new tip is the last remaining element
            BounceElement(ordered[sourceIdx], rippleAmplitude * 0.75f, delay: 0f);
            RippleBackward(ordered, sourceIdx, skipKeys: null);
        }

        /// <summary>Sends decaying bounces backward from sourceIdx through the ordered key list.</summary>
        private void RippleBackward(List<string> ordered, int sourceIdx, HashSet<string> skipKeys)
        {
            for (int i = sourceIdx - 1; i >= 0; i--)
            {
                int   dist = sourceIdx - i;   // 1, 2, 3 …
                float amp  = rippleAmplitude * Mathf.Pow(rippleDecay, dist);
                if (amp < 0.005f) break;

                var key = ordered[i];
                if (skipKeys != null && skipKeys.Contains(key)) continue;
                BounceElement(key, amp, delay: rippleStepDelay * dist);
            }
        }

        /// <summary>Animates a single trail image: scaleY current → 1+amp → 1, with optional delay.</summary>
        private void BounceElement(string key, float amp, float delay)
        {
            if (!_trailElements.TryGetValue(key, out var img) || img == null) return;
            var rt = img.rectTransform;
            DOTween.Kill(rt);
            var seq = DOTween.Sequence().SetTarget(rt).SetId(TweenId);
            if (delay > 0f) seq.AppendInterval(delay);
            seq.Append(rt.DOScaleY(1f + amp, rippleDuration * 0.4f).SetEase(Ease.OutQuad));
            seq.Append(rt.DOScaleY(1f,        rippleDuration * 0.6f).SetEase(Ease.InQuad));
        }

        private static List<string> BuildOrderedKeys(IReadOnlyList<string> cellPath)
        {
            var ordered = new List<string>(cellPath.Count * 2);
            for (int i = 0; i < cellPath.Count; i++)
            {
                ordered.Add("dot:" + cellPath[i]);
                if (i < cellPath.Count - 1)
                    ordered.Add("seg:" + cellPath[i] + ":" + cellPath[i + 1]);
            }
            return ordered;
        }
#endif

        // ── Spawn ──────────────────────────────────────────────────────────

        private Image SpawnSegment(Vector2 from, Vector2 to)
        {
            var img   = SpawnImage("Seg", segmentSprite, segmentSprite != null ? Image.Type.Sliced : Image.Type.Simple);
            var delta = to - from;
            var rect  = (RectTransform)img.transform;
            rect.sizeDelta        = new Vector2(delta.magnitude, thickness);
            rect.anchoredPosition = (from + to) * 0.5f;
            rect.localRotation    = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
#if DOTWEEN
            // Appear: narrow → wide (local Y = thickness direction, even for rotated segments)
            rect.localScale = new Vector3(1f, 0f, 1f);
            rect.DOScaleY(1f, appearDuration).SetEase(Ease.OutBack).SetId(TweenId);
#endif
            return img;
        }

        private Image SpawnDot(Vector2 center)
        {
            var size = thickness * dotScale;
            var img  = SpawnImage("Dot", dotSprite, Image.Type.Simple);
            var rect = (RectTransform)img.transform;
            rect.sizeDelta        = new Vector2(size, size);
            rect.anchoredPosition = center;
            rect.localRotation    = Quaternion.identity;
#if DOTWEEN
            rect.localScale = new Vector3(1f, 0f, 1f);
            rect.DOScaleY(1f, appearDuration).SetEase(Ease.OutBack).SetId(TweenId);
#endif
            return img;
        }

        private Image SpawnImage(string goName, Sprite sprite, Image.Type type)
        {
            var go  = new GameObject(goName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(trailRoot, false);
            var img           = go.GetComponent<Image>();
            img.color         = feedbackPalette != null ? Palette.trailDefaultColor : lineColor;
            img.raycastTarget = false;
            if (sprite != null) { img.sprite = sprite; img.type = type; }
            return img;
        }

        // ── Coroutine fallback ─────────────────────────────────────────────

#if !DOTWEEN
        private IEnumerator FadeRoutine(float duration)
        {
            var elapsed = 0f;
            var images  = new List<Image>(_trailElements.Values);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var a = Mathf.Clamp01(1f - elapsed / duration);
                foreach (var img in images)
                {
                    if (img == null) continue;
                    var c = lineColor; c.a = lineColor.a * a; img.color = c;
                }
                yield return null;
            }
            ClearImmediate();
            _fadeRoutine = null;
        }
#endif

        private Vector2 GetLocalCenter(HexCellView view)
        {
            var r = (RectTransform)view.transform;
            return trailRoot.InverseTransformPoint(r.TransformPoint(r.rect.center));
        }
    }
}

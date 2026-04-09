// Remove this comment after installing DOTween via Asset Store
#define DOTWEEN

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if DOTWEEN
using DG.Tweening;
#endif

namespace HexWords.Gameplay
{
    public class SwipeTrailView : MonoBehaviour
    {
        [Header("Container")]
        [SerializeField] private RectTransform trailRoot;

        [Header("Visuals")]
        [SerializeField] private Sprite segmentSprite;  // pill/stadium shape (9-sliced)
        [SerializeField] private Sprite dotSprite;      // circle blob
        [SerializeField] private Color  lineColor  = new Color(0.30f, 0.18f, 0.10f, 0.80f);
        [SerializeField] private float  thickness  = 22f;
        [SerializeField] private float  dotScale   = 1.4f;
        [SerializeField] private float  fadeOutTime = 0.25f;

        private readonly List<Image> _trailImages = new List<Image>();
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
            ClearImmediate();
            if (trailRoot == null || cellPath == null || cellPath.Count < 1) return;

            foreach (var id in cellPath)
                if (cellViews.TryGetValue(id, out var v)) SpawnDot(GetLocalCenter(v));

            for (var i = 0; i < cellPath.Count - 1; i++)
                if (cellViews.TryGetValue(cellPath[i], out var f) &&
                    cellViews.TryGetValue(cellPath[i + 1], out var t))
                    SpawnSegment(GetLocalCenter(f), GetLocalCenter(t));
        }

        public void FadeOutAndClear(float duration = -1f)
        {
            if (_trailImages.Count == 0) return;
            var dur = duration >= 0f ? duration : fadeOutTime;

#if DOTWEEN
            DOTween.Kill(TweenId);
            foreach (var img in _trailImages)
                if (img != null) img.DOFade(0f, dur).SetEase(Ease.OutCubic).SetId(TweenId);
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
            foreach (var img in _trailImages)
                if (img != null) Destroy(img.gameObject);
            _trailImages.Clear();
        }

        // ── Spawn ──────────────────────────────────────────────────────────

        private void SpawnSegment(Vector2 from, Vector2 to)
        {
            var img   = SpawnImage("Seg", segmentSprite, segmentSprite != null ? Image.Type.Sliced : Image.Type.Simple);
            var delta = to - from;
            var rect  = (RectTransform)img.transform;
            rect.sizeDelta        = new Vector2(delta.magnitude, thickness);
            rect.anchoredPosition = (from + to) * 0.5f;
            rect.localRotation    = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private void SpawnDot(Vector2 center)
        {
            var size = thickness * dotScale;
            var img  = SpawnImage("Dot", dotSprite, Image.Type.Simple);
            var rect = (RectTransform)img.transform;
            rect.sizeDelta        = new Vector2(size, size);
            rect.anchoredPosition = center;
            rect.localRotation    = Quaternion.identity;

#if DOTWEEN
            rect.localScale = Vector3.zero;
            rect.DOScale(1f, 0.12f).SetEase(Ease.OutBack).SetId(TweenId);
#endif
        }

        private Image SpawnImage(string goName, Sprite sprite, Image.Type type)
        {
            var go  = new GameObject(goName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(trailRoot, false);
            var img           = go.GetComponent<Image>();
            img.color         = lineColor;
            img.raycastTarget = false;
            if (sprite != null) { img.sprite = sprite; img.type = type; }
            _trailImages.Add(img);
            return img;
        }

        // ── Coroutine fallback ─────────────────────────────────────────────

#if !DOTWEEN
        private IEnumerator FadeRoutine(float duration)
        {
            var elapsed = 0f;
            var images  = new List<Image>(_trailImages);
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

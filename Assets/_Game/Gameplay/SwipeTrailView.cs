using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HexWords.Gameplay
{
    public class SwipeTrailView : MonoBehaviour
    {
        [SerializeField] private RectTransform trailRoot;
        [SerializeField] private float thickness = 10f;
        [SerializeField] private Color lineColor = Color.black;

        private readonly List<GameObject> _segments = new List<GameObject>();
        private Coroutine _fadeRoutine;

        public void DrawPath(IReadOnlyList<string> cellPath, IReadOnlyDictionary<string, HexCellView> cellViews)
        {
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            ClearImmediate();
            if (trailRoot == null || cellPath == null || cellPath.Count < 2)
            {
                return;
            }

            for (var i = 0; i < cellPath.Count - 1; i++)
            {
                if (!cellViews.TryGetValue(cellPath[i], out var from) || !cellViews.TryGetValue(cellPath[i + 1], out var to))
                {
                    continue;
                }

                CreateSegment(GetLocalCenter(from), GetLocalCenter(to));
            }
        }

        public void FadeOutAndClear(float duration)
        {
            if (_segments.Count == 0)
            {
                return;
            }

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
            }

            _fadeRoutine = StartCoroutine(FadeAndClearRoutine(Mathf.Max(0.01f, duration)));
        }

        public void ClearImmediate()
        {
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            for (var i = 0; i < _segments.Count; i++)
            {
                Destroy(_segments[i]);
            }

            _segments.Clear();
        }

        private void CreateSegment(Vector2 from, Vector2 to)
        {
            var segment = new GameObject("TrailSegment", typeof(RectTransform), typeof(RawImage));
            segment.transform.SetParent(trailRoot, false);
            var rect = (RectTransform)segment.transform;
            var image = segment.GetComponent<RawImage>();

            image.color = lineColor;
            image.raycastTarget = false;
            image.texture = Texture2D.whiteTexture;

            var delta = to - from;
            var length = delta.magnitude;
            var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            rect.sizeDelta = new Vector2(length, thickness);
            rect.anchoredPosition = (from + to) * 0.5f;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);

            _segments.Add(segment);
        }

        private IEnumerator FadeAndClearRoutine(float duration)
        {
            var images = new List<RawImage>();
            for (var i = 0; i < _segments.Count; i++)
            {
                if (_segments[i] != null)
                {
                    var image = _segments[i].GetComponent<RawImage>();
                    if (image != null)
                    {
                        images.Add(image);
                    }
                }
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var k = Mathf.Clamp01(1f - elapsed / duration);
                for (var i = 0; i < images.Count; i++)
                {
                    if (images[i] != null)
                    {
                        var c = lineColor;
                        c.a *= k;
                        images[i].color = c;
                    }
                }

                yield return null;
            }

            ClearImmediate();
            _fadeRoutine = null;
        }

        private Vector2 GetLocalCenter(HexCellView view)
        {
            var cellRect = (RectTransform)view.transform;
            var worldCenter = cellRect.TransformPoint(cellRect.rect.center);
            return trailRoot.InverseTransformPoint(worldCenter);
        }
    }
}

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

        public void DrawPath(IReadOnlyList<string> cellPath, IReadOnlyDictionary<string, HexCellView> cellViews)
        {
            Clear();
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

        public void Clear()
        {
            for (var i = 0; i < _segments.Count; i++)
            {
                Destroy(_segments[i]);
            }

            _segments.Clear();
        }

        private void CreateSegment(Vector2 from, Vector2 to)
        {
            var segment = new GameObject("TrailSegment", typeof(RectTransform), typeof(Image));
            segment.transform.SetParent(trailRoot, false);
            var rect = (RectTransform)segment.transform;
            var image = segment.GetComponent<Image>();

            image.color = lineColor;
            image.raycastTarget = false;
            image.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");

            var delta = to - from;
            var length = delta.magnitude;
            var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            rect.sizeDelta = new Vector2(length, thickness);
            rect.anchoredPosition = (from + to) * 0.5f;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);

            _segments.Add(segment);
        }

        private Vector2 GetLocalCenter(HexCellView view)
        {
            var cellRect = (RectTransform)view.transform;
            var worldCenter = cellRect.TransformPoint(cellRect.rect.center);
            return trailRoot.InverseTransformPoint(worldCenter);
        }
    }
}

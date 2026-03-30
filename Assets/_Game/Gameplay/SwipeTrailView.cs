using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace HexWords.Gameplay
{
    /// <summary>
    /// Draws an ink-style path between selected hex cells.
    ///
    /// Visual components (set in Inspector):
    ///   trailRoot      — RectTransform under which segments and dots are spawned
    ///   segmentSprite  — (optional) rounded-cap sprite for segments (e.g. a stadium/pill shape)
    ///   dotSprite      — (optional) circular blob sprite for ink dots at each cell center
    ///   lineColor      — base ink color
    ///   thickness      — segment thickness in pixels
    ///   dotScale       — size of the ink-dot relative to thickness
    ///   fadeOutTime    — how long the trail takes to fade after a word is submitted
    /// </summary>
    public class SwipeTrailView : MonoBehaviour
    {
        [Header("Container")]
        [SerializeField] private RectTransform trailRoot;

        [Header("Visuals")]
        [SerializeField] private Sprite segmentSprite;   // pill/stadium shape — set in Inspector
        [SerializeField] private Sprite dotSprite;       // circle blob — set in Inspector
        [SerializeField] private Color  lineColor  = new Color(0.30f, 0.18f, 0.10f, 0.80f);
        [SerializeField] private float  thickness  = 22f;
        [SerializeField] private float  dotScale   = 1.4f;   // dot diameter = thickness × dotScale
        [SerializeField] private float  fadeOutTime = 0.25f;

        // All spawned GameObjects for the current path (segments + dots)
        private readonly List<Image> _trailImages = new List<Image>();

        private int TweenId => GetInstanceID();

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>Redraws the full trail for the current cell path.</summary>
        public void DrawPath(IReadOnlyList<string> cellPath,
                             IReadOnlyDictionary<string, HexCellView> cellViews)
        {
            DOTween.Kill(TweenId);
            ClearImmediate();

            if (trailRoot == null || cellPath == null || cellPath.Count < 1) return;

            // Draw a dot at every cell
            foreach (var id in cellPath)
            {
                if (cellViews.TryGetValue(id, out var view))
                    SpawnDot(GetLocalCenter(view));
            }

            // Draw segments between consecutive cells
            for (var i = 0; i < cellPath.Count - 1; i++)
            {
                if (cellViews.TryGetValue(cellPath[i],     out var from) &&
                    cellViews.TryGetValue(cellPath[i + 1], out var to))
                {
                    SpawnSegment(GetLocalCenter(from), GetLocalCenter(to));
                }
            }
        }

        /// <summary>Fades out all trail images then destroys them.</summary>
        public void FadeOutAndClear(float duration = -1f)
        {
            if (_trailImages.Count == 0) return;

            var dur = duration >= 0f ? duration : fadeOutTime;
            DOTween.Kill(TweenId);

            foreach (var img in _trailImages)
            {
                if (img == null) continue;
                img.DOFade(0f, dur).SetEase(Ease.OutCubic).SetId(TweenId);
            }

            DOVirtual.DelayedCall(dur + 0.02f, ClearImmediate, ignoreTimeScale: false)
                     .SetId(TweenId);
        }

        /// <summary>Destroys all trail GameObjects immediately.</summary>
        public void ClearImmediate()
        {
            DOTween.Kill(TweenId);
            foreach (var img in _trailImages)
            {
                if (img != null) Destroy(img.gameObject);
            }
            _trailImages.Clear();
        }

        // ── Spawn helpers ──────────────────────────────────────────────────

        private void SpawnSegment(Vector2 from, Vector2 to)
        {
            var img = SpawnImage("Seg");

            var delta  = to - from;
            var length = delta.magnitude;
            var angle  = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            var rect = (RectTransform)img.transform;
            rect.sizeDelta        = new Vector2(length, thickness);
            rect.anchoredPosition = (from + to) * 0.5f;
            rect.localRotation    = Quaternion.Euler(0f, 0f, angle);
        }

        private void SpawnDot(Vector2 center)
        {
            var img  = SpawnImage("Dot");
            var size = thickness * dotScale;

            if (dotSprite != null) img.sprite = dotSprite;
            img.type = Image.Type.Simple;

            var rect = (RectTransform)img.transform;
            rect.sizeDelta        = new Vector2(size, size);
            rect.anchoredPosition = center;
            rect.localRotation    = Quaternion.identity;

            // Punch-in ink drop
            rect.localScale = Vector3.zero;
            rect.DOScale(1f, 0.12f).SetEase(Ease.OutBack).SetId(TweenId);
        }

        private Image SpawnImage(string goName)
        {
            var go   = new GameObject(goName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(trailRoot, false);

            var img = go.GetComponent<Image>();
            img.color          = lineColor;
            img.raycastTarget  = false;

            if (segmentSprite != null && goName == "Seg")
            {
                img.sprite = segmentSprite;
                img.type   = Image.Type.Sliced; // pill sprite should be 9-sliced
            }
            else if (dotSprite == null && goName == "Dot")
            {
                // Fallback: use a plain white circle via Sprite.Create not available at runtime.
                // Just use a square; designer should assign a circle sprite.
            }

            _trailImages.Add(img);
            return img;
        }

        private Vector2 GetLocalCenter(HexCellView view)
        {
            var cellRect    = (RectTransform)view.transform;
            var worldCenter = cellRect.TransformPoint(cellRect.rect.center);
            return trailRoot.InverseTransformPoint(worldCenter);
        }
    }
}

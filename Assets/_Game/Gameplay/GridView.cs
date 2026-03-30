using System.Collections.Generic;
using DG.Tweening;
using HexWords.Core;
using UnityEngine;

namespace HexWords.Gameplay
{
    public class GridView : MonoBehaviour
    {
        [SerializeField] private HexCellView      cellPrefab;
        [SerializeField] private RectTransform    gridRoot;
        [SerializeField] private float            cellSize = 90f;
        [SerializeField] private HintAnimationConfig hintConfig;

        private readonly Dictionary<string, HexCellView> _cellViews
            = new Dictionary<string, HexCellView>();

        private int TweenId => GetInstanceID();

        public IReadOnlyDictionary<string, HexCellView> CellViews => _cellViews;
        public int HintRevealCount => hintConfig != null ? hintConfig.revealCount : 2;

        // ── Build ──────────────────────────────────────────────────────────

        public void Build(LevelDefinition level)
        {
            Clear();

            var cells = level.shape.cells;
            if (level.boardLayoutMode == BoardLayoutMode.Fixed16Symmetric &&
                cells != null &&
                cells.Count == HexBoardTemplate16.CellCount &&
                !HexBoardTemplate16.HasCanonicalShape(level.shape))
            {
                cells = new List<CellDefinition>(cells);
                HexBoardTemplate16.ApplyCanonicalLayout(cells);
            }

            foreach (var cell in cells)
            {
                var view = Instantiate(cellPrefab, gridRoot);
                view.Bind(cell);
                view.GetComponent<RectTransform>().anchoredPosition =
                    AxialToPixel(cell.q, cell.r, cellSize);
                _cellViews.Add(cell.cellId, view);
            }
        }

        // ── FX ────────────────────────────────────────────────────────────

        public void ResetFx()
        {
            DOTween.Kill(TweenId);
            foreach (var pair in _cellViews)
                pair.Value.ResetFx();
        }

        /// <summary>
        /// Plays a staggered hint pulse across the listed cells using DOTween.
        /// Each cell lights up with a delay, the full sequence repeats per
        /// <see cref="HintAnimationConfig.repetitionCount"/>.
        /// </summary>
        public void PlayHintAnimation(IReadOnlyList<string> cellIds)
        {
            DOTween.Kill(TweenId);

            int   reps      = hintConfig != null ? hintConfig.repetitionCount        : 1;
            float stepDelay = hintConfig != null ? hintConfig.delayBetweenCells      : 0.18f;
            float repDelay  = hintConfig != null ? hintConfig.delayBetweenRepetitions : 0.6f;
            float pulseDur  = hintConfig != null
                ? (hintConfig.pulseFadeIn + hintConfig.pulseFadeOut) * hintConfig.pulseCount
                  + hintConfig.pauseBetweenPulses * (hintConfig.pulseCount - 1)
                : 1.1f;

            float seriesDuration = (cellIds.Count - 1) * stepDelay + pulseDur;

            var seq = DOTween.Sequence().SetId(TweenId);

            for (var rep = 0; rep < reps; rep++)
            {
                var repCapture = rep;
                seq.AppendCallback(() =>
                {
                    for (var i = 0; i < cellIds.Count; i++)
                    {
                        if (_cellViews.TryGetValue(cellIds[i], out var view))
                            view.PlayHintPulse(i * stepDelay, hintConfig);
                    }
                });

                if (repCapture < reps - 1)
                    seq.AppendInterval(seriesDuration + repDelay);
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private void Clear()
        {
            DOTween.Kill(TweenId);
            for (var i = gridRoot.childCount - 1; i >= 0; i--)
                Destroy(gridRoot.GetChild(i).gameObject);
            _cellViews.Clear();
        }

        private static Vector2 AxialToPixel(int q, int r, float size)
        {
            var x = size * (Mathf.Sqrt(3f) * q + Mathf.Sqrt(3f) / 2f * r);
            var y = size * (3f / 2f * r);
            return new Vector2(x, -y);
        }
    }
}

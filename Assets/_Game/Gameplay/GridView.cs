// Remove this comment after installing DOTween via Asset Store
#define DOTWEEN

using System.Collections;
using System.Collections.Generic;
using HexWords.Core;
using UnityEngine;
#if DOTWEEN
using DG.Tweening;
#endif

namespace HexWords.Gameplay
{
    public class GridView : MonoBehaviour
    {
        [SerializeField] private HexCellView         cellPrefab;
        [SerializeField] private RectTransform        gridRoot;
        [SerializeField] private float                cellSize = 90f;
        [SerializeField] private HintAnimationConfig  hintConfig;

        private readonly Dictionary<string, HexCellView> _cellViews = new Dictionary<string, HexCellView>();
        private Coroutine _hintRoutine;

#if DOTWEEN
        private int TweenId => GetInstanceID();
#endif

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
                view.GetComponent<RectTransform>().anchoredPosition = AxialToPixel(cell.q, cell.r, cellSize);
                _cellViews.Add(cell.cellId, view);
            }
        }

        // ── FX ────────────────────────────────────────────────────────────

        public void ResetFx()
        {
#if DOTWEEN
            DOTween.Kill(TweenId);
#else
            if (_hintRoutine != null) { StopCoroutine(_hintRoutine); _hintRoutine = null; }
#endif
            foreach (var pair in _cellViews) pair.Value.ResetFx();
        }

        public void PlayHintAnimation(IReadOnlyList<string> cellIds)
        {
#if DOTWEEN
            DOTween.Kill(TweenId);
            int   reps      = hintConfig != null ? hintConfig.repetitionCount         : 1;
            float stepDelay = hintConfig != null ? hintConfig.delayBetweenCells       : 0.18f;
            float repDelay  = hintConfig != null ? hintConfig.delayBetweenRepetitions : 0.6f;
            float pulseDur  = hintConfig != null
                ? (hintConfig.pulseFadeIn + hintConfig.pulseFadeOut) * hintConfig.pulseCount
                  + hintConfig.pauseBetweenPulses * (hintConfig.pulseCount - 1)
                : 1.1f;
            float seriesDur = (cellIds.Count - 1) * stepDelay + pulseDur;

            var seq = DOTween.Sequence().SetId(TweenId);
            for (var rep = 0; rep < reps; rep++)
            {
                seq.AppendCallback(() =>
                {
                    for (var i = 0; i < cellIds.Count; i++)
                        if (_cellViews.TryGetValue(cellIds[i], out var v))
                            v.PlayHintPulse(i * stepDelay, hintConfig);
                });
                if (rep < reps - 1) seq.AppendInterval(seriesDur + repDelay);
            }
#else
            if (_hintRoutine != null) StopCoroutine(_hintRoutine);
            _hintRoutine = StartCoroutine(HintSequenceRoutine(cellIds));
#endif
        }

        // ── Coroutine fallback ─────────────────────────────────────────────

#if !DOTWEEN
        private IEnumerator HintSequenceRoutine(IReadOnlyList<string> cellIds)
        {
            int   reps      = hintConfig != null ? hintConfig.repetitionCount         : 1;
            float stepDelay = hintConfig != null ? hintConfig.delayBetweenCells       : 0.18f;
            float repDelay  = hintConfig != null ? hintConfig.delayBetweenRepetitions : 0.6f;
            float pulseDur  = hintConfig != null
                ? (hintConfig.pulseFadeIn + hintConfig.pulseFadeOut) * hintConfig.pulseCount
                  + hintConfig.pauseBetweenPulses * (hintConfig.pulseCount - 1)
                : 1.1f;
            float seriesDur = (cellIds.Count - 1) * stepDelay + pulseDur;

            for (int rep = 0; rep < reps; rep++)
            {
                float delay = 0f;
                for (int i = 0; i < cellIds.Count; i++)
                {
                    if (_cellViews.TryGetValue(cellIds[i], out var view))
                        view.PlayHintPulse(delay, hintConfig);
                    delay += stepDelay;
                }
                if (rep < reps - 1)
                    yield return new WaitForSeconds(seriesDur + repDelay);
            }
            _hintRoutine = null;
        }
#endif

        // ── Helpers ────────────────────────────────────────────────────────

        private void Clear()
        {
#if DOTWEEN
            DOTween.Kill(TweenId);
#endif
            for (var i = gridRoot.childCount - 1; i >= 0; i--)
                Destroy(gridRoot.GetChild(i).gameObject);
            _cellViews.Clear();
        }

        private static Vector2 AxialToPixel(int q, int r, float size)
        {
            return new Vector2(
                size * (Mathf.Sqrt(3f) * q + Mathf.Sqrt(3f) / 2f * r),
               -size * (3f / 2f * r));
        }
    }
}

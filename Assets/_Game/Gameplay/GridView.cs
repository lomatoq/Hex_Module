using System.Collections;
using System.Collections.Generic;
using HexWords.Core;
using UnityEngine;

namespace HexWords.Gameplay
{
    public class GridView : MonoBehaviour
    {
        [SerializeField] private HexCellView cellPrefab;
        [SerializeField] private RectTransform gridRoot;
        [SerializeField] private float cellSize = 90f;
        [SerializeField] private HintAnimationConfig hintConfig;

        private readonly Dictionary<string, HexCellView> _cellViews = new Dictionary<string, HexCellView>();
        private Coroutine _hintSequenceRoutine;

        public IReadOnlyDictionary<string, HexCellView> CellViews => _cellViews;
        public int HintRevealCount => hintConfig != null ? hintConfig.revealCount : 2;

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

            for (var i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                var view = Instantiate(cellPrefab, gridRoot);
                view.Bind(cell);
                view.GetComponent<RectTransform>().anchoredPosition = AxialToPixel(cell.q, cell.r, cellSize);
                _cellViews.Add(cell.cellId, view);
            }
        }

        public void ResetFx()
        {
            if (_hintSequenceRoutine != null)
            {
                StopCoroutine(_hintSequenceRoutine);
                _hintSequenceRoutine = null;
            }
            foreach (var pair in _cellViews)
            {
                pair.Value.ResetFx();
            }
        }

        /// <summary>
        /// Запускае пульс-анімацыю на кожнай клетцы з спісу па чарзе,
        /// паўтараючы ўсю серыю згодна з <see cref="HintAnimationConfig"/>.
        /// </summary>
        public void PlayHintAnimation(IReadOnlyList<string> cellIds)
        {
            if (_hintSequenceRoutine != null)
                StopCoroutine(_hintSequenceRoutine);
            _hintSequenceRoutine = StartCoroutine(HintSequenceRoutine(cellIds));
        }

        private IEnumerator HintSequenceRoutine(IReadOnlyList<string> cellIds)
        {
            int   reps          = hintConfig != null ? hintConfig.repetitionCount        : 1;
            float stepDelay     = hintConfig != null ? hintConfig.delayBetweenCells      : 0.18f;
            float repDelay      = hintConfig != null ? hintConfig.delayBetweenRepetitions : 0.6f;
            float pulseDuration = hintConfig != null
                ? (hintConfig.pulseFadeIn + hintConfig.pulseFadeOut) * hintConfig.pulseCount
                  + hintConfig.pauseBetweenPulses * (hintConfig.pulseCount - 1)
                : 1.1f;

            // Час, пакуль ідзе адна поўная серыя (усе клеткі скончылі свае пульсы)
            float seriesDuration = (cellIds.Count - 1) * stepDelay + pulseDuration;

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
                    yield return new WaitForSeconds(seriesDuration + repDelay);
            }

            _hintSequenceRoutine = null;
        }

        private void Clear()
        {
            for (var i = gridRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(gridRoot.GetChild(i).gameObject);
            }

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

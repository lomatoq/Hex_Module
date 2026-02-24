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

        private readonly Dictionary<string, HexCellView> _cellViews = new Dictionary<string, HexCellView>();

        public IReadOnlyDictionary<string, HexCellView> CellViews => _cellViews;

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
            foreach (var pair in _cellViews)
            {
                pair.Value.ResetFx();
            }
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

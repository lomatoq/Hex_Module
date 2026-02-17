using HexWords.Core;
using UnityEngine;

namespace HexWords.Gameplay
{
    public class SwipeInputController : MonoBehaviour
    {
        [SerializeField] private GridView gridView;

        private SwipePathBuilder _pathBuilder;
        private LevelSessionController _session;
        private LevelDefinition _level;

        public void Initialize(LevelDefinition level, LevelSessionController session, IAdjacencyService adjacencyService)
        {
            _level = level;
            _session = session;
            _pathBuilder = new SwipePathBuilder(adjacencyService, level.shape);

            foreach (var pair in gridView.CellViews)
            {
                var cell = pair.Value;
                cell.PointerDownOnCell += OnCellPointerDown;
                cell.PointerEnterOnCell += OnCellPointerEnter;
                cell.PointerUpOnCell += OnCellPointerUp;
            }
        }

        private void OnDestroy()
        {
            if (gridView == null)
            {
                return;
            }

            foreach (var pair in gridView.CellViews)
            {
                var cell = pair.Value;
                cell.PointerDownOnCell -= OnCellPointerDown;
                cell.PointerEnterOnCell -= OnCellPointerEnter;
                cell.PointerUpOnCell -= OnCellPointerUp;
            }
        }

        private void OnCellPointerDown(HexCellView cell)
        {
            gridView.ResetFx();
            if (_pathBuilder.TryStart(cell.CellId))
            {
                cell.OnSelected();
            }
        }

        private void OnCellPointerEnter(HexCellView cell)
        {
            if (_pathBuilder.TryAppend(cell.CellId))
            {
                cell.OnSelected();
            }
        }

        private void OnCellPointerUp(HexCellView cell)
        {
            var word = _pathBuilder.BuildWord();
            var accepted = _session.TrySubmitWord(word, _level);

            foreach (var cellId in _pathBuilder.CellPath)
            {
                if (gridView.CellViews.TryGetValue(cellId, out var view))
                {
                    if (accepted)
                    {
                        view.OnPathAccepted();
                    }
                    else
                    {
                        view.OnPathRejected();
                    }
                }
            }

            _pathBuilder.Reset();
        }
    }
}

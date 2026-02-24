using HexWords.Core;
using HexWords.UI;
using UnityEngine;

namespace HexWords.Gameplay
{
    public class SwipeInputController : MonoBehaviour
    {
        [SerializeField] private GridView gridView;
        [SerializeField] private SwipeTrailView trailView;
        [SerializeField] private LevelHudView hudView;

        private SwipePathBuilder _pathBuilder;
        private LevelSessionController _session;
        private LevelDefinition _level;
        private bool _isTrackingPath;
        private bool _subscribed;

        public void Initialize(LevelDefinition level, LevelSessionController session, IAdjacencyService adjacencyService)
        {
            Unsubscribe();
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

            _subscribed = true;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        public void Unsubscribe()
        {
            if (!_subscribed || gridView == null)
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

            _subscribed = false;
            _isTrackingPath = false;
            trailView?.ClearImmediate();
        }

        private void OnCellPointerDown(HexCellView cell)
        {
            gridView.ResetFx();
            _isTrackingPath = true;
            if (_pathBuilder.TryStart(cell.CellId))
            {
                cell.OnSelected();
                trailView?.DrawPath(_pathBuilder.CellPath, gridView.CellViews);
                hudView?.SetCurrentWord(_pathBuilder.BuildWord());
            }
        }

        private void OnCellPointerEnter(HexCellView cell)
        {
            if (!_isTrackingPath)
            {
                return;
            }

            if (_pathBuilder.TryAppend(cell.CellId))
            {
                cell.OnSelected();
                trailView?.DrawPath(_pathBuilder.CellPath, gridView.CellViews);
                hudView?.SetCurrentWord(_pathBuilder.BuildWord());
            }
        }

        private void OnCellPointerUp(HexCellView cell)
        {
            if (!_isTrackingPath)
            {
                return;
            }

            _isTrackingPath = false;
            var word = _pathBuilder.BuildWord();
            _session.TrySubmitWord(word, _level);
            var outcome = _session.LastSubmitOutcome;

            foreach (var cellId in _pathBuilder.CellPath)
            {
                if (gridView.CellViews.TryGetValue(cellId, out var view))
                {
                    switch (outcome)
                    {
                        case WordSubmitOutcome.TargetAccepted:
                            view.OnPathAccepted();
                            break;
                        case WordSubmitOutcome.BonusAccepted:
                            view.OnPathBonusAccepted();
                            break;
                        case WordSubmitOutcome.AlreadyAccepted:
                            view.OnPathAlreadyAccepted();
                            break;
                        default:
                            view.OnPathRejected();
                            break;
                    }
                }
            }

            _pathBuilder.Reset();
            trailView?.FadeOutAndClear(0.2f);
        }
    }
}

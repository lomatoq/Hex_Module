using System.Collections.Generic;
using System.Text;
using HexWords.Core;

namespace HexWords.Gameplay
{
    public class SwipePathBuilder
    {
        private readonly IAdjacencyService _adjacencyService;
        private readonly GridShape _shape;
        private readonly List<string> _cellPath = new List<string>();
        private readonly HashSet<string> _visited = new HashSet<string>();

        public SwipePathBuilder(IAdjacencyService adjacencyService, GridShape shape)
        {
            _adjacencyService = adjacencyService;
            _shape = shape;
        }

        public IReadOnlyList<string> CellPath => _cellPath;

        public bool TryStart(string cellId)
        {
            if (_shape.TryGetCell(cellId, out _) == false)
            {
                return false;
            }

            _cellPath.Clear();
            _visited.Clear();
            _cellPath.Add(cellId);
            _visited.Add(cellId);
            return true;
        }

        public bool TryAppend(string cellId)
        {
            if (_visited.Contains(cellId) || _cellPath.Count == 0)
            {
                return false;
            }

            var last = _cellPath[_cellPath.Count - 1];
            if (!_adjacencyService.AreNeighbors(last, cellId, _shape))
            {
                return false;
            }

            _cellPath.Add(cellId);
            _visited.Add(cellId);
            return true;
        }

        public string BuildWord()
        {
            var sb = new StringBuilder();
            for (var i = 0; i < _cellPath.Count; i++)
            {
                if (_shape.TryGetCell(_cellPath[i], out var cell))
                {
                    sb.Append(cell.letter);
                }
            }

            return WordNormalizer.Normalize(sb.ToString());
        }

        public void Reset()
        {
            _cellPath.Clear();
            _visited.Clear();
        }
    }
}

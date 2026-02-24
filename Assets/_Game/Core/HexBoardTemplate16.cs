using System.Collections.Generic;
using System.Linq;

namespace HexWords.Core
{
    public static class HexBoardTemplate16
    {
        private static readonly (int q, int r)[] Coords =
        {
            (-2, 0), (-2, 1), (-2, 2),
            (-1, -1), (-1, 0), (-1, 1),
            (0, -2), (0, -1), (0, 0), (0, 1),
            (1, -2), (1, -1), (1, 0),
            (2, -2), (2, -1), (2, 0)
        };

        public const int CellCount = 16;

        public static IReadOnlyList<(int q, int r)> Coordinates => Coords;
        public static IReadOnlyList<string> CellIds => _cellIds;

        public static bool HasCanonicalShape(GridShape shape)
        {
            if (shape == null || shape.cells == null || shape.cells.Count != CellCount)
            {
                return false;
            }

            var byId = new Dictionary<string, CellDefinition>(CellCount);
            for (var i = 0; i < CellCount; i++)
            {
                byId[shape.cells[i].cellId] = shape.cells[i];
            }

            for (var i = 0; i < CellCount; i++)
            {
                var expectedId = BuildCellId(i);
                if (!byId.TryGetValue(expectedId, out var cell))
                {
                    return false;
                }

                if (cell.q != Coords[i].q || cell.r != Coords[i].r)
                {
                    return false;
                }
            }

            return true;
        }

        public static GridShape CreateShape(IReadOnlyList<char> letters)
        {
            return new GridShape { cells = BuildCells(letters) };
        }

        public static List<CellDefinition> BuildCells(IReadOnlyList<char> letters)
        {
            var cells = new List<CellDefinition>(CellCount);
            for (var i = 0; i < CellCount; i++)
            {
                var letter = i < letters.Count ? letters[i].ToString() : string.Empty;
                cells.Add(new CellDefinition
                {
                    cellId = BuildCellId(i),
                    letter = letter,
                    q = Coords[i].q,
                    r = Coords[i].r
                });
            }

            return cells;
        }

        public static bool TryGetCoordinate(string cellId, out int q, out int r)
        {
            if (_cellIndexById.TryGetValue(cellId, out var index))
            {
                q = Coords[index].q;
                r = Coords[index].r;
                return true;
            }

            q = 0;
            r = 0;
            return false;
        }

        public static IReadOnlyList<string> GetNeighbors(string cellId)
        {
            return _adjacencyById.TryGetValue(cellId, out var neighbors)
                ? neighbors
                : _emptyNeighbors;
        }

        public static void ApplyCanonicalLayout(List<CellDefinition> cells)
        {
            if (cells == null)
            {
                return;
            }

            var count = cells.Count < CellCount ? cells.Count : CellCount;
            var ordered = new CellDefinition[count];
            var taken = new bool[count];

            for (var i = 0; i < count; i++)
            {
                var cell = cells[i];
                var targetIndex = i;
                if (TryParseCellIndex(cell.cellId, out var parsed) && parsed < count && !taken[parsed])
                {
                    targetIndex = parsed;
                }
                else
                {
                    while (targetIndex < count && taken[targetIndex])
                    {
                        targetIndex++;
                    }

                    if (targetIndex >= count)
                    {
                        targetIndex = 0;
                        while (targetIndex < count && taken[targetIndex])
                        {
                            targetIndex++;
                        }
                    }

                    if (targetIndex >= count)
                    {
                        continue;
                    }
                }

                cell.cellId = BuildCellId(targetIndex);
                cell.q = Coords[targetIndex].q;
                cell.r = Coords[targetIndex].r;
                ordered[targetIndex] = cell;
                taken[targetIndex] = true;
            }

            cells.Clear();
            for (var i = 0; i < count; i++)
            {
                if (!taken[i])
                {
                    ordered[i] = new CellDefinition
                    {
                        cellId = BuildCellId(i),
                        q = Coords[i].q,
                        r = Coords[i].r,
                        letter = string.Empty
                    };
                }

                cells.Add(ordered[i]);
            }
        }

        private static string BuildCellId(int index)
        {
            return $"c{index + 1}";
        }

        private static bool TryParseCellIndex(string cellId, out int index)
        {
            index = -1;
            if (string.IsNullOrWhiteSpace(cellId) || cellId.Length < 2 || cellId[0] != 'c')
            {
                return false;
            }

            if (!int.TryParse(cellId.Substring(1), out var parsed))
            {
                return false;
            }

            parsed -= 1;
            if (parsed < 0 || parsed >= CellCount)
            {
                return false;
            }

            index = parsed;
            return true;
        }

        private static readonly string[] _cellIds = BuildCellIds();
        private static readonly Dictionary<string, int> _cellIndexById = BuildCellIndexById();
        private static readonly Dictionary<string, List<string>> _adjacencyById = BuildAdjacencyById();
        private static readonly IReadOnlyList<string> _emptyNeighbors = new string[0];

        private static string[] BuildCellIds()
        {
            var ids = new string[CellCount];
            for (var i = 0; i < ids.Length; i++)
            {
                ids[i] = BuildCellId(i);
            }

            return ids;
        }

        private static Dictionary<string, int> BuildCellIndexById()
        {
            var map = new Dictionary<string, int>(CellCount);
            for (var i = 0; i < CellCount; i++)
            {
                map[_cellIds[i]] = i;
            }

            return map;
        }

        private static Dictionary<string, List<string>> BuildAdjacencyById()
        {
            var map = new Dictionary<string, List<string>>(CellCount);
            var dirs = new (int dq, int dr)[]
            {
                (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1)
            };

            for (var i = 0; i < CellCount; i++)
            {
                var current = Coords[i];
                var neighbors = new List<string>(6);

                for (var d = 0; d < dirs.Length; d++)
                {
                    var q = current.q + dirs[d].dq;
                    var r = current.r + dirs[d].dr;
                    for (var j = 0; j < CellCount; j++)
                    {
                        if (Coords[j].q == q && Coords[j].r == r)
                        {
                            neighbors.Add(_cellIds[j]);
                            break;
                        }
                    }
                }

                map[_cellIds[i]] = neighbors.OrderBy(id => id, System.StringComparer.Ordinal).ToList();
            }

            return map;
        }
    }
}

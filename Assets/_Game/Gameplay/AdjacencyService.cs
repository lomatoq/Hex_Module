using HexWords.Core;

namespace HexWords.Gameplay
{
    public class AdjacencyService : IAdjacencyService
    {
        private static readonly (int dq, int dr)[] Offsets =
        {
            (1, 0),
            (1, -1),
            (0, -1),
            (-1, 0),
            (-1, 1),
            (0, 1)
        };

        public bool AreNeighbors(string fromCellId, string toCellId, GridShape shape)
        {
            if (!shape.TryGetCell(fromCellId, out var from) || !shape.TryGetCell(toCellId, out var to))
            {
                return false;
            }

            for (var i = 0; i < Offsets.Length; i++)
            {
                if (from.q + Offsets[i].dq == to.q && from.r + Offsets[i].dr == to.r)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

using System;
using System.Collections.Generic;

namespace HexWords.Core
{
    [Serializable]
    public class GridShape
    {
        public List<CellDefinition> cells = new List<CellDefinition>();

        public bool TryGetCell(string cellId, out CellDefinition cell)
        {
            for (var i = 0; i < cells.Count; i++)
            {
                if (cells[i].cellId == cellId)
                {
                    cell = cells[i];
                    return true;
                }
            }

            cell = default;
            return false;
        }

        public bool TryGetCellByCoord(int q, int r, out CellDefinition cell)
        {
            for (var i = 0; i < cells.Count; i++)
            {
                if (cells[i].q == q && cells[i].r == r)
                {
                    cell = cells[i];
                    return true;
                }
            }

            cell = default;
            return false;
        }
    }
}

using System.Collections.Generic;
using HexWords.Core;

namespace HexWords.Gameplay
{
    /// <summary>
    /// Finds a valid cell path on the hex grid that spells the given (normalised) word.
    /// Used by the hint system to know which cells to highlight.
    /// </summary>
    public static class HintPathFinder
    {
        /// <summary>
        /// Returns a list of cellIds that spell <paramref name="normalizedWord"/>,
        /// or null if no such path exists on the grid.
        /// </summary>
        public static List<string> FindPath(
            string           normalizedWord,
            GridShape        shape,
            IAdjacencyService adjacency)
        {
            if (string.IsNullOrEmpty(normalizedWord) || shape?.cells == null)
                return null;

            var path    = new List<string>(normalizedWord.Length);
            var visited = new HashSet<string>();

            foreach (var startCell in shape.cells)
            {
                if (WordNormalizer.Normalize(startCell.letter) != normalizedWord[0].ToString())
                    continue;

                path.Clear();
                visited.Clear();

                if (Dfs(normalizedWord, 0, startCell.cellId, path, visited, shape, adjacency))
                    return path;
            }

            return null;
        }

        private static bool Dfs(
            string            word,
            int               idx,
            string            cellId,
            List<string>      path,
            HashSet<string>   visited,
            GridShape         shape,
            IAdjacencyService adjacency)
        {
            if (!shape.TryGetCell(cellId, out var cell)) return false;
            if (WordNormalizer.Normalize(cell.letter) != word[idx].ToString()) return false;

            path.Add(cellId);
            visited.Add(cellId);

            if (idx + 1 >= word.Length) return true; // full word found

            foreach (var neighbor in shape.cells)
            {
                if (visited.Contains(neighbor.cellId)) continue;
                if (!adjacency.AreNeighbors(cellId, neighbor.cellId, shape)) continue;

                if (Dfs(word, idx + 1, neighbor.cellId, path, visited, shape, adjacency))
                    return true;
            }

            path.RemoveAt(path.Count - 1);
            visited.Remove(cellId);
            return false;
        }
    }
}

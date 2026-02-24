using System.Collections.Generic;
using HexWords.Core;
using HexWords.Gameplay;

namespace HexWords.EditorTools
{
    public static class LevelPathValidator
    {
        public static bool CanBuildWord(LevelDefinition level, string rawWord)
        {
            if (level == null)
            {
                return false;
            }

            if (level.boardLayoutMode == BoardLayoutMode.Fixed16Symmetric &&
                level.shape != null &&
                level.shape.cells != null &&
                level.shape.cells.Count == HexBoardTemplate16.CellCount &&
                !HexBoardTemplate16.HasCanonicalShape(level.shape))
            {
                var copy = new List<CellDefinition>(level.shape.cells);
                HexBoardTemplate16.ApplyCanonicalLayout(copy);
                return CanBuildWord(new GridShape { cells = copy }, rawWord);
            }

            return CanBuildWord(level.shape, rawWord);
        }

        public static bool CanBuildWord(GridShape shape, string rawWord)
        {
            if (shape == null || shape.cells == null || shape.cells.Count == 0)
            {
                return false;
            }

            var word = WordNormalizer.Normalize(rawWord);
            if (string.IsNullOrEmpty(word))
            {
                return false;
            }

            var adjacency = new AdjacencyService();
            var map = new Dictionary<int, List<CellDefinition>>();
            for (var i = 0; i < word.Length; i++)
            {
                map[i] = new List<CellDefinition>();
                var letter = word[i].ToString();
                for (var c = 0; c < shape.cells.Count; c++)
                {
                    var cell = shape.cells[c];
                    if (WordNormalizer.Normalize(cell.letter) == letter)
                    {
                        map[i].Add(cell);
                    }
                }

                if (map[i].Count == 0)
                {
                    return false;
                }
            }

            var visited = new HashSet<string>();
            var firstList = map[0];
            for (var i = 0; i < firstList.Count; i++)
            {
                visited.Clear();
                if (Dfs(firstList[i], 0, word, map, shape, adjacency, visited))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Dfs(
            CellDefinition current,
            int idx,
            string word,
            Dictionary<int, List<CellDefinition>> map,
            GridShape shape,
            AdjacencyService adjacency,
            HashSet<string> visited)
        {
            visited.Add(current.cellId);
            if (idx == word.Length - 1)
            {
                visited.Remove(current.cellId);
                return true;
            }

            var nextCells = map[idx + 1];
            for (var i = 0; i < nextCells.Count; i++)
            {
                var next = nextCells[i];
                if (visited.Contains(next.cellId))
                {
                    continue;
                }

                if (!adjacency.AreNeighbors(current.cellId, next.cellId, shape))
                {
                    continue;
                }

                if (Dfs(next, idx + 1, word, map, shape, adjacency, visited))
                {
                    visited.Remove(current.cellId);
                    return true;
                }
            }

            visited.Remove(current.cellId);
            return false;
        }
    }
}

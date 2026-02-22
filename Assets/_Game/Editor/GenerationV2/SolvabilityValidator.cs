using System.Collections.Generic;
using HexWords.Core;

namespace HexWords.EditorTools.GenerationV2
{
    public static class SolvabilityValidator
    {
        public static bool ValidateAll(
            IReadOnlyList<CellDefinition> cells,
            IReadOnlyList<string> targetWords,
            out List<string> failedWords)
        {
            failedWords = new List<string>();
            if (cells == null || targetWords == null)
            {
                return false;
            }

            var shape = new GridShape
            {
                cells = new List<CellDefinition>(cells)
            };

            for (var i = 0; i < targetWords.Count; i++)
            {
                if (HexWords.EditorTools.LevelPathValidator.CanBuildWord(shape, targetWords[i]))
                {
                    continue;
                }

                failedWords.Add(targetWords[i]);
            }

            return failedWords.Count == 0;
        }
    }
}

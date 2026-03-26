using System.Collections.Generic;
using HexWords.Core;

namespace HexWords.Gameplay
{
    public class ScoreService : IScoreService
    {
        // Nonlinear scoring table per wiki spec (Core mech Word Master)
        private static readonly Dictionary<int, int> ScoreTable = new()
        {
            { 3,  1  },
            { 4,  3  },
            { 5,  7  },
            { 6,  13 },
            { 7,  21 },
            { 8,  31 },
            { 9,  43 },
            { 10, 57 },
        };

        public int ScoreWord(string word, LevelDefinition level)
        {
            var normalized = WordNormalizer.Normalize(word);
            var len = normalized.Length;

            if (ScoreTable.TryGetValue(len, out var pts))
                return pts;

            // Longer than 10 — extrapolate linearly past last entry
            if (len > 10)
                return 57 + (len - 10) * 14;

            // Shorter than 3 — no score
            return 0;
        }
    }
}

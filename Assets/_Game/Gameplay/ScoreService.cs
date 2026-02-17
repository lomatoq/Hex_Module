using HexWords.Core;

namespace HexWords.Gameplay
{
    public class ScoreService : IScoreService
    {
        public int ScoreWord(string word, LevelDefinition level)
        {
            var normalized = WordNormalizer.Normalize(word);
            return normalized.Length;
        }
    }
}

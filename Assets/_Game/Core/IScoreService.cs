namespace HexWords.Core
{
    public interface IScoreService
    {
        int ScoreWord(string word, LevelDefinition level);
    }
}

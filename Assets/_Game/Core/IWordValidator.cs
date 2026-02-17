namespace HexWords.Core
{
    public interface IWordValidator
    {
        bool TryValidate(string word, LevelDefinition level, LevelSessionState sessionState, out ValidationReason reason);
    }
}

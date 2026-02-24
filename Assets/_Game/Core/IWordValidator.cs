namespace HexWords.Core
{
    public interface IWordValidator
    {
        WordValidationResult Validate(string word, LevelDefinition level, LevelSessionState sessionState);
    }
}

namespace HexWords.Core
{
    public enum ValidationReason
    {
        None = 0,
        TooShort = 1,
        AlreadyAccepted = 2,
        NotInLevelTargets = 3,
        NotInDictionary = 4,
        InvalidCharacters = 5
    }
}

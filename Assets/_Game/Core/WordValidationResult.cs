namespace HexWords.Core
{
    public struct WordValidationResult
    {
        public bool accepted;
        public WordSubmitOutcome outcome;
        public ValidationReason reason;
        public string normalizedWord;
    }
}

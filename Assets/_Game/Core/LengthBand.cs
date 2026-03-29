namespace HexWords.Core
{
    /// <summary>
    /// Controls word-length distribution requirements for a generation run.
    /// </summary>
    public enum LengthBand
    {
        /// <summary>No length constraint — any mix is accepted.</summary>
        Free = 0,

        /// <summary>
        /// At least two distinct lengths must be present in the word set
        /// (e.g. a 4-letter + a 6-letter word).
        /// </summary>
        MixRequired = 1,

        /// <summary>
        /// At least one word must be long (≥ LongWordMinLength from the profile).
        /// Remaining words can be any length.
        /// </summary>
        OneLongRequired = 2,
    }
}

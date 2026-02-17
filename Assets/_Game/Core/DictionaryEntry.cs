using System;

namespace HexWords.Core
{
    [Serializable]
    public struct DictionaryEntry
    {
        public string word;
        public Language language;
        public string category;
        public int minLevel;
        public int maxLevel;
        public int difficultyBand;
    }
}

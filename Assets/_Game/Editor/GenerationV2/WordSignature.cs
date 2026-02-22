using System.Collections.Generic;
using HexWords.Core;

namespace HexWords.EditorTools.GenerationV2
{
    public sealed class WordSignature
    {
        private readonly Dictionary<char, int> _counts;

        public WordSignature(string word, Dictionary<char, int> counts, ulong bitmask)
        {
            Word = word;
            _counts = counts;
            Bitmask = bitmask;
            UniqueLetterCount = counts.Count;
        }

        public string Word { get; }

        public int Length => Word.Length;

        public ulong Bitmask { get; }

        public int UniqueLetterCount { get; }

        public IReadOnlyDictionary<char, int> Counts => _counts;

        public static WordSignature FromWord(string rawWord, Language language)
        {
            var word = WordNormalizer.Normalize(rawWord);
            var counts = new Dictionary<char, int>();
            var bitmask = 0UL;

            for (var i = 0; i < word.Length; i++)
            {
                var ch = word[i];
                counts.TryGetValue(ch, out var count);
                counts[ch] = count + 1;

                var idx = GetAlphabetIndex(language, ch);
                if (idx >= 0 && idx < 64)
                {
                    bitmask |= 1UL << idx;
                }
            }

            return new WordSignature(word, counts, bitmask);
        }

        public int CountSharedUniqueLetters(WordSignature other)
        {
            if (other == null)
            {
                return 0;
            }

            var shared = 0;
            foreach (var pair in _counts)
            {
                if (other._counts.ContainsKey(pair.Key))
                {
                    shared++;
                }
            }

            return shared;
        }

        private static int GetAlphabetIndex(Language language, char ch)
        {
            const string ruAlphabet = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
            const string enAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            var alphabet = language == Language.RU ? ruAlphabet : enAlphabet;
            for (var i = 0; i < alphabet.Length; i++)
            {
                if (alphabet[i] == ch)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using HexWords.Core;

namespace HexWords.EditorTools
{
    public static class LevelGenerator
    {
        private const string RuAlphabet = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
        private const string EnAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static List<DictionaryEntry> FilterWords(DictionaryDatabase db, GenerationProfile profile)
        {
            var include = WordNormalizer.Normalize(profile.includeLetters);
            var exclude = WordNormalizer.Normalize(profile.excludeLetters);

            return db.entries
                .Where(e => e.language == profile.language)
                .Where(e => string.IsNullOrEmpty(profile.category) || e.category == profile.category)
                .Where(e => e.word.Length >= profile.minLength && e.word.Length <= profile.maxLength)
                .Where(e => e.difficultyBand >= profile.minDifficultyBand && e.difficultyBand <= profile.maxDifficultyBand)
                .Where(e => MatchesLetters(WordNormalizer.Normalize(e.word), include, exclude))
                .ToList();
        }

        public static List<CellDefinition> GenerateCells(GenerationProfile profile, List<DictionaryEntry> candidates)
        {
            var letters = new List<char>();
            foreach (var entry in candidates)
            {
                var normalized = WordNormalizer.Normalize(entry.word);
                for (var i = 0; i < normalized.Length; i++)
                {
                    letters.Add(normalized[i]);
                }
            }

            if (letters.Count == 0)
            {
                letters.Add(profile.language == Language.RU ? 'А' : 'A');
            }

            var rng = new Random(1);
            var selectedLetters = profile.avoidDuplicateLetters
                ? SelectUniqueLetters(profile, letters, rng)
                : SelectWithRepeats(profile.cellCount, letters, rng);

            var cells = new List<CellDefinition>();
            for (var i = 0; i < profile.cellCount; i++)
            {
                cells.Add(new CellDefinition
                {
                    cellId = $"c{i + 1}",
                    letter = selectedLetters[i].ToString(),
                    q = i % 4,
                    r = i / 4
                });
            }

            return cells;
        }

        private static List<char> SelectWithRepeats(int count, List<char> source, Random rng)
        {
            var result = new List<char>(count);
            for (var i = 0; i < count; i++)
            {
                result.Add(source[rng.Next(source.Count)]);
            }

            return result;
        }

        private static List<char> SelectUniqueLetters(GenerationProfile profile, List<char> source, Random rng)
        {
            var unique = source.Distinct().ToList();
            var alphabet = profile.language == Language.RU ? RuAlphabet : EnAlphabet;
            for (var i = 0; i < alphabet.Length && unique.Count < profile.cellCount; i++)
            {
                var ch = alphabet[i];
                if (!unique.Contains(ch))
                {
                    unique.Add(ch);
                }
            }

            if (unique.Count < profile.cellCount)
            {
                return SelectWithRepeats(profile.cellCount, source, rng);
            }

            for (var i = unique.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (unique[i], unique[j]) = (unique[j], unique[i]);
            }

            return unique.Take(profile.cellCount).ToList();
        }

        private static bool MatchesLetters(string word, string include, string exclude)
        {
            if (!string.IsNullOrEmpty(include))
            {
                for (var i = 0; i < include.Length; i++)
                {
                    if (!word.Contains(include[i]))
                    {
                        return false;
                    }
                }
            }

            if (!string.IsNullOrEmpty(exclude))
            {
                for (var i = 0; i < exclude.Length; i++)
                {
                    if (word.Contains(exclude[i]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static void EnsureUniqueLetters(List<CellDefinition> cells, Language language)
        {
            if (cells == null || cells.Count <= 1)
            {
                return;
            }

            var used = new HashSet<char>();
            var duplicates = new List<int>();
            for (var i = 0; i < cells.Count; i++)
            {
                var letter = WordNormalizer.Normalize(cells[i].letter);
                if (string.IsNullOrEmpty(letter))
                {
                    duplicates.Add(i);
                    continue;
                }

                var ch = letter[0];
                if (!used.Add(ch))
                {
                    duplicates.Add(i);
                }
            }

            if (duplicates.Count == 0)
            {
                return;
            }

            var alphabet = language == Language.RU ? RuAlphabet : EnAlphabet;
            var replacementPool = new List<char>();
            for (var i = 0; i < alphabet.Length; i++)
            {
                if (!used.Contains(alphabet[i]))
                {
                    replacementPool.Add(alphabet[i]);
                }
            }

            for (var i = 0; i < duplicates.Count; i++)
            {
                var idx = duplicates[i];
                var cell = cells[idx];
                if (replacementPool.Count > 0)
                {
                    var replacement = replacementPool[0];
                    replacementPool.RemoveAt(0);
                    cell.letter = replacement.ToString();
                    used.Add(replacement);
                }

                cells[idx] = cell;
            }
        }
    }
}

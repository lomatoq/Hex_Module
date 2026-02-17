using System;
using System.Collections.Generic;
using System.Linq;
using HexWords.Core;

namespace HexWords.EditorTools
{
    public static class LevelGenerator
    {
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

        public static List<CellDefinition> GenerateCells(int cellCount, List<DictionaryEntry> candidates)
        {
            var letters = new List<string>();
            foreach (var entry in candidates)
            {
                var normalized = WordNormalizer.Normalize(entry.word);
                for (var i = 0; i < normalized.Length; i++)
                {
                    letters.Add(normalized[i].ToString());
                }
            }

            if (letters.Count == 0)
            {
                letters.Add("A");
            }

            var rng = new Random(1);
            var cells = new List<CellDefinition>();
            for (var i = 0; i < cellCount; i++)
            {
                cells.Add(new CellDefinition
                {
                    cellId = $"c{i + 1}",
                    letter = letters[rng.Next(letters.Count)],
                    q = i % 4,
                    r = i / 4
                });
            }

            return cells;
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
    }
}

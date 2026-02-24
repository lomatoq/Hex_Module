using System;
using System.Collections.Generic;
using System.Linq;
using HexWords.Core;

namespace HexWords.EditorTools.GenerationV2
{
    public sealed class BoardPlacementOptions
    {
        public Language language = Language.EN;
        public int minCells = 3;
        public int maxCells = 18;
        public int fillerLettersMax;
        public bool avoidDuplicateLetters = true;
        public int maxLetterRepeats;
        public int hexBudgetMin;
        public int hexBudgetMax;
        public int attempts = 12;
        public int seed = 1;
        public bool requireAllTargetsSolvable = true;
    }

    public sealed class BoardPlacementResult
    {
        public List<CellDefinition> cells = new List<CellDefinition>();
        public string mergedPath = string.Empty;
    }

    public static class BoardPlacer
    {
        private static readonly (int dq, int dr)[] Directions =
        {
            (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1)
        };

        private const string EnAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string RuAlphabet = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
        private const string EnCommon = "ETAOINSHRDLCUMWFGYPBVKJXQZ";
        private const string RuCommon = "ОЕАИНТСРВЛКМДПУЯЫЬГЗБЧЙХЖШЮЦЩЭФЪЁ";

        public static bool TryPlace(
            IReadOnlyList<string> words,
            BoardPlacementOptions options,
            out BoardPlacementResult result)
        {
            result = null;
            if (words == null || words.Count == 0 || options == null)
            {
                return false;
            }

            var normalizedWords = words
                .Select(WordNormalizer.Normalize)
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (normalizedWords.Count == 0)
            {
                return false;
            }

            var attempts = Math.Max(1, options.attempts);
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                var rng = new Random(options.seed + attempt * 911);
                var merged = BuildMergedPath(normalizedWords, rng);
                var baseLetters = BuildBaseLetters(merged, options.avoidDuplicateLetters);

                if (!TryResolveCellCount(baseLetters.Count, options, out var cellCount))
                {
                    continue;
                }

                var boardLetters = new List<char>(baseLetters);
                if (!FillToCellCount(boardLetters, cellCount, options, rng))
                {
                    continue;
                }

                var cells = BuildCells(boardLetters, options.seed + attempt);
                if (options.requireAllTargetsSolvable &&
                    !SolvabilityValidator.ValidateAll(cells, normalizedWords, out _))
                {
                    continue;
                }

                result = new BoardPlacementResult
                {
                    cells = cells,
                    mergedPath = merged
                };
                return true;
            }

            return false;
        }

        private static List<string> ShuffleWords(List<string> words, Random rng)
        {
            var copy = new List<string>(words);
            for (var i = copy.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (copy[i], copy[j]) = (copy[j], copy[i]);
            }

            return copy;
        }

        private static string BuildMergedPath(IReadOnlyList<string> words, Random rng)
        {
            if (words == null || words.Count == 0)
            {
                return string.Empty;
            }

            if (words.Count == 1)
            {
                return words[0];
            }

            var pool = words.ToList();
            var startIndex = rng.Next(pool.Count);
            var merged = pool[startIndex];
            pool.RemoveAt(startIndex);

            while (pool.Count > 0)
            {
                var bestIndex = -1;
                var bestMerged = string.Empty;
                var bestLength = int.MaxValue;
                var bestOverlap = -1;

                for (var i = 0; i < pool.Count; i++)
                {
                    if (!TryBestMergeWithMetrics(merged, pool[i], out var candidate, out var overlap, out _))
                    {
                        continue;
                    }

                    if (candidate.Length < bestLength)
                    {
                        bestLength = candidate.Length;
                        bestOverlap = overlap;
                        bestIndex = i;
                        bestMerged = candidate;
                        continue;
                    }

                    if (candidate.Length == bestLength && overlap > bestOverlap)
                    {
                        bestOverlap = overlap;
                        bestIndex = i;
                        bestMerged = candidate;
                        continue;
                    }

                    if (candidate.Length == bestLength && overlap == bestOverlap && rng.NextDouble() < 0.15d)
                    {
                        bestIndex = i;
                        bestMerged = candidate;
                    }
                }

                if (bestIndex < 0)
                {
                    merged += pool[0];
                    pool.RemoveAt(0);
                    continue;
                }

                merged = bestMerged;
                pool.RemoveAt(bestIndex);
            }

            return merged;
        }

        private static string MergeWords(IReadOnlyList<string> words)
        {
            if (words == null || words.Count == 0)
            {
                return string.Empty;
            }

            var merged = words[0];
            for (var i = 1; i < words.Count; i++)
            {
                TryBestMerge(merged, words[i], out var nextMerged);
                merged = nextMerged;
            }

            return merged;
        }

        private static bool TryBestMerge(string a, string b, out string merged)
        {
            merged = a + b;
            var best = merged;
            var bestOverlap = 0;
            var maxOverlap = Math.Min(a.Length, b.Length);

            for (var k = maxOverlap; k > 0; k--)
            {
                if (a.EndsWith(b.Substring(0, k), StringComparison.Ordinal))
                {
                    var candidate = a + b.Substring(k);
                    if (k > bestOverlap)
                    {
                        bestOverlap = k;
                        best = candidate;
                    }

                    break;
                }
            }

            for (var k = maxOverlap; k > 0; k--)
            {
                if (a.StartsWith(b.Substring(b.Length - k, k), StringComparison.Ordinal))
                {
                    var candidate = b + a.Substring(k);
                    if (k > bestOverlap)
                    {
                        bestOverlap = k;
                        best = candidate;
                    }

                    break;
                }
            }

            merged = best;
            return true;
        }

        private static bool TryBestMergeWithMetrics(string a, string b, out string merged, out int overlap, out int addedLength)
        {
            merged = a + b;
            overlap = 0;
            addedLength = b.Length;

            var best = merged;
            var bestOverlap = 0;
            var maxOverlap = Math.Min(a.Length, b.Length);

            for (var k = maxOverlap; k > 0; k--)
            {
                if (a.EndsWith(b.Substring(0, k), StringComparison.Ordinal))
                {
                    var candidate = a + b.Substring(k);
                    if (k > bestOverlap)
                    {
                        bestOverlap = k;
                        best = candidate;
                    }

                    break;
                }
            }

            for (var k = maxOverlap; k > 0; k--)
            {
                if (a.StartsWith(b.Substring(b.Length - k, k), StringComparison.Ordinal))
                {
                    var candidate = b + a.Substring(k);
                    if (k > bestOverlap)
                    {
                        bestOverlap = k;
                        best = candidate;
                    }

                    break;
                }
            }

            merged = best;
            overlap = bestOverlap;
            addedLength = merged.Length - a.Length;
            return true;
        }

        private static List<char> BuildBaseLetters(string merged, bool avoidDuplicateLetters)
        {
            var letters = new List<char>();
            var seen = new HashSet<char>();
            for (var i = 0; i < merged.Length; i++)
            {
                var ch = merged[i];
                if (avoidDuplicateLetters && !seen.Add(ch))
                {
                    continue;
                }

                letters.Add(ch);
            }

            return letters;
        }

        private static bool TryResolveCellCount(int requiredCount, BoardPlacementOptions options, out int cellCount)
        {
            var maxCells = Math.Max(1, options.maxCells);
            if (options.hexBudgetMax > 0)
            {
                maxCells = Math.Min(maxCells, options.hexBudgetMax);
            }

            if (requiredCount > maxCells)
            {
                cellCount = 0;
                return false;
            }

            var minCells = Math.Max(options.minCells, requiredCount);
            if (options.hexBudgetMin > 0)
            {
                minCells = Math.Max(minCells, options.hexBudgetMin);
            }

            var maxWithFiller = requiredCount + Math.Max(0, options.fillerLettersMax);
            var upperBound = Math.Min(maxCells, maxWithFiller);
            if (minCells > upperBound)
            {
                cellCount = 0;
                return false;
            }

            cellCount = minCells;
            return true;
        }

        private static bool FillToCellCount(
            List<char> boardLetters,
            int targetCount,
            BoardPlacementOptions options,
            Random rng)
        {
            var counts = new Dictionary<char, int>();
            for (var i = 0; i < boardLetters.Count; i++)
            {
                var ch = boardLetters[i];
                counts.TryGetValue(ch, out var count);
                counts[ch] = count + 1;
            }

            var alphabet = options.language == Language.RU ? RuAlphabet : EnAlphabet;
            var common = options.language == Language.RU ? RuCommon : EnCommon;

            while (boardLetters.Count < targetCount)
            {
                var source = rng.NextDouble() < 0.9d ? common : alphabet;
                var added = false;

                for (var i = 0; i < source.Length; i++)
                {
                    var ch = source[(i + rng.Next(source.Length)) % source.Length];

                    if (options.avoidDuplicateLetters && counts.ContainsKey(ch))
                    {
                        continue;
                    }

                    var repeatsAfterAdd = PredictRepeatsAfterAdd(counts, ch);
                    if (repeatsAfterAdd > options.maxLetterRepeats)
                    {
                        continue;
                    }

                    AddChar(boardLetters, counts, ch);
                    added = true;
                    break;
                }

                if (!added)
                {
                    return false;
                }
            }

            return boardLetters.Count == targetCount;
        }

        private static List<CellDefinition> BuildCells(List<char> letters, int seed)
        {
            var coords = BuildCompactPathCoords(letters.Count, seed);
            var cells = new List<CellDefinition>(letters.Count);

            for (var i = 0; i < letters.Count; i++)
            {
                cells.Add(new CellDefinition
                {
                    cellId = $"c{i + 1}",
                    letter = letters[i].ToString(),
                    q = coords[i].q,
                    r = coords[i].r
                });
            }

            return cells;
        }

        private static void AddChar(List<char> letters, Dictionary<char, int> counts, char ch)
        {
            letters.Add(ch);
            counts.TryGetValue(ch, out var count);
            counts[ch] = count + 1;
        }

        private static int PredictRepeatsAfterAdd(Dictionary<char, int> counts, char ch)
        {
            var repeats = CountRepeats(counts);
            counts.TryGetValue(ch, out var count);
            if (count >= 1)
            {
                repeats += 1;
            }

            return repeats;
        }

        private static int CountRepeats(Dictionary<char, int> counts)
        {
            var repeats = 0;
            foreach (var pair in counts)
            {
                if (pair.Value > 1)
                {
                    repeats += pair.Value - 1;
                }
            }

            return repeats;
        }

        private static List<(int q, int r)> BuildCompactPathCoords(int count, int seed)
        {
            var path = new List<(int q, int r)>(count) { (0, 0) };
            if (count == 1)
            {
                return path;
            }

            var visited = new HashSet<(int q, int r)> { (0, 0) };
            var rng = new Random(seed);
            TryExtendPath(path, visited, count, rng);
            return path;
        }

        private static bool TryExtendPath(List<(int q, int r)> path, HashSet<(int q, int r)> visited, int targetCount, Random rng)
        {
            if (path.Count >= targetCount)
            {
                return true;
            }

            var current = path[path.Count - 1];
            var nexts = new List<(int q, int r)>();
            for (var i = 0; i < Directions.Length; i++)
            {
                var n = (current.q + Directions[i].dq, current.r + Directions[i].dr);
                if (!visited.Contains(n))
                {
                    nexts.Add(n);
                }
            }

            nexts = nexts
                .OrderBy(v => HexDistance(v.q, v.r, 0, 0))
                .ThenBy(_ => rng.Next())
                .ToList();

            for (var i = 0; i < nexts.Count; i++)
            {
                var next = nexts[i];
                visited.Add(next);
                path.Add(next);

                if (TryExtendPath(path, visited, targetCount, rng))
                {
                    return true;
                }

                path.RemoveAt(path.Count - 1);
                visited.Remove(next);
            }

            return false;
        }

        private static int HexDistance(int q1, int r1, int q2, int r2)
        {
            var s1 = -q1 - r1;
            var s2 = -q2 - r2;
            return (Math.Abs(q1 - q2) + Math.Abs(r1 - r2) + Math.Abs(s1 - s2)) / 2;
        }
    }
}

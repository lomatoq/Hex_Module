using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using HexWords.Core;

namespace HexWords.EditorTools.GenerationV2
{
    public static class WordSetSelector
    {
        public static List<string> FilterCandidates(DictionaryDatabase dictionary, GenerationProfile profile)
        {
            if (dictionary == null || profile == null)
            {
                return new List<string>();
            }

            return HexWords.EditorTools.LevelGenerator.FilterWords(dictionary, profile)
                .Select(e => WordNormalizer.Normalize(e.word))
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        public static bool TrySelect(
            IReadOnlyList<string> candidates,
            WordSetSelectionOptions options,
            out WordSetSelectionResult result)
        {
            result = null;
            if (candidates == null || candidates.Count == 0)
            {
                return false;
            }

            var signatures = new List<WordSignature>(candidates.Count);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < candidates.Count; i++)
            {
                var word = WordNormalizer.Normalize(candidates[i]);
                if (string.IsNullOrWhiteSpace(word) || !seen.Add(word))
                {
                    continue;
                }

                signatures.Add(WordSignature.FromWord(word, options.language));
            }

            if (signatures.Count == 0)
            {
                return false;
            }

            var poolLimit = Math.Max(0, options.candidatePoolLimit);
            if (poolLimit > 0 && signatures.Count > poolLimit)
            {
                signatures = signatures
                    .OrderByDescending(QuickScore)
                    .ThenBy(s => s.Word, StringComparer.Ordinal)
                    .Take(poolLimit)
                    .ToList();
            }

            options.maxWords = Math.Min(Math.Max(options.minWords, options.maxWords), signatures.Count);
            var solverBudgetMs = Math.Max(20, options.maxSolverMilliseconds);
            var sw = Stopwatch.StartNew();

            var greedy = WordSetSolverGreedy.Solve(signatures, options);
            WordSetState beam = null;

            var allowBeam = options.beamWidth > 1 && signatures.Count <= Math.Max(50, options.beamInputLimit);
            if (allowBeam && sw.ElapsedMilliseconds < solverBudgetMs)
            {
                beam = WordSetSolverBeam.Solve(signatures, options);
            }

            var greedyRank = WordSetObjective.RankFeasibleState(greedy, options);
            var beamRank = WordSetObjective.RankFeasibleState(beam, options);

            var best = beamRank > greedyRank ? beam : greedy;
            if (!WordSetObjective.IsFeasible(best, options))
            {
                return false;
            }

            result = WordSetObjective.ToResult(best, signatures, options);
            return result.words.Count >= options.minWords;
        }

        private static double QuickScore(WordSignature signature)
        {
            if (signature == null)
            {
                return double.MinValue;
            }

            return signature.UniqueLetterCount * 12d + signature.Length * 2d;
        }
    }
}

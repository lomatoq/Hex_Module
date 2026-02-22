using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HexWords.EditorTools.GenerationV2
{
    internal static class WordSetSolverGreedy
    {
        public static WordSetState Solve(
            IReadOnlyList<WordSignature> signatures,
            WordSetSelectionOptions options)
        {
            if (signatures == null || signatures.Count == 0)
            {
                return null;
            }

            var restarts = Math.Max(1, options.greedyRestarts);
            WordSetState best = null;
            var bestRank = double.MinValue;
            var sw = Stopwatch.StartNew();
            var budgetMs = Math.Max(20, options.maxSolverMilliseconds);

            for (var restart = 0; restart < restarts; restart++)
            {
                if (sw.ElapsedMilliseconds >= budgetMs)
                {
                    break;
                }

                var seed = options.seed + restart * 9973;
                var rng = new Random(seed);
                var state = WordSetState.Empty();

                while (state.WordCount < options.maxWords)
                {
                    if (sw.ElapsedMilliseconds >= budgetMs)
                    {
                        break;
                    }

                    var bestNext = FindBestNextState(state, signatures, options, rng);
                    if (bestNext == null)
                    {
                        break;
                    }

                    state = bestNext;
                }

                var rank = WordSetObjective.RankFeasibleState(state, options);
                if (rank > bestRank)
                {
                    best = state;
                    bestRank = rank;
                }
            }

            return best;
        }

        private static WordSetState FindBestNextState(
            WordSetState current,
            IReadOnlyList<WordSignature> signatures,
            WordSetSelectionOptions options,
            Random rng)
        {
            WordSetState best = null;
            var bestRank = double.MinValue;

            for (var i = 0; i < signatures.Count; i++)
            {
                if (current.Contains(i))
                {
                    continue;
                }

                if (!current.TryAdd(signatures[i], i, options, out var next))
                {
                    continue;
                }

                if (options.hexBudgetMax > 0 && next.hexCount > options.hexBudgetMax)
                {
                    continue;
                }

                var rank = WordSetObjective.RankPartialState(next, options) + rng.NextDouble();
                if (rank > bestRank)
                {
                    best = next;
                    bestRank = rank;
                }
            }

            return best;
        }
    }
}

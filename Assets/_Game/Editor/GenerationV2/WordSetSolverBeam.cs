using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace HexWords.EditorTools.GenerationV2
{
    internal static class WordSetSolverBeam
    {
        public static WordSetState Solve(
            IReadOnlyList<WordSignature> signatures,
            WordSetSelectionOptions options)
        {
            if (signatures == null || signatures.Count == 0)
            {
                return null;
            }

            var beamWidth = Math.Max(2, options.beamWidth);
            var frontier = new List<WordSetState> { WordSetState.Empty() };
            WordSetState bestFeasible = null;
            var bestFeasibleRank = double.MinValue;
            var sw = Stopwatch.StartNew();
            var budgetMs = Math.Max(20, options.maxSolverMilliseconds);
            var expansions = 0;
            var expansionLimit = Math.Max(1000, options.beamExpansionLimit);

            for (var depth = 0; depth < options.maxWords; depth++)
            {
                if (sw.ElapsedMilliseconds >= budgetMs)
                {
                    break;
                }

                var expanded = new List<WordSetState>();
                var dedupe = new Dictionary<string, WordSetState>(StringComparer.Ordinal);

                for (var f = 0; f < frontier.Count; f++)
                {
                    if (sw.ElapsedMilliseconds >= budgetMs)
                    {
                        break;
                    }

                    var current = frontier[f];
                    for (var i = 0; i < signatures.Count; i++)
                    {
                        if (sw.ElapsedMilliseconds >= budgetMs || expansions >= expansionLimit)
                        {
                            break;
                        }

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

                        var key = BuildKey(next.selectedIndices);
                        if (dedupe.TryGetValue(key, out var existing))
                        {
                            var existingRank = WordSetObjective.RankPartialState(existing, options);
                            var newRank = WordSetObjective.RankPartialState(next, options);
                            if (newRank > existingRank)
                            {
                                dedupe[key] = next;
                            }

                            continue;
                        }

                        dedupe[key] = next;
                        expansions++;
                    }
                }

                expanded.AddRange(dedupe.Values);
                if (expanded.Count == 0)
                {
                    break;
                }

                for (var i = 0; i < expanded.Count; i++)
                {
                    var feasibleRank = WordSetObjective.RankFeasibleState(expanded[i], options);
                    if (feasibleRank > bestFeasibleRank)
                    {
                        bestFeasible = expanded[i];
                        bestFeasibleRank = feasibleRank;
                    }
                }

                frontier = expanded
                    .OrderByDescending(s => WordSetObjective.RankPartialState(s, options))
                    .ThenBy(s => s.hexCount)
                    .Take(beamWidth)
                    .ToList();
            }

            return bestFeasible;
        }

        private static string BuildKey(List<int> selectedIndices)
        {
            if (selectedIndices == null || selectedIndices.Count == 0)
            {
                return string.Empty;
            }

            var copy = new List<int>(selectedIndices);
            copy.Sort();
            return string.Join(",", copy);
        }
    }
}

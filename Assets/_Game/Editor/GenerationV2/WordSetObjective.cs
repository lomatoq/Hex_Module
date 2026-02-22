using System;
using System.Collections.Generic;
using System.Linq;
using HexWords.Core;

namespace HexWords.EditorTools.GenerationV2
{
    public sealed class WordSetSelectionOptions
    {
        public Language language = Language.EN;
        public GenerationObjective objective = GenerationObjective.MinHexForKWords;
        public bool avoidDuplicateLetters = true;
        public int minWords = 4;
        public int maxWords = 7;
        public int hexBudgetMin;
        public int hexBudgetMax;
        public int targetScore;
        public int greedyRestarts = 12;
        public int beamWidth = 24;
        public float overlapWeight = 90f;
        public float diversityWeight = 22f;
        public int seed = 1;
        public int candidatePoolLimit = 800;
        public int maxSolverMilliseconds = 250;
        public int beamInputLimit = 600;
        public int beamExpansionLimit = 120000;
    }

    public sealed class WordSetSelectionResult
    {
        public List<string> words = new List<string>();
        public int totalScore;
        public int hexCount;
        public double rankingScore;
    }

    internal sealed class WordSetState
    {
        public readonly List<int> selectedIndices;
        public readonly HashSet<int> selectedSet;
        public readonly Dictionary<char, int> mergedLetterMaxCounts;
        public readonly Dictionary<char, int> mergedLetterPresence;
        public readonly int totalScore;
        public readonly int hexCount;
        public readonly double heuristicScore;

        private WordSetState(
            List<int> selectedIndices,
            HashSet<int> selectedSet,
            Dictionary<char, int> mergedLetterMaxCounts,
            Dictionary<char, int> mergedLetterPresence,
            int totalScore,
            int hexCount,
            double heuristicScore)
        {
            this.selectedIndices = selectedIndices;
            this.selectedSet = selectedSet;
            this.mergedLetterMaxCounts = mergedLetterMaxCounts;
            this.mergedLetterPresence = mergedLetterPresence;
            this.totalScore = totalScore;
            this.hexCount = hexCount;
            this.heuristicScore = heuristicScore;
        }

        public static WordSetState Empty()
        {
            return new WordSetState(
                new List<int>(),
                new HashSet<int>(),
                new Dictionary<char, int>(),
                new Dictionary<char, int>(),
                0,
                0,
                0d);
        }

        public bool Contains(int index)
        {
            return selectedSet.Contains(index);
        }

        public int WordCount => selectedIndices.Count;

        public bool TryAdd(
            WordSignature signature,
            int signatureIndex,
            WordSetSelectionOptions options,
            out WordSetState nextState)
        {
            var maxCounts = CloneDictionary(mergedLetterMaxCounts);
            var presence = CloneDictionary(mergedLetterPresence);

            var deltaHex = 0;
            var overlap = 0;

            foreach (var pair in signature.Counts)
            {
                if (presence.TryGetValue(pair.Key, out var existingPresence))
                {
                    if (existingPresence > 0)
                    {
                        overlap++;
                    }
                }

                if (options.avoidDuplicateLetters)
                {
                    if (!presence.ContainsKey(pair.Key))
                    {
                        presence[pair.Key] = 1;
                        maxCounts[pair.Key] = 1;
                        deltaHex++;
                    }

                    continue;
                }

                maxCounts.TryGetValue(pair.Key, out var currentMax);
                if (pair.Value > currentMax)
                {
                    deltaHex += pair.Value - currentMax;
                    maxCounts[pair.Key] = pair.Value;
                }

                if (!presence.ContainsKey(pair.Key))
                {
                    presence[pair.Key] = 1;
                }
            }

            var redundancyPenalty = signature.UniqueLetterCount > 0
                ? (double)overlap / signature.UniqueLetterCount
                : 0d;

            const double gainScoreWeight = 1d;
            const double hexCostWeight = 14d;
            var deltaHeuristic = gainScoreWeight * signature.Length
                                 - hexCostWeight * deltaHex
                                 + options.overlapWeight * overlap
                                 - options.diversityWeight * redundancyPenalty;

            var nextIndices = new List<int>(selectedIndices.Count + 1);
            nextIndices.AddRange(selectedIndices);
            nextIndices.Add(signatureIndex);

            var nextSet = new HashSet<int>(selectedSet) { signatureIndex };
            nextState = new WordSetState(
                nextIndices,
                nextSet,
                maxCounts,
                presence,
                totalScore + signature.Length,
                hexCount + deltaHex,
                heuristicScore + deltaHeuristic);
            return true;
        }

        private static Dictionary<char, int> CloneDictionary(Dictionary<char, int> source)
        {
            var result = new Dictionary<char, int>(source.Count);
            foreach (var pair in source)
            {
                result[pair.Key] = pair.Value;
            }

            return result;
        }
    }

    public static class WordSetObjective
    {
        internal static bool IsFeasible(WordSetState state, WordSetSelectionOptions options)
        {
            if (state == null)
            {
                return false;
            }

            if (state.WordCount < options.minWords || state.WordCount > options.maxWords)
            {
                return false;
            }

            if (options.hexBudgetMin > 0 && state.hexCount < options.hexBudgetMin)
            {
                return false;
            }

            if (options.hexBudgetMax > 0 && state.hexCount > options.hexBudgetMax)
            {
                return false;
            }

            switch (options.objective)
            {
                case GenerationObjective.MaxWordsUnderHexBudget:
                    return true;
                case GenerationObjective.MeetTargetScore:
                    return state.totalScore >= options.targetScore;
                case GenerationObjective.MinHexForKWords:
                default:
                    return true;
            }
        }

        internal static double RankFeasibleState(WordSetState state, WordSetSelectionOptions options)
        {
            if (!IsFeasible(state, options))
            {
                return double.MinValue;
            }

            switch (options.objective)
            {
                case GenerationObjective.MaxWordsUnderHexBudget:
                    return state.WordCount * 100000d - state.hexCount * 100d + state.totalScore;
                case GenerationObjective.MeetTargetScore:
                    return 1_000_000d - state.hexCount * 100d + state.totalScore * 10d + state.WordCount;
                case GenerationObjective.MinHexForKWords:
                default:
                    return 1_000_000d - state.hexCount * 100d + state.WordCount * 10d + state.totalScore;
            }
        }

        internal static double RankPartialState(WordSetState state, WordSetSelectionOptions options)
        {
            if (state == null)
            {
                return double.MinValue;
            }

            var objectiveBonus = 0d;
            switch (options.objective)
            {
                case GenerationObjective.MaxWordsUnderHexBudget:
                    objectiveBonus = state.WordCount * 80d;
                    break;
                case GenerationObjective.MeetTargetScore:
                    objectiveBonus = state.totalScore;
                    break;
            }

            return state.heuristicScore + objectiveBonus - state.hexCount * 4d;
        }

        internal static WordSetSelectionResult ToResult(
            WordSetState state,
            IReadOnlyList<WordSignature> signatures,
            WordSetSelectionOptions options)
        {
            var result = new WordSetSelectionResult
            {
                totalScore = state.totalScore,
                hexCount = state.hexCount,
                rankingScore = RankFeasibleState(state, options)
            };

            var words = state.selectedIndices
                .Select(i => signatures[i].Word)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            result.words = words;
            return result;
        }

        public static int ComputeHexCount(IReadOnlyList<string> words, bool avoidDuplicateLetters, Language language)
        {
            _ = language;
            if (words == null || words.Count == 0)
            {
                return 0;
            }

            if (avoidDuplicateLetters)
            {
                var unique = new HashSet<char>();
                for (var i = 0; i < words.Count; i++)
                {
                    var normalized = WordNormalizer.Normalize(words[i]);
                    for (var j = 0; j < normalized.Length; j++)
                    {
                        unique.Add(normalized[j]);
                    }
                }

                return unique.Count;
            }

            var maxCounts = new Dictionary<char, int>();
            for (var i = 0; i < words.Count; i++)
            {
                var normalized = WordNormalizer.Normalize(words[i]);
                var counts = new Dictionary<char, int>();
                for (var j = 0; j < normalized.Length; j++)
                {
                    var ch = normalized[j];
                    counts.TryGetValue(ch, out var c);
                    counts[ch] = c + 1;
                }

                foreach (var pair in counts)
                {
                    maxCounts.TryGetValue(pair.Key, out var current);
                    if (pair.Value > current)
                    {
                        maxCounts[pair.Key] = pair.Value;
                    }
                }
            }

            var total = 0;
            foreach (var pair in maxCounts)
            {
                total += pair.Value;
            }

            return total;
        }
    }
}

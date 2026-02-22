using System.Collections.Generic;
using System.Linq;
using HexWords.Core;
using HexWords.EditorTools.GenerationV2;
using NUnit.Framework;

namespace HexWords.Tests.EditMode
{
    public class WordSetSelectorTests
    {
        [Test]
        public void TrySelect_ReturnsDeterministicResult_ForSameSeed()
        {
            var candidates = new List<string> { "CAT", "CAR", "CAN", "DOG", "RUG" };
            var options = new WordSetSelectionOptions
            {
                language = Language.EN,
                objective = GenerationObjective.MinHexForKWords,
                avoidDuplicateLetters = true,
                minWords = 3,
                maxWords = 3,
                greedyRestarts = 8,
                beamWidth = 16,
                overlapWeight = 120f,
                diversityWeight = 10f,
                seed = 42
            };

            Assert.IsTrue(WordSetSelector.TrySelect(candidates, options, out var first));
            Assert.IsTrue(WordSetSelector.TrySelect(candidates, options, out var second));

            CollectionAssert.AreEqual(first.words, second.words);
            Assert.AreEqual(first.hexCount, second.hexCount);
        }

        [Test]
        public void TrySelect_RespectsHexBudget_ForMaxWordsUnderBudget()
        {
            var candidates = new List<string> { "CAT", "CAR", "CAN", "DOG", "RUG", "MOP" };
            var options = new WordSetSelectionOptions
            {
                language = Language.EN,
                objective = GenerationObjective.MaxWordsUnderHexBudget,
                avoidDuplicateLetters = true,
                minWords = 2,
                maxWords = 5,
                hexBudgetMax = 5,
                greedyRestarts = 8,
                beamWidth = 20,
                seed = 7
            };

            Assert.IsTrue(WordSetSelector.TrySelect(candidates, options, out var result));
            Assert.LessOrEqual(result.hexCount, 5);
            Assert.GreaterOrEqual(result.words.Count, 2);
            Assert.LessOrEqual(result.words.Count, 5);
        }

        [Test]
        public void TrySelect_MeetTargetScore_ObjectiveMeetsScoreConstraint()
        {
            var candidates = new List<string> { "WITH", "THEY", "THEN", "WHEN", "HOW", "NOW" };
            var options = new WordSetSelectionOptions
            {
                language = Language.EN,
                objective = GenerationObjective.MeetTargetScore,
                avoidDuplicateLetters = false,
                minWords = 2,
                maxWords = 4,
                targetScore = 12,
                greedyRestarts = 6,
                beamWidth = 14,
                seed = 11
            };

            Assert.IsTrue(WordSetSelector.TrySelect(candidates, options, out var result));
            var score = result.words.Sum(w => w.Length);
            Assert.GreaterOrEqual(score, 12);
        }
    }
}

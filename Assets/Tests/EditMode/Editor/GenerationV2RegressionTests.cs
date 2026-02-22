using System.Collections.Generic;
using System.Linq;
using HexWords.Core;
using HexWords.EditorTools.GenerationV2;
using NUnit.Framework;

namespace HexWords.Tests.EditMode
{
    public class GenerationV2RegressionTests
    {
        [Test]
        public void Pipeline_IsDeterministic_ForFixedSeed()
        {
            var candidates = new List<string> { "WITH", "THE", "THEY", "WHEN", "HOW", "NOW", "THEN", "HEN" };
            var selectionOptions = new WordSetSelectionOptions
            {
                language = Language.EN,
                objective = GenerationObjective.MinHexForKWords,
                avoidDuplicateLetters = false,
                minWords = 4,
                maxWords = 4,
                hexBudgetMax = 12,
                greedyRestarts = 10,
                beamWidth = 20,
                seed = 2026
            };

            Assert.IsTrue(WordSetSelector.TrySelect(candidates, selectionOptions, out var firstSelection));
            Assert.IsTrue(WordSetSelector.TrySelect(candidates, selectionOptions, out var secondSelection));
            CollectionAssert.AreEqual(firstSelection.words, secondSelection.words);

            var placementOptions = new BoardPlacementOptions
            {
                language = Language.EN,
                minCells = 6,
                maxCells = 12,
                fillerLettersMax = 2,
                avoidDuplicateLetters = false,
                maxLetterRepeats = 8,
                attempts = 16,
                seed = 2026,
                requireAllTargetsSolvable = true
            };

            Assert.IsTrue(BoardPlacer.TryPlace(firstSelection.words, placementOptions, out var firstBoard));
            Assert.IsTrue(BoardPlacer.TryPlace(secondSelection.words, placementOptions, out var secondBoard));

            var firstLetters = firstBoard.cells.Select(c => c.letter).ToList();
            var secondLetters = secondBoard.cells.Select(c => c.letter).ToList();
            CollectionAssert.AreEqual(firstLetters, secondLetters);
        }
    }
}

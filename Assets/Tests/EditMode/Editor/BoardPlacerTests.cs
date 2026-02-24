using System.Collections.Generic;
using HexWords.Core;
using HexWords.EditorTools.GenerationV2;
using NUnit.Framework;

namespace HexWords.Tests.EditMode
{
    public class BoardPlacerTests
    {
        [Test]
        public void TryPlace_CreatesSolvableBoard_ForSimpleWordSet()
        {
            var words = new List<string> { "CAT", "ATE" };
            var options = new BoardPlacementOptions
            {
                language = Language.EN,
                boardLayoutMode = BoardLayoutMode.Fixed16Symmetric,
                minCells = 3,
                maxCells = 8,
                fillerLettersMax = 0,
                avoidDuplicateLetters = true,
                maxLetterRepeats = 0,
                attempts = 12,
                seed = 123,
                requireAllTargetsSolvable = true
            };

            Assert.IsTrue(BoardPlacer.TryPlace(words, options, out var result));
            Assert.IsTrue(SolvabilityValidator.ValidateAll(result.cells, words, out var failed));
            Assert.AreEqual(0, failed.Count);
            Assert.AreEqual(HexBoardTemplate16.CellCount, result.cells.Count);
            Assert.IsTrue(HexBoardTemplate16.HasCanonicalShape(new GridShape { cells = result.cells }));
        }

        [Test]
        public void TryPlace_FailsForRepeatedLetterWord_WhenUniqueLettersForced()
        {
            var words = new List<string> { "TOOT" };
            var options = new BoardPlacementOptions
            {
                language = Language.EN,
                boardLayoutMode = BoardLayoutMode.Fixed16Symmetric,
                minCells = 3,
                maxCells = 8,
                fillerLettersMax = 2,
                avoidDuplicateLetters = true,
                maxLetterRepeats = 0,
                attempts = 8,
                seed = 15,
                requireAllTargetsSolvable = true
            };

            Assert.IsFalse(BoardPlacer.TryPlace(words, options, out _));
        }
    }
}

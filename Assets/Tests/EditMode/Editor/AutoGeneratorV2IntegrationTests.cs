using System.Collections.Generic;
using HexWords.Core;
using HexWords.EditorTools.GenerationV2;
using NUnit.Framework;

namespace HexWords.Tests.EditMode
{
    public class AutoGeneratorV2IntegrationTests
    {
        [Test]
        public void V2Pipeline_SelectsAndPlaces_OnlySolvableTargets()
        {
            var candidates = new List<string>
            {
                "WITH", "THE", "THEY", "WHEN", "HOW", "NOW", "THEN", "HEN"
            };

            var selectionOptions = new WordSetSelectionOptions
            {
                language = Language.EN,
                objective = GenerationObjective.MinHexForKWords,
                avoidDuplicateLetters = false,
                minWords = 4,
                maxWords = 4,
                hexBudgetMax = 12,
                greedyRestarts = 12,
                beamWidth = 24,
                seed = 100
            };

            Assert.IsTrue(WordSetSelector.TrySelect(candidates, selectionOptions, out var selected));
            Assert.AreEqual(4, selected.words.Count);

            var placementOptions = new BoardPlacementOptions
            {
                language = Language.EN,
                boardLayoutMode = BoardLayoutMode.Fixed16Symmetric,
                minCells = 6,
                maxCells = 12,
                fillerLettersMax = 2,
                avoidDuplicateLetters = false,
                maxLetterRepeats = 8,
                attempts = 20,
                seed = 100,
                requireAllTargetsSolvable = true
            };

            Assert.IsTrue(BoardPlacer.TryPlace(selected.words, placementOptions, out var board));
            Assert.IsTrue(SolvabilityValidator.ValidateAll(board.cells, selected.words, out var failed));
            Assert.AreEqual(0, failed.Count);
            Assert.AreEqual(HexBoardTemplate16.CellCount, board.cells.Count);
            Assert.IsTrue(HexBoardTemplate16.HasCanonicalShape(new GridShape { cells = board.cells }));
        }
    }
}

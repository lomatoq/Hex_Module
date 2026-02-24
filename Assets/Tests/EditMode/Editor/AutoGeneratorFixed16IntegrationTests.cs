using System.Collections.Generic;
using System.Linq;
using HexWords.Core;
using HexWords.EditorTools.GenerationV2;
using NUnit.Framework;

namespace HexWords.Tests.EditMode
{
    public class AutoGeneratorFixed16IntegrationTests
    {
        [Test]
        public void Fixed16Pipeline_GeneratesTwentySolvableBoards_WithSameShape()
        {
            var candidates = new List<string>
            {
                "ROUTE", "ROUTER", "ROUGE", "ROUSE", "ROSE", "RULE", "RULER", "SOUL", "SOUR", "SORE",
                "OUT", "OUR", "EGO", "GOES", "RUST", "ROTOR", "TOUR", "TOURS", "GORE", "SURE"
            };

            string firstSignature = null;
            for (var i = 0; i < 20; i++)
            {
                var seed = 100 + i * 17;
                var selectionOptions = new WordSetSelectionOptions
                {
                    language = Language.EN,
                    objective = GenerationObjective.MaxWordsUnderHexBudget,
                    avoidDuplicateLetters = false,
                    minWords = 4,
                    maxWords = 6,
                    hexBudgetMin = 0,
                    hexBudgetMax = 16,
                    greedyRestarts = 8,
                    beamWidth = 16,
                    seed = seed,
                    maxSolverMilliseconds = 250
                };

                Assert.IsTrue(WordSetSelector.TrySelect(candidates, selectionOptions, out var selected), $"Selection failed at {i}");
                Assert.GreaterOrEqual(selected.words.Count, 4);

                var placementOptions = new BoardPlacementOptions
                {
                    language = Language.EN,
                    boardLayoutMode = BoardLayoutMode.Fixed16Symmetric,
                    minCells = HexBoardTemplate16.CellCount,
                    maxCells = HexBoardTemplate16.CellCount,
                    avoidDuplicateLetters = false,
                    maxLetterRepeats = 8,
                    fillerLettersMax = 0,
                    attempts = 24,
                    seed = seed,
                    requireAllTargetsSolvable = true
                };

                Assert.IsTrue(BoardPlacer.TryPlace(selected.words, placementOptions, out var placement), $"Placement failed at {i}");
                Assert.IsTrue(SolvabilityValidator.ValidateAll(placement.cells, selected.words, out var failed), $"Unsolved at {i}: {string.Join(",", failed)}");
                Assert.AreEqual(HexBoardTemplate16.CellCount, placement.cells.Count);
                Assert.IsTrue(HexBoardTemplate16.HasCanonicalShape(new GridShape { cells = placement.cells }));

                var signature = string.Join(";", placement.cells.Select(c => $"{c.cellId}:{c.q},{c.r}"));
                if (firstSignature == null)
                {
                    firstSignature = signature;
                }
                else
                {
                    Assert.AreEqual(firstSignature, signature);
                }
            }
        }
    }
}

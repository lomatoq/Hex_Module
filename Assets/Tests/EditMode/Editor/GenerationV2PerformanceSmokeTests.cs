using System.Collections.Generic;
using System.Diagnostics;
using HexWords.Core;
using HexWords.EditorTools.GenerationV2;
using NUnit.Framework;

namespace HexWords.Tests.EditMode
{
    public class GenerationV2PerformanceSmokeTests
    {
        [Test]
        [Category("Performance")]
        public void Selector_PerformanceSmoke_HandlesLargeCandidatePool()
        {
            var chunksA = new[] { "CA", "TE", "RI", "NO", "LA", "ME", "PO", "SU", "TH", "WE" };
            var chunksB = new[] { "T", "R", "N", "L", "M", "P", "S", "H", "W", "D" };
            var candidates = new List<string>(5000);

            for (var i = 0; i < chunksA.Length; i++)
            {
                for (var j = 0; j < chunksA.Length; j++)
                {
                    var word = chunksA[i] + chunksA[j] + chunksB[(i + j) % chunksB.Length];
                    candidates.Add(word);
                    if (candidates.Count >= 4000)
                    {
                        break;
                    }
                }

                if (candidates.Count >= 4000)
                {
                    break;
                }
            }

            var options = new WordSetSelectionOptions
            {
                language = Language.EN,
                objective = GenerationObjective.MinHexForKWords,
                avoidDuplicateLetters = false,
                minWords = 4,
                maxWords = 7,
                hexBudgetMax = 18,
                greedyRestarts = 12,
                beamWidth = 24,
                seed = 99,
                candidatePoolLimit = 300,
                maxSolverMilliseconds = 120,
                beamInputLimit = 500,
                beamExpansionLimit = 60000
            };

            var sw = Stopwatch.StartNew();
            var ok = WordSetSelector.TrySelect(candidates, options, out var result);
            sw.Stop();

            Assert.IsTrue(ok);
            Assert.GreaterOrEqual(result.words.Count, 4);
            Assert.Less(sw.ElapsedMilliseconds, 2000);
        }
    }
}

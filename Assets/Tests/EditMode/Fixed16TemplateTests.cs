using System.Collections.Generic;
using System.Linq;
using HexWords.Core;
using HexWords.Gameplay;
using NUnit.Framework;

namespace HexWords.Tests.EditMode
{
    public class Fixed16TemplateTests
    {
        [Test]
        public void Template_HasFixed16CellsAndCanonicalIds()
        {
            Assert.AreEqual(16, HexBoardTemplate16.CellCount);
            Assert.AreEqual(16, HexBoardTemplate16.Coordinates.Count);
            Assert.AreEqual(16, HexBoardTemplate16.CellIds.Count);

            for (var i = 0; i < HexBoardTemplate16.CellCount; i++)
            {
                Assert.AreEqual($"c{i + 1}", HexBoardTemplate16.CellIds[i]);
            }
        }

        [Test]
        public void Template_Adjacency_MatchesHexNeighborRule()
        {
            var shape = new GridShape
            {
                cells = HexBoardTemplate16.BuildCells(new[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P' })
            };
            var adjacency = new AdjacencyService();

            for (var i = 0; i < HexBoardTemplate16.CellCount; i++)
            {
                var from = HexBoardTemplate16.CellIds[i];
                var expected = new HashSet<string>(HexBoardTemplate16.GetNeighbors(from));
                var actual = new HashSet<string>();
                for (var j = 0; j < HexBoardTemplate16.CellCount; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    var to = HexBoardTemplate16.CellIds[j];
                    if (adjacency.AreNeighbors(from, to, shape))
                    {
                        actual.Add(to);
                    }
                }

                CollectionAssert.AreEquivalent(expected, actual, $"Mismatch for {from}");
            }
        }

        [Test]
        public void BuildCells_ProducesCanonicalFixedShape()
        {
            var letters = Enumerable.Repeat('A', HexBoardTemplate16.CellCount).ToList();
            var shape = new GridShape { cells = HexBoardTemplate16.BuildCells(letters) };
            Assert.IsTrue(HexBoardTemplate16.HasCanonicalShape(shape));
        }
    }
}

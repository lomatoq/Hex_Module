#if UNITY_EDITOR
using System.Collections.Generic;
using HexWords.Core;
using HexWords.EditorTools;
using NUnit.Framework;

namespace HexWords.Tests.EditMode
{
    public class LevelPathValidatorTests
    {
        [Test]
        public void CanBuildWord_ByGridShape_ReturnsTrueForReachableWord()
        {
            var shape = new GridShape
            {
                cells = new List<CellDefinition>
                {
                    new CellDefinition { cellId = "c1", letter = "C", q = 0, r = 0 },
                    new CellDefinition { cellId = "c2", letter = "A", q = 1, r = 0 },
                    new CellDefinition { cellId = "c3", letter = "T", q = 1, r = -1 }
                }
            };

            Assert.IsTrue(LevelPathValidator.CanBuildWord(shape, "CAT"));
        }

        [Test]
        public void CanBuildWord_RejectsWordRequiringCellReuse()
        {
            var shape = new GridShape
            {
                cells = new List<CellDefinition>
                {
                    new CellDefinition { cellId = "c1", letter = "T", q = 0, r = 0 },
                    new CellDefinition { cellId = "c2", letter = "O", q = 1, r = 0 }
                }
            };

            Assert.IsFalse(LevelPathValidator.CanBuildWord(shape, "TOOT"));
        }
    }
}
#endif

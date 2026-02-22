using HexWords.Core;
using HexWords.EditorTools.GenerationV2;
using NUnit.Framework;

namespace HexWords.Tests.EditMode
{
    public class GenerationObjectiveTests
    {
        [Test]
        public void ComputeHexCount_UsesUniqueLetters_WhenUniqueModeOn()
        {
            var words = new[] { "TOOL", "LOOT" };
            var hexCount = WordSetObjective.ComputeHexCount(words, true, Language.EN);
            Assert.AreEqual(3, hexCount);
        }

        [Test]
        public void ComputeHexCount_UsesPerLetterMaxCounts_WhenUniqueModeOff()
        {
            var words = new[] { "TOOL", "LOOT" };
            var hexCount = WordSetObjective.ComputeHexCount(words, false, Language.EN);
            Assert.AreEqual(4, hexCount);
        }

        [Test]
        public void ComputeHexCount_Treats_E_And_YO_AsDifferent()
        {
            var words = new[] { "ЕЛКА", "ЁЛКА" };
            var hexCount = WordSetObjective.ComputeHexCount(words, true, Language.RU);
            Assert.AreEqual(5, hexCount);
        }
    }
}

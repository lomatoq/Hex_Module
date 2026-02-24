using HexWords.EditorTools.GenerationV2;
using NUnit.Framework;

namespace HexWords.Tests.EditMode
{
    public class GenerationPlanUtilityTests
    {
        [Test]
        public void BuildDesiredWordCounts_StrictMode_UsesDescendingRange()
        {
            var desired = GenerationPlanUtility.BuildDesiredWordCounts(4, 7, true);
            CollectionAssert.AreEqual(new[] { 7, 6, 5, 4 }, desired);
        }

        [Test]
        public void BuildDesiredWordCounts_NonStrictMode_UsesAscendingRange()
        {
            var desired = GenerationPlanUtility.BuildDesiredWordCounts(4, 7, false);
            CollectionAssert.AreEqual(new[] { 4, 5, 6, 7 }, desired);
        }
    }
}

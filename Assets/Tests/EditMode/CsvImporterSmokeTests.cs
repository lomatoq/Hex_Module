#if UNITY_EDITOR
using System.IO;
using NUnit.Framework;

namespace HexWords.Tests.EditMode
{
    public class CsvImporterSmokeTests
    {
        [Test]
        public void SourceCsvFiles_Exist()
        {
            Assert.IsTrue(File.Exists("Assets/_Game/Data/Source/dictionary_ru.csv"));
            Assert.IsTrue(File.Exists("Assets/_Game/Data/Source/dictionary_en.csv"));
            Assert.IsTrue(File.Exists("Assets/_Game/Data/Source/levels.csv"));
            Assert.IsTrue(File.Exists("Assets/_Game/Data/Source/level_cells.csv"));
        }

        [Test]
        public void SourceCsvFiles_AreNotEmpty()
        {
            Assert.Greater(new FileInfo("Assets/_Game/Data/Source/dictionary_ru.csv").Length, 0);
            Assert.Greater(new FileInfo("Assets/_Game/Data/Source/dictionary_en.csv").Length, 0);
            Assert.Greater(new FileInfo("Assets/_Game/Data/Source/levels.csv").Length, 0);
            Assert.Greater(new FileInfo("Assets/_Game/Data/Source/level_cells.csv").Length, 0);
        }
    }
}
#endif

using System.Collections.Generic;
using HexWords.Core;
using HexWords.Gameplay;
using NUnit.Framework;

namespace HexWords.Tests.EditMode
{
    public class HexWordsCoreTests
    {
        [Test]
        public void AdjacencyService_DetectsAllSixNeighbors()
        {
            var shape = new GridShape
            {
                cells = new List<CellDefinition>
                {
                    new CellDefinition { cellId = "c", q = 0, r = 0, letter = "A" },
                    new CellDefinition { cellId = "n1", q = 1, r = 0, letter = "A" },
                    new CellDefinition { cellId = "n2", q = 1, r = -1, letter = "A" },
                    new CellDefinition { cellId = "n3", q = 0, r = -1, letter = "A" },
                    new CellDefinition { cellId = "n4", q = -1, r = 0, letter = "A" },
                    new CellDefinition { cellId = "n5", q = -1, r = 1, letter = "A" },
                    new CellDefinition { cellId = "n6", q = 0, r = 1, letter = "A" }
                }
            };

            var sut = new AdjacencyService();
            Assert.IsTrue(sut.AreNeighbors("c", "n1", shape));
            Assert.IsTrue(sut.AreNeighbors("c", "n2", shape));
            Assert.IsTrue(sut.AreNeighbors("c", "n3", shape));
            Assert.IsTrue(sut.AreNeighbors("c", "n4", shape));
            Assert.IsTrue(sut.AreNeighbors("c", "n5", shape));
            Assert.IsTrue(sut.AreNeighbors("c", "n6", shape));
        }

        [Test]
        public void SwipePathBuilder_DoesNotAllowReusingCell()
        {
            var shape = new GridShape
            {
                cells = new List<CellDefinition>
                {
                    new CellDefinition { cellId = "c1", q = 0, r = 0, letter = "A" },
                    new CellDefinition { cellId = "c2", q = 1, r = 0, letter = "B" }
                }
            };

            var builder = new SwipePathBuilder(new AdjacencyService(), shape);
            Assert.IsTrue(builder.TryStart("c1"));
            Assert.IsTrue(builder.TryAppend("c2"));
            Assert.IsFalse(builder.TryAppend("c1"));
        }

        [Test]
        public void WordValidator_LevelOnly_RejectsWordNotInTargets()
        {
            var level = TestLevel(ValidationMode.LevelOnly, Language.EN, new[] { "CAT" });
            var session = new LevelSessionState();
            var validator = new WordValidator(null);

            var result = validator.Validate("DOG", level, session);
            Assert.IsFalse(result.accepted);
            Assert.AreEqual(WordSubmitOutcome.Rejected, result.outcome);
            Assert.AreEqual(ValidationReason.NotInLevelTargets, result.reason);
        }

        [Test]
        public void WordValidator_DictionaryMode_UsesLanguageSpecificDictionary()
        {
            var level = TestLevel(ValidationMode.Dictionary, Language.EN, new string[0]);
            var session = new LevelSessionState();
            var db = UnityEngine.ScriptableObject.CreateInstance<DictionaryDatabase>();
            db.entries = new List<DictionaryEntry>
            {
                new DictionaryEntry { language = Language.EN, word = "CAT" },
                new DictionaryEntry { language = Language.RU, word = "КОТ" }
            };

            var validator = new WordValidator(db);
            Assert.IsTrue(validator.Validate("CAT", level, session).accepted);
            Assert.IsFalse(validator.Validate("КОТ", level, session).accepted);
        }

        [Test]
        public void WordValidator_Treats_E_And_YO_AsDifferentLetters()
        {
            var level = TestLevel(ValidationMode.Dictionary, Language.RU, new string[0]);
            var session = new LevelSessionState();
            var db = UnityEngine.ScriptableObject.CreateInstance<DictionaryDatabase>();
            db.entries = new List<DictionaryEntry>
            {
                new DictionaryEntry { language = Language.RU, word = "ЕЛКА" }
            };

            var validator = new WordValidator(db);
            Assert.IsFalse(validator.Validate("ЁЛКА", level, session).accepted);
            Assert.IsTrue(validator.Validate("ЕЛКА", level, session).accepted);
        }

        [Test]
        public void ScoreService_ReturnsWordLength()
        {
            var level = TestLevel(ValidationMode.LevelOnly, Language.EN, new[] { "TEST" });
            var score = new ScoreService();
            Assert.AreEqual(4, score.ScoreWord("TEST", level));
        }

        private static LevelDefinition TestLevel(ValidationMode mode, Language language, string[] targetWords)
        {
            var level = UnityEngine.ScriptableObject.CreateInstance<LevelDefinition>();
            level.validationMode = mode;
            level.language = language;
            level.targetWords = targetWords;
            level.shape = new GridShape();
            return level;
        }
    }
}

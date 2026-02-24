using System.Collections.Generic;
using HexWords.Core;
using HexWords.Gameplay;
using NUnit.Framework;

namespace HexWords.Tests.EditMode
{
    public class WordValidatorOutcomeTests
    {
        [Test]
        public void Validate_ReturnsTargetAccepted_ForExactTargetMatch()
        {
            var level = CreateLevel(ValidationMode.LevelOnly, new[] { "BAY", "BAYER" });
            var validator = new WordValidator(null);

            var result = validator.Validate("BAY", level, new LevelSessionState());

            Assert.IsTrue(result.accepted);
            Assert.AreEqual(WordSubmitOutcome.TargetAccepted, result.outcome);
        }

        [Test]
        public void Validate_ReturnsBonusAccepted_ForEmbeddedWordInLevelOnly()
        {
            var level = CreateLevel(ValidationMode.LevelOnly, new[] { "BAYER" });
            level.allowBonusWords = true;
            level.allowBonusInLevelOnly = true;
            level.bonusRequiresEmbeddedInLevelOnly = true;

            var validator = new WordValidator(null);
            var result = validator.Validate("BAY", level, new LevelSessionState());

            Assert.IsTrue(result.accepted);
            Assert.AreEqual(WordSubmitOutcome.BonusAccepted, result.outcome);
        }

        [Test]
        public void Validate_ReturnsRejected_WhenLevelOnlyBonusMustBeEmbedded()
        {
            var level = CreateLevel(ValidationMode.LevelOnly, new[] { "BAYER" });
            level.allowBonusWords = true;
            level.allowBonusInLevelOnly = true;
            level.bonusRequiresEmbeddedInLevelOnly = true;

            var validator = new WordValidator(null);
            var result = validator.Validate("SON", level, new LevelSessionState());

            Assert.IsFalse(result.accepted);
            Assert.AreEqual(WordSubmitOutcome.Rejected, result.outcome);
            Assert.AreEqual(ValidationReason.NotInLevelTargets, result.reason);
        }

        [Test]
        public void Validate_ReturnsAlreadyAccepted_ForDuplicateSubmit()
        {
            var level = CreateLevel(ValidationMode.LevelOnly, new[] { "BAY" });
            var state = new LevelSessionState();
            state.acceptedWords.Add("BAY");

            var validator = new WordValidator(null);
            var result = validator.Validate("BAY", level, state);

            Assert.IsFalse(result.accepted);
            Assert.AreEqual(WordSubmitOutcome.AlreadyAccepted, result.outcome);
            Assert.AreEqual(ValidationReason.AlreadyAccepted, result.reason);
        }

        [Test]
        public void Validate_DictionaryMode_AllowsDictionaryBonus()
        {
            var db = UnityEngine.ScriptableObject.CreateInstance<DictionaryDatabase>();
            db.entries = new List<DictionaryEntry>
            {
                new DictionaryEntry { language = Language.EN, word = "ROSE" }
            };

            var level = CreateLevel(ValidationMode.Dictionary, new[] { "ROUTE" });
            var validator = new WordValidator(db);
            var result = validator.Validate("ROSE", level, new LevelSessionState());

            Assert.IsTrue(result.accepted);
            Assert.AreEqual(WordSubmitOutcome.BonusAccepted, result.outcome);
        }

        private static LevelDefinition CreateLevel(ValidationMode mode, string[] targets)
        {
            var level = UnityEngine.ScriptableObject.CreateInstance<LevelDefinition>();
            level.language = Language.EN;
            level.validationMode = mode;
            level.targetWords = targets;
            return level;
        }
    }
}

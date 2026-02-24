using HexWords.Core;
using HexWords.Gameplay;
using NUnit.Framework;

namespace HexWords.Tests.EditMode
{
    public class CompletionGateTests
    {
        [Test]
        public void Session_NotCompleted_WhenScoreReachedButTargetThresholdIsLower()
        {
            var level = UnityEngine.ScriptableObject.CreateInstance<LevelDefinition>();
            level.validationMode = ValidationMode.LevelOnly;
            level.language = Language.EN;
            level.targetWords = new[] { "BAYER" };
            level.targetScore = 3;
            level.minTargetWordsToComplete = 1;
            level.allowBonusWords = true;
            level.allowBonusInLevelOnly = true;
            level.bonusRequiresEmbeddedInLevelOnly = true;

            var controller = new LevelSessionController(new WordValidator(null), new ScoreService());
            controller.StartSession();

            Assert.IsTrue(controller.TrySubmitWord("BAY", level));
            Assert.AreEqual(WordSubmitOutcome.BonusAccepted, controller.LastSubmitOutcome);
            Assert.AreEqual(0, controller.State.acceptedTargetCount);
            Assert.GreaterOrEqual(controller.State.currentScore, level.targetScore);
            Assert.IsFalse(controller.State.isCompleted);
        }

        [Test]
        public void Session_Completes_OnlyAfterScoreAndMinTargetsAreSatisfied()
        {
            var level = UnityEngine.ScriptableObject.CreateInstance<LevelDefinition>();
            level.validationMode = ValidationMode.LevelOnly;
            level.language = Language.EN;
            level.targetWords = new[] { "BAY", "BAYER" };
            level.targetScore = 7;
            level.minTargetWordsToComplete = 2;
            level.allowBonusWords = true;
            level.allowBonusInLevelOnly = true;
            level.bonusRequiresEmbeddedInLevelOnly = true;

            var controller = new LevelSessionController(new WordValidator(null), new ScoreService());
            controller.StartSession();

            Assert.IsTrue(controller.TrySubmitWord("BAYER", level));
            Assert.AreEqual(WordSubmitOutcome.TargetAccepted, controller.LastSubmitOutcome);
            Assert.AreEqual(1, controller.State.acceptedTargetCount);
            Assert.IsFalse(controller.State.isCompleted);

            Assert.IsTrue(controller.TrySubmitWord("BAY", level));
            Assert.AreEqual(WordSubmitOutcome.TargetAccepted, controller.LastSubmitOutcome);
            Assert.AreEqual(2, controller.State.acceptedTargetCount);
            Assert.IsTrue(controller.State.isCompleted);
        }
    }
}

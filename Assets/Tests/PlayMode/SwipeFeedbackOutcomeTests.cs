using HexWords.Core;
using HexWords.Gameplay;
using NUnit.Framework;

namespace HexWords.Tests.PlayMode
{
    public class SwipeFeedbackOutcomeTests
    {
        [Test]
        public void SubmitSequence_ProducesExpectedOutcomes_AndBonusScore()
        {
            var level = UnityEngine.ScriptableObject.CreateInstance<LevelDefinition>();
            level.language = Language.EN;
            level.validationMode = ValidationMode.LevelOnly;
            level.targetWords = new[] { "ROUTE" };
            level.targetScore = 12;
            level.minTargetWordsToComplete = 1;
            level.allowBonusWords = true;
            level.allowBonusInLevelOnly = true;
            level.bonusRequiresEmbeddedInLevelOnly = true;

            var session = new LevelSessionController(new WordValidator(null), new ScoreService());
            session.StartSession();

            Assert.IsTrue(session.TrySubmitWord("ROU", level));
            Assert.AreEqual(WordSubmitOutcome.BonusAccepted, session.LastSubmitOutcome);
            Assert.AreEqual(3, session.State.bonusScore);

            Assert.IsFalse(session.TrySubmitWord("ROU", level));
            Assert.AreEqual(WordSubmitOutcome.AlreadyAccepted, session.LastSubmitOutcome);

            Assert.IsTrue(session.TrySubmitWord("ROUTE", level));
            Assert.AreEqual(WordSubmitOutcome.TargetAccepted, session.LastSubmitOutcome);
            Assert.GreaterOrEqual(session.State.currentScore, 8);
            Assert.AreEqual(1, session.State.acceptedTargetCount);
        }
    }
}

using HexWords.Core;
using HexWords.Gameplay;
using NUnit.Framework;

namespace HexWords.Tests.PlayMode
{
    public class LevelSessionPlayModeTests
    {
        [Test]
        public void Session_Completes_WhenTargetScoreReached()
        {
            var level = UnityEngine.ScriptableObject.CreateInstance<LevelDefinition>();
            level.targetScore = 3;
            level.validationMode = ValidationMode.LevelOnly;
            level.targetWords = new[] { "CAT" };

            var validator = new WordValidator(null);
            var session = new LevelSessionController(validator, new ScoreService());
            session.StartSession();

            var completed = false;
            session.LevelCompleted += () => completed = true;

            var accepted = session.TrySubmitWord("CAT", level);

            Assert.IsTrue(accepted);
            Assert.IsTrue(completed);
            Assert.IsTrue(session.State.isCompleted);
        }

        [Test]
        public void Session_RejectsDuplicateWord()
        {
            var level = UnityEngine.ScriptableObject.CreateInstance<LevelDefinition>();
            level.targetScore = 10;
            level.validationMode = ValidationMode.LevelOnly;
            level.targetWords = new[] { "CAT" };

            var validator = new WordValidator(null);
            var session = new LevelSessionController(validator, new ScoreService());
            session.StartSession();

            Assert.IsTrue(session.TrySubmitWord("CAT", level));
            Assert.IsFalse(session.TrySubmitWord("CAT", level));
        }
    }
}

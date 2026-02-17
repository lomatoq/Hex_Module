using System;
using HexWords.Core;

namespace HexWords.Gameplay
{
    public class LevelSessionController
    {
        private readonly IWordValidator _wordValidator;
        private readonly IScoreService _scoreService;

        public LevelSessionController(IWordValidator wordValidator, IScoreService scoreService)
        {
            _wordValidator = wordValidator;
            _scoreService = scoreService;
        }

        public LevelSessionState State { get; } = new LevelSessionState();

        public event Action<int, int> ScoreChanged;
        public event Action<string> WordAccepted;
        public event Action<ValidationReason> WordRejected;
        public event Action LevelCompleted;

        public void StartSession()
        {
            State.Reset();
        }

        public bool TrySubmitWord(string rawWord, LevelDefinition level)
        {
            if (!_wordValidator.TryValidate(rawWord, level, State, out var reason))
            {
                WordRejected?.Invoke(reason);
                return false;
            }

            var normalized = WordNormalizer.Normalize(rawWord);
            State.acceptedWords.Add(normalized);
            State.currentScore += _scoreService.ScoreWord(normalized, level);
            ScoreChanged?.Invoke(State.currentScore, level.targetScore);
            WordAccepted?.Invoke(normalized);

            if (!State.isCompleted && State.currentScore >= level.targetScore)
            {
                State.isCompleted = true;
                LevelCompleted?.Invoke();
            }

            return true;
        }
    }
}

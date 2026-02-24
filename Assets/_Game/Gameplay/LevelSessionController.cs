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
        public WordSubmitOutcome LastSubmitOutcome { get; private set; } = WordSubmitOutcome.Rejected;
        public ValidationReason LastSubmitReason { get; private set; } = ValidationReason.None;
        public string LastSubmittedWord { get; private set; } = string.Empty;

        public event Action<int, int> ScoreChanged;
        public event Action<string> WordAccepted;
        public event Action<string, WordSubmitOutcome, ValidationReason> WordSubmittedDetailed;
        public event Action<string, bool> WordSubmitted;
        public event Action<ValidationReason> WordRejected;
        public event Action LevelCompleted;

        public void StartSession()
        {
            State.Reset();
            LastSubmitOutcome = WordSubmitOutcome.Rejected;
            LastSubmitReason = ValidationReason.None;
            LastSubmittedWord = string.Empty;
        }

        public bool TrySubmitWord(string rawWord, LevelDefinition level)
        {
            if (level == null)
            {
                LastSubmitOutcome = WordSubmitOutcome.Rejected;
                LastSubmitReason = ValidationReason.NotInLevelTargets;
                LastSubmittedWord = string.Empty;
                WordRejected?.Invoke(ValidationReason.NotInLevelTargets);
                WordSubmittedDetailed?.Invoke(string.Empty, LastSubmitOutcome, LastSubmitReason);
                WordSubmitted?.Invoke(string.Empty, false);
                return false;
            }

            var result = _wordValidator.Validate(rawWord, level, State);
            var normalized = result.normalizedWord;
            LastSubmitOutcome = result.outcome;
            LastSubmitReason = result.reason;
            LastSubmittedWord = normalized;

            if (!result.accepted)
            {
                if (result.outcome == WordSubmitOutcome.Rejected)
                {
                    WordRejected?.Invoke(result.reason);
                }

                WordSubmittedDetailed?.Invoke(normalized, result.outcome, result.reason);
                WordSubmitted?.Invoke(normalized, false);
                return false;
            }

            State.acceptedWords.Add(normalized);
            var scoreDelta = _scoreService.ScoreWord(normalized, level);
            State.currentScore += scoreDelta;

            if (result.outcome == WordSubmitOutcome.TargetAccepted)
            {
                State.acceptedTargetWords.Add(normalized);
                State.acceptedTargetCount = State.acceptedTargetWords.Count;
            }
            else if (result.outcome == WordSubmitOutcome.BonusAccepted)
            {
                State.acceptedBonusWords.Add(normalized);
                State.bonusScore += scoreDelta;
            }

            ScoreChanged?.Invoke(State.currentScore, level.targetScore);
            WordAccepted?.Invoke(normalized);
            WordSubmittedDetailed?.Invoke(normalized, result.outcome, result.reason);
            WordSubmitted?.Invoke(normalized, true);

            var maxPossibleTargets = level.targetWords != null ? level.targetWords.Length : 0;
            var minTargets = Math.Min(Math.Max(0, level.minTargetWordsToComplete), maxPossibleTargets);
            if (!State.isCompleted &&
                State.currentScore >= level.targetScore &&
                State.acceptedTargetCount >= minTargets)
            {
                State.isCompleted = true;
                LevelCompleted?.Invoke();
            }

            return true;
        }
    }
}

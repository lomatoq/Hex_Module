using System.Collections.Generic;

namespace HexWords.Core
{
    public class LevelSessionState
    {
        public int currentScore;
        public int bonusScore;
        public HashSet<string> acceptedWords       = new HashSet<string>();
        public HashSet<string> acceptedTargetWords  = new HashSet<string>();
        public HashSet<string> acceptedBonusWords   = new HashSet<string>();
        public int acceptedTargetCount;
        public bool isCompleted;

        // Streak — consecutive accepted words without a wrong guess
        public int currentStreak;
        public int bestStreak;

        public void Reset()
        {
            currentScore = 0;
            bonusScore   = 0;
            acceptedWords.Clear();
            acceptedTargetWords.Clear();
            acceptedBonusWords.Clear();
            acceptedTargetCount = 0;
            isCompleted         = false;
            currentStreak       = 0;
            bestStreak          = 0;
        }
    }
}

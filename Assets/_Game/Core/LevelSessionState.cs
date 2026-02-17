using System.Collections.Generic;

namespace HexWords.Core
{
    public class LevelSessionState
    {
        public int currentScore;
        public HashSet<string> acceptedWords = new HashSet<string>();
        public bool isCompleted;

        public void Reset()
        {
            currentScore = 0;
            acceptedWords.Clear();
            isCompleted = false;
        }
    }
}

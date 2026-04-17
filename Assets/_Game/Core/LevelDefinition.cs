using UnityEngine;

namespace HexWords.Core
{
    [CreateAssetMenu(menuName = "HexWords/Level Definition", fileName = "LevelDefinition")]
    public class LevelDefinition : ScriptableObject
    {
        public string levelId;
        public GridShape shape = new GridShape();
        public BoardLayoutMode boardLayoutMode = BoardLayoutMode.Fixed16Symmetric;
        // Dictionary mode by default: any real word the player swipes is a bonus,
        // even if it is not a substring of a target word. LevelOnly is reserved
        // for hand-curated puzzles where only target words (and their substrings)
        // should score.
        public ValidationMode validationMode = ValidationMode.Dictionary;
        public string[] targetWords;
        public int targetScore = 10;
        public int minTargetWordsToComplete = 2;
        public bool allowBonusWords = true;
        public bool allowBonusInLevelOnly = true;
        public bool bonusRequiresEmbeddedInLevelOnly = false;
        public Language language = Language.EN;
    }
}

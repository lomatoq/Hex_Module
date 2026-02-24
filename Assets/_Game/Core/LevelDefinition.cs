using UnityEngine;

namespace HexWords.Core
{
    [CreateAssetMenu(menuName = "HexWords/Level Definition", fileName = "LevelDefinition")]
    public class LevelDefinition : ScriptableObject
    {
        public string levelId;
        public GridShape shape = new GridShape();
        public BoardLayoutMode boardLayoutMode = BoardLayoutMode.Fixed16Symmetric;
        public ValidationMode validationMode = ValidationMode.LevelOnly;
        public string[] targetWords;
        public int targetScore = 10;
        public int minTargetWordsToComplete = 2;
        public bool allowBonusWords = true;
        public bool allowBonusInLevelOnly = true;
        public bool bonusRequiresEmbeddedInLevelOnly = true;
        public Language language = Language.EN;
    }
}

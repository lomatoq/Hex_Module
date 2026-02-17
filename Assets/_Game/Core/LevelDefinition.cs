using UnityEngine;

namespace HexWords.Core
{
    [CreateAssetMenu(menuName = "HexWords/Level Definition", fileName = "LevelDefinition")]
    public class LevelDefinition : ScriptableObject
    {
        public string levelId;
        public GridShape shape = new GridShape();
        public ValidationMode validationMode = ValidationMode.LevelOnly;
        public string[] targetWords;
        public int targetScore = 10;
        public Language language = Language.EN;
    }
}

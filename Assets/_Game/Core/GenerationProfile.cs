using UnityEngine;

namespace HexWords.Core
{
    [CreateAssetMenu(menuName = "HexWords/Generation Profile", fileName = "GenerationProfile")]
    public class GenerationProfile : ScriptableObject
    {
        public Language language = Language.EN;
        public string category = "general";
        public int minLength = 3;
        public int maxLength = 8;
        public int cellCount = 12;
        public bool avoidDuplicateLetters = true;
        public string includeLetters = string.Empty;
        public string excludeLetters = string.Empty;
        public int minDifficultyBand;
        public int maxDifficultyBand = 10;
    }
}

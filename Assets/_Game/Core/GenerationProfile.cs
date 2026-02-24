using UnityEngine;

namespace HexWords.Core
{
    [CreateAssetMenu(menuName = "HexWords/Generation Profile", fileName = "GenerationProfile")]
    public class GenerationProfile : ScriptableObject
    {
        public Language language = Language.EN;
        public string category = "general";
        public int minLength = 3;
        public int maxLength = 6;
        public int cellCount = 18;
        public int targetWordsMin = 4;
        public int targetWordsMax = 7;
        public int maxLetterRepeats = 0;
        public bool allowSingleRepeatFallback = true;
        public int fillerLettersMax = 0;
        public bool avoidDuplicateLetters = true;
        public string includeLetters = string.Empty;
        public string excludeLetters = string.Empty;
        public int minDifficultyBand;
        public int maxDifficultyBand = 10;

        [Header("Generation V2")]
        public GenerationAlgorithm generationAlgorithm = GenerationAlgorithm.Legacy;
        public GenerationObjective objective = GenerationObjective.MinHexForKWords;
        public int hexBudgetMin = 0;
        public int hexBudgetMax = 0;
        public int beamWidth = 24;
        public int greedyRestarts = 12;
        public int maxResampleAttempts = 40;
        public float overlapWeight = 90f;
        public float diversityWeight = 22f;
        public bool requireAllTargetsSolvable = true;
        public bool useLegacyFallback = false;
    }
}

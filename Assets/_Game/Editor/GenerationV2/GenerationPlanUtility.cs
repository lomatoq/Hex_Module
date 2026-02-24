using System;
using System.Collections.Generic;

namespace HexWords.EditorTools.GenerationV2
{
    public static class GenerationPlanUtility
    {
        public static List<int> BuildDesiredWordCounts(int minTargets, int maxTargets, bool strictTargetWordCount)
        {
            var min = Math.Max(1, minTargets);
            var max = Math.Max(min, maxTargets);

            var desired = new List<int>(max - min + 1);
            if (strictTargetWordCount)
            {
                for (var value = max; value >= min; value--)
                {
                    desired.Add(value);
                }
                return desired;
            }

            for (var value = min; value <= max; value++)
            {
                desired.Add(value);
            }

            return desired;
        }
    }
}

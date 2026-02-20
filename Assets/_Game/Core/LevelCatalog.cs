using System.Collections.Generic;
using UnityEngine;

namespace HexWords.Core
{
    [CreateAssetMenu(menuName = "HexWords/Level Catalog", fileName = "LevelCatalog")]
    public class LevelCatalog : ScriptableObject
    {
        public List<LevelDefinition> levels = new List<LevelDefinition>();

        public int Count => levels != null ? levels.Count : 0;

        public LevelDefinition GetAt(int index)
        {
            if (levels == null || levels.Count == 0)
            {
                return null;
            }

            if (index < 0)
            {
                index = 0;
            }

            return levels[index % levels.Count];
        }
    }
}

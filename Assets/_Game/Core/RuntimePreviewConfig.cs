using UnityEngine;

namespace HexWords.Core
{
    [CreateAssetMenu(menuName = "HexWords/Runtime Preview Config", fileName = "RuntimePreviewConfig")]
    public class RuntimePreviewConfig : ScriptableObject
    {
        public LevelDefinition previewLevel;
    }
}

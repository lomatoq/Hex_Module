using System.Collections.Generic;
using UnityEngine;

namespace HexWords.Core
{
    [CreateAssetMenu(menuName = "HexWords/Dictionary Database", fileName = "DictionaryDatabase")]
    public class DictionaryDatabase : ScriptableObject
    {
        public List<DictionaryEntry> entries = new List<DictionaryEntry>();
    }
}

using System;

namespace HexWords.Core
{
    [Serializable]
    public struct CellDefinition
    {
        public string cellId;
        public string letter;
        public int q;
        public int r;
    }
}

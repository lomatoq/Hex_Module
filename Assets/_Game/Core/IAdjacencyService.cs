namespace HexWords.Core
{
    public interface IAdjacencyService
    {
        bool AreNeighbors(string fromCellId, string toCellId, GridShape shape);
    }
}

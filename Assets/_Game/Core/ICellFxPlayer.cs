namespace HexWords.Core
{
    public interface ICellFxPlayer
    {
        void OnSelected();
        void OnPathAccepted();
        void OnPathBonusAccepted();
        void OnPathAlreadyAccepted();
        void OnPathRejected();
    }
}

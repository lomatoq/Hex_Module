using UnityEngine;

namespace HexWords.Core
{
    [CreateAssetMenu(menuName = "HexWords/Feedback Palette", fileName = "FeedbackPalette")]
    public class FeedbackPalette : ScriptableObject
    {
        public Color selectedCellColor = new Color(0.85f, 0.95f, 1f, 1f);
        public Color targetAcceptedCellColor = new Color(0.75f, 1f, 0.75f, 1f);
        public Color bonusAcceptedCellColor = new Color(0.65f, 0.95f, 1f, 1f);
        public Color alreadyAcceptedCellColor = new Color(0.55f, 0.7f, 1f, 1f);
        public Color rejectedCellColor = new Color(1f, 0.8f, 0.8f, 1f);

        public Color hudTargetAcceptedColor = new Color(0.2f, 0.5f, 0.2f);
        public Color hudBonusAcceptedColor = new Color(0.1f, 0.55f, 0.65f);
        public Color hudAlreadyAcceptedColor = new Color(0.2f, 0.35f, 0.8f);
        public Color hudRejectedColor = new Color(0.7f, 0.2f, 0.2f);
        public Color hudCurrentWordColor = new Color(0.2f, 0.2f, 0.2f);
    }
}

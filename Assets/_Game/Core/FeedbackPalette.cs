using UnityEngine;

namespace HexWords.Core
{
    [CreateAssetMenu(menuName = "HexWords/Feedback Palette", fileName = "FeedbackPalette")]
    public class FeedbackPalette : ScriptableObject
    {
        [Header("HUD Word State Colors")]
        public Color hudTargetAcceptedColor  = new Color(0.2f,  0.5f,  0.2f);
        public Color hudBonusAcceptedColor   = new Color(0.1f,  0.55f, 0.65f);
        public Color hudAlreadyAcceptedColor = new Color(0.2f,  0.35f, 0.8f);
        public Color hudRejectedColor        = new Color(0.7f,  0.2f,  0.2f);
        public Color hudCurrentWordColor     = new Color(0.2f,  0.2f,  0.2f);

        [Header("Bubble Text Colors")]
        public Color hudBubbleTextDefault      = Color.black;
        public Color hudBubbleTextAlreadyFound = new Color(0.2f, 0.35f, 0.8f);

        [Header("Hex Cell — Letter Colors")]
        public Color cellLetterDefault  = Color.black;
        public Color cellLetterSelected = Color.white;

        [Header("Hex Cell — Background Flash Colors")]
        [Tooltip("Background tint when cell is selected (swipe hold) and hint pulse.")]
        public Color cellSelectedBackground = new Color(0.85f, 0.95f, 1f,  1f);
        [Tooltip("Background flash when a target word path is accepted.")]
        public Color cellAcceptedBackground = new Color(0.75f, 1f,   0.75f, 1f);
        [Tooltip("Background flash when a bonus word path is accepted.")]
        public Color cellBonusBackground    = new Color(0.65f, 0.95f, 1f,  1f);
        [Tooltip("Background flash when the path is rejected.")]
        public Color cellRejectedBackground = new Color(1f,   0.8f,  0.8f,  1f);

        [Header("Word State — Cell Hold Color (background + letter, held during score drop)")]
        public Color cellWordTargetColor       = new Color(0.6f,  1f,   0.6f, 1f);
        public Color cellWordBonusColor        = new Color(0.5f,  0.9f, 1f,   1f);
        public Color cellWordAlreadyFoundColor = new Color(0.7f,  0.75f, 1f,  1f);

        [Header("Swipe Trail")]
        [Tooltip("Default color of the swipe trail line and dots.")]
        public Color trailDefaultColor = new Color(0.30f, 0.18f, 0.10f, 0.80f);
    }
}

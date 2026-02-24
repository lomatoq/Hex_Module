using HexWords.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HexWords.UI
{
    public class LevelHudView : MonoBehaviour
    {
        [SerializeField] private Text levelText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Text lastWordText;
        [SerializeField] private FeedbackPalette feedbackPalette;

        public void SetLevel(string levelId)
        {
            if (levelText != null)
            {
                levelText.text = $"Level {levelId}";
            }
        }

        public void SetScore(int current, int target)
        {
            if (scoreText != null)
            {
                scoreText.text = $"{current}/{target}";
            }

            if (progressBar != null)
            {
                progressBar.maxValue = target;
                progressBar.value = current;
            }
        }

        public void SetLastWord(string text, bool accepted)
        {
            SetLastWord(text, accepted ? WordSubmitOutcome.TargetAccepted : WordSubmitOutcome.Rejected);
        }

        public void SetLastWord(string text, WordSubmitOutcome outcome)
        {
            if (lastWordText == null)
            {
                return;
            }

            lastWordText.text = text;
            lastWordText.color = GetHudColor(outcome);
        }

        public void SetCurrentWord(string text)
        {
            if (lastWordText == null)
            {
                return;
            }

            lastWordText.text = text;
            lastWordText.color = feedbackPalette != null
                ? feedbackPalette.hudCurrentWordColor
                : new Color(0.2f, 0.2f, 0.2f);
        }

        private Color GetHudColor(WordSubmitOutcome outcome)
        {
            if (feedbackPalette == null)
            {
                return outcome switch
                {
                    WordSubmitOutcome.TargetAccepted => new Color(0.2f, 0.5f, 0.2f),
                    WordSubmitOutcome.BonusAccepted => new Color(0.1f, 0.55f, 0.65f),
                    WordSubmitOutcome.AlreadyAccepted => new Color(0.2f, 0.35f, 0.8f),
                    _ => new Color(0.7f, 0.2f, 0.2f)
                };
            }

            return outcome switch
            {
                WordSubmitOutcome.TargetAccepted => feedbackPalette.hudTargetAcceptedColor,
                WordSubmitOutcome.BonusAccepted => feedbackPalette.hudBonusAcceptedColor,
                WordSubmitOutcome.AlreadyAccepted => feedbackPalette.hudAlreadyAcceptedColor,
                _ => feedbackPalette.hudRejectedColor
            };
        }
    }
}

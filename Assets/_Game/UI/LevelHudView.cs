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
            if (lastWordText == null)
            {
                return;
            }

            lastWordText.text = text;
            lastWordText.color = accepted ? new Color(0.2f, 0.5f, 0.2f) : new Color(0.7f, 0.2f, 0.2f);
        }
    }
}

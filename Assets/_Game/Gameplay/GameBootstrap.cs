using HexWords.Core;
using HexWords.UI;
using UnityEngine;

namespace HexWords.Gameplay
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private LevelDefinition levelDefinition;
        [SerializeField] private DictionaryDatabase dictionaryDatabase;
        [SerializeField] private GridView gridView;
        [SerializeField] private SwipeInputController inputController;
        [SerializeField] private LevelHudView hudView;
        [SerializeField] private GameObject levelCompletePanel;

        private LevelSessionController _session;

        private void Start()
        {
            var resolvedLevel = ResolveLevel();
            if (resolvedLevel == null)
            {
                Debug.LogError("GameBootstrap requires a LevelDefinition asset or RuntimePreviewConfig.previewLevel.");
                return;
            }

            var adjacency = new AdjacencyService();
            var validator = new WordValidator(dictionaryDatabase);
            var score = new ScoreService();
            _session = new LevelSessionController(validator, score);

            _session.ScoreChanged += OnScoreChanged;
            _session.LevelCompleted += OnLevelCompleted;
            _session.WordSubmitted += OnWordSubmitted;

            gridView.Build(resolvedLevel);
            inputController.Initialize(resolvedLevel, _session, adjacency);

            _session.StartSession();
            hudView.SetLevel(resolvedLevel.levelId);
            hudView.SetScore(0, resolvedLevel.targetScore);
            hudView.SetLastWord(string.Empty, true);

            if (levelCompletePanel != null)
            {
                levelCompletePanel.SetActive(false);
            }
        }

        private LevelDefinition ResolveLevel()
        {
            if (levelDefinition != null)
            {
                return levelDefinition;
            }

            var preview = Resources.Load<RuntimePreviewConfig>("HexWordsPreviewConfig");
            return preview != null ? preview.previewLevel : null;
        }

        private void OnDestroy()
        {
            if (_session == null)
            {
                return;
            }

            _session.ScoreChanged -= OnScoreChanged;
            _session.LevelCompleted -= OnLevelCompleted;
            _session.WordSubmitted -= OnWordSubmitted;
        }

        private void OnScoreChanged(int current, int target)
        {
            hudView.SetScore(current, target);
        }

        private void OnWordSubmitted(string word, bool accepted)
        {
            hudView.SetLastWord(word, accepted);
        }

        private void OnLevelCompleted()
        {
            if (levelCompletePanel != null)
            {
                levelCompletePanel.SetActive(true);
            }
        }
    }
}

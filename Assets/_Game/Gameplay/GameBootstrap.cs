using System.Collections;
using HexWords.Core;
using HexWords.UI;
using UnityEngine;

namespace HexWords.Gameplay
{
    public class GameBootstrap : MonoBehaviour
    {
        private const string PlayerPrefLevelIndex = "HexWords.CurrentLevelIndex";

        [SerializeField] private LevelDefinition levelDefinition;
        [SerializeField] private LevelCatalog levelCatalog;
        [SerializeField] private DictionaryDatabase dictionaryDatabase;
        [SerializeField] private GridView gridView;
        [SerializeField] private SwipeInputController inputController;
        [SerializeField] private LevelHudView hudView;
        [SerializeField] private GameObject levelCompletePanel;
        [SerializeField] private bool autoAdvanceToNextLevel = true;
        [SerializeField] private float nextLevelDelaySeconds = 1.2f;

        private LevelSessionController _session;
        private Coroutine _advanceRoutine;
        private int _currentLevelIndex;

        private void Start()
        {
            _currentLevelIndex = PlayerPrefs.GetInt(PlayerPrefLevelIndex, 0);
            LoadCurrentLevel();
        }

        private void OnDestroy()
        {
            TearDownSession();
        }

        public void NextLevel()
        {
            if (levelCatalog == null || levelCatalog.Count == 0)
            {
                return;
            }

            _currentLevelIndex = (_currentLevelIndex + 1) % levelCatalog.Count;
            PlayerPrefs.SetInt(PlayerPrefLevelIndex, _currentLevelIndex);
            PlayerPrefs.Save();
            LoadCurrentLevel();
        }

        private void LoadCurrentLevel()
        {
            var resolvedLevel = ResolveLevel();
            if (resolvedLevel == null)
            {
                Debug.LogError("GameBootstrap requires a LevelDefinition, LevelCatalog, or RuntimePreviewConfig.previewLevel.");
                return;
            }

            TearDownSession();

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
            if (levelCatalog == null)
            {
                levelCatalog = Resources.Load<LevelCatalog>("LevelCatalog");
            }

            if (levelCatalog != null && levelCatalog.Count > 0)
            {
                return levelCatalog.GetAt(_currentLevelIndex);
            }

            if (levelDefinition != null)
            {
                return levelDefinition;
            }

            var preview = Resources.Load<RuntimePreviewConfig>("HexWordsPreviewConfig");
            return preview != null ? preview.previewLevel : null;
        }

        private void TearDownSession()
        {
            if (_advanceRoutine != null)
            {
                StopCoroutine(_advanceRoutine);
                _advanceRoutine = null;
            }

            inputController.Unsubscribe();

            if (_session == null)
            {
                return;
            }

            _session.ScoreChanged -= OnScoreChanged;
            _session.LevelCompleted -= OnLevelCompleted;
            _session.WordSubmitted -= OnWordSubmitted;
            _session = null;
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

            if (autoAdvanceToNextLevel)
            {
                _advanceRoutine = StartCoroutine(AdvanceAfterDelay());
            }
        }

        private IEnumerator AdvanceAfterDelay()
        {
            yield return new WaitForSeconds(nextLevelDelaySeconds);
            NextLevel();
        }
    }
}

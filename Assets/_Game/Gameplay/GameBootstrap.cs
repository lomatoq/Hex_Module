using System;
using System.Collections;
using HexWords.Core;
using HexWords.UI;
using UnityEngine;

namespace HexWords.Gameplay
{
    public class GameBootstrap : MonoBehaviour
    {
        private const string PrefLevelIndex  = "HexWords.CurrentLevelIndex";
        private const string PrefFirstLaunch = "HexWords.FirstLaunchDone";
        private const int    CoinRewardPerLevel = 25;

        // ── Scene references ───────────────────────────────────────────────
        [Header("Level Data")]
        [SerializeField] private LevelDefinition levelDefinition;
        [SerializeField] private LevelCatalog levelCatalog;
        [SerializeField] private DictionaryDatabase dictionaryDatabase;

        [Header("Gameplay")]
        [SerializeField] private GridView gridView;
        [SerializeField] private SwipeInputController inputController;

        [Header("UI")]
        [SerializeField] private LevelHudView hudView;
        [SerializeField] private LevelCompleteView levelCompleteView;
        [SerializeField] private HomeScreenView homeScreenView;
        [SerializeField] private SettingsPausePopup settingsPopup;
        [SerializeField] private FoundWordsScreen foundWordsScreen;
        [SerializeField] private TutorialController tutorialController;

        [Header("Ads / Analytics")]
        [SerializeField] private AdsManager adsManager;

        [Header("Legacy (keep for backwards compat)")]
        [SerializeField] private GameObject levelCompletePanel; // old panel — used only if levelCompleteView is null

        // ── State ──────────────────────────────────────────────────────────
        private LevelSessionController _session;
        private LevelDefinition        _currentLevel;
        private ScoreService           _scoreService;
        private BoosterHintService     _hintService;
        private CoinWallet             _wallet;
        private int                    _currentLevelIndex;

        // ── Unity lifecycle ────────────────────────────────────────────────

        private void Start()
        {
            // Initialize remote config (fetches defaults synchronously for now)
            RemoteConfigService.Instance.FetchAsync(OnRemoteConfigReady);
        }

        private void OnDestroy()
        {
            TearDownSession();
            UnsubscribeHud();

            if (_wallet != null)
                _wallet.BalanceChanged -= OnBalanceChanged;
            if (_hintService != null)
                _hintService.ChargesChanged -= OnHintChargesChanged;
        }

        // ── Boot flow ──────────────────────────────────────────────────────

        private void OnRemoteConfigReady()
        {
            _wallet       = CoinWallet.Instance;
            _scoreService = new ScoreService();
            _hintService  = new BoosterHintService(_scoreService);

            _wallet.BalanceChanged  += OnBalanceChanged;
            _hintService.ChargesChanged += OnHintChargesChanged;

            _currentLevelIndex = PlayerPrefs.GetInt(PrefLevelIndex, 0);

            bool firstLaunch = PlayerPrefs.GetInt(PrefFirstLaunch, 0) == 0;
            bool skipHome    = RemoteConfigService.Get<bool>("skipHomeScreenFirstLaunch");

            if (firstLaunch)
            {
                PlayerPrefs.SetInt(PrefFirstLaunch, 1);
                PlayerPrefs.Save();

                if (skipHome)
                    StartGame();
                else
                    ShowHomeScreen();
            }
            else
            {
                ShowHomeScreen();
            }
        }

        // ── Home screen ────────────────────────────────────────────────────

        private void ShowHomeScreen()
        {
            if (homeScreenView != null)
            {
                homeScreenView.SetCurrentLevel(_currentLevelIndex + 1);
                homeScreenView.SetCoins(_wallet?.Balance ?? 0);
                homeScreenView.Show();
                homeScreenView.OnPlayClicked           -= StartGame;
                homeScreenView.OnSettingsClicked       -= ShowSettings;
                homeScreenView.OnDailyChallengeClicked -= OnDailyChallengeClicked;
                homeScreenView.OnPlayClicked           += StartGame;
                homeScreenView.OnSettingsClicked       += ShowSettings;
                homeScreenView.OnDailyChallengeClicked += OnDailyChallengeClicked;
            }
            else
            {
                StartGame();
            }
        }

        /// <summary>Dev-only: jump directly to a specific level index.</summary>
        public void JumpToLevel(int levelIndex)
        {
            _currentLevelIndex = Mathf.Max(0, levelIndex);
            PlayerPrefs.SetInt(PrefLevelIndex, _currentLevelIndex);
            PlayerPrefs.Save();

            // Close home screen if open
            if (homeScreenView != null)
            {
                homeScreenView.OnPlayClicked         -= StartGame;
                homeScreenView.OnSettingsClicked     -= ShowSettings;
                homeScreenView.OnDailyChallengeClicked -= OnDailyChallengeClicked;
                homeScreenView.Hide();
            }

            LoadCurrentLevel();
        }

        public void StartGame()
        {
            if (homeScreenView != null)
            {
                homeScreenView.OnPlayClicked         -= StartGame;
                homeScreenView.OnSettingsClicked     -= ShowSettings;
                homeScreenView.OnDailyChallengeClicked -= OnDailyChallengeClicked;
                homeScreenView.Hide();
            }

            LoadCurrentLevel();
        }

        public void GoToHomeScreen()
        {
            TearDownSession();

            adsManager?.TryShowInterstitial(AdPlacement.HomeExit);

            ShowHomeScreen();
        }

        // ── Level loading ──────────────────────────────────────────────────

        public void NextLevel()
        {
            if (levelCatalog == null || levelCatalog.Count == 0) return;

            _currentLevelIndex = (_currentLevelIndex + 1) % levelCatalog.Count;
            PlayerPrefs.SetInt(PrefLevelIndex, _currentLevelIndex);
            PlayerPrefs.Save();

            LoadCurrentLevel();
        }

        private void LoadCurrentLevel()
        {
            _currentLevel = ResolveLevel();
            if (_currentLevel == null)
            {
                Debug.LogError("[GameBootstrap] No level found. Assign a LevelCatalog or LevelDefinition.");
                return;
            }

            TearDownSession();

            var adjacency = new AdjacencyService();
            var validator = new WordValidator(dictionaryDatabase);
            _session = new LevelSessionController(validator, _scoreService);

            _session.ScoreChanged          += OnScoreChanged;
            _session.LevelCompleted        += OnLevelCompleted;
            _session.WordSubmittedDetailed += OnWordSubmittedDetailed;
            _session.StreakChanged         += OnStreakChanged;

            // Normalise level shape in-memory so both Build and Initialize
            // use consistent canonical coordinates (fixes c3 adjacency after grid fix)
            if (_currentLevel.boardLayoutMode == BoardLayoutMode.Fixed16Symmetric &&
                _currentLevel.shape?.cells != null &&
                !HexBoardTemplate16.HasCanonicalShape(_currentLevel.shape))
            {
                HexBoardTemplate16.ApplyCanonicalLayout(_currentLevel.shape.cells);
            }

            gridView.Build(_currentLevel);
            inputController.Initialize(_currentLevel, _session, adjacency);
            _session.StartSession();

            // HUD
            hudView.SetLevel(_currentLevel.levelId);
            hudView.SetScore(0, _currentLevel.targetScore);
            hudView.SetCurrentWord(string.Empty);
            hudView.SetCoins(_wallet?.Balance ?? 0);
            hudView.SetStreak(0);

            int hintCharges   = _hintService?.Charges ?? 0;
            bool rvAvailable  = adsManager?.IsRewardedAvailable() ?? false;
            hudView.SetHintCharges(hintCharges, rvAvailable);

            SubscribeHud();
            HideWinScreen();

            // Tutorial
            bool tutorialEnabled = RemoteConfigService.Get<int>("interactive_tutor") != 0;
            if (tutorialController != null && tutorialEnabled)
            {
                if (_currentLevelIndex == 0)
                    tutorialController.StartSwipeTutorial(_currentLevel, gridView);
                else if (_currentLevelIndex == 1)
                    tutorialController.StartHintTutorial(hudView);
            }

            // Interstitial — game start
            adsManager?.TryShowInterstitial(AdPlacement.GameStart);

            // Analytics
            AnalyticsManager.LogEvent("level_started",
                ("level_index", _currentLevelIndex),
                ("level_id",    _currentLevel.levelId));
        }

        private LevelDefinition ResolveLevel()
        {
            if (levelCatalog == null)
                levelCatalog = Resources.Load<LevelCatalog>("LevelCatalog");

            if (levelCatalog != null && levelCatalog.Count > 0)
                return levelCatalog.GetAt(_currentLevelIndex);

            if (levelDefinition != null)
                return levelDefinition;

            var preview = Resources.Load<RuntimePreviewConfig>("HexWordsPreviewConfig");
            return preview != null ? preview.previewLevel : null;
        }

        // ── Session teardown ───────────────────────────────────────────────

        private void TearDownSession()
        {
            if (_session == null) return;

            _session.ScoreChanged          -= OnScoreChanged;
            _session.LevelCompleted        -= OnLevelCompleted;
            _session.WordSubmittedDetailed -= OnWordSubmittedDetailed;
            _session.StreakChanged         -= OnStreakChanged;
            _session = null;

            inputController.Unsubscribe();
        }

        // ── HUD event wiring ───────────────────────────────────────────────

        private void SubscribeHud()
        {
            if (hudView == null) return;
            UnsubscribeHud(); // prevent duplicate subscriptions on level reload
            hudView.OnHintClicked       += OnHintClicked;
            hudView.OnSettingsClicked   += ShowSettingsInGame;
            hudView.OnFoundWordsClicked += ShowFoundWords;

            if (settingsPopup != null)
                settingsPopup.OnMainMenuClicked += GoToHomeScreen;
        }

        private void UnsubscribeHud()
        {
            if (hudView == null) return;
            hudView.OnHintClicked       -= OnHintClicked;
            hudView.OnSettingsClicked   -= ShowSettingsInGame;
            hudView.OnFoundWordsClicked -= ShowFoundWords;

            if (settingsPopup != null)
                settingsPopup.OnMainMenuClicked -= GoToHomeScreen;
        }

        // ── Session callbacks ──────────────────────────────────────────────

        private void OnScoreChanged(int current, int target)
        {
            hudView.SetScore(current, target);
        }

        private void OnStreakChanged(int streak)
        {
            hudView.SetStreak(streak);
        }

        private void OnWordSubmittedDetailed(string word, WordSubmitOutcome outcome, ValidationReason reason)
        {
            hudView.SetLastWord(word, outcome);

            // Interstitial — game interrupt (mid-game, after collecting a category)
            if (outcome == WordSubmitOutcome.TargetAccepted)
                adsManager?.TryShowInterstitial(AdPlacement.GameInterrupt);
        }

        private void OnLevelCompleted()
        {
            // Interstitial — game end (before win screen)
            adsManager?.TryShowInterstitial(AdPlacement.GameEnd);

            // Coins
            _wallet?.Add(CoinRewardPerLevel);

            // Win screen
            if (levelCompleteView != null)
            {
                levelCompleteView.OnCoinRewardCollected += OnCoinRewardCollected;
                levelCompleteView.OnNextLevelClicked    += OnWinNextLevel;
                levelCompleteView.OnMainMenuClicked     += GoToHomeScreen;
                levelCompleteView.Show(_currentLevelIndex + 1, CoinRewardPerLevel);
            }
            else if (levelCompletePanel != null)
            {
                levelCompletePanel.SetActive(true);
            }

            // Rate Us check
            CheckRateUsPopup();

            AnalyticsManager.LogEvent("level_completed",
                ("level_index", _currentLevelIndex),
                ("level_id",    _currentLevel?.levelId ?? ""));
        }

        private void OnCoinRewardCollected(int amount)
        {
            if (levelCompleteView == null) return;
            levelCompleteView.OnCoinRewardCollected -= OnCoinRewardCollected;
        }

        private void OnWinNextLevel()
        {
            if (levelCompleteView != null)
            {
                levelCompleteView.OnNextLevelClicked -= OnWinNextLevel;
                levelCompleteView.OnMainMenuClicked  -= GoToHomeScreen;
                levelCompleteView.Hide();
            }
            NextLevel();
        }

        // ── Win screen helpers ─────────────────────────────────────────────

        private void HideWinScreen()
        {
            if (levelCompleteView != null)
            {
                levelCompleteView.OnCoinRewardCollected -= OnCoinRewardCollected;
                levelCompleteView.OnNextLevelClicked    -= OnWinNextLevel;
                levelCompleteView.OnMainMenuClicked     -= GoToHomeScreen;
                levelCompleteView.Hide();
            }
            if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
        }

        // ── Coin / Hint callbacks ──────────────────────────────────────────

        private void OnBalanceChanged(int balance)
        {
            hudView.SetCoins(balance);
            if (homeScreenView != null) homeScreenView.SetCoins(balance);
        }

        private void OnHintChargesChanged(int charges)
        {
            bool rvAvailable = adsManager?.IsRewardedAvailable() ?? false;
            hudView.SetHintCharges(charges, rvAvailable);
        }

        private void OnHintClicked()
        {
            if (_hintService == null || _session == null || _currentLevel == null) return;

            if (_hintService.Charges > 0)
            {
                _hintService.HintRevealed += OnHintRevealed;
                _hintService.UseHint(_currentLevel, _session.State);
                _hintService.HintRevealed -= OnHintRevealed;
            }
            else
            {
                // No charges → try rewarded video
                adsManager?.ShowRewardedVideo(success =>
                {
                    if (success) _hintService.RefillViaRV();
                });
            }
        }

        private void OnHintRevealed(string word, int _)
        {
            if (_currentLevel == null || gridView == null) return;

            var path = HintPathFinder.FindPath(word, _currentLevel.shape, new AdjacencyService());
            if (path == null) return;

            int count = Math.Min(gridView.HintRevealCount, path.Count);
            gridView.PlayHintAnimation(path.GetRange(0, count));
        }

        // ── Settings / Found Words ─────────────────────────────────────────

        private void ShowSettings()
        {
            settingsPopup?.Show(inGame: false);
        }

        private void ShowSettingsInGame()
        {
            settingsPopup?.Show(inGame: true);
        }

        private void ShowFoundWords()
        {
            if (foundWordsScreen != null && _session != null)
                foundWordsScreen.Show(_session.State);
        }

        // ── Daily Challenge ────────────────────────────────────────────────

        private void OnDailyChallengeClicked()
        {
            // Daily Challenge is post-MVP; placeholder for the scene/manager entry point.
            Debug.Log("[GameBootstrap] Daily Challenge not yet implemented.");
        }

        // ── Rate Us ────────────────────────────────────────────────────────

        private void CheckRateUsPopup()
        {
            // Handled by RateUsPopup component if present in scene
        }
    }
}

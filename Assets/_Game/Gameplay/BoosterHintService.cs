using System;
using System.Collections.Generic;
using HexWords.Core;
using UnityEngine;

namespace HexWords.Gameplay
{
    /// <summary>
    /// Manages the Hint booster: tracks charge count, finds the highest-scoring
    /// remaining target word on the current board, and reveals its first 2 letters.
    /// </summary>
    public class BoosterHintService
    {
        private const string PrefKeyCharges   = "HexWords.HintCharges";
        private const string PrefKeyInitDone  = "HexWords.HintInitDone";
        private const int    RevealCount      = 2;

        private readonly IScoreService _scoreService;

        public int Charges { get; private set; }

        // ── Events ─────────────────────────────────────────────────────────
        /// <summary>Fired after a successful hint. (word, revealCount)</summary>
        public event Action<string, int> HintRevealed;
        /// <summary>Fired whenever charge count changes.</summary>
        public event Action<int> ChargesChanged;

        public BoosterHintService(IScoreService scoreService)
        {
            _scoreService = scoreService;

            if (PlayerPrefs.GetInt(PrefKeyInitDone, 0) == 0)
            {
                int initialCharges = RemoteConfigService.Get<int>("hint_initial_charges");
                Charges = initialCharges > 0 ? initialCharges : 5;
                PersistCharges();
                PlayerPrefs.SetInt(PrefKeyInitDone, 1);
                PlayerPrefs.Save();
            }
            else
            {
                Charges = PlayerPrefs.GetInt(PrefKeyCharges, 5);
            }
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to use a hint charge. Finds the highest-scoring unused target word,
        /// reveals its first letters, and fires HintRevealed.
        /// Returns the word string if successful, null if no charges or no valid word.
        /// </summary>
        public string UseHint(LevelDefinition level, LevelSessionState state)
        {
            if (Charges <= 0)
            {
                Debug.LogWarning("[BoosterHintService] No hint charges remaining.");
                return null;
            }

            var word = FindBestUnrevealedWord(level, state);
            if (word == null)
            {
                Debug.Log("[BoosterHintService] No remaining target words to hint.");
                return null;
            }

            Charges--;
            PersistCharges();
            ChargesChanged?.Invoke(Charges);

            HintRevealed?.Invoke(word, RevealCount);

            AnalyticsManager.LogEvent("hint_used",
                ("charges_remaining", Charges),
                ("word_length",       word.Length));

            return word;
        }

        /// <summary>Adds charges received from a Rewarded Video completion.</summary>
        public void RefillViaRV()
        {
            int reward = RemoteConfigService.Get<int>("rvHintReward");
            if (reward <= 0) reward = 1;

            AddCharges(reward);

            AnalyticsManager.LogEvent("rv_watched",
                ("placement", "hint_refill"),
                ("charges_added", reward));
        }

        /// <summary>Spends coins to purchase one hint charge. Returns true if successful.</summary>
        public bool TryRefillViaCoins(CoinWallet wallet)
        {
            int cost = RemoteConfigService.Get<int>("hint_charge_cost_coins");
            if (cost <= 0) cost = 100;

            if (!wallet.TrySpend(cost))
            {
                Debug.Log("[BoosterHintService] Not enough coins for hint refill.");
                return false;
            }

            AddCharges(1);
            return true;
        }

        // ── Internal ───────────────────────────────────────────────────────

        private string FindBestUnrevealedWord(LevelDefinition level, LevelSessionState state)
        {
            if (level?.targetWords == null) return null;

            string bestWord  = null;
            int    bestScore = -1;

            foreach (var word in level.targetWords)
            {
                if (string.IsNullOrEmpty(word)) continue;

                var normalized = WordNormalizer.Normalize(word);

                // Skip already accepted words
                if (state.acceptedTargetWords.Contains(normalized)) continue;

                var score = _scoreService.ScoreWord(normalized, level);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestWord  = normalized;
                }
            }

            return bestWord;
        }

        private void AddCharges(int amount)
        {
            Charges += amount;
            PersistCharges();
            ChargesChanged?.Invoke(Charges);
        }

        private void PersistCharges()
        {
            PlayerPrefs.SetInt(PrefKeyCharges, Charges);
            PlayerPrefs.Save();
        }

        // Editor / test helper
        public static void ResetForTesting()
        {
            PlayerPrefs.DeleteKey(PrefKeyCharges);
            PlayerPrefs.DeleteKey(PrefKeyInitDone);
            PlayerPrefs.Save();
        }
    }
}

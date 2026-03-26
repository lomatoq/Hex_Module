using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexWords.Core
{
    /// <summary>
    /// Remote config service with hardcoded defaults.
    /// When Firebase Remote Config SDK is integrated, replace FetchAsync()
    /// to fetch from Firebase and override the defaults dict.
    /// </summary>
    public class RemoteConfigService
    {
        private static RemoteConfigService _instance;
        public static RemoteConfigService Instance => _instance ??= new RemoteConfigService();

        private readonly Dictionary<string, object> _fetched = new();

        private static readonly Dictionary<string, object> Defaults = new()
        {
            // Ads — banner
            { "bannerLevelStart",           2 },

            // Ads — interstitials
            { "interGameStartLevel",        4 },
            { "interGameEndLevel",          3 },
            { "interGameInterrupt",         3 },
            { "interHomeExitLevel",         2 },
            { "interInterval",              120 },   // seconds
            { "interInterruptInterval",     240 },   // seconds
            { "rvToInterInterval",          30 },    // seconds

            // Ads — rewarded video
            { "rvHintReward",               1 },     // hint charges per RV

            // Booster Hint
            { "startHintsCount",            3 },
            { "hint_initial_charges",       5 },
            { "hint_charge_cost_coins",     100 },

            // Tutorial
            { "interactive_tutor",          1 },     // 1 = enabled

            // Home screen
            { "skipHomeScreenFirstLaunch",  false },

            // Rate Us — comma-separated level numbers
            { "ratePopupLevelTrigger",      "3,5" },

            // Daily Challenge rewards
            { "daily_bronze_reward_coins",  100 },
            { "daily_silver_reward_coins",  200 },
            { "daily_gold_reward_coins",    500 },
        };

        private bool _fetching;

        // ── Public API ────────────────────────────────────────────────────────

        public static T Get<T>(string key) => Instance.GetValue<T>(key);

        /// <summary>
        /// Call once on app startup. In a real implementation this would
        /// hit Firebase Remote Config. For now it resolves immediately.
        /// </summary>
        public void FetchAsync(Action onComplete = null)
        {
            if (_fetching)
            {
                onComplete?.Invoke();
                return;
            }

            _fetching = true;

            // TODO: When Firebase SDK is integrated, replace this block with:
            //   FirebaseRemoteConfig.DefaultInstance.FetchAndActivateAsync()
            //   then populate _fetched from FirebaseRemoteConfig.DefaultInstance
            // For now just resolve with defaults immediately.

            onComplete?.Invoke();
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private T GetValue<T>(string key)
        {
            // Prefer fetched value, fall back to defaults
            if (_fetched.TryGetValue(key, out var fetched))
            {
                return ConvertValue<T>(fetched);
            }

            if (Defaults.TryGetValue(key, out var def))
            {
                return ConvertValue<T>(def);
            }

            Debug.LogWarning($"[RemoteConfigService] Unknown key '{key}', returning default({typeof(T).Name})");
            return default;
        }

        private static T ConvertValue<T>(object value)
        {
            if (value is T cast)
                return cast;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                Debug.LogWarning($"[RemoteConfigService] Cannot convert {value?.GetType().Name} to {typeof(T).Name}");
                return default;
            }
        }
    }
}

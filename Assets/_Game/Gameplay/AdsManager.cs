using System;
using HexWords.Core;
using UnityEngine;

namespace HexWords.Gameplay
{
    /// <summary>
    /// Ads manager stub — implements all placement logic and cooldowns.
    /// When Google Mobile Ads (AdMob) SDK is integrated:
    ///   1. Replace ShowBannerInternal(), ShowInterstitialInternal(), ShowRewardedInternal()
    ///      with actual AdMob calls.
    ///   2. Replace IsRewardedAvailableInternal() with AdMob ready-check.
    ///   3. Call InitUMP() from Initialize() to show GDPR consent.
    /// </summary>
    public class AdsManager : MonoBehaviour
    {
        private const string PrefLastInterTime = "HexWords.LastInterTimestamp";
        private const string PrefLastRVTime    = "HexWords.LastRVTimestamp";

        // ── Remote config keys (matches wiki Remote configs page) ──────────
        private int BannerLevelStart      => RemoteConfigService.Get<int>("bannerLevelStart");
        private int InterGameStartLevel   => RemoteConfigService.Get<int>("interGameStartLevel");
        private int InterGameEndLevel     => RemoteConfigService.Get<int>("interGameEndLevel");
        private int InterGameInterrupt    => RemoteConfigService.Get<int>("interGameInterrupt");
        private int InterHomeExitLevel    => RemoteConfigService.Get<int>("interHomeExitLevel");
        private int InterInterval         => RemoteConfigService.Get<int>("interInterval");
        private int InterInterruptInterval => RemoteConfigService.Get<int>("interInterruptInterval");
        private int RvToInterInterval     => RemoteConfigService.Get<int>("rvToInterInterval");

        private int _currentPlayerLevel;
        private float _levelStartTime;

        // ── Public API ─────────────────────────────────────────────────────

        public void Initialize(int playerLevel)
        {
            _currentPlayerLevel = playerLevel;
            _levelStartTime     = Time.realtimeSinceStartup;

            // TODO: Call InitUMP() for GDPR consent on first launch
            // TODO: Initialize AdMob MobileAds.Initialize(...)

            if (_currentPlayerLevel >= BannerLevelStart)
                ShowBannerInternal();
        }

        public void OnLevelChanged(int playerLevel)
        {
            _currentPlayerLevel = playerLevel;
            _levelStartTime     = Time.realtimeSinceStartup;

            if (playerLevel >= BannerLevelStart)
                ShowBannerInternal();
        }

        /// <summary>
        /// Tries to show an interstitial for the given placement.
        /// All cooldown/level-threshold logic is applied here.
        /// </summary>
        public void TryShowInterstitial(AdPlacement placement)
        {
            if (!MeetsLevelThreshold(placement)) return;
            if (!MeetsIntervalCooldown(placement)) return;

            ShowInterstitialInternal();
            PlayerPrefs.SetFloat(PrefLastInterTime, Time.realtimeSinceStartup);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Requests a rewarded video. Calls back with success=true if the
        /// user watched it to completion.
        /// </summary>
        public void ShowRewardedVideo(Action<bool> onComplete)
        {
            // TODO: Use AdMob RewardedAd.Show() and pass the result
            // Stub: simulate a successful watch in editor
            Debug.Log("[AdsManager] ShowRewardedVideo — stub (SDK not integrated).");
#if UNITY_EDITOR
            PlayerPrefs.SetFloat(PrefLastRVTime, Time.realtimeSinceStartup);
            PlayerPrefs.Save();
            onComplete?.Invoke(true);
#else
            ShowRewardedInternal(onComplete);
#endif
        }

        public bool IsRewardedAvailable()
        {
            // TODO: return AdMob rewardedAd.CanShowAd()
            return true; // assume available until SDK is integrated
        }

        // ── Internal stubs (replace with AdMob calls) ──────────────────────

        private void ShowBannerInternal()
        {
            Debug.Log("[AdsManager] ShowBanner — SDK not integrated.");
            // TODO: bannerView.Show();
        }

        private void ShowInterstitialInternal()
        {
            Debug.Log("[AdsManager] ShowInterstitial — SDK not integrated.");
            // TODO: interstitialAd.Show();
        }

        private void ShowRewardedInternal(Action<bool> onComplete)
        {
            Debug.Log("[AdsManager] ShowRewarded — SDK not integrated.");
            // TODO: rewardedAd.Show(_ => onComplete(true));
            onComplete?.Invoke(false);
        }

        // ── Cooldown helpers ───────────────────────────────────────────────

        private bool MeetsLevelThreshold(AdPlacement placement)
        {
            int required = placement switch
            {
                AdPlacement.GameStart     => InterGameStartLevel,
                AdPlacement.GameEnd       => InterGameEndLevel,
                AdPlacement.GameInterrupt => InterGameInterrupt,
                AdPlacement.HomeExit      => InterHomeExitLevel,
                _                         => int.MaxValue
            };
            return _currentPlayerLevel >= required;
        }

        private bool MeetsIntervalCooldown(AdPlacement placement)
        {
            float now = Time.realtimeSinceStartup;

            // RV → Inter cooldown
            float lastRV = PlayerPrefs.GetFloat(PrefLastRVTime, -9999f);
            if (now - lastRV < RvToInterInterval) return false;

            if (placement == AdPlacement.GameInterrupt)
            {
                // Additional: must wait interInterruptInterval since level start
                float sinceStart = now - _levelStartTime;
                if (sinceStart < InterInterruptInterval) return false;
            }

            // General inter-to-inter cooldown
            float lastInter = PlayerPrefs.GetFloat(PrefLastInterTime, -9999f);
            return now - lastInter >= InterInterval;
        }
    }
}

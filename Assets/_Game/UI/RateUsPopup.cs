using HexWords.Core;
using HexWords.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HexWords.UI
{
    /// <summary>
    /// Rate Us popup per wiki spec.
    /// • 5★ → opens App Store / Google Play.
    /// • 1–4★ → shows "Thank you" sub-popup, auto-hides in 2 s.
    /// • Shown at levels specified by remote config "ratePopupLevelTrigger" (e.g. "3,5").
    /// • Once the player gives 5★, never shown again.
    /// </summary>
    public class RateUsPopup : MonoBehaviour
    {
        private const string PrefShownKey    = "HexWords.RateUsShown_";
        private const string PrefFiveStarKey = "HexWords.RateUsGaveFiveStar";

        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("Stars (index 0 = 1 star … index 4 = 5 stars)")]
        [SerializeField] private Button[] starButtons;
        [SerializeField] private Image[]  starImages;
        [SerializeField] private Color    starSelectedColor   = Color.yellow;
        [SerializeField] private Color    starUnselectedColor = Color.gray;

        [Header("Action")]
        [SerializeField] private Button rateNowButton;

        [Header("Close")]
        [SerializeField] private Button closeButton;

        [Header("Thank-you sub-popup")]
        [SerializeField] private GameObject thankYouPopup;
        [SerializeField] private float thankYouAutoHideSeconds = 2f;

        [Header("Store URLs")]
        [SerializeField] private string appStoreUrl    = "https://apps.apple.com/app/idXXXXXXXXXX";
        [SerializeField] private string googlePlayUrl  = "https://play.google.com/store/apps/details?id=com.yourcompany.yourapp";

        private int _selectedStars;
        private int _currentLevelNumber;

        private void Awake()
        {
            for (int i = 0; i < starButtons.Length; i++)
            {
                int starIndex = i; // capture
                if (starButtons[i] != null)
                    starButtons[i].onClick.AddListener(() => OnStarSelected(starIndex + 1));
            }

            if (rateNowButton != null)
                rateNowButton.onClick.AddListener(OnRateNow);

            SetRootVisible(false);
            if (thankYouPopup != null) thankYouPopup.SetActive(false);
        }

        private void OnEnable()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnDismissed);
                closeButton.onClick.AddListener(OnDismissed);
            }
        }

        private void OnDisable()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnDismissed);
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Call after each level completion. Popup shows only if:
        /// • levelNumber matches a trigger in remote config,
        /// • not already shown for that level,
        /// • player has never given 5★.
        /// </summary>
        public void TryShow(int levelNumber)
        {
            if (PlayerPrefs.GetInt(PrefFiveStarKey, 0) == 1) return;
            if (PlayerPrefs.GetInt(PrefShownKey + levelNumber, 0) == 1) return;
            if (!IsLevelTrigger(levelNumber)) return;

            Show(levelNumber);
        }

        public void Show(int levelNumber)
        {
            _currentLevelNumber = levelNumber;
            _selectedStars      = 0;

            RefreshStarVisuals(0);
            SetRootVisible(true);

            AnalyticsManager.LogEvent("rate_us_popup_shown", ("level_number", levelNumber));

            PlayerPrefs.SetInt(PrefShownKey + levelNumber, 1);
            PlayerPrefs.Save();
        }

        public void Hide()
        {
            SetRootVisible(false);
        }

        // ── Interaction handlers ───────────────────────────────────────────

        private void OnStarSelected(int stars)
        {
            _selectedStars = stars;
            RefreshStarVisuals(stars);

            AnalyticsManager.LogEvent("rate_us_star_selected",
                ("stars_selected", stars),
                ("level_number",   _currentLevelNumber));
        }

        private void OnRateNow()
        {
            if (_selectedStars == 0) return; // require a selection

            if (_selectedStars == 5)
            {
                PlayerPrefs.SetInt(PrefFiveStarKey, 1);
                PlayerPrefs.Save();

                AnalyticsManager.LogEvent("rate_us_five_star_click",
                    ("level_number", _currentLevelNumber));

#if UNITY_IOS
                Application.OpenURL(appStoreUrl);
#else
                Application.OpenURL(googlePlayUrl);
#endif
                Hide();
            }
            else
            {
                AnalyticsManager.LogEvent("rate_us_less_five_star_click",
                    ("level_number", _currentLevelNumber));

                ShowThankYouPopup();
            }
        }

        private void OnDismissed()
        {
            AnalyticsManager.LogEvent("rate_us_dismissed",
                ("level_number", _currentLevelNumber));
            Hide();
        }

        // ── Thank-you sub-popup ────────────────────────────────────────────

        private void ShowThankYouPopup()
        {
            Hide();
            if (thankYouPopup != null)
            {
                thankYouPopup.SetActive(true);
                StartCoroutine(AutoHideThankYou());
            }
        }

        private System.Collections.IEnumerator AutoHideThankYou()
        {
            yield return new WaitForSeconds(thankYouAutoHideSeconds);
            if (thankYouPopup != null)
                thankYouPopup.SetActive(false);
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private bool IsLevelTrigger(int levelNumber)
        {
            var raw = RemoteConfigService.Get<string>("ratePopupLevelTrigger");
            if (string.IsNullOrEmpty(raw)) return false;

            foreach (var part in raw.Split(','))
            {
                if (int.TryParse(part.Trim(), out int trigger) && trigger == levelNumber)
                    return true;
            }
            return false;
        }

        private void RefreshStarVisuals(int filledCount)
        {
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                    starImages[i].color = i < filledCount ? starSelectedColor : starUnselectedColor;
            }
        }

        private void SetRootVisible(bool visible)
        {
            if (root != null)
                root.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }
    }
}

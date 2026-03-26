using HexWords.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HexWords.UI
{
    /// <summary>
    /// Initial splash screen shown on app launch.
    /// Fetches remote config in the background, then transitions to HomeScreenView.
    /// </summary>
    public class SplashScreen : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Slider loadingBar;
        [SerializeField] private Text   loadingText;

        [Header("Transition")]
        [SerializeField] private float minDisplaySeconds = 1.5f;
        [SerializeField] private HomeScreenView homeScreenView;

        private float _elapsed;
        private bool  _configReady;
        private bool  _transitioned;

        private void Start()
        {
            if (loadingBar != null)
            {
                loadingBar.minValue = 0f;
                loadingBar.maxValue = 1f;
                loadingBar.value    = 0f;
            }

            RemoteConfigService.Instance.FetchAsync(OnConfigReady);
        }

        private void Update()
        {
            if (_transitioned) return;

            _elapsed += Time.deltaTime;

            // Animate loading bar up to 90% while waiting for config
            if (!_configReady && loadingBar != null)
                loadingBar.value = Mathf.Min(0.9f, _elapsed / (minDisplaySeconds * 0.9f));

            if (_configReady && _elapsed >= minDisplaySeconds)
                Transition();
        }

        private void OnConfigReady()
        {
            _configReady = true;

            if (loadingBar != null)
                loadingBar.value = 1f;

            if (loadingText != null)
                loadingText.text = "Ready!";

            if (_elapsed >= minDisplaySeconds && !_transitioned)
                Transition();
        }

        private void Transition()
        {
            _transitioned = true;
            gameObject.SetActive(false);

            if (homeScreenView != null)
                homeScreenView.Show();
        }
    }
}

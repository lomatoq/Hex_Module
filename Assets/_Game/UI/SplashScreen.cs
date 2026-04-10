#define DOTWEEN

using HexWords.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if DOTWEEN
using DG.Tweening;
#endif

namespace HexWords.UI
{
    public class SplashScreen : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Slider   loadingBar;
        [SerializeField] private TMP_Text loadingText;

        [Header("Transition")]
        [SerializeField] private float minDisplaySeconds = 1.5f;

        [Header("Bar Animation")]
        [SerializeField] private float          barEaseDuration = 0.3f;
        [SerializeField] private AnimationCurve barCurve        = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("References")]
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

            if (loadingText != null)
                loadingText.text = "LOADING...";

            RemoteConfigService.Instance.FetchAsync(OnConfigReady);

#if DOTWEEN
            if (loadingBar != null)
                DOTween.To(() => loadingBar.value,
                           v  => loadingBar.value = v,
                           0.9f,
                           minDisplaySeconds * 0.9f)
                       .SetEase(barCurve)
                       .SetId(loadingBar);
#endif
        }

        private void Update()
        {
            if (_transitioned) return;
            _elapsed += Time.deltaTime;

#if !DOTWEEN
            if (!_configReady && loadingBar != null)
                loadingBar.value = Mathf.Min(0.9f, _elapsed / (minDisplaySeconds * 0.9f));
#endif

            if (_configReady && _elapsed >= minDisplaySeconds)
                Transition();
        }

        private void OnConfigReady()
        {
            _configReady = true;

#if DOTWEEN
            if (loadingBar != null)
            {
                DOTween.Kill(loadingBar);
                DOTween.To(() => loadingBar.value,
                           v  => loadingBar.value = v,
                           1f,
                           barEaseDuration)
                       .SetEase(barCurve)
                       .SetId(loadingBar);
            }
#else
            if (loadingBar != null) loadingBar.value = 1f;
#endif

            if (_elapsed >= minDisplaySeconds && !_transitioned)
                Transition();
        }

        private void Transition()
        {
            _transitioned = true;
#if DOTWEEN
            DOTween.Kill(loadingBar);
#endif
            gameObject.SetActive(false);
            if (homeScreenView != null)
                homeScreenView.Show();
        }
    }
}

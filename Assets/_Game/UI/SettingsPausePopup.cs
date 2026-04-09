using System;
using HexWords.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HexWords.UI
{
    /// <summary>
    /// Settings / Pause popup. Opens from both the Home Screen and the in-game header.
    /// Controls sound, music, and vibration toggles.
    /// When opened in-game, shows an additional "Main Menu" button.
    /// </summary>
    public class SettingsPausePopup : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("Toggles")]
        [SerializeField] private Toggle sfxToggle;
        [SerializeField] private Toggle musicToggle;
        [SerializeField] private Toggle vibrationToggle;

        [Header("Close")]
        [SerializeField] private Button closeButton;

        [Header("Main Menu (in-game only)")]
        [SerializeField] private Button mainMenuButton;

        [Header("References")]
        [SerializeField] private SoundManager soundManager;

        public event Action OnMainMenuClicked;

        private void EnsureOverlayCanvas()
        {
            var c = GetComponent<Canvas>();
            if (c == null) c = gameObject.AddComponent<Canvas>();
            c.overrideSorting = true;
            c.sortingOrder    = 100;
            if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        private void Awake()
        {
            EnsureOverlayCanvas();
            // Toggles — suppress listener during init
            if (sfxToggle != null)
                sfxToggle.onValueChanged.AddListener(OnSfxToggled);

            if (musicToggle != null)
                musicToggle.onValueChanged.AddListener(OnMusicToggled);

            if (vibrationToggle != null)
                vibrationToggle.onValueChanged.AddListener(OnVibrationToggled);

            SetRootVisible(false);
        }

        private void OnEnable()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
                closeButton.onClick.AddListener(Hide);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(OnMainMenuButtonClicked);
                mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Hide);

            if (mainMenuButton != null)
                mainMenuButton.onClick.RemoveListener(OnMainMenuButtonClicked);
        }

        /// <param name="inGame">Pass true when opened during a level — shows the Main Menu button.</param>
        public void Show(bool inGame = false)
        {
            if (mainMenuButton != null)
                mainMenuButton.gameObject.SetActive(inGame);

            // Sync toggles with current state
            if (sfxToggle != null)
            {
                sfxToggle.onValueChanged.RemoveListener(OnSfxToggled);
                sfxToggle.isOn = soundManager != null ? soundManager.SfxEnabled : true;
                sfxToggle.onValueChanged.AddListener(OnSfxToggled);
            }

            if (musicToggle != null)
            {
                musicToggle.onValueChanged.RemoveListener(OnMusicToggled);
                musicToggle.isOn = soundManager != null ? soundManager.MusicEnabled : true;
                musicToggle.onValueChanged.AddListener(OnMusicToggled);
            }

            if (vibrationToggle != null)
            {
                vibrationToggle.onValueChanged.RemoveListener(OnVibrationToggled);
                vibrationToggle.isOn = HapticManager.IsEnabled;
                vibrationToggle.onValueChanged.AddListener(OnVibrationToggled);
            }

            transform.SetAsLastSibling();
            SetRootVisible(true);
        }

        public void Hide()
        {
            SetRootVisible(false);
        }

        private void OnMainMenuButtonClicked()
        {
            Hide();
            OnMainMenuClicked?.Invoke();
        }

        private void OnSfxToggled(bool value)
        {
            soundManager?.SetSfxEnabled(value);
        }

        private void OnMusicToggled(bool value)
        {
            soundManager?.SetMusicEnabled(value);
        }

        private void OnVibrationToggled(bool value)
        {
            HapticManager.SetEnabled(value);
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

using HexWords.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HexWords.UI
{
    /// <summary>
    /// Settings / Pause popup. Opens from both the Home Screen and the in-game header.
    /// Controls sound, music, and vibration toggles.
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

        [Header("References")]
        [SerializeField] private SoundManager soundManager;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            // Toggles — suppress listener during init
            if (sfxToggle != null)
                sfxToggle.onValueChanged.AddListener(OnSfxToggled);

            if (musicToggle != null)
                musicToggle.onValueChanged.AddListener(OnMusicToggled);

            if (vibrationToggle != null)
                vibrationToggle.onValueChanged.AddListener(OnVibrationToggled);

            SetRootVisible(false);
        }

        public void Show()
        {
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

            SetRootVisible(true);
        }

        public void Hide()
        {
            SetRootVisible(false);
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

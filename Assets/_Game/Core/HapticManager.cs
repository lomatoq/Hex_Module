using UnityEngine;

namespace HexWords.Core
{
    public enum HapticPattern
    {
        Light,   // tile tap
        Medium,  // word accepted
        Heavy,   // level complete
    }

    /// <summary>
    /// Cross-platform haptic feedback wrapper.
    /// iOS uses Device.RequestUserInteraction; Android uses Handheld.Vibrate.
    /// </summary>
    public static class HapticManager
    {
        private const string PrefKey = "HexWords.HapticsEnabled";

        private static bool? _enabled;

        public static bool IsEnabled
        {
            get
            {
                _enabled ??= PlayerPrefs.GetInt(PrefKey, 1) == 1;
                return _enabled.Value;
            }
        }

        public static void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            PlayerPrefs.SetInt(PrefKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void Vibrate(HapticPattern pattern)
        {
            if (!IsEnabled) return;

#if UNITY_IOS && !UNITY_EDITOR
            switch (pattern)
            {
                case HapticPattern.Light:
                    UnityEngine.iOS.Device.RequestUserInteraction(
                        UnityEngine.iOS.UserInteraction.TouchUp);
                    break;
                case HapticPattern.Medium:
                    UnityEngine.iOS.Device.RequestUserInteraction(
                        UnityEngine.iOS.UserInteraction.ImpactOccurredMedium);
                    break;
                case HapticPattern.Heavy:
                    UnityEngine.iOS.Device.RequestUserInteraction(
                        UnityEngine.iOS.UserInteraction.ImpactOccurredHeavy);
                    break;
            }
#elif UNITY_ANDROID && !UNITY_EDITOR
            switch (pattern)
            {
                case HapticPattern.Light:
                    Handheld.Vibrate();
                    break;
                case HapticPattern.Medium:
                    AndroidVibrate(50);
                    break;
                case HapticPattern.Heavy:
                    AndroidVibrate(120);
                    break;
            }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void AndroidVibrate(long milliseconds)
        {
            try
            {
                using var vibrator = new AndroidJavaObject("android.os.Vibrator");
                // On API 26+ use VibrationEffect; fallback to simple vibrate
                vibrator.Call("vibrate", milliseconds);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[HapticManager] Android vibrate failed: {e.Message}");
            }
        }
#endif
    }
}

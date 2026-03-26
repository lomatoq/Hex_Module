using UnityEngine;

namespace HexWords.Gameplay
{
    /// <summary>
    /// Analytics stub — wraps Firebase Analytics and AppMetrica.
    /// All event calls are safe to use now; they log to console in the editor.
    /// When SDK is integrated, add Firebase and AppMetrica calls inside LogEvent().
    /// </summary>
    public static class AnalyticsManager
    {
        /// <summary>
        /// Logs an analytics event with optional key-value parameters.
        /// Usage: AnalyticsManager.LogEvent("level_started", ("level_index", 0), ("level_id", "lvl_001"));
        /// </summary>
        public static void LogEvent(string eventName, params (string key, object value)[] parameters)
        {
#if UNITY_EDITOR
            var sb = new System.Text.StringBuilder();
            sb.Append($"[Analytics] {eventName}");
            foreach (var (key, value) in parameters)
                sb.Append($" | {key}={value}");
            Debug.Log(sb.ToString());
#endif

            // ── Firebase Analytics ──────────────────────────────────────────
            // TODO: Uncomment when Firebase SDK is integrated:
            //
            // var firebaseParams = new List<Firebase.Analytics.Parameter>();
            // foreach (var (key, value) in parameters)
            // {
            //     if (value is int i)    firebaseParams.Add(new Parameter(key, i));
            //     else if (value is long l) firebaseParams.Add(new Parameter(key, l));
            //     else if (value is double d) firebaseParams.Add(new Parameter(key, d));
            //     else firebaseParams.Add(new Parameter(key, value?.ToString() ?? ""));
            // }
            // Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName, firebaseParams.ToArray());

            // ── AppMetrica ──────────────────────────────────────────────────
            // TODO: Uncomment when AppMetrica SDK is integrated:
            //
            // var dict = new System.Collections.Generic.Dictionary<string, object>();
            // foreach (var (key, value) in parameters) dict[key] = value;
            // AppMetrica.Instance.ReportEvent(eventName, dict, null);
        }
    }
}

using HexWords.UI.Transitions;
using UnityEditor;
using UnityEngine;

namespace HexWords.Editor.Transitions
{
    /// <summary>
    /// Custom inspector drawer for <see cref="TransitionElementConfig"/>.
    ///
    /// Layout:
    ///   [✓] Use Global Timing
    ///       Duration ____   Extra Delay ____      ← only when global timing ON
    ///
    ///   [✓] Alpha         From [0]   To [1]
    ///       Ease  [OutCubic]
    ///       Dur   ____     Delay ____             ← only when global timing OFF
    ///
    ///   (Scale / Position / Rotation follow the same pattern)
    /// </summary>
    [CustomPropertyDrawer(typeof(TransitionElementConfig))]
    public class TransitionElementConfigDrawer : PropertyDrawer
    {
        // ── Sizing ─────────────────────────────────────────────────────────

        private static float LH    => EditorGUIUtility.singleLineHeight;
        private static float SP    => EditorGUIUtility.standardVerticalSpacing;
        private static float LHS   => LH + SP;
        // EaseSettingDrawer is always 2 rows
        private static float EaseH => 2f * LHS;

        // ── Height ─────────────────────────────────────────────────────────

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
        {
            bool useGlobal = prop.FindPropertyRelative("useGlobalTiming").boolValue;
            float h = LHS; // useGlobalTiming toggle

            if (useGlobal)
                h += LHS + LHS; // duration + extraDelay

            h += SectionHeight(prop, "alphaEnabled",    useGlobal);
            h += SectionHeight(prop, "scaleEnabled",    useGlobal);
            h += SectionHeight(prop, "positionEnabled", useGlobal);
            h += SectionHeight(prop, "rotationEnabled", useGlobal);

            return h + SP; // bottom padding
        }

        /// <summary>Height of one property section (enabled toggle + optional fields).</summary>
        private static float SectionHeight(SerializedProperty prop, string enabledKey, bool useGlobal)
        {
            float h = LHS; // enabled toggle row
            if (!prop.FindPropertyRelative(enabledKey).boolValue) return h;

            h += LHS;   // from
            h += LHS;   // to
            h += EaseH; // EaseSetting (2 rows via EaseSettingDrawer)

            if (!useGlobal)
                h += LHS + LHS; // per-property duration + delay

            return h + SP; // small section gap
        }

        // ── Drawing ────────────────────────────────────────────────────────

        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            EditorGUI.BeginProperty(pos, label, prop);

            float y = pos.y;
            float x = pos.x;
            float w = pos.width;

            var useGlobal = prop.FindPropertyRelative("useGlobalTiming");

            // ── useGlobalTiming ────────────────────────────────────────────
            EditorGUI.PropertyField(Row(ref y, x, w), useGlobal,
                new GUIContent("Use Global Timing",
                    "ON  — all properties share Duration and Extra Delay.\n" +
                    "OFF — each property has its own Duration and Delay."));

            bool global = useGlobal.boolValue;

            if (global)
            {
                int prevIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel++;
                EditorGUI.PropertyField(Row(ref y, x, w), prop.FindPropertyRelative("duration"),
                    new GUIContent("Duration", "Animation duration for every property (seconds)."));
                EditorGUI.PropertyField(Row(ref y, x, w), prop.FindPropertyRelative("extraDelay"),
                    new GUIContent("Extra Delay",
                        "Additional delay on top of the block stagger (seconds)."));
                EditorGUI.indentLevel = prevIndent;
            }

            // small gap before property sections
            y += SP * 2f;

            // ── Alpha ──────────────────────────────────────────────────────
            DrawSection(prop, ref y, x, w, global,
                enabledKey:   "alphaEnabled",
                fromKey:      "alphaFrom",       fromLabel: "From",
                toKey:        "alphaTo",         toLabel:   "To",
                fromTooltip:  "0 = fully transparent, 1 = fully opaque",
                toTooltip:    "0 = fully transparent, 1 = fully opaque",
                easeKey:      "alphaEase",
                durKey:       "alphaDuration",
                delayKey:     "alphaDelay",
                sectionLabel: "Alpha");

            // ── Scale ──────────────────────────────────────────────────────
            DrawSection(prop, ref y, x, w, global,
                enabledKey:   "scaleEnabled",
                fromKey:      "scaleFrom",       fromLabel: "From",
                toKey:        "scaleTo",         toLabel:   "To",
                fromTooltip:  "Scale at animation start.",
                toTooltip:    "Scale at animation end (usually Vector3.one).",
                easeKey:      "scaleEase",
                durKey:       "scaleDuration",
                delayKey:     "scaleDelay",
                sectionLabel: "Scale");

            // ── Position ───────────────────────────────────────────────────
            DrawSection(prop, ref y, x, w, global,
                enabledKey:   "positionEnabled",
                fromKey:      "positionFrom",    fromLabel: "From (offset)",
                toKey:        "positionTo",      toLabel:   "To (offset)",
                fromTooltip:  "Anchored-position offset from rest at animation START.",
                toTooltip:    "Anchored-position offset from rest at animation END (0,0 = rest).",
                easeKey:      "positionEase",
                durKey:       "positionDuration",
                delayKey:     "positionDelay",
                sectionLabel: "Position");

            // ── Rotation ───────────────────────────────────────────────────
            DrawSection(prop, ref y, x, w, global,
                enabledKey:   "rotationEnabled",
                fromKey:      "rotationFrom",    fromLabel: "From (Z°)",
                toKey:        "rotationTo",      toLabel:   "To (Z°)",
                fromTooltip:  "Euler-Z rotation at animation start (degrees).",
                toTooltip:    "Euler-Z rotation at animation end (degrees).",
                easeKey:      "rotationEase",
                durKey:       "rotationDuration",
                delayKey:     "rotationDelay",
                sectionLabel: "Rotation");

            EditorGUI.EndProperty();
        }

        // ── Section helper ─────────────────────────────────────────────────

        private static void DrawSection(
            SerializedProperty prop,
            ref float y, float x, float w,
            bool global,
            string enabledKey,
            string fromKey, string fromLabel, string fromTooltip,
            string toKey,   string toLabel,   string toTooltip,
            string easeKey,
            string durKey,  string delayKey,
            string sectionLabel)
        {
            var enabledProp = prop.FindPropertyRelative(enabledKey);

            // ── Enabled toggle + bold section label on same row ────────────
            Rect toggleRow = Row(ref y, x, w);

            // Manually position toggle + label (not using PropertyField for the toggle
            // so we can draw a bold label next to it)
            float indentPx  = EditorGUI.indentLevel * 15f;
            float toggleW   = EditorGUIUtility.singleLineHeight;
            Rect  toggleR   = new Rect(toggleRow.x + indentPx, toggleRow.y, toggleW, toggleRow.height);
            Rect  labelR    = new Rect(toggleR.xMax + 2f, toggleRow.y,
                                       toggleRow.width - indentPx - toggleW - 2f, toggleRow.height);

            EditorGUI.BeginChangeCheck();
            bool enabled = EditorGUI.Toggle(toggleR, enabledProp.boolValue);
            if (EditorGUI.EndChangeCheck()) enabledProp.boolValue = enabled;

            EditorGUI.LabelField(labelR, sectionLabel,
                enabled ? EditorStyles.boldLabel : EditorStyles.label);

            if (!enabled) return;

            // ── Sub-fields ─────────────────────────────────────────────────
            int prevIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;

            EditorGUI.PropertyField(Row(ref y, x, w),
                prop.FindPropertyRelative(fromKey), new GUIContent(fromLabel, fromTooltip));
            EditorGUI.PropertyField(Row(ref y, x, w),
                prop.FindPropertyRelative(toKey),   new GUIContent(toLabel, toTooltip));

            // EaseSetting — uses EaseSettingDrawer (2 rows); pass full x/w, let drawer handle indent
            var  easeProp = prop.FindPropertyRelative(easeKey);
            Rect easeRect = new Rect(x, y, w, EaseH);
            y += EaseH;
            EditorGUI.PropertyField(easeRect, easeProp, new GUIContent("Ease"), true);

            // Per-property timing (only when !global)
            if (!global)
            {
                EditorGUI.PropertyField(Row(ref y, x, w),
                    prop.FindPropertyRelative(durKey),
                    new GUIContent("Duration", "Duration for this property only (seconds)."));
                EditorGUI.PropertyField(Row(ref y, x, w),
                    prop.FindPropertyRelative(delayKey),
                    new GUIContent("Delay",
                        "Extra delay for this property on top of the block stagger (seconds)."));
            }

            EditorGUI.indentLevel = prevIndent;
            y += SP * 2f; // small gap after each section
        }

        // ── Row helper ─────────────────────────────────────────────────────

        private static Rect Row(ref float y, float x, float w)
        {
            var r = new Rect(x, y, w, LH);
            y += LHS;
            return r;
        }
    }
}

// Remove this comment after installing DOTween via Asset Store
#define DOTWEEN

using HexWords.UI.Transitions;
using UnityEditor;
using UnityEngine;

namespace HexWords.Editor.Transitions
{
    /// <summary>
    /// Two-line PropertyDrawer for <see cref="EaseSetting"/>.
    ///   Line 1 — "Use Custom Curve" toggle.
    ///   Line 2 — either the DOTween Ease enum (when toggle is OFF)
    ///            or an AnimationCurve field (when toggle is ON).
    /// </summary>
    [CustomPropertyDrawer(typeof(EaseSetting))]
    public class EaseSettingDrawer : PropertyDrawer
    {
        private static float LH  => EditorGUIUtility.singleLineHeight;
        private static float SP  => EditorGUIUtility.standardVerticalSpacing;
        private static float LHS => LH + SP;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => 2f * LHS;

        public override void OnGUI(Rect pos, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(pos, label, property);

            var useCustom = property.FindPropertyRelative("useCustomCurve");

            Rect r0 = new Rect(pos.x, pos.y,        pos.width, LH);
            Rect r1 = new Rect(pos.x, pos.y + LHS,  pos.width, LH);

            // Line 1 — toggle
            EditorGUI.PropertyField(r0, useCustom, new GUIContent("Use Custom Curve",
                "When ON: uses an AnimationCurve instead of a DOTween preset."));

            // Line 2 — ease or curve
            if (useCustom.boolValue)
            {
                var curveProp = property.FindPropertyRelative("curve");
                EditorGUI.PropertyField(r1, curveProp,
                    new GUIContent("Curve", "X = normalised time 0→1 | Y = normalised value 0→1"));
            }
            else
            {
#if DOTWEEN
                var easeProp = property.FindPropertyRelative("ease");
                EditorGUI.PropertyField(r1, easeProp, new GUIContent("Ease"));
#else
                EditorGUI.LabelField(r1, "Ease", "(DOTween not installed)");
#endif
            }

            EditorGUI.EndProperty();
        }
    }
}

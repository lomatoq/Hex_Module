using UnityEditor;
using UnityEngine;
using HexWords.Theming;

namespace HexWords.Editor.Theming
{
    /// <summary>
    /// Shared inspector for ThemedImage and ThemedGraphicColor — shows a
    /// dropdown of every slot id the collector has seen, so you can re-assign
    /// a slot without typing.
    /// </summary>
    [CustomEditor(typeof(ThemedImage))]
    [CanEditMultipleObjects]
    public class ThemedImageInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawSlotDropdown(serializedObject);
            DrawPropertiesExcluding(serializedObject, "m_Script", "slotId");
            serializedObject.ApplyModifiedProperties();
        }

        internal static void DrawSlotDropdown(SerializedObject so)
        {
            so.Update();
            var prop = so.FindProperty("slotId");
            if (prop == null) return;

            var known = ThemeSlotRegistry.All;
            var current = prop.stringValue ?? string.Empty;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(prop, new GUIContent("Slot Id"));
            if (GUILayout.Button("▾", GUILayout.Width(22)))
            {
                var menu = new GenericMenu();
                if (known.Count == 0)
                {
                    menu.AddDisabledItem(new GUIContent("No slots registered yet — run the collector."));
                }
                else
                {
                    foreach (var id in known)
                    {
                        var captured = id;
                        menu.AddItem(new GUIContent(id.Replace('/', '/')), id == current,
                            () =>
                            {
                                prop.stringValue = captured;
                                so.ApplyModifiedProperties();
                            });
                    }
                }
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    [CustomEditor(typeof(ThemedGraphicColor))]
    [CanEditMultipleObjects]
    public class ThemedGraphicColorInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            ThemedImageInspector.DrawSlotDropdown(serializedObject);
            DrawPropertiesExcluding(serializedObject, "m_Script", "slotId");
            serializedObject.ApplyModifiedProperties();
        }
    }
}

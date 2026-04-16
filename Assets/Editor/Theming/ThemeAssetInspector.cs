using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using HexWords.Theming;

namespace HexWords.Editor.Theming
{
    /// <summary>
    /// Groups theme entries by the first segment of their slot id
    /// (e.g. "HUD/*", "HexCell/*"), so a theme with dozens of entries
    /// stays navigable. Each group can be folded / unfolded; each entry
    /// renders inline with its sprite / color / visibility overrides.
    /// </summary>
    [CustomEditor(typeof(ThemeAsset))]
    public class ThemeAssetInspector : UnityEditor.Editor
    {
        private readonly Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();
        private string _search = string.Empty;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var displayName = serializedObject.FindProperty("displayName");
            EditorGUILayout.PropertyField(displayName);

            var entries = serializedObject.FindProperty("entries");
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"Entries: {entries.arraySize}", EditorStyles.miniBoldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                _search = EditorGUILayout.TextField("Filter", _search);
                if (GUILayout.Button("Clear", GUILayout.Width(60))) _search = string.Empty;
            }

            // Group by first segment of slotId ("HUD/Foo" → "HUD"; "Something" → "(root)")
            var groups = new Dictionary<string, List<int>>();
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                var slotId = entry.FindPropertyRelative("slotId").stringValue ?? string.Empty;
                if (!string.IsNullOrEmpty(_search) &&
                    slotId.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) < 0) continue;

                var slash = slotId.IndexOf('/');
                var key   = slash > 0 ? slotId.Substring(0, slash) : "(root)";
                if (!groups.TryGetValue(key, out var list)) groups[key] = list = new List<int>();
                list.Add(i);
            }

            foreach (var g in groups.OrderBy(p => p.Key))
            {
                if (!_foldouts.TryGetValue(g.Key, out var open)) open = false;
                open = EditorGUILayout.Foldout(open, $"{g.Key}  ({g.Value.Count})", true, EditorStyles.foldoutHeader);
                _foldouts[g.Key] = open;
                if (!open) continue;

                EditorGUI.indentLevel++;
                foreach (var idx in g.Value) DrawEntry(entries.GetArrayElementAtIndex(idx));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Enable All Sprites"))  SetAllBool(entries, "useSprite", true);
                if (GUILayout.Button("Disable All Sprites")) SetAllBool(entries, "useSprite", false);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Enable All Colors"))   SetAllBool(entries, "useColor", true);
                if (GUILayout.Button("Disable All Colors"))  SetAllBool(entries, "useColor", false);
            }
            if (GUILayout.Button("Sort By Slot Id"))
            {
                SortBySlotId();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawEntry(SerializedProperty entry)
        {
            var slotId     = entry.FindPropertyRelative("slotId");
            var useSprite  = entry.FindPropertyRelative("useSprite");
            var sprite     = entry.FindPropertyRelative("sprite");
            var useColor   = entry.FindPropertyRelative("useColor");
            var color      = entry.FindPropertyRelative("color");
            var useVis     = entry.FindPropertyRelative("useVisibility");
            var visible    = entry.FindPropertyRelative("visible");

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(slotId.stringValue, EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    useSprite.boolValue = EditorGUILayout.ToggleLeft("Sprite", useSprite.boolValue, GUILayout.Width(70));
                    using (new EditorGUI.DisabledScope(!useSprite.boolValue))
                        EditorGUILayout.PropertyField(sprite, GUIContent.none);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    useColor.boolValue = EditorGUILayout.ToggleLeft("Color", useColor.boolValue, GUILayout.Width(70));
                    using (new EditorGUI.DisabledScope(!useColor.boolValue))
                        EditorGUILayout.PropertyField(color, GUIContent.none);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    useVis.boolValue = EditorGUILayout.ToggleLeft("Visible", useVis.boolValue, GUILayout.Width(70));
                    using (new EditorGUI.DisabledScope(!useVis.boolValue))
                        visible.boolValue = EditorGUILayout.Toggle(visible.boolValue);
                }
            }
        }

        private static void SetAllBool(SerializedProperty entries, string field, bool v)
        {
            for (int i = 0; i < entries.arraySize; i++)
                entries.GetArrayElementAtIndex(i).FindPropertyRelative(field).boolValue = v;
        }

        private void SortBySlotId()
        {
            var asset = (ThemeAsset)target;
            Undo.RecordObject(asset, "Sort Theme Entries");
            asset.entries = asset.entries.OrderBy(e => e.slotId).ToList();
            EditorUtility.SetDirty(asset);
            serializedObject.Update();
        }
    }
}

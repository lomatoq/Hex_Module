using System.Collections.Generic;
using System.IO;
using System.Linq;
using HexWords.Theming;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HexWords.Editor.Theming
{
    /// <summary>
    /// One-click collector: walks the open scene and/or a prefab folder,
    /// attaches <see cref="ThemedImage"/> to every Image that lacks one,
    /// assigns a hierarchy-derived slot id, then optionally rebuilds a
    /// "DefaultTheme" asset that snapshots every slot's current sprite /
    /// color so other themes can override from a known baseline.
    /// </summary>
    public class ThemeCollectorWindow : EditorWindow
    {
        // ── Settings ──────────────────────────────────────────────────────────

        private bool    scanOpenScene        = true;
        private bool    scanPrefabFolder     = true;
        private string  prefabFolder         = "Assets/_Game/Prefabs";
        private bool    includeInactive      = true;
        private bool    dryRun               = false;
        private bool    rebuildDefaultTheme  = true;
        private string  defaultThemePath     = "Assets/_Game/Data/Themes/DefaultTheme.asset";
        private int     spriteGroupMinUses   = 2;

        // ── Runtime state ─────────────────────────────────────────────────────

        private int    _addedBindings;
        private int    _renamedSlots;
        private int    _slotIdsSeen;
        private string _lastLog = string.Empty;
        private Vector2 _scroll;

        [MenuItem("HexWords/Theme/Collect Themeable Elements…")]
        public static void Open()
        {
            var w = GetWindow<ThemeCollectorWindow>("Theme Collector");
            w.minSize = new Vector2(440, 360);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Theme Collector", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Scans the open scene and/or a prefab folder for UI Image components, " +
                "attaches a ThemedImage to each, and assigns a hierarchy-derived slot id. " +
                "Optionally rebuilds a DefaultTheme snapshot with every slot.",
                MessageType.Info);
            EditorGUILayout.Space(4);

            scanOpenScene    = EditorGUILayout.ToggleLeft("Scan open scene",        scanOpenScene);
            scanPrefabFolder = EditorGUILayout.ToggleLeft("Scan prefab folder",     scanPrefabFolder);
            using (new EditorGUI.DisabledScope(!scanPrefabFolder))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    prefabFolder = EditorGUILayout.TextField("Folder", prefabFolder);
                    if (GUILayout.Button("…", GUILayout.Width(28)))
                    {
                        var picked = EditorUtility.OpenFolderPanel("Prefab folder", prefabFolder, string.Empty);
                        if (!string.IsNullOrEmpty(picked) && picked.StartsWith(Application.dataPath))
                            prefabFolder = "Assets" + picked.Substring(Application.dataPath.Length);
                    }
                }
            }

            includeInactive = EditorGUILayout.ToggleLeft("Include inactive GameObjects", includeInactive);
            dryRun          = EditorGUILayout.ToggleLeft("Dry run (don't modify assets)", dryRun);

            EditorGUILayout.Space(6);
            rebuildDefaultTheme = EditorGUILayout.ToggleLeft("Rebuild / update Default Theme", rebuildDefaultTheme);
            using (new EditorGUI.DisabledScope(!rebuildDefaultTheme))
            {
                defaultThemePath = EditorGUILayout.TextField("Default theme path", defaultThemePath);
                spriteGroupMinUses = EditorGUILayout.IntSlider(
                    new GUIContent("Sprite-group threshold",
                        "Bundle every slot that shares a sprite into one SpriteGroup once the sprite is used in N+ places. Editing a group reskins them all."),
                    spriteGroupMinUses, 2, 20);
            }

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
                if (GUILayout.Button("▶  Run Collector", GUILayout.Height(30))) Run();
                GUI.backgroundColor = Color.white;
                if (GUILayout.Button("Clear Slot Registry", GUILayout.Height(30), GUILayout.Width(160)))
                {
                    ThemeSlotRegistry.Replace(System.Array.Empty<string>());
                    _lastLog = "Slot registry cleared.";
                }
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Last run", EditorStyles.boldLabel);
            using (var sv = new EditorGUILayout.ScrollViewScope(_scroll, GUILayout.MinHeight(120)))
            {
                _scroll = sv.scrollPosition;
                EditorGUILayout.TextArea(_lastLog, GUILayout.ExpandHeight(true));
            }
        }

        // ── Run ───────────────────────────────────────────────────────────────

        private void Run()
        {
            _addedBindings = 0;
            _renamedSlots  = 0;
            _slotIdsSeen   = 0;
            var allSlots = new HashSet<string>();
            var log = new System.Text.StringBuilder();
            log.AppendLine(dryRun ? "-- DRY RUN --" : "-- Collect run --");

            try
            {
                AssetDatabase.StartAssetEditing();

                if (scanOpenScene)
                    ProcessOpenScene(allSlots, log);

                if (scanPrefabFolder)
                    ProcessPrefabFolder(prefabFolder, allSlots, log);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                if (!dryRun)
                {
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }

            _slotIdsSeen = allSlots.Count;
            if (!dryRun) ThemeSlotRegistry.Replace(allSlots);

            if (rebuildDefaultTheme && !dryRun)
                UpdateDefaultTheme(allSlots, log);

            log.AppendLine($"\nBindings added: {_addedBindings}");
            log.AppendLine($"Slot ids renamed: {_renamedSlots}");
            log.AppendLine($"Unique slots seen: {_slotIdsSeen}");
            _lastLog = log.ToString();
            Debug.Log(_lastLog);
        }

        // ── Scene scan ────────────────────────────────────────────────────────

        private void ProcessOpenScene(HashSet<string> allSlots, System.Text.StringBuilder log)
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid()) { log.AppendLine("No active scene."); return; }

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                var images = root.GetComponentsInChildren<Image>(includeInactive);
                foreach (var img in images)
                {
                    var slot = MakeSceneSlotId(img.transform, root.transform);
                    ApplySlotToImage(img, slot, allSlots, log);
                }
            }

            if (!dryRun) EditorSceneManager.MarkSceneDirty(scene);
        }

        // ── Prefab scan ───────────────────────────────────────────────────────

        private void ProcessPrefabFolder(string folder, HashSet<string> allSlots, System.Text.StringBuilder log)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                log.AppendLine($"Prefab folder not found: {folder}");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                if (root == null) continue;

                bool anyChange = false;
                var prefabName = Path.GetFileNameWithoutExtension(path);
                var images = root.GetComponentsInChildren<Image>(includeInactive);
                foreach (var img in images)
                {
                    var slot = MakePrefabSlotId(img.transform, root.transform, prefabName);
                    if (ApplySlotToImage(img, slot, allSlots, log)) anyChange = true;
                }

                if (anyChange && !dryRun) PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ── Binding helpers ──────────────────────────────────────────────────

        private bool ApplySlotToImage(Image img, string slot, HashSet<string> allSlots, System.Text.StringBuilder log)
        {
            allSlots.Add(slot);

            var themed = img.GetComponent<ThemedImage>();
            bool changed = false;

            if (themed == null)
            {
                if (!dryRun) themed = Undo.AddComponent<ThemedImage>(img.gameObject);
                _addedBindings++;
                log.AppendLine($"+ {slot}  (added ThemedImage)");
                changed = true;
            }
            else if (themed.SlotId != slot)
            {
                log.AppendLine($"~ {themed.SlotId} → {slot}  (renamed)");
                _renamedSlots++;
                changed = true;
            }

            if (themed != null && !dryRun && themed.SlotId != slot)
            {
                themed.EditorSetSlotId(slot);
                EditorUtility.SetDirty(themed);
            }

            return changed;
        }

        // ── Slot id generation ────────────────────────────────────────────────

        private static string MakeSceneSlotId(Transform t, Transform sceneRoot)
        {
            var path = GetRelativePath(t, sceneRoot, includeRoot: true);
            return path;
        }

        private static string MakePrefabSlotId(Transform t, Transform prefabRoot, string prefabName)
        {
            var rel = GetRelativePath(t, prefabRoot, includeRoot: false);
            return string.IsNullOrEmpty(rel) ? prefabName : $"{prefabName}/{rel}";
        }

        private static string GetRelativePath(Transform t, Transform root, bool includeRoot)
        {
            var parts = new List<string>();
            var cur = t;
            while (cur != null && cur != root)
            {
                parts.Add(cur.name);
                cur = cur.parent;
            }
            parts.Reverse();
            if (includeRoot && root != null) parts.Insert(0, root.name);
            return string.Join("/", parts);
        }

        // ── Default theme rebuild ─────────────────────────────────────────────

        private void UpdateDefaultTheme(HashSet<string> slotIds, System.Text.StringBuilder log)
        {
            var dir = Path.GetDirectoryName(defaultThemePath);
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            var theme = AssetDatabase.LoadAssetAtPath<ThemeAsset>(defaultThemePath);
            bool created = false;
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<ThemeAsset>();
                theme.displayName = "Default";
                AssetDatabase.CreateAsset(theme, defaultThemePath);
                created = true;
            }

            // Snapshot current sprites/colors from a representative binding per slot.
            // Prefer scene occurrences (active, disambiguated); fall back to prefab lookup.
            var snapshots = BuildSnapshotMap();

            // Build sprite usage histogram — sprites seen on ≥ spriteGroupMinUses
            // distinct slots are collapsed into SpriteGroups. Per-slot entries
            // for those slots skip the sprite snapshot (the group owns it).
            var spriteUsage = new Dictionary<Sprite, int>();
            foreach (var snap in snapshots.Values)
            {
                if (snap.sprite == null) continue;
                spriteUsage.TryGetValue(snap.sprite, out var n);
                spriteUsage[snap.sprite] = n + 1;
            }
            var groupedSprites = new HashSet<Sprite>();
            foreach (var kv in spriteUsage)
                if (kv.Value >= Mathf.Max(2, spriteGroupMinUses)) groupedSprites.Add(kv.Key);

            int added = 0;
            foreach (var id in slotIds)
            {
                var entry = theme.entries.FirstOrDefault(e => e.slotId == id);
                if (entry == null)
                {
                    entry = new ThemeAsset.Entry { slotId = id };
                    theme.entries.Add(entry);
                    added++;
                }

                if (snapshots.TryGetValue(id, out var snap))
                {
                    // Per-slot sprite snapshot ONLY when the sprite isn't shared.
                    // Shared sprites live on a SpriteGroup; per-slot entries exist
                    // for custom overrides that escape the group default.
                    if (!groupedSprites.Contains(snap.sprite))
                    {
                        if (entry.sprite == null) entry.sprite = snap.sprite;
                    }
                    if (entry.color  == default || entry.color == Color.white) entry.color = snap.color;
                }
            }

            // Remove entries whose slotId no longer exists in the project.
            int removed = theme.entries.RemoveAll(e => !slotIds.Contains(e.slotId));

            // Build / refresh SpriteGroups. Preserve existing group overrides
            // (useSprite/sprite/useColor/color) when the group already exists.
            int groupsAdded = 0;
            var liveGroupSprites = new HashSet<Sprite>();
            foreach (var src in groupedSprites)
            {
                liveGroupSprites.Add(src);
                var g = theme.spriteGroups.FirstOrDefault(x => x != null && x.sourceSprite == src);
                if (g == null)
                {
                    g = new ThemeAsset.SpriteGroup { sourceSprite = src, label = src.name };
                    theme.spriteGroups.Add(g);
                    groupsAdded++;
                }
                else if (string.IsNullOrEmpty(g.label))
                {
                    g.label = src.name;
                }
            }
            int groupsRemoved = theme.spriteGroups.RemoveAll(g => g == null || g.sourceSprite == null || !liveGroupSprites.Contains(g.sourceSprite));

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            log.AppendLine($"\nDefaultTheme: {(created ? "created" : "updated")} at {defaultThemePath}");
            log.AppendLine($"  entries added: {added}");
            log.AppendLine($"  entries removed (stale): {removed}");
            log.AppendLine($"  total entries: {theme.entries.Count}");
            log.AppendLine($"  sprite groups added: {groupsAdded}");
            log.AppendLine($"  sprite groups removed: {groupsRemoved}");
            log.AppendLine($"  total sprite groups: {theme.spriteGroups.Count}  (threshold ≥ {Mathf.Max(2, spriteGroupMinUses)} uses)");
        }

        private struct SlotSnapshot { public Sprite sprite; public Color color; }

        private Dictionary<string, SlotSnapshot> BuildSnapshotMap()
        {
            var map = new Dictionary<string, SlotSnapshot>();

            // Scene pass
            if (scanOpenScene)
            {
                var scene = EditorSceneManager.GetActiveScene();
                if (scene.IsValid())
                {
                    foreach (var root in scene.GetRootGameObjects())
                    foreach (var themed in root.GetComponentsInChildren<ThemedImage>(includeInactive))
                    {
                        var img = themed.GetComponent<Image>();
                        if (img == null || string.IsNullOrEmpty(themed.SlotId)) continue;
                        if (!map.ContainsKey(themed.SlotId))
                            map[themed.SlotId] = new SlotSnapshot { sprite = img.sprite, color = img.color };
                    }
                }
            }

            // Prefab pass (fills slots that only exist in prefabs)
            if (scanPrefabFolder && AssetDatabase.IsValidFolder(prefabFolder))
            {
                var guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolder });
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var go   = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go == null) continue;
                    foreach (var themed in go.GetComponentsInChildren<ThemedImage>(includeInactive))
                    {
                        var img = themed.GetComponent<Image>();
                        if (img == null || string.IsNullOrEmpty(themed.SlotId)) continue;
                        if (!map.ContainsKey(themed.SlotId))
                            map[themed.SlotId] = new SlotSnapshot { sprite = img.sprite, color = img.color };
                    }
                }
            }

            return map;
        }
    }
}

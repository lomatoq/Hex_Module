using HexWords.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HexWords.Editor
{
    /// <summary>
    /// Builds the DevPanel UI and wires all references automatically.
    /// Menu: Tools → HexWords → Build Dev Panel
    /// </summary>
    public static class DevLevelSelectorBuilder
    {
        [MenuItem("Tools/HexWords/Build Dev Panel")]
        public static void BuildDevPanel()
        {
            // Find DevLevelSelector in scene
            var selector = Object.FindFirstObjectByType<DevLevelSelector>();
            if (selector == null)
            {
                EditorUtility.DisplayDialog("Dev Panel Builder",
                    "No DevLevelSelector component found in scene.\nAdd it to a GameObject first.", "OK");
                return;
            }

            var selectorGO  = selector.gameObject;
            var selectorRect = selectorGO.GetComponent<RectTransform>();

            // ── 1. DevTrigger (invisible button in top-left) ───────────────
            var triggerGO = GetOrCreate("DevTrigger", selectorGO.transform);
            var triggerRT = EnsureRect(triggerGO);
            triggerRT.anchorMin = new Vector2(0, 1);
            triggerRT.anchorMax = new Vector2(0, 1);
            triggerRT.pivot     = new Vector2(0, 1);
            triggerRT.anchoredPosition = new Vector2(10, -10);
            triggerRT.sizeDelta = new Vector2(80, 80);
            var triggerImg = EnsureComponent<Image>(triggerGO);
            triggerImg.color = new Color(0, 0, 0, 0); // fully transparent
            var triggerBtn = EnsureComponent<Button>(triggerGO);

            // ── 2. DevPanel ────────────────────────────────────────────────
            var panelGO = GetOrCreate("DevPanel", selectorGO.transform);
            var panelRT = EnsureRect(panelGO);
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot     = new Vector2(0.5f, 0.5f);
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(300, 220);
            var panelImg = EnsureComponent<Image>(panelGO);
            panelImg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            panelGO.SetActive(false); // hidden by default

            // ── 3. Title label ─────────────────────────────────────────────
            var titleGO = GetOrCreate("TitleText", panelGO.transform);
            var titleRT = EnsureRect(titleGO);
            titleRT.anchorMin = new Vector2(0, 1);
            titleRT.anchorMax = new Vector2(1, 1);
            titleRT.pivot     = new Vector2(0.5f, 1);
            titleRT.anchoredPosition = new Vector2(0, -10);
            titleRT.sizeDelta = new Vector2(0, 30);
            var titleText = EnsureComponent<TextMeshProUGUI>(titleGO);
            titleText.text      = "DEV — Select Level";
            titleText.color     = Color.yellow;
            titleText.fontSize  = 16;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;

            // ── 4. Level text (center) ─────────────────────────────────────
            var levelGO = GetOrCreate("LevelText", panelGO.transform);
            var levelRT = EnsureRect(levelGO);
            levelRT.anchoredPosition = new Vector2(0, 20);
            levelRT.sizeDelta        = new Vector2(160, 40);
            var levelText = EnsureComponent<TextMeshProUGUI>(levelGO);
            levelText.text      = "Level 1";
            levelText.color     = Color.white;
            levelText.fontSize  = 24;
            levelText.fontStyle = FontStyles.Bold;
            levelText.alignment = TextAlignmentOptions.Center;

            // ── 5. Prev button (←) ─────────────────────────────────────────
            var prevGO  = CreateLabelledButton("PrevBtn", panelGO.transform, "←", new Vector2(-100, 20));
            var prevBtn = prevGO.GetComponent<Button>();

            // ── 6. Next button (→) ─────────────────────────────────────────
            var nextGO  = CreateLabelledButton("NextBtn", panelGO.transform, "→", new Vector2(100, 20));
            var nextBtn = nextGO.GetComponent<Button>();

            // ── 7. GO button ───────────────────────────────────────────────
            var goGO  = CreateLabelledButton("GoBtn", panelGO.transform, "▶  PLAY", new Vector2(0, -50), new Vector2(180, 50));
            StyleButton(goGO, new Color(0.1f, 0.7f, 0.1f));
            var goBtn = goGO.GetComponent<Button>();

            // ── 8. Restart button ──────────────────────────────────────────
            var restartGO  = CreateLabelledButton("RestartBtn", panelGO.transform, "↺  RESTART", new Vector2(0, -105), new Vector2(180, 40));
            StyleButton(restartGO, new Color(0.15f, 0.45f, 0.75f));
            var restartBtn = restartGO.GetComponent<Button>();

            // ── 9. Close button (✕) ────────────────────────────────────────
            var closeGO = CreateLabelledButton("CloseBtn", panelGO.transform, "✕", new Vector2(130, 90), new Vector2(40, 40));
            StyleButton(closeGO, new Color(0.7f, 0.1f, 0.1f));
            var closeBtn = closeGO.GetComponent<Button>();

            // ── 10. Wire references via SerializedObject ───────────────────
            var so = new SerializedObject(selector);

            so.FindProperty("triggerZone").objectReferenceValue      = triggerBtn;
            so.FindProperty("panelRoot").objectReferenceValue        = panelGO;
            so.FindProperty("closeButton").objectReferenceValue      = closeBtn;
            so.FindProperty("currentLevelText").objectReferenceValue = levelText;
            so.FindProperty("prevButton").objectReferenceValue       = prevBtn;
            so.FindProperty("nextButton").objectReferenceValue       = nextBtn;
            so.FindProperty("goButton").objectReferenceValue         = goBtn;
            so.FindProperty("restartButton").objectReferenceValue    = restartBtn;

            // Wire GameBootstrap if present
            var bootstrap = Object.FindFirstObjectByType<HexWords.Gameplay.GameBootstrap>();
            if (bootstrap != null)
                so.FindProperty("gameBootstrap").objectReferenceValue = bootstrap;

            so.ApplyModifiedProperties();

            // Mark scene dirty
            EditorUtility.SetDirty(selector);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[DevLevelSelectorBuilder] DevPanel built and wired successfully!");
            EditorUtility.DisplayDialog("Done!",
                "DevPanel created and all references wired.\n\nActivate: Press L in Play Mode, or tap top-left corner 5× on device.",
                "OK");
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static GameObject GetOrCreate(string name, Transform parent)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.gameObject;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static RectTransform EnsureRect(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            return rt;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        private static GameObject CreateLabelledButton(string name, Transform parent,
            string label, Vector2 pos, Vector2? size = null)
        {
            var go = GetOrCreate(name, parent);
            var rt = EnsureRect(go);
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size ?? new Vector2(70, 50);

            var img = EnsureComponent<Image>(go);
            img.color = new Color(0.25f, 0.25f, 0.25f);
            EnsureComponent<Button>(go);

            // Label child
            var labelGO = GetOrCreate("Label", go.transform);
            var labelRT = EnsureRect(labelGO);
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.sizeDelta = Vector2.zero;
            var txt = EnsureComponent<TextMeshProUGUI>(labelGO);
            txt.text      = label;
            txt.color     = Color.white;
            txt.fontSize  = 18;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;

            return go;
        }

        private static void StyleButton(GameObject go, Color color)
        {
            var img = go.GetComponent<Image>();
            if (img != null) img.color = color;
        }
    }
}

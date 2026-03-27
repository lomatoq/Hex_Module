using HexWords.Gameplay;
using HexWords.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HexWords.Editor
{
    /// <summary>
    /// Tools → HexWords → Build Missing UI
    /// Creates every missing GameObject in the scene hierarchy and
    /// wires all None-fields on LevelHudView, HomeScreenView,
    /// LevelCompleteView, SplashScreen, GameBootstrap.
    /// </summary>
    public static class SceneHierarchyBuilder
    {
        [MenuItem("Tools/HexWords/Build Missing UI")]
        public static void BuildAll()
        {
            BuildHud();
            BuildHomeScreen();
            BuildLevelComplete();
            BuildSplash();
            BuildGameBootstrap();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[SceneHierarchyBuilder] Done — all missing UI built and wired.");
            EditorUtility.DisplayDialog("Done!", "All missing UI elements created and wired.\nPosition them manually in the Scene.", "OK");
        }

        // ══════════════════════════════════════════════════════════════════
        // HUD
        // ══════════════════════════════════════════════════════════════════
        static void BuildHud()
        {
            var hud = FindComponent<LevelHudView>("HUD");
            if (hud == null) { Debug.LogWarning("[Builder] HUD/LevelHudView not found."); return; }

            var hudGO = hud.gameObject;
            var so = new SerializedObject(hud);

            // ── Coin Text ─────────────────────────────────────────────────
            Wire(so, "coinText", GetOrCreateText(hudGO, "CoinText", "250", 22, Color.white));

            // ── Settings Button ───────────────────────────────────────────
            Wire(so, "settingsButton", GetOrCreateButton(hudGO, "SettingsButton", "⚙", 18));

            // ── Found Words Button ────────────────────────────────────────
            Wire(so, "foundWordsButton", GetOrCreateButton(hudGO, "FoundWordsButton", "📖", 18));

            // ── Word Preview ──────────────────────────────────────────────
            var previewRoot = GetOrCreateGO("WordPreview", hudGO.transform);
            EnsureRect(previewRoot).sizeDelta = new Vector2(200, 50);
            EnsureImage(previewRoot, new Color(0, 0, 0, 0.5f));
            previewRoot.SetActive(false);

            var previewText = GetOrCreateText(previewRoot, "PreviewText", "WORD", 24, Color.white);
            var badgeRoot   = GetOrCreateGO("ScoreBadge", previewRoot.transform);
            EnsureRect(badgeRoot).sizeDelta = new Vector2(60, 30);
            EnsureImage(badgeRoot, new Color(0.2f, 0.7f, 0.2f));
            var badgeText = GetOrCreateText(badgeRoot, "BadgeText", "+7", 18, Color.white);

            Wire(so, "wordPreviewRoot", previewRoot);
            Wire(so, "wordPreviewText", previewText.GetComponent<Text>());
            Wire(so, "scoreBadgeRoot",  badgeRoot);
            Wire(so, "scoreBadgeText",  badgeText.GetComponent<Text>());

            // ── Hint Button ───────────────────────────────────────────────
            var hintGO  = GetOrCreateButton(hudGO, "HintButton", "💡", 20).gameObject;
            var chargeT = GetOrCreateText(hintGO, "ChargeText", "5", 16, Color.white);
            var rvIcon  = GetOrCreateGO("RvIcon",    hintGO.transform);
            var emptyI  = GetOrCreateGO("EmptyIcon", hintGO.transform);
            EnsureRect(rvIcon).sizeDelta    = new Vector2(24, 24);
            EnsureRect(emptyI).sizeDelta    = new Vector2(24, 24);
            EnsureImage(rvIcon,  new Color(0.9f, 0.7f, 0));
            EnsureImage(emptyI, new Color(0.4f, 0.4f, 0.4f));
            rvIcon.SetActive(false);
            emptyI.SetActive(false);

            Wire(so, "hintButton",     hintGO.GetComponent<Button>());
            Wire(so, "hintChargeText", chargeT.GetComponent<Text>());
            Wire(so, "hintRvIcon",     rvIcon);
            Wire(so, "hintEmptyIcon",  emptyI);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(hud);
        }

        // ══════════════════════════════════════════════════════════════════
        // HOME SCREEN
        // ══════════════════════════════════════════════════════════════════
        static void BuildHomeScreen()
        {
            var home = FindComponent<HomeScreenView>("HomeScreenView");
            if (home == null) { Debug.LogWarning("[Builder] HomeScreenView not found."); return; }

            var homeGO = home.gameObject;
            var so     = new SerializedObject(home);

            // Coin Text — look inside "Coin Group" first
            var coinGroup = homeGO.transform.Find("Coin Group");
            GameObject coinCountGO = coinGroup != null
                ? coinGroup.Find("Coins Count")?.gameObject
                : null;
            if (coinCountGO == null)
                coinCountGO = GetOrCreateText(homeGO, "CoinsCount", "250", 20, Color.white);
            Wire(so, "coinText", coinCountGO.GetComponent<Text>());

            // Settings Button — may already exist
            var settBtn = homeGO.transform.Find("Settings Button");
            if (settBtn == null)
                Wire(so, "settingsButton", GetOrCreateButton(homeGO, "Settings Button", "⚙", 18));
            else
                Wire(so, "settingsButton", settBtn.GetComponent<Button>());

            // Play Button
            var playBtn = homeGO.transform.Find("Play Button");
            if (playBtn != null)
            {
                Wire(so, "playButton", playBtn.GetComponent<Button>());
                var playTextT = playBtn.Find("Play Text");
                if (playTextT != null)
                    Wire(so, "playButtonText", playTextT.GetComponent<Text>());
            }
            else
            {
                var pb = GetOrCreateButton(homeGO, "Play Button", "▶ Play", 20);
                Wire(so, "playButton", pb);
                var pbt = GetOrCreateText(pb.gameObject, "Play Text", "Play (Level 1)", 18, Color.white);
                Wire(so, "playButtonText", pbt.GetComponent<Text>());
            }

            // Daily Challenge Button
            var daily = homeGO.transform.Find("Daily Challenge");
            if (daily != null)
                Wire(so, "dailyChallengeButton", daily.GetComponent<Button>());
            else
                Wire(so, "dailyChallengeButton", GetOrCreateButton(homeGO, "Daily Challenge", "📅", 18));

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(home);
        }

        // ══════════════════════════════════════════════════════════════════
        // LEVEL COMPLETE VIEW
        // ══════════════════════════════════════════════════════════════════
        static void BuildLevelComplete()
        {
            var lc = FindComponent<LevelCompleteView>("LevelCompleteView");
            if (lc == null) { Debug.LogWarning("[Builder] LevelCompleteView not found."); return; }

            var lcGO = lc.gameObject;
            var so   = new SerializedObject(lc);

            // Header text
            var header = lcGO.transform.Find("Header");
            if (header != null)
                Wire(so, "levelCompleteText", GetOrCreateText(header.gameObject, "HeaderText",
                    "Level 1 completed!", 24, new Color(0.2f, 0.6f, 0.2f)).GetComponent<Text>());
            else
                Wire(so, "levelCompleteText", GetOrCreateText(lcGO, "HeaderText",
                    "Level 1 completed!", 24, new Color(0.2f, 0.6f, 0.2f)).GetComponent<Text>());

            // Coin reward button + text + icon
            var coinBtn  = GetOrCreateButton(lcGO, "CoinRewardButton", "🪙", 30);
            var coinText = GetOrCreateText(coinBtn.gameObject, "CoinAmountText", "+25", 18, Color.yellow);
            var coinIcon = GetOrCreateGO("CoinIcon", coinBtn.gameObject.transform);
            EnsureRect(coinIcon).sizeDelta = new Vector2(48, 48);
            EnsureImage(coinIcon, new Color(1f, 0.8f, 0f));
            Wire(so, "coinRewardButton", coinBtn);
            Wire(so, "coinRewardText",   coinText.GetComponent<Text>());
            Wire(so, "coinIcon",         coinIcon.GetComponent<RectTransform>());

            // Next Level Button
            var nextBtn = lcGO.transform.Find("Next Level Button");
            if (nextBtn != null)
            {
                Wire(so, "nextLevelButton", nextBtn.GetComponent<Button>());
                var nlt = nextBtn.Find("Label") ?? nextBtn.GetChild(0);
                if (nlt != null)
                    Wire(so, "nextLevelButtonText", nlt.GetComponent<Text>());
            }
            else
            {
                var nb = GetOrCreateButton(lcGO, "Next Level Button", "Next →", 20);
                Wire(so, "nextLevelButton",     nb);
                Wire(so, "nextLevelButtonText", GetOrCreateText(nb.gameObject, "Label", "Next Level 2", 18, Color.white).GetComponent<Text>());
            }

            // Main Menu Button
            Wire(so, "mainMenuButton", GetOrCreateButton(lcGO, "MainMenuButton", "Menu", 18));

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(lc);
        }

        // ══════════════════════════════════════════════════════════════════
        // SPLASH SCREEN
        // ══════════════════════════════════════════════════════════════════
        static void BuildSplash()
        {
            var splash = FindComponent<SplashScreen>("SplashScreen");
            if (splash == null) { Debug.LogWarning("[Builder] SplashScreen not found."); return; }

            var splashGO = splash.gameObject;
            var so       = new SerializedObject(splash);

            // ProgressBar slider
            var pb = splashGO.transform.Find("ProgressBar");
            if (pb != null)
                Wire(so, "loadingBar", pb.GetComponent<Slider>());

            // Splash Text → loading label
            var splashText = splashGO.transform.Find("Splash Text");
            if (splashText != null)
                Wire(so, "loadingText", splashText.GetComponent<Text>());
            else
                Wire(so, "loadingText", GetOrCreateText(splashGO, "Splash Text", "Loading...", 18, Color.white).GetComponent<Text>());

            // HomeScreenView reference
            var homeView = Object.FindFirstObjectByType<HomeScreenView>();
            if (homeView != null)
                Wire(so, "homeScreenView", homeView);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(splash);
        }

        // ══════════════════════════════════════════════════════════════════
        // GAME BOOTSTRAP — wire all view references
        // ══════════════════════════════════════════════════════════════════
        static void BuildGameBootstrap()
        {
            var bs = Object.FindFirstObjectByType<GameBootstrap>();
            if (bs == null) { Debug.LogWarning("[Builder] GameBootstrap not found."); return; }

            var so = new SerializedObject(bs);

            Bind(so, "hudView",             Object.FindFirstObjectByType<LevelHudView>());
            Bind(so, "levelCompleteView",   Object.FindFirstObjectByType<LevelCompleteView>());
            Bind(so, "homeScreenView",      Object.FindFirstObjectByType<HomeScreenView>());
            Bind(so, "settingsPopup",       Object.FindFirstObjectByType<SettingsPausePopup>());
            Bind(so, "foundWordsScreen",    Object.FindFirstObjectByType<FoundWordsScreen>());
            Bind(so, "tutorialController",  Object.FindFirstObjectByType<TutorialController>());
            Bind(so, "adsManager",          Object.FindFirstObjectByType<AdsManager>());

            // gridView, inputController, levelCatalog, dictionaryDatabase — don't overwrite if already set
            BindIfEmpty(so, "gridView",         Object.FindFirstObjectByType<GridView>());
            BindIfEmpty(so, "inputController",  Object.FindFirstObjectByType<SwipeInputController>());

            // Legacy panel
            var legacyPanel = GameObject.Find("LevelCompletePanel");
            if (legacyPanel != null) BindIfEmpty(so, "levelCompletePanel", legacyPanel);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(bs);
        }

        // ══════════════════════════════════════════════════════════════════
        // Helpers
        // ══════════════════════════════════════════════════════════════════

        static T FindComponent<T>(string goName) where T : Component
        {
            var go = GameObject.Find(goName);
            return go != null ? go.GetComponent<T>() : Object.FindFirstObjectByType<T>();
        }

        static GameObject GetOrCreateGO(string name, Transform parent)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        static Button GetOrCreateButton(GameObject parent, string name, string label, int fontSize)
        {
            var existing = parent.transform.Find(name);
            if (existing != null)
            {
                var b = existing.GetComponent<Button>();
                return b != null ? b : existing.gameObject.AddComponent<Button>();
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120, 40);
            go.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);
            var btn = go.AddComponent<Button>();

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var lrt = labelGO.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.sizeDelta = Vector2.zero;
            var txt = labelGO.AddComponent<Text>();
            txt.text      = label;
            txt.fontSize  = fontSize;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color     = Color.white;

            return btn;
        }

        static GameObject GetOrCreateText(GameObject parent, string name, string content, int fontSize, Color color)
        {
            var existing = parent.transform.Find(name);
            if (existing != null) return existing.gameObject;

            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 30);
            var txt = go.AddComponent<Text>();
            txt.text      = content;
            txt.fontSize  = fontSize;
            txt.color     = color;
            txt.alignment = TextAnchor.MiddleCenter;
            return go;
        }

        static RectTransform EnsureRect(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            return rt;
        }

        static Image EnsureImage(GameObject go, Color color)
        {
            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        static void Wire(SerializedObject so, string prop, Object value)
        {
            if (value == null) return;
            var sp = so.FindProperty(prop);
            if (sp != null) sp.objectReferenceValue = value;
        }

        static void Bind<T>(SerializedObject so, string prop, T value) where T : Object
        {
            if (value == null) return;
            var sp = so.FindProperty(prop);
            if (sp != null) sp.objectReferenceValue = value;
        }

        static void BindIfEmpty<T>(SerializedObject so, string prop, T value) where T : Object
        {
            if (value == null) return;
            var sp = so.FindProperty(prop);
            if (sp != null && sp.objectReferenceValue == null)
                sp.objectReferenceValue = value;
        }
    }
}

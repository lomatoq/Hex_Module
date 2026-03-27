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
    ///
    /// RULE: існуючыя аб'екты НІКОЛІ не рухаюцца і не мяняюцца.
    /// Ствараюцца толькі тыя, якіх яшчэ няма.
    /// Рэферэнсы прывязваюцца заўсёды (нават на існуючыя).
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
            Debug.Log("[SceneHierarchyBuilder] Done.");
            EditorUtility.DisplayDialog("Done!",
                "Усе адсутныя элементы створаны і прывязаны.\nЛакацыі існуючых аб'ектаў не зменены.", "OK");
        }

        // ══════════════════════════════════════════════════════════════════
        // HUD
        // ══════════════════════════════════════════════════════════════════
        static void BuildHud()
        {
            var hud = FindComponent<LevelHudView>("HUD");
            if (hud == null) { Debug.LogWarning("[Builder] HUD/LevelHudView not found."); return; }

            var hudGO = hud.gameObject;
            var so    = new SerializedObject(hud);

            Wire(so, "coinText",        GetOrCreateText(hudGO, "CoinText",         "250", 22, Color.white));
            Wire(so, "settingsButton",  GetOrCreateButton(hudGO, "SettingsButton",  "⚙",  18));
            Wire(so, "foundWordsButton",GetOrCreateButton(hudGO, "FoundWordsButton","📖", 18));

            // Score Badge — дачарны да LastWordText, не чапае пазіцыю LastWordText
            var lastWordGO = hudGO.transform.Find("LastWordText")?.gameObject;
            if (lastWordGO != null)
            {
                bool badgeCreated;
                var badgeRoot = GetOrCreateGO("ScoreBadge", lastWordGO.transform, out badgeCreated);
                if (badgeCreated)
                {
                    // Толькі для НОВАГА аб'екта задаём памер і выгляд
                    AddRect(badgeRoot, new Vector2(50, 26));
                    AddImage(badgeRoot, new Color(0.2f, 0.7f, 0.2f));
                    badgeRoot.SetActive(false);
                }

                bool btCreated;
                var badgeTextGO = GetOrCreateText(badgeRoot, "BadgeText", "+7", 16, Color.white, out btCreated);
                Wire(so, "scoreBadgeRoot", badgeRoot);
                Wire(so, "scoreBadgeText", badgeTextGO.GetComponent<Text>());
            }

            // Hint button + children
            bool hintCreated;
            var hintGO = GetOrCreateButton(hudGO, "HintButton", "💡", 20, out hintCreated).gameObject;

            bool ctCreated;
            var chargeT = GetOrCreateText(hintGO, "ChargeText", "5", 16, Color.white, out ctCreated);

            bool rvCreated, emCreated;
            var rvIcon = GetOrCreateGO("RvIcon",    hintGO.transform, out rvCreated);
            var emIcon = GetOrCreateGO("EmptyIcon", hintGO.transform, out emCreated);

            if (rvCreated) { AddRect(rvIcon, new Vector2(24, 24)); AddImage(rvIcon, new Color(0.9f, 0.7f, 0f)); rvIcon.SetActive(false); }
            if (emCreated) { AddRect(emIcon, new Vector2(24, 24)); AddImage(emIcon, new Color(0.4f, 0.4f, 0.4f)); emIcon.SetActive(false); }

            Wire(so, "hintButton",     hintGO.GetComponent<Button>());
            Wire(so, "hintChargeText", chargeT.GetComponent<Text>());
            Wire(so, "hintRvIcon",     rvIcon);
            Wire(so, "hintEmptyIcon",  emIcon);

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

            // Coin Text: шукаем Coin Group → Coins Count, інакш ствараем
            var coinGroup  = homeGO.transform.Find("Coin Group");
            var coinCountT = coinGroup?.Find("Coins Count");
            GameObject coinCountGO = coinCountT != null
                ? coinCountT.gameObject
                : GetOrCreateText(homeGO, "CoinsCount", "250", 20, Color.white);
            Wire(so, "coinText", coinCountGO.GetComponent<Text>());

            // Settings Button (можа ўжо быць)
            var settT = homeGO.transform.Find("Settings Button");
            if (settT != null)
                Wire(so, "settingsButton", settT.GetComponent<Button>());
            else
                Wire(so, "settingsButton", GetOrCreateButton(homeGO, "Settings Button", "⚙", 18));

            // Play Button
            var playT = homeGO.transform.Find("Play Button");
            if (playT != null)
            {
                Wire(so, "playButton", playT.GetComponent<Button>());
                var playTxt = playT.Find("Play Text");
                if (playTxt != null)
                    Wire(so, "playButtonText", playTxt.GetComponent<Text>());
            }
            else
            {
                var pb = GetOrCreateButton(homeGO, "Play Button", "▶ Play", 20);
                Wire(so, "playButton", pb);
                Wire(so, "playButtonText",
                    GetOrCreateText(pb.gameObject, "Play Text", "Play (Level 1)", 18, Color.white).GetComponent<Text>());
            }

            // Daily Challenge Button
            var dailyT = homeGO.transform.Find("Daily Challenge");
            if (dailyT != null)
                Wire(so, "dailyChallengeButton", dailyT.GetComponent<Button>());
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

            // Header text — шукаем у Header, інакш у root
            var headerT = lcGO.transform.Find("Header");
            var headerParent = headerT != null ? headerT.gameObject : lcGO;
            Wire(so, "levelCompleteText",
                GetOrCreateText(headerParent, "HeaderText", "Level 1 completed!", 24,
                    new Color(0.2f, 0.6f, 0.2f)).GetComponent<Text>());

            // Coin reward button
            bool coinBtnCreated;
            var coinBtn = GetOrCreateButton(lcGO, "CoinRewardButton", "🪙", 30, out coinBtnCreated);
            Wire(so, "coinRewardButton", coinBtn);
            Wire(so, "coinRewardText",
                GetOrCreateText(coinBtn.gameObject, "CoinAmountText", "+25", 18, Color.yellow).GetComponent<Text>());

            bool coinIconCreated;
            var coinIcon = GetOrCreateGO("CoinIcon", coinBtn.gameObject.transform, out coinIconCreated);
            if (coinIconCreated) { AddRect(coinIcon, new Vector2(48, 48)); AddImage(coinIcon, new Color(1f, 0.8f, 0f)); }
            Wire(so, "coinIcon", coinIcon.GetComponent<RectTransform>());

            // Next Level Button (можа ўжо быць)
            var nextT = lcGO.transform.Find("Next Level Button");
            if (nextT != null)
            {
                Wire(so, "nextLevelButton", nextT.GetComponent<Button>());
                var child = nextT.Find("Label") ?? (nextT.childCount > 0 ? nextT.GetChild(0) : null);
                if (child != null) Wire(so, "nextLevelButtonText", child.GetComponent<Text>());
            }
            else
            {
                var nb = GetOrCreateButton(lcGO, "Next Level Button", "Next →", 20);
                Wire(so, "nextLevelButton", nb);
                Wire(so, "nextLevelButtonText",
                    GetOrCreateText(nb.gameObject, "Label", "Next Level 2", 18, Color.white).GetComponent<Text>());
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

            var pbT = splashGO.transform.Find("ProgressBar");
            if (pbT != null) Wire(so, "loadingBar", pbT.GetComponent<Slider>());

            var stT = splashGO.transform.Find("Splash Text");
            if (stT != null)
                Wire(so, "loadingText", stT.GetComponent<Text>());
            else
                Wire(so, "loadingText",
                    GetOrCreateText(splashGO, "Splash Text", "Loading...", 18, Color.white).GetComponent<Text>());

            var homeView = Object.FindFirstObjectByType<HomeScreenView>();
            if (homeView != null) Wire(so, "homeScreenView", homeView);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(splash);
        }

        // ══════════════════════════════════════════════════════════════════
        // GAME BOOTSTRAP
        // ══════════════════════════════════════════════════════════════════
        static void BuildGameBootstrap()
        {
            var bs = Object.FindFirstObjectByType<GameBootstrap>();
            if (bs == null) { Debug.LogWarning("[Builder] GameBootstrap not found."); return; }

            var so = new SerializedObject(bs);

            Bind(so, "hudView",            Object.FindFirstObjectByType<LevelHudView>());
            Bind(so, "levelCompleteView",  Object.FindFirstObjectByType<LevelCompleteView>());
            Bind(so, "homeScreenView",     Object.FindFirstObjectByType<HomeScreenView>());
            Bind(so, "settingsPopup",      Object.FindFirstObjectByType<SettingsPausePopup>());
            Bind(so, "foundWordsScreen",   Object.FindFirstObjectByType<FoundWordsScreen>());
            Bind(so, "tutorialController", Object.FindFirstObjectByType<TutorialController>());
            Bind(so, "adsManager",         Object.FindFirstObjectByType<AdsManager>());

            BindIfEmpty(so, "gridView",       Object.FindFirstObjectByType<GridView>());
            BindIfEmpty(so, "inputController",Object.FindFirstObjectByType<SwipeInputController>());

            var legacyPanel = GameObject.Find("LevelCompletePanel");
            if (legacyPanel != null) BindIfEmpty(so, "levelCompletePanel", legacyPanel);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(bs);
        }

        // ══════════════════════════════════════════════════════════════════
        // Helpers — НІКОЛІ не мяняюць існуючыя аб'екты
        // ══════════════════════════════════════════════════════════════════

        static T FindComponent<T>(string goName) where T : Component
        {
            var go = GameObject.Find(goName);
            return go != null ? go.GetComponent<T>() : Object.FindFirstObjectByType<T>();
        }

        // Вяртае існуючы АБО стварае новы; isNew=true толькі калі стварыўся
        static GameObject GetOrCreateGO(string name, Transform parent, out bool isNew)
        {
            var existing = parent.Find(name);
            if (existing != null) { isNew = false; return existing.gameObject; }
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            isNew = true;
            return go;
        }

        static Button GetOrCreateButton(GameObject parent, string name, string label, int fontSize)
        {
            bool _;
            return GetOrCreateButton(parent, name, label, fontSize, out _);
        }

        static Button GetOrCreateButton(GameObject parent, string name, string label, int fontSize, out bool isNew)
        {
            var existing = parent.transform.Find(name);
            if (existing != null)
            {
                isNew = false;
                var b = existing.GetComponent<Button>();
                return b != null ? b : existing.gameObject.AddComponent<Button>();
            }

            isNew = true;
            var go  = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120, 40);
            go.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);
            var btn = go.AddComponent<Button>();

            var labelGO  = new GameObject("Label");
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
            bool _;
            return GetOrCreateText(parent, name, content, fontSize, color, out _);
        }

        static GameObject GetOrCreateText(GameObject parent, string name, string content,
            int fontSize, Color color, out bool isNew)
        {
            var existing = parent.transform.Find(name);
            if (existing != null) { isNew = false; return existing.gameObject; }

            isNew = true;
            var go  = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 30);
            var txt = go.AddComponent<Text>();
            txt.text      = content;
            txt.fontSize  = fontSize;
            txt.color     = color;
            txt.alignment = TextAnchor.MiddleCenter;
            return go;
        }

        // Дадаць RectTransform толькі калі яго яшчэ няма (не мяняць існуючы)
        static void AddRect(GameObject go, Vector2 size)
        {
            if (go.GetComponent<RectTransform>() != null) return;
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = size;
        }

        // Дадаць Image толькі калі яго яшчэ няма (не мяняць колер існуючага)
        static void AddImage(GameObject go, Color color)
        {
            if (go.GetComponent<Image>() != null) return;
            go.AddComponent<Image>().color = color;
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

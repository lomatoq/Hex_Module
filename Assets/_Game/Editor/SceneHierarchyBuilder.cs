using HexWords.Core;
using HexWords.Gameplay;
using HexWords.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
            BuildFoundWordsScreen();
            BuildSettingsPausePopup();
            BuildRateUsPopup();
            BuildTutorialController();
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

            // Level text
            Wire(so, "levelText",
                GetOrCreateText(hudGO, "LevelText", "Level 1", 20, Color.white).GetComponent<TextMeshProUGUI>());

            // Coin text
            Wire(so, "coinText",
                GetOrCreateText(hudGO, "CoinText", "250", 22, Color.white).GetComponent<TextMeshProUGUI>());

            // Buttons
            Wire(so, "settingsButton",   GetOrCreateButton(hudGO, "SettingsButton",   "⚙",  18));
            Wire(so, "foundWordsButton", GetOrCreateButton(hudGO, "FoundWordsButton", "📖", 18));

            // Score text
            Wire(so, "scoreText",
                GetOrCreateText(hudGO, "ScoreText", "0/10", 18, Color.white).GetComponent<TextMeshProUGUI>());

            // Progress bar — search for existing Slider first
            var pbT = hudGO.transform.Find("ProgressBar");
            if (pbT != null)
            {
                Wire(so, "progressBar", pbT.GetComponent<Slider>());
            }
            else
            {
                bool pbCreated;
                var pbGO = GetOrCreateGO("ProgressBar", hudGO.transform, out pbCreated);
                if (pbCreated) { AddRect(pbGO, new Vector2(300, 20)); }
                var slider = pbGO.GetComponent<Slider>() ?? pbGO.AddComponent<Slider>();
                Wire(so, "progressBar", slider);
            }

            // Score Badge — дачарны да LastWordText, не чапае пазіцыю LastWordText
            var lastWordGO = hudGO.transform.Find("LastWordText")?.gameObject;
            if (lastWordGO != null)
            {
                bool badgeCreated;
                var badgeRoot = GetOrCreateGO("ScoreBadge", lastWordGO.transform, out badgeCreated);
                if (badgeCreated)
                {
                    AddRect(badgeRoot, new Vector2(50, 26));
                    AddImage(badgeRoot, new Color(0.2f, 0.7f, 0.2f));
                    badgeRoot.SetActive(false);
                }

                bool btCreated;
                var badgeTextGO = GetOrCreateText(badgeRoot, "BadgeText", "+7", 16, Color.white, out btCreated);
                Wire(so, "scoreBadgeRoot", badgeRoot);
                Wire(so, "scoreBadgeText", badgeTextGO.GetComponent<TextMeshProUGUI>());
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
            Wire(so, "hintChargeText", chargeT.GetComponent<TextMeshProUGUI>());
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
            Wire(so, "coinText", coinCountGO.GetComponent<TextMeshProUGUI>());

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
                    Wire(so, "playButtonText", playTxt.GetComponent<TextMeshProUGUI>());
            }
            else
            {
                var pb = GetOrCreateButton(homeGO, "Play Button", "▶ Play", 20);
                Wire(so, "playButton", pb);
                Wire(so, "playButtonText",
                    GetOrCreateText(pb.gameObject, "Play Text", "Play (Level 1)", 18, Color.white).GetComponent<TextMeshProUGUI>());
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
                    new Color(0.2f, 0.6f, 0.2f)).GetComponent<TextMeshProUGUI>());

            // Coin reward button
            bool coinBtnCreated;
            var coinBtn = GetOrCreateButton(lcGO, "CoinRewardButton", "🪙", 30, out coinBtnCreated);
            Wire(so, "coinRewardButton", coinBtn);
            Wire(so, "coinRewardText",
                GetOrCreateText(coinBtn.gameObject, "CoinAmountText", "+25", 18, Color.yellow).GetComponent<TextMeshProUGUI>());

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
                if (child != null) Wire(so, "nextLevelButtonText", child.GetComponent<TextMeshProUGUI>());
            }
            else
            {
                var nb = GetOrCreateButton(lcGO, "Next Level Button", "Next →", 20);
                Wire(so, "nextLevelButton", nb);
                Wire(so, "nextLevelButtonText",
                    GetOrCreateText(nb.gameObject, "Label", "Next Level 2", 18, Color.white).GetComponent<TextMeshProUGUI>());
            }

            // Main Menu Button
            Wire(so, "mainMenuButton", GetOrCreateButton(lcGO, "MainMenuButton", "Menu", 18));

            // Ad Banner Slot (optional, hidden by default)
            bool bannerCreated;
            var bannerGO = GetOrCreateGO("AdBannerSlot", lcGO.transform, out bannerCreated);
            if (bannerCreated)
            {
                AddRect(bannerGO, new Vector2(320, 50));
                AddImage(bannerGO, new Color(0.2f, 0.2f, 0.2f));
                bannerGO.SetActive(false);
            }
            Wire(so, "adBannerSlot", bannerGO);

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
                Wire(so, "loadingText", stT.GetComponent<TextMeshProUGUI>());
            else
                Wire(so, "loadingText",
                    GetOrCreateText(splashGO, "Splash Text", "Loading...", 18, Color.white).GetComponent<TextMeshProUGUI>());

            var homeView = Object.FindFirstObjectByType<HomeScreenView>();
            if (homeView != null) Wire(so, "homeScreenView", homeView);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(splash);
        }

        // ══════════════════════════════════════════════════════════════════
        // FOUND WORDS SCREEN
        // ══════════════════════════════════════════════════════════════════
        static void BuildFoundWordsScreen()
        {
            var fws = Object.FindFirstObjectByType<FoundWordsScreen>();
            if (fws == null) { Debug.LogWarning("[Builder] FoundWordsScreen not found."); return; }

            var fwsGO = fws.gameObject;
            var so    = new SerializedObject(fws);

            // Close button
            Wire(so, "closeButton", GetOrCreateButton(fwsGO, "CloseButton", "✕", 20));

            // ScrollView → Content (wordListContainer)
            bool scrollCreated;
            var scrollGO = GetOrCreateGO("ScrollView", fwsGO.transform, out scrollCreated);
            if (scrollCreated) { AddRect(scrollGO, new Vector2(300, 400)); AddImage(scrollGO, new Color(0f, 0f, 0f, 0.1f)); }

            bool contentCreated;
            var contentGO = GetOrCreateGO("Content", scrollGO.transform, out contentCreated);
            if (contentCreated) { AddRect(contentGO, new Vector2(300, 400)); }

            Wire(so, "wordListContainer", contentGO.transform);

            // WordEntry template — inactive GO with Text, used as instantiation template
            bool weCreated;
            var wordEntryGO = GetOrCreateGO("WordEntryTemplate", fwsGO.transform, out weCreated);
            if (weCreated)
            {
                AddRect(wordEntryGO, new Vector2(280, 32));
                var txt = wordEntryGO.AddComponent<TextMeshProUGUI>();
                txt.text      = "WORD";
                txt.fontSize  = 18;
                txt.alignment = TextAlignmentOptions.Left;
                txt.color     = Color.white;
                wordEntryGO.SetActive(false);
            }
            Wire(so, "wordEntryPrefab", wordEntryGO);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(fws);

            // Ensure the GO starts inactive so it is hidden at scene start.
            // FoundWordsScreen.Awake() no longer calls gameObject.SetActive(false),
            // so the scene must start with it inactive.
            if (fwsGO.activeSelf)
            {
                fwsGO.SetActive(false);
                EditorUtility.SetDirty(fwsGO);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // SETTINGS / PAUSE POPUP
        // ══════════════════════════════════════════════════════════════════
        static void BuildSettingsPausePopup()
        {
            var popup = Object.FindFirstObjectByType<SettingsPausePopup>();
            if (popup == null) { Debug.LogWarning("[Builder] SettingsPausePopup not found."); return; }

            var popupGO = popup.gameObject;
            var so      = new SerializedObject(popup);

            // Close button
            Wire(so, "closeButton", GetOrCreateButton(popupGO, "CloseButton", "✕", 18));

            // SFX Toggle
            Wire(so, "sfxToggle",       GetOrCreateToggle(popupGO, "SfxToggle",       "Sound FX"));
            // Music Toggle
            Wire(so, "musicToggle",     GetOrCreateToggle(popupGO, "MusicToggle",     "Music"));
            // Vibration Toggle
            Wire(so, "vibrationToggle", GetOrCreateToggle(popupGO, "VibrationToggle", "Vibration"));

            // SoundManager reference
            var sm = Object.FindFirstObjectByType<SoundManager>();
            if (sm != null) Wire(so, "soundManager", sm);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(popup);
        }

        // ══════════════════════════════════════════════════════════════════
        // RATE US POPUP
        // ══════════════════════════════════════════════════════════════════
        static void BuildRateUsPopup()
        {
            var rate = Object.FindFirstObjectByType<RateUsPopup>();
            if (rate == null) { Debug.LogWarning("[Builder] RateUsPopup not found."); return; }

            var rateGO = rate.gameObject;
            var so     = new SerializedObject(rate);

            // Rate Now button
            Wire(so, "rateNowButton", GetOrCreateButton(rateGO, "RateNowButton", "Rate Now!", 18));

            // Close button
            Wire(so, "closeButton", GetOrCreateButton(rateGO, "CloseButton", "✕", 16));

            // ThankYou sub-popup (hidden by default)
            bool tyCreated;
            var tyGO = GetOrCreateGO("ThankYouPopup", rateGO.transform, out tyCreated);
            if (tyCreated)
            {
                AddRect(tyGO, new Vector2(220, 80));
                AddImage(tyGO, new Color(0.1f, 0.1f, 0.1f, 0.9f));
                GetOrCreateText(tyGO, "ThankYouText", "Thank you! ❤", 18, Color.white);
                tyGO.SetActive(false);
            }
            Wire(so, "thankYouPopup", tyGO);

            // 5 Star buttons + images
            var spStars  = so.FindProperty("starButtons");
            var spImages = so.FindProperty("starImages");
            if (spStars  != null) spStars.arraySize  = 5;
            if (spImages != null) spImages.arraySize = 5;

            for (int i = 0; i < 5; i++)
            {
                bool starCreated;
                var starGO = GetOrCreateGO($"Star{i + 1}", rateGO.transform, out starCreated);
                if (starCreated)
                {
                    AddRect(starGO, new Vector2(48, 48));
                    AddImage(starGO, new Color(0.8f, 0.7f, 0f));
                    GetOrCreateText(starGO, "Label", "★", 24, Color.white);
                }
                var starBtn = starGO.GetComponent<Button>() ?? starGO.AddComponent<Button>();
                var starImg = starGO.GetComponent<Image>();

                if (spStars  != null) spStars.GetArrayElementAtIndex(i).objectReferenceValue  = starBtn;
                if (spImages != null) spImages.GetArrayElementAtIndex(i).objectReferenceValue = starImg;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(rate);
        }

        // ══════════════════════════════════════════════════════════════════
        // TUTORIAL CONTROLLER
        // ══════════════════════════════════════════════════════════════════
        static void BuildTutorialController()
        {
            var tc = Object.FindFirstObjectByType<TutorialController>();
            if (tc == null) { Debug.LogWarning("[Builder] TutorialController not found."); return; }

            var tcGO = tc.gameObject;
            var so   = new SerializedObject(tc);

            // Dim overlay — full-screen dark panel
            bool dimCreated;
            var dimGO = GetOrCreateGO("DimOverlay", tcGO.transform, out dimCreated);
            if (dimCreated)
            {
                AddRect(dimGO, new Vector2(1080, 1920));
                AddImage(dimGO, new Color(0f, 0f, 0f, 0.7f));
                dimGO.SetActive(false);
            }
            Wire(so, "dimOverlay", dimGO);

            // Instruction text
            bool instrCreated;
            var instrGO = GetOrCreateText(tcGO, "InstructionText",
                "Swipe the highlighted tiles!", 22, Color.white, out instrCreated);
            Wire(so, "instructionText", instrGO.GetComponent<TextMeshProUGUI>());

            // Tap to continue prompt
            bool tapCreated;
            var tapGO = GetOrCreateText(tcGO, "TapToContinue",
                "Tap to continue...", 16, new Color(1f, 1f, 1f, 0.7f), out tapCreated);
            if (tapCreated) tapGO.SetActive(false);
            Wire(so, "tapToContinuePrompt", tapGO);

            // Hint button highlight ring
            bool hlCreated;
            var hlGO = GetOrCreateGO("HintButtonHighlight", tcGO.transform, out hlCreated);
            if (hlCreated)
            {
                AddRect(hlGO, new Vector2(80, 80));
                AddImage(hlGO, new Color(1f, 1f, 0f, 0.35f));
                hlGO.SetActive(false);
            }
            Wire(so, "hintButtonHighlight", hlGO);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(tc);
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
            var txt = labelGO.AddComponent<TextMeshProUGUI>();
            txt.text      = label;
            txt.fontSize  = fontSize;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color     = Color.white;

            return btn;
        }

        static Toggle GetOrCreateToggle(GameObject parent, string name, string label)
        {
            var existing = parent.transform.Find(name);
            if (existing != null)
            {
                var t = existing.GetComponent<Toggle>();
                return t != null ? t : existing.gameObject.AddComponent<Toggle>();
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 40);

            // Background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(go.transform, false);
            var bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.sizeDelta = new Vector2(40, 40);
            bgRT.anchorMin = new Vector2(0, 0.5f);
            bgRT.anchorMax = new Vector2(0, 0.5f);
            bgRT.pivot     = new Vector2(0, 0.5f);
            bgRT.anchoredPosition = Vector2.zero;
            bgGO.AddComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f);

            // Checkmark
            var ckGO = new GameObject("Checkmark");
            ckGO.transform.SetParent(bgGO.transform, false);
            var ckRT = ckGO.AddComponent<RectTransform>();
            ckRT.anchorMin = Vector2.zero;
            ckRT.anchorMax = Vector2.one;
            ckRT.sizeDelta = Vector2.zero;
            var ckImg = ckGO.AddComponent<Image>();
            ckImg.color = new Color(0.2f, 0.8f, 0.2f);

            // Label
            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(go.transform, false);
            var lblRT = lblGO.AddComponent<RectTransform>();
            lblRT.anchorMin = new Vector2(0, 0);
            lblRT.anchorMax = new Vector2(1, 1);
            lblRT.offsetMin = new Vector2(48, 0);
            lblRT.offsetMax = Vector2.zero;
            var lblTxt = lblGO.AddComponent<TextMeshProUGUI>();
            lblTxt.text      = label;
            lblTxt.fontSize  = 18;
            lblTxt.color     = Color.white;
            lblTxt.alignment = TextAlignmentOptions.Left;

            // Wire toggle
            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = bgGO.GetComponent<Image>();
            toggle.graphic       = ckImg;
            toggle.isOn          = true;

            return toggle;
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
            var txt = go.AddComponent<TextMeshProUGUI>();
            txt.text      = content;
            txt.fontSize  = fontSize;
            txt.color     = color;
            txt.alignment = TextAlignmentOptions.Center;
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

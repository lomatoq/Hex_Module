using HexWords.UI.Transitions;
using UnityEditor;
using UnityEngine;

namespace HexWords.Editor.Transitions
{
    [CustomEditor(typeof(ScreenAnimator))]
    public class ScreenAnimatorEditor : UnityEditor.Editor
    {
        // ── Persistent foldout keys ────────────────────────────────────────
        private const string K_Elements  = "SAE_elements";
        private const string K_Appear    = "SAE_appear";
        private const string K_Disappear = "SAE_disappear";
        private const string K_Quick     = "SAE_quick";
        private const string K_Delay     = "SAE_delay";
        private const string K_Options   = "SAE_options";
        private const string K_AutoPlay  = "SAE_autoplay";
        private const string K_Idle      = "SAE_idle";

        private bool _foldElements, _foldAppear, _foldDisappear, _foldQuick;
        private bool _foldDelay, _foldOptions, _foldAutoPlay, _foldIdle;

        private void OnEnable()
        {
            _foldElements  = EditorPrefs.GetBool(K_Elements,  true);
            _foldAppear    = EditorPrefs.GetBool(K_Appear,    true);
            _foldDisappear = EditorPrefs.GetBool(K_Disappear, true);
            _foldQuick     = EditorPrefs.GetBool(K_Quick,     false);
            _foldDelay     = EditorPrefs.GetBool(K_Delay,     true);
            _foldOptions   = EditorPrefs.GetBool(K_Options,   false);
            _foldAutoPlay  = EditorPrefs.GetBool(K_AutoPlay,  true);
            _foldIdle      = EditorPrefs.GetBool(K_Idle,      false);
        }

        private void OnDisable()
        {
            // Stop animated preview when inspector loses focus / deselects
            var animator = target as ScreenAnimator;
            animator?.EditorStopAnimatedPreview();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var animator = (ScreenAnimator)target;

            // Repaint inspector every frame while preview is running
            if (animator.PreviewRunning)
                Repaint();

            // ── Preset ────────────────────────────────────────────────────
            Field("preset", "Preset",
                "Optional TransitionPreset SO — provides default configs for elements.");
            Space(4);

            // ── Preview buttons ───────────────────────────────────────────
            DrawPreviewButtons(animator);
            Space(6);

            // ── Animated Elements ─────────────────────────────────────────
            _foldElements = Fold(_foldElements, K_Elements,
                $"Animated Elements  ({ElementCount()})");
            if (_foldElements)
            {
                Indent(() => Field("elements"));
            }
            Space(2);

            // ── Appear Block ──────────────────────────────────────────────
            _foldAppear = Fold(_foldAppear, K_Appear, "Appear Block");
            if (_foldAppear)
                Indent(() => Field("appearSettings"));
            Space(2);

            // ── Disappear Block ───────────────────────────────────────────
            _foldDisappear = Fold(_foldDisappear, K_Disappear, "Disappear Block");
            if (_foldDisappear)
                Indent(() => Field("disappearSettings"));
            Space(2);

            // ── Quick Block ───────────────────────────────────────────────
            _foldQuick = Fold(_foldQuick, K_Quick, "Quick Block  (tab switches)");
            if (_foldQuick)
                Indent(() => Field("quickSettings"));
            Space(2);

            // ── Source-based Delay ────────────────────────────────────────
            _foldDelay = Fold(_foldDelay, K_Delay, "Source-based Delay");
            if (_foldDelay)
            {
                Indent(() =>
                {
                    // ── First appear delay ────────────────────────────────
                    EditorGUILayout.LabelField("On First Appear", EditorStyles.boldLabel);
                    Field("firstAppearDelay", "First Appear Delay  (sec)",
                        "Delay applied the very first time this screen appears (e.g. right after SplashScreen).\n" +
                        "Set 0 to disable. Drag SplashScreen nowhere — this fires automatically.");
                    Space(6);

                    // ── Source whitelist ──────────────────────────────────
                    EditorGUILayout.LabelField("Source Whitelist  (via ScreenTransitionManager)", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox(
                        "Drag any GameObject here (SplashScreen, LoadingScreen, etc.).\n" +
                        "When ScreenTransitionManager switches from one of these, comingFromDelay is applied.\n" +
                        "Works only if both screens use the manager.",
                        MessageType.None);
                    Field("delayWhenComingFrom", "Delay When Coming From");
                    Field("comingFromDelay",     "Coming From Delay  (sec)");
                });
            }
            Space(2);

            // ── Auto-Play ─────────────────────────────────────────────────
            _foldAutoPlay = Fold(_foldAutoPlay, K_AutoPlay, "Auto-Play");
            if (_foldAutoPlay)
            {
                Indent(() =>
                {
                    Field("startHidden",    "Start Hidden",
                        "Snap to disappeared state on Awake — prevents visible flicker before the first animation.");
                    Field("playOnEnable",   "Play On Enable",
                        "Run Appear animation every time this object is activated (SetActive true).\nDisable if ScreenTransitionManager drives everything.");
                    Field("resetOnDisable", "Reset On Disable",
                        "Snap to disappeared state when this object is deactivated.");
                });
            }
            Space(2);

            // ── Options ───────────────────────────────────────────────────
            _foldOptions = Fold(_foldOptions, K_Options, "Options");
            if (_foldOptions)
            {
                Indent(() =>
                {
                    Field("manageCanvasGroup");
                    Field("interruptStrategy");
                    Field("localSpeedMultiplier");
                });
            }
            Space(2);

            // ── Idle Animations ───────────────────────────────────────────
            _foldIdle = Fold(_foldIdle, K_Idle, "Idle Animations  (looping effects while visible)");
            if (_foldIdle)
            {
                Indent(() =>
                {
                    EditorGUILayout.HelpBox(
                        "Define looping visual effects per element.\n" +
                        "Runtime playback (StartIdleAnims / StopIdleAnims) coming soon.",
                        MessageType.Info);
                    Field("idleAnims");
                });
            }

            serializedObject.ApplyModifiedProperties();
        }

        // ── Preview UI ─────────────────────────────────────────────────────

        private void DrawPreviewButtons(ScreenAnimator animator)
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
                Space(2);

                // ── Row 1: snap previews ───────────────────────────────────
                EditorGUILayout.LabelField("Snap (instant):", EditorStyles.miniLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    var prev = GUI.backgroundColor;

                    GUI.backgroundColor = new Color(0.55f, 1f, 0.55f);
                    if (GUILayout.Button("▶  Appear", GUILayout.Height(24)))
                    {
                        Undo.RecordObject(animator, "Preview Appear");
                        animator.EditorPreviewAppear();
                        EditorUtility.SetDirty(animator);
                        SceneView.RepaintAll();
                    }

                    GUI.backgroundColor = new Color(1f, 0.65f, 0.55f);
                    if (GUILayout.Button("◀  Disappear", GUILayout.Height(24)))
                    {
                        Undo.RecordObject(animator, "Preview Disappear");
                        animator.EditorPreviewDisappear();
                        EditorUtility.SetDirty(animator);
                        SceneView.RepaintAll();
                    }

                    GUI.backgroundColor = new Color(0.82f, 0.82f, 0.82f);
                    if (GUILayout.Button("↺  Reset", GUILayout.Width(68), GUILayout.Height(24)))
                    {
                        Undo.RecordObject(animator, "Reset Positions");
                        animator.EditorResetPositions();
                        EditorUtility.SetDirty(animator);
                        SceneView.RepaintAll();
                    }

                    GUI.backgroundColor = prev;
                }

                Space(4);

                // ── Row 2: animated previews ───────────────────────────────
                EditorGUILayout.LabelField("Animated (real-time):", EditorStyles.miniLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    var prev = GUI.backgroundColor;

                    bool running = animator.PreviewRunning;

                    GUI.backgroundColor = running ? new Color(0.4f, 0.85f, 1f) : new Color(0.5f, 0.85f, 1f);
                    if (GUILayout.Button("▶▶ Appear", GUILayout.Height(24)))
                    {
                        animator.EditorStartAnimatedPreview(appear: true);
                    }

                    GUI.backgroundColor = running ? new Color(1f, 0.75f, 0.4f) : new Color(1f, 0.78f, 0.5f);
                    if (GUILayout.Button("▶▶ Disappear", GUILayout.Height(24)))
                    {
                        animator.EditorStartAnimatedPreview(appear: false);
                    }

                    GUI.backgroundColor = running
                        ? new Color(1f, 0.4f, 0.4f)
                        : new Color(0.7f, 0.7f, 0.7f);

                    using (new EditorGUI.DisabledScope(!running))
                    {
                        if (GUILayout.Button("■ Stop", GUILayout.Width(68), GUILayout.Height(24)))
                        {
                            animator.EditorStopAnimatedPreview();
                            SceneView.RepaintAll();
                        }
                    }

                    GUI.backgroundColor = prev;
                }

                Space(2);

                // ── Status line ────────────────────────────────────────────
                var mini = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
                string status;
                if (Application.isPlaying)
                {
                    status = animator.IsAnimating ? "● Animating"
                           : animator.IsVisible   ? "● Visible"
                                                  : "● Hidden";
                }
                else if (animator.PreviewRunning)
                {
                    status = "▶▶ Preview playing…";
                }
                else
                {
                    status = "Edit mode — snap or animated preview";
                }
                EditorGUILayout.LabelField(status, mini);
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private bool Fold(bool current, string key, string label)
        {
            bool next = EditorGUILayout.Foldout(current, label, true,
                new GUIStyle(EditorStyles.foldoutHeader));
            if (next != current) EditorPrefs.SetBool(key, next);
            return next;
        }

        private void Indent(System.Action draw)
        {
            EditorGUI.indentLevel++;
            draw();
            EditorGUI.indentLevel--;
        }

        private static void Space(float px) => EditorGUILayout.Space(px);

        private void Field(string propName, string label = null, string tooltip = null)
        {
            var prop = serializedObject.FindProperty(propName);
            if (prop == null) return;
            EditorGUILayout.PropertyField(prop,
                label != null ? new GUIContent(label, tooltip) : new GUIContent(prop.displayName),
                true);
        }

        private int ElementCount()
            => serializedObject.FindProperty("elements")?.arraySize ?? 0;
    }
}

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace HexWords.Editor
{
    /// <summary>
    /// One-click responsive UI setup.
    /// Menu: HexWords ▸ Setup Responsive UI
    ///
    /// What it does:
    ///  1. Canvas Scaler → Scale With Screen Size, 1080×1920, Match Height
    ///  2. For every RectTransform direct-child of HUD:
    ///       top 25 % of canvas  → anchor to top    (min/max Y = 1)
    ///       bottom 25 %         → anchor to bottom (min/max Y = 0)
    ///       middle 50 %         → anchor to center (min/max Y = 0.5)
    ///     X anchors are preserved (they're usually already correct).
    ///  3. GridRoot & TrailRoot → full-stretch anchors (0,0 → 1,1).
    ///  4. Background           → full-stretch anchors.
    /// </summary>
    public static class ResponsiveUISetup
    {
        private const float REF_W = 1080f;
        private const float REF_H = 1920f;

        [MenuItem("HexWords/Setup Responsive UI")]
        public static void Run()
        {
            // ── 1. Canvas Scaler ───────────────────────────────────────────
            var scaler = Object.FindObjectOfType<CanvasScaler>();
            if (scaler == null)
            {
                Debug.LogError("[ResponsiveUISetup] No CanvasScaler found in scene.");
                return;
            }

            Undo.RecordObject(scaler, "Responsive UI – Canvas Scaler");
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(REF_W, REF_H);
            scaler.matchWidthOrHeight  = 1f;   // match height → portrait
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            EditorUtility.SetDirty(scaler);

            // ── 2. Full-stretch helpers ────────────────────────────────────
            SetStretch("Background");
            SetStretch("GridRoot");
            SetStretch("TrailRoot");

            // ── 3. HUD children ────────────────────────────────────────────
            var hud = GameObject.Find("HUD");
            if (hud == null)
            {
                Debug.LogWarning("[ResponsiveUISetup] 'HUD' GameObject not found. Skipping HUD anchors.");
            }
            else
            {
                var canvas     = hud.GetComponentInParent<Canvas>();
                var canvasRect = canvas.GetComponent<RectTransform>();
                // In Scale-With-Screen-Size the canvas rect matches the reference resolution
                // before the first frame, so use the reference height directly.
                float canvasH  = canvasRect.rect.height;
                if (canvasH < 1f) canvasH = REF_H;   // fallback when not yet laid out

                foreach (Transform child in hud.transform)
                {
                    var rt = child.GetComponent<RectTransform>();
                    if (rt == null) continue;

                    Undo.RecordObject(rt, "Responsive UI – " + child.name);

                    // Normalised Y of the element centre in canvas space
                    float centreY    = rt.localPosition.y + canvasH * 0.5f;
                    float normY      = centreY / canvasH;

                    float anchorMinY = rt.anchorMin.y;
                    float anchorMaxY = rt.anchorMax.y;

                    if (normY > 0.75f)
                    {
                        anchorMinY = 1f;
                        anchorMaxY = 1f;
                        Debug.Log($"[Responsive] {child.name} → TOP  (normY={normY:F2})");
                    }
                    else if (normY < 0.25f)
                    {
                        anchorMinY = 0f;
                        anchorMaxY = 0f;
                        Debug.Log($"[Responsive] {child.name} → BOTTOM (normY={normY:F2})");
                    }
                    else
                    {
                        anchorMinY = 0.5f;
                        anchorMaxY = 0.5f;
                        Debug.Log($"[Responsive] {child.name} → CENTER (normY={normY:F2})");
                    }

                    rt.anchorMin = new Vector2(rt.anchorMin.x, anchorMinY);
                    rt.anchorMax = new Vector2(rt.anchorMax.x, anchorMaxY);

                    EditorUtility.SetDirty(rt);
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[ResponsiveUISetup] Done. Check anchors in Inspector and save the scene.");
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static void SetStretch(string goName)
        {
            var go = GameObject.Find(goName);
            if (go == null) return;
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) return;

            Undo.RecordObject(rt, "Responsive UI – " + goName + " stretch");
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            EditorUtility.SetDirty(rt);
            Debug.Log($"[Responsive] {goName} → STRETCH");
        }
    }
}

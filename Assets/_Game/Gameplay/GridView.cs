// Remove this comment after installing DOTween via Asset Store
#define DOTWEEN

using System.Collections;
using System.Collections.Generic;
using HexWords.Core;
using HexWords.Theming;
using UnityEngine;
#if DOTWEEN
using DG.Tweening;
#endif

namespace HexWords.Gameplay
{
    public class GridView : MonoBehaviour
    {
        [SerializeField] private HexCellView         cellPrefab;
        [SerializeField] private RectTransform        gridRoot;
        [SerializeField] private RectTransform        trailRoot;
        [SerializeField] private float                cellSize = 90f;
        [SerializeField] private HintAnimationConfig  hintConfig;
        [SerializeField] private FeedbackPalette      feedbackPalette;
        private FeedbackPalette Palette => FeedbackPaletteProvider.Resolve(feedbackPalette);
        [SerializeField] private HexCellAnimConfig    animConfig;
        [Tooltip("Enable state color + sequential bounce effect for Bonus words.")]
        [SerializeField] private bool                 bonusWordEffect = true;

        [Header("Auto-Scale")]
        [Tooltip("If true, cellSize is computed from gridRoot rect so the grid fills the available space.")]
        [SerializeField] private bool  autoScale        = true;
        [Tooltip("Fraction of available space the grid occupies (0.88 = 12% padding).")]
        [SerializeField] private float autoScalePadding = 0.88f;

        [Header("Spacing")]
        [Tooltip("Multiplier for the distance between cell centres.\n1.0 = cells touch, >1 = gaps, <1 = overlap.")]
        [Range(0.5f, 2f)]
        [SerializeField] private float cellSpacing = 1.0f;

        private readonly Dictionary<string, HexCellView> _cellViews = new Dictionary<string, HexCellView>();
        private readonly List<GameObject>               _letterGOs = new List<GameObject>();
        private RectTransform _letterRoot;
        private Coroutine _hintRoutine;

#if DOTWEEN
        private int TweenId => GetInstanceID();
#endif

        public IReadOnlyDictionary<string, HexCellView> CellViews => _cellViews;
        public int HintRevealCount => hintConfig != null ? hintConfig.revealCount : 2;

        // ── Build ──────────────────────────────────────────────────────────

        public void Build(LevelDefinition level)
        {
            Clear();
            var cells = level.shape.cells;
            if (level.boardLayoutMode == BoardLayoutMode.Fixed16Symmetric &&
                cells != null &&
                cells.Count == HexBoardTemplate16.CellCount &&
                !HexBoardTemplate16.HasCanonicalShape(level.shape))
            {
                cells = new List<CellDefinition>(cells);
                HexBoardTemplate16.ApplyCanonicalLayout(cells);
            }

            float effective = autoScale ? ComputeCellSize(cells) : cellSize;

            float hexW = Mathf.Sqrt(3f) * effective;
            float hexH = 2f * effective;

            foreach (var cell in cells)
            {
                var view = Instantiate(cellPrefab, gridRoot);
                view.Bind(cell);
                var rt = view.GetComponent<RectTransform>();
                // cellSpacing moves centres apart; visual size stays at 'effective'
                var pos = AxialToPixel(cell.q, cell.r, effective * cellSpacing);
                rt.anchoredPosition = pos;
                rt.sizeDelta = new Vector2(hexW, hexH);
                _cellViews.Add(cell.cellId, view);
            }

            // Reparent letter texts into a dedicated layer above TrailRoot
            var lr = GetOrCreateLetterRoot();
            foreach (var pair in _cellViews)
            {
                var lt = pair.Value.LetterText;
                if (lt == null) continue;
                var lRt = lt.rectTransform;
                // worldPositionStays=true: letter keeps its world position (= cell centre)
                lRt.SetParent(lr, true);
                lRt.sizeDelta    = new Vector2(hexW, hexH);
                lt.raycastTarget = false;
                _letterGOs.Add(lt.gameObject);
            }
        }

        // ── Word state FX ─────────────────────────────────────────────────

        public float WordEffectHoldExtra      => animConfig != null ? animConfig.wordColorHoldExtra      : 0.25f;
        public float WordEffectReturnDuration => animConfig != null ? animConfig.wordColorReturnDuration : 0.35f;

        /// <summary>
        /// Tints cells in the word path to the state color, holds, then fades back.
        /// Returns the state color used (Color.clear if no effect was applied).
        /// </summary>
        public Color PlayWordStateEffect(
            IReadOnlyList<string> orderedCellIds,
            WordSubmitOutcome outcome,
            bool hasScore,
            float dropDuration)
        {
            if (feedbackPalette == null || orderedCellIds == null) return Color.clear;

            // AlreadyAccepted — no visual effect
            if (outcome == WordSubmitOutcome.AlreadyAccepted) return Color.clear;

            // Bonus — skip if toggle is off
            if (outcome == WordSubmitOutcome.BonusAccepted && !bonusWordEffect) return Color.clear;

            Color stateColor = outcome switch
            {
                WordSubmitOutcome.TargetAccepted => Palette.cellWordTargetColor,
                WordSubmitOutcome.BonusAccepted  => Palette.cellWordBonusColor,
                _                                => Color.clear
            };

            if (stateColor == Color.clear) return Color.clear;

            float holdExtra = animConfig != null ? animConfig.wordColorHoldExtra      : 0.25f;
            float returnDur = animConfig != null ? animConfig.wordColorReturnDuration : 0.35f;
            float stagger   = animConfig != null ? animConfig.wordBounceStagger       : 0.06f;
            float holdDur   = dropDuration + holdExtra;

            for (int i = 0; i < orderedCellIds.Count; i++)
            {
                if (!_cellViews.TryGetValue(orderedCellIds[i], out var view)) continue;
                view.ApplyWordStateColor(stateColor, holdDur, returnDur);
                if (hasScore)
                    view.PlayWordBounce(i * stagger, animConfig);
            }

            return stateColor;
        }

        // ── FX ────────────────────────────────────────────────────────────

        public void ResetFx()
        {
#if DOTWEEN
            DOTween.Kill(TweenId);
#else
            if (_hintRoutine != null) { StopCoroutine(_hintRoutine); _hintRoutine = null; }
#endif
            foreach (var pair in _cellViews) pair.Value.ResetFx();
        }

        public void PlayHintAnimation(IReadOnlyList<string> cellIds)
        {
#if DOTWEEN
            DOTween.Kill(TweenId);
            int   reps      = hintConfig != null ? hintConfig.repetitionCount         : 1;
            float stepDelay = hintConfig != null ? hintConfig.delayBetweenCells       : 0.18f;
            float repDelay  = hintConfig != null ? hintConfig.delayBetweenRepetitions : 0.6f;
            float pulseDur  = hintConfig != null
                ? (hintConfig.pulseFadeIn + hintConfig.pulseFadeOut) * hintConfig.pulseCount
                  + hintConfig.pauseBetweenPulses * (hintConfig.pulseCount - 1)
                : 1.1f;
            float seriesDur = (cellIds.Count - 1) * stepDelay + pulseDur;

            var seq = DOTween.Sequence().SetId(TweenId);
            for (var rep = 0; rep < reps; rep++)
            {
                seq.AppendCallback(() =>
                {
                    for (var i = 0; i < cellIds.Count; i++)
                        if (_cellViews.TryGetValue(cellIds[i], out var v))
                            v.PlayHintPulse(i * stepDelay, hintConfig);
                });
                if (rep < reps - 1) seq.AppendInterval(seriesDur + repDelay);
            }
#else
            if (_hintRoutine != null) StopCoroutine(_hintRoutine);
            _hintRoutine = StartCoroutine(HintSequenceRoutine(cellIds));
#endif
        }

        // ── Coroutine fallback ─────────────────────────────────────────────

#if !DOTWEEN
        private IEnumerator HintSequenceRoutine(IReadOnlyList<string> cellIds)
        {
            int   reps      = hintConfig != null ? hintConfig.repetitionCount         : 1;
            float stepDelay = hintConfig != null ? hintConfig.delayBetweenCells       : 0.18f;
            float repDelay  = hintConfig != null ? hintConfig.delayBetweenRepetitions : 0.6f;
            float pulseDur  = hintConfig != null
                ? (hintConfig.pulseFadeIn + hintConfig.pulseFadeOut) * hintConfig.pulseCount
                  + hintConfig.pauseBetweenPulses * (hintConfig.pulseCount - 1)
                : 1.1f;
            float seriesDur = (cellIds.Count - 1) * stepDelay + pulseDur;

            for (int rep = 0; rep < reps; rep++)
            {
                float delay = 0f;
                for (int i = 0; i < cellIds.Count; i++)
                {
                    if (_cellViews.TryGetValue(cellIds[i], out var view))
                        view.PlayHintPulse(delay, hintConfig);
                    delay += stepDelay;
                }
                if (rep < reps - 1)
                    yield return new WaitForSeconds(seriesDur + repDelay);
            }
            _hintRoutine = null;
        }
#endif

        // ── Helpers ────────────────────────────────────────────────────────

        private RectTransform GetOrCreateLetterRoot()
        {
            if (_letterRoot != null) return _letterRoot;

            var go = new GameObject("LetterRoot", typeof(RectTransform));
            _letterRoot = go.GetComponent<RectTransform>();
            _letterRoot.SetParent(gridRoot.parent, false);
            // Full-stretch so anchoredPositions of letters match gridRoot space
            _letterRoot.anchorMin = Vector2.zero;
            _letterRoot.anchorMax = Vector2.one;
            _letterRoot.offsetMin = _letterRoot.offsetMax = Vector2.zero;

            // Place immediately after trailRoot; fall back to last sibling
            if (trailRoot != null)
                _letterRoot.SetSiblingIndex(trailRoot.GetSiblingIndex() + 1);
            else
                _letterRoot.SetAsLastSibling();

            return _letterRoot;
        }

        private void Clear()
        {
#if DOTWEEN
            DOTween.Kill(TweenId);
#endif
            for (var i = gridRoot.childCount - 1; i >= 0; i--)
                Destroy(gridRoot.GetChild(i).gameObject);
            // Destroy reparented letter GOs (they live in _letterRoot, not gridRoot)
            foreach (var go in _letterGOs) if (go != null) Destroy(go);
            _letterGOs.Clear();
            _cellViews.Clear();
        }

        private float ComputeCellSize(IReadOnlyList<CellDefinition> cells)
        {
            if (gridRoot == null || cells == null || cells.Count == 0) return cellSize;

            // Force layout rebuild so rect is up to date before reading it
            Canvas.ForceUpdateCanvases();
            var available = gridRoot.rect;
            if (available.width < 1f || available.height < 1f) return cellSize;

            // Bounding box of cell centres at size = 1
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var c in cells)
            {
                var p = AxialToPixel(c.q, c.r, 1f);
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
            }

            // Add one full hex diameter (pointy-top: w = sqrt(3), h = 2)
            float spanX = (maxX - minX) + Mathf.Sqrt(3f);
            float spanY = (maxY - minY) + 2f;

            float scaleX = available.width  / spanX;
            float scaleY = available.height / spanY;
            return Mathf.Min(scaleX, scaleY) * autoScalePadding;
        }

        private static Vector2 AxialToPixel(int q, int r, float size)
        {
            return new Vector2(
                size * (Mathf.Sqrt(3f) * q + Mathf.Sqrt(3f) / 2f * r),
               -size * (3f / 2f * r));
        }
    }
}

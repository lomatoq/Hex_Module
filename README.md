# HexWords — Unity 6 Mobile Word Game

A mobile hex-grid word game prototype built with Unity 6000.3.8f1.
Players drag a path across neighbouring hex cells to form words, earning points toward a level target.

---

## Tech stack

| Thing | Detail |
|---|---|
| Engine | Unity 6000.3.8f1 |
| UI | Unity UI (uGUI) + TextMeshPro |
| Animations | DOTween (Asset Store) — all code guarded by `#define DOTWEEN` |
| Language | C# 9, no external runtime dependencies |
| Platforms | iOS / Android (portrait) |

---

## Folder layout

```
Assets/_Game/
  Core/          — domain types, interfaces, services (ScoreService, AdjacencyService…)
  Gameplay/      — runtime session, swipe input, grid view, trail view, cell view
  UI/            — HUD, home screen, popups, safe area panel, ButtonFeedback
  Editor/        — CSV importer, level editor, auto-generator, responsive UI setup, DevPanel builder
  Data/
    Source/      — CSV source files (dictionary, levels, cells)
    Generated/   — ScriptableObject assets produced by importers
    FeedbackPalette.asset — centralised color config for all UI feedback
  Graphics/
    UI/          — sprites and textures
    Segments/    — swipe trail segment graphics
  Prefabs/       — HexCell prefab
  Scenes/        — Main.unity
Assets/Tests/    — EditMode + PlayMode tests
```

---

## Quick start

1. Open project in **Unity 6000.3.8f1**.
2. Install **DOTween** from the Asset Store, run its setup wizard.
3. `Tools → HexWords → Import CSV` — imports dictionary + levels from `Data/Source/`.
4. `Tools → HexWords → Level Editor` — create / validate / preview individual levels.
5. `Tools → HexWords → Build Dev Panel` — builds the in-game developer panel (run once after scene setup).
6. Hit **Play** — press **L** or tap the top-left corner 5× to open the DevPanel: pick any level, restart, or trigger the win screen.

---

## Core gameplay

- **Swipe path** — tap and drag across adjacent hex cells; backtrack by re-entering the previous cell.
- **Word validation** — two modes: `LevelOnly` (only target words score) / `Dictionary` (any valid dictionary word scores as bonus).
- **Outcomes** — `TargetAccepted`, `BonusAccepted`, `AlreadyAccepted`, `Rejected`.
- **Level completion** — both conditions must be met: `currentScore ≥ targetScore` AND `acceptedTargetCount ≥ minTargetWordsToComplete`.
- **Score table** (nonlinear, mirrors Wordle-style games):

  | Length | Points |
  |--------|--------|
  | 3 | 1 |
  | 4 | 3 |
  | 5 | 7 |
  | 6 | 13 |
  | 7 | 21 |
  | 8 | 31 |
  | 9 | 43 |
  | 10 | 57 |

---

## UI & Feedback system

### FeedbackPalette (ScriptableObject)

All game feedback colors live in a single `FeedbackPalette` asset (`Data/FeedbackPalette.asset`).
Change colors here — they propagate everywhere automatically.

| Group | Fields |
|---|---|
| Hex Cell backgrounds | `selectedCellColor`, `targetAcceptedCellColor`, `bonusAcceptedCellColor`, `alreadyAcceptedCellColor`, `rejectedCellColor` |
| Hex Cell letters | `cellLetterDefault`, `cellLetterSelected` |
| HUD bubble | `hudTargetAcceptedColor`, `hudBonusAcceptedColor`, `hudAlreadyAcceptedColor`, `hudRejectedColor`, `hudCurrentWordColor` |
| Bubble text | `hudBubbleTextDefault`, `hudBubbleTextAlreadyFound` |

### ButtonFeedback

Drop the `ButtonFeedback` component on any Button for a press-bounce effect.
Built-in presets: `Default`, `Small`, `Soft`, `Strong`, `Custom`.

---

## Animations (DOTween)

All animation code is wrapped in `#if DOTWEEN` — the project compiles cleanly without the package.

### HexCellView
- **OnSelected** — `DOPunchScale` spring pop + letter color switches to `cellLetterSelected`.
- **OnPathRejected** — color flash + horizontal `DOShakePosition` + letter color reset.
- **OnPathAccepted / OnPathBonusAccepted** — color flash + `DOPunchScale` + ink splat overlay.
- **PlayHintPulse** — DOTween Sequence of pulses with configurable fade-in/out and repetition (`HintAnimationConfig`).
- **ResetFx** — kills all tweens, restores `localScale`, `anchoredPosition`, base background color, and base letter color.

### SwipeTrailView
- Spawns ink **dot** at each selected cell (`DOScale` pop, `Ease.OutBack`).
- Spawns **pill segment** between consecutive cells.
- **FadeOutAndClear** — `DOFade` on all images, then `DOVirtual.DelayedCall` cleanup.

### GridView — hint sequence
- `PlayHintAnimation` runs a DOTween Sequence: per-cell `AppendCallback` → `AppendInterval`, repeated N times.

### Word preview bubble (LevelHudView)
- **Dynamic width** — `RectTransform.sizeDelta.x` animated via `DOSizeDelta` from `lastWordText.preferredWidth + padding`.
- **Accent color sync** — bubble background, score badge image, and flying drops all share `_currentAccentColor` set at preview time.
- **CanvasGroup alpha** — the entire bubble (text + badge + background) fades as one unit; text and badge are cleared only after `alpha = 0`.
- **PlayBubbleAccepted** (new word) — `DOScale` bounce, then after delay → dismiss.
- **PlayBubbleAccepted** (already found) — horizontal `DOShakeAnchorPos` left-right instead of bounce.
- **PlayBubbleDismiss** — `DOAnchorPosY` fly-up + `DOFade` to 0 via CanvasGroup. Text and badge hidden in `OnComplete`.
- **Score drop** — on accepted new word, a chain of N circles (head = full size, tail = `scoreDropTailScale`) launches along a configurable quadratic bezier arc from the score badge to the progress bar. Each circle tinted to the current accent color. Drops only fire when the word actually awards points.

### Progress bar (LevelHudView)
- **Fill** — `DOTween.To` with `AnimationCurve` (`scoreFillCurve`), delayed by `scoreDropDuration` so it starts on drop arrival.
- **Bounce** — `DOPunchScale` on bar + score label, with `scoreBarAnticipation` offset so it begins just before the drop lands.
- Progress bars (both HUD and Home) are `interactable = false` — display only, not draggable.

### Splash screen (SplashScreen)
- Animated progress bar: `0 → 90 %` over `minDisplaySeconds × 0.9` with `barCurve`, then `90 → 100 %` when config is ready.
- Shows **LOADING…** throughout; no "Ready!" text.

All timing, curves, and scales are exposed as `[SerializeField]` fields in the Inspector under clearly labelled headers.

---

## Word preview bubble — Inspector wiring

Bubble requires two GameObject layers inside `WordPreview`:

| Layer | Component | Role |
|---|---|---|
| BubbleStroke | Image | Colored background (stroke), padding -6 on all sides via anchors 0,0→1,1 |
| BubbleFill | Image | White fill on top |

`WordPreview` also needs a **CanvasGroup** component → wire to `Word Bubble Canvas Group` on `LevelHudView`.

Score badge must be a **child of `WordPreview`** so it fades with the CanvasGroup.

### Score drop setup
1. Create a small circle Image inside the HUD Canvas, ~24×24 px, set inactive → wire to `Score Drop Template`.
2. Drag the progress bar `RectTransform` → `Score Drop Target`.
3. Tune `Score Drop Control Offset` to shape the arc (`(0, 180)` = arc upward).

---

## Responsive UI

### Canvas Scaler
`HexWords → Setup Responsive UI` (Editor menu) does in one click:
- Sets **Canvas Scaler** → Scale With Screen Size, **1080×1920**, Match Height.
- `Background`, `GridRoot`, `TrailRoot` → full-stretch anchors (0,0 → 1,1).
- HUD children auto-anchored: top 25 % → top, bottom 25 % → bottom, center → center.

### Safe Area (notch / home bar)
Add **SafeAreaPanel** component to a full-screen child of Canvas.
At runtime it reads `Screen.safeArea` and adjusts `anchorMin`/`anchorMax` every frame when the value changes.
In Editor, enable `Simulate In Editor` + set `Simulated Inset` to test notch layouts.

### Hex grid auto-scale
`GridView` has an **Auto Scale** toggle (on by default):
- Reads `gridRoot.rect` at build time (`Canvas.ForceUpdateCanvases()`).
- Computes the bounding box of all hex centres at size = 1.
- Picks `min(scaleX, scaleY) × autoScalePadding` so the grid always fits.
- **Cell Spacing** (Range 0.5–2.0) — multiplier for the distance between cell centres:
  - `1.0` = cells touch flush
  - `> 1.0` = visible gap
  - `< 1.0` = overlap

---

## Level auto-generation

`Tools → HexWords → Generate Levels (Auto)`

| Field | Purpose |
|---|---|
| Dictionary | Source word list |
| Generation Profile | Algorithm settings |
| Start ID / Count | Range of level IDs to produce (capped at 100 000) |
| Level Solve Budget | Time limit per level solver (default 8 000 ms) |

### Algorithm (GreedyBeamV2)
1. **Stratified pool sampling** — candidates grouped by word length, equal quota per length per attempt, prevents short-word domination.
2. **Word-set optimisation** — Greedy + Beam search over `GenerationObjective`.
3. **Board placement** — DFS solvability validation with time budget.
4. **Score calculation** — uses the nonlinear `ScoreForLength` table (matches `ScoreService`) so `targetScore` is always beatable.

### GenerationProfileMixed settings
- `avoidDuplicateLetters = false`, `maxLetterRepeats = 2` — allows richer bonus word sets on the same board.

---

## Dictionary pipeline

- `Tools → HexWords → Dictionary Importer` — imports `.txt` (one word per line) or `.csv` (with `word` column).
- Normalises, deduplicates, filters by language and length.
- EN mode: frequency-driven (`frequency_en.txt`), auto-filters calendar/time tokens and abbreviation noise, optional `generator_blacklist_en.txt`.

### CSV formats

**dictionary_ru.csv / dictionary_en.csv**
```
word,category,minLevel,maxLevel,difficultyBand
```

**levels.csv**
```
levelId,language,validationMode,targetScore,targetWords
```
`targetWords` uses `|` as separator.

**level_cells.csv**
```
levelId,cellId,letter,q,r
```

---

## Key scripts

| Script | Role |
|---|---|
| `GameBootstrap` | Wires all services, starts session, handles events |
| `LevelSessionController` | Word submit logic, score tracking, completion check |
| `GridView` | Builds hex cell views, auto-scales to screen |
| `HexCellView` | Per-cell visuals + DOTween FX + letter color feedback |
| `SwipeInputController` | Touch input → path builder → session |
| `SwipeTrailView` | Ink trail DOTween effects |
| `LevelHudView` | HUD + word bubble + score drop animation |
| `HomeScreenView` | Home screen with animated progress bar |
| `SplashScreen` | Animated loading bar with DOTween bezier fill |
| `ButtonFeedback` | Reusable press-bounce component for any Button |
| `SafeAreaPanel` | Runtime safe area inset adjustment |
| `DevLevelSelector` | In-game dev panel: level picker, restart, win trigger |
| `DevLevelSelectorBuilder` | Editor tool that auto-builds and wires the DevPanel |
| `DOTweenInit` | Initialises DOTween pool at startup (ExecutionOrder -100) |
| `AutoLevelGeneratorWindow` | Editor window for batch level generation |
| `ResponsiveUISetup` | Editor menu one-click anchor + Canvas Scaler fix |

---

## Notes

- `Е` and `Ё` are treated as distinct letters.
- `Fixed16Symmetric` layout uses a canonical 16-cell topology (`3-3-4-3-3`) shared across all generated levels via `HexBoardTemplate16`.
- `RuntimePreviewConfig` (under `Resources`) stores the level selected in the Editor for in-Play preview.
- `GameBootstrap` defensively recalculates `targetScore` via `ScoreService.ScoreWord()` before session start to guard against stale generated values.
- All `Slider` components used as progress displays have `interactable = false` set in `Awake()` — they cannot be dragged by the user.

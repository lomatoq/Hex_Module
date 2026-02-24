# HexWords MVP (Unity 6.3 LTS)

Implemented core mechanics for a hex-cell word game with:
- drag path over neighboring hexes (no cell reuse)
- level score progression by word length
- dual validation modes (`LevelOnly`, `Dictionary`)
- CSV import pipeline into ScriptableObjects
- minimal editor tooling (`Tools/HexWords/Level Editor`)

## Unity version
- `6000.3.8f1`

## Folder layout
- `Assets/_Game/Core` domain types and interfaces
- `Assets/_Game/Gameplay` runtime logic and session flow
- `Assets/_Game/UI` HUD and completion view scripts
- `Assets/_Game/Editor` CSV importer, level editor window, generation helpers
- `Assets/_Game/Data/Source` CSV source files
- `Assets/_Game/Data/Generated` generated ScriptableObject assets destination
- `Assets/Tests` edit/play mode tests

## Quick start
1. Open project in Unity 6000.3.8f1.
2. Run `Tools -> HexWords -> Import CSV`.
3. Open `Tools -> HexWords -> Level Editor`.
4. Select/create a `LevelDefinition` and validate it.
5. Click `Preview Play` from the editor window.
6. In scene, ensure `GameBootstrap` references:
   - `GridView`
   - `SwipeInputController`
   - `LevelHudView`
   - optional `levelCompletePanel`

## Dictionary at scale
- Use `Tools -> HexWords -> Dictionary Importer` to import a large `.txt` or `.csv` word list.
- Supported input:
  - plain text list (one word per line)
  - csv with `word` column
- The tool normalizes words, deduplicates, filters by language and length, and writes into your source CSV.

## Avoiding repeated letters in cells
- In `GenerationProfile`, enable `avoidDuplicateLetters` (enabled by default).
- In `Tools -> HexWords -> Level Editor`, use:
  - `Generate Draft Cells From Profile` for new unique-letter draft
  - `Make Letters Unique` to fix duplicates in an existing level

## Full auto level generation
- Use `Tools -> HexWords -> Generate Levels (Auto)` to create many `LevelDefinition` assets in one click.
- Select `Dictionary`, `Generation Profile`, start id and count, then run generation.
- Generation v2 (`GreedyBeamV2`) adds a two-stage pipeline:
  - `Word-set optimization` (`Greedy + Beam`) using `GenerationObjective`
  - `Board placement` with strict DFS solvability validation
- V2 now retries target-word counts from `targetWordsMax` down to `targetWordsMin` when strict mode is enabled.
- Auto-generator logs failure reasons (`selectionFails`, `placementFails`, `solvabilityFails`) to help tuning.
- V2 has anti-freeze guards for large dictionaries: capped candidate pool, solver time budget, beam expansion cap, and cancelable progress bar during generation.
- For EN, auto generation is frequency-driven (`frequency_en.txt`) with automatic common-word filtering by rank and lexical heuristics.
- In EN mode, generator can run even if the Dictionary asset is empty/outdated (candidate source is frequency list).
- EN filters now reject calendar/time tokens and abbreviation-like noise automatically.
- Optional extra exclusions can still be added in `generator_blacklist_en.txt` if project-specific.
- Legacy generator remains available via profile (`generationAlgorithm=Legacy`) or window override.
- `useLegacyFallback` is hidden under advanced settings and is disabled by default for cleaner V2 output.

## CSV formats
### dictionary_ru.csv / dictionary_en.csv
Header:
`word,category,minLevel,maxLevel,difficultyBand`

### levels.csv
Header:
`levelId,language,validationMode,targetScore,targetWords`

`targetWords` uses `|` separator.

### level_cells.csv
Header:
`levelId,cellId,letter,q,r`

## Notes
- Runtime reads ScriptableObjects only.
- `Е` and `Ё` are treated as different letters.
- Preview-level selection stores selected asset in `RuntimePreviewConfig` under `Resources`.
- `GenerationProfile` now includes v2 controls: objective, hex budgets, beam/restarth counts, overlap/diversity weights, and strict solvability toggle.

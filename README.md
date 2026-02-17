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

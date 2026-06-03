# Project Architecture

This document summarizes the runtime architecture, game logic, scene flow, and script responsibilities observed in the current Unity project.

## Project Overview

The project is a Unity 6 chess-inspired puzzle game. The player controls a chess piece on a generated board, captures enemies, unlocks captured piece types as morph options, avoids enemy threat zones, and progresses through a ScriptableObject-driven campaign.

The architecture is centered around one gameplay scene controller:

- `GameController` builds each level, owns turn resolution, drives UI state, and coordinates visuals, audio, camera, scoring, victory, restart, and debug level editing.
- `ChessEngine` is a plain C# rules/model class that stores the board and calculates movement and threat maps.
- `LevelData` ScriptableObjects define board dimensions, active cells, starting pieces, alignments, and score thresholds.
- `CellView` and `PieceView` are lightweight MonoBehaviour view components for board and piece presentation.
- Menu, pause, cutscene, tween, and FMOD scripts provide scene flow and UI/audio support.

## Runtime Stack

- Unity editor version: `6000.4.4f1`
- Render pipeline: Universal Render Pipeline, configured through project graphics and quality settings.
- Input:
  - Core gameplay uses the legacy `Input` API directly for mouse, keyboard shortcuts, and camera tilt.
  - Pause UI uses the new Input System via `InputActionReference`.
  - `Assets/InputSystem_Actions.inputactions` appears to be the default Unity action asset with `Player` and `UI` maps.
- UI: uGUI plus TextMesh Pro.
- Tweening: DOTween from `Assets/Plugins/Demigiant/DOTween`.
- Audio: FMOD Unity integration from `Assets/Plugins/FMOD`.
- Persistence: `PlayerPrefs` for earned level stars and FMOD bus volumes.

## Build Scenes

The enabled scenes in `ProjectSettings/EditorBuildSettings.asset` are:

1. `Assets/Scenes/StartGame.unity`
2. `Assets/Scenes/GameScene.unity`
3. `Assets/Scenes/FinalSCene.unity`

Observed scene responsibilities:

- `StartGame` contains menu and campaign level selection. `MainMenuManager` creates level buttons and loads `GameScene`.
- `GameScene` contains `GameController`, board/camera/UI setup, pause/settings UI, inventory buttons, and victory UI.
- `FinalSCene` appears to be the final/cutscene or credits scene, driven by typewriter/video/next-scene helpers.

## High-Level Flow

```text
StartGame
  MainMenuManager reads CampaignLevels
  PlayerPrefs supplies earned stars per LevelData
  LevelSelectButtonUI loads GameScene with GameController.TargetStartLevelIndex

GameScene
  GameController selects the requested LevelData
  GameController creates ChessEngine(width, height)
  GameController instantiates CellView and PieceView objects from LevelData
  ChessEngine calculates legal moves and enemy threat maps
  Player selects/moves/morphs pieces through mouse and inventory UI
  Capturing enemy pieces increases score and can unlock morph types
  Capturing the enemy king opens victory UI and saves stars
  Last level can transition to the next build scene

FinalSCene
  Typewriter/video helpers advance to the next scene or menu
```

## Core Gameplay Model

### `ChessData.cs`

Defines shared gameplay types:

- `PieceType`: `None`, `Rook`, `Bishop`, `Knight`, `Queen`, `King`, `Pawn`.
- `Alignment`: `None`, `Player`, `Enemy`.
- `BoardCell`: runtime board state for one logical coordinate.

`BoardCell` stores:

- `Position`
- `IsActive`, used for walls, voids, or inactive board spaces.
- `CurrentPiece`
- `PieceAlignment`
- `AttackedBy`, a list of enemy positions currently threatening the cell.

`BoardCell` is logic-only and has no Unity GameObject responsibility.

### `ChessEngine.cs`

`ChessEngine` is a plain C# class, not a MonoBehaviour. It owns:

- A `BoardCell[,]` grid.
- Board `Width` and `Height`.
- Bounds and cell lookup helpers.
- Piece movement calculation.
- Enemy threat-map generation.
- Logical piece movement.

Movement logic supports:

- Rook and bishop line movement.
- Queen combined line movement.
- Knight offsets.
- King one-cell movement.
- Pawn forward movement and diagonal capture/threat behavior.

Important rule characteristics:

- Active/inactive cells are part of the rule model; inactive cells block movement.
- For normal line-piece movement, the engine tracks the last empty cell and adds it when a line hits a wall, own piece, or board edge. This gives the game a custom "slide to last valid cell" behavior rather than normal chess step-by-step destination selection.
- Threat-map calculation calls `GetValidMoves(position, true)` for every enemy piece and records attacker positions into threatened cells.
- Pawn threat behavior differs from pawn move behavior: pawns threaten diagonals but only move forward when not building a threat map.

`ChessEngine` does not instantiate, destroy, animate, play audio, save progress, or load scenes. Those concerns live in MonoBehaviours.

## Level Data

### `LevelData.cs`

Levels are ScriptableObjects created from `Create > ChessGame > Level`.

Data shape:

- `Width`
- `Height`
- `TargetScoreForStar`
- `Rows[]`
- `Rows[y].Columns[x]`
- Each cell has `IsActive`, `Piece`, and `Alignment`.

`OnValidate` keeps `Rows` and `Columns` sized to `Height` and `Width`, and initializes missing `CellSetup` objects. This keeps inspector-edited levels structurally valid.

### Current Campaign Assets

The campaign level assets are stored in `Assets/Levels`:

- `Level_01` through `Level_13`
- Board sizes range from narrow tutorial boards to larger layouts, including 8x8, 9x7, 10x7, and 6x10.
- Each level has a `TargetScoreForStar` threshold used by `GameController` scoring.

The actual campaign order is supplied by scene-assigned `CampaignLevels` lists in `MainMenuManager` and `GameController`.

## Gameplay Orchestration

### `GameController.cs`

`GameController` is the main gameplay composition root for `GameScene`.

It owns:

- Level loading and board clearing.
- Runtime `ChessEngine` instance.
- `CellView` instances keyed by `Vector2Int`.
- `PieceView` instances keyed by `Vector2Int`.
- Player selection and valid-move highlighting.
- Move execution, capture, enemy retaliation, restart, and victory.
- Player inventory and piece morphing.
- Score, stars, hints, PlayerPrefs progress, and victory UI.
- Camera focus through `CameraController`.
- FMOD calls through `AudioManager`.
- A runtime editor mode gated by `Tab`.

Level startup:

1. Choose level:
   - If `GameController.TargetStartLevelIndex` is valid, load that campaign index.
   - Else load the first campaign level.
   - Else fall back to `_currentLevel`.
2. Create a `ChessEngine` with the level dimensions.
3. Iterate level rows/columns.
4. Copy `CellSetup.IsActive` into `BoardCell.IsActive`.
5. Instantiate `CellView` using centered board positions.
6. Spawn pieces for populated active cells.
7. Build the enemy threat map.
8. Refresh visual threat state.
9. Cache starting inventory for restart.
10. Configure camera limits and focus.
11. Initialize stars and inventory UI.
12. Start FMOD level music and ambience after a short delay.

Player input:

- `Tab` toggles edit mode.
- `H` toggles threat hints.
- Left mouse selects player pieces and executes valid moves.
- Inventory UI calls `OnCardClicked(int)` to morph the current player piece or select an enemy brush in edit mode.

Move resolution:

1. Deselect the current piece and lock animation.
2. If the destination has an enemy, destroy the enemy view, add score, unlock the captured piece type, and update UI.
3. Move the piece in `ChessEngine`.
4. Recompute enemy threats.
5. Move the `PieceView` with DOTween.
6. If the captured piece was the enemy king, play victory sounds and show victory UI.
7. If the moved player piece ends on a threatened cell, the first listed attacker retaliates.
8. Retaliation moves the enemy into the player cell, destroys the player view, and restarts the level.

Victory and progress:

- Capturing the enemy king opens the victory panel.
- `PlayerPrefs` key format is `LevelProgress_{LevelData.name}`.
- The menu reads the same key to display earned stars.
- Star calculation is split between current in-level UI and victory UI:
  - No hint use contributes a star.
  - Reaching `TargetScoreForStar` contributes a star.
  - Victory UI currently adds one extra star when saving/displaying completion.

Edit mode:

- Toggled with `Tab`.
- Left-click removes existing pieces, places selected enemy pieces, disables active empty cells, or enables inactive cells.
- Right-click clears the selected enemy brush.
- In the editor, modified `LevelData` assets are marked dirty and saved through `EditorUtility.SetDirty` and `AssetDatabase.SaveAssets`.

## Visual Layer

### `CellView.cs`

`CellView` represents one rendered board cell.

Responsibilities:

- Store logical position.
- Apply textures through `MaterialPropertyBlock`.
- Track active/inactive visual state.
- Apply move, attack, hover, and threat textures.

The object stays active even when the logical cell is inactive. Inactive cells are shown by texture, not by disabling the GameObject.

### `PieceView.cs`

`PieceView` represents a rendered chess piece.

Responsibilities:

- Store `PieceType`, `Alignment`, and logical position.
- Animate world movement with DOTween `DOJump`.

It does not decide legal moves or game outcomes.

### `CameraController.cs`

`CameraController` follows the current player focus point.

Responsibilities:

- Configure camera bounds from board dimensions and cell size.
- Dynamically adjust camera height for larger boards.
- Clamp follow target within calculated board limits.
- Blend player focus toward board center.
- Support edge peeking based on mouse position.
- Support right-mouse camera tilt with automatic reset.
- Snap camera instantly after level load or restart.

## Menu and Scene Flow

### `MainMenuManager.cs`

`MainMenuManager` builds the level-select grid in `StartGame`.

Responsibilities:

- Read campaign levels.
- Read per-level stars from PlayerPrefs.
- Instantiate `LevelSelectButtonUI` buttons.
- Calculate total stars and rank text.
- Set `GameController.TargetStartLevelIndex`.
- Load `GameScene`.

### `LevelSelectButtonUI.cs`

Small view/controller for one level-select button.

Responsibilities:

- Show level number.
- Toggle filled star objects.
- Assign the click callback.

### `IntroSceneUI.cs`

Fades from an intro panel to the main menu panel using CanvasGroups and DOTween.

### `VideoEndToNextScene` in `UI/SceneManager.cs`

Listens for `VideoPlayer.loopPointReached` and loads either an override build index or the next build scene.

### `LoadSceneOnEnd.cs`

Finds a `TypeWritterEffect` and loads a hard-coded scene index when the typewriter finishes.

Note: this script currently uses deprecated `FindObjectOfType<T>(bool)` and hard-codes scene index `5`, while the current enabled build settings list contains three scenes. This is a maintenance risk unless additional scenes are enabled elsewhere before use.

## Pause, Settings, and UI Tweening

### `SceneManagerUI.cs`

Handles pause state through an `InputActionReference`.

Responsibilities:

- Lock/hide cursor during gameplay.
- Toggle pause state.
- Set `Time.timeScale` to `0` or `1`.
- Show/hide pause and settings pop windows.
- Start/stop the FMOD paused snapshot through `PauseAudioManager`.

### `UIPopWindow.cs`

Generic pop-window animation and scene-navigation helper.

Responsibilities:

- Scale UI panels in/out with DOTween.
- Continue, restart, start game, load main menu, and load next scene helpers.
- On restart, flush FMOD commands and stop all events on the root bus before reloading the active scene.

### Other UI helpers

- `UIButtonTween`: hover/click scale animations plus UI sounds through `AudioManager`.
- `InventorySlotUI`: locked/unlocked piece icon state and hover animation.
- `UIMenuParallax`: parallax rotation/position from mouse movement.
- `TypeWritterEffect`: sequential typewriter text, skip-on-input, optional sound, and completion flag.
- `TypeWritterRandom`: random single-message typewriter with public playback API and `Finished` event.
- `BollboardFollow`: rotates an object to match the main camera.

## Audio Architecture

The project has two audio layers:

### Static gameplay audio facade

`Assets/Scripts/ChessEngine/AudioManager.cs` is a static facade over FMOD event paths.

Responsibilities:

- One-shot gameplay sounds for pickup, placement, capture, morph, enemy attack, kill, win, and UI hover/click.
- Persistent FMOD event instances for level music and ambience.
- Music/ambience stop, volume, and parameter helpers.

Game systems call this facade instead of direct FMOD event paths in most gameplay cases.

### Scene/UI FMOD helpers

- `FMODButtonSound`: per-button click and hover sounds using serialized `EventReference`s.
- `FMODVolumeSlider`: controls serialized FMOD bus volume and persists values in PlayerPrefs with key prefix `FMODVolume_`.
- `MenuMusicStarter`: persists across scenes, waits for banks to load, and starts menu emitters.
- `PauseAudioManager`: static paused snapshot lifecycle for `snapshot:/Paused`.
- `BandPerformanceManager`: destroys the tagged menu music object, then loops random speaking and music FMOD events in sequence.

## Data and State Ownership

```text
LevelData asset
  Authoritative serialized level layout and score threshold.

ChessEngine
  Authoritative runtime board state, movement rules, and threat map.

GameController
  Runtime composition and gameplay state:
  selected piece, view dictionaries, score, hints, inventory,
  current campaign index, animations, victory/restart flow.

CellView / PieceView
  Visual representation of current runtime state.

PlayerPrefs
  Cross-session progression and audio volume persistence.
```

The code generally follows a Model/View/Controller split for board gameplay:

- Model: `ChessEngine`, `BoardCell`, `LevelData`
- View: `CellView`, `PieceView`, UI prefabs
- Controller/composition: `GameController`, menu and pause controllers

The split is pragmatic rather than strict. `GameController` currently handles many responsibilities that could be separated later if gameplay complexity grows.

## Key Dependencies

- `GameController` depends on:
  - `ChessEngine`, `BoardCell`, `LevelData`
  - `CellView`, `PieceView`
  - `InventorySlotUI`
  - `CameraController`
  - `AudioManager`
  - DOTween
  - Unity scene management and PlayerPrefs

- `ChessEngine` depends on:
  - `UnityEngine.Vector2Int`
  - Shared enums and `BoardCell`

- UI scripts depend on:
  - uGUI
  - TextMesh Pro
  - DOTween
  - Input System for pause/menu parallax in selected scripts

- Audio scripts depend on:
  - FMOD Unity integration
  - FMOD event/bus paths configured in banks and inspector references

## Current Extension Points

Adding a level:

1. Create a `LevelData` asset from `ChessGame/Level`.
2. Set width, height, score threshold, cell activity, pieces, and alignments.
3. Add it to the campaign lists on `MainMenuManager` and `GameController`.

Adding a piece type:

1. Add the value to `PieceType`.
2. Add movement logic in `ChessEngine.GetValidMoves`.
3. Add player/enemy prefabs and fields or a data-driven prefab lookup in `GameController`.
4. Add inventory UI support if the player can morph into it.
5. Update level data and any UI labels/assets.

Changing scoring:

- Update `GameController.PointsPerCapture`.
- Update `CalculateEarnedStars` and victory star save/display logic if star conditions change.
- Existing progress keys are based on level asset names.

Changing audio:

- Prefer adding gameplay event wrappers to `AudioManager`.
- Use serialized `EventReference`s for reusable UI button sounds where scene designers need per-button control.
- Confirm FMOD banks contain matching event paths.

## Maintenance Notes

- `GameController` is the largest class and mixes several responsibilities: level composition, rules orchestration, UI, persistence, edit mode, audio, and camera. Future changes will be safer if new systems are extracted around inventory, scoring/progress, level editing, and victory flow.
- `GameController.TargetStartLevelIndex` is static cross-scene state. It is simple and works for level select, but it should be reset deliberately if other entry paths are added.
- The board logic is partly decoupled from Unity objects, which makes `ChessEngine` a good candidate for edit-mode or play-mode unit tests.
- `LoadSceneOnEnd` hard-codes scene index `5`, which does not match the current three enabled build scenes.
- `LoadSceneOnEnd` uses deprecated `FindObjectOfType<T>(bool)`; Unity recommends `FindFirstObjectByType` or `FindAnyObjectByType`.
- `UIPopWindow` contains a static paused snapshot field that overlaps with `PauseAudioManager`. The active pause flow uses `PauseAudioManager`, so this duplication should be treated carefully.
- `UIPopWindow.StartGame()` and `LoadMainMenu()` load build index `0`; this currently maps to `StartGame`.
- Scene and file naming have minor inconsistencies, such as `FinalSCene`, `BollboardFollow`, and `TypeWritter`.
- Several scripts use direct string FMOD event paths and scene names/indexes. These are convenient but can break silently when banks or build settings change.
- Current git status shows unrelated modified assets and package files. This document does not account for uncommitted future changes beyond the files inspected.

## Testing Opportunities

High-value automated tests would target:

- `ChessEngine.GetValidMoves` for all piece types, inactive cells, allied blockers, enemy captures, and threat-map mode.
- `ChessEngine.UpdateThreatMap` for pawn diagonals, line-piece blockers, and multiple attackers.
- `LevelData.OnValidate` resizing behavior.
- `GameController` progression save/load semantics around `PlayerPrefs` keys.
- Victory star calculation, especially the current extra victory star.


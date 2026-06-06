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
- Post-processing:
  - `GameScene` has URP camera post-processing enabled on the main camera.
  - `GameScene` uses a global `Volume` with `Assets/Settings/SampleSceneProfile.asset`.
  - Bloom is active in that profile and is used by HDR-bright transformation sparkles.
- Input:
  - Core gameplay uses the legacy `Input` API directly for mouse, keyboard shortcuts, and camera tilt.
  - Pause UI uses the new Input System via `InputActionReference`.
  - `Assets/InputSystem_Actions.inputactions` appears to be the default Unity action asset with `Player` and `UI` maps.
- UI: uGUI plus TextMesh Pro.
- Localization: Unity Localization package (`com.unity.localization`) with Addressables-backed string tables.
- Tweening: DOTween from `Assets/Plugins/Demigiant/DOTween`.
- Audio: FMOD Unity integration from `Assets/Plugins/FMOD`.
- Persistence: `PlayerPrefs` for earned level stars, selected locale, and FMOD bus volumes.

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
  GameLocalization applies saved locale from PlayerPrefs after localization initialization
  MainMenuManager reads CampaignLevels
  PlayerPrefs supplies earned stars per LevelData
  MainMenuManager resolves localized rank text through the Table1 string table
  LevelSelectButtonUI requests GameScene through SceneTransitionManager
  GameController.TargetStartLevelIndex carries the chosen campaign index

GameScene
  GameController selects the requested LevelData
  GameController creates ChessEngine(width, height)
  GameController instantiates CellView and PieceView objects from LevelData
  ChessEngine calculates legal moves and enemy threat maps
  Player selects/moves/morphs pieces through mouse and inventory UI
  Morphing plays PieceTransformationVfx and swaps the PieceView with DOTween scale animation
  Capture feedback plays CaptureFeedbackVfx, camera impulse, and enemy pop/shrink animation
  Capturing enemy pieces increases score and can unlock morph types
  Capturing the enemy king opens localized victory UI and saves stars
  Last level can transition to the next build scene through SceneTransitionManager

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

Piece morphing:

1. `SwapPlayerPiece` finds the current player piece and target `PieceType`.
2. The current `BoardCell.CurrentPiece` is updated in the `ChessEngine`.
3. `_morphParticlePrefab` is instantiated slightly above the player cell.
4. If the spawned prefab has `PieceTransformationVfx`, `Play()` is called immediately.
5. `AudioManager.PlayChangeSound` plays the morph sound at the cell position.
6. The old `PieceView` is killed with DOTween, briefly bumps, then scales to zero.
7. The replacement `PieceView` is instantiated at zero scale, overshoots, and settles to its prefab scale.
8. The piece view dictionary is updated and camera focus is refreshed.

Move resolution:

1. Deselect the current piece and lock animation.
2. If the destination has an enemy, remove its view from the runtime lookup, add score, unlock the captured piece type, and update UI.
3. Play capture feedback immediately: editable `CaptureFeedbackVfx` prefab when assigned, runtime fallback otherwise, captured-enemy pop/shrink, and camera impulse.
4. Move the piece in `ChessEngine`.
5. Recompute enemy threats.
6. Move the `PieceView` with DOTween.
7. Play the capture or place sound once the moving piece lands.
8. If the captured piece was the enemy king, play victory sounds and show victory UI.
9. If the moved player piece ends on a threatened cell, the first listed attacker retaliates.
10. Retaliation moves the enemy into the player cell, destroys the player view, and restarts the level.

Victory and progress:

- Capturing the enemy king opens the victory panel.
- `PlayerPrefs` key format is `LevelProgress_{LevelData.name}`.
- The menu reads the same key to display earned stars.
- Victory progress text is localized through `GameLocalization.GetStringAsync`.
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
- Store a base world position used by movement and camera logic even while hover lift is active.
- Apply textures through `MaterialPropertyBlock`.
- Track active/inactive visual state.
- Apply move, attack, hover, and threat textures.
- Animate valid destination hover and invalid-click feedback.
- Lift the hovered cell upward and return it to its base position when hover leaves.

The object stays active even when the logical cell is inactive. Inactive cells are shown by texture, not by disabling the GameObject.

### `PieceView.cs`

`PieceView` represents a rendered chess piece.

Responsibilities:

- Store `PieceType`, `Alignment`, and logical position.
- Apply selectable-hover scale/tint feedback through DOTween and `MaterialPropertyBlock`.
- Animate world movement with DOTween `DOJump`.

It does not decide legal moves or game outcomes.

### `CaptureFeedbackVfx.cs`

`CaptureFeedbackVfx` is the capture effect component used by the prefab assigned to `GameController._captureFeedbackPrefab`.

The current editable prefab is `Assets/Prefabs/VFX/CaptureFeedbackVfx.prefab`. It is assigned in `GameScene` and contains child particle systems that can be tuned directly in the Unity editor:

- `Impact Flash`
- `Impact Ring`
- `Burst Shards`
- `Ring Shards`
- `Heavy Dust Shards`
- `Lingering Glints`

Supporting generated assets live in `Assets/Prefabs/VFX`:

- `CaptureFeedback_MeshParticle.mat`
- `CaptureFeedback_SparkMesh.asset`
- `CaptureFeedback_RingMesh.asset`

Responsibilities:

- Cache and play editable child `ParticleSystem` objects.
- Optionally build a runtime fallback effect if no child particle systems exist.
- Apply `_heightOffset` and optional intensity-based scale.
- Self-destroy after `_destroyAfter`.

The component intentionally lives in its own script file so Unity prefab script references remain stable after domain reloads and Play Mode edits.

### `PieceTransformationVfx.cs`

`PieceTransformationVfx` is the reusable transformation effect attached to the particle prefab assigned to `GameController._morphParticlePrefab`.

Responsibilities:

- Build child `ParticleSystem` objects at runtime.
- Play the effect on spawn or from an explicit `Play()` call.
- Stop, clear, play, and immediately emit each child system so the burst is visible on the transformation frame.
- Optionally self-destroy after `_destroyAfter`.
- Disable the prefab root particle system when `_disableExistingRootParticleSystem` is enabled, so only the scripted effect plays.

Current generated systems:

- `Sparkle Burst`: main outward star burst.
- `Shooting Glitters`: fast directional sparkle streaks.
- `Core Flash`: short center flash.
- `Arcane Ring`: colored radial burst.
- `Star Pop`: bigger accent sparkles.
- `Twinkle Sparkles`: slower lingering sparkles.
- `Rising Sparks`: upward trailing sparks.
- `Soft Smoke`: optional smoke layer, currently disabled on the prefab.

The effect uses local simulation space and generated runtime meshes/materials. Sparkle systems render as small 3D star-shard meshes instead of relying only on billboard particles, which keeps the effect readable from the angled gameplay camera. Runtime materials use unlit shaders with HDR color values controlled by `_hdrGlowIntensity`.

Prefab tuning fields:

- `_sparkleIntensity`: scales particle counts.
- `_hdrGlowIntensity`: multiplies particle RGB values above 1.0 for Bloom.
- `_radius`: controls effect spread.
- `_heightOffset`: controls vertical placement above the board cell.
- `_useSmoke`: toggles the optional smoke system.
- `_showVisibilityFlash`: diagnostic fallback that shows a solid diamond/ring flash if particle visibility needs debugging.
- `_logPlayback`: diagnostic log for spawn/playback confirmation.

### Post-Processing and HDR Glow

Transformation glow is a two-part setup:

- `PieceTransformationVfx` outputs HDR-bright particle colors.
- The `GameScene` main camera renders post-processing and the global volume profile enables Bloom.

The active profile is `Assets/Settings/SampleSceneProfile.asset`. Its Bloom settings are intentionally moderate so the sparkle VFX glows without washing out the whole scene. High-quality Bloom filtering is disabled for a cheaper pass, which is relevant for WebGL performance.

Bloom affects all bright HDR content in the scene. If future materials use emission values above 1.0, they can also bloom.

### `CameraController.cs`

`CameraController` follows the current player focus point and supports short gameplay impulses.

Responsibilities:

- Configure camera bounds from board dimensions and cell size.
- Dynamically adjust camera height for larger boards.
- Clamp follow target within calculated board limits.
- Blend player focus toward board center.
- Support edge peeking based on mouse position.
- Support right-mouse camera tilt with automatic reset.
- Apply a damped impulse offset through `AddImpulse(Vector3 worldPosition, float strength)`, currently used by capture feedback.
- Snap camera instantly after level load or restart.

## Menu and Scene Flow

### `SceneTransitionManager.cs`

`SceneTransitionManager` is the reusable scene-loading facade. It is a persistent singleton and should be used instead of direct `SceneManager.LoadScene` calls for normal scene changes.

Responsibilities:

- Expose static `LoadScene(string)`, `LoadScene(int)`, and `ReloadActiveScene()` helpers.
- Create itself if no scene instance exists.
- Persist across scenes with `DontDestroyOnLoad`.
- Destroy duplicate transition manager components when another scene also contains one.
- Build and own a child `SceneTransitionView`.
- Block input while the transition overlay is active.
- Load scenes asynchronously with `allowSceneActivation = false`.
- Keep the old scene covered until the cover animation, load operation, and minimum covered duration are complete.
- Reveal after the new scene is activated.
- Emit transition phases through both static `PhaseChanged` and inspector `UnityEvent`s.

Transition phases:

- `CoverStarted`
- `CoverCompleted`
- `SceneActivationStarted`
- `SceneActivated`
- `RevealStarted`
- `RevealCompleted`

The default config is assigned on the manager when present. If no config is assigned, the manager creates a temporary runtime default `SceneTransitionConfig`.

### `SceneTransitionConfig.cs`

`SceneTransitionConfig` is a ScriptableObject-driven transition preset.

It controls:

- Overlay color and optional sprite.
- Whether the sprite preserves aspect ratio.
- Optional transition material.
- The shader progress property name, normally `_Progress`.
- Cover and reveal durations.
- Minimum fully-covered duration.
- Whether async loading starts during the cover animation.
- Scaled vs unscaled time.
- Cover/reveal animation curves.
- Overlay canvas sorting order.

`Assets/Scripts/SceneTransitions/TransitionConfig1.asset` is the current project transition preset.

### `SceneTransitionView.cs`

`SceneTransitionView` renders the transition overlay as a screen-space uGUI canvas.

Responsibilities:

- Create a full-screen `Image` with raycast blocking.
- Apply color, sprite, preserve-aspect, sorting order, and optional material from `SceneTransitionConfig`.
- If no material is present, animate `CanvasGroup.alpha`.
- If a material is present, animate the configured progress property.
- Clone the configured transition material at runtime so scene loads do not mutate the source asset.
- Generate a Perlin noise texture when the transition material needs `_NoiseTex` and none is assigned.

### Dissolve Transition Shader

`Assets/Shaders/SceneTransitionDissolve.shader` implements the current UI dissolve transition.

Important properties:

- `_Progress`: cover/reveal progress.
- `_NoiseTex`: dissolve noise map, generated by `SceneTransitionView` if missing.
- `_BurnDirection`: direction of the paper-burn wipe.
- `_NoiseScale` and `_NoiseStrength`: breakup pattern.
- `_Softness`: dissolve edge softness.
- `_EdgeColor` and `_EdgeWidth`: colored burn edge.

The shader is authored for uGUI transparent rendering and is driven by `SceneTransitionView`.

### `SceneTransitionFmodAudio.cs`

`SceneTransitionFmodAudio` is an optional FMOD adapter that listens to `SceneTransitionManager.PhaseChanged`.

It exposes one serialized FMOD `EventReference` per transition phase and plays each non-null event with `RuntimeManager.PlayOneShot`.

### `MainMenuManager.cs`

`MainMenuManager` builds the level-select grid in `StartGame`.

Responsibilities:

- Read campaign levels.
- Read per-level stars from PlayerPrefs.
- Instantiate `LevelSelectButtonUI` buttons.
- Calculate total stars and localized rank text.
- Set `GameController.TargetStartLevelIndex`.
- Load `GameScene` through `SceneTransitionManager`.

`MainMenuManager` listens to `LocalizationSettings.SelectedLocaleChanged` and refreshes the rank label when the selected language changes. Rank text uses async string lookups through `GameLocalization.GetStringAsync` with request-version guards so stale WebGL localization loads cannot overwrite newer locale selections.

### `LevelSelectButtonUI.cs`

Small view/controller for one level-select button.

Responsibilities:

- Show level number.
- Toggle filled star objects.
- Assign the click callback.

### `IntroSceneUI.cs`

Fades from an intro panel to the main menu panel using CanvasGroups and DOTween.

### `VideoEndToNextScene` in `UI/SceneManager.cs`

Listens for `VideoPlayer.loopPointReached` and requests either an override build index or the next build scene through `SceneTransitionManager`.

### `LoadSceneOnEnd.cs`

Finds a `TypeWritterEffect` and requests a hard-coded scene index through `SceneTransitionManager` when the typewriter finishes.

Note: this script currently uses deprecated `FindObjectOfType<T>(bool)` and hard-codes scene index `5`, while the current enabled build settings list contains three scenes. This is a maintenance risk unless additional scenes are enabled elsewhere before use.

## Localization Architecture

The project currently supports two locales:

- English: `en`
- Russian: `ru`

Localization assets live under `Assets/Localizations`:

- `Assets/Localizations/Localization Settings.asset`
- `Assets/Localizations/Locals/English (en).asset`
- `Assets/Localizations/Locals/Russian (ru).asset`
- `Assets/Localizations/Locals/Table1 Shared Data.asset`
- `Assets/Localizations/Locals/Table1_en.asset`
- `Assets/Localizations/Locals/Table1_ru.asset`

The active string table collection is named `Table1`. Current keys include:

- UI labels: `ui.start`, `ui.settings`, `ui.restart`, `ui.menu`, `ui.victory`
- Rank labels: `rank.jester`, `rank.debutant`, `rank.castling_fan`, `rank.pawn_grandmaster`, `rank.king_of_metamorphoses`, `rank.lavender_raf`
- Victory/progress labels: `state.game_complete`, `state.level`, `state.from`

### `GameLocalization.cs`

`GameLocalization` is a static helper for script-driven localization.

Responsibilities:

- Store the shared string table name, currently `Table1`.
- Persist the selected locale in `PlayerPrefs` under `Game.Locale`.
- Apply the saved locale on startup after `LocalizationSettings.InitializationOperation` is ready.
- Switch locale by code through `SetLocale("en")` and `SetLocale("ru")`.
- Provide `GetStringAsync(key, fallback, callback)` for runtime-generated text.
- Provide a synchronous `GetString` fallback for cases where synchronous lookup is acceptable.

The async lookup path is important for WebGL, where localization tables can load later than scene scripts. Dynamic labels that depend on script logic should prefer `GetStringAsync`.

### `LanguageSelector.cs`

`LanguageSelector` is a button-facing MonoBehaviour.

Public UI methods:

- `SetEnglish()`
- `SetRussian()`
- `ToggleLanguage()`
- `SetLanguage(string localeCode)`

It delegates all actual locale switching to `GameLocalization.SetLocale`. Button objects can call these methods from inspector-assigned `OnClick` events while keeping their existing scene-flow callbacks, such as advancing from a language choice screen.

### Static and Dynamic Text Split

Scene-authored labels should use Unity's `LocalizeStringEvent` component directly on the TextMesh Pro text object. This is the preferred path for button labels, menu labels, and other static UI text assigned in the editor.

Script-generated labels should use `GameLocalization.GetStringAsync`. Current script-driven localized text includes:

- Main menu rank labels in `MainMenuManager`.
- Victory progress text in `GameController`.

Both `MainMenuManager` and `GameController` use integer request-version counters before assigning async results. This prevents a slow lookup from a previous locale from overwriting text after the player switches language again.

## Pause, Settings, and UI Tweening

### `CustomCursorManager.cs`

`CustomCursorManager` is the project-level software cursor system.

Responsibilities:

- Persist across scenes with `DontDestroyOnLoad`.
- Enforce one active instance; duplicate scene components destroy only themselves.
- Hide the OS cursor and render a uGUI image cursor in a screen-space overlay canvas.
- Keep the cursor canvas at sorting order `32700`, below scene transitions but above normal UI.
- Reset cached `EventSystem` pointer data on scene load.
- Switch between default and interactable sprites.
- Detect interactable UI through `EventSystem.RaycastAll`.
- Detect interactable world objects through 3D and optional 2D raycasts.
- Force the cursor unlocked/visible as a software cursor when `_forceVisibleAndUnlocked` is enabled.

Default editor-assigned sprites:

- `Assets/Sprites/Cursors/gauntlet_default.png`
- `Assets/Sprites/Cursors/gauntlet_point.png`

Default hover layer assumptions:

- UI hover layer: `UI`
- World hover layers: `BoardLayer`, `CellLayer`

When `_onlyInteractiveUiElements` is true, UI hits must have an interactable `Selectable` or pointer handler before the cursor changes to the interactable state.

### `SceneManagerUI.cs`

Handles pause state through an `InputActionReference`.

Responsibilities:

- Lock/hide cursor during gameplay.
- Toggle pause state.
- Set `Time.timeScale` to `0` or `1`.
- Show/hide pause and settings pop windows.
- Start/stop the FMOD paused snapshot through `PauseAudioManager`.

Note: the pause scripts still use Unity's OS cursor APIs. `CustomCursorManager` then hides the OS cursor and draws the software cursor in `LateUpdate`, so cursor-lock behavior should be tested when changing pause or settings flows.

### `UIPopWindow.cs`

Generic pop-window animation and scene-navigation helper.

Responsibilities:

- Scale UI panels in/out with DOTween.
- Continue, restart, start game, load main menu, and load next scene helpers.
- Route scene changes through `SceneTransitionManager`.
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
  Cross-session progression, selected locale, and audio volume persistence.

LocalizationSettings / String Tables
  Authoritative localized UI strings for English and Russian.
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
  - `CaptureFeedbackVfx`
  - `PieceTransformationVfx`
  - `InventorySlotUI`
  - `CameraController`
  - `AudioManager`
  - `GameLocalization`
  - DOTween
  - Unity scene management, Unity Localization, and PlayerPrefs

- `ChessEngine` depends on:
  - `UnityEngine.Vector2Int`
  - Shared enums and `BoardCell`

- UI scripts depend on:
  - uGUI
  - TextMesh Pro
  - Unity Localization for `LocalizeStringEvent`, `LocalizationSettings`, and string tables
  - DOTween
  - Input System for pause/menu parallax in selected scripts

- Audio scripts depend on:
  - FMOD Unity integration
  - FMOD event/bus paths configured in banks and inspector references

- Scene transition scripts depend on:
  - Unity scene management
  - uGUI canvas, image, canvas group, and graphic raycaster
  - Optional transition materials/shaders
  - Optional FMOD event references through `SceneTransitionFmodAudio`

- Custom cursor depends on:
  - uGUI and `EventSystem`
  - `Selectable` and pointer event interfaces for UI hover detection
  - 3D/2D physics raycasts for world hover detection
  - Scene load callbacks to refresh cached event-system state

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

Changing scene transitions:

1. Create or edit a `SceneTransitionConfig` asset.
2. Assign overlay color/sprite and timing.
3. Assign a transition material if a shader-driven effect is needed.
4. Confirm the material exposes the configured progress property, normally `_Progress`.
5. Assign the config to the scene's `SceneTransitionManager` or pass it to `SceneTransitionManager.LoadScene`.
6. Use `SceneTransitionFmodAudio` or manager phase events for synchronized transition sounds.

Changing the transformation VFX:

1. Edit the `PieceTransformationVfx` component on `Assets/Prefabs/Particle System.prefab`.
2. Use `_sparkleIntensity` for particle count.
3. Use `_hdrGlowIntensity` for Bloom strength.
4. Use `_radius` and `_heightOffset` for shape and placement.
5. Keep `_showVisibilityFlash` off for normal gameplay; enable it only when debugging visibility.
6. Tune Bloom in `Assets/Settings/SampleSceneProfile.asset` if glow strength should change globally.

Changing the capture VFX:

1. Edit `Assets/Prefabs/VFX/CaptureFeedbackVfx.prefab`.
2. Tune the child particle systems directly in the prefab for shape, counts, colors, materials, and lifetimes.
3. Use the `CaptureFeedbackVfx` component fields for `_destroyAfter`, `_heightOffset`, `_radius`, fallback behavior, and intensity scaling.
4. Adjust `GameController` capture feedback fields for global intensity, enemy pop/shrink timing, and camera impulse.
5. Keep the `CaptureFeedbackVfx` script reference intact on the prefab; missing script references will still allow fallback code in some cases but should be treated as prefab corruption.

Changing custom cursor behavior:

1. Edit the `CustomCursorManager` scene object or prefab instance.
2. Assign default/interactable sprites and hotspots.
3. Add UI layers to `_uiInteractableLayers`.
4. Add board/piece/cell layers to `_worldInteractableLayers`.
5. Keep `_onlyInteractiveUiElements` enabled when decorative UI graphics should not trigger the interactable cursor.
6. Test pause/settings transitions because cursor lock/unlock is shared with pause UI scripts.

Adding localized text:

1. Add the key to `Assets/Localizations/Locals/Table1 Shared Data.asset` through Unity's Localization Tables window.
2. Fill English and Russian values in `Table1_en` and `Table1_ru`.
3. For static scene text, add or configure `LocalizeStringEvent` on the TextMesh Pro text component.
4. For script-generated text, call `GameLocalization.GetStringAsync(key, fallback, callback)`.
5. For dynamic text assembled from multiple pieces, prefer adding a single formatted localization key later rather than concatenating translated fragments.

Adding a language:

1. Create a new Locale asset.
2. Add a matching `Table1_<locale>.asset` string table.
3. Add a button or selector option that calls `LanguageSelector.SetLanguage(localeCode)`.
4. Confirm the TextMesh Pro font assets include glyphs for the new language.
5. Verify WebGL builds because localization table loading is asynchronous there.

## Maintenance Notes

- `GameController` is the largest class and mixes several responsibilities: level composition, rules orchestration, UI, persistence, edit mode, audio, and camera. Future changes will be safer if new systems are extracted around inventory, scoring/progress, level editing, and victory flow.
- `GameController.TargetStartLevelIndex` is static cross-scene state. It is simple and works for level select, but it should be reset deliberately if other entry paths are added.
- `SceneTransitionManager` is also static and persistent. Keep duplicate scene instances lightweight, because duplicate manager components destroy themselves during `Awake`.
- Scene-loading scripts should prefer `SceneTransitionManager.LoadScene` over direct `SceneManager.LoadScene` so future transition/audio behavior stays centralized.
- The board logic is partly decoupled from Unity objects, which makes `ChessEngine` a good candidate for edit-mode or play-mode unit tests.
- `LoadSceneOnEnd` hard-codes scene index `5`, which does not match the current three enabled build scenes.
- `LoadSceneOnEnd` uses deprecated `FindObjectOfType<T>(bool)`; Unity recommends `FindFirstObjectByType` or `FindAnyObjectByType`.
- `UIPopWindow` contains a static paused snapshot field that overlaps with `PauseAudioManager`. The active pause flow uses `PauseAudioManager`, so this duplication should be treated carefully.
- `UIPopWindow.StartGame()` and `LoadMainMenu()` load build index `0`; this currently maps to `StartGame`.
- Scene and file naming have minor inconsistencies, such as `FinalSCene`, `BollboardFollow`, and `TypeWritter`.
- Several scripts use direct string FMOD event paths and scene names/indexes. These are convenient but can break silently when banks or build settings change.
- Bloom is now active in the gameplay volume profile. This improves HDR VFX, but any future HDR/emissive materials may also bloom.
- WebGL performance should be checked after Bloom/VFX changes. The current Bloom setup disables high-quality filtering to keep the pass cheaper.
- `CaptureFeedbackVfx` must stay as a standalone script asset while the prefab references it. Moving the class back into another file can break the serialized `m_Script` reference and cause Unity missing-script console errors.
- Capture feedback currently instantiates and destroys one prefab per capture. That is fine for current gameplay frequency, but pooling should be considered if capture density increases.
- `CustomCursorManager` forces the software cursor visible/unlocked in `LateUpdate`; this can conflict with future gameplay modes that need a locked hardware cursor.
- Script-driven localization should avoid synchronous lookups in WebGL-facing UI. Use `GameLocalization.GetStringAsync` and guard against stale callbacks when locale changes can happen while a lookup is in flight.
- Some localized dynamic text is currently assembled from multiple keys, such as `state.level` + number + `state.from` + count. This works for English/Russian now, but a single smart/formatted string key would scale better for languages with different word order.
- Current git status shows unrelated modified assets and package files. This document does not account for uncommitted future changes beyond the files inspected.

## Testing Opportunities

High-value automated tests would target:

- `ChessEngine.GetValidMoves` for all piece types, inactive cells, allied blockers, enemy captures, and threat-map mode.
- `ChessEngine.UpdateThreatMap` for pawn diagonals, line-piece blockers, and multiple attackers.
- `LevelData.OnValidate` resizing behavior.
- `GameController` progression save/load semantics around `PlayerPrefs` keys.
- Victory star calculation, especially the current extra victory star.
- `GameController.SwapPlayerPiece` spawning and playing `PieceTransformationVfx`.
- Capture feedback prefab assignment and script-reference stability after Play Mode reloads or prefab edits.
- `GameController` capture flow: score/unlock update, VFX spawn, enemy pop/shrink destroy, capture sound after landing, and camera impulse.
- Scene transition phase ordering, input blocking, async load activation, and duplicate manager behavior.
- Custom cursor UI/world hover detection across scene changes and pause/settings flows.
- Locale switching in StartGame, GameScene victory UI, and WebGL builds.
- `GameLocalization` saved-locale startup behavior and fallback behavior for missing keys.

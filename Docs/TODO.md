# Project TODO

This backlog focuses on polish, stability, performance, and architecture improvements based on the current project state.

## Priority 1: Highest Impact Polish

Status: move/selection/capture impact polish is complete for the current pass. Transformation polish remains valid follow-up work, but was deferred after this session.

### Move and Selection Feel

- [x] Add stronger hover feedback for selectable player pieces.
  - Goal: the player should immediately understand which piece can be selected.
  - Suggested implementation: subtle scale pulse, rim highlight, or material tint through `PieceView`.

- [x] Add clear hover feedback for valid destination cells.
  - Goal: make legal moves easier to read before clicking.
  - Suggested implementation: animated cell texture, soft pulse, or small marker above the cell.

- [x] Add feedback for invalid clicks.
  - Goal: reduce confusion when a player clicks an inactive cell, blocked cell, or unavailable piece.
  - Suggested implementation: short UI/board sound, quick red flash on the cell, or cursor shake.

- [x] Replace the ghost move preview with hover-lift feedback.
  - Goal: show which board cell is currently under the pointer without adding an extra ghost object.
  - Implemented: hovered cells lift upward and return to their base position when hover leaves; if a piece is on the hovered cell, the piece lifts with the cell.
  - Note: the ghost preview was removed after playtesting because it did not fit the game's look.

- [x] Improve capture feedback.
  - Goal: enemy captures should feel more satisfying and readable.
  - Implemented: editable `CaptureFeedbackVfx` prefab, camera impulse, existing capture sound on landing, and captured-enemy pop/shrink animation.

### Deferred Transformation Polish

- [ ] Add piece-type color presets for transformation VFX.
  - Goal: morphing into different pieces should feel distinct.
  - Example: Queen purple/gold, Knight blue/white, Bishop violet, Rook orange, Pawn green.

- [ ] Sync transformation VFX timing with `AudioManager.PlayChangeSound`.
  - Goal: sound peak and sparkle burst should land together.
  - Suggested implementation: tune VFX burst frame, sound event timing, or add FMOD parameter support.

- [ ] Add optional stronger transformation preset for important unlocks.
  - Goal: first-time piece unlocks can feel more special than normal morphs.
  - Suggested implementation: expose a stronger preset or multiplier on `PieceTransformationVfx`.

- [ ] Pool transformation VFX instances.
  - Goal: avoid repeated instantiate/destroy during frequent morphing.
  - Suggested implementation: simple object pool keyed by prefab or a small `VfxPool` service.

## Priority 2: UI and Scene Flow

### Victory and Progress UI

- [ ] Animate victory stars one by one.
  - Goal: make reward feedback clearer and more satisfying.
  - Suggested implementation: DOTween scale-in sequence with small delays and star sounds.

- [ ] Add a clearer score/star progress display during gameplay.
  - Goal: the player should know what is needed for extra stars.
  - Suggested implementation: progress text, score meter, or target score marker.

- [ ] Improve locked/unlocked level button states.
  - Goal: level select should communicate progress more clearly.
  - Suggested implementation: stronger locked visual, hover tooltip, star requirement display.

- [ ] Add language-change feedback.
  - Goal: changing language should feel instant and reliable.
  - Suggested implementation: brief UI pulse, selected-language checkmark, or localized "applied" label.

### Scene Transitions

- [ ] Create named transition presets.
  - Goal: reuse different transition styles for menu, gameplay, victory, and restart.
  - Suggested assets: `MenuTransition`, `LevelLoadTransition`, `VictoryTransition`, `FastRestartTransition`.

- [ ] Confirm every scene load path uses `SceneTransitionManager`.
  - Goal: avoid hard scene cuts and keep audio/transition behavior centralized.
  - Audit targets: menu buttons, pause UI, final scene helpers, typewriter/video helpers.

- [ ] Add optional loading diagnostics for WebGL.
  - Goal: easier debugging when scene loading stalls or feels slow.
  - Suggested implementation: editor-only or development-build logs for transition phase durations.

- [ ] Tune dissolve shader presets.
  - Goal: make the paper-burn transition more intentional.
  - Suggested tuning: `_NoiseScale`, `_NoiseStrength`, `_Softness`, `_EdgeColor`, `_EdgeWidth`, `_BurnDirection`.

## Priority 3: Performance and WebGL

### VFX Performance

- [ ] Add a WebGL-lower particle setting.
  - Goal: keep the transformation effect affordable in browser builds.
  - Suggested implementation: reduce `_sparkleIntensity`, `_hdrGlowIntensity`, and trail ratio on WebGL.

- [ ] Profile Bloom cost in WebGL.
  - Goal: verify HDR sparkle glow does not hurt browser performance too much.
  - Suggested implementation: compare builds with Bloom on/off and high-quality filtering off/on.

- [ ] Avoid runtime material/mesh creation per effect instance if VFX usage grows.
  - Goal: reduce allocation spikes.
  - Suggested implementation: cache shared generated meshes/materials statically or pool VFX objects.

- [ ] Pool capture VFX prefab instances if capture density increases.
  - Goal: avoid instantiate/destroy churn if later levels create frequent captures.
  - Current state: capture feedback instantiates `Assets/Prefabs/VFX/CaptureFeedbackVfx.prefab` per capture and self-destroys.

### Quality Profiles

- [ ] Separate PC and WebGL quality assumptions more explicitly.
  - Goal: make builds look consistent while respecting platform limits.
  - Suggested implementation: PC keeps stronger Bloom/particles; WebGL lowers shadows, Bloom, texture limits, and particles.

- [ ] Re-check WebGL resolution and render scale.
  - Goal: avoid the browser build looking lower resolution than Windows.
  - Audit targets: URP render scale, quality level selection, canvas scaler behavior, WebGL template sizing.

- [ ] Audit expensive UI animations.
  - Goal: reduce unnecessary per-frame work in menu and WebGL.
  - Suggested targets: parallax scripts, repeated DOTween loops, animated background elements.

## Priority 4: Architecture Cleanup

### Gradual `GameController` Extraction

- [ ] Extract morphing into a `MorphController` or `PieceMorphService`.
  - Goal: isolate transformation logic, VFX, audio, and piece swapping.
  - Keep `GameController` responsible for deciding when morph is allowed.

- [ ] Extract level building into a `LevelRuntimeBuilder`.
  - Goal: separate board/piece instantiation from turn logic.
  - Candidate outputs: `ChessEngine`, `CellView` dictionary, `PieceView` dictionary, board center.

- [ ] Extract inventory handling into `PlayerInventoryController`.
  - Goal: isolate unlock state, card clicks, and inventory UI refresh.

- [ ] Extract victory/progress saving into `VictoryProgressController`.
  - Goal: centralize star calculation, PlayerPrefs keys, and completion UI data.

- [ ] Extract edit-mode behavior into `LevelEditController`.
  - Goal: keep debug/editor tools away from normal runtime turn logic.

### Scene and Naming Cleanup

- [ ] Rename `FinalSCene` to `FinalScene`.
  - Risk: requires updating Build Settings and references.

- [ ] Rename `TypeWritter` scripts/classes to `TypeWriter`.
  - Risk: Unity serialized script references need careful handling.

- [ ] Rename `BollboardFollow` to `BillboardFollow`.
  - Risk: update any prefab or scene component references.

- [ ] Replace hard-coded scene index `5` in `LoadSceneOnEnd`.
  - Goal: avoid loading a scene that is not in the current build settings.
  - Suggested implementation: serialized scene name/build index with validation.

- [ ] Replace deprecated `FindObjectOfType<T>(bool)` usages.
  - Suggested replacement: `FindFirstObjectByType` or `FindAnyObjectByType`.

## Priority 5: Audio Polish

- [ ] Add sound variants for move, select, invalid click, capture, morph, victory stars.
  - Goal: reduce repetition and improve feedback.
  - Suggested implementation: FMOD multi-instrument or randomized event variants.

- [ ] Add transition phase sounds.
  - Goal: scene transitions feel intentional.
  - Suggested implementation: assign FMOD events to `SceneTransitionFmodAudio`.

- [ ] Add FMOD parameters for transformation intensity.
  - Goal: stronger morphs can sound bigger without separate event paths.

- [ ] Confirm pause snapshot and UI restart audio cleanup.
  - Goal: avoid stuck snapshots or lingering events after scene reload.

## Priority 6: Testing

### Logic Tests

- [ ] Test `ChessEngine.GetValidMoves` for every piece type.
- [ ] Test inactive cells and allied blockers.
- [ ] Test enemy captures and line-piece stopping behavior.
- [ ] Test pawn move behavior vs pawn threat behavior.
- [ ] Test `ChessEngine.UpdateThreatMap` with multiple attackers.

### Runtime/Integration Tests

- [ ] Test `GameController.SwapPlayerPiece`.
  - Verify board model changes, old view is removed, new view is added, VFX plays, and inventory state is preserved.

- [ ] Test capture feedback flow.
  - Verify the prefab script reference is valid, VFX plays, enemy view is removed from lookup, captured enemy pop/shrinks, capture sound plays after landing, and camera impulse is applied.

- [ ] Test victory star calculation.
  - Include hint/no-hint, target score, and current extra victory-star behavior.

- [ ] Test scene transition phase order.
  - Verify input blocking, cover, scene activation, reveal, and duplicate manager handling.

- [ ] Test custom cursor state transitions.
  - Verify default/interactable states on UI, cells, pieces, pause menu, and after scene changes.

- [ ] Test localization switching in WebGL.
  - Verify async string callbacks do not apply stale text after rapid locale changes.

## Nice-To-Have Ideas

- [x] Add a small camera impulse on capture.
- [ ] Add a small camera impulse on transformation.
- [ ] Add a board-cell "danger shimmer" for threatened cells.
- [ ] Add first-time tutorial callouts for morphing and threatened cells.
- [ ] Add a settings toggle for reduced VFX.
- [ ] Add a settings toggle for Bloom intensity or "reduced glow".
- [ ] Add a small editor preview button for `PieceTransformationVfx`.
- [ ] Add a `TransformationVfxPreset` ScriptableObject if more VFX variants are needed.
- [ ] Add a `SceneTransitionConfig` preview utility in editor.

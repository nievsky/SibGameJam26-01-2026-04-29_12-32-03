# Session Context

Date: 2026-06-06

Scope: Priority 1 impact polishing plus experimental drag-and-drop controls.

## Completed This Pass

- Selectable player pieces now get hover scale/tint feedback through `PieceView.SetSelectableHover`.
- Valid destination cells now get hover feedback through `CellView`.
- Invalid board clicks now trigger short cell feedback through `CellView.PlayInvalidClickFeedback`.
- The ghost move preview was removed after playtesting and replaced with hover-lift feedback.
- Hovered cells lift upward and return to their base position; if a piece is on the hovered cell, the piece lifts with it.
- Capture feedback now uses an editable prefab at `Assets/Prefabs/VFX/CaptureFeedbackVfx.prefab`.
- Captures play VFX, camera impulse, existing capture sound after landing, and captured-enemy pop/shrink before destroy.
- The capture VFX missing-script issue was fixed by keeping `CaptureFeedbackVfx` in its own script file.
- Capture camera impact now uses both impulse and procedural shake through `CameraController`.
- Enemy capture no longer restarts immediately. `GameController._enemyCaptureRestartDelay` controls the pause before restart.
- Experimental drag-and-drop controls are implemented behind `GameController._enableDragAndDrop`.
- Drag movement is responsive: the piece grab point follows the cursor immediately.
- Drag feel comes from a head/top grab pivot plus body inertia, not from delaying the whole piece.
- `GameController._dragGrabPivotHeightRatio` controls where the piece is grabbed from root/body (`0`) to rendered top/head (`1`).
- `PieceView` computes the drag pivot from renderer bounds, solves the root position from the pivot, and tilts/settles the visual body during drag.
- Drag release prioritizes the nearest valid cell under the dragged piece before falling back to the cursor target.

## Important Files

- `Assets/Scripts/ChessEngine/GameController.cs`
- `Assets/Scripts/Visuals/CellView.cs`
- `Assets/Scripts/Visuals/PieceView.cs`
- `Assets/Scripts/Visuals/CaptureFeedbackVfx.cs`
- `Assets/Scripts/Visuals/PieceTransformationVfx.cs`
- `Assets/Scripts/CameraController.cs`
- `Assets/Prefabs/VFX/CaptureFeedbackVfx.prefab`
- `Assets/Scenes/GameScene.unity`

## Current Verification

- User playtested the move/selection feedback and capture VFX in Unity.
- User playtested drag responsiveness, head-pivot feel, body inertia, and under-piece drop priority; current behavior matched expectations.
- `dotnet build .\Assembly-CSharp.csproj --no-restore` passes with `0 Error(s)` and `27 Warning(s)`.
- The remaining build warnings are existing Unity/API or assembly-version warnings, including deprecated object-find API usage.

## Notes For Resume

- Edit capture particles in `Assets/Prefabs/VFX/CaptureFeedbackVfx.prefab`; the prefab has child systems named `Impact Flash`, `Impact Ring`, `Burst Shards`, `Ring Shards`, `Heavy Dust Shards`, and `Lingering Glints`.
- Keep `CaptureFeedbackVfx.cs` as a standalone script asset while the prefab references it. Moving the class into another file can recreate Unity missing-script errors.
- `GameController` uses `CellView.BaseWorldPosition` for logical movement targets so hover-lift does not shift gameplay positions.
- Drag tuning fields live under `GameController` > `Drag And Drop`: `_enableDragAndDrop`, `_dragLiftHeight`, `_dragGrabPivotHeightRatio`, `_dragBodyMaxTiltAngle`, `_dragBodyTiltSmoothing`, `_dragBodySettleDuration`, `_dragSnapBackDuration`, `_dragDropCellRadiusMultiplier`.
- Capture impact tuning fields live under `GameController` > `Capture Camera Impact`: player and enemy capture shake/impulse values plus `_captureCameraShakeFrequency`.
- Enemy restart delay lives under `GameController` > `Enemy Capture Restart` as `_enemyCaptureRestartDelay`.
- If drag feels wrong later, first tune global `GameController` values. Only add per-piece `PieceView` overrides if different prefabs cannot share one good setup.
- Transformation polish is still deferred: piece-type colors, timing with morph audio, stronger unlock presets, and VFX pooling.

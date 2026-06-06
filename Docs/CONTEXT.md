# Session Context

Date: 2026-06-06

Scope: Priority 1 impact polishing.

## Completed This Pass

- Selectable player pieces now get hover scale/tint feedback through `PieceView.SetSelectableHover`.
- Valid destination cells now get hover feedback through `CellView`.
- Invalid board clicks now trigger short cell feedback through `CellView.PlayInvalidClickFeedback`.
- The ghost move preview was removed after playtesting and replaced with hover-lift feedback.
- Hovered cells lift upward and return to their base position; if a piece is on the hovered cell, the piece lifts with it.
- Capture feedback now uses an editable prefab at `Assets/Prefabs/VFX/CaptureFeedbackVfx.prefab`.
- Captures play VFX, camera impulse, existing capture sound after landing, and captured-enemy pop/shrink before destroy.
- The capture VFX missing-script issue was fixed by keeping `CaptureFeedbackVfx` in its own script file.

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
- `dotnet build .\Assembly-CSharp.csproj --no-restore` passes with `0 Error(s)` and `27 Warning(s)`.
- The remaining build warnings are existing Unity/API or assembly-version warnings, including deprecated object-find API usage.

## Notes For Resume

- Edit capture particles in `Assets/Prefabs/VFX/CaptureFeedbackVfx.prefab`; the prefab has child systems named `Impact Flash`, `Impact Ring`, `Burst Shards`, `Ring Shards`, `Heavy Dust Shards`, and `Lingering Glints`.
- Keep `CaptureFeedbackVfx.cs` as a standalone script asset while the prefab references it. Moving the class into another file can recreate Unity missing-script errors.
- `GameController` uses `CellView.BaseWorldPosition` for logical movement targets so hover-lift does not shift gameplay positions.
- Transformation polish is still deferred: piece-type colors, timing with morph audio, stronger unlock presets, and VFX pooling.

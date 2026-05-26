# VR Fixes Documentation (All Phases)

## Phase 1: Remove duplicate tools
**File:** `GenericStandingMode.cs`
- Removed `MenuTool` and `WarpTool` from the tools list, keeping only `GripMoveKKCharaStudioTool`.
- Changed to `new Type[1]` instead of using `base.Tools.Concat()` to completely prevent VRGIN's built-in tools from being registered.

## Phase 2: Fix duplicate UI panel
**File:** `GripMoveKKCharaStudioTool.cs`
- Made `internalGui` a `static` field so both left and right controllers share one UI panel.
- Added `if (internalGui == null)` guard in `OnStart()` so only the first controller creates the UI.
- Removed `DestroyImmediate(internalGui)` from `OnDestroy()` since the shared instance must persist.

## Phase 3: Fix laser direction + hand orientation
**File:** `GripMenuHandler.cs`
- **Root cause of laser disappearing:** `GetLaserOrigin()` was using `fingerTip.forward` as the laser direction when HandModel was enabled. But the hand model has a -40° pitch rotation, which rotated the finger's forward direction downward. This meant the raycast could never hit the UI quad.
- **Fix:** Changed `GetLaserOrigin()` to only use `fingerTip.position` (for visual accuracy) but keep using `Laser.transform.forward` (aligned with the actual controller) as the ray direction.
- Also increased the scroll deadzone from 0.3 to 0.7 and added `!IsPressing` guard to prevent accidental scrolling when pressing the trigger.

**File:** `VRHandModelManager.cs`
- Removed the Z-roll rotation (±90°) that was causing the palm to face outward/the wrong direction.
- Kept the -40° pitch offset to compensate for Quest 3 grip angle.
- Hand model is parented directly to `tracked.transform` (not to `Model` child) to prevent SteamVR from destroying it.

## Current State Summary
| File | Key Changes |
|------|-------------|
| `GenericStandingMode.cs` | Only `GripMoveKKCharaStudioTool` in tools list |
| `GripMoveKKCharaStudioTool.cs` | `static GUIQuad internalGui` prevents duplicate UI |
| `GripMenuHandler.cs` | Laser uses controller forward (not rotated finger forward); scroll deadzone increased |
| `VRHandModelManager.cs` | Hand parented to controller root; -40° pitch only; no Z-roll |

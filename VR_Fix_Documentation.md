# VR Fixes Documentation (All Phases)

## Phase 1: Remove Duplicate Tools
**File:** `GenericStandingMode.cs`
- Removed legacy `MenuTool` and `WarpTool` from the tools list, keeping only `GripMoveKKCharaStudioTool`.
- Changed to `new Type[1]` instead of using `base.Tools.Concat()` to completely prevent VRGIN's built-in tools from being registered.

## Phase 2: Fix Duplicate UI Panel
**File:** `GripMoveKKCharaStudioTool.cs`
- Made `internalGui` a `static` field so both left and right controllers share one UI panel.
- Added `if (internalGui == null)` guard in `OnStart()` so only the first controller creates the UI.
- Removed `DestroyImmediate(internalGui)` from `OnDestroy()` since the shared instance must persist.

## Phase 3: Fix Laser Direction + Hand Orientation
**File:** `GripMenuHandler.cs`
- **Root cause of laser disappearing:** `GetLaserOrigin()` was using `fingerTip.forward` as the laser direction when HandModel was enabled. But the hand model has a -40° pitch rotation, which rotated the finger's forward direction downward. This meant the raycast could never hit the UI quad.
- **Fix:** Changed `GetLaserOrigin()` to only use `fingerTip.position` (for visual accuracy) but keep using `Laser.transform.forward` (aligned with the actual controller) as the ray direction.
- Also increased the scroll deadzone from 0.3 to 0.7 and added `!IsPressing` guard to prevent accidental scrolling when pressing the trigger.

**File:** `VRHandModelManager.cs`
- Removed the Z-roll rotation (±90°) that was causing the palm to face outward/the wrong direction.
- Kept the -40° pitch offset to compensate for Quest 3 grip angle.
- Hand model is parented directly to `tracked.transform` (not to `Model` child) to prevent SteamVR from destroying it.

## Phase 4: Quest 3 Hand Overhaul, Soft Physics, Proximity Grab & Vignette
**File:** `VRHandModelManager.cs`
- **Natural Finger Curling**: Replaced uniform finger bending with realistic, biological curling (proximal joints bend more, distal joints bend less in a decreasing curl cascade).
- **Relaxation State**: Fingers have a natural, relaxed curve at rest rather than sticking straight out.
- **DynamicBone Collision**: Integrated sphere colliders on palms and fingertips, enabling soft-body physical collisions with characters' hair, breasts, clothing, and other DynamicBone objects.
- **Configuration Offsets**: Added full settings in settings panel for Hand Offset (X, Y, Z) and Hand Rotation (Pitch, Yaw, Roll) to calibrate virtual hands to controller positions in real-time.

**File:** `GripMoveKKCharaStudioTool.cs`
- **Proximity Grab**: OverlapSphere scans for character IK targets within `ProximityGrabRadius`. Displays a subtle green breathing visual pulse on the closest target.
- **Distance Tether (Grab Line)**: A 3D LineRenderer draws a color-shifting line from hand to target (Green < 0.1m, Yellow < 0.3m, Orange > 0.3m) to show tension.
- **Haptic Feedback**: Triggers high-fidelity vibration pulses on controller (1500 intensity on grab, 800 on release).
- **Smooth IK Grabbing**: Uses lerp/slerp interpolation for grabbed IK targets to give them a satisfying springy physics feel.

**File:** `VRComfortVignette.cs`
- **Cinematic Anti-Motion-Sickness**: Dynamically fades in a black vignetted border around the screen during left-stick locomotion or right-stick turning. Zero performance cost and highly customizable via settings GUI.

**File:** `VRQuickActions.cs` & `GripMoveKKCharaStudioTool.cs`
- **Thumbstick Locomotion**: Fully implemented smooth left-stick walk, right-stick snap/smooth turn, and right-stick Up/Down vertical height (Y-axis) adjustment.

## Phase 5: High-Performance Desktop Privacy blackout & Scale Removal
**File:** `VRLoader.cs`
- **Complete Scale Disable**: Commented out the `VRTwoHandScale` component. This completely disables two-hand scaling and movement, preventing accidental vertical height drift or horizontal scale expansion when both controllers' triggers/grips are held, giving users clean, joystick-only locomotion.

**File:** `KKCharaStudioVRGUI.cs`
- **VRDesktopCoverCamera**: Replaced slow and intrusive Canvas/IMGUI overlay methods with a lightweight, high-performance Camera (`depth = 99999f`, `cullingMask = 0`, `clearFlags = SolidColor`, `backgroundColor = Color.black`) rendering directly to the screen backbuffer (`targetTexture = null`).
- **0% VR UI Interference**: This camera only clears the PC display buffer, leaving VR headset eye textures, uGUI captures, and IMGUI captures 100% unaffected and fully interactive inside VR.
- **Save GPU Load**: Disables Unity's VR desktop mirror rendering (`showDeviceView = false`) when covered, saving substantial GPU/CPU rendering resources.
- **Key Bind**: Bind to keyboard **Spacebar** or the click button in VR settings GUI.

---

## Final Source Files Overview
| File | Key Purpose |
|------|-------------|
| `GenericStandingMode.cs` | Prevent duplicate tool registration |
| `GripMoveKKCharaStudioTool.cs` | Share static GUI, smooth grabbing, proximity grab, joysticks |
| `GripMenuHandler.cs` | Realign raycast laser to controller forward; fix dropdowns/sliders |
| `VRHandModelManager.cs` | Custom hand offsets, natural resting curl, biological curl cascade, DynamicBone colliders |
| `VRComfortVignette.cs` | Comfort vignette during locomotion to prevent motion sickness |
| `VRQuickActions.cs` | A/X UI toggles, joystick click actions (Summon, Undo) |
| `VRLoader.cs` | Disabled two-hand scale; load optimized components |
| `KKCharaStudioVRGUI.cs` | Settings menu; high-performance `VRDesktopCoverCamera` for blackout |

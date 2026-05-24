# KK_VR Plugin - Project Status & Review Notes

## Project Location
- Plugin: E:\KK_VR\KKCharaStudioVRPlugin
- VRGIN (modified): E:\KK_VR\VRGIN_KKCS
- Git repo cloned from: https://github.com/Ermin610/KK_VR

## Tech Stack
- Unity 5.x, .NET 3.5, LangVersion 11.0
- BepInEx mod framework
- VRGIN VR abstraction layer (decompiled/modified)
- SteamVR old API (NOT SteamVR 2.x Input System)
- Target HMD: Meta Quest 3

## Workflow
- I (Claude) analyze code, design architecture, write detailed prompts
- User copies prompts to other agents for execution
- User brings results back for review and fixes
- Goal: make features feel "像kk自带的功能一样融洽" (native)

## SDK-style csproj CRITICAL NOTE
The .csproj is SDK-style and auto-includes all *.cs files.
NEVER add explicit <Compile Include> entries — causes duplicate compilation errors.

## Key API Gotcha
In Tool base class:
- `Owner` = VRGIN.Controls.Controller (has SetRenderModelVisible)
- `Controller` = SteamVR_Controller.Device (has TriggerHapticPulse, GetAxis, GetPress etc.)
These names are VERY confusing. Always use Owner for VRGIN controller, Controller for SteamVR device.

---

## Completed Phases

### Phase 1: Core VR Interaction (Initial Implementation)
- VRHandModelManager.cs (NEW) - Procedural hand models with finger animation
- DynamicBoneColliderManager.cs (NEW) - Hand colliders for character physics
- VRHandHapticTrigger.cs (NEW) - Haptic feedback on character touch
- KKCharaStudioVRSettings.cs (MODIFIED) - All new settings properties
- KKCharaStudioVRGUI.cs (MODIFIED) - Settings UI with organized sections
- VRQuickActions.cs (MODIFIED) - UI toggle + menu summon
- VRLoader.cs (MODIFIED) - Component initialization
- GripMoveKKCharaStudioTool.cs (MODIFIED) - Thumbstick locomotion
- IKTool.cs (MODIFIED) - Incremental scanning, null safety

### Phase 2: Bug Fixes (Round 1 + Round 2)
Round 1 (10 fixes):
- VRHandModelManager: FindObjectsOfType every frame → coroutine init
- CreateHand missing isLeft parameter
- Trigger finger smoothing added
- Hand model positions recalculated for SteamVR coords
- Finger segment taper added
- DynamicBone collider positions realigned
- VRHandHapticTrigger SkinnedMeshRenderer/DynamicBone filter
- GUI reset defaults fixed (HandModelEnabled=true)
- IKTool processedObjectKeys.Clear() on level change
- DynamicBoneColliderManager cleanup (OnLevelWasLoaded, OnDestroy)

Round 2 (3 fixes):
- GripMoveKKCharaStudioTool: Controller → Owner for SetRenderModelVisible
- GripMenuHandler: removed dead UpdateLaserTransform method
- csproj: removed duplicate Compile Include entries

### Phase 3: Per-Hand Visibility + Button Fixes
- VRHandModelManager: Added SetHandVisible(bool isLeft, bool visible)
- VRHandModelManager: UpdateSingleHand no longer controls SetActive
- VRHandModelManager: CreateHand sets root inactive initially
- GripMoveKKCharaStudioTool: Per-hand visibility control via _isLeftHand
- GripMoveKKCharaStudioTool: Runtime settings reactivity in OnUpdate
- GripMoveKKCharaStudioTool: Haptic feedback on grab/release
- VRQuickActions: Removed CheckMenuSummon (conflicted with VRGIN tool switch)
- VRQuickActions: A/X button only for UI toggle (removed Axis0)
- VRQuickActions: Left stick click = summon main GUI
- VRQuickActions: Right stick click = undo (TryUndo with try/catch)

### Phase 4: Proximity IK Grab (JUST COMPLETED - REVIEWED OK)
- GripMoveKKCharaStudioTool: UpdateProximityDetection() with OverlapSphere
- GripMoveKKCharaStudioTool: ShowProximityHighlight/ClearProximityHighlight
- GripMoveKKCharaStudioTool: Proximity grab injection in HandleObjectGrab
- Detection radius: 0.12m, throttled every 3 frames
- Only targets MoveableGUIObject with guideObject != null (IK targets only)
- Green semi-transparent highlight sphere at nearest target
- Clean cleanup in OnDisable, OnDestroy, OnLevel

---

## Phase 4 Review Details

Code at GripMoveKKCharaStudioTool.cs verified correct:
- Line 432: Proximity grab checks GetPressDown BEFORE existing grab logic
- GetPressDown is frame-based (not consumption-based), so calling it twice is fine
- Proximity grab sets screenGrabbed + lastGrabbedObject → existing grab init picks it up
- grabbingObject persists across frames for continuous movement
- lastGrabbedObject reset at end of OnUpdate doesn't affect active grabs
- Direct contact (OnTriggerStay) takes priority: screenGrabbed already true → proximity skipped
- No proximity target → world grip-move works normally (screenGrabbed stays false)

---

## Phase 5: Small Fixes (COMPLETED)
- VRQuickActions.cs: Added `if (VR.Mode == null) return;` null guard
- VRHandModelManager.cs: Removed dead `lastAlpha` field
- GripMoveKKCharaStudioTool.cs: Cached `_isLeftHand` in HandleThumbstickLocomotion
- GripMoveKKCharaStudioTool.cs: Added ProximityGrabEnabled check in UpdateProximityDetection
- GripMoveKKCharaStudioTool.cs: Configurable ProximityGrabRadius from settings
- KKCharaStudioVRSettings.cs: Added ProximityGrabEnabled + ProximityGrabRadius
- KKCharaStudioVRGUI.cs: Added Proximity Grab toggle + radius slider + defaults

## Phase 6: VAM-Level Features (PENDING - Prompts Ready)

See `06_consolidated_prompts.md` (replaces `05_vam_level_prompts.md`)

5 prompts, zero file conflicts. New features:
1. **VRComfortVignette** — Movement vignette (procedural mesh, vertex color gradient)
2. **VRTwoHandScale** — Two-hand world scaling (scale-invariant distance math)
3. **Settings + GUI + VRLoader** — All config for new features
4. **GripMoveKKCharaStudioTool ALL changes** — Vignette integration, touch-to-select,
   grab distance line, two-hand scale conflict resolution, IK smooth interpolation,
   proximity highlight pulse animation
5. **Hand touch color feedback** — Hand model turns warm pink on character contact

Bugs fixed vs v1 prompts (05):
- VRTwoHandScale: Inverted scaling direction + oscillation from world-space distance
- VRComfortVignette: Contradictory shader instructions + incompatible SetFloat calls
- Grab line: Invisible due to Particles/Alpha Blended shader needing texture
- Multiple prompts modifying same file → consolidated into single prompts per file

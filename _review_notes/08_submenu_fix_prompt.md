# Phase 7: VR UI Submenu Fix — Agent Prompt

## Background

This is a KK Chara Studio VR plugin (Unity 5.x, .NET 3.5, SteamVR old API). The VR UI interaction pipeline:

1. **Rendering**: `VRGUI.CatchCanvas()` catches unprocessed Canvas components, redirects them to `_VRGUICamera` → renders to `uGuiTexture`. `GUIQuad` displays this texture as a 3D plane in VR.
2. **Input**: `GripMenuHandler` laser-raycasts against GUIQuad → UV hit → screen pixel coordinates via `MouseOperations.SetClientCursorPosition()` → simulates Windows mouse clicks via `VR.Input.Mouse.LeftButtonDown/Up()`.

**Problem**: Primary (top-level) menus work fine. But secondary/sub-menus cannot be clicked — clicking a submenu item has no effect or closes the submenu.

## Root Cause: GUIAlternativeSortingMode is Disabled

**File**: `KKCharaStudioVRPlugin/KKCharaStudioVR/ConfigurableContext.cs`

**Analysis**: Line 63 sets `GUIAlternativeSortingMode = false`. When this is false, `VRGUI.CatchCanvas()` (in `VRGIN_KKCS/VRGIN.Core/VRGUI.cs`) does NOT replace `GraphicRaycaster` with `SortingAwareGraphicRaycaster`.

Why this breaks submenus:
- Submenu canvases spawn with HIGHER `sortingOrder` than their parent menu canvas
- Without `SortingAwareGraphicRaycaster`, the EventSystem's default `GraphicRaycaster` returns `sortOrderPriority = Int32.MinValue`, which tells EventSystem to use its own sorting — unreliable for overlapping ScreenSpaceCamera canvases
- Result: clicks on submenu items get intercepted by the parent canvas underneath
- Unity's built-in Dropdown component is especially affected — it creates a full-screen "Blocker" canvas behind the dropdown. Without sorting awareness, the Blocker intercepts all clicks, immediately closing the dropdown

The `SortingAwareGraphicRaycaster` class already exists in `VRGIN.Core/VRGUI.cs`. It overrides `sortOrderPriority` to return `-canvas.sortingOrder`, ensuring canvases with higher sortingOrder get processed first. All the infrastructure is already built — it just needs to be turned on.

**Fix**: Change ONE line in `ConfigurableContext.cs`:
```
Line 63: GUIAlternativeSortingMode = false;
→
Line 63: GUIAlternativeSortingMode = true;
```

## Bonus Enhancement: Scroll Wheel Simulation

**File**: `KKCharaStudioVRPlugin/KKCharaStudioVR/GripMenuHandler.cs`

**Problem**: Even with submenus working, dropdown lists and scrollable panels can't be scrolled in VR because there's no scroll wheel simulation. This makes long lists unusable.

**Fix architecture**:
- Add scroll simulation using **thumbstick Y-axis** (`EVRButtonId.k_EButton_Axis0`) while the laser is visible and pointing at a GUIQuad
- Add a new field: `private float _lastScrollTime;`
- In `CheckInput()` method, after the existing left-click block (after the `GetPressUp` block, around line 394), add scroll logic:
  - Guard: only when `LaserVisible && _Target != null && !IsResizing`
  - Read thumbstick Y: `Device.GetAxis(EVRButtonId.k_EButton_Axis0).y`
  - Dead zone: only act if `Mathf.Abs(thumbstickY) > 0.3f`
  - Cooldown: only act if `Time.time - _lastScrollTime > 0.05f`
  - Simulate scroll: `WindowsInterop.mouse_event(0x0800, 0, 0, (int)(thumbstickY * 120), 0)`
    - `0x0800` = `MOUSEEVENTF_WHEEL` Windows constant
    - `120` = one wheel notch in Windows (WHEEL_DELTA)
    - Positive = scroll up, Negative = scroll down
  - Update `_lastScrollTime = Time.time`

**Key constraints**:
- Use `WindowsInterop.mouse_event()` directly from `VRGIN.Native` namespace — the P/Invoke is already declared at `VRGIN_KKCS/VRGIN.Native/WindowsInterop.cs` line 57
- Add `using VRGIN.Native;` if not already imported (it is already imported in GripMenuHandler)
- **No thumbstick conflict**: When laser is visible, the GripMenuHandler is in UI interaction mode. The thumbstick locomotion in `GripMoveKKCharaStudioTool` only runs when the laser is NOT active.

## Files to Modify

1. **`KKCharaStudioVRPlugin/KKCharaStudioVR/ConfigurableContext.cs`** — Line 63: `false` → `true`
2. **`KKCharaStudioVRPlugin/KKCharaStudioVR/GripMenuHandler.cs`** — Add `_lastScrollTime` field + scroll logic in `CheckInput()`

## Testing

1. Open KK Chara Studio in VR
2. Click a top-level menu item → submenu should appear
3. Click a submenu item → should register the click (this was broken before)
4. Open any dropdown/list → use thumbstick up/down to scroll
5. Verify primary menu interaction still works normally

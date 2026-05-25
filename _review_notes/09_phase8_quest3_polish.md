# Phase 8: Quest 3 Comfort Polish — Agent Prompt

## Background

KK Chara Studio VR plugin, Unity 5.x, SteamVR old API (`SteamVR_Controller.Device`), targeting Meta Quest 3 via SteamVR Link.

Previous phases implemented: VR locomotion, hand models, DynamicBone collision, haptic feedback, proximity grab, comfort vignette, two-hand scale, submenu fix, scroll wheel. The plugin is functionally complete but needs polish for comfortable Quest 3 usage.

## Goals

1. Fix a confirmed thumbstick conflict bug
2. Add haptic feedback for UI interactions (click/scroll feel)
3. Improve laser pointer visual quality
4. Add locomotion guard to prevent accidental movement during UI use

---

## Fix 1: Thumbstick Locomotion / Scroll Conflict (BUG)

**File**: `KKCharaStudioVRPlugin/KKCharaStudioVR/GripMoveKKCharaStudioTool.cs`

**Problem**: `HandleThumbstickLocomotion()` (line ~390) reads the thumbstick every frame for locomotion (left hand = move, right hand = turn). `GripMenuHandler` (on the same controller) also reads the thumbstick for UI scroll when its laser is active. Both fire simultaneously → user scrolls a dropdown while accidentally walking/turning.

**The field `gripMenuHandler` already exists** (line 29) and is already initialized (line 131). It's the same GripMenuHandler instance on this controller.

**Fix**: At the very beginning of `HandleThumbstickLocomotion()`, add:
```
if (gripMenuHandler != null && gripMenuHandler.LaserVisible) return;
```

This is a 1-line fix. When the laser is pointing at UI, skip locomotion entirely for this hand. The other hand's tool instance has its own `gripMenuHandler`, so only the hand currently interacting with UI is affected.

**Location**: `HandleThumbstickLocomotion()` method, right after the opening brace, before `Vector2 axis = controller.GetAxis(...)`.

---

## Fix 2: UI Interaction Haptics

**File**: `KKCharaStudioVRPlugin/KKCharaStudioVR/GripMenuHandler.cs`

**Problem**: Clicking and scrolling UI has zero haptic feedback. On Quest 3 this makes UI interaction feel disconnected and "floaty". Adding subtle vibration for UI clicks and scroll ticks makes it feel physical.

**Fix architecture**:

Add a helper method to GripMenuHandler:
```
private void PulseHaptic(ushort durationMicroseconds)
```
- Get the controller's `SteamVR_TrackedObject` via `_Controller.Tracking` (or `GetComponent<SteamVR_TrackedObject>()`)
- Call `SteamVR_Controller.Input((int)trackedObj.index).TriggerHapticPulse(duration, EVRButtonId.k_EButton_Axis0)`
- Wrap in null/validity checks

Then call it from `CheckInput()`:
- **On left click down** (`GetPressDown(k_EButton_Axis1)`): `PulseHaptic(800)` — short crisp click
- **On scroll tick** (when `mouse_event` fires): `PulseHaptic(200)` — very soft tick

**Key constraints**:
- `_Controller` is a `VRGIN.Controls.Controller` — it has a `Tracking` property that returns the `SteamVR_TrackedObject`
- Alternatively use `((Component)this).GetComponent<SteamVR_TrackedObject>()` — both work
- `TriggerHapticPulse(ushort duration, EVRButtonId buttonId)` — duration in microseconds (100-3999), buttonId should be `k_EButton_Axis0`
- Keep click pulse SHORT (800μs) — too long feels muddy
- Keep scroll pulse VERY light (200μs) — continuous scroll shouldn't be jarring

---

## Fix 3: Laser Pointer Visual Polish

**File**: `KKCharaStudioVRPlugin/KKCharaStudioVR/GripMenuHandler.cs`

**Problem**: The laser pointer has abrupt color switches (cyan → green → red) and the dot cursor's material is fetched via `GetComponent<Renderer>()` every frame in `UpdateLaser()`, which is wasteful and causes GC allocations.

**Fix architecture**:

### 3a: Cache dot cursor renderer
- Add a field: `private Renderer _dotRenderer;`
- In `InitLaser()`, after creating dotCursor, cache: `_dotRenderer = dotCursor.GetComponent<Renderer>();`
- In `UpdateLaser()`, replace all `dotCursor.GetComponent<Renderer>()` calls with `_dotRenderer`

### 3b: Smooth color transitions
- Add fields: `private Color _currentLaserColor = Color.cyan;` and `private Color _currentDotColor = Color.cyan;`
- In `UpdateLaser()`, instead of setting colors directly, lerp toward target:
  - Target colors: hitting UI + pressing = soft red, hitting UI + not pressing = green, not hitting = cyan
  - `_currentLaserColor = Color.Lerp(_currentLaserColor, targetColor, Time.deltaTime * 10f);`
  - Apply: `Laser.SetColors(_currentLaserColor, _currentLaserColor);`
  - Same for dot: `_dotRenderer.material.color = _currentDotColor;`
- This creates smooth fade transitions instead of harsh snapping

### 3c: Laser width pulse when hitting UI
- When laser hits UI (`hitUI = true`), slightly pulse the laser width for visual feedback:
  - `float w = 0.002f + 0.001f * Mathf.Sin(Time.time * 3f);`
  - `Laser.SetWidth(w, w * 0.5f);` (taper toward hit point)
- When not hitting: `Laser.SetWidth(0.002f, 0.002f);`
- This makes the laser feel "alive" when interacting

---

## Files to Modify

1. **`GripMoveKKCharaStudioTool.cs`** — Add 1 line at top of `HandleThumbstickLocomotion()`
2. **`GripMenuHandler.cs`** — Add haptic helper + calls, cache renderer, smooth colors, width pulse

## Technical Reference

- **SteamVR old API**: `SteamVR_Controller.Device.TriggerHapticPulse(ushort durationMicroseconds, EVRButtonId buttonId)`, `GetAxis()`, `GetPressDown/Up()`
- **Unity 5.x LineRenderer**: `SetColors(Color start, Color end)`, `SetWidth(float start, float end)`, `SetVertexCount(int)`, `SetPosition(int, Vector3)` — NOT the modern API (startColor/endColor/positionCount/startWidth)
- **Quest 3 SteamVR mapping**: `k_EButton_A` = A/X, `k_EButton_Axis0` = thumbstick, `k_EButton_Axis1` = trigger, `k_EButton_Grip` = grip, `k_EButton_ApplicationMenu` = B/Y
- **Namespace imports already present**: `VRGIN.Controls`, `VRGIN.Core`, `VRGIN.Native`, `VRGIN.Visuals`, `Valve.VR`

## Testing

1. Point laser at UI → use thumbstick to scroll → verify NO movement/turning happens
2. Click UI elements → feel haptic click pulse
3. Scroll UI → feel soft haptic ticks
4. Watch laser color transitions — should be smooth fades, not sudden switches
5. Laser should gently pulse width when hitting UI surface
6. Verify locomotion still works normally when NOT pointing at UI

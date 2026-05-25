# Phase 9: VAM-Style UI Reposition on Toggle

## Goal
When the user hides UI then re-shows it (A/X button), ALL UI panels should reappear in front of the user's head at a comfortable position — exactly like VAM's menu behavior. While visible, UI stays fixed in world space (user can walk around it).

## Current Behavior (Problems)
1. `ToggleAllGUI()` in `VRQuickActions.cs` **skips** the main studio panel (`internalGui`) because it has `MoveableGUIObject + IsOwned=true`
2. When showing UI back, quads reappear **at their old world positions** — no repositioning
3. The user has to manually long-press B/Y (1.5s) on GripMoveKKCharaStudioTool to call `resetGUIPosition()` separately

## Target Behavior
- A/X toggle hides **everything** (including `internalGui`)
- A/X toggle shows **everything**, **repositioned in front of the user's head**
- While visible, panels stay fixed in world space (no change needed — already works)
- Left stick click (`SummonMainGUI`) continues to work as a separate "summon to face" shortcut

## Architecture

### File 1: `KKCharaStudioVRPlugin/KKCharaStudioVR/VRQuickActions.cs`

This is the ONLY file that needs significant changes.

#### Change 1: Remove the MoveableGUIObject skip in ToggleAllGUI

Current code at line 74 skips owned MoveableGUIObject quads:
```csharp
if (quad.gameObject.GetComponent<MoveableGUIObject>() != null && quad.IsOwned)
{
    continue;  // ← DELETE THIS SKIP
}
```

Remove this `continue` block so that ALL quads (including `internalGui`) are toggled.

#### Change 2: Reposition all quads when showing

When `uiVisible` becomes `true`, before playing the scale animation, reposition each quad in front of the user's head. The positioning logic:

```
Transform head = VR.Camera.Head;
if (head == null) return; // safety

// Calculate spawn position: in front of head, slightly below eye level
Vector3 forward = head.forward;  
forward.y = 0;  // project onto horizontal plane
if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
forward.Normalize();

Vector3 spawnPos = head.position + forward * GUI_DISTANCE - Vector3.up * GUI_DROP;
Quaternion spawnRot = Quaternion.LookRotation(forward);
```

Constants (add as private fields):
- `GUI_DISTANCE = 0.5f` — distance in front of head (meters)
- `GUI_DROP = 0.05f` — slight drop below eye level for comfort

For EACH quad being shown:
1. Set `quad.transform.position = spawnPos`
2. Set `quad.transform.rotation = spawnRot`
3. Then play the existing scale-up animation

**Important**: If there are multiple quads, stack them vertically or use the same position (they render the same UI texture so overlapping is fine). In practice there is usually only 1 main quad.

#### Change 3: Also reposition in SummonMainGUI

`SummonMainGUI()` already repositions — but it also skips MoveableGUIObject quads. Remove that skip too (lines 163-166):
```csharp
if (quad.gameObject.GetComponent<MoveableGUIObject>() != null && quad.IsOwned)
{
    continue;  // ← DELETE THIS SKIP
}
```

This way left-stick-click can summon any quad including the main panel.

#### Change 4: Notify GripMoveKKCharaStudioTool about visibility

The `internalGui` in GripMoveKKCharaStudioTool is controlled by `OnEnable/OnDisable` of the tool. When VRQuickActions hides it, the tool's own enable/disable won't fire — the quad just gets `SetActive(false)` directly. This is fine because:
- GripMoveKKCharaStudioTool.OnEnable calls `internalGui.gameObject.SetActive(true)` — but only when the tool itself is enabled/disabled (weapon switching)
- VRQuickActions directly sets `quad.gameObject.SetActive(false/true)` — this is independent and doesn't conflict

No change needed in GripMoveKKCharaStudioTool.

### File 2: `KKCharaStudioVRPlugin/KKCharaStudioVR/KKCharaStudioVRSettings.cs`

Add one setting to control the UI spawn distance:

```
private float _UISpawnDistance = 0.5f;

[XmlComment("Distance in front of head when UI respawns (meters)")]
public float UISpawnDistance
{
    get { return _UISpawnDistance; }
    set { _UISpawnDistance = value; TriggerPropertyChanged("UISpawnDistance"); }
}
```

Then in VRQuickActions, read this setting instead of hardcoded 0.5f.

## Critical Constraints

1. **Unity 5.x API** — no `positionCount`, no `startWidth`, no null-conditional `?.` in Unity API calls. Use `Object.op_Implicit((Object)(object)x)` for null checks on Unity objects.
2. **.NET 3.5 runtime** — but LangVersion 11.0 in csproj (C# syntax features OK, runtime APIs limited)
3. **SteamVR old API** — `SteamVR_Controller.Device`, NOT SteamVR 2.x
4. **Do NOT modify VRGIN_KKCS project** — all changes in KKCharaStudioVRPlugin only
5. **SDK-style csproj** — auto-includes all `*.cs` files, no need to edit csproj
6. **`VR.Camera.Head`** returns a Transform — use `.position`, `.forward`, `.TransformPoint()` etc.
7. **GUIQuadRegistry.Quads** returns `IEnumerable<GUIQuad>` — iterate with foreach, call `.ToList()` if modifying during iteration
8. **Scale animation** — keep the existing `ScaleAnimation` coroutine, just add position/rotation setting BEFORE starting the animation
9. **The `IsOwned` flag** — don't change it. It controls whether MenuTool can grab the quad. The main panel should stay `IsOwned=true`.
10. **Debounce** — keep the existing 0.5s debounce (`Time.time - lastToggleTime < 0.5f`)

## Files to Read Before Coding
- `KKCharaStudioVRPlugin/KKCharaStudioVR/VRQuickActions.cs` (main file to modify)
- `KKCharaStudioVRPlugin/KKCharaStudioVR/KKCharaStudioVRSettings.cs` (add setting)
- `KKCharaStudioVRPlugin/KKCharaStudioVR/GripMoveKKCharaStudioTool.cs` (understand internalGui lifecycle — DO NOT modify)
- `VRGIN_KKCS/VRGIN.Visuals/GUIQuadRegistry.cs` (understand quad registry — DO NOT modify)
- `VRGIN_KKCS/VRGIN.Visuals/GUIQuad.cs` (understand quad structure — DO NOT modify)

## Testing Checklist
- [ ] A/X hides ALL UI panels including the main studio panel
- [ ] A/X shows ALL UI panels, repositioned directly in front of head
- [ ] Panel faces the user (rotation correct)
- [ ] Panel is at comfortable reading distance (~0.5m)
- [ ] While visible, panel stays fixed in world space (walk around it)
- [ ] Left stick click still summons panel to face
- [ ] B/Y long press on GripMoveKKCharaStudioTool still works as before
- [ ] Laser pointer still works on repositioned panel
- [ ] Scale animation plays smoothly on show/hide
- [ ] No errors when toggling rapidly
- [ ] Switching tools (OnEnable/OnDisable) doesn't conflict with toggle state

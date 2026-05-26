# Bug Fix: UI Duplication + Hand Model Improvements

## Bug 1: Tool Switching Creates Duplicate UIs (CRITICAL)

### Root Cause

VRGIN's `Controller` class (in the pre-compiled VRGIN_KKCS.dll, **DO NOT MODIFY**) has a built-in tool switching system in its `OnUpdate()`:

```
// Controller.cs line 303-331 (in VRGIN_KKCS.dll — READ ONLY)
if (device.GetPressUp(EVRButtonId.k_EButton_ApplicationMenu))
    ToolIndex = (ToolIndex + 1) % Tools.Count;
```

On Quest 3, `k_EButton_ApplicationMenu` maps to **B button (right hand)** and **Y button (left hand)**.

`GenericStandingMode.cs` registers **3 tools** per controller:
```csharp
typeof(MenuTool),        // VRGIN built-in — creates its own GUIQuad, attaches to controller
typeof(WarpTool),        // VRGIN built-in — teleport/scale tool
typeof(GripMoveKKCharaStudioTool)  // Our custom tool — creates internalGui GUIQuad
```

**Result**: Pressing B/Y cycles through 3 tools. Both `MenuTool` and `GripMoveKKCharaStudioTool` create their own GUIQuad that follows the controller. Since BOTH left and right controllers each have all 3 tools, the user sees up to **3 separate UIs** (left MenuTool + right MenuTool + our internalGui).

### Fix

**File: `KKCharaStudioVRPlugin/KKCharaStudioVR/GenericStandingMode.cs`**

Remove `MenuTool` and `WarpTool` from the tool list. We only need `GripMoveKKCharaStudioTool` — it already handles all interactions (grip move, locomotion, UI laser, IK grab, etc.):

```csharp
internal class GenericStandingMode : StandingMode
{
    public override IEnumerable<Type> Tools => base.Tools.Concat(new Type[1]
    {
        typeof(GripMoveKKCharaStudioTool)
    });

    // ... rest unchanged
}
```

This eliminates tool cycling entirely — B/Y press on the VRGIN Controller.OnUpdate will still fire but `(ToolIndex + 1) % 1 == 0` so it stays on the same tool.

**IMPORTANT**: Also need to handle the B/Y long-press behavior in `GripMoveKKCharaStudioTool.HandleButtonEvents()`. Currently it uses `k_EButton_ApplicationMenu` for:
- Long press (1.5s): reset GUI position
- Triple combo (Trigger+Grip+B/Y): toggle rotation lock

Since VRGIN's Controller.OnUpdate consumes the short press for tool switching BEFORE our tool's OnUpdate runs, we need to verify these long-press gestures still work. They should, because:
- VRGIN only acts on `GetPressUp` (release) within 0.5s
- Our code uses `GetPress` (hold) with 1.5s threshold
- These don't conflict

But the short B/Y press will still fire VRGIN's tool switch code (even though it's harmless with only 1 tool). This is acceptable.

### Also remove the GripMenuHandler MenuTool integration

In `GripMoveKKCharaStudioTool.cs`, the `menuHandlder` field references the VRGIN `MenuHandler` component. With `MenuTool` removed, we should also stop managing it:

In `OnStart()` around line 145: 
```csharp
menuHandlder = ((Component)this).GetComponent<MenuHandler>();  // This may return null now
```

In `OnEnable()` and `OnDisable()` — the lines that enable/disable `menuHandlder` should gracefully handle null (they already null-check, so no change needed):
```csharp
if (menuHandlder != null) ...  // Already safe
```

No code change needed here — just noting it's safe.

---

## Bug 2: Hand Model Doesn't Track Real Hand Pose

### Root Cause

The current `VRHandModelManager.cs` builds procedural hand models (Cube palm + Capsule fingers) and animates them using ONLY two inputs:

```csharp
float trigger = controller.GetAxis(EVRButtonId.k_EButton_Axis1).x;  // Index finger curl (0-1)
bool grip = controller.GetPress(EVRButtonId.k_EButton_Grip);          // Binary grip (0 or 1)
```

Problems:
1. **Grip is binary** (`GetPress` = true/false), not analog. The hand either fully grips or doesn't — looks like a clapper board.
2. **Thumb has no independent input** — just `max(grip, trigger*0.5)`.
3. **All non-index fingers move identically** at same angle — unnatural.
4. **Finger curl angles are too small** — max 50° for index, 65° for others. Real fist is 80-90° per joint.
5. **Palm orientation may be wrong** for Quest 3 controller coordinate space.

### Fix

**File: `KKCharaStudioVRPlugin/KKCharaStudioVR/VRHandModelManager.cs`**

#### Change 1: Use analog Grip axis instead of binary press

Quest 3 via SteamVR exposes grip as an analog axis on the same `k_EButton_Grip` button, but you must read it with `GetAxis`:

```csharp
// OLD (binary):
bool grip = controller.GetPress(EVRButtonId.k_EButton_Grip);
float targetGrip = grip ? 1.0f : 0.0f;

// NEW (analog):
float gripAxis = controller.GetAxis(EVRButtonId.k_EButton_Grip).x;
// Fallback: some SteamVR runtimes expose grip analog on Axis2
if (gripAxis == 0f && controller.GetPress(EVRButtonId.k_EButton_Grip))
    gripAxis = 1.0f;
```

**IMPORTANT API NOTE**: `controller.GetAxis()` returns a `Vector2`. For grip, the value is in `.x`. The button ID for analog grip on Quest 3 may be `k_EButton_Axis2` rather than `k_EButton_Grip`. Try both:

```csharp
float gripAxis = controller.GetAxis(EVRButtonId.k_EButton_Axis2).x;
if (gripAxis < 0.01f)
{
    // Fallback — some runtimes put grip on k_EButton_Grip
    gripAxis = controller.GetPress(EVRButtonId.k_EButton_Grip) ? 1.0f : 0.0f;
}
```

#### Change 2: Use capacitive touch for thumb estimation

Quest 3 has capacitive touch sensors. We can detect if the thumb is resting on the thumbstick or face buttons:

```csharp
bool thumbOnStick = controller.GetTouch(EVRButtonId.k_EButton_Axis0);
bool thumbOnButton = controller.GetTouch(EVRButtonId.k_EButton_A);
float thumbCurl = (thumbOnStick || thumbOnButton) ? 0.3f : 0.8f;
// If also gripping, curl thumb more
if (gripAxis > 0.7f && !thumbOnStick && !thumbOnButton)
    thumbCurl = 1.0f;
```

#### Change 3: Increase curl angles to realistic values

```csharp
// OLD:
AnimateFinger(ctx.fingers[1], ctx.currentTriggerVal * 50f);   // Index: max 50°
AnimateFinger(ctx.fingers[2], ctx.currentGripVal * 65f);       // Middle: max 65°

// NEW:
AnimateFinger(ctx.fingers[1], ctx.currentTriggerVal * 85f);   // Index: max 85° (real fist)
AnimateFinger(ctx.fingers[2], ctx.currentGripVal * 90f);       // Middle: max 90°
AnimateFinger(ctx.fingers[3], ctx.currentGripVal * 88f);       // Ring: slightly less
AnimateFinger(ctx.fingers[4], ctx.currentGripVal * 85f);       // Little: slightly less
AnimateFinger(ctx.fingers[0], thumbCurl * 70f);                // Thumb
```

#### Change 4: Add slight stagger between fingers for natural curl

Real hands don't curl all fingers simultaneously. Add per-finger time offset:

```csharp
// In HandContext, add:
public float[] fingerTargets = new float[5];  // target curl per finger (0-1)

// In UpdateHandAnimation, calculate per-finger targets:
ctx.fingerTargets[0] = thumbCurl;
ctx.fingerTargets[1] = ctx.currentTriggerVal;
ctx.fingerTargets[2] = gripAxis;
ctx.fingerTargets[3] = Mathf.Lerp(ctx.fingerTargets[3], gripAxis, Time.deltaTime * 10f);  // Ring lags slightly
ctx.fingerTargets[4] = Mathf.Lerp(ctx.fingerTargets[4], gripAxis, Time.deltaTime * 8f);   // Little lags more
```

#### Change 5: Verify palm orientation for Quest 3

The current palm position (`localPosition = new Vector3(0f, -0.03f, 0.04f)`) assumes the SteamVR "wand" convention (Y-up from grip, Z-forward along controller body). Quest 3 Touch Plus controllers have a different grip angle.

Check in-game whether the palm needs rotation adjustment. The likely fix is to add a rotation offset:

```csharp
// In CreateHand, after setting localPosition/localRotation:
// Quest 3 controller grip angle offset — tilts the hand model to match
// the natural hand position when holding the controller
ctx.root.transform.localRotation = Quaternion.Euler(-40f, 0f, 0f);
```

The exact angle (-40° pitch) needs to be tuned in-game. Start with -40 and adjust.

#### Complete rewritten UpdateHandAnimation:

```csharp
void UpdateHandAnimation(HandContext ctx)
{
    if (ctx.trackedObj == null || ctx.trackedObj.index == SteamVR_TrackedObject.EIndex.None) return;
    var controller = SteamVR_Controller.Input((int)ctx.trackedObj.index);
    if (controller == null) return;

    // Read analog inputs
    float trigger = controller.GetAxis(EVRButtonId.k_EButton_Axis1).x;
    
    // Grip: try analog axis first, fall back to binary
    float gripAxis = controller.GetAxis(EVRButtonId.k_EButton_Axis2).x;
    if (gripAxis < 0.01f)
        gripAxis = controller.GetPress(EVRButtonId.k_EButton_Grip) ? 1.0f : 0.0f;
    
    // Thumb: capacitive touch detection
    bool thumbOnStick = controller.GetTouch(EVRButtonId.k_EButton_Axis0);
    bool thumbOnButton = controller.GetTouch(EVRButtonId.k_EButton_A);
    float thumbTarget;
    if (thumbOnStick || thumbOnButton)
        thumbTarget = 0.3f;  // Thumb resting on surface — slightly curled
    else if (gripAxis > 0.7f)
        thumbTarget = 1.0f;  // Fist — thumb fully curled
    else
        thumbTarget = 0.0f;  // Thumb extended
    
    // Smooth interpolation
    float dt = Time.deltaTime;
    ctx.currentTriggerVal = Mathf.Lerp(ctx.currentTriggerVal, trigger, dt * 15f);
    ctx.currentGripVal = Mathf.Lerp(ctx.currentGripVal, gripAxis, dt * 15f);
    
    // Per-finger targets with slight stagger for natural look
    float smoothThumb = Mathf.Lerp(ctx.fingerTargets[0], thumbTarget, dt * 10f);
    ctx.fingerTargets[0] = smoothThumb;
    ctx.fingerTargets[1] = ctx.currentTriggerVal;
    ctx.fingerTargets[2] = ctx.currentGripVal;
    ctx.fingerTargets[3] = Mathf.Lerp(ctx.fingerTargets[3], gripAxis, dt * 11f);
    ctx.fingerTargets[4] = Mathf.Lerp(ctx.fingerTargets[4], gripAxis, dt * 9f);

    // Apply with realistic curl angles per finger
    AnimateFinger(ctx.fingers[0], ctx.fingerTargets[0] * 70f);   // Thumb
    AnimateFinger(ctx.fingers[1], ctx.fingerTargets[1] * 85f);   // Index
    AnimateFinger(ctx.fingers[2], ctx.fingerTargets[2] * 90f);   // Middle
    AnimateFinger(ctx.fingers[3], ctx.fingerTargets[3] * 88f);   // Ring
    AnimateFinger(ctx.fingers[4], ctx.fingerTargets[4] * 85f);   // Little
}
```

---

## Critical Constraints (MUST follow)

1. **Unity 5.x API** — no `?.` on Unity objects, no `positionCount`, no `startWidth`
2. **.NET 3.5 runtime** — but LangVersion 11.0 (C# syntax features OK)
3. **SteamVR OLD API** — `SteamVR_Controller.Device`, NOT SteamVR 2.x Input System
4. **DO NOT modify VRGIN_KKCS project or DLL** — all changes in KKCharaStudioVRPlugin only
5. **SDK-style csproj** — auto-includes all `*.cs` files, do NOT add `<Compile Include>` entries
6. **`Owner`** = VRGIN.Controls.Controller (has Model, IsTracking). **`controller`** (lowercase) = SteamVR_Controller.Device (has GetAxis, GetPress, GetTouch)
7. **Quest 3 button mapping**:
   - `k_EButton_A` = A (right), X (left)
   - `k_EButton_ApplicationMenu` = B (right), Y (left)  
   - `k_EButton_Grip` = Grip squeeze
   - `k_EButton_Axis0` = Thumbstick press/touch
   - `k_EButton_Axis1` = Trigger (analog axis in .x)
   - `k_EButton_Axis2` = Possibly analog grip (try this for analog grip value)

## Files to Modify

1. **`GenericStandingMode.cs`** — Remove MenuTool and WarpTool from tool list (3 lines → 1 line change)
2. **`VRHandModelManager.cs`** — Rewrite `UpdateHandAnimation()`, add `fingerTargets` to `HandContext`, adjust curl angles, add palm rotation offset

## Files to Read (Context Only — DO NOT modify)

- `VRGIN_KKCS/VRGIN.Controls/Controller.cs` — Understand tool switching mechanism (line 303-331)
- `VRGIN_KKCS/SteamVR_Controller.cs` — Available API: GetAxis, GetPress, GetTouch
- `GripMoveKKCharaStudioTool.cs` — Understand HandleButtonEvents B/Y usage (line 448-469)
- `GripMenuHandler.cs` — Understand laser/UI interaction (no changes needed)

## Testing Checklist

- [ ] B/Y button press no longer cycles through different tools
- [ ] Only ONE UI panel exists (the internalGui from GripMoveKKCharaStudioTool)
- [ ] No "attached to hand" floating menu from MenuTool appears
- [ ] No teleport arc from WarpTool appears
- [ ] B/Y long-press (1.5s) still resets GUI position
- [ ] Hand model fingers curl smoothly with analog grip (not binary snap)
- [ ] Index finger tracks trigger independently
- [ ] Thumb responds to capacitive touch (curls when not touching any button)
- [ ] Fingers have slightly different curl speeds (natural look)
- [ ] Palm aligns with actual hand position when holding Quest 3 controller
- [ ] All existing features still work (grip move, locomotion, IK grab, UI laser, etc.)

# Phase 4 Agent Task: Proximity IK Grab + Visual Highlight

## Task

Modify `E:\KK_VR\KKCharaStudioVRPlugin\KKCharaStudioVR\GripMoveKKCharaStudioTool.cs` to add proximity-based IK target detection and grab.

## Background

This is a VR plugin for Koikatu Chara Studio. The project is at `E:\KK_VR\KKCharaStudioVRPlugin`.

- Framework: Unity 5.x + BepInEx + VRGIN (old SteamVR API)
- Language: C# (.NET 3.5), csproj LangVersion 11.0
- SDK-style csproj auto-includes all .cs files — do NOT add Compile Include entries

### Current grab system

The existing grab works via Unity trigger collisions:
1. `Controller.cs` (VRGIN) has a `BoxCollider` (trigger, size 0.05x0.05x0.2) on the controller
2. `GripMoveKKCharaStudioTool.OnTriggerStay` detects collision with `MoveableGUIObject` components
3. When user presses Grip while colliding, the tool grabs and moves that object
4. `MoveableGUIObject` is a component on IK target markers created by `IKTool.cs`

The problem: the controller's box collider is small. Users must precisely reach into the tiny IK marker spheres. This feels unnatural in VR.

### What MoveableGUIObject markers look like

Created by `IKTool.InstallGripMoveMarker`:
- Each has a `SphereCollider` with `isTrigger = true`
- Character IK targets have `guideObject != null` (joints, IK handles)
- GUI elements like the internal GUIQuad also have MoveableGUIObject but with `guideObject == null` or no guideObject
- IK markers are on layer `"Studio/Select"`

### Key API references

- `Tool` base class: `Owner` = `VRGIN.Controls.Controller`, `Controller` property = `SteamVR_Controller.Device` (confusing names!)
- `MaterialHelper.GetColorZOrderShader()` returns a shader that renders on top of other geometry
- `Physics.OverlapSphere(Vector3 position, float radius)` returns `Collider[]` including trigger colliders
- The controller transform position is roughly the palm center for Quest 3 controllers

## Changes Required

### 1. Add new fields (after `_lastHandModelEnabled`)

```csharp
private MoveableGUIObject _proximityTarget;
private GameObject _proximityHighlight;
private int _proximityCheckCounter;
```

### 2. Add proximity detection method (after ApplyHandModelState method)

```csharp
private void UpdateProximityDetection()
{
    // Only when hand model is active and not currently grabbing
    if (_settings == null || !_settings.HandModelEnabled || grabbingObject != null)
    {
        ClearProximityHighlight();
        return;
    }

    // Throttle: check every 3 frames to save performance
    _proximityCheckCounter++;
    if (_proximityCheckCounter < 3) 
    {
        // Still update highlight position even on skip frames
        if (_proximityTarget != null && _proximityHighlight != null && _proximityHighlight.activeSelf)
        {
            _proximityHighlight.transform.position = _proximityTarget.transform.position;
        }
        return;
    }
    _proximityCheckCounter = 0;

    Vector3 handPos = ((Component)this).transform.position;
    // Move detection center slightly forward (toward fingers)
    handPos += ((Component)this).transform.forward * 0.05f;

    Collider[] nearby = Physics.OverlapSphere(handPos, 0.12f);

    float nearestDist = float.MaxValue;
    MoveableGUIObject nearest = null;

    foreach (var col in nearby)
    {
        if (col == null) continue;
        MoveableGUIObject mgo = ((Component)col).GetComponent<MoveableGUIObject>();
        // Only consider character IK targets, not GUI elements
        if (mgo == null || (Object)(object)mgo.guideObject == (Object)null) continue;

        float dist = Vector3.Distance(((Component)col).transform.position, handPos);
        if (dist < nearestDist)
        {
            nearestDist = dist;
            nearest = mgo;
        }
    }

    if (nearest != _proximityTarget)
    {
        ClearProximityHighlight();
        _proximityTarget = nearest;
        if (_proximityTarget != null)
        {
            ShowProximityHighlight(_proximityTarget);
        }
    }
    else if (_proximityTarget != null && _proximityHighlight != null && _proximityHighlight.activeSelf)
    {
        // Update position for existing highlight
        _proximityHighlight.transform.position = _proximityTarget.transform.position;
    }
}

private void ShowProximityHighlight(MoveableGUIObject target)
{
    if ((Object)(object)_proximityHighlight == (Object)null)
    {
        _proximityHighlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ((Object)_proximityHighlight).name = "_VRProximityHighlight";
        Object.Destroy(_proximityHighlight.GetComponent<Collider>());
        Renderer r = _proximityHighlight.GetComponent<Renderer>();
        r.material = new Material(MaterialHelper.GetColorZOrderShader());
        r.material.color = new Color(0f, 1f, 0.5f, 0.35f);
        r.material.renderQueue = 3500;
    }
    _proximityHighlight.SetActive(true);
    _proximityHighlight.transform.position = ((Component)target).transform.position;
    _proximityHighlight.transform.localScale = Vector3.one * 0.035f;
}

private void ClearProximityHighlight()
{
    _proximityTarget = null;
    if (_proximityHighlight != null)
        _proximityHighlight.SetActive(false);
}
```

### 3. Call UpdateProximityDetection in OnUpdate

In `OnUpdate`, add the call right BEFORE the `HandleThumbstickLocomotion()` line (after the settings reactivity check block):

```csharp
UpdateProximityDetection();
```

### 4. Modify HandleObjectGrab to support proximity grab

At the very beginning of `HandleObjectGrab`, BEFORE the grabHandle null check, add:

```csharp
// Proximity grab: when grip pressed near character IK target but not directly touching
bool pressDownEarly = controller.GetPressDown(EVRButtonId.k_EButton_Grip);
if (pressDownEarly && !screenGrabbed && _proximityTarget != null)
{
    screenGrabbed = true;
    lastGrabbedObject = ((Component)_proximityTarget).gameObject;
    ClearProximityHighlight();
}
```

IMPORTANT: This block must be BEFORE the existing `bool pressDown = controller.GetPressDown(...)` line. The `GetPressDown` returns true only once per press, so we need to check it before the existing code consumes it.

Wait — actually `GetPressDown` can be called multiple times in the same frame and returns the same value. It's frame-based, not consumption-based. So the order doesn't matter for correctness. But for clarity, add this block right after the grabHandle null-check block (the `if (grabHandle == null)` block), and BEFORE `bool pressDown = ...`:

```csharp
// After grabHandle null-check, before existing pressDown/press/pressUp reads:

// Proximity grab: when grip pressed near character IK target but not directly touching
if (controller.GetPressDown(EVRButtonId.k_EButton_Grip) && !screenGrabbed && _proximityTarget != null)
{
    screenGrabbed = true;
    lastGrabbedObject = ((Component)_proximityTarget).gameObject;
    ClearProximityHighlight();
}
```

### 5. Clear proximity state on tool disable

In `OnDisable`, add before the existing pointer/hand cleanup:

```csharp
ClearProximityHighlight();
```

### 6. Clean up proximity highlight on destroy

In `OnDestroy`, add:

```csharp
if ((Object)(object)_proximityHighlight != (Object)null)
    Object.Destroy((Object)(object)_proximityHighlight);
```

### 7. Clear proximity on level change

In `OnLevel`, add after `StopAllCoroutines`:

```csharp
ClearProximityHighlight();
```

## Verification

- The proximity detection ONLY works when `HandModelEnabled` is true (hand model mode)
- Only `MoveableGUIObject` with non-null `guideObject` are considered (character IK targets only, NOT GUI panels)
- The highlight sphere is green-ish semi-transparent, rendered on top via ZOrder shader
- The detection radius is 0.12m — slightly larger than finger reach but not so large that it interferes with world grip-move when standing near characters
- When grip is pressed with a proximity target, it sets `screenGrabbed = true` and `lastGrabbedObject`, letting the existing grab logic handle the rest
- `Physics.OverlapSphere` is throttled to every 3 frames
- All cleanup paths handled: OnDisable, OnDestroy, OnLevel

Do NOT modify any other files. Do NOT modify the csproj.

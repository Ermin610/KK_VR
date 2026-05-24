# Known Issues & Edge Cases

## Issue 1: Proximity Grab vs World Move Conflict
**Severity: MEDIUM**
**File: GripMoveKKCharaStudioTool.cs line 432**

When user stands near a character and wants to grip-move the world,
if a MoveableGUIObject IK marker is within 0.12m, the proximity grab
fires instead of world-move. User cannot world-move while near characters.

**Fix options:**
a) Reduce radius to 0.08m (less convenient but less conflict)
b) Add settings toggle ProximityGrabEnabled
c) Require trigger+grip combo for proximity grab
d) Only proximity grab if hand is moving slowly (velocity check)

**Recommended:** Option b — let user choose. Default 0.12m with toggle.

## Issue 2: GetComponent Called Every Frame in HandleThumbstickLocomotion
**Severity: LOW (performance)**
**File: GripMoveKKCharaStudioTool.cs line 326**

```csharp
bool isLeft = ((Component)this).GetComponent<VRGIN.Controls.LeftController>() != null;
```

This is called every frame. Should use cached `_isLeftHand` field.
One-line fix: `bool isLeft = _isLeftHand;`

## Issue 3: Dead Code - lastAlpha in VRHandModelManager
**Severity: TRIVIAL**
**File: VRHandModelManager.cs line 31**

`private float lastAlpha = -1f;` is declared but never read.
Remove it.

## Issue 4: Pointer MeshRenderer Color Change When Pointer Hidden
**Severity: LOW**
**File: GripMoveKKCharaStudioTool.cs lines 509-613**

OnTriggerStay sets pointer MeshRenderer color to red.
OnTriggerExit sets it to white.
But when hand model is enabled, pointer is hidden (SetActive false).
The color changes still execute (GetComponent calls on hidden object).
Not a crash bug but wasteful. Could skip color changes when pointer hidden.

## Issue 5: TryUndo May Not Work (Unverified API)
**Severity: UNKNOWN**
**File: VRQuickActions.cs line 216**

`Singleton<UndoRedoManager>.Instance.Undo()` — the Undo() method
is assumed to exist on KK Studio's UndoRedoManager class. We've seen
Push() used in the codebase but never Undo(). It's wrapped in try/catch
so it won't crash, but it might silently fail.

**To verify:** Check Assembly-CSharp.dll with dnSpy or ILSpy for
UndoRedoManager.Undo() method signature.

## Issue 6: VRQuickActions.Update Runs Without Null Check on VR.Mode
**Severity: LOW**  
**File: VRQuickActions.cs line 22-23**

```csharp
var left = VR.Mode.Left;
var right = VR.Mode.Right;
```

If VR.Mode is null (during init), this throws NullReferenceException.
Should add: `if (VR.Mode == null) return;`

## Issue 7: internalGui GUIQuad MoveableGUIObject Has No guideObject
**Severity: INFO (by design)**
**File: GripMoveKKCharaStudioTool.cs line 120**

The internal GUIQuad gets a MoveableGUIObject component but no guideObject
is assigned. This means the proximity grab correctly ignores it
(checks guideObject != null). This is correct behavior but worth noting.

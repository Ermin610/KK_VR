# Phase 5 Plan - Remaining Work for Native Feel

## Priority Analysis

What's DONE:
- Hand models with finger animation ✅
- DynamicBone collision + haptics ✅  
- Thumbstick locomotion (move/turn) ✅
- Settings panel with organized sections ✅
- Per-hand visibility control ✅
- Runtime settings reactivity ✅
- Button conflict fixes ✅
- Proximity IK grab with highlight ✅
- Grab haptic feedback ✅
- UI toggle + menu summon shortcuts ✅
- Undo shortcut ✅

What's still needed for "native feel":

### A. [HIGH] Pointer visibility when hand model OFF + proximity active
Current issue: when pointer is hidden (hand model mode), proximity highlight
is the only visual feedback. But the pointer.SetActive(false) call in 
ApplyHandModelState hides the distance-check sphere. The MeshRenderer color 
feedback in OnTriggerStay/OnTriggerExit still references pointer but it's
invisible. Consider: when hand model is ON, use fingertip position for
the pointer color change instead. Or accept that proximity highlight replaces
pointer feedback entirely.

### B. [HIGH] Proximity grab + world grip-move conflict edge case
When user is standing near a character and wants to world-grip-move:
- If _proximityTarget is found (nearby IK marker), pressing grip grabs that
  target instead of world-moving
- Fix: only engage proximity grab when trigger is ALSO held? Or require
  proximity target to be closer than 0.08m? Current 0.12m might be too large
  for some scenarios.
- Alternative: Add a "proximity grab sensitivity" setting to let users tune it

### C. [MEDIUM] HandleThumbstickLocomotion caches _isLeftHand inefficiently
Line 326 calls GetComponent<LeftController>() every frame in HandleThumbstickLocomotion.
Should use the cached _isLeftHand field instead. Simple one-line fix:
  Change: `bool isLeft = ((Component)this).GetComponent<VRGIN.Controls.LeftController>() != null;`
  To: `bool isLeft = _isLeftHand;`

### D. [MEDIUM] Missing settings: Proximity grab radius + sensitivity
KKCharaStudioVRSettings.cs should have:
- ProximityGrabEnabled (bool, default true)
- ProximityGrabRadius (float, default 0.12)
KKCharaStudioVRGUI.cs should expose these in the settings panel.
GripMoveKKCharaStudioTool.UpdateProximityDetection should read these.

### E. [LOW] VRHandModelManager.lastAlpha field is dead code
Line 31: `private float lastAlpha = -1f;` is never used. Should remove.

### F. [LOW] Redo shortcut
Currently only undo (right stick click). Could add redo via
right stick click while holding trigger, or left stick click as redo.

### G. [LOW] Camera save/load shortcuts
VRCameraMoveHelper has SaveCamera/MoveToCamera for camera slots.
Could add controller shortcuts for quick camera save/load.

### H. [CONSIDERATION] Proximity highlight could pulse/animate
Instead of static green sphere, animate scale (pulse) or alpha to draw attention.
Simple sine wave animation: scale = baseScale * (1 + 0.15 * sin(Time.time * 4))

---

## Suggested Phase 5 Scope

Small focused batch:
1. Fix C (one-line _isLeftHand cache usage)
2. Add D (proximity grab settings)
3. Fix B (add proximity grab radius setting, let user tune conflict)
4. Optional H (highlight pulse animation)

Files to modify:
- GripMoveKKCharaStudioTool.cs (fix C, use setting for radius)
- KKCharaStudioVRSettings.cs (add settings D)
- KKCharaStudioVRGUI.cs (expose settings D)
- VRHandModelManager.cs (remove dead code E - optional)

## Button Layout Reference (Quest 3)

Left Controller:
- Thumbstick axis: Move (forward/back/strafe)
- Thumbstick click: Summon main GUI
- X button (k_EButton_A): Toggle all UI
- Y button (k_EButton_ApplicationMenu): Tool switch (VRGIN built-in)
- Trigger: Click/select UI, select objects
- Grip: Grab world or objects

Right Controller:
- Thumbstick axis: Turn (smooth or snap)
- Thumbstick click: Undo
- A button (k_EButton_A): Toggle all UI
- B button (k_EButton_ApplicationMenu): Tool switch (VRGIN built-in)
- Trigger: Click/select UI, select objects
- Grip: Grab world or objects

Both Controllers (GripMoveKKCharaStudioTool active):
- Grip near IK marker: Proximity grab (when hand model enabled)
- Grip nothing: World move
- Grip + Trigger + Menu held 0.5s: Toggle rotation lock
- Menu held 1.5s: Reset GUI position

## File Inventory (All Modified/New Files)

New files:
- KKCharaStudioVR/VRHandModelManager.cs
- KKCharaStudioVR/DynamicBoneColliderManager.cs
- KKCharaStudioVR/VRHandHapticTrigger.cs

Modified plugin files:
- KKCharaStudioVR/GripMoveKKCharaStudioTool.cs (major changes)
- KKCharaStudioVR/GripMenuHandler.cs (laser origin, dot cursor)
- KKCharaStudioVR/KKCharaStudioVRSettings.cs (new properties)
- KKCharaStudioVR/KKCharaStudioVRGUI.cs (settings UI)
- KKCharaStudioVR/VRQuickActions.cs (button fixes, shortcuts)
- KKCharaStudioVR/IKTool.cs (incremental scanning)
- KKCharaStudioVR/VRLoader.cs (component init)

Modified VRGIN files:
- VRGIN.Controls/Controller.cs (SetRenderModelVisible method)

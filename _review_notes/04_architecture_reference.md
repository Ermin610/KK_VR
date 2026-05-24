# Architecture Quick Reference

## Component Dependency Graph

```
VRLoader.cs (init entry point)
  ├── IKTool.Create()                    → Creates MoveableGUIObject markers on IK targets
  ├── VRControllerMgr.Install()          → Detects Oculus Touch vs Vive
  ├── VRCameraMoveHelper.Install()       → Camera movement helpers
  ├── VRItemObjMoveHelper.Install()      → Object movement + selection
  ├── DynamicBoneColliderManager         → Hand colliders for character physics
  ├── KKCharaStudioVRGUI                 → Settings UI panel
  ├── VRHandModelManager                 → Procedural hand models
  └── VRQuickActions                     → Button shortcuts (UI toggle, summon, undo)

GenericStandingMode (VRGIN mode)
  └── Tools: [MenuTool, WarpTool, GripMoveKKCharaStudioTool]
      └── GripMoveKKCharaStudioTool      → Main VR interaction tool
          ├── GripMenuHandler            → Laser pointer + GUI interaction
          └── Uses: VRHandModelManager, IKTool, VRItemObjMoveHelper, VRCameraMoveHelper
```

## Data Flow: Character IK Manipulation

1. IKTool scans Studio.dicObjectCtrl every 1s (coroutine)
2. For each OCIChar, creates MoveableGUIObject on each GuideObject
3. MoveableGUIObject has SphereCollider (trigger) + guideObject reference
4. Controller BoxCollider (VRGIN) triggers OnTriggerStay in GripMoveKKCharaStudioTool
5. OR: UpdateProximityDetection finds nearby MoveableGUIObject via OverlapSphere
6. Grip press → grabbingObject = target, grabHandle tracks controller
7. Grip hold → grabbingObject position/rotation follows grabHandle
8. MoveableGUIObject.OnMoved() → IKTool.DoOnMove() → updates guideObject.changeAmount
9. Grip release → MoveableGUIObject.OnReleased() → pushes UndoRedoManager command

## Data Flow: Hand Model Visibility

1. VRHandModelManager.CreateHand() → root.SetActive(false) initially
2. GripMoveKKCharaStudioTool.OnEnable → reads HandModelEnabled setting
3. If true: SetHandVisible(isLeft, true) + hide pointer + hide SteamVR model
4. If false: SetHandVisible(isLeft, false) + show pointer + show SteamVR model  
5. OnUpdate: checks for setting changes, calls ApplyHandModelState if changed
6. OnDisable: always restores (show pointer, hide hand, show SteamVR model)
7. Each controller independently manages its own hand (via _isLeftHand flag)

## Data Flow: DynamicBone Collision

1. DynamicBoneColliderManager creates 6 colliders per hand (palm + 5 fingers)
2. Finger colliders parent to VRHandModelManager finger tip transforms (if active)
3. Falls back to controller-relative positions (if hand model inactive)
4. ScanDynamicBonesCo every 1.5s finds all DynamicBone on characters
5. Registers hand colliders into DynamicBone.m_Colliders list
6. DynamicBone physics system uses these colliders for hair/clothing collision
7. VRHandHapticTrigger: OnTriggerStay on SkinnedMeshRenderer/DynamicBone → haptic pulse

## Settings Properties (KKCharaStudioVRSettings.cs)

Movement:
- LockRotXZ (bool, true)
- LocomotionSpeed (float, 2.0)
- SnapTurnAngle (float, 45)
- SnapTurnCooldown (float, 0.3)
- SmoothTurnEnabled (bool, false)
- SmoothTurnSpeed (float, 90)

Hand:
- HandModelEnabled (bool, true)
- HandModelAlpha (float, 0.3)
- HandModelScale (float, 1.0)

Physics:
- DynamicBoneCollisionEnabled (bool, true)
- ColliderRadius (float, 0.02)
- HapticFeedbackEnabled (bool, true)
- HapticFeedbackIntensity (float, 0.5)

Proximity (Phase 5 TODO):
- ProximityGrabEnabled (bool, true)
- ProximityGrabRadius (float, 0.12)

## SteamVR Button IDs (Quest 3 Mapping)

- k_EButton_ApplicationMenu = B/Y buttons (also VRGIN tool switch!)
- k_EButton_A = A/X buttons
- k_EButton_Axis0 = Thumbstick click (GetPressDown) / Thumbstick axis (GetAxis)
- k_EButton_Axis1 = Trigger axis (GetAxis) / Trigger click (GetPress)
- k_EButton_Grip = Grip button
- k_EButton_SteamVR_Touchpad = Same as k_EButton_Axis0

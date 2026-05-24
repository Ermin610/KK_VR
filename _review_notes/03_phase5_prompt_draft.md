# Phase 5 Agent Prompt (Ready to Copy)

## Task: Add proximity grab settings + fix minor issues

Modify 3 files in `E:\KK_VR\KKCharaStudioVRPlugin\KKCharaStudioVR\`:

Background: VR plugin for Koikatu Chara Studio. Unity 5.x, .NET 3.5, LangVersion 11.0.
SDK-style csproj auto-includes all .cs — do NOT add Compile Include entries.
Tool base class: `Owner` = VRGIN.Controls.Controller, `Controller` = SteamVR_Controller.Device.

### File 1: KKCharaStudioVRSettings.cs

Add two new settings after the HapticFeedbackIntensity property block (before the Load method):

```csharp
private bool _ProximityGrabEnabled = true;
private float _ProximityGrabRadius = 0.12f;

[XmlComment("Enable proximity-based IK target grab")]
public bool ProximityGrabEnabled
{
    get { return _ProximityGrabEnabled; }
    set { _ProximityGrabEnabled = value; TriggerPropertyChanged("ProximityGrabEnabled"); }
}

[XmlComment("Radius for proximity grab detection (meters)")]
public float ProximityGrabRadius
{
    get { return _ProximityGrabRadius; }
    set { _ProximityGrabRadius = value; TriggerPropertyChanged("ProximityGrabRadius"); }
}
```

### File 2: KKCharaStudioVRGUI.cs

In the FuncWindowGUI method, after the Haptic Feedback section (after the
`settings.HapticFeedbackIntensity = GUILayout.HorizontalSlider(...)` line
and its closing brace), add:

```csharp
settings.ProximityGrabEnabled = GUILayout.Toggle(settings.ProximityGrabEnabled, "Proximity Grab");
if (settings.ProximityGrabEnabled)
{
    GUILayout.Label($"Grab Radius: {settings.ProximityGrabRadius:F2}m");
    settings.ProximityGrabRadius = GUILayout.HorizontalSlider(settings.ProximityGrabRadius, 0.05f, 0.25f);
}
```

Also in the Reset to Default button handler, add these two lines:
```csharp
settings.ProximityGrabEnabled = true;
settings.ProximityGrabRadius = 0.12f;
```

### File 3: GripMoveKKCharaStudioTool.cs

Three small changes:

**Change 1:** In HandleThumbstickLocomotion (line ~326), replace:
```csharp
bool isLeft = ((Component)this).GetComponent<VRGIN.Controls.LeftController>() != null;
```
With:
```csharp
bool isLeft = _isLeftHand;
```

**Change 2:** In UpdateProximityDetection, replace the condition on the first line:
```csharp
if (_settings == null || !_settings.HandModelEnabled || grabbingObject != null)
```
With:
```csharp
if (_settings == null || !_settings.HandModelEnabled || !_settings.ProximityGrabEnabled || grabbingObject != null)
```

**Change 3:** In UpdateProximityDetection, replace the hardcoded radius:
```csharp
Collider[] nearby = Physics.OverlapSphere(handPos, 0.12f);
```
With:
```csharp
float grabRadius = _settings != null ? _settings.ProximityGrabRadius : 0.12f;
Collider[] nearby = Physics.OverlapSphere(handPos, grabRadius);
```

### File 4 (Optional): VRHandModelManager.cs

Remove the unused field on line 31:
```csharp
private float lastAlpha = -1f;
```

### Verification
- ProximityGrabEnabled defaults to true
- ProximityGrabRadius defaults to 0.12, range 0.05-0.25 in GUI slider
- When ProximityGrabEnabled is false, UpdateProximityDetection clears highlight and returns
- HandleThumbstickLocomotion uses cached _isLeftHand (no per-frame GetComponent)
- Reset to Default includes the two new settings
- Do NOT modify the csproj

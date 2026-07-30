using System.Xml.Serialization;
using VRGIN.Core;

namespace KKCharaStudioVR;

[XmlRoot("Settings")]
public class KKCharaStudioVRSettings : VRSettings
{
	private bool _LockRotXZ = true;
	private float _LocomotionSpeed = 2.0f;
	private float _SnapTurnAngle = 45f;
	private float _SnapTurnCooldown = 0.3f;
	private bool _SmoothTurnEnabled = false;
	private float _SmoothTurnSpeed = 90f;
	private bool _HandModelEnabled = true;
	private float _HandModelAlpha = 0.3f;
	private float _HandModelScale = 1.0f;

	[XmlComment("Lock XZ Axis (pitch / roll) rotation.")]
	public bool LockRotXZ
	{
		get { return _LockRotXZ; }
		set { _LockRotXZ = value; TriggerPropertyChanged("LockRotXZ"); }
	}

	[XmlComment("Speed for thumbstick locomotion")]
	public float LocomotionSpeed
	{
		get { return _LocomotionSpeed; }
		set { _LocomotionSpeed = value; TriggerPropertyChanged("LocomotionSpeed"); }
	}

	[XmlComment("Angle for snap turning")]
	public float SnapTurnAngle
	{
		get { return _SnapTurnAngle; }
		set { _SnapTurnAngle = value; TriggerPropertyChanged("SnapTurnAngle"); }
	}

	[XmlComment("Cooldown time for snap turning")]
	public float SnapTurnCooldown
	{
		get { return _SnapTurnCooldown; }
		set { _SnapTurnCooldown = value; TriggerPropertyChanged("SnapTurnCooldown"); }
	}

	[XmlComment("Enable smooth turning instead of snap turning")]
	public bool SmoothTurnEnabled
	{
		get { return _SmoothTurnEnabled; }
		set { _SmoothTurnEnabled = value; TriggerPropertyChanged("SmoothTurnEnabled"); }
	}

	[XmlComment("Speed for smooth turning (degrees per second)")]
	public float SmoothTurnSpeed
	{
		get { return _SmoothTurnSpeed; }
		set { _SmoothTurnSpeed = value; TriggerPropertyChanged("SmoothTurnSpeed"); }
	}

	[XmlComment("Enable VR hand models")]
	public bool HandModelEnabled
	{
		get { return _HandModelEnabled; }
		set { _HandModelEnabled = value; TriggerPropertyChanged("HandModelEnabled"); }
	}

	[XmlComment("Alpha transparency for VR hand models")]
	public float HandModelAlpha
	{
		get { return _HandModelAlpha; }
		set { _HandModelAlpha = value; TriggerPropertyChanged("HandModelAlpha"); }
	}

	[XmlComment("Scale of VR hand models")]
	public float HandModelScale
	{
		get { return _HandModelScale; }
		set { _HandModelScale = value; TriggerPropertyChanged("HandModelScale"); }
	}

	private bool _DynamicBoneCollisionEnabled = true;
	private float _ColliderRadius = 0.02f;
	private bool _HapticFeedbackEnabled = true;
	private float _HapticFeedbackIntensity = 0.5f;
	private bool _PhysicsHandsEnabled = true;
	private bool _VibrateOnlyOnBreasts = true;

	[XmlComment("Enable VAM-style non-clipping physics hands")]
	public bool PhysicsHandsEnabled
	{
		get { return _PhysicsHandsEnabled; }
		set { _PhysicsHandsEnabled = value; TriggerPropertyChanged("PhysicsHandsEnabled"); }
	}

	[XmlComment("Enable DynamicBone collision on hands")]
	public bool DynamicBoneCollisionEnabled
	{
		get { return _DynamicBoneCollisionEnabled; }
		set { _DynamicBoneCollisionEnabled = value; TriggerPropertyChanged("DynamicBoneCollisionEnabled"); }
	}

	[XmlComment("Hand collider radius")]
	public float ColliderRadius
	{
		get { return _ColliderRadius; }
		set { _ColliderRadius = value; TriggerPropertyChanged("ColliderRadius"); }
	}

	[XmlComment("Enable haptic feedback when touching characters")]
	public bool HapticFeedbackEnabled
	{
		get { return _HapticFeedbackEnabled; }
		set { _HapticFeedbackEnabled = value; TriggerPropertyChanged("HapticFeedbackEnabled"); }
	}

	[XmlComment("Haptic feedback intensity (0.0 to 1.0)")]
	public float HapticFeedbackIntensity
	{
		get { return _HapticFeedbackIntensity; }
		set { _HapticFeedbackIntensity = value; TriggerPropertyChanged("HapticFeedbackIntensity"); }
	}

	[XmlComment("Only vibrate when touching breasts")]
	public bool VibrateOnlyOnBreasts
	{
		get { return _VibrateOnlyOnBreasts; }
		set { _VibrateOnlyOnBreasts = value; TriggerPropertyChanged("VibrateOnlyOnBreasts"); }
	}

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

	private float _UISpawnDistance = 2.0f;

	[XmlComment("Distance in front of head when UI respawns (meters)")]
	public float UISpawnDistance
	{
		get { return _UISpawnDistance; }
		set { _UISpawnDistance = value; TriggerPropertyChanged("UISpawnDistance"); }
	}

	private bool _ComfortVignetteEnabled = true;
	private float _ComfortVignetteRadius = 0.5f;
	private bool _TwoHandScaleEnabled = true;
	private bool _WristMenuEnabled = true;
	private float _WristMenuScale = 1.0f;
	private bool _CameraSyncEnabled = true;
	private bool _CameraSyncPreserveHeadTracking = true;
	private bool _CameraSyncAlignOnSceneLoad = true;
	private VRCameraRotationMode _CameraSyncInitialRotationMode = VRCameraRotationMode.YawOnly;
	private VRCameraRotationMode _CameraSyncRotationMode = VRCameraRotationMode.YawOnly;
	private VRCameraPositionFollowMode _CameraSyncPositionMode = VRCameraPositionFollowMode.AllMotion;
	private float _CameraSyncCutThreshold = 2.0f;
	private bool _CameraSyncReadObjectCamera = true;

	[XmlComment("Enable comfort vignette during movement")]
	public bool ComfortVignetteEnabled
	{
		get { return _ComfortVignetteEnabled; }
		set { _ComfortVignetteEnabled = value; TriggerPropertyChanged("ComfortVignetteEnabled"); }
	}

	[XmlComment("Vignette clear radius (0.3 = strong, 0.8 = subtle)")]
	public float ComfortVignetteRadius
	{
		get { return _ComfortVignetteRadius; }
		set { _ComfortVignetteRadius = value; TriggerPropertyChanged("ComfortVignetteRadius"); }
	}

	[XmlComment("Enable two-hand world scaling")]
	public bool TwoHandScaleEnabled
	{
		get { return _TwoHandScaleEnabled; }
		set { _TwoHandScaleEnabled = value; TriggerPropertyChanged("TwoHandScaleEnabled"); }
	}

	[XmlComment("Enable the compact left-wrist quick menu")]
	public bool WristMenuEnabled
	{
		get { return _WristMenuEnabled; }
		set { _WristMenuEnabled = value; TriggerPropertyChanged("WristMenuEnabled"); }
	}

	[XmlComment("Scale of the compact wrist menu")]
	public float WristMenuScale
	{
		get { return _WristMenuScale; }
		set { _WristMenuScale = value; TriggerPropertyChanged("WristMenuScale"); }
	}

	[XmlComment("Follow the final Studio camera, including Timeline-driven camera motion")]
	public bool CameraSyncEnabled
	{
		get { return _CameraSyncEnabled; }
		set { _CameraSyncEnabled = value; TriggerPropertyChanged("CameraSyncEnabled"); }
	}

	[XmlComment("Preserve physical headset movement relative to the animated Studio camera")]
	public bool CameraSyncPreserveHeadTracking
	{
		get { return _CameraSyncPreserveHeadTracking; }
		set
		{
			_CameraSyncPreserveHeadTracking = value;
			TriggerPropertyChanged("CameraSyncPreserveHeadTracking");
		}
	}

	[XmlComment("Align the headset once to a scene card's saved camera after loading")]
	public bool CameraSyncAlignOnSceneLoad
	{
		get { return _CameraSyncAlignOnSceneLoad; }
		set
		{
			_CameraSyncAlignOnSceneLoad = value;
			TriggerPropertyChanged("CameraSyncAlignOnSceneLoad");
		}
	}

	[XmlComment("Rotation used for the one-time scene-load camera alignment")]
	public VRCameraRotationMode CameraSyncInitialRotationMode
	{
		get { return _CameraSyncInitialRotationMode; }
		set
		{
			_CameraSyncInitialRotationMode = value;
			TriggerPropertyChanged("CameraSyncInitialRotationMode");
		}
	}

	[XmlComment("Rotation followed from the final Studio camera")]
	public VRCameraRotationMode CameraSyncRotationMode
	{
		get { return _CameraSyncRotationMode; }
		set
		{
			_CameraSyncRotationMode = value;
			TriggerPropertyChanged("CameraSyncRotationMode");
		}
	}

	[XmlComment("Translation mode for final Studio camera movement")]
	public VRCameraPositionFollowMode CameraSyncPositionMode
	{
		get { return _CameraSyncPositionMode; }
		set
		{
			_CameraSyncPositionMode = value;
			TriggerPropertyChanged("CameraSyncPositionMode");
		}
	}

	[XmlComment("World-space distance required for CutsOnly camera translation")]
	public float CameraSyncCutThreshold
	{
		get { return _CameraSyncCutThreshold; }
		set
		{
			_CameraSyncCutThreshold = value;
			TriggerPropertyChanged("CameraSyncCutThreshold");
		}
	}

	[XmlComment("Read the active Studio camera object when one is selected")]
	public bool CameraSyncReadObjectCamera
	{
		get { return _CameraSyncReadObjectCamera; }
		set
		{
			_CameraSyncReadObjectCamera = value;
			TriggerPropertyChanged("CameraSyncReadObjectCamera");
		}
	}

	private float _HandOffsetX = 0f;
	private float _HandOffsetY = -0.02f;
	private float _HandOffsetZ = -0.05f;

	[XmlComment("Hand model X offset")]
	public float HandOffsetX
	{
		get { return _HandOffsetX; }
		set { _HandOffsetX = value; TriggerPropertyChanged("HandOffsetX"); }
	}

	[XmlComment("Hand model Y offset")]
	public float HandOffsetY
	{
		get { return _HandOffsetY; }
		set { _HandOffsetY = value; TriggerPropertyChanged("HandOffsetY"); }
	}

	[XmlComment("Hand model Z offset")]
	public float HandOffsetZ
	{
		get { return _HandOffsetZ; }
		set { _HandOffsetZ = value; TriggerPropertyChanged("HandOffsetZ"); }
	}

	private float _HandRotPitch = 30f;
	private float _HandRotYaw = 0f;
	private float _HandRotRoll = 0f;

	[XmlComment("Hand model Pitch rotation offset")]
	public float HandRotPitch
	{
		get { return _HandRotPitch; }
		set { _HandRotPitch = value; TriggerPropertyChanged("HandRotPitch"); }
	}

	[XmlComment("Hand model Yaw rotation offset")]
	public float HandRotYaw
	{
		get { return _HandRotYaw; }
		set { _HandRotYaw = value; TriggerPropertyChanged("HandRotYaw"); }
	}

	[XmlComment("Hand model Roll rotation offset")]
	public float HandRotRoll
	{
		get { return _HandRotRoll; }
		set { _HandRotRoll = value; TriggerPropertyChanged("HandRotRoll"); }
	}

	public static KKCharaStudioVRSettings Load(string path)
	{
		return VRSettings.Load<KKCharaStudioVRSettings>(path);
	}
}

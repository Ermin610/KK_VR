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

	private bool _ComfortVignetteEnabled = true;
	private float _ComfortVignetteRadius = 0.5f;
	private bool _TwoHandScaleEnabled = true;

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

	public static KKCharaStudioVRSettings Load(string path)
	{
		return VRSettings.Load<KKCharaStudioVRSettings>(path);
	}
}

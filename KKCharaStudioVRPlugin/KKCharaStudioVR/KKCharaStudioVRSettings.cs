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

	public static KKCharaStudioVRSettings Load(string path)
	{
		return VRSettings.Load<KKCharaStudioVRSettings>(path);
	}
}

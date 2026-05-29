using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VRGIN.Helpers;

public class VelocityRumble : IRumbleSession, IComparable<IRumbleSession>
{
	private readonly ushort _MicroDuration;

	private readonly float _MilliInterval;

	private readonly float _MaxVelocity;

	private readonly ushort _MaxMicroDuration;

	private readonly float _MaxMilliInterval;

	public bool IsOver { get; set; }

	public ushort MicroDuration
	{
		get
		{
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			float num = (int)_MicroDuration;
			Vector3 velocity = Device.velocity;
			return (ushort)(num + velocity.magnitude / _MaxVelocity * (float)(_MaxMicroDuration - _MicroDuration));
		}
	}

	public float MilliInterval
	{
		get
		{
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			float milliInterval = _MilliInterval;
			float maxMilliInterval = _MaxMilliInterval;
			Vector3 velocity = Device.velocity;
			return Mathf.Lerp(milliInterval, maxMilliInterval, velocity.magnitude / _MaxVelocity);
		}
	}

	public SteamVR_Controller.Device Device { get; set; }

	public VelocityRumble(SteamVR_Controller.Device device, ushort microDuration, float milliInterval, float maxVelocity, ushort maxMicroDuration, float maxMilliInterval)
	{
		Device = device;
		_MaxMilliInterval = maxMilliInterval;
		_MaxMicroDuration = maxMicroDuration;
		_MaxVelocity = maxVelocity;
		_MilliInterval = milliInterval;
		_MicroDuration = microDuration;
	}

	public int CompareTo(IRumbleSession other)
	{
		return MicroDuration.CompareTo(other.MicroDuration);
	}

	public void Consume()
	{
	}
}

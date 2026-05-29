using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VRGIN.Helpers;

public class RumbleSession : IRumbleSession, IComparable<IRumbleSession>
{
	private float _Time;

	public bool IsOver { get; private set; }

	public ushort MicroDuration { get; set; }

	public float MilliInterval { get; set; }

	public float Lifetime { get; set; }

	public RumbleSession(ushort microDuration, float milliInterval)
	{
		MicroDuration = microDuration;
		MilliInterval = milliInterval;
		_Time = Time.time;
	}

	public RumbleSession(ushort microDuration, float milliInterval, float lifetime)
	{
		MicroDuration = microDuration;
		MilliInterval = milliInterval;
		Lifetime = lifetime;
		_Time = Time.time;
	}

	public void Close()
	{
		IsOver = true;
	}

	public int CompareTo(IRumbleSession other)
	{
		return MicroDuration.CompareTo(other.MicroDuration);
	}

	public void Restart()
	{
		_Time = Time.time;
	}

	public void Consume()
	{
		if (Lifetime > 0f && Time.time - _Time > Lifetime)
		{
			IsOver = true;
		}
	}
}

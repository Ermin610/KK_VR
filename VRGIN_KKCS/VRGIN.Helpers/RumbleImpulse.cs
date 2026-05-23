using System;

namespace VRGIN.Helpers;

public class RumbleImpulse : IRumbleSession, IComparable<IRumbleSession>
{
	private bool _Over;

	public bool IsOver => _Over;

	public ushort MicroDuration { get; set; }

	public float MilliInterval => 0f;

	public void Consume()
	{
		_Over = true;
	}

	public RumbleImpulse(ushort strength)
	{
		MicroDuration = strength;
	}

	public int CompareTo(IRumbleSession other)
	{
		return MicroDuration.CompareTo(other.MicroDuration);
	}
}

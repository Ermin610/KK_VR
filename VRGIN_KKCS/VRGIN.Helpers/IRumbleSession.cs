using System;

namespace VRGIN.Helpers;

public interface IRumbleSession : IComparable<IRumbleSession>
{
	bool IsOver { get; }

	ushort MicroDuration { get; }

	float MilliInterval { get; }

	void Consume();
}

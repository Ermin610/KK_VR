using System;
using VRGIN.Modes;

namespace VRGIN.Core;

public class ModeInitializedEventArgs : EventArgs
{
	public ControlMode Mode { get; private set; }

	public ModeInitializedEventArgs(ControlMode mode)
	{
		Mode = mode;
	}
}

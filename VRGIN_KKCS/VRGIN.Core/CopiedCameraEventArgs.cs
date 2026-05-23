using System;
using UnityEngine;

namespace VRGIN.Core;

public class CopiedCameraEventArgs : EventArgs
{
	public readonly Camera Camera;

	public CopiedCameraEventArgs(Camera camera)
	{
		Camera = camera;
	}
}

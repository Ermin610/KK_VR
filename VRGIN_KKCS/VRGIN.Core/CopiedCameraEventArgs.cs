using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VRGIN.Core;

public class CopiedCameraEventArgs : EventArgs
{
	public readonly Camera Camera;

	public CopiedCameraEventArgs(Camera camera)
	{
		Camera = camera;
	}
}

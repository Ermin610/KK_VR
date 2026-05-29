using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VRGIN.Core;

public class InitializeCameraEventArgs : EventArgs
{
	public readonly Camera Camera;

	public readonly Camera Blueprint;

	public InitializeCameraEventArgs(Camera camera, Camera blueprint)
	{
		Camera = camera;
		Blueprint = blueprint;
	}
}

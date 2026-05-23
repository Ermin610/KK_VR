using System;
using UnityEngine;

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

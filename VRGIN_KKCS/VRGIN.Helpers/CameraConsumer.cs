using System.Collections.Generic;
using UnityEngine;
using VRGIN.Core;

namespace VRGIN.Helpers;

public class CameraConsumer : IScreenGrabber
{
	private RenderTexture _Texture;

	private bool _SpareMainCamera;

	private bool _SoftMode;

	public bool Check(Camera camera)
	{
		if (!(((Component)camera).GetComponent("UICamera") != null) && !((Object)camera).name.Contains("VR") && camera.targetTexture == null)
		{
			if (((Component)camera).CompareTag("MainCamera"))
			{
				return !_SpareMainCamera;
			}
			return true;
		}
		return false;
	}

	public IEnumerable<RenderTexture> GetTextures()
	{
		yield return _Texture;
	}

	public void OnAssign(Camera camera)
	{
		if (_SoftMode)
		{
			camera.cullingMask = 0;
			camera.nearClipPlane = 1f;
			camera.farClipPlane = 1f;
		}
		else
		{
			((Behaviour)camera).enabled = false;
		}
	}

	public CameraConsumer(bool spareMainCamera = false, bool softMode = false)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		_SoftMode = softMode;
		_SpareMainCamera = spareMainCamera;
		_Texture = new RenderTexture(1, 1, 0);
	}
}

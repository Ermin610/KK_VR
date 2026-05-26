using System;
using UnityEngine;

namespace KKCharaStudioVR;

internal class MaterialHelper
{
	private static AssetBundle _GripMovePluginResources;

	private static Shader _ColorZOrderShader;

	public static Shader GetColorZOrderShader()
	{
		if (_ColorZOrderShader != null)
		{
			return _ColorZOrderShader;
		}
		try
		{
			if (_GripMovePluginResources == null)
			{
				_GripMovePluginResources = AssetBundle.LoadFromMemory(Resource.kkcharastudiovrshader);
			}
			_ColorZOrderShader = _GripMovePluginResources.LoadAsset<Shader>("ColorZOrder");
			return _ColorZOrderShader;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
			return null;
		}
	}
}

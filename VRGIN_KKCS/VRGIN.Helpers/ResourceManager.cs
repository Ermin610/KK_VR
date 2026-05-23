using System.Linq;
using UnityEngine;

namespace VRGIN.Helpers;

public static class ResourceManager
{
	private static readonly string VERSION = string.Join(".", Application.unityVersion.Split('.').Take(2).ToArray());

	public static byte[] SteamVR
	{
		get
		{
			if (VERSION.CompareTo("5.0") <= 0)
			{
				return Resource.vrgin_5_0;
			}
			if (VERSION.CompareTo("5.2") <= 0)
			{
				return Resource.vrgin_5_2;
			}
			if (VERSION.CompareTo("5.3") <= 0)
			{
				return Resource.vrgin_5_3;
			}
			if (VERSION.CompareTo("5.4") <= 0)
			{
				return Resource.vrgin_5_4;
			}
			if (VERSION.CompareTo("5.5") <= 0)
			{
				return Resource.vrgin_5_5;
			}
			return Resource.vrgin_5_6;
		}
	}

	public static byte[] Capture => SteamVR;

	public static byte[] Hands => Resource.hands_5_3;
}

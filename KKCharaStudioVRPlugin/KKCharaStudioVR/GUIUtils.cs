using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KKCharaStudioVR;

public class GUIUtils
{
	private static bool isVR;

	private static Texture2D windowBG;

	static GUIUtils()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		isVR = false;
		windowBG = new Texture2D(1, 1, (TextureFormat)5, false);
		if ((Environment.CommandLine.Contains("--vr") || Environment.CommandLine.Contains("--studiovr")) && !Environment.CommandLine.Contains("--novr"))
		{
			isVR = true;
		}
		windowBG.SetPixel(0, 0, Color.black);
		windowBG.Apply();
	}

	public static GUIStyle GetWindowStyle()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		GUIStyle val = new GUIStyle(GUI.skin.window);
		if (isVR)
		{
			GUI.backgroundColor = Color.black;
			val.onNormal.background = windowBG;
			val.normal.background = windowBG;
			val.hover.background = windowBG;
			val.focused.background = windowBG;
			val.active.background = windowBG;
			val.hover.textColor = Color.blue;
			val.onHover.textColor = Color.blue;
		}
		return val;
	}
}

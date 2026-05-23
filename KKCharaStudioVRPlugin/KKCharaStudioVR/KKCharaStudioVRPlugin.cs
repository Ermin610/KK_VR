using System;
using BepInEx;
using VRGIN.Helpers;

namespace KKCharaStudioVR;

[BepInProcess("CharaStudio")]
[BepInPlugin("KKCharaStudioVRPlugin.KKCharaStudioVRPlugin", "KKCharaStudioVRPlugin", "0.0.3")]
public class KKCharaStudioVRPlugin : BaseUnityPlugin
{
	public const string NAME = "KKCharaStudioVRPlugin";

	public const string VERSION = "0.0.3";

	public KKCharaStudioVRPlugin()
	{
		bool flag = Environment.CommandLine.Contains("--novr");
		if (Environment.CommandLine.Contains("--studiovr") || (!flag && SteamVRDetector.IsRunning))
		{
			VRLoader.Create(isEnable: true);
			SaveLoadSceneHook.InstallHook();
			LoadFixHook.InstallHook();
		}
		else
		{
			VRLoader.Create(isEnable: false);
		}
	}
}

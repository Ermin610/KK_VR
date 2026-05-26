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
			DropdownFixHook.InstallHook();
		}
		else
		{
			VRLoader.Create(isEnable: false);
		}
	}

	[System.Runtime.InteropServices.DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(IntPtr hWnd);

	[System.Runtime.InteropServices.DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

	public void Start()
	{
		// Force the window to the foreground so VR UI interaction works without alt-tabbing or clicking
		try
		{
			var process = System.Diagnostics.Process.GetCurrentProcess();
			if (process != null && process.MainWindowHandle != IntPtr.Zero)
			{
				ShowWindow(process.MainWindowHandle, 5); // 5 = SW_SHOW
				SetForegroundWindow(process.MainWindowHandle);
			}
		}
		catch (Exception e)
		{
			UnityEngine.Debug.LogWarning("[KKCharaStudioVRPlugin] Failed to set foreground window: " + e.Message);
		}
	}
}

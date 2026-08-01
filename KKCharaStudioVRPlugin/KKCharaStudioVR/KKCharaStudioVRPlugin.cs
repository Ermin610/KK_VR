using System;
using System.Runtime.InteropServices;
using BepInEx;
using VRGIN.Core;
using VRGIN.Helpers;

namespace KKCharaStudioVR;

[BepInProcess("CharaStudio")]
[BepInPlugin("KKCharaStudioVRPlugin.KKCharaStudioVRPlugin", "KKCharaStudioVRPlugin", "0.0.17")]
public class KKCharaStudioVRPlugin : BaseUnityPlugin
{
	public const string NAME = "KKCharaStudioVRPlugin";

	public const string VERSION = "0.0.17";

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

	public KKCharaStudioVRPlugin()
	{
		bool flag = Environment.CommandLine.Contains("--novr");
		if (Environment.CommandLine.Contains("--studiovr") || (!flag && SteamVRDetector.IsRunning))
		{
			// NOTE: Do NOT pre-load openvr_api.dll or call LoadDeviceByName here.
			// Doing so causes an extra VR_Init -> VR_Shutdown cycle that breaks
			// ReShade's IVRCompositor::Submit hooks. For ReShade VR to work,
			// VR must be initialized at engine startup via boot.config or the
			// globalgamemanagers patch. See StartCharaStudioVR.bat.
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

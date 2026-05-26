using System;
using BepInEx.Logging;
using BepInEx4;
using Harmony;
using Studio;
using VRGIN.Core;
using Logger = BepInEx4.Logger;

namespace KKCharaStudioVR;

public static class LoadFixHook
{
	public static bool forceSetStandingMode;

	public static void InstallHook()
	{
		HarmonyInstance.Create("KKChacaStudioVR.LoadFixHook").PatchAll(typeof(LoadFixHook));
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(SceneLoadScene), "OnClickLoad", new Type[] { }, null)]
	public static bool LoadScenePreHook(SceneLoadScene __instance)
	{
		Logger.Log((LogLevel)32, (object)"Start Scene Loading.");
		if (VRManager.Instance.Mode is GenericStandingMode)
		{
			(VR.Manager.Interpreter as KKCharaStudioInterpreter).ForceResetVRMode();
		}
		return true;
	}
}

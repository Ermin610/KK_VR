using System;
using System.Reflection;
using BepInEx.Logging;
using BepInEx4;
using Harmony;
using Studio;
using UnityEngine;
using VRGIN.Core;

namespace KKCharaStudioVR;

public static class SaveLoadSceneHook
{
	private static Camera[] backupRenderCam;

	public static void InstallHook()
	{
		Logger.Log((LogLevel)16, (object)"Install SaveLoadSceneHook");
		HarmonyInstance.Create("KKChacaStudioVR.SaveLoadSceneHook").PatchAll(typeof(SaveLoadSceneHook));
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(Studio), "SaveScene", new Type[] { }, null)]
	public static bool SaveScenePreHook(Studio __instance, ref Camera[] __state)
	{
		Logger.Log((LogLevel)32, (object)"Update Camera position and rotation for Scene Capture and last Camera data.");
		VRCameraMoveHelper.Instance.CurrentToCameraCtrl();
		FieldInfo field = typeof(GameScreenShot).GetField("renderCam", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		Camera[] obj = field.GetValue(Singleton<Studio>.Instance.gameScreenShot) as Camera[];
		Logger.Log((LogLevel)32, (object)"Backup Screenshot render cam.");
		backupRenderCam = obj;
		Camera[] value = (Camera[])(object)new Camera[1] { VR.Camera.SteamCam.camera };
		__state = backupRenderCam;
		field.SetValue(Singleton<Studio>.Instance.gameScreenShot, value);
		return true;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Studio), "SaveScene", new Type[] { }, null)]
	public static void SaveScenePostHook(Studio __instance, Camera[] __state)
	{
		Logger.Log((LogLevel)32, (object)"Restore backup render cam.");
		typeof(GameScreenShot).GetField("renderCam", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(Singleton<Studio>.Instance.gameScreenShot, __state);
	}
}

using System;
using System.Collections;
using BepInEx.Logging;
using BepInEx4;
using Harmony;
using Studio;
using UnityEngine;
using VRGIN.Core;
using VRGIN.Visuals;
using Logger = BepInEx4.Logger;

namespace KKCharaStudioVR
{
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
			PrepareSceneLoad();
			return true;
		}

		public static void PrepareSceneLoad()
		{
			Logger.Log((LogLevel)32, (object)"Start Scene Loading.");
			if (VRManager.Instance != null && VRManager.Instance.Mode is GenericStandingMode)
			{
				KKCharaStudioInterpreter interpreter =
					VR.Manager?.Interpreter as KKCharaStudioInterpreter;
				interpreter?.ForceResetVRMode();
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Studio.Studio), "LoadScene", new Type[] { typeof(string) }, null)]
		public static void LoadScenePostHook(Studio.Studio __instance, bool __result)
		{
			if (__result)
			{
				CompleteSceneLoad(__instance);
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Studio.Studio), "ImportScene", new Type[] { typeof(string) }, null)]
		public static void ImportScenePostHook(Studio.Studio __instance, bool __result)
		{
			if (__result)
			{
				CompleteSceneLoad(__instance);
			}
		}

		public static void CompleteSceneLoad(Studio.Studio studio)
		{
			if (studio == null)
				return;
			Logger.Log((LogLevel)32, (object)"Scene loaded successfully. Starting camera alignment coroutine.");
			studio.StartCoroutine(AlignVRCameraAfterLoadCo());
		}

		private static IEnumerator AlignVRCameraAfterLoadCo()
		{
			Logger.Log((LogLevel)32, (object)"AlignVRCameraAfterLoadCo: Waiting for scene load to register...");
			yield return null;
			yield return null;

			var sceneManager = Singleton<Manager.Scene>.Instance;
			if (sceneManager != null)
			{
				while (sceneManager.IsNowLoading || sceneManager.IsNowLoadingFade)
				{
					yield return null;
				}
			}

			Logger.Log((LogLevel)32, (object)"AlignVRCameraAfterLoadCo: Scene load finished, waiting for camera data to import...");
			// Wait a few frames for imported camera parameters to be applied to mainCamera
			yield return null;
			yield return null;
			yield return null;
			yield return null;
			yield return null;

			Logger.Log((LogLevel)32, (object)"AlignVRCameraAfterLoadCo: Re-aligning VR camera to new camera position.");
			if (VRCameraMoveHelper.Instance != null)
			{
				VRCameraMoveHelper.Instance.MoveToCurrent();
			}

			Logger.Log((LogLevel)32, (object)"AlignVRCameraAfterLoadCo: Repositioning the main floating studio UI quad in front of the camera.");
			float dist = 0.5f;
			var settings = VR.Manager.Context.Settings as KKCharaStudioVRSettings;
			if (settings != null)
			{
				dist = settings.UISpawnDistance;
			}
			if (dist > 0.6f) dist = 0.5f; // Clamp to comfortable distance
			VRCameraMoveHelper.RepositionMainUI(dist);
		}
	}
}

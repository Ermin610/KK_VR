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
		private static int _uiRefreshGeneration;

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
			VRCameraSyncController.Instance?.BeginSceneLoad();

			if (VRManager.Instance != null && VRManager.Instance.Mode is GenericStandingMode)
			{
				KKCharaStudioInterpreter interpreter =
					VR.Manager?.Interpreter as KKCharaStudioInterpreter;
				interpreter?.ForceResetVRMode();
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Studio.Studio), "LoadSceneCoroutine", new Type[] { typeof(string) }, null)]
		public static void LoadSceneCoroutinePostHook(
			Studio.Studio __instance,
			ref IEnumerator __result)
		{
			if (__instance == null || __result == null)
			{
				return;
			}

			object previousSceneInfo = __instance.sceneInfo;
			VRCameraSyncController.Instance?.BeginSceneLoad();
			__result = CompleteStudioLoadCoroutine(
				__instance,
				__result,
				previousSceneInfo);
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Studio.Studio), "LoadScene", new Type[] { typeof(string) }, null)]
		public static void StudioLoadScenePreHook()
		{
			VRCameraSyncController.Instance?.BeginSceneLoad();
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Studio.Studio), "LoadScene", new Type[] { typeof(string) }, null)]
		public static void LoadScenePostHook(Studio.Studio __instance, bool __result)
		{
			VRCameraSyncController.Instance?.CompleteSceneLoad(__result);
			if (__result)
			{
				CompleteSceneLoad(__instance);
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Studio.Studio), "ImportScene", new Type[] { typeof(string) }, null)]
		public static void ImportScenePreHook()
		{
			VRCameraSyncController.Instance?.BeginSceneImport();
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Studio.Studio), "ImportScene", new Type[] { typeof(string) }, null)]
		public static void ImportScenePostHook(Studio.Studio __instance, bool __result)
		{
			// ImportScene adds objects to the current scene and does not import
			// cameraSaveData. Reset the observer without teleporting the player.
			VRCameraSyncController.Instance?.CompleteSceneImport(__result);
			if (__result)
			{
				CompleteSceneLoad(__instance);
			}
		}

		public static void CompleteSceneLoad(Studio.Studio studio)
		{
			if (studio == null)
				return;
			int generation = ++_uiRefreshGeneration;
			Logger.Log((LogLevel)32, (object)"Scene mutation completed. Scheduling VR UI refresh.");
			studio.StartCoroutine(RefreshVRAfterLoadCo(generation));
		}

		private static IEnumerator CompleteStudioLoadCoroutine(
			Studio.Studio studio,
			IEnumerator original,
			object previousSceneInfo)
		{
			bool completed = false;
			try
			{
				while (original.MoveNext())
				{
					yield return original.Current;
				}
				completed = true;
			}
			finally
			{
				IDisposable disposable = original as IDisposable;
				disposable?.Dispose();

				bool sceneChanged =
					studio != null &&
					!ReferenceEquals(studio.sceneInfo, previousSceneInfo);
				bool succeeded = completed && sceneChanged;
				VRCameraSyncController.Instance?.CompleteSceneLoad(succeeded);
				if (succeeded)
				{
					CompleteSceneLoad(studio);
				}
			}
		}

		private static IEnumerator RefreshVRAfterLoadCo(int generation)
		{
			Logger.Log((LogLevel)32, (object)"RefreshVRAfterLoadCo: Waiting for scene load to register...");
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

			Logger.Log((LogLevel)32, (object)"RefreshVRAfterLoadCo: Waiting for Studio UI and camera data.");
			yield return null;
			yield return null;
			yield return null;
			yield return null;
			yield return null;

			if (generation != _uiRefreshGeneration)
			{
				yield break;
			}

			// The integrated sync controller owns camera alignment. Keep this
			// fallback for installations where the controller could not start.
			if (VRCameraSyncController.Instance == null &&
			    VRCameraMoveHelper.Instance != null)
			{
				VRCameraMoveHelper.Instance.MoveToCurrent();
			}

			Logger.Log((LogLevel)32, (object)"RefreshVRAfterLoadCo: Repositioning the main Studio UI.");
			float dist = 2.0f;
			var settings = VR.Manager.Context.Settings as KKCharaStudioVRSettings;
			if (settings != null)
			{
				dist = settings.UISpawnDistance;
			}
			dist = Mathf.Clamp(dist, 0.5f, 3.0f);
			VRCameraMoveHelper.RepositionMainUI(dist);
		}
	}
}

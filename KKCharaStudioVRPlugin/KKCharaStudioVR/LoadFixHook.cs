using System;
using System.Collections;
using System.IO;
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
		private static int sceneTransitionGeneration;
		private static int activeSceneTransitionGeneration;
		private static int lastCompletedSceneTransitionGeneration;
		private static bool sceneTransitionActive;

		public static void InstallHook()
		{
			HarmonyInstance.Create("KKChacaStudioVR.LoadFixHook").PatchAll(typeof(LoadFixHook));
		}

		public static void PrepareSceneLoad(string source = "scene load")
		{
			BeginSceneTransition(source, true);
		}

		private static int BeginSceneTransition(string source, bool reuseActiveTransition)
		{
			if (reuseActiveTransition && sceneTransitionActive)
				return activeSceneTransitionGeneration;

			activeSceneTransitionGeneration = ++sceneTransitionGeneration;
			sceneTransitionActive = true;
			Logger.Log((LogLevel)32,
				(object)("Start Scene Loading: " + source + " (generation "
					+ activeSceneTransitionGeneration + ")."));

			if (VR.Active)
			{
				VRMmdPlaybackController.PrepareForSceneTransition();
				VRTimelineCameraFollowController.BeginSceneTransition(
					activeSceneTransitionGeneration);
				VRHandModelManager.SetPresentationSuppressionRequested(true);
			}
			return activeSceneTransitionGeneration;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Studio.Studio), "LoadScene", new Type[] { typeof(string) }, null)]
		public static void LoadScenePreHook(out int __state)
		{
			__state = BeginSceneTransition("Studio.LoadScene", false);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Studio.Studio), "LoadScene", new Type[] { typeof(string) }, null)]
		public static void LoadScenePostHook(
			Studio.Studio __instance,
			bool __result,
			int __state)
		{
			// A true return means only that Studio accepted the request. The
			// wrapped LoadSceneCoroutine below is the single completion source.
			if (!__result)
				FailSceneTransition(__state, "Studio.LoadScene returned false");
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Studio.Studio), "LoadSceneCoroutine", new Type[] { typeof(string) }, null)]
		public static void LoadSceneCoroutinePostHook(
			Studio.Studio __instance,
			string _path,
			ref IEnumerator __result)
		{
			int generation = BeginSceneTransition(
				"Studio.LoadSceneCoroutine",
				true);
			bool clearsScene = !string.IsNullOrEmpty(_path) && File.Exists(_path);
			__result = ObserveSceneLoadCoroutine(
				__instance,
				__result,
				clearsScene,
				generation);
		}

		private static IEnumerator ObserveSceneLoadCoroutine(
			Studio.Studio studio,
			IEnumerator inner,
			bool clearsScene,
			int generation)
		{
			bool completed = false;
			Exception failure = null;
			try
			{
				while (inner != null)
				{
					bool moved = false;
					object current = null;
					try
					{
						moved = inner.MoveNext();
						if (moved)
							current = inner.Current;
					}
					catch (Exception ex)
					{
						failure = ex;
					}

					if (failure != null || !moved)
					{
						completed = failure == null;
						break;
					}

					yield return current;
				}
			}
			finally
			{
				IDisposable disposable = inner as IDisposable;
				if (disposable != null)
				{
					try
					{
						disposable.Dispose();
					}
					catch (Exception ex)
					{
						Logger.Log((LogLevel)4,
							(object)("Scene load enumerator cleanup failed: " + ex));
					}
				}

				if (completed)
				{
					if (clearsScene)
						VRCoordinateAccessoryTracker.ClearAll();
					CompleteSceneLoad(studio, generation);
				}
				else
				{
					string reason = failure != null
						? "Studio.LoadSceneCoroutine threw an exception"
						: "Studio.LoadSceneCoroutine was interrupted";
					Logger.Log((LogLevel)2, (object)reason);
					FailSceneTransition(generation, reason);
				}
			}

			if (failure != null)
				throw failure;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Studio.Studio), "ImportScene", new Type[] { typeof(string) }, null)]
		public static void ImportScenePreHook(out int __state)
		{
			__state = BeginSceneTransition("Studio.ImportScene", false);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Studio.Studio), "ImportScene", new Type[] { typeof(string) }, null)]
		public static void ImportScenePostHook(
			Studio.Studio __instance,
			bool __result,
			int __state)
		{
			if (__result)
			{
				CompleteSceneLoad(__instance, __state);
			}
			else
			{
				FailSceneTransition(__state, "Studio.ImportScene returned false");
			}
		}

		public static void CompleteSceneLoad(Studio.Studio studio)
		{
			if (!sceneTransitionActive)
			{
				Logger.Log((LogLevel)32,
					(object)"Ignoring scene completion because no transition is active.");
				return;
			}
			CompleteSceneLoad(studio, activeSceneTransitionGeneration);
		}

		private static void CompleteSceneLoad(Studio.Studio studio, int generation)
		{
			if (!sceneTransitionActive
				|| generation != activeSceneTransitionGeneration
				|| generation == lastCompletedSceneTransitionGeneration)
			{
				Logger.Log((LogLevel)32,
					(object)("Ignoring stale or duplicate scene completion for generation "
						+ generation + "."));
				return;
			}

			if (studio == null)
			{
				FailSceneTransition(
					generation,
					"scene load completed without a Studio instance");
				return;
			}

			sceneTransitionActive = false;
			lastCompletedSceneTransitionGeneration = generation;
			VRTimelineCameraFollowController.CompleteSceneTransition(generation);
			VRHandModelManager.SetPresentationSuppressionRequested(false);
			Logger.Log((LogLevel)32,
				(object)("Scene loaded successfully (generation " + generation
					+ "). Starting post-load recovery."));
			RequestVRRecovery("scene load completed");
			if (VR.Active)
				studio.StartCoroutine(AlignVRCameraAfterLoadCo(generation));
		}

		private static void FailSceneTransition(int generation, string reason)
		{
			if (!sceneTransitionActive || generation != activeSceneTransitionGeneration)
				return;

			sceneTransitionActive = false;
			lastCompletedSceneTransitionGeneration = generation;
			VRTimelineCameraFollowController.CompleteSceneTransition(generation);
			VRHandModelManager.SetPresentationSuppressionRequested(false);
			Logger.Log((LogLevel)2,
				(object)(reason + " (generation " + generation + ")."));
			RequestVRRecovery(reason);
		}

		private static void RequestVRRecovery(string reason)
		{
			if (!VR.Active)
				return;

			try
			{
				KKCharaStudioInterpreter interpreter =
					VR.Manager?.Interpreter as KKCharaStudioInterpreter;
				interpreter?.ForceResetVRMode(reason);
			}
			catch (InvalidOperationException)
			{
				// VR is still booting; there is no controller lifecycle to recover yet.
			}
		}

		private static IEnumerator AlignVRCameraAfterLoadCo(int generation)
		{
			Logger.Log((LogLevel)32, (object)"AlignVRCameraAfterLoadCo: Waiting for scene load to register...");
			yield return null;
			yield return null;
			if (!IsLatestCompletedGeneration(generation))
				yield break;

			var sceneManager = Singleton<Manager.Scene>.Instance;
			if (sceneManager != null)
			{
				while (sceneManager.IsNowLoading || sceneManager.IsNowLoadingFade)
				{
					if (!IsLatestCompletedGeneration(generation))
						yield break;
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
			if (!IsLatestCompletedGeneration(generation))
				yield break;

			if (IsCompanionCameraSyncLoaded())
			{
				Logger.Log((LogLevel)32,
					(object)"AlignVRCameraAfterLoadCo: CameraSync owns the one-time camera alignment; legacy MoveToCurrent skipped.");
			}
			else if (VRCameraMoveHelper.Instance != null)
			{
				Logger.Log((LogLevel)32,
					(object)"AlignVRCameraAfterLoadCo: CameraSync is absent; applying legacy camera alignment.");
				VRCameraMoveHelper.Instance.MoveToCurrent();
			}

			Logger.Log((LogLevel)32, (object)"AlignVRCameraAfterLoadCo: Repositioning the main floating studio UI quad in front of the camera.");
			float dist = 0.5f;
			var settings = VR.Manager.Context.Settings as KKCharaStudioVRSettings;
			if (settings != null)
			{
				dist = settings.UISpawnDistance;
			}
			VRCameraMoveHelper.RepositionMainUI(dist);
		}

		private static bool IsLatestCompletedGeneration(int generation)
		{
			return !sceneTransitionActive
				&& generation == lastCompletedSceneTransitionGeneration;
		}

		private static bool IsCompanionCameraSyncLoaded()
		{
			foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (string.Equals(
					assembly.GetName().Name,
					"KK_VR_CameraSync",
					StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}
	}
}

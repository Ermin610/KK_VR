using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using BepInEx4;
using Manager;
using Studio;
using UnityEngine;
using Object = UnityEngine.Object;
using Valve.VR;
using VRGIN.Controls;
using VRGIN.Core;
using VRGIN.Modes;
using Logger = BepInEx4.Logger;

namespace KKCharaStudioVR;

internal class KKCharaStudioInterpreter : GameInterpreter
{
	private List<KKCharaStudioActor> _Actors = new List<KKCharaStudioActor>();

	private Camera _SubCamera;

	private StudioScene studioScene;

	private int additionalCullingMask;

	private int vrRecoveryGeneration;

	private bool standingModeRecoveryArmed;

	private enum ControllerLifecycleHealth
	{
		Ready,
		WaitingForRuntime,
		Damaged
	}

	public override IEnumerable<IActor> Actors => _Actors.Cast<IActor>();

	protected override void OnStart()
	{
		base.OnStart();
		studioScene = UnityEngine.Object.FindObjectOfType<StudioScene>();
		additionalCullingMask = LayerMask.GetMask(new string[1] { "Studio/Select" });
	}

	protected override void OnLevel(int level)
	{
		base.OnLevel(level);
	}

	public override Camera FindCamera()
	{
		return null;
	}

	public override IActor FindNextActorToImpersonate()
	{
		List<IActor> list = Actors.ToList();
		IActor actor = FindImpersonatedActor();
		if (actor == null)
		{
			return list.FirstOrDefault();
		}
		return list[(list.IndexOf(actor) + 1) % list.Count];
	}

	protected override void OnUpdate()
	{
		try
		{
			if ((VR.Manager != null))
			{
				RefreshActors();
				UpdateMainCameraCullingMask();
			}
		}
		catch (Exception)
		{
		}
	}

	private void UpdateMainCameraCullingMask()
	{
		Camera component = ((Component)VR.Camera.SteamCam).GetComponent<Camera>();
		if (Singleton<Studio.Studio>.Instance.workInfo.visibleAxis)
		{
			component.cullingMask |= additionalCullingMask;
		}
		else
		{
			component.cullingMask &= ~additionalCullingMask;
		}
	}

	private void RefreshActors()
	{
		_Actors.Clear();
		foreach (ChaControl value in Singleton<Character>.Instance.dictEntryChara.Values)
		{
			if (((ChaInfo)value).objBodyBone != null)
			{
				AddActor(DefaultActorBehaviour<ChaControl>.Create<KKCharaStudioActor>(value));
			}
		}
	}

	private void AddActor(KKCharaStudioActor actor)
	{
		if ((actor.Eyes == null))
		{
			actor.Head.Reinitialize();
		}
		else
		{
			_Actors.Add(actor);
		}
	}

	public void ForceResetVRMode()
	{
		ForceResetVRMode("unspecified recovery request");
	}

	public void ForceResetVRMode(string reason)
	{
		if (!VR.Active)
			return;

		ControlMode currentMode;
		try
		{
			currentMode = VR.Manager.Mode;
		}
		catch (InvalidOperationException)
		{
			return;
		}

		bool shouldRecoverStanding = currentMode == null
			|| currentMode is GenericStandingMode
			|| standingModeRecoveryArmed;
		if (!shouldRecoverStanding)
		{
			Logger.Log((LogLevel)32,
				(object)("VR controller recovery skipped in "
					+ currentMode.GetType().Name + ": " + reason));
			return;
		}

		standingModeRecoveryArmed = true;
		int generation = ++vrRecoveryGeneration;
		((MonoBehaviour)this).StartCoroutine(ForceResetVRModeCo(reason, generation));
	}

	private IEnumerator ForceResetVRModeCo(string reason, int generation)
	{
		try
		{
			Logger.Log((LogLevel)32,
				(object)("Checking VR controller lifecycle after " + reason + "."));

			// SceneLoadScene drives Studio.LoadSceneCoroutine. During that coroutine
			// map loading can briefly change the active Unity scene and deactivate
			// controller objects. Wait for a stable, non-loading window before judging it.
			float deadline = Time.realtimeSinceStartup + 45f;
			int stableFrames = 0;
			int observedFrames = 0;
			while (Time.realtimeSinceStartup < deadline)
			{
				if (generation != vrRecoveryGeneration)
					yield break;

				Manager.Scene sceneManager = Singleton<Manager.Scene>.Instance;
				bool loading = sceneManager != null
					&& (sceneManager.IsNowLoading || sceneManager.IsNowLoadingFade);
				stableFrames = loading ? 0 : stableFrames + 1;
				observedFrames++;
				if (stableFrames >= 10 && observedFrames >= 10)
					break;
				yield return null;
			}

			if (generation != vrRecoveryGeneration)
				yield break;
			if (VR.Manager.Mode != null && !(VR.Manager.Mode is GenericStandingMode))
			{
				Logger.Log((LogLevel)32,
					(object)"VR controller recovery cancelled because the user changed modes.");
				yield break;
			}

			RefreshControllerManagers();
			for (int i = 0; i < 12; i++)
			{
				if (generation != vrRecoveryGeneration)
					yield break;
				yield return null;
			}

			string health;
			VRHandModelManager.Instance?.EnsureControllerBinding(reason);
			ControllerLifecycleHealth lifecycle =
				GetStandingControllerLifecycleHealth(out health);
			if (lifecycle == ControllerLifecycleHealth.Ready)
			{
				Logger.Log((LogLevel)32,
					(object)("VR controllers recovered without mode rebuild: " + health));
				yield break;
			}

			if (lifecycle == ControllerLifecycleHealth.WaitingForRuntime)
			{
				// Reconnection is a per-hand runtime transition, not evidence that the
				// whole StandingMode hierarchy is damaged. Give SteamVR events time to
				// repair their tracked indices while preserving the healthy hand.
				for (int frame = 0; frame < 180; frame++)
				{
					if (generation != vrRecoveryGeneration)
						yield break;
					yield return null;
					if (frame % 30 == 0)
					{
						RefreshControllerManagers();
						VRHandModelManager.Instance?.EnsureControllerBinding(
							"waiting for SteamVR runtime after " + reason);
					}

					lifecycle = GetStandingControllerLifecycleHealth(out health);
					if (lifecycle == ControllerLifecycleHealth.Ready)
					{
						Logger.Log((LogLevel)32,
							(object)("VR controller runtime binding recovered: " + health));
						yield break;
					}
					if (lifecycle == ControllerLifecycleHealth.Damaged)
						break;
				}

				if (lifecycle == ControllerLifecycleHealth.WaitingForRuntime)
				{
					Logger.Log((LogLevel)2,
						(object)("VR controller hierarchy is intact but a hand is still offline: "
							+ health + ". It will recover from the SteamVR reconnect event."));
					yield break;
				}
			}

			Logger.Log((LogLevel)2,
				(object)("VR controller lifecycle is damaged (" + health
					+ "); rebuilding GenericStandingMode."));

			Transform origin = null;
			Vector3 savedPosition = Vector3.zero;
			Quaternion savedRotation = Quaternion.identity;
			Vector3 savedScale = Vector3.one;
			if (VR.Camera != null && VR.Camera.SteamCam != null)
			{
				origin = VR.Camera.SteamCam.origin;
				if (origin != null)
				{
					savedPosition = origin.position;
					savedRotation = origin.rotation;
					savedScale = origin.localScale;
				}
			}

			VRHandModelManager.Instance?.PrepareForControllerRebuild(reason);
			ForceResetAsStandingMode();

			bool rebuilt = false;
			for (int frame = 0; frame < 180; frame++)
			{
				if (generation != vrRecoveryGeneration)
					yield break;
				yield return null;

				// StandingMode.OnStart zeros the tracking origin on its first frame.
				// Restore only during that short startup window; restoring again at
				// the end of a long reconnect wait would overwrite legitimate
				// CameraSync/Timeline movement accumulated in the meantime.
				if (origin != null && frame < 3)
				{
					origin.position = savedPosition;
					origin.rotation = savedRotation;
					origin.localScale = savedScale;
				}
				if (frame % 30 == 0)
					RefreshControllerManagers();
				lifecycle = GetStandingControllerLifecycleHealth(out health);
				if (frame >= 2 && lifecycle == ControllerLifecycleHealth.Ready)
				{
					rebuilt = true;
					break;
				}
			}

			VRHandModelManager.Instance?.EnsureControllerBinding("standing mode rebuilt");
			if (rebuilt)
			{
				Logger.Log((LogLevel)32,
					(object)("VR controller rebuild completed: " + health));
			}
			else
			{
				Logger.Log((LogLevel)2,
					(object)("VR controller rebuild timed out: " + health
						+ ". SteamVR/OpenVR may still have a physical disconnect."));
			}
		}
		finally
		{
			if (generation == vrRecoveryGeneration)
				standingModeRecoveryArmed = false;
		}
	}

	private static void RefreshControllerManagers()
	{
		try
		{
			if (VR.Camera == null || VR.Camera.SteamCam == null
				|| VR.Camera.SteamCam.origin == null)
				return;

			SteamVR_ControllerManager[] managers =
				((Component)VR.Camera.SteamCam.origin)
					.GetComponents<SteamVR_ControllerManager>();
			foreach (SteamVR_ControllerManager manager in managers)
			{
				if (manager != null && manager.enabled)
					manager.Refresh();
			}
		}
		catch (Exception ex)
		{
			Logger.Log((LogLevel)4,
				(object)("SteamVR controller refresh failed: " + ex.Message));
		}
	}

	private static ControllerLifecycleHealth GetStandingControllerLifecycleHealth(
		out string status)
	{
		status = "VR manager unavailable";
		VRManager manager;
		try
		{
			manager = VR.Manager;
		}
		catch (InvalidOperationException)
		{
			return ControllerLifecycleHealth.Damaged;
		}
		if (manager == null)
			return ControllerLifecycleHealth.Damaged;

		GenericStandingMode mode = manager.Mode as GenericStandingMode;
		if (mode == null)
		{
			status = "GenericStandingMode is missing";
			return ControllerLifecycleHealth.Damaged;
		}
		if (!mode.enabled || !((Component)mode).gameObject.activeInHierarchy)
		{
			status = "GenericStandingMode is disabled";
			return ControllerLifecycleHealth.Damaged;
		}

		if (VR.Camera == null || VR.Camera.SteamCam == null
			|| VR.Camera.SteamCam.origin == null)
		{
			status = "SteamVR camera origin is missing";
			return ControllerLifecycleHealth.Damaged;
		}
		SteamVR_ControllerManager[] controllerManagers =
			((Component)VR.Camera.SteamCam.origin)
				.GetComponents<SteamVR_ControllerManager>();
		if (controllerManagers.Length == 0)
		{
			status = "SteamVR_ControllerManager is missing";
			return ControllerLifecycleHealth.Damaged;
		}

		string leftStatus;
		string rightStatus;
		ControllerLifecycleHealth leftHealth =
			GetControllerLifecycleHealth(mode.Left, true, out leftStatus);
		ControllerLifecycleHealth rightHealth =
			GetControllerLifecycleHealth(mode.Right, false, out rightStatus);
		status = "left=" + leftStatus + "; right=" + rightStatus;
		if (leftHealth == ControllerLifecycleHealth.Damaged
			|| rightHealth == ControllerLifecycleHealth.Damaged)
			return ControllerLifecycleHealth.Damaged;
		if (leftHealth == ControllerLifecycleHealth.WaitingForRuntime
			|| rightHealth == ControllerLifecycleHealth.WaitingForRuntime)
			return ControllerLifecycleHealth.WaitingForRuntime;
		return ControllerLifecycleHealth.Ready;
	}

	private static ControllerLifecycleHealth GetControllerLifecycleHealth(
		VRGIN.Controls.Controller controller,
		bool isLeft,
		out string status)
	{
		string side = isLeft ? "left" : "right";
		if (controller == null)
		{
			status = side + " controller object missing";
			return ControllerLifecycleHealth.Damaged;
		}
		if (!controller.enabled)
		{
			status = side + " controller component disabled";
			return ControllerLifecycleHealth.Damaged;
		}

		SteamVR_TrackedObject tracked =
			((Component)controller).GetComponent<SteamVR_TrackedObject>();
		if (tracked == null)
		{
			status = side + " TrackedObject missing";
			return ControllerLifecycleHealth.Damaged;
		}
		if (controller.Tools == null || controller.Tools.Count == 0
			|| controller.Tools.Any(tool => tool == null))
		{
			status = side + " controller tools missing";
			return ControllerLifecycleHealth.Damaged;
		}

		if (controller.ToolIndex < 0 || controller.ToolIndex >= controller.Tools.Count)
		{
			status = side + " ToolIndex is outside the tool list";
			return ControllerLifecycleHealth.Damaged;
		}

		VRGIN.Controls.Tools.Tool activeTool = controller.ActiveTool;
		if (activeTool == null)
		{
			status = side + " ActiveTool missing or ToolIndex is invalid";
			return ControllerLifecycleHealth.Damaged;
		}
		Behaviour activeToolBehaviour = (Behaviour)activeTool;
		if (!((Component)activeTool).gameObject.activeInHierarchy)
		{
			status = side + " ActiveTool GameObject inactive";
			return ControllerLifecycleHealth.Damaged;
		}
		if (!activeToolBehaviour.enabled)
		{
			try
			{
				controller.ToolEnabled = true;
			}
			catch
			{
				// The health result below decides whether a mode rebuild is needed.
			}
			if (!activeToolBehaviour.enabled)
			{
				status = side + " ActiveTool disabled";
				return ControllerLifecycleHealth.Damaged;
			}
		}

		uint expectedIndex;
		if (!TryGetRuntimeControllerIndex(isLeft, out expectedIndex))
		{
			// The OpenVR/SteamVR runtime does not currently expose this physical
			// controller. Preserve the structurally sound mode and let its normal
			// DeviceConnected event restore tracking when hardware reconnects.
			status = side + " runtime device offline (structure intact)";
			return ControllerLifecycleHealth.WaitingForRuntime;
		}

		GameObject controllerObject = ((Component)controller).gameObject;
		if (!controllerObject.activeInHierarchy)
		{
			status = side + " controller inactive while runtime device is online";
			return ControllerLifecycleHealth.Damaged;
		}
		if (tracked.index == SteamVR_TrackedObject.EIndex.None)
		{
			status = side + " TrackedObject index is None while device is online";
			return ControllerLifecycleHealth.WaitingForRuntime;
		}

		uint actualIndex = (uint)(int)tracked.index;
		if (actualIndex != expectedIndex)
		{
			status = side + " index mismatch " + actualIndex + " != " + expectedIndex;
			return ControllerLifecycleHealth.WaitingForRuntime;
		}

		SteamVR_Controller.Device device = SteamVR_Controller.Input((int)tracked.index);
		if (device == null || !device.connected)
		{
			status = side + " SteamVR device binding is stale";
			return ControllerLifecycleHealth.WaitingForRuntime;
		}

		status = side + " ready on index " + actualIndex;
		return ControllerLifecycleHealth.Ready;
	}

	private static bool TryGetRuntimeControllerIndex(bool isLeft, out uint index)
	{
		index = uint.MaxValue;
		CVRSystem system = OpenVR.System;
		if (system == null)
			return false;

		index = system.GetTrackedDeviceIndexForControllerRole(isLeft
			? ETrackedControllerRole.LeftHand
			: ETrackedControllerRole.RightHand);
		if (index == uint.MaxValue || index >= SteamVR.connected.Length)
			return false;
		if (!system.IsTrackedDeviceConnected(index)
			|| system.GetTrackedDeviceClass(index) != ETrackedDeviceClass.Controller)
			return false;

		// SteamVR_ControllerManager assigns from this array. If OpenVR has
		// reconnected first but SteamVR has not delivered its event yet, wait
		// instead of rebuilding a healthy hierarchy in a loop.
		return SteamVR.connected[index];
	}

	public static void ForceResetAsStandingMode()
	{
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Invalid comparison between Unknown and I4
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			ControlMode oldMode = VR.Manager.Mode;
			if (oldMode != null)
			{
				Object.DestroyImmediate((Object)(object)oldMode);
			}
			VR.Manager.SetMode<GenericStandingMode>();
			if ((VR.Camera != null))
			{
				_ = VR.Camera.Blueprint;
				Camera mainCmaera = Singleton<Studio.Studio>.Instance.cameraCtrl.mainCmaera;
				Logger.Log((LogLevel)32, (object)$"Force replace blueprint camera with {mainCmaera}");
				Camera camera = VR.Camera.SteamCam.camera;
				Camera val = mainCmaera;
				camera.nearClipPlane = VR.Context.NearClipPlane;
				camera.farClipPlane = Mathf.Max(val.farClipPlane, 10f);
				camera.clearFlags = (CameraClearFlags)(((int)val.clearFlags == 1) ? 1 : 2);
				camera.renderingPath = val.renderingPath;
				camera.clearStencilAfterLightingPass = val.clearStencilAfterLightingPass;
				camera.depthTextureMode = val.depthTextureMode;
				camera.layerCullDistances = val.layerCullDistances;
				camera.layerCullSpherical = val.layerCullSpherical;
				camera.useOcclusionCulling = val.useOcclusionCulling;
				camera.allowHDR = val.allowHDR;
				camera.backgroundColor = val.backgroundColor;
				Skybox component = ((Component)val).GetComponent<Skybox>();
				if (component != null)
				{
					Skybox val2 = ((Component)camera).gameObject.GetComponent<Skybox>();
					if (val2 == null)
					{
						val2 = ((Component)camera).gameObject.AddComponent<Skybox>();
					}
					val2.material = component.material;
				}
				VR.Camera.CopyFX(val);
			}
			else
			{
				Logger.Log((LogLevel)32, (object)"VR.Camera is null");
			}
		}
		catch (Exception value)
		{
			Console.WriteLine(value);
		}
	}
}

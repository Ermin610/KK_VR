// Camera synchronization logic is adapted from KK_VR_CameraSync v0.1.5.
// Copyright (c) 2026 YukyoMoe. Licensed under the MIT License.

using System;
using Manager;
using Studio;
using UnityEngine;
using VRGIN.Core;

namespace KKCharaStudioVR;

public enum VRCameraRotationMode
{
	Full,
	YawOnly,
	None
}

public enum VRCameraPositionFollowMode
{
	AllMotion,
	CutsOnly,
	Off
}

internal enum StudioCameraPoseSource
{
	CameraControl,
	ObjectCamera
}

internal struct StudioCameraPose
{
	public Vector3 Position;
	public Quaternion Rotation;
	public StudioCameraPoseSource Source;
}

internal enum StudioSceneMutation
{
	None,
	Load,
	Import
}

[DefaultExecutionOrder(32000)]
public sealed class VRCameraSyncController : MonoBehaviour
{
	private const float PositionChangeThreshold = 0.0001f;
	private const float RotationChangeThreshold = 0.02f;
	private const int StaleMutationFrameLimit = 600;

	private static VRCameraSyncController _instance;

	private KKCharaStudioVRSettings _settings;
	private bool _baselineValid;
	private StudioCameraPose _previousCameraPose;
	private bool _initialAlignmentPending;
	private bool _initialAlignmentPoseValid;
	private StudioCameraPose _initialAlignmentPose;
	private int _initialAlignmentReadyFrame = -1;
	private string _initialAlignmentReason = "Studio scene loaded";
	private int _suspendDepth;
	private StudioSceneMutation _sceneMutation;
	private int _sceneMutationStartFrame = -1;
	private bool _sceneInfoChangedDuringLoad;
	private bool _sceneInfoObserved;
	private object _lastSceneInfo;
	private int _resumeFrame = -1;
	private float _nextErrorLogTime;
	private bool _settingsStateValid;
	private bool _lastSyncEnabled;
	private VRCameraRotationMode _lastRotationMode;
	private bool _lastReadObjectCamera;

	public static VRCameraSyncController Instance => _instance;

	private bool IsSuspended
	{
		get
		{
			return _suspendDepth > 0 ||
			       _sceneMutation != StudioSceneMutation.None ||
			       Time.frameCount < _resumeFrame;
		}
	}

	public static void Install(GameObject container)
	{
		if (_instance == null)
		{
			_instance = container.AddComponent<VRCameraSyncController>();
		}
	}

	private void Awake()
	{
		_instance = this;
		RememberCurrentSceneInfo();
	}

	private void OnDestroy()
	{
		if (_instance == this)
		{
			_instance = null;
		}
	}

	internal void Suspend()
	{
		_suspendDepth++;
		_baselineValid = false;
	}

	internal void ResumeAndReset()
	{
		if (_suspendDepth > 0)
		{
			_suspendDepth--;
		}

		_baselineValid = false;
		_resumeFrame = Math.Max(_resumeFrame, Time.frameCount + 2);
	}

	internal void ResetBaseline()
	{
		_baselineValid = false;
		_resumeFrame = Math.Max(_resumeFrame, Time.frameCount + 1);
	}

	internal void CompleteNativeCameraReset()
	{
		_baselineValid = false;
		_resumeFrame = Math.Max(_resumeFrame, Time.frameCount + 2);
	}

	internal void BeginSceneLoad()
	{
		BeginSceneMutation(StudioSceneMutation.Load);
	}

	internal void BeginSceneImport()
	{
		BeginSceneMutation(StudioSceneMutation.Import);
	}

	internal void CompleteSceneLoad(bool succeeded)
	{
		CompleteSceneMutation(succeeded, alignAfterLoad: true);
	}

	internal void CompleteSceneImport(bool succeeded)
	{
		CompleteSceneMutation(succeeded, alignAfterLoad: false);
	}

	private void BeginSceneMutation(StudioSceneMutation mutation)
	{
		if (_sceneMutation == StudioSceneMutation.None)
		{
			_sceneMutationStartFrame = Time.frameCount;
			_sceneInfoChangedDuringLoad = false;
		}

		// A full load is stronger than an import and must not be downgraded by
		// another hook observing the same operation.
		if (_sceneMutation != StudioSceneMutation.Load)
		{
			_sceneMutation = mutation;
		}

		_baselineValid = false;
		ClearInitialAlignment();
	}

	private void CompleteSceneMutation(bool succeeded, bool alignAfterLoad)
	{
		_sceneMutation = StudioSceneMutation.None;
		_sceneMutationStartFrame = -1;
		_sceneInfoChangedDuringLoad = false;
		_baselineValid = false;
		_resumeFrame = Math.Max(_resumeFrame, Time.frameCount + 2);
		RememberCurrentSceneInfo();

		if (!succeeded || !alignAfterLoad)
		{
			return;
		}

		CaptureInitialAlignmentPose();
		RequestInitialAlignment("Studio scene loaded");
	}

	private void RequestInitialAlignment(string reason)
	{
		_initialAlignmentPending = true;
		_initialAlignmentReadyFrame = Math.Max(
			_initialAlignmentReadyFrame,
			Time.frameCount + 2);
		_initialAlignmentReason = reason;
		_baselineValid = false;
	}

	private void ClearInitialAlignment()
	{
		_initialAlignmentPending = false;
		_initialAlignmentPoseValid = false;
		_initialAlignmentReadyFrame = -1;
	}

	private void CaptureInitialAlignmentPose()
	{
		if (TryGetSceneInitialCameraPose(out StudioCameraPose capturedPose))
		{
			_initialAlignmentPose = capturedPose;
			_initialAlignmentPoseValid = true;
		}
	}

	private void LateUpdate()
	{
		if (ObserveStudioSceneInfoChange())
		{
			HandleObservedStudioSceneChange();
			return;
		}

		RecoverStaleSceneMutation();

		KKCharaStudioVRSettings settings = GetSettings();
		ObserveSettingsState(settings);
		if (settings == null || !settings.CameraSyncEnabled)
		{
			_baselineValid = false;
			ClearInitialAlignment();
			return;
		}

		if (IsSuspended || IsSceneLoading())
		{
			_baselineValid = false;
			return;
		}

		try
		{
			if (!TryGetVrRig(out Transform origin, out Transform head))
			{
				_baselineValid = false;
				return;
			}

			if (!TryGetStudioCameraPose(out StudioCameraPose currentCameraPose))
			{
				_baselineValid = false;
				return;
			}

			StudioCameraPose fullCurrentCameraPose = currentCameraPose;
			currentCameraPose.Rotation = FilterRotation(
				currentCameraPose.Rotation,
				settings.CameraSyncRotationMode);

			if (_initialAlignmentPending)
			{
				if (!settings.CameraSyncAlignOnSceneLoad)
				{
					ClearInitialAlignment();
				}
				else
				{
					if (Time.frameCount < _initialAlignmentReadyFrame)
					{
						_baselineValid = false;
						return;
					}

					ApplyInitialAlignment(
						origin,
						head,
						fullCurrentCameraPose,
						currentCameraPose,
						settings);
					return;
				}
			}

			if (!_baselineValid)
			{
				_previousCameraPose = currentCameraPose;
				_baselineValid = true;
				return;
			}

			if (currentCameraPose.Source != _previousCameraPose.Source)
			{
				_previousCameraPose = currentCameraPose;
				return;
			}

			float positionDelta = Vector3.Distance(
				currentCameraPose.Position,
				_previousCameraPose.Position);
			float rotationDelta = Quaternion.Angle(
				currentCameraPose.Rotation,
				_previousCameraPose.Rotation);

			if (positionDelta > PositionChangeThreshold ||
			    rotationDelta > RotationChangeThreshold)
			{
				ApplyCameraMotion(
					origin,
					head,
					currentCameraPose,
					positionDelta,
					settings);
			}

			_previousCameraPose = currentCameraPose;
		}
		catch (Exception exception)
		{
			_baselineValid = false;
			LogExceptionThrottled(exception);
		}
	}

	private void ApplyInitialAlignment(
		Transform origin,
		Transform head,
		StudioCameraPose fullCurrentCameraPose,
		StudioCameraPose currentCameraPose,
		KKCharaStudioVRSettings settings)
	{
		StudioCameraPose targetPose = _initialAlignmentPoseValid
			? _initialAlignmentPose
			: fullCurrentCameraPose;
		Quaternion initialRotation = FilterRotation(
			targetPose.Rotation,
			settings.CameraSyncInitialRotationMode);

		SnapHeadToTarget(
			origin,
			head,
			targetPose.Position,
			initialRotation,
			settings.CameraSyncInitialRotationMode);

		ClearInitialAlignment();

		StudioCameraPose followBaseline = targetPose;
		followBaseline.Rotation = FilterRotation(
			followBaseline.Rotation,
			settings.CameraSyncRotationMode);
		_previousCameraPose = followBaseline;
		_baselineValid = true;

		// Timeline may start advancing before the load fade finishes. Align to
		// the card's saved pose first, then retain that already-played delta.
		if (currentCameraPose.Source == followBaseline.Source)
		{
			float initialPositionDelta = Vector3.Distance(
				currentCameraPose.Position,
				followBaseline.Position);
			float initialRotationDelta = Quaternion.Angle(
				currentCameraPose.Rotation,
				followBaseline.Rotation);
			if (initialPositionDelta > PositionChangeThreshold ||
			    initialRotationDelta > RotationChangeThreshold)
			{
				ApplyCameraMotion(
					origin,
					head,
					currentCameraPose,
					initialPositionDelta,
					settings);
			}
		}

		_previousCameraPose = currentCameraPose;
		VRLog.Info(
			"VR camera aligned to Studio camera. Source={0}, reason={1}, position={2}, rotation={3}.",
			currentCameraPose.Source,
			_initialAlignmentReason,
			FormatVector(targetPose.Position),
			settings.CameraSyncInitialRotationMode);
	}

	private void ObserveSettingsState(KKCharaStudioVRSettings settings)
	{
		if (settings == null)
		{
			_settingsStateValid = false;
			return;
		}

		if (!_settingsStateValid)
		{
			_settingsStateValid = true;
			_lastSyncEnabled = settings.CameraSyncEnabled;
			_lastRotationMode = settings.CameraSyncRotationMode;
			_lastReadObjectCamera = settings.CameraSyncReadObjectCamera;
			return;
		}

		if (_lastSyncEnabled != settings.CameraSyncEnabled ||
		    _lastRotationMode != settings.CameraSyncRotationMode ||
		    _lastReadObjectCamera != settings.CameraSyncReadObjectCamera)
		{
			_baselineValid = false;
			_resumeFrame = Math.Max(_resumeFrame, Time.frameCount + 1);
		}

		_lastSyncEnabled = settings.CameraSyncEnabled;
		_lastRotationMode = settings.CameraSyncRotationMode;
		_lastReadObjectCamera = settings.CameraSyncReadObjectCamera;
	}

	private KKCharaStudioVRSettings GetSettings()
	{
		if (_settings == null && VR.Manager != null && VR.Manager.Context != null)
		{
			_settings = VR.Manager.Context.Settings as KKCharaStudioVRSettings;
		}

		return _settings;
	}

	private bool ObserveStudioSceneInfoChange()
	{
		Studio.Studio studio = Singleton<Studio.Studio>.Instance;
		object currentSceneInfo = studio == null ? null : studio.sceneInfo;
		if (currentSceneInfo == null)
		{
			return false;
		}

		if (!_sceneInfoObserved)
		{
			_sceneInfoObserved = true;
			_lastSceneInfo = currentSceneInfo;
			return false;
		}

		if (ReferenceEquals(currentSceneInfo, _lastSceneInfo))
		{
			return false;
		}

		_lastSceneInfo = currentSceneInfo;
		return true;
	}

	private void RememberCurrentSceneInfo()
	{
		Studio.Studio studio = Singleton<Studio.Studio>.Instance;
		if (studio == null || studio.sceneInfo == null)
		{
			return;
		}

		_sceneInfoObserved = true;
		_lastSceneInfo = studio.sceneInfo;
	}

	private void HandleObservedStudioSceneChange()
	{
		if (_sceneMutation == StudioSceneMutation.Load)
		{
			// LoadSceneCoroutine replaces SceneInfo before maps, objects, and
			// CameraControl finish loading. Its wrapper will complete the
			// mutation after the original enumerator actually ends.
			_baselineValid = false;
			_sceneInfoChangedDuringLoad = true;
			ClearInitialAlignment();
			return;
		}

		bool shouldAlign = _sceneMutation != StudioSceneMutation.Import;
		_sceneMutation = StudioSceneMutation.None;
		_sceneMutationStartFrame = -1;
		_baselineValid = false;
		_resumeFrame = Math.Max(_resumeFrame, Time.frameCount + 2);
		ClearInitialAlignment();

		if (shouldAlign)
		{
			CaptureInitialAlignmentPose();
			RequestInitialAlignment("Studio sceneInfo changed");
			VRLog.Info("Studio scene-card change detected; VR camera alignment was scheduled.");
		}
	}

	private void RecoverStaleSceneMutation()
	{
		if (_sceneMutation == StudioSceneMutation.None ||
		    _sceneMutationStartFrame < 0 ||
		    Time.frameCount - _sceneMutationStartFrame <= StaleMutationFrameLimit ||
		    IsSceneLoading())
		{
			return;
		}

		StudioSceneMutation staleMutation = _sceneMutation;
		bool sceneChangedDuringLoad = _sceneInfoChangedDuringLoad;
		_sceneMutation = StudioSceneMutation.None;
		_sceneMutationStartFrame = -1;
		_sceneInfoChangedDuringLoad = false;
		_baselineValid = false;
		_resumeFrame = Math.Max(_resumeFrame, Time.frameCount + 2);
		if (staleMutation == StudioSceneMutation.Load && sceneChangedDuringLoad)
		{
			CaptureInitialAlignmentPose();
			RequestInitialAlignment("recovered Studio scene coroutine");
		}
		VRLog.Warn(
			"Studio {0} did not reach its completion hook; camera sync recovered with a fresh baseline.",
			staleMutation);
	}

	private void ApplyCameraMotion(
		Transform origin,
		Transform head,
		StudioCameraPose currentCameraPose,
		float positionDelta,
		KKCharaStudioVRSettings settings)
	{
		bool positionAuthoritative;
		switch (settings.CameraSyncPositionMode)
		{
			case VRCameraPositionFollowMode.AllMotion:
				positionAuthoritative = true;
				break;
			case VRCameraPositionFollowMode.CutsOnly:
				positionAuthoritative =
					positionDelta > Mathf.Max(0.01f, settings.CameraSyncCutThreshold);
				break;
			default:
				positionAuthoritative = false;
				break;
		}

		if (settings.CameraSyncPreserveHeadTracking)
		{
			if (positionAuthoritative)
			{
				ApplyCameraPoseDelta(origin, currentCameraPose);
			}
			else
			{
				ApplyRotationDeltaKeepingHeadPosition(
					origin,
					head,
					currentCameraPose.Rotation);
			}
		}
		else if (positionAuthoritative)
		{
			SnapHeadToTarget(
				origin,
				head,
				currentCameraPose.Position,
				currentCameraPose.Rotation,
				settings.CameraSyncRotationMode);
		}
		else
		{
			RotateHeadToTargetKeepingPosition(
				origin,
				head,
				currentCameraPose.Rotation,
				settings.CameraSyncRotationMode);
		}
	}

	private void ApplyCameraPoseDelta(
		Transform origin,
		StudioCameraPose currentCameraPose)
	{
		Quaternion rotationDelta =
			currentCameraPose.Rotation *
			Quaternion.Inverse(_previousCameraPose.Rotation);
		Vector3 nextOriginPosition =
			currentCameraPose.Position +
			rotationDelta *
			(origin.position - _previousCameraPose.Position);

		origin.rotation = rotationDelta * origin.rotation;
		origin.position = nextOriginPosition;
	}

	private void ApplyRotationDeltaKeepingHeadPosition(
		Transform origin,
		Transform head,
		Quaternion currentCameraRotation)
	{
		Vector3 headPosition = head.position;
		Quaternion rotationDelta =
			currentCameraRotation *
			Quaternion.Inverse(_previousCameraPose.Rotation);

		origin.rotation = rotationDelta * origin.rotation;
		origin.position += headPosition - head.position;
	}

	private static void SnapHeadToTarget(
		Transform origin,
		Transform head,
		Vector3 targetPosition,
		Quaternion targetRotation,
		VRCameraRotationMode rotationMode)
	{
		if (rotationMode != VRCameraRotationMode.None)
		{
			Quaternion currentHeadRotation =
				rotationMode == VRCameraRotationMode.YawOnly
					? Quaternion.Euler(0f, head.rotation.eulerAngles.y, 0f)
					: head.rotation;
			Quaternion rotationDelta =
				targetRotation * Quaternion.Inverse(currentHeadRotation);
			origin.rotation = rotationDelta * origin.rotation;
		}

		origin.position += targetPosition - head.position;
	}

	private static void RotateHeadToTargetKeepingPosition(
		Transform origin,
		Transform head,
		Quaternion targetRotation,
		VRCameraRotationMode rotationMode)
	{
		if (rotationMode == VRCameraRotationMode.None)
		{
			return;
		}

		Vector3 headPosition = head.position;
		Quaternion currentHeadRotation =
			rotationMode == VRCameraRotationMode.YawOnly
				? Quaternion.Euler(0f, head.rotation.eulerAngles.y, 0f)
				: head.rotation;
		Quaternion rotationDelta =
			targetRotation * Quaternion.Inverse(currentHeadRotation);

		origin.rotation = rotationDelta * origin.rotation;
		origin.position += headPosition - head.position;
	}

	private static Quaternion FilterRotation(
		Quaternion rotation,
		VRCameraRotationMode rotationMode)
	{
		switch (rotationMode)
		{
			case VRCameraRotationMode.Full:
				return rotation;
			case VRCameraRotationMode.YawOnly:
				return Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
			default:
				return Quaternion.identity;
		}
	}

	private static bool TryGetVrRig(out Transform origin, out Transform head)
	{
		origin = null;
		head = null;
		if (!VR.Active || VR.Camera == null)
		{
			return false;
		}

		origin = VR.Camera.Origin;
		head = VR.Camera.Head;
		return origin != null && head != null;
	}

	private static bool IsSceneLoading()
	{
		Manager.Scene scene = Singleton<Manager.Scene>.Instance;
		return scene != null && (scene.IsNowLoading || scene.IsNowLoadingFade);
	}

	private bool TryGetStudioCameraPose(out StudioCameraPose pose)
	{
		pose = new StudioCameraPose();
		Studio.Studio studio = Singleton<Studio.Studio>.Instance;
		if (studio == null || studio.cameraCtrl == null)
		{
			return false;
		}

		KKCharaStudioVRSettings settings = GetSettings();
		if (settings != null &&
		    settings.CameraSyncReadObjectCamera &&
		    TryGetActiveObjectCameraPose(studio, out pose))
		{
			return true;
		}

		Transform cameraTransform = studio.cameraCtrl.transform;
		pose.Position = cameraTransform.position;
		pose.Rotation = cameraTransform.rotation;
		pose.Source = StudioCameraPoseSource.CameraControl;
		return true;
	}

	private bool TryGetSceneInitialCameraPose(out StudioCameraPose pose)
	{
		pose = new StudioCameraPose();
		Studio.Studio studio = Singleton<Studio.Studio>.Instance;
		if (studio == null || studio.cameraCtrl == null)
		{
			return false;
		}

		KKCharaStudioVRSettings settings = GetSettings();
		if (settings != null &&
		    settings.CameraSyncReadObjectCamera &&
		    TryGetActiveObjectCameraPose(studio, out pose))
		{
			return true;
		}

		Studio.CameraControl.CameraData savedCameraData =
			studio.sceneInfo == null ? null : studio.sceneInfo.cameraSaveData;
		if (savedCameraData != null)
		{
			return TryConvertCameraData(studio.cameraCtrl, savedCameraData, out pose);
		}

		return TryGetStudioCameraPose(out pose);
	}

	private static bool TryConvertCameraData(
		Studio.CameraControl cameraControl,
		Studio.CameraControl.CameraData cameraData,
		out StudioCameraPose pose)
	{
		pose = new StudioCameraPose();
		if (cameraControl == null || cameraData == null)
		{
			return false;
		}

		Quaternion localRotation = Quaternion.Euler(cameraData.rotate);
		Transform transformBase = cameraControl.transBase;
		if (transformBase != null)
		{
			pose.Rotation = transformBase.rotation * localRotation;
			pose.Position =
				transformBase.TransformPoint(cameraData.pos) +
				pose.Rotation * cameraData.distance;
		}
		else
		{
			pose.Rotation = localRotation;
			pose.Position =
				cameraData.pos +
				pose.Rotation * cameraData.distance;
		}

		pose.Source = StudioCameraPoseSource.CameraControl;
		return true;
	}

	private static bool TryGetActiveObjectCameraPose(
		Studio.Studio studio,
		out StudioCameraPose pose)
	{
		pose = new StudioCameraPose();
		OCICamera objectCamera = studio.ociCamera;
		if (objectCamera == null || objectCamera.objectItem == null)
		{
			return false;
		}

		Transform cameraTransform = objectCamera.objectItem.transform;
		pose.Position = cameraTransform.position;
		pose.Rotation = cameraTransform.rotation;
		pose.Source = StudioCameraPoseSource.ObjectCamera;
		return true;
	}

	private void LogExceptionThrottled(Exception exception)
	{
		if (Time.unscaledTime < _nextErrorLogTime)
		{
			return;
		}

		_nextErrorLogTime = Time.unscaledTime + 5f;
		VRLog.Warn(
			"Camera synchronization failed and its baseline was reset: {0}",
			exception);
	}

	private static string FormatVector(Vector3 value)
	{
		return "(" +
		       value.x.ToString("F3") + ", " +
		       value.y.ToString("F3") + ", " +
		       value.z.ToString("F3") + ")";
	}
}

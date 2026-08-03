using System;
using System.Reflection;
using UnityEngine;
using Valve.VR;
using VRGIN.Core;

namespace KKCharaStudioVR;

/// <summary>
/// Applies the wrist-menu Timeline camera mode without changing the companion
/// CameraSync plug-in's saved global configuration. When animation-only mode is
/// active, playback is suspended and paused seek/stop evaluations are filtered
/// without blocking later manual Studio camera edits.
/// </summary>
[DefaultExecutionOrder(31000)]
internal sealed class VRTimelineCameraFollowController : MonoBehaviour
{
    private const string CameraSyncAssemblyName = "KK_VR_CameraSync";
    private const string CameraSyncPluginTypeName = "KK_VR_CameraSync.Plugin";
    private const float CompositionStickDeadzone = 0.15f;
    private const float CompositionApplyInterval = 0.08f;
    private const float TimelineVerticalAdjustSpeed = 0.6f;
    private const float TimelineYawAdjustSpeed = 60f;

    private static VRTimelineCameraFollowController _instance;
    private static bool _sceneTransitionActive;
    private static int _sceneTransitionGeneration;

    private KKCharaStudioVRSettings _settings;
    private object _cameraSyncDriver;
    private MethodInfo _suspendMethod;
    private MethodInfo _resumeMethod;
    private MethodInfo _resumeAndPrimeTimelineMethod;
    private MethodInfo _setTimelineCameraSuppressedMethod;
    private MethodInfo _setTimelineFovCompensationOverrideMethod;
    private MethodInfo _clearTimelineFovCompensationOverrideMethod;
    private MethodInfo _setTimelineUserPoseOffsetMethod;
    private MethodInfo _clearTimelineUserPoseOffsetMethod;
    private MethodInfo _setExternalVrCameraOwnerMethod;
    private MethodInfo _isTimelineCameraWriterActiveMethod;
    private bool _suspendedByThisPlugin;
    private bool _timelineSuppressionStateKnown;
    private bool _timelineSuppressionApplied;
    private bool _missingCompanionLogged;
    private bool _deferredResumeLogged;
    private float _nextResolveTime;
    private int _manualMovementLockedThroughFrame = -1;
    private bool _timelinePlaybackLockKnown;
    private bool _timelinePlaybackLocked;
    private bool _timelineCompositionStateKnown;
    private bool _timelineCompositionEnabledApplied;
    private float _timelineCompositionReferenceApplied;
    private bool _timelinePoseOffsetStateKnown;
    private float _timelineVerticalOffsetApplied;
    private float _timelineYawOffsetApplied;
    private bool _externalCameraOwnerStateKnown;
    private bool _externalCameraOwnerApplied;
    private int _rightStickTransportConsumedFrame = -1;
    private bool _timelineControlStickAdjusting;
    private bool _timelineControlStickDirty;
    private float _timelineFovStickValue = VRTimelineService.DefaultCameraFov;
    private float _timelineVerticalStickValue;
    private float _timelineYawStickValue;
    private float _nextTimelineFovStickApply;

    internal static bool IsCameraSyncAvailable
    {
        get
        {
            return _instance != null && _instance.TryResolveCameraSyncDriver(false);
        }
    }

    internal static bool IsTimelineCameraWriterActive
    {
        get
        {
            return _instance != null
                && _instance.QueryTimelineCameraWriterActive();
        }
    }

    /// <summary>
    /// Artificial locomotion and VR object manipulation stay locked while the
    /// user selected Timeline camera following. Physical headset tracking is not
    /// affected. The one-frame grace after pausing prevents the same stick click
    /// from also firing another shortcut or applying a movement delta.
    /// </summary>
    internal static bool IsManualMovementLocked
    {
        get
        {
            return _sceneTransitionActive
                || (_instance != null && _instance.IsManualMovementLockedNow());
        }
    }

    /// <summary>
    /// True only while Timeline is actively playing with camera-follow mode
    /// selected. Consumers can use this to suppress rotation without inheriting
    /// scene-transition or one-frame locomotion grace locks.
    /// </summary>
    internal static bool IsPlaybackInputLocked
    {
        get
        {
            if (VRTimelineService.IsSceneMutationActive)
                return true;
            if (_instance == null)
                return false;
            _instance.ResolveSettings();
            if (_instance._settings == null
                || !_instance._settings.TimelineFollowCamera)
                return false;
            bool isPlaying;
            return VRTimelineService.TryGetIsPlaying(out isPlaying) && isPlaying;
        }
    }

    internal static bool ConsumedRightStickTransportThisFrame =>
        _instance != null
        && _instance._rightStickTransportConsumedFrame == Time.frameCount;

    /// <summary>
    /// The dedicated Timeline page is an explicit transport context. When its
    /// Timeline is paused, let it claim the click before an independently
    /// playing MMDD controller can consume the same edge.
    /// </summary>
    internal static bool ShouldClaimRightStickTransport
    {
        get
        {
            if (VRTimelineService.IsSceneMutationActive)
                return true;
            bool isPlaying;
            bool available = VRTimelineService.TryGetIsPlaying(out isPlaying);
            return VRWristMenuController.IsTimelinePageOpen
                || available;
        }
    }

    internal static bool IsTimelineControlSpaceActive
    {
        get
        {
            if (_instance == null)
                return false;
            _instance.ResolveSettings();
            bool isPlaying;
            return _instance._settings != null
                && _instance._settings.TimelineFollowCamera
                && VRTimelineService.TryGetIsPlaying(out isPlaying)
                && isPlaying;
        }
    }

    internal static bool TryHandleRightStickPlaybackToggle()
    {
        if (VRTimelineService.IsSceneMutationActive)
        {
            if (_instance != null)
                _instance._rightStickTransportConsumedFrame = Time.frameCount;
            return true;
        }
        return _instance != null && _instance.HandleRightStickPlaybackToggle();
    }

    internal static void BeginSceneTransition(int generation)
    {
        if (generation < _sceneTransitionGeneration)
            return;

        _sceneTransitionGeneration = generation;
        _sceneTransitionActive = true;
        if (_instance != null)
        {
            _instance.SetExternalVrCameraOwner(false, false);
        }
        GripMoveKKCharaStudioTool.CancelAllTimelineLockedInteraction();
        VRTwoHandScale.CancelTimelineInteraction();
    }

    internal static void CompleteSceneTransition(int generation)
    {
        if (!_sceneTransitionActive || generation != _sceneTransitionGeneration)
            return;

        _sceneTransitionActive = false;
        if (_instance != null)
        {
            _instance._manualMovementLockedThroughFrame = Time.frameCount;
            _instance._timelinePlaybackLockKnown = false;
        }
        GripMoveKKCharaStudioTool.CancelAllTimelineLockedInteraction();
        VRTwoHandScale.CancelTimelineInteraction();
    }

    private void Awake()
    {
        _instance = this;
        ResolveSettings();
    }

    private void Update()
    {
        RefreshTimelinePlaybackLock(true);
        UpdateTimelineTransportInput();
        UpdateTimelineCompositionInput();
    }

    private void LateUpdate()
    {
        ApplyCurrentMode(false);
        ApplyTimelineCompositionOverride(false);
        ApplyTimelinePoseOffsetOverride(false);
        ApplyExternalMmdCameraOwnership(false);
    }

    private void OnDisable()
    {
        FinishTimelineControlInput();
        ClearRuntimeCameraOverrides(false);
        SetTimelineCameraSuppression(false, false);
        ReleaseSuspension(false);
    }

    private void OnDestroy()
    {
        FinishTimelineControlInput();
        ClearRuntimeCameraOverrides(false);
        SetTimelineCameraSuppression(false, false);
        ReleaseSuspension(false);
        if (_instance == this)
            _instance = null;
    }

    internal static bool ApplyNow()
    {
        if (_instance == null)
            return false;
        _instance.ApplyCurrentMode(true);
        _instance.ApplyTimelineCompositionOverride(true);
        _instance.ApplyTimelinePoseOffsetOverride(true);
        _instance.ApplyExternalMmdCameraOwnership(true);
        return _instance.TryResolveCameraSyncDriver(true);
    }

    internal static bool ApplyTimelineCompositionNow()
    {
        if (_instance == null)
            return false;
        bool compositionApplied = _instance.ApplyTimelineCompositionOverride(true);
        bool poseApplied = _instance.ApplyTimelinePoseOffsetOverride(true);
        return compositionApplied && poseApplied;
    }

    private void ResolveSettings()
    {
        if (_settings == null && VR.Manager != null && VR.Manager.Context != null)
            _settings = VR.Manager.Context.Settings as KKCharaStudioVRSettings;
    }

    /// <summary>
    /// Right-stick click owns Timeline transport. While the Timeline camera is
    /// playing, unmodified vertical input adjusts the persisted reference FOV.
    /// Holding the middle-finger Grip turns the same stick into a linear pose
    /// editor: Y changes world height and X changes yaw continuously.
    /// </summary>
    private void UpdateTimelineTransportInput()
    {
        SteamVR_Controller.Device right = GetDevice(VR.Mode?.Right);
        if (right == null)
            return;

        bool chorded = right.GetPress(EVRButtonId.k_EButton_Grip)
            || right.GetPress(EVRButtonId.k_EButton_Axis1)
            || right.GetPress(EVRButtonId.k_EButton_ApplicationMenu);
        if (!chorded
            && right.GetPressDown(EVRButtonId.k_EButton_Axis0))
        {
            HandleRightStickPlaybackToggle();
        }
    }

    private void UpdateTimelineCompositionInput()
    {
        ResolveSettings();
        bool timelinePlaying;
        bool canAdjust = !_sceneTransitionActive
            && !VRTimelineService.IsSceneMutationActive
            && _settings != null
            && _settings.TimelineFollowCamera
            && VRTimelineService.TryGetIsPlaying(out timelinePlaying)
            && timelinePlaying
            && (!VRWristMenuController.IsOpen
                || VRWristMenuController.IsTimelinePageOpen);
        if (!canAdjust)
        {
            FinishTimelineControlInput();
            return;
        }

        SteamVR_Controller.Device right = GetDevice(VR.Mode?.Right);
        if (right == null)
        {
            FinishTimelineControlInput();
            return;
        }

        bool blockedChord = right.GetPress(EVRButtonId.k_EButton_Axis1)
            || right.GetPress(EVRButtonId.k_EButton_ApplicationMenu);
        if (blockedChord)
        {
            FinishTimelineControlInput();
            return;
        }

        Vector2 axis = right.GetAxis(EVRButtonId.k_EButton_Axis0);
        bool gripHeld = right.GetPress(EVRButtonId.k_EButton_Grip);
        float fovAxis = !gripHeld && Mathf.Abs(axis.y) > CompositionStickDeadzone
            ? axis.y
            : 0f;
        float verticalAxis = gripHeld && Mathf.Abs(axis.y) > CompositionStickDeadzone
            ? axis.y
            : 0f;
        float yawAxis = gripHeld && Mathf.Abs(axis.x) > CompositionStickDeadzone
            ? axis.x
            : 0f;
        if (Mathf.Abs(fovAxis) <= 0.0001f
            && Mathf.Abs(verticalAxis) <= 0.0001f
            && Mathf.Abs(yawAxis) <= 0.0001f)
        {
            FinishTimelineControlInput();
            return;
        }

        if (!_timelineControlStickAdjusting)
        {
            _timelineControlStickAdjusting = true;
            _timelineFovStickValue = Mathf.Clamp(
                _settings.TimelineFovOverrideValue,
                VRTimelineService.MinCameraFov,
                VRTimelineService.MaxCameraFov);
            _timelineVerticalStickValue = _settings.TimelineVerticalOffset;
            _timelineYawStickValue = _settings.TimelineYawOffset;
        }

        float deltaTime = Time.unscaledDeltaTime;
        if (Mathf.Abs(fovAxis) > 0.0001f)
        {
            float speed = Mathf.Clamp(_settings.MmdFovAdjustSpeed, 5f, 60f);
            _timelineFovStickValue = Mathf.Clamp(
                _timelineFovStickValue + fovAxis * speed * deltaTime,
                VRTimelineService.MinCameraFov,
                VRTimelineService.MaxCameraFov);
            _settings.TimelineFovOverrideValue = _timelineFovStickValue;
            _settings.TimelineFovOverrideEnabled = true;
        }
        if (Mathf.Abs(verticalAxis) > 0.0001f)
        {
            _timelineVerticalStickValue = Mathf.Clamp(
                _timelineVerticalStickValue
                    + verticalAxis * TimelineVerticalAdjustSpeed * deltaTime,
                -10f,
                10f);
            _settings.TimelineVerticalOffset = _timelineVerticalStickValue;
        }
        if (Mathf.Abs(yawAxis) > 0.0001f)
        {
            _timelineYawStickValue = NormalizeYaw(
                _timelineYawStickValue
                    + yawAxis * TimelineYawAdjustSpeed * deltaTime);
            _settings.TimelineYawOffset = _timelineYawStickValue;
        }
        _timelineControlStickDirty = true;

        if (Time.unscaledTime >= _nextTimelineFovStickApply)
        {
            _nextTimelineFovStickApply =
                Time.unscaledTime + CompositionApplyInterval;
            ApplyTimelineCompositionOverride(false);
            ApplyTimelinePoseOffsetOverride(false);
            VRWristMenuController.Instance?.RefreshTimelineFovVisuals();
        }
    }

    private void FinishTimelineControlInput()
    {
        if (!_timelineControlStickAdjusting && !_timelineControlStickDirty)
            return;

        _timelineControlStickAdjusting = false;
        if (!_timelineControlStickDirty)
            return;

        _timelineControlStickDirty = false;
        ApplyTimelineCompositionOverride(false);
        ApplyTimelinePoseOffsetOverride(false);
        VRWristMenuController.Instance?.RefreshTimelineFovVisuals();
        try
        {
            _settings?.Save();
        }
        catch (Exception exception)
        {
            VRLog.Warn(
                "Unable to save the Timeline VR control parameters: " +
                exception.Message);
        }
    }

    private static float NormalizeYaw(float value)
    {
        while (value > 180f)
            value -= 360f;
        while (value < -180f)
            value += 360f;
        return value;
    }

    private static SteamVR_Controller.Device GetDevice(
        VRGIN.Controls.Controller controller)
    {
        if (controller == null || !controller.IsTracking)
            return null;
        SteamVR_TrackedObject tracked =
            controller.GetComponent<SteamVR_TrackedObject>();
        return tracked != null
            ? SteamVR_Controller.Input((int)tracked.index)
            : null;
    }

    private bool IsManualMovementLockedNow()
    {
        if (_sceneTransitionActive)
            return true;
        if (VRTimelineService.IsSceneMutationActive)
            return true;
        if (Time.frameCount <= _manualMovementLockedThroughFrame)
            return true;

        RefreshTimelinePlaybackLock(true);
        return _timelinePlaybackLocked;
    }

    private bool HandleRightStickPlaybackToggle()
    {
        if (_rightStickTransportConsumedFrame == Time.frameCount)
            return true;

        if (VRTimelineService.IsSceneMutationActive)
        {
            _rightStickTransportConsumedFrame = Time.frameCount;
            return true;
        }

        bool isPlaying;
        if (!VRTimelineService.TryGetIsPlaying(out isPlaying))
        {
            return false;
        }

        if (isPlaying)
        {
            bool wasPlaying;
            bool paused = VRTimelineService.PauseIfPlaying(out wasPlaying);
            if (!paused || !wasPlaying)
            {
                return false;
            }

            _rightStickTransportConsumedFrame = Time.frameCount;
            _manualMovementLockedThroughFrame = Time.frameCount;
            _timelinePlaybackLockKnown = false;
            RefreshTimelinePlaybackLock(false);
            ApplyCurrentMode(true);
            return true;
        }

        string status;
        if (!VRTimelineService.TogglePlayPause(out status))
        {
            return false;
        }

        // A successful Play invocation owns and consumes this edge. Some
        // Timeline builds expose IsPlaying one callback later; an immediate
        // read-back failure must not fall through and start MMDD as well.
        _rightStickTransportConsumedFrame = Time.frameCount;
        _manualMovementLockedThroughFrame = Time.frameCount;
        _timelinePlaybackLockKnown = false;
        RefreshTimelinePlaybackLock(true);
        ApplyCurrentMode(true);
        return true;
    }

    private void RefreshTimelinePlaybackLock(bool cancelInteractionsOnStart)
    {
        ResolveSettings();
        bool locked = false;
        bool stateAvailable = false;
        bool isPlaying;
        if (_settings != null && _settings.TimelineFollowCamera
            && VRTimelineService.TryGetIsPlaying(out isPlaying))
        {
            stateAvailable = true;
            locked = isPlaying;
        }

        bool started = locked
            && (!_timelinePlaybackLockKnown || !_timelinePlaybackLocked);
        _timelinePlaybackLocked = locked;
        _timelinePlaybackLockKnown = stateAvailable;
        if (started && cancelInteractionsOnStart)
        {
            GripMoveKKCharaStudioTool.CancelAllTimelineLockedInteraction();
            VRTwoHandScale.CancelTimelineInteraction();
            VRLog.Info("Timeline camera playback locked manual VR origin movement.");
        }
    }

    private void ApplyCurrentMode(bool forceResolve)
    {
        ResolveSettings();

        bool isPlaying;
        bool timelineAvailable = VRTimelineService.TryGetIsPlaying(out isPlaying);
        bool animationOnly = _settings != null && !_settings.TimelineFollowCamera;
        SetTimelineCameraSuppression(animationOnly, forceResolve);
        bool shouldSuspend = timelineAvailable && isPlaying && animationOnly;

        if (shouldSuspend)
        {
            if (_suspendedByThisPlugin)
                return;
            if (!TryResolveCameraSyncDriver(forceResolve))
            {
                if (!_missingCompanionLogged)
                {
                    _missingCompanionLogged = true;
                    VRLog.Warn(
                        "Timeline animation-only mode is selected, but KK_VR_CameraSync is not available yet.");
                }
                return;
            }

            try
            {
                _suspendMethod.Invoke(_cameraSyncDriver, null);
                _suspendedByThisPlugin = true;
                _missingCompanionLogged = false;
                VRLog.Info("Timeline camera following suspended; animation continues without moving the VR view.");
            }
            catch (Exception exception)
            {
                ClearCameraSyncDriver();
                VRLog.Warn("Unable to suspend Timeline camera following: " + exception.Message);
            }
            return;
        }

        ReleaseSuspension(true);
    }

    private void ReleaseSuspension(bool allowDeferredResume)
    {
        if (!_suspendedByThisPlugin)
            return;

        if (!TryResolveCameraSyncDriver(true))
            return;

        bool isPlaying;
        bool resumeDuringPlayback = VRTimelineService.TryGetIsPlaying(out isPlaying) && isPlaying;
        if (allowDeferredResume && resumeDuringPlayback
            && _resumeAndPrimeTimelineMethod == null)
        {
            if (!_deferredResumeLogged)
            {
                _deferredResumeLogged = true;
                VRLog.Warn(
                    "Camera follow will resume after Timeline stops because this CameraSync version cannot be primed safely.");
            }
            return;
        }

        try
        {
            MethodInfo resume = resumeDuringPlayback && _resumeAndPrimeTimelineMethod != null
                ? _resumeAndPrimeTimelineMethod
                : _resumeMethod;
            resume.Invoke(_cameraSyncDriver, null);
            _suspendedByThisPlugin = false;
            _deferredResumeLogged = false;
            VRLog.Info("Timeline camera following resumed from a fresh delta baseline without re-aligning the headset.");
        }
        catch (Exception exception)
        {
            ClearCameraSyncDriver();
            VRLog.Warn("Unable to resume Timeline camera following: " + exception.Message);
        }
    }

    private bool TryResolveCameraSyncDriver(bool force)
    {
        if (_cameraSyncDriver != null && _suspendMethod != null && _resumeMethod != null)
            return true;

        if (!force && Time.unscaledTime < _nextResolveTime)
            return false;
        _nextResolveTime = Time.unscaledTime + 1f;

        try
        {
            Assembly cameraSyncAssembly = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(
                        assembly.GetName().Name,
                        CameraSyncAssemblyName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    cameraSyncAssembly = assembly;
                    break;
                }
            }

            Type pluginType = cameraSyncAssembly == null
                ? null
                : cameraSyncAssembly.GetType(CameraSyncPluginTypeName, false);
            if (pluginType == null)
                return false;

            const BindingFlags staticFlags =
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            const BindingFlags instanceFlags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            PropertyInfo instanceProperty = pluginType.GetProperty("Instance", staticFlags);
            object pluginInstance = instanceProperty == null
                ? null
                : instanceProperty.GetValue(null, null);
            PropertyInfo driverProperty = pluginType.GetProperty("Driver", instanceFlags);
            object driver = pluginInstance == null || driverProperty == null
                ? null
                : driverProperty.GetValue(pluginInstance, null);
            if (driver == null)
                return false;

            MethodInfo suspend = driver.GetType().GetMethod("Suspend", instanceFlags);
            MethodInfo resume = driver.GetType().GetMethod("ResumeAndReset", instanceFlags);
            if (suspend == null || resume == null)
                return false;

            _cameraSyncDriver = driver;
            _suspendMethod = suspend;
            _resumeMethod = resume;
            _resumeAndPrimeTimelineMethod = driver.GetType().GetMethod(
                "ResumeAndPrimeTimelineState",
                instanceFlags);
            _setTimelineCameraSuppressedMethod = driver.GetType().GetMethod(
                "SetTimelineCameraSuppressed",
                instanceFlags);
            _setTimelineFovCompensationOverrideMethod = driver.GetType().GetMethod(
                "SetTimelineFovCompensationOverride",
                instanceFlags);
            _clearTimelineFovCompensationOverrideMethod = driver.GetType().GetMethod(
                "ClearTimelineFovCompensationOverride",
                instanceFlags);
            _setTimelineUserPoseOffsetMethod = driver.GetType().GetMethod(
                "SetTimelineUserPoseOffset",
                instanceFlags);
            _clearTimelineUserPoseOffsetMethod = driver.GetType().GetMethod(
                "ClearTimelineUserPoseOffset",
                instanceFlags);
            _setExternalVrCameraOwnerMethod = driver.GetType().GetMethod(
                "SetExternalVrCameraOwner",
                instanceFlags);
            _isTimelineCameraWriterActiveMethod = driver.GetType().GetMethod(
                "IsTimelineCameraWriterActive",
                instanceFlags);
            _timelineSuppressionStateKnown = false;
            _timelineCompositionStateKnown = false;
            _timelinePoseOffsetStateKnown = false;
            _externalCameraOwnerStateKnown = false;
            _missingCompanionLogged = false;
            return true;
        }
        catch
        {
            ClearCameraSyncDriver();
            return false;
        }
    }

    private void ClearCameraSyncDriver()
    {
        _cameraSyncDriver = null;
        _suspendMethod = null;
        _resumeMethod = null;
        _resumeAndPrimeTimelineMethod = null;
        _setTimelineCameraSuppressedMethod = null;
        _setTimelineFovCompensationOverrideMethod = null;
        _clearTimelineFovCompensationOverrideMethod = null;
        _setTimelineUserPoseOffsetMethod = null;
        _clearTimelineUserPoseOffsetMethod = null;
        _setExternalVrCameraOwnerMethod = null;
        _isTimelineCameraWriterActiveMethod = null;
        _timelineSuppressionStateKnown = false;
        _timelineCompositionStateKnown = false;
        _timelinePoseOffsetStateKnown = false;
        _externalCameraOwnerStateKnown = false;
    }

    private void SetTimelineCameraSuppression(bool suppressed, bool forceResolve)
    {
        if (_timelineSuppressionStateKnown &&
            _timelineSuppressionApplied == suppressed)
        {
            return;
        }

        if (!TryResolveCameraSyncDriver(forceResolve) ||
            _setTimelineCameraSuppressedMethod == null)
        {
            return;
        }

        try
        {
            _setTimelineCameraSuppressedMethod.Invoke(
                _cameraSyncDriver,
                new object[] { suppressed });
            _timelineSuppressionApplied = suppressed;
            _timelineSuppressionStateKnown = true;
        }
        catch (Exception exception)
        {
            ClearCameraSyncDriver();
            VRLog.Warn("Unable to update CameraSync Timeline filtering: " + exception.Message);
        }
    }

    private bool ApplyTimelineCompositionOverride(bool forceResolve)
    {
        ResolveSettings();
        bool enabled = !_sceneTransitionActive
            && _settings != null
            && _settings.TimelineFovOverrideEnabled;
        float referenceFov = _settings == null
            ? VRTimelineService.DefaultCameraFov
            : Mathf.Clamp(
                _settings.TimelineFovOverrideValue,
                VRTimelineService.MinCameraFov,
                VRTimelineService.MaxCameraFov);

        if (_timelineCompositionStateKnown
            && _timelineCompositionEnabledApplied == enabled
            && Mathf.Abs(_timelineCompositionReferenceApplied - referenceFov) < 0.001f)
        {
            return _setTimelineFovCompensationOverrideMethod != null;
        }

        if (!TryResolveCameraSyncDriver(forceResolve)
            || _setTimelineFovCompensationOverrideMethod == null)
        {
            return false;
        }

        try
        {
            _setTimelineFovCompensationOverrideMethod.Invoke(
                _cameraSyncDriver,
                new object[] { enabled, referenceFov });
            _timelineCompositionEnabledApplied = enabled;
            _timelineCompositionReferenceApplied = referenceFov;
            _timelineCompositionStateKnown = true;
            return true;
        }
        catch (Exception exception)
        {
            ClearCameraSyncDriver();
            VRLog.Warn("Unable to update Timeline VR composition matching: " + exception.Message);
            return false;
        }
    }

    private bool ApplyTimelinePoseOffsetOverride(bool forceResolve)
    {
        ResolveSettings();
        float verticalOffset = _settings == null
            ? 0f
            : Mathf.Clamp(_settings.TimelineVerticalOffset, -10f, 10f);
        float yawOffset = _settings == null
            ? 0f
            : NormalizeYaw(_settings.TimelineYawOffset);

        if (_timelinePoseOffsetStateKnown
            && Mathf.Abs(_timelineVerticalOffsetApplied - verticalOffset) < 0.0001f
            && Mathf.Abs(Mathf.DeltaAngle(_timelineYawOffsetApplied, yawOffset)) < 0.001f)
        {
            return _setTimelineUserPoseOffsetMethod != null;
        }

        if (!TryResolveCameraSyncDriver(forceResolve)
            || _setTimelineUserPoseOffsetMethod == null)
        {
            return false;
        }

        try
        {
            _setTimelineUserPoseOffsetMethod.Invoke(
                _cameraSyncDriver,
                new object[] { verticalOffset, yawOffset });
            _timelineVerticalOffsetApplied = verticalOffset;
            _timelineYawOffsetApplied = yawOffset;
            _timelinePoseOffsetStateKnown = true;
            return true;
        }
        catch (Exception exception)
        {
            ClearCameraSyncDriver();
            VRLog.Warn("Unable to update Timeline VR pose offsets: " + exception.Message);
            return false;
        }
    }

    private void ApplyExternalMmdCameraOwnership(bool forceResolve)
    {
        bool freshPlaybackReport = VRMmddStateBridge.PlaybackReported
            && Time.realtimeSinceStartup - VRMmddStateBridge.PlaybackReportRealtime <= 1f;
        bool active = !_sceneTransitionActive
            && freshPlaybackReport
            && VRMmddStateBridge.PlaybackAvailable
            && VRMmddStateBridge.PlaybackIsPlaying
            && VRMmddStateBridge.DirectVrCameraOwner;
        SetExternalVrCameraOwner(active, forceResolve);
    }

    private bool QueryTimelineCameraWriterActive()
    {
        if (_sceneTransitionActive
            || !TryResolveCameraSyncDriver(false)
            || _isTimelineCameraWriterActiveMethod == null)
        {
            return false;
        }

        try
        {
            object result = _isTimelineCameraWriterActiveMethod.Invoke(
                _cameraSyncDriver,
                null);
            return result is bool && (bool)result;
        }
        catch (Exception exception)
        {
            ClearCameraSyncDriver();
            VRLog.Warn("Unable to query the active Timeline camera writer: " + exception.Message);
            return false;
        }
    }

    private void SetExternalVrCameraOwner(bool active, bool forceResolve)
    {
        if (_externalCameraOwnerStateKnown
            && _externalCameraOwnerApplied == active)
        {
            return;
        }

        if (!TryResolveCameraSyncDriver(forceResolve)
            || _setExternalVrCameraOwnerMethod == null)
        {
            return;
        }

        try
        {
            _setExternalVrCameraOwnerMethod.Invoke(
                _cameraSyncDriver,
                new object[] { active });
            _externalCameraOwnerApplied = active;
            _externalCameraOwnerStateKnown = true;
        }
        catch (Exception exception)
        {
            ClearCameraSyncDriver();
            VRLog.Warn("Unable to arbitrate the MMDD VR camera owner: " + exception.Message);
        }
    }

    private void ClearRuntimeCameraOverrides(bool forceResolve)
    {
        if (!TryResolveCameraSyncDriver(forceResolve))
            return;

        try
        {
            if (_clearTimelineFovCompensationOverrideMethod != null)
                _clearTimelineFovCompensationOverrideMethod.Invoke(_cameraSyncDriver, null);
            if (_clearTimelineUserPoseOffsetMethod != null)
                _clearTimelineUserPoseOffsetMethod.Invoke(_cameraSyncDriver, null);
            if (_setExternalVrCameraOwnerMethod != null)
                _setExternalVrCameraOwnerMethod.Invoke(_cameraSyncDriver, new object[] { false });
        }
        catch (Exception exception)
        {
            VRLog.Warn("Unable to clear runtime CameraSync overrides: " + exception.Message);
        }
        finally
        {
            _timelineCompositionStateKnown = false;
            _timelinePoseOffsetStateKnown = false;
            _externalCameraOwnerStateKnown = false;
        }
    }
}

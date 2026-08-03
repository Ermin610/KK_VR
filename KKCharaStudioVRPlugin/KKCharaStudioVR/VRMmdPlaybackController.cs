using System;
using System.Collections.Generic;
using Studio;
using UnityEngine;
using Valve.VR;
using VRGIN.Core;

namespace KKCharaStudioVR;

[DefaultExecutionOrder(-32000)]
internal sealed class VRMmdPlaybackController : MonoBehaviour
{
    private const float ReporterRetrySeconds = 2f;
    private const float ReporterStaleSeconds = 1f;
    private const float FovApplyInterval = 0.08f;
    private const float HeelRetrySeconds = 0.75f;
    private const float StickDeadzone = 0.15f;
    private const float YawAdjustSpeed = 60f;
    private const float RightStickReleaseTimeout = 1.25f;
    private const float CueOpacityApplyInterval = 0.06f;
    private const float CueOpacityMinimumStep = 0.0095f;

    public static VRMmdPlaybackController Instance { get; private set; }
    public static bool BlocksNormalInput => Instance != null
        && (Instance._presentationActive || Instance._awaitRightStickRelease);
    public static bool ConsumedPlaybackClickThisFrame =>
        Instance != null && Instance._playbackClickConsumedFrame == Time.frameCount;
    private KKCharaStudioVRSettings _settings;
    private bool _presentationActive;
    private bool _awaitRightStickRelease;
    private float _rightStickReleaseDeadline;
    private bool _lastReportedPlaying;
    private bool _cuePlaybackActive;
    private bool _fovAdjusting;
    private bool _fovDirty;
    private float _runtimeFov = VRMmddService.DefaultFixedFov;
    private float _runtimeYawOffset;
    private float _nextReporterRetry;
    private float _nextFovApply;
    private float _nextHeelRetry;
    private float _nextCueOpacityApply;
    private float _lastNormalized = -1f;
    private int _lastGeneration = -1;
    private int _playbackClickConsumedFrame = -1;
    private bool _resumeMmdOnLeftStick;
    private bool _applicationQuitting;
    private string _currentVmdPath;
    private int[] _targetObjectKeys = new int[0];
    private VRMmdCueSheet _effectiveCueSheet;
    private bool _hasCustomCueSheet;
    private int _appliedCueCount;
    private readonly Dictionary<int, VRClothingStateSnapshot> _baseline =
        new Dictionary<int, VRClothingStateSnapshot>();
    private readonly Dictionary<long, float> _baselineTransparency =
        new Dictionary<long, float>();
    private readonly Dictionary<long, float> _lastAppliedTransparency =
        new Dictionary<long, float>();
    private readonly Dictionary<long, byte> _cueStateByTargetPart =
        new Dictionary<long, byte>();
    private readonly Dictionary<int, VRClothingOpacityService.RuntimeOpacitySession>
        _runtimeOpacitySessions =
            new Dictionary<int, VRClothingOpacityService.RuntimeOpacitySession>();
    private readonly Dictionary<int, int> _pendingHighHeels = new Dictionary<int, int>();

    public string CurrentVmdPath => _currentVmdPath;
    public VRMmdCueSheet EffectiveCueSheet => _effectiveCueSheet;
    public bool HasCustomCueSheet => _hasCustomCueSheet;
    public bool IsPresentationActive => _presentationActive;
    internal static bool IsPresentationCameraControlActive =>
        Instance != null && Instance._presentationActive;
    internal static float PresentationYawOffset =>
        Instance != null ? Instance._runtimeYawOffset : 0f;

    private void Awake()
    {
        Instance = this;
        ResolveSettings();
        TryInstallReporter();
    }

    private void OnDisable()
    {
        RestoreBaseline();
        ResetPresentationYaw(true);
        SetPresentationActive(false);
        _awaitRightStickRelease = false;
    }

    private void OnApplicationQuit()
    {
        _applicationQuitting = true;
    }

    private void OnDestroy()
    {
        RestoreBaseline();
        ResetPresentationYaw(true);
        SetPresentationActive(false);
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        ResolveSettings();
        EnsureReporterHealth();
        ProcessPendingHighHeels();
        UpdateRightStickReleaseLatch();

        bool playbackAvailable = VRMmddStateBridge.PlaybackReported
            && VRMmddStateBridge.PlaybackAvailable
            && Time.realtimeSinceStartup - VRMmddStateBridge.PlaybackReportRealtime <= ReporterStaleSeconds;
        bool isPlaying = playbackAvailable && VRMmddStateBridge.PlaybackIsPlaying;

        bool shouldPresent = isPlaying
            && _settings != null
            && _settings.HideHandsAndUiDuringMmd;
        SetPresentationActive(shouldPresent);
        if (_presentationActive)
            UpdatePresentationInput();

        UpdateClothingCues(playbackAvailable, isPlaying);
        _lastReportedPlaying = isPlaying;
    }

    public void PrepareForPackageChange()
    {
        ClearLeftStickTransportResume();
        RestoreBaseline();
        ResetCuePlaybackState();
    }

    public void PrepareForCharacterReplacement()
    {
        ClearLeftStickTransportResume();
        RestoreBaseline();
        ResetCuePlaybackState();
        SetPresentationActive(false);
        _lastReportedPlaying = false;
    }

    public void NotifyCharacterReplacementRebound()
    {
        ClearLeftStickTransportResume();
        ResetCuePlaybackState();
        SetPresentationActive(false);
        _lastReportedPlaying = false;
    }

    public void NotifyPackageLoadFailed()
    {
        ClearLeftStickTransportResume();
        ResetCuePlaybackState();
        ReloadCueSheet();
    }

    public void NotifyPackageLoaded(string motionPath, int[] targetObjectKeys)
    {
        ClearLeftStickTransportResume();
        RestoreBaseline();
        ResetPresentationYaw(true);
        _currentVmdPath = motionPath;
        _targetObjectKeys = targetObjectKeys ?? new int[0];
        ResetCuePlaybackState();
        ReloadCueSheet();
    }

    public void NotifyReturnedToStart()
    {
        ClearLeftStickTransportResume();
        RestoreBaseline();
        ResetCuePlaybackState();
    }

    public void RequestHighHeelsRefresh(int[] objectKeys)
    {
        if (objectKeys == null)
            return;
        foreach (int objectKey in objectKeys)
        {
            if (objectKey >= 0)
                _pendingHighHeels[objectKey] = 0;
        }
        _nextHeelRetry = 0f;
    }

    public void RequestHighHeelsRefresh(int objectKey)
    {
        if (objectKey < 0)
            return;
        _pendingHighHeels[objectKey] = 0;
        _nextHeelRetry = 0f;
    }

    public bool CreateCustomCueSheet(out string status)
    {
        VRMmdCueSheet sheet;
        string presetId = _settings != null
            ? _settings.MmdClothingCuePresetId
            : VRMmdCueSheetStore.DefaultPresetId;
        if (!VRMmdCueSheetStore.CreateCustomFromGlobal(
                _currentVmdPath, presetId, out sheet, out status))
            return false;
        _effectiveCueSheet = sheet;
        _hasCustomCueSheet = true;
        RestartCuesAtCurrentFrame();
        return true;
    }

    public bool SaveCurrentCustomCueSheet(out string status)
    {
        if (!_hasCustomCueSheet)
        {
            status = "请先自定义当前 VMD";
            return false;
        }
        if (!VRMmdCueSheetStore.SaveCustom(_currentVmdPath, _effectiveCueSheet, out status))
            return false;
        ReloadCueSheet();
        RestartCuesAtCurrentFrame();
        return true;
    }

    public bool RestoreGlobalCueSheet(out string status)
    {
        if (!VRMmdCueSheetStore.DeleteCustom(_currentVmdPath, out status))
            return false;
        ReloadCueSheet();
        RestartCuesAtCurrentFrame();
        return true;
    }

    public void NotifyCueSettingChanged(bool enabled)
    {
        if (!enabled)
        {
            RestoreBaseline();
            ResetCuePlaybackState();
            return;
        }

        if (VRMmddStateBridge.PlaybackReported
            && VRMmddStateBridge.PlaybackAvailable
            && VRMmddStateBridge.PlaybackIsPlaying
            && _effectiveCueSheet?.Cues != null
            && _targetObjectKeys.Length > 0)
        {
            CaptureBaseline();
            _cuePlaybackActive = true;
            _appliedCueCount = 0;
            _lastNormalized = -1f;
            RestartCuesAtCurrentFrame();
        }
    }

    public void NotifyCuePresetChanged()
    {
        bool wasActive = _cuePlaybackActive;
        if (wasActive)
            RestoreBaseline(false);
        ReloadCueSheet();
        if (wasActive)
            RestartCuesAtCurrentFrame();
    }

    public void NotifyTargetMotionCleared(int objectKey)
    {
        ClearLeftStickTransportResume();
        RestoreTargetBaseline(objectKey);
        List<int> remaining = new List<int>();
        foreach (int key in _targetObjectKeys)
        {
            if (key != objectKey)
                remaining.Add(key);
        }
        _targetObjectKeys = remaining.ToArray();
        _cuePlaybackActive = false;
        _appliedCueCount = 0;
        _lastNormalized = -1f;
        _lastGeneration = -1;
        _lastReportedPlaying = false;
        _cueStateByTargetPart.Clear();
        ResetPresentationYaw(true);
        SetPresentationActive(false);
        VRMmdCameraAnchorController.ResetForSceneTransition();
    }

    public void ReloadCueSheet()
    {
        VRMmdCueSheet sheet;
        bool custom;
        string ignored;
        if (VRMmdCueSheetStore.TryGetEffective(
            _currentVmdPath,
            _settings != null
                ? _settings.MmdClothingCuePresetId
                : VRMmdCueSheetStore.DefaultPresetId,
            out sheet,
            out custom,
            out ignored))
        {
            _effectiveCueSheet = sheet;
            _hasCustomCueSheet = custom;
        }
        else
        {
            _effectiveCueSheet = null;
            _hasCustomCueSheet = false;
        }
    }

    public static void PrepareForSceneTransition()
    {
        VRMmddStateBridge.ResetPlayback();
        if (Instance != null)
            Instance.ResetPresentationYaw(true);
        VRMmdCameraAnchorController.ResetForSceneTransition();
        if (Instance == null)
            return;
        Instance.RestoreBaseline();
        Instance.ResetCuePlaybackState();
        Instance.SetPresentationActive(false);
        Instance.ClearLeftStickTransportResume();
        Instance._pendingHighHeels.Clear();
    }

    internal static bool TryHandleLeftStickPlaybackToggle()
    {
        return Instance != null && Instance.HandleLeftStickPlaybackToggle();
    }

    internal static void CancelLeftStickTransportResume()
    {
        if (Instance != null)
            Instance.ClearLeftStickTransportResume();
    }

    internal static void ConsumeLeftStickPlaybackClick()
    {
        if (Instance != null)
            Instance._playbackClickConsumedFrame = Time.frameCount;
    }

    private void ResolveSettings()
    {
        if (_settings == null && VR.Manager != null && VR.Manager.Context != null)
            _settings = VR.Manager.Context.Settings as KKCharaStudioVRSettings;
    }

    private void EnsureReporterHealth()
    {
        if (_applicationQuitting)
            return;
        if (Time.unscaledTime < _nextReporterRetry)
            return;
        bool stale = !VRMmddStateBridge.PlaybackReported
            || Time.realtimeSinceStartup - VRMmddStateBridge.PlaybackReportRealtime > ReporterStaleSeconds;
        if (stale)
            TryInstallReporter();
    }

    private void TryInstallReporter()
    {
        _nextReporterRetry = Time.unscaledTime + ReporterRetrySeconds;
        string ignored;
        VRMmddService.EnsurePlaybackStateReporter(out ignored);
    }

    private void SetPresentationActive(bool active)
    {
        if (_presentationActive == active)
            return;
        bool wasActive = _presentationActive;
        _presentationActive = active;
        if (active)
        {
            _awaitRightStickRelease = false;
            _rightStickReleaseDeadline = 0f;
        }
        else if (wasActive)
        {
            _awaitRightStickRelease = true;
            _rightStickReleaseDeadline = Time.unscaledTime + RightStickReleaseTimeout;
        }
        _fovAdjusting = false;
        if (!active && _fovDirty)
            CommitRuntimeFov();

        if (VRHandModelManager.Instance != null)
            VRHandModelManager.Instance.SetPresentationSuppressed(active);
        if (VRWristMenuController.Instance != null)
            VRWristMenuController.Instance.SetPresentationSuppressed(active);
        if (VRQuickActions.Instance != null)
            VRQuickActions.Instance.SetPresentationSuppressed(active);
    }

    private void UpdateRightStickReleaseLatch()
    {
        if (!_awaitRightStickRelease || _presentationActive)
            return;

        if (Time.unscaledTime >= _rightStickReleaseDeadline)
        {
            _awaitRightStickRelease = false;
            return;
        }

        SteamVR_Controller.Device right = GetDevice(VR.Mode?.Right);
        if (right == null)
        {
            _awaitRightStickRelease = false;
            return;
        }

        Vector2 stick = right.GetAxis(EVRButtonId.k_EButton_Axis0);
        if (!IsFinite(stick.x)
            || !IsFinite(stick.y)
            || (Mathf.Abs(stick.x) <= StickDeadzone
            && Mathf.Abs(stick.y) <= StickDeadzone)
        )
        {
            _awaitRightStickRelease = false;
        }
    }

    private void UpdatePresentationInput()
    {
        SteamVR_Controller.Device left = GetDevice(VR.Mode?.Left);
        SteamVR_Controller.Device right = GetDevice(VR.Mode?.Right);

        if (left != null)
        {
            bool leftChorded = left.GetPress(EVRButtonId.k_EButton_Grip)
                || left.GetPress(EVRButtonId.k_EButton_Axis1)
                || left.GetPress(EVRButtonId.k_EButton_ApplicationMenu);
            if (!leftChorded && left.GetPressDown(EVRButtonId.k_EButton_Axis0))
            {
                // MMD transport belongs exclusively to the left hand.
                if (HandleLeftStickPlaybackToggle())
                    return;
            }
        }

        // The right hand remains available for continuous FOV/yaw composition
        // adjustment and reset even if the left controller is unavailable.
        if (right == null)
        {
            if (_fovAdjusting)
            {
                _fovAdjusting = false;
                CommitRuntimeFov();
            }
            return;
        }

        // Timeline owns the right-hand camera controls while its control space
        // is active. Do not let MMDD consume the same stick or reset button.
        if (VRTimelineCameraFollowController.IsTimelineControlSpaceActive)
        {
            if (_fovAdjusting)
            {
                _fovAdjusting = false;
                CommitRuntimeFov();
            }
            return;
        }

        bool resetChorded = right.GetPress(EVRButtonId.k_EButton_Grip)
            || right.GetPress(EVRButtonId.k_EButton_Axis1)
            || right.GetPress(EVRButtonId.k_EButton_ApplicationMenu);
        if (!resetChorded && right.GetPressDown(EVRButtonId.k_EButton_A))
        {
            ResetPresentationComposition(right);
            return;
        }

        Vector2 stick = right.GetAxis(EVRButtonId.k_EButton_Axis0);
        float fovAxis = Mathf.Abs(stick.y) > StickDeadzone ? stick.y : 0f;
        float yawAxis = Mathf.Abs(stick.x) > StickDeadzone ? stick.x : 0f;

        if (Mathf.Abs(yawAxis) > 0.0001f)
        {
            _runtimeYawOffset = NormalizeYaw(
                _runtimeYawOffset
                + yawAxis * YawAdjustSpeed * Time.unscaledDeltaTime);
        }

        if (Mathf.Abs(fovAxis) <= 0.0001f)
        {
            if (_fovAdjusting)
            {
                _fovAdjusting = false;
                CommitRuntimeFov();
            }
            return;
        }

        if (!_fovAdjusting)
        {
            _fovAdjusting = true;
            string ignored;
            VRMmddService.RefreshFixedFov(out ignored);
            _runtimeFov = VRMmddStateBridge.FixedFovReported
                ? VRMmddStateBridge.FixedFovValue
                : VRMmddService.DefaultFixedFov;
        }

        float speed = _settings != null ? _settings.MmdFovAdjustSpeed : 20f;
        _runtimeFov = Mathf.Clamp(
            _runtimeFov + fovAxis * speed * Time.unscaledDeltaTime,
            VRMmddService.MinFixedFov,
            VRMmddService.MaxFixedFov);
        _fovDirty = true;
        if (Time.unscaledTime >= _nextFovApply)
        {
            _nextFovApply = Time.unscaledTime + FovApplyInterval;
            string ignored;
            VRMmddService.SetFixedFovRuntime(_runtimeFov, out ignored);
        }
    }

    private bool HandleLeftStickPlaybackToggle()
    {
        if (_playbackClickConsumedFrame == Time.frameCount)
            return true;

        if (_resumeMmdOnLeftStick)
        {
            _playbackClickConsumedFrame = Time.frameCount;
            ResumeLeftStickMmdTransport();
            return true;
        }

        if (!IsFreshMmdPlaybackPlaying())
            return false;

        string mmdStatus;
        bool mmdPaused = VRMmddService.PausePlayback(out mmdStatus);
        if (!mmdPaused)
            VRLog.Warn(mmdStatus);

        if (!mmdPaused)
            return false;

        _resumeMmdOnLeftStick = true;
        _playbackClickConsumedFrame = Time.frameCount;
        SetPresentationActive(false);
        return true;
    }

    private void ResumeLeftStickMmdTransport()
    {
        if (!_resumeMmdOnLeftStick)
            return;
        if (!IsFreshMmdPlaybackPlaying())
        {
            string mmdStatus;
            if (!VRMmddService.StartPlayback(out mmdStatus))
            {
                VRLog.Warn(mmdStatus);
                return;
            }
        }
        ClearLeftStickTransportResume();
    }

    private bool IsFreshMmdPlaybackPlaying()
    {
        return VRMmddStateBridge.PlaybackReported
            && VRMmddStateBridge.PlaybackAvailable
            && VRMmddStateBridge.PlaybackIsPlaying
            && Time.realtimeSinceStartup - VRMmddStateBridge.PlaybackReportRealtime
                <= ReporterStaleSeconds;
    }

    private void ClearLeftStickTransportResume()
    {
        _resumeMmdOnLeftStick = false;
    }

    private void CommitRuntimeFov()
    {
        if (!_fovDirty)
            return;
        _fovDirty = false;
        string status;
        if (!VRMmddService.SetFixedFov(_runtimeFov, out status))
            VRLog.Warn(status);
    }

    private void ResetPresentationComposition(SteamVR_Controller.Device right)
    {
        _runtimeYawOffset = 0f;
        _runtimeFov = VRMmddService.DefaultFixedFov;
        _fovAdjusting = false;
        _fovDirty = false;

        // Reset yaw before MMDD evaluates the default FOV so the final camera
        // correction restores the authored direction instead of preserving the
        // previous user offset.
        VRMmdCameraAnchorController.RefreshPresentationYawNow();
        string status;
        if (!VRMmddService.SetFixedFov(_runtimeFov, out status))
            VRLog.Warn("Unable to reset MMD VR composition: " + status);
        VRMmdCameraAnchorController.RefreshPresentationYawNow();

        try
        {
            right.TriggerHapticPulse(900, EVRButtonId.k_EButton_Axis0);
        }
        catch
        {
            // Reset success must not depend on haptics support.
        }
        VRLog.Info("MMD VR composition reset to authored yaw and FOV 53.13.");
    }

    private void ResetPresentationYaw(bool restoreCurrentRig)
    {
        if (Mathf.Abs(_runtimeYawOffset) <= 0.0001f)
            return;
        _runtimeYawOffset = 0f;
        if (restoreCurrentRig)
            VRMmdCameraAnchorController.RefreshPresentationYawNow();
    }

    private static float NormalizeYaw(float value)
    {
        while (value > 180f)
            value -= 360f;
        while (value < -180f)
            value += 360f;
        return value;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private void UpdateClothingCues(bool playbackAvailable, bool isPlaying)
    {
        bool enabled = _settings != null && _settings.MmdClothingCueEnabled;
        if (!enabled)
        {
            if (_cuePlaybackActive || _baseline.Count > 0)
                RestoreBaseline();
            ResetCuePlaybackState();
            return;
        }

        if (!playbackAvailable || _effectiveCueSheet?.Cues == null
            || string.IsNullOrEmpty(_currentVmdPath) || _targetObjectKeys.Length == 0)
            return;

        if (isPlaying && !_lastReportedPlaying && !_cuePlaybackActive)
        {
            CaptureBaseline();
            _cuePlaybackActive = true;
            _appliedCueCount = 0;
            _lastNormalized = -1f;
        }

        if (!_cuePlaybackActive)
            return;

        float start = VRMmddStateBridge.PlaybackStartFrame;
        float end = VRMmddStateBridge.PlaybackEndFrame;
        float span = end - start;
        if (span <= 0.0001f)
            return;
        float normalized = Mathf.Clamp01((VRMmddStateBridge.PlaybackCurrentFrame - start) / span);

        bool generationChanged = _lastGeneration >= 0
            && _lastGeneration != VRMmddStateBridge.PlaybackGeneration;
        bool movedBackward = _lastNormalized >= 0f && normalized + 0.0005f < _lastNormalized;
        if (generationChanged || movedBackward)
        {
            RestoreBaseline(false);
            _appliedCueCount = 0;
        }

        float percent = normalized * 100f;
        bool stateChanged = ApplyCuesThrough(percent);
        if (stateChanged || Time.unscaledTime >= _nextCueOpacityApply)
        {
            ApplyOpacityCues(percent, stateChanged);
            _nextCueOpacityApply = Time.unscaledTime + CueOpacityApplyInterval;
        }
        _lastNormalized = normalized;
        _lastGeneration = VRMmddStateBridge.PlaybackGeneration;
    }

    private void CaptureBaseline()
    {
        RestoreRuntimeOpacitySessions();
        _baseline.Clear();
        _baselineTransparency.Clear();
        foreach (int objectKey in _targetObjectKeys)
        {
            VRClothingStateSnapshot snapshot;
            string ignored;
            if (VRCharacterClothingService.TryCaptureStates(objectKey, out snapshot, out ignored))
                _baseline[objectKey] = snapshot;

            OCIChar character;
            if (!VRCharacterClothingService.TryGetCharacter(
                    objectKey, out character, out ignored)
                || character == null
                || character.charInfo == null)
            {
                continue;
            }

            for (int partId = 0;
                 partId < VRCharacterClothingService.PartCount;
                 partId++)
            {
                if (!VRClothingOpacityService.HasPart(character.charInfo, partId))
                    continue;
                VRClothingOpacityInfo info;
                if (VRClothingOpacityService.TryInspect(
                        character.charInfo, partId, out info, out ignored))
                {
                    // Keep the most transparent material as the floor.  A cue
                    // must never make a user's pre-authored transparent look
                    // more opaque.
                    _baselineTransparency[BuildTargetPartKey(objectKey, partId)] =
                        Mathf.Clamp01(1f - info.MinimumOpacity);
                }
            }
        }
        _lastAppliedTransparency.Clear();
        _cueStateByTargetPart.Clear();
        _nextCueOpacityApply = 0f;
    }

    private void RestoreBaseline()
    {
        RestoreBaseline(true);
    }

    private void RestoreBaseline(bool clear)
    {
        // Restore live materials before changing clothing states.  Half/full
        // state changes can swap renderer objects, and restoring first ensures
        // both the outgoing and currently visible AZ materials are exact.
        RestoreRuntimeOpacitySessions();
        foreach (VRClothingStateSnapshot snapshot in new List<VRClothingStateSnapshot>(_baseline.Values))
        {
            string ignored;
            VRCharacterClothingService.TryRestoreStates(snapshot, out ignored);
        }
        if (clear)
        {
            _baseline.Clear();
            _baselineTransparency.Clear();
        }
        _lastAppliedTransparency.Clear();
        _cueStateByTargetPart.Clear();
    }

    private bool ApplyCuesThrough(float percent)
    {
        bool changed = false;
        List<VRMmdCue> cues = _effectiveCueSheet.Cues;
        while (_appliedCueCount < cues.Count && cues[_appliedCueCount].Percent <= percent + 0.0001f)
        {
            VRMmdCue cue = cues[_appliedCueCount++];
            if (!cue.ApplyState)
                continue;
            foreach (int objectKey in _targetObjectKeys)
            {
                if (cue.State == 3)
                    RestoreRuntimeOpacityPart(objectKey, cue.PartId);
                string status;
                if (VRCharacterClothingService.TrySetPartState(
                    objectKey,
                    cue.PartId,
                    cue.State,
                    out status))
                {
                    _cueStateByTargetPart[BuildTargetPartKey(objectKey, cue.PartId)] = cue.State;
                    changed = true;
                }
                else
                {
                    VRLog.Warn("MMD clothing cue skipped: " + status);
                }
            }
        }
        return changed;
    }

    private void ApplyOpacityCues(float percent, bool force)
    {
        if (_effectiveCueSheet?.Cues == null)
            return;

        HashSet<int> parts = new HashSet<int>();
        foreach (VRMmdCue cue in _effectiveCueSheet.Cues)
        {
            if (cue != null && cue.TargetTransparency >= 0f)
                parts.Add(cue.PartId);
        }

        foreach (int objectKey in _targetObjectKeys)
        {
            foreach (int partId in parts)
            {
                long key = BuildTargetPartKey(objectKey, partId);
                byte state;
                if (_cueStateByTargetPart.TryGetValue(key, out state) && state == 3)
                {
                    RestoreRuntimeOpacityPart(objectKey, partId);
                    continue;
                }

                float baseline;
                if (!_baselineTransparency.TryGetValue(key, out baseline))
                    baseline = 0f;
                float transparency = EvaluateTransparency(partId, percent, baseline);
                if (transparency <= baseline + 0.0005f)
                {
                    RestoreRuntimeOpacityPart(objectKey, partId);
                    continue;
                }

                float previous;
                if (!force
                    && _lastAppliedTransparency.TryGetValue(key, out previous)
                    && Mathf.Abs(previous - transparency) < CueOpacityMinimumStep)
                {
                    continue;
                }

                VRClothingOpacityService.RuntimeOpacitySession session;
                if (!_runtimeOpacitySessions.TryGetValue(objectKey, out session))
                {
                    OCIChar character;
                    string createStatus;
                    if (!VRCharacterClothingService.TryGetCharacter(
                            objectKey, out character, out createStatus)
                        || character == null
                        || !VRClothingOpacityService.TryCreateRuntimeSession(
                            character.charInfo, out session, out createStatus))
                    {
                        VRLog.Warn("MMD transparency cue skipped: " + createStatus);
                        continue;
                    }
                    _runtimeOpacitySessions[objectKey] = session;
                }

                string status;
                if (session.TrySetPartTransparency(partId, transparency, out status))
                    _lastAppliedTransparency[key] = transparency;
                else if (!string.IsNullOrEmpty(status))
                    VRLog.Warn("MMD transparency cue skipped: " + status);
            }
        }
    }

    private float EvaluateTransparency(int partId, float percent, float baseline)
    {
        float current = Mathf.Clamp01(baseline);
        foreach (VRMmdCue cue in _effectiveCueSheet.Cues)
        {
            if (cue == null || cue.PartId != partId || cue.TargetTransparency < 0f)
                continue;

            float desired = Mathf.Max(baseline, cue.TargetTransparency / 100f);
            float start = cue.FadeStartPercent >= 0f
                ? Mathf.Min(cue.FadeStartPercent, cue.Percent)
                : cue.Percent;
            if (percent + 0.0001f < start)
                return current;
            if (cue.Percent <= start + 0.0001f || percent >= cue.Percent)
            {
                current = desired;
                continue;
            }

            float t = Mathf.Clamp01((percent - start) / (cue.Percent - start));
            t = t * t * (3f - 2f * t);
            return Mathf.Lerp(current, desired, t);
        }
        return current;
    }

    private void RestoreRuntimeOpacityPart(int objectKey, int partId)
    {
        VRClothingOpacityService.RuntimeOpacitySession session;
        if (_runtimeOpacitySessions.TryGetValue(objectKey, out session))
            session.RestorePart(partId);
        _lastAppliedTransparency.Remove(BuildTargetPartKey(objectKey, partId));
    }

    private void RestoreRuntimeOpacitySessions()
    {
        foreach (VRClothingOpacityService.RuntimeOpacitySession session in
                 new List<VRClothingOpacityService.RuntimeOpacitySession>(
                     _runtimeOpacitySessions.Values))
        {
            session.Dispose();
        }
        _runtimeOpacitySessions.Clear();
        _lastAppliedTransparency.Clear();
    }

    private void RestoreTargetBaseline(int objectKey)
    {
        VRClothingOpacityService.RuntimeOpacitySession session;
        if (_runtimeOpacitySessions.TryGetValue(objectKey, out session))
        {
            session.Dispose();
            _runtimeOpacitySessions.Remove(objectKey);
        }

        VRClothingStateSnapshot snapshot;
        if (_baseline.TryGetValue(objectKey, out snapshot))
        {
            string ignored;
            VRCharacterClothingService.TryRestoreStates(snapshot, out ignored);
            _baseline.Remove(objectKey);
        }

        RemoveTargetPartEntries(_baselineTransparency, objectKey);
        RemoveTargetPartEntries(_lastAppliedTransparency, objectKey);
        RemoveTargetPartEntries(_cueStateByTargetPart, objectKey);
    }

    private static void RemoveTargetPartEntries<T>(Dictionary<long, T> values, int objectKey)
    {
        List<long> keys = new List<long>();
        foreach (long key in values.Keys)
        {
            if ((int)(key >> 32) == objectKey)
                keys.Add(key);
        }
        foreach (long key in keys)
            values.Remove(key);
    }

    private static long BuildTargetPartKey(int objectKey, int partId)
    {
        return ((long)objectKey << 32) ^ (uint)partId;
    }

    private void RestartCuesAtCurrentFrame()
    {
        if (!_cuePlaybackActive)
            return;
        RestoreBaseline(false);
        _appliedCueCount = 0;
        float span = VRMmddStateBridge.PlaybackEndFrame - VRMmddStateBridge.PlaybackStartFrame;
        if (span > 0.0001f)
        {
            float normalized = Mathf.Clamp01(
                (VRMmddStateBridge.PlaybackCurrentFrame - VRMmddStateBridge.PlaybackStartFrame) / span);
            float percent = normalized * 100f;
            ApplyCuesThrough(percent);
            ApplyOpacityCues(percent, true);
            _lastNormalized = normalized;
        }
    }

    private void ResetCuePlaybackState()
    {
        _cuePlaybackActive = false;
        _appliedCueCount = 0;
        _lastNormalized = -1f;
        _lastGeneration = -1;
        _lastReportedPlaying = false;
        _cueStateByTargetPart.Clear();
        _nextCueOpacityApply = 0f;
    }

    private void ProcessPendingHighHeels()
    {
        if (_pendingHighHeels.Count == 0 || Time.unscaledTime < _nextHeelRetry)
            return;
        _nextHeelRetry = Time.unscaledTime + HeelRetrySeconds;

        if (_settings == null || !_settings.AutoApplyHighHeelsPreset)
        {
            _pendingHighHeels.Clear();
            return;
        }

        foreach (int objectKey in new List<int>(_pendingHighHeels.Keys))
        {
            bool deferred;
            string status;
            if (VRMmddService.TryApplyHighHeelsPresetOrAutomatic(
                objectKey,
                out deferred,
                out status))
            {
                _pendingHighHeels.Remove(objectKey);
                VRLog.Info(status);
                continue;
            }

            int attempts = _pendingHighHeels[objectKey] + 1;
            if (!deferred || attempts >= 20)
            {
                _pendingHighHeels.Remove(objectKey);
                VRLog.Warn(status);
            }
            else
            {
                _pendingHighHeels[objectKey] = attempts;
            }
        }
    }

    private static SteamVR_Controller.Device GetDevice(VRGIN.Controls.Controller controller)
    {
        if (controller == null || !controller.IsTracking)
            return null;
        SteamVR_TrackedObject tracked = controller.GetComponent<SteamVR_TrackedObject>();
        return tracked != null ? SteamVR_Controller.Input((int)tracked.index) : null;
    }
}

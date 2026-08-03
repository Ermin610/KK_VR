using System;
using System.Reflection;
using UnityEngine;

namespace KKCharaStudioVR;

internal sealed class VRTimelineMutationSession
{
    public bool Available;
    public bool WasPlaying;
    public float PlaybackTime;
    internal bool OwnsMutationLock;
    internal bool Completed;
}

internal static class VRTimelineService
{
    public const float DefaultCameraFov = 53.13f;
    public const float MinCameraFov = 20f;
    public const float MaxCameraFov = 120f;

    private static Type _timelineType;
    private static PropertyInfo _isPlayingProperty;
    private static PropertyInfo _playbackTimeProperty;
    private static MethodInfo _playMethod;
    private static MethodInfo _pauseMethod;
    private static MethodInfo _seekMethod;
    private static FieldInfo _selfField;
    private static MethodInfo _interpolateMethod;
    private static int _sceneMutationDepth;

    internal static bool IsSceneMutationActive
    {
        get { return _sceneMutationDepth > 0; }
    }

    public static bool TryGetIsPlaying(out bool isPlaying)
    {
        isPlaying = false;
        if (!ResolvePlaybackState())
            return false;

        try
        {
            isPlaying = (bool)_isPlayingProperty.GetValue(null, null);
            return true;
        }
        catch
        {
            Clear();
            return false;
        }
    }

    public static bool TogglePlayPause(out string status)
    {
        if (!ResolveControls())
        {
            status = "未检测到 Timeline 1.5.x";
            return false;
        }

        bool isPlaying;
        if (!TryGetIsPlaying(out isPlaying))
        {
            status = "未检测到 Timeline 1.5.x";
            return false;
        }

        try
        {
            if (isPlaying)
            {
                _pauseMethod.Invoke(null, null);
                status = "Timeline 已暂停";
            }
            else
            {
                _playMethod.Invoke(null, null);
                status = "Timeline 开始播放";
            }

            return true;
        }
        catch (Exception exception)
        {
            status = "Timeline 操作失败";
            VRGIN.Core.VRLog.Warn("Timeline wrist action failed: {0}", exception);
            Clear();
            return false;
        }
    }

    /// <summary>
    /// Pauses Timeline without ever toggling a paused Timeline back to Play.
    /// Returns false only when Timeline could not be resolved or invoked.
    /// </summary>
    public static bool PauseIfPlaying(out bool wasPlaying)
    {
        wasPlaying = false;
        if (!ResolveControls())
            return false;

        bool isPlaying;
        if (!TryGetIsPlaying(out isPlaying))
            return false;
        if (!isPlaying)
            return true;

        try
        {
            _pauseMethod.Invoke(null, null);
            wasPlaying = true;
            return true;
        }
        catch (Exception exception)
        {
            VRGIN.Core.VRLog.Warn("Timeline pause-for-movement failed: {0}", exception);
            Clear();
            return false;
        }
    }

    public static bool PauseForSceneMutation(
        out VRTimelineMutationSession session,
        out string status)
    {
        session = new VRTimelineMutationSession();
        session.OwnsMutationLock = true;
        _sceneMutationDepth++;
        status = null;
        if (!ResolveControls())
            return true;

        bool isPlaying;
        float playbackTime;
        if (!TryGetIsPlaying(out isPlaying) || !TryGetPlaybackTime(out playbackTime))
        {
            status = "无法读取 Timeline 播放状态";
            return false;
        }

        session.Available = true;
        session.WasPlaying = isPlaying;
        session.PlaybackTime = playbackTime;
        if (!isPlaying)
            return true;

        try
        {
            _pauseMethod.Invoke(null, null);
            status = "Timeline 已在角色变更前暂停";
            return true;
        }
        catch (Exception exception)
        {
            status = "角色变更前无法暂停 Timeline";
            VRGIN.Core.VRLog.Warn("Timeline pause-for-scene-mutation failed: {0}", exception);
            Clear();
            return false;
        }
    }

    public static bool RestoreAfterSceneMutation(
        VRTimelineMutationSession session,
        bool sceneChanged,
        bool targetReady,
        out string status)
    {
        status = null;
        if (session == null)
            return true;
        if (session.Completed)
            return true;

        try
        {
            if (!session.Available)
                return true;
            if (!ResolveControls())
            {
                status = "Timeline 恢复接口不可用";
                return false;
            }

            // Keep exclusive ownership until the rebuilt target is ready. Another
            // plugin or controller may have restarted Timeline while the character
            // was loading, so explicitly pause again before touching its tracks.
            bool isPlaying;
            if (!TryGetIsPlaying(out isPlaying))
            {
                status = "无法读取 Timeline 恢复状态";
                return false;
            }
            if (isPlaying)
            {
                try
                {
                    _pauseMethod.Invoke(null, null);
                }
                catch (Exception exception)
                {
                    status = "Timeline 恢复前无法重新暂停";
                    VRGIN.Core.VRLog.Warn("Timeline re-pause after scene mutation failed: {0}", exception);
                    return false;
                }
            }

            if (sceneChanged && !targetReady)
            {
                status = "新角色尚未稳定，Timeline 已保持暂停；请重新载入角色或场景后再播放";
                return false;
            }

            bool refreshed = true;
            bool restoredBySeek = false;
            string refreshStatus = null;
            float currentTime;
            if (!TryGetPlaybackTime(out currentTime))
            {
                status = "无法读取 Timeline 当前播放位置";
                return false;
            }

            if (Mathf.Abs(currentTime - session.PlaybackTime) > 0.0001f)
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
                if (_seekMethod == null)
                    _seekMethod = _timelineType.GetMethod(
                        "Seek",
                        flags,
                        null,
                        new[] { typeof(float) },
                        null);
                if (_seekMethod == null)
                {
                    status = "Timeline 原播放位置恢复接口不可用";
                    return false;
                }

                try
                {
                    // Timeline.Seek already performs its before/after interpolation
                    // passes. Do not call RefreshCurrentFrame afterwards or discrete
                    // event tracks would be evaluated twice.
                    _seekMethod.Invoke(null, new object[] { session.PlaybackTime });
                    restoredBySeek = true;
                }
                catch (Exception exception)
                {
                    status = "Timeline 原播放位置恢复失败";
                    VRGIN.Core.VRLog.Warn("Timeline time restore after scene mutation failed: {0}", exception);
                    return false;
                }
            }

            if (sceneChanged && !restoredBySeek)
                refreshed = RefreshCurrentFrame(out refreshStatus);
            if (!refreshed)
            {
                status = refreshStatus ?? "Timeline 当前帧重新应用失败";
                return false;
            }

            if (session.WasPlaying)
            {
                try
                {
                    _playMethod.Invoke(null, null);
                }
                catch (Exception exception)
                {
                    status = "Timeline 原播放状态恢复失败";
                    VRGIN.Core.VRLog.Warn("Timeline resume-after-scene-mutation failed: {0}", exception);
                    return false;
                }
            }

            status = sceneChanged
                ? (session.WasPlaying
                    ? "Timeline 已重新应用当前帧并继续播放"
                    : "Timeline 已重新应用当前帧")
                : (session.WasPlaying ? "Timeline 已继续播放" : null);
            return true;
        }
        finally
        {
            CompleteMutationSession(session);
        }
    }

    private static void CompleteMutationSession(VRTimelineMutationSession session)
    {
        if (session == null || session.Completed)
            return;

        session.Completed = true;
        if (!session.OwnsMutationLock)
            return;

        session.OwnsMutationLock = false;
        if (_sceneMutationDepth > 0)
            _sceneMutationDepth--;
    }

    public static bool TryGetCameraFov(out float fieldOfView)
    {
        fieldOfView = DefaultCameraFov;
        try
        {
            Studio.Studio studio = Studio.Studio.Instance;
            if (studio == null || studio.cameraCtrl == null)
                return false;

            float current = studio.cameraCtrl.fieldOfView;
            if (float.IsNaN(current) || float.IsInfinity(current))
                return false;

            fieldOfView = Mathf.Clamp(current, MinCameraFov, MaxCameraFov);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool SetCameraFov(float fieldOfView, out string status)
    {
        fieldOfView = Mathf.Clamp(fieldOfView, MinCameraFov, MaxCameraFov);
        if (!ApplyCameraFov(fieldOfView))
        {
            status = "Timeline 镜头尚未就绪";
            return false;
        }

        status = "Timeline FOV 已设为 " + fieldOfView.ToString("F1") + "°";
        return true;
    }

    internal static bool ApplyCameraFov(float fieldOfView)
    {
        try
        {
            Studio.Studio studio = Studio.Studio.Instance;
            if (studio == null || studio.cameraCtrl == null)
                return false;

            studio.cameraCtrl.fieldOfView = Mathf.Clamp(
                fieldOfView,
                MinCameraFov,
                MaxCameraFov);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool RefreshCurrentFrame(out string status)
    {
        if (!ResolvePlaybackState())
        {
            status = "未检测到 Timeline 1.5.x";
            return false;
        }

        const BindingFlags staticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        const BindingFlags instanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        if (_selfField == null)
            _selfField = _timelineType.GetField("_self", staticFlags);
        if (_interpolateMethod == null)
            _interpolateMethod = _timelineType.GetMethod(
                "Interpolate",
                instanceFlags,
                null,
                new[] { typeof(bool) },
                null);
        if (_selfField == null || _interpolateMethod == null)
        {
            status = "Timeline 当前帧刷新接口不可用";
            return false;
        }

        try
        {
            object instance = _selfField.GetValue(null);
            if (instance == null)
            {
                status = "Timeline 尚未初始化";
                return false;
            }

            // Timeline.Seek ignores an unchanged playback time. Invoke the same two
            // interpolation passes used by its own Stop/seek path so rebuilt
            // character components receive the current frame without moving time.
            _interpolateMethod.Invoke(instance, new object[] { true });
            _interpolateMethod.Invoke(instance, new object[] { false });
            status = "Timeline 已重新应用当前帧";
            return true;
        }
        catch (Exception exception)
        {
            status = "Timeline 当前帧刷新失败";
            VRGIN.Core.VRLog.Warn("Timeline current-frame refresh failed: {0}", exception);
            Clear();
            return false;
        }
    }

    private static bool TryGetPlaybackTime(out float playbackTime)
    {
        playbackTime = 0f;
        if (!ResolvePlaybackState())
            return false;

        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        if (_playbackTimeProperty == null)
            _playbackTimeProperty = _timelineType.GetProperty("playbackTime", flags);
        if (_playbackTimeProperty == null)
            return false;

        try
        {
            playbackTime = (float)_playbackTimeProperty.GetValue(null, null);
            return !float.IsNaN(playbackTime) && !float.IsInfinity(playbackTime);
        }
        catch
        {
            return false;
        }
    }

    private static bool ResolvePlaybackState()
    {
        if (_isPlayingProperty != null)
            return true;

        if (_timelineType == null)
        {
            _timelineType = Type.GetType("Timeline.Timeline, Timeline", false);
            if (_timelineType == null)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (!string.Equals(assembly.GetName().Name, "Timeline", StringComparison.OrdinalIgnoreCase))
                        continue;
                    _timelineType = assembly.GetType("Timeline.Timeline", false);
                    if (_timelineType != null)
                        break;
                }
            }
        }

        if (_timelineType == null)
            return false;

        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        _isPlayingProperty = _timelineType.GetProperty("isPlaying", flags);
        return _isPlayingProperty != null;
    }

    private static bool ResolveControls()
    {
        if (!ResolvePlaybackState())
            return false;
        if (_playMethod != null && _pauseMethod != null)
            return true;

        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        _playMethod = _timelineType.GetMethod("Play", flags, null, Type.EmptyTypes, null);
        _pauseMethod = _timelineType.GetMethod("Pause", flags, null, Type.EmptyTypes, null);
        return _playMethod != null && _pauseMethod != null;
    }

    private static void Clear()
    {
        _timelineType = null;
        _isPlayingProperty = null;
        _playbackTimeProperty = null;
        _playMethod = null;
        _pauseMethod = null;
        _seekMethod = null;
        _selfField = null;
        _interpolateMethod = null;
    }
}

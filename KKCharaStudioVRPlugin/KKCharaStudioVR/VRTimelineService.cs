using System;
using System.Reflection;

namespace KKCharaStudioVR;

internal static class VRTimelineService
{
    private static PropertyInfo _isPlayingProperty;
    private static MethodInfo _playMethod;
    private static MethodInfo _pauseMethod;

    public static bool TryGetIsPlaying(out bool isPlaying)
    {
        isPlaying = false;
        if (!Resolve())
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

    private static bool Resolve()
    {
        if (_isPlayingProperty != null && _playMethod != null && _pauseMethod != null)
            return true;

        Type timelineType = Type.GetType("Timeline.Timeline, Timeline", false);
        if (timelineType == null)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!string.Equals(assembly.GetName().Name, "Timeline", StringComparison.OrdinalIgnoreCase))
                    continue;
                timelineType = assembly.GetType("Timeline.Timeline", false);
                if (timelineType != null)
                    break;
            }
        }

        if (timelineType == null)
            return false;

        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
        _isPlayingProperty = timelineType.GetProperty("isPlaying", flags);
        _playMethod = timelineType.GetMethod("Play", flags, null, Type.EmptyTypes, null);
        _pauseMethod = timelineType.GetMethod("Pause", flags, null, Type.EmptyTypes, null);
        return _isPlayingProperty != null && _playMethod != null && _pauseMethod != null;
    }

    private static void Clear()
    {
        _isPlayingProperty = null;
        _playMethod = null;
        _pauseMethod = null;
    }
}

namespace KKCharaStudioVR;

// IronPython calls this public bridge synchronously so the wrist UI can read
// MMDD runtime state without taking a compile-time dependency on VNGE.
public static class VRMmddStateBridge
{
    public static bool FixedFovReported { get; private set; }
    public static bool FixedFovEnabled { get; private set; }
    public static float FixedFovValue { get; private set; } = 53.13f;
    public static int CameraControllerCount { get; private set; }

    public static bool HighHeelsReported { get; private set; }
    public static string HighHeelsTargetName { get; private set; }
    public static string HighHeelsPluginName { get; private set; }
    public static bool HighHeelsAutoMode { get; private set; }
    public static bool HighHeelsShoesDetect { get; private set; }
    public static float HighHeelsAnkle { get; private set; }
    public static float HighHeelsHeel { get; private set; }
    public static float HighHeelsToes { get; private set; }
    public static bool ShoesOffsetEnabled { get; private set; }
    public static float ShoesOnOffset { get; private set; }
    public static float ShoesOffOffset { get; private set; }

    public static void ReportFixedFov(bool enabled, float value, int controllerCount)
    {
        FixedFovReported = true;
        FixedFovEnabled = enabled;
        FixedFovValue = value;
        CameraControllerCount = controllerCount;
    }

    public static void ReportHighHeels(
        string targetName,
        string pluginName,
        bool autoMode,
        bool shoesDetect,
        float ankle,
        float heel,
        float toes,
        bool shoesOffsetEnabled,
        float shoesOnOffset,
        float shoesOffOffset)
    {
        HighHeelsReported = true;
        HighHeelsTargetName = targetName;
        HighHeelsPluginName = pluginName;
        HighHeelsAutoMode = autoMode;
        HighHeelsShoesDetect = shoesDetect;
        HighHeelsAnkle = ankle;
        HighHeelsHeel = heel;
        HighHeelsToes = toes;
        ShoesOffsetEnabled = shoesOffsetEnabled;
        ShoesOnOffset = shoesOnOffset;
        ShoesOffOffset = shoesOffOffset;
    }

    internal static void ResetFixedFov()
    {
        FixedFovReported = false;
        CameraControllerCount = 0;
    }

    internal static void ResetHighHeels()
    {
        HighHeelsReported = false;
        HighHeelsTargetName = null;
        HighHeelsPluginName = null;
    }
}

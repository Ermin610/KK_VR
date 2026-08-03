namespace KKCharaStudioVR;

// IronPython calls this public bridge synchronously so the wrist UI can read
// MMDD runtime state without taking a compile-time dependency on VNGE.
public static class VRMmddStateBridge
{
    public static bool PlaybackReported { get; private set; }
    public static bool PlaybackAvailable { get; private set; }
    public static bool PlaybackIsPlaying { get; private set; }
    public static float PlaybackCurrentFrame { get; private set; }
    public static float PlaybackStartFrame { get; private set; }
    public static float PlaybackEndFrame { get; private set; }
    public static int PlaybackGeneration { get; private set; }
    public static bool DirectVrCameraOwner { get; private set; }
    public static int PlaybackReportSequence { get; private set; }
    public static float PlaybackReportRealtime { get; private set; }

    public static bool FixedFovReported { get; private set; }
    public static bool FixedFovEnabled { get; private set; }
    public static float FixedFovValue { get; private set; } = 53.13f;
    public static int CameraControllerCount { get; private set; }

    public static bool HighHeelsReported { get; private set; }
    public static int HighHeelsObjectKey { get; private set; } = -1;
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

    public static bool CharacterVmdReported { get; private set; }
    public static bool CharacterVmdHadMotion { get; private set; }
    public static bool CharacterVmdHadMorph { get; private set; }
    public static string CharacterVmdMotionFile { get; private set; }
    public static string CharacterVmdMorphFile { get; private set; }
    public static bool CharacterVmdWasPlaying { get; private set; }
    public static float CharacterVmdFrame { get; private set; }

    public static bool MmdClearReported { get; private set; }
    public static bool MmdClearSucceeded { get; private set; }
    public static int MmdClearObjectKey { get; private set; } = -1;
    public static string MmdClearTargetName { get; private set; }
    public static int MmdClearMotionCount { get; private set; }
    public static int MmdClearMorphCount { get; private set; }
    public static int MmdClearCameraCount { get; private set; }
    public static bool MmdClearRolledBack { get; private set; }
    public static bool MmdClearRollbackFailed { get; private set; }
    public static string MmdClearError { get; private set; }

    public static void ReportFixedFov(bool enabled, float value, int controllerCount)
    {
        FixedFovReported = true;
        FixedFovEnabled = enabled;
        FixedFovValue = value;
        CameraControllerCount = controllerCount;
    }

    public static void ReportPlayback(
        bool available,
        bool isPlaying,
        float currentFrame,
        float startFrame,
        float endFrame,
        int generation,
        bool directVrCameraOwner)
    {
        if (float.IsNaN(currentFrame) || float.IsInfinity(currentFrame)
            || float.IsNaN(startFrame) || float.IsInfinity(startFrame)
            || float.IsNaN(endFrame) || float.IsInfinity(endFrame)
            || endFrame < startFrame)
        {
            available = false;
            isPlaying = false;
            currentFrame = 0f;
            startFrame = 0f;
            endFrame = 0f;
            directVrCameraOwner = false;
        }
        PlaybackReported = true;
        PlaybackAvailable = available;
        PlaybackIsPlaying = isPlaying;
        PlaybackCurrentFrame = currentFrame;
        PlaybackStartFrame = startFrame;
        PlaybackEndFrame = endFrame;
        PlaybackGeneration = generation;
        DirectVrCameraOwner = available && directVrCameraOwner;
        PlaybackReportSequence++;
        PlaybackReportRealtime = UnityEngine.Time.realtimeSinceStartup;
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

    public static void ReportHighHeels(
        int objectKey,
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
        HighHeelsObjectKey = objectKey;
        ReportHighHeels(
            targetName,
            pluginName,
            autoMode,
            shoesDetect,
            ankle,
            heel,
            toes,
            shoesOffsetEnabled,
            shoesOnOffset,
            shoesOffOffset);
    }

    public static void ReportCharacterVmd(
        bool hadMotion,
        bool hadMorph,
        string motionFile,
        string morphFile,
        bool wasPlaying,
        float frame)
    {
        CharacterVmdReported = true;
        CharacterVmdHadMotion = hadMotion;
        CharacterVmdHadMorph = hadMorph;
        CharacterVmdMotionFile = motionFile;
        CharacterVmdMorphFile = morphFile;
        CharacterVmdWasPlaying = wasPlaying;
        CharacterVmdFrame = frame;
    }

    public static void ReportMmdClear(
        bool succeeded,
        int objectKey,
        string targetName,
        int motionCount,
        int morphCount,
        int cameraCount,
        bool rolledBack,
        bool rollbackFailed,
        string error)
    {
        MmdClearReported = true;
        MmdClearSucceeded = succeeded;
        MmdClearObjectKey = objectKey;
        MmdClearTargetName = targetName;
        MmdClearMotionCount = motionCount < 0 ? 0 : motionCount;
        MmdClearMorphCount = morphCount < 0 ? 0 : morphCount;
        MmdClearCameraCount = cameraCount < 0 ? 0 : cameraCount;
        MmdClearRolledBack = !succeeded && rolledBack;
        MmdClearRollbackFailed = !succeeded && rollbackFailed;
        MmdClearError = error;
    }

    internal static void ResetFixedFov()
    {
        FixedFovReported = false;
        CameraControllerCount = 0;
    }

    internal static void ResetHighHeels()
    {
        HighHeelsReported = false;
        HighHeelsObjectKey = -1;
        HighHeelsTargetName = null;
        HighHeelsPluginName = null;
    }

    internal static void ResetPlayback()
    {
        PlaybackReported = false;
        PlaybackAvailable = false;
        PlaybackIsPlaying = false;
        PlaybackCurrentFrame = 0f;
        PlaybackStartFrame = 0f;
        PlaybackEndFrame = 0f;
        DirectVrCameraOwner = false;
        PlaybackReportRealtime = 0f;
    }

    internal static void ResetCharacterVmd()
    {
        CharacterVmdReported = false;
        CharacterVmdHadMotion = false;
        CharacterVmdHadMorph = false;
        CharacterVmdMotionFile = null;
        CharacterVmdMorphFile = null;
        CharacterVmdWasPlaying = false;
        CharacterVmdFrame = 0f;
    }

    internal static void ResetMmdClear()
    {
        MmdClearReported = false;
        MmdClearSucceeded = false;
        MmdClearObjectKey = -1;
        MmdClearTargetName = null;
        MmdClearMotionCount = 0;
        MmdClearMorphCount = 0;
        MmdClearCameraCount = 0;
        MmdClearRolledBack = false;
        MmdClearRollbackFailed = false;
        MmdClearError = null;
    }
}

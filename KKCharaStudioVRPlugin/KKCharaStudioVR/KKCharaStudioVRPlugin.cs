using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using VRGIN.Core;

namespace KKCharaStudioVR;

[BepInProcess("CharaStudio")]
[BepInPlugin("KKCharaStudioVRPlugin.KKCharaStudioVRPlugin", "KKCharaStudioVRPlugin", "0.4.0")]
public class KKCharaStudioVRPlugin : BaseUnityPlugin
{
    public const string NAME = "KKCharaStudioVRPlugin";
    public const string VERSION = "0.4.0";

    private const string ReShadeSection = "ReShade（桌面控制）";
    private const string TestNone = "无";
    private const string TestToggle = "切换开关";
    private const string TestNext = "下一个预设";
    private const string TestPrevious = "上一个预设";
    private const string TestFull = "完整自检";

    private static KKCharaStudioVRPlugin _instance;

    private readonly bool _noVrRequested;
    private readonly bool _engineNoVrRequested;
    private readonly bool _managedVrEnabled;

    private ConfigEntry<bool> _desktopReShadeEnabled;
    private ConfigEntry<string> _desktopReShadePreset;
    private ConfigEntry<KeyboardShortcut> _desktopReShadeToggleShortcut;
    private ConfigEntry<KeyboardShortcut> _desktopReShadeNextShortcut;
    private ConfigEntry<string> _desktopReShadeTestCommand;
    private bool _suppressReShadeConfigEvents;
    private bool _selfTestRunning;
    private bool _waitPassed;
    private KKVRReShadeState _waitObservedState;
    private bool _clothingOpacitySelfTestEnabled;
    private bool _clothingOpacityCalibrationEnabled;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public KKCharaStudioVRPlugin()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        bool customNoVr = HasExactArgument(arguments, "--novr")
            || HasExactArgument(arguments, "--no-vr");
        _engineNoVrRequested = HasVrModeNone(arguments);
        _noVrRequested = customNoVr || _engineNoVrRequested;

        // The release package enables VR support in globalgamemanagers so
        // ReShade can hook OpenVR before D3D11 is created. That native startup
        // happens before BepInEx. Therefore --novr is the managed safety gate,
        // while Unity's earlier `-vrmode None` is what prevents openvr_api.dll
        // from entering the process at all. No-VR always wins over every VR
        // request, including a conflicting --studiovr argument.
        if (_noVrRequested)
        {
            if (!_engineNoVrRequested)
            {
                // Compatibility fallback for direct `--novr` launches. This can
                // switch Unity back to None, but it is too late to undo a native
                // OpenVR load that already happened during engine bootstrap.
                VRLoader.Create(isEnable: false);
            }
            return;
        }

        bool explicitVr = HasExactArgument(arguments, "--studiovr")
            || HasExactArgument(arguments, "--vr");
        // VR is now explicit opt-in. Merely leaving SteamVR running must not
        // pull a normal Studio session back into VR.
        if (explicitVr)
        {
            // Do not pre-load openvr_api.dll here. An extra VR_Init/VR_Shutdown
            // cycle breaks ReShade's IVRCompositor::Submit hooks.
            VRLoader.Create(isEnable: true);
            SaveLoadSceneHook.InstallHook();
            LoadFixHook.InstallHook();
            DropdownFixHook.InstallHook();
            MirrorFixHook.InstallHook();
            _managedVrEnabled = true;
        }
        else
        {
            VRLoader.Create(isEnable: false);
        }
    }

    public void Start()
    {
        _instance = this;
        InitializeDesktopReShadeControls();

        _clothingOpacitySelfTestEnabled = Environment.CommandLine.IndexOf(
            "--kkvr-opacity-self-test", StringComparison.OrdinalIgnoreCase) >= 0;
        _clothingOpacityCalibrationEnabled = Environment.CommandLine.IndexOf(
            "--kkvr-opacity-calibration", StringComparison.OrdinalIgnoreCase) >= 0;
        if (_clothingOpacitySelfTestEnabled)
        {
            Logger.LogInfo(
                "[KKVR opacity self-test] Automatic desktop test enabled; "
                + "F10=50%, F11=0%, F12=restore remain available for diagnostics.");
        }
        if (_clothingOpacityCalibrationEnabled)
        {
            Logger.LogInfo(
                "[KKVR opacity calibration] Automatic desktop calibration enabled; "
                + "normal VR/UI behavior is not modified.");
        }

        if (_noVrRequested)
        {
            if (_engineNoVrRequested)
            {
                Logger.LogInfo(
                    "No-VR mode active: Unity started with -vrmode None; " +
                    "VRLoader and VR-only hooks were not installed.");
            }
            else
            {
                Logger.LogWarning(
                    "--novr disabled managed VR, but this package enables OpenVR " +
                    "before BepInEx. Use StartCharaStudioNoVR.bat (or add " +
                    "-vrmode None) to prevent openvr_api.dll from loading.");
            }
        }
        else if (_managedVrEnabled)
        {
            Logger.LogInfo("Managed CharaStudio VR features are enabled.");
        }

        // Force the window to the foreground so VR UI interaction works without alt-tabbing.
        // The opt-in desktop material self-test stays in the background so it does not
        // steal the user's mouse or keyboard while its screenshots are produced.
        if (!_clothingOpacitySelfTestEnabled && !_clothingOpacityCalibrationEnabled)
        {
            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                if (process != null && process.MainWindowHandle != IntPtr.Zero)
                {
                    ShowWindow(process.MainWindowHandle, 5); // SW_SHOW
                    SetForegroundWindow(process.MainWindowHandle);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[KKCharaStudioVRPlugin] Failed to set foreground window: " + e.Message);
            }
        }

        bool commandLineSelfTest = Environment.CommandLine.IndexOf(
            "--kkvr-reshade-self-test", StringComparison.OrdinalIgnoreCase) >= 0;
        bool configuredSelfTest = _desktopReShadeTestCommand != null
            && string.Equals(_desktopReShadeTestCommand.Value, TestFull, StringComparison.Ordinal);
        if (configuredSelfTest)
            ResetDesktopTestCommand();

        if (commandLineSelfTest || configuredSelfTest)
            StartCoroutine(RunReShadeSelfTest());
        else if (!_clothingOpacitySelfTestEnabled && !_clothingOpacityCalibrationEnabled)
            StartCoroutine(ApplySavedDesktopReShadeSettings());

        if (_clothingOpacityCalibrationEnabled)
            StartCoroutine(RunAutomaticClothingOpacityCalibration());
        else if (_clothingOpacitySelfTestEnabled)
            StartCoroutine(RunAutomaticClothingOpacitySelfTest());
    }

    public void Update()
    {
        if (_clothingOpacitySelfTestEnabled)
        {
            if (Input.GetKeyDown(KeyCode.F10))
                RunClothingOpacitySelfTest(0.5f, false);
            if (Input.GetKeyDown(KeyCode.F11))
                RunClothingOpacitySelfTest(0f, false);
            if (Input.GetKeyDown(KeyCode.F12))
                RunClothingOpacitySelfTest(1f, true);
        }

        if (_desktopReShadeToggleShortcut != null
            && _desktopReShadeToggleShortcut.Value.IsDown())
        {
            _desktopReShadeEnabled.Value = !_desktopReShadeEnabled.Value;
        }

        if (_desktopReShadeNextShortcut != null
            && _desktopReShadeNextShortcut.Value.IsDown())
        {
            SelectAdjacentDesktopPreset(1);
        }
    }

    private void RunClothingOpacitySelfTest(float opacity, bool reset)
    {
        RunClothingOpacitySelfTest(opacity, reset, null);
    }

    private void RunClothingOpacitySelfTest(
        float opacity,
        bool reset,
        string expectedCardFileName)
    {
        Studio.OCIChar character;
        int partId;
        if (!TryGetClothingOpacitySelfTestTarget(
                expectedCardFileName, out character, out partId))
        {
            Logger.LogError("[KKVR opacity self-test] No loaded character with renderable clothing was found.");
            return;
        }

        string occupied = string.Empty;
        for (int index = 0; index < VRCharacterClothingService.PartCount; index++)
        {
            if (!VRClothingOpacityService.HasPart(character.charInfo, index))
                continue;
            if (occupied.Length > 0)
                occupied += ",";
            occupied += index + ":" + VRCharacterClothingService.GetPartName(index);
        }

        VRClothingOpacityInfo info;
        string status;
        bool success = reset
            ? VRClothingOpacityService.TryResetPart(
                character.charInfo, partId, out info, out status)
            : VRClothingOpacityService.TrySetPartOpacity(
                character.charInfo, partId, opacity, true, out info, out status);
        Logger.Log(
            success ? BepInEx.Logging.LogLevel.Info : BepInEx.Logging.LogLevel.Error,
            "[KKVR opacity self-test] " + (reset ? "RESTORE" : Mathf.RoundToInt(opacity * 100f) + "%")
            + "; character=" + (character.treeNodeObject == null
                ? character.charInfo.name
                : character.treeNodeObject.textName)
            + "; occupied parts=" + occupied
            + "; result=" + status
            + "; materials=" + info.MaterialCount
            + "; supported=" + info.SupportedCount
            + "; compatible=" + info.CompatibleCount
            + "; protected=" + info.ProtectedCount
            + "; unsupported=" + info.UnsupportedCount);
    }

    private static bool TryGetClothingOpacitySelfTestTarget(
        out Studio.OCIChar character,
        out int partId)
    {
        return TryGetClothingOpacitySelfTestTarget(
            null, out character, out partId);
    }

    private static bool TryGetClothingOpacitySelfTestTarget(
        string expectedCardFileName,
        out Studio.OCIChar character,
        out int partId)
    {
        return TryGetClothingOpacitySelfTestTarget(
            expectedCardFileName, -1, out character, out partId);
    }

    private static bool TryGetClothingOpacitySelfTestTarget(
        string expectedCardFileName,
        int requestedPartId,
        out Studio.OCIChar character,
        out int partId)
    {
        character = null;
        partId = -1;
        Studio.Studio studio = Studio.Studio.Instance;
        if (studio == null || studio.dicObjectCtrl == null)
            return false;

        foreach (var pair in studio.dicObjectCtrl)
        {
            Studio.OCIChar candidate = pair.Value as Studio.OCIChar;
            if (candidate == null || candidate.charInfo == null || !candidate.charInfo.loadEnd)
                continue;
            if (!string.IsNullOrEmpty(expectedCardFileName))
            {
                string liveCardFileName = candidate.charInfo.chaFile == null
                    ? string.Empty
                    : candidate.charInfo.chaFile.charaFileName;
                if (!string.Equals(
                        liveCardFileName,
                        expectedCardFileName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }
            if (requestedPartId >= 0)
            {
                if (requestedPartId < VRCharacterClothingService.PartCount
                    && VRClothingOpacityService.HasPart(
                        candidate.charInfo, requestedPartId))
                {
                    character = candidate;
                    partId = requestedPartId;
                    return true;
                }
                continue;
            }
            for (int index = 0; index < VRCharacterClothingService.PartCount; index++)
            {
                if (!VRClothingOpacityService.HasPart(candidate.charInfo, index))
                    continue;
                character = candidate;
                partId = index;
                return true;
            }
        }
        return false;
    }

    private IEnumerator RunAutomaticClothingOpacitySelfTest()
    {
        yield return new WaitForSecondsRealtime(3f);

        string reshadeMessage;
        bool reshadeRequestAccepted = KKVRReShadeControl.TrySetEnabled(
            false, out reshadeMessage);
        Logger.LogInfo("[KKVR opacity self-test] ReShade disable request accepted="
            + reshadeRequestAccepted + "; " + reshadeMessage);

        string cardPath = Path.Combine(
            Path.Combine(
                Path.Combine(
                    Path.Combine(Paths.GameRootPath, "UserData"),
                    "chara"),
                "female"),
            Path.Combine("Ermin", "胡桃.png"));
        if (!File.Exists(cardPath))
        {
            Logger.LogError("[KKVR opacity self-test] Test card was not found: " + cardPath);
            yield return new WaitForSecondsRealtime(1f);
            Application.Quit();
            yield break;
        }

        bool cardQueued = false;
        try
        {
            Studio.Studio.Instance.AddFemale(cardPath);
            cardQueued = true;
            Logger.LogInfo("[KKVR opacity self-test] Loading Ermin test card: " + cardPath);
        }
        catch (Exception ex)
        {
            Logger.LogError("[KKVR opacity self-test] Unable to load test card: " + ex);
        }
        if (!cardQueued)
        {
            Application.Quit();
            yield break;
        }

        float deadline = Time.realtimeSinceStartup + 90f;
        string expectedCardFileName = Path.GetFileName(cardPath);
        Studio.OCIChar character;
        int partId;
        while (!TryGetClothingOpacitySelfTestTarget(
                   expectedCardFileName, out character, out partId)
               && Time.realtimeSinceStartup < deadline)
        {
            yield return new WaitForSecondsRealtime(0.5f);
        }
        if (!TryGetClothingOpacitySelfTestTarget(
                expectedCardFileName, out character, out partId))
        {
            Logger.LogError("[KKVR opacity self-test] Test card did not finish loading within 90 seconds.");
            Application.Quit();
            yield break;
        }

        bool reshadeDisabled = false;
        float reshadeDeadline = Time.realtimeSinceStartup + 12f;
        while (!reshadeDisabled && Time.realtimeSinceStartup < reshadeDeadline)
        {
            KKVRReShadeState reshadeState;
            string stateMessage;
            KKVRReShadeControl.TryGetState(out reshadeState, out stateMessage);
            reshadeDisabled = reshadeState != null
                && reshadeState.EffectsEnabled.HasValue
                && !reshadeState.EffectsEnabled.Value
                && !reshadeState.RequestPending;
            reshadeMessage = stateMessage;
            if (!reshadeDisabled)
            {
                KKVRReShadeControl.TrySetEnabled(false, out reshadeMessage);
                yield return new WaitForSecondsRealtime(0.5f);
            }
        }
        Logger.Log(
            reshadeDisabled ? BepInEx.Logging.LogLevel.Info : BepInEx.Logging.LogLevel.Warning,
            "[KKVR opacity self-test] ReShade verified disabled="
            + reshadeDisabled + "; " + reshadeMessage);

        yield return new WaitForSecondsRealtime(8f);
        string captureDirectory = Path.Combine(
            Path.Combine(
                Path.Combine(Paths.GameRootPath, "UserData"),
                "KKVRTests"),
            "Opacity_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(captureDirectory);

        // The first capture warms up Studio's deferred character renderer and is
        // intentionally excluded from the actual before/after comparison.
        yield return StartCoroutine(CaptureClothingOpacitySelfTestFrame(
            Path.Combine(captureDirectory, "00_warmup.png")));
        yield return new WaitForSecondsRealtime(2f);
        yield return StartCoroutine(CaptureClothingOpacitySelfTestFrame(
            Path.Combine(captureDirectory, "01_baseline_100.png")));
        RunClothingOpacitySelfTest(0.5f, false, expectedCardFileName);
        yield return new WaitForSecondsRealtime(2f);
        yield return StartCoroutine(CaptureClothingOpacitySelfTestFrame(
            Path.Combine(captureDirectory, "02_opacity_50.png")));
        RunClothingOpacitySelfTest(0f, false, expectedCardFileName);
        yield return new WaitForSecondsRealtime(2f);
        yield return StartCoroutine(CaptureClothingOpacitySelfTestFrame(
            Path.Combine(captureDirectory, "03_opacity_0.png")));
        RunClothingOpacitySelfTest(1f, true, expectedCardFileName);
        yield return new WaitForSecondsRealtime(2f);
        yield return StartCoroutine(CaptureClothingOpacitySelfTestFrame(
            Path.Combine(captureDirectory, "04_restored_100.png")));

        Logger.LogInfo("[KKVR opacity self-test] COMPLETE; screenshots=" + captureDirectory);
        yield return new WaitForSecondsRealtime(2f);
        Application.Quit();
    }

    private IEnumerator RunAutomaticClothingOpacityCalibration()
    {
        yield return new WaitForSecondsRealtime(3f);

        string cardPath = ResolveOpacityCalibrationCardPath();
        if (string.IsNullOrEmpty(cardPath) || !File.Exists(cardPath))
        {
            Logger.LogError("[KKVR opacity calibration] Card was not found: " + cardPath);
            Application.Quit();
            yield break;
        }

        try
        {
            Studio.Studio.Instance.AddFemale(cardPath);
            Logger.LogInfo("[KKVR opacity calibration] Loading card: " + cardPath);
        }
        catch (Exception ex)
        {
            Logger.LogError("[KKVR opacity calibration] Unable to load card: " + ex);
            Application.Quit();
            yield break;
        }

        string expectedCardFileName = Path.GetFileName(cardPath);
        Studio.OCIChar character;
        int detectedPartId;
        float deadline = Time.realtimeSinceStartup + 180f;
        while (!TryGetClothingOpacitySelfTestTarget(
                   expectedCardFileName, out character, out detectedPartId)
               && Time.realtimeSinceStartup < deadline)
        {
            yield return new WaitForSecondsRealtime(0.5f);
        }
        if (!TryGetClothingOpacitySelfTestTarget(
                expectedCardFileName, out character, out detectedPartId))
        {
            Logger.LogError(
                "[KKVR opacity calibration] Card did not finish loading within 180 seconds.");
            Application.Quit();
            yield break;
        }

        KKVRReShadeState reshadeState;
        string reshadeStatus;
        KKVRReShadeControl.TryGetState(out reshadeState, out reshadeStatus);
        Logger.LogInfo("[KKVR opacity calibration] ReShade state: " + reshadeStatus);

        yield return new WaitForSecondsRealtime(6f);
        if (HasExactArgument(
                Environment.GetCommandLineArgs(),
                "--kkvr-opacity-disable-reshade"))
        {
            bool disabled = false;
            float disableDeadline = Time.realtimeSinceStartup + 15f;
            while (!disabled && Time.realtimeSinceStartup < disableDeadline)
            {
                KKVRReShadeControl.TrySetEnabled(false, out reshadeStatus);
                yield return new WaitForSecondsRealtime(0.5f);
                KKVRReShadeControl.TryGetState(out reshadeState, out reshadeStatus);
                disabled = reshadeState != null
                    && reshadeState.EffectsEnabled.HasValue
                    && !reshadeState.EffectsEnabled.Value
                    && !reshadeState.RequestPending;
            }
            Logger.Log(
                disabled
                    ? BepInEx.Logging.LogLevel.Info
                    : BepInEx.Logging.LogLevel.Warning,
                "[KKVR opacity calibration] ReShade disabled=" + disabled
                + "; " + reshadeStatus);
        }

        float[] transparencyLevels = ParseOpacityCalibrationLevels();
        int[] requestedParts = ParseOpacityCalibrationParts();
        bool isolatePart = HasExactArgument(
            Environment.GetCommandLineArgs(),
            "--kkvr-opacity-isolate");
        int characterObjectKey;
        VRClothingStateSnapshot clothingBaseline = null;
        string baselineStatus;
        if (isolatePart
            && TryGetOpacityCalibrationObjectKey(character, out characterObjectKey))
        {
            VRCharacterClothingService.TryCaptureStates(
                characterObjectKey, out clothingBaseline, out baselineStatus);
        }
        else
        {
            characterObjectKey = -1;
        }
        string captureDirectory = Path.Combine(
            Path.Combine(
                Path.Combine(Paths.GameRootPath, "UserData"),
                "KKVRTests"),
            "OpacityCalibration_"
            + SanitizeCalibrationName(Path.GetFileNameWithoutExtension(cardPath))
            + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(captureDirectory);

        Logger.LogInfo(
            "[KKVR opacity calibration] START; screenshots=" + captureDirectory);
        foreach (int requestedPartId in requestedParts)
        {
            if (clothingBaseline != null)
                VRCharacterClothingService.TryRestoreStates(
                    clothingBaseline, out baselineStatus);
            int partId;
            if (!TryGetClothingOpacitySelfTestTarget(
                    expectedCardFileName,
                    requestedPartId,
                    out character,
                    out partId))
            {
                Logger.LogWarning(
                    "[KKVR opacity calibration] Skipping empty part "
                    + requestedPartId + ":"
                    + VRCharacterClothingService.GetPartName(requestedPartId));
                continue;
            }

            if (isolatePart && characterObjectKey >= 0)
            {
                PrepareOpacityCalibrationIsolation(
                    characterObjectKey, character.charInfo, partId);
                yield return new WaitForSecondsRealtime(0.75f);
            }

            string partDirectory = Path.Combine(
                captureDirectory,
                "part_" + partId + "_"
                + SanitizeCalibrationName(VRCharacterClothingService.GetPartName(partId)));
            Directory.CreateDirectory(partDirectory);

            VRClothingOpacityInfo resetInfo;
            string resetStatus;
            VRClothingOpacityService.TryResetPart(
                character.charInfo, partId, out resetInfo, out resetStatus);
            ResetOpacityCalibrationIsolation(character.charInfo, partId);
            if (clothingBaseline != null)
                VRCharacterClothingService.TryRestoreStates(
                    clothingBaseline, out baselineStatus);
            yield return new WaitForSecondsRealtime(0.75f);

            foreach (float transparency in transparencyLevels)
            {
                float clampedTransparency = Mathf.Clamp(transparency, 0f, 100f);
                float opacity = 1f - clampedTransparency / 100f;
                VRClothingOpacityInfo info;
                string status;
                bool success = VRClothingOpacityService.TrySetPartOpacity(
                    character.charInfo,
                    partId,
                    opacity,
                    true,
                    out info,
                    out status);
                Logger.Log(
                    success
                        ? BepInEx.Logging.LogLevel.Info
                        : BepInEx.Logging.LogLevel.Error,
                    "[KKVR opacity calibration] part=" + partId + ":"
                    + VRCharacterClothingService.GetPartName(partId)
                    + "; transparency="
                    + clampedTransparency.ToString("0.##", CultureInfo.InvariantCulture)
                    + "%"
                    + "; internalOpacity="
                    + opacity.ToString("0.###", CultureInfo.InvariantCulture)
                    + "; materials=" + info.MaterialCount
                    + "; supported=" + info.SupportedCount
                    + "; compatible=" + info.CompatibleCount
                    + "; protected=" + info.ProtectedCount
                    + "; unsupported=" + info.UnsupportedCount
                    + "; result=" + status);
                yield return new WaitForSecondsRealtime(0.45f);
                string fileName = "T"
                    + Mathf.RoundToInt(clampedTransparency).ToString("000")
                    + ".png";
                yield return StartCoroutine(CaptureClothingOpacityCalibrationFrame(
                    Path.Combine(partDirectory, fileName)));
            }

            VRClothingOpacityService.TryResetPart(
                character.charInfo, partId, out resetInfo, out resetStatus);
            yield return new WaitForSecondsRealtime(0.75f);
            yield return StartCoroutine(CaptureClothingOpacityCalibrationFrame(
                Path.Combine(partDirectory, "RESTORED.png")));
            Logger.LogInfo(
                "[KKVR opacity calibration] Restored part " + partId + ":"
                + VRCharacterClothingService.GetPartName(partId)
                + "; " + resetStatus);
        }

        Logger.LogInfo(
            "[KKVR opacity calibration] COMPLETE; screenshots=" + captureDirectory);
        yield return new WaitForSecondsRealtime(2f);
        Application.Quit();
    }

    private static bool TryGetOpacityCalibrationObjectKey(
        Studio.OCIChar character,
        out int objectKey)
    {
        objectKey = -1;
        Studio.Studio studio = Studio.Studio.Instance;
        if (character == null || studio == null || studio.dicObjectCtrl == null)
            return false;
        foreach (var pair in studio.dicObjectCtrl)
        {
            if (!ReferenceEquals(pair.Value, character))
                continue;
            objectKey = pair.Key;
            return true;
        }
        return false;
    }

    private static void PrepareOpacityCalibrationIsolation(
        int objectKey,
        ChaControl character,
        int targetPartId)
    {
        int[] coveringParts;
        switch (targetPartId)
        {
            case 1:
            case 2:
            case 5:
                coveringParts = new[] { 0 };
                break;
            case 3:
                coveringParts = new[] { 0, 1 };
                break;
            case 6:
                coveringParts = new[] { 5, 7 };
                break;
            default:
                coveringParts = new int[0];
                break;
        }

        foreach (int coveringPartId in coveringParts)
        {
            if (coveringPartId == targetPartId
                || !VRClothingOpacityService.HasPart(character, coveringPartId))
            {
                continue;
            }

            string stateStatus;
            if (VRCharacterClothingService.TrySetPartState(
                    objectKey, coveringPartId, 3, out stateStatus))
            {
                continue;
            }

            VRClothingOpacityInfo info;
            string opacityStatus;
            VRClothingOpacityService.TrySetPartOpacity(
                character,
                coveringPartId,
                0f,
                true,
                out info,
                out opacityStatus);
        }
    }

    private static void ResetOpacityCalibrationIsolation(
        ChaControl character,
        int targetPartId)
    {
        if (character == null)
            return;
        for (int partId = 0; partId < VRCharacterClothingService.PartCount; partId++)
        {
            if (partId == targetPartId
                || !VRClothingOpacityService.HasPart(character, partId))
            {
                continue;
            }
            VRClothingOpacityInfo info;
            string status;
            VRClothingOpacityService.TryResetPart(
                character, partId, out info, out status);
        }
    }

    private IEnumerator CaptureClothingOpacityCalibrationFrame(string path)
    {
        yield return new WaitForEndOfFrame();
        Application.CaptureScreenshot(path, 1);
        yield return new WaitForSecondsRealtime(0.8f);
        Logger.LogInfo("[KKVR opacity calibration] Screenshot=" + path);
    }

    private static string ResolveOpacityCalibrationCardPath()
    {
        string configured = GetCommandLineOption("--kkvr-opacity-card=");
        string erminDirectory = Path.Combine(
            Path.Combine(
                Path.Combine(
                    Path.Combine(Paths.GameRootPath, "UserData"),
                    "chara"),
                "female"),
            "Ermin");
        if (string.IsNullOrEmpty(configured))
            return Path.Combine(erminDirectory, "胡桃.png");
        return Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(erminDirectory, configured);
    }

    private static float[] ParseOpacityCalibrationLevels()
    {
        string configured = GetCommandLineOption("--kkvr-opacity-levels=");
        if (string.IsNullOrEmpty(configured))
        {
            return new[]
            {
                0f, 20f, 25f, 30f, 35f, 40f, 45f, 50f,
                55f, 60f, 65f, 70f, 75f, 80f, 85f, 100f
            };
        }

        List<float> levels = new List<float>();
        foreach (string item in configured.Split(','))
        {
            float value;
            if (float.TryParse(
                    item,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out value))
            {
                value = Mathf.Clamp(value, 0f, 100f);
                if (!levels.Contains(value))
                    levels.Add(value);
            }
        }
        levels.Sort();
        return levels.Count > 0 ? levels.ToArray() : new[] { 0f, 50f, 100f };
    }

    private static int[] ParseOpacityCalibrationParts()
    {
        string configured = GetCommandLineOption("--kkvr-opacity-parts=");
        if (string.IsNullOrEmpty(configured))
            return new[] { 0 };

        List<int> parts = new List<int>();
        foreach (string item in configured.Split(','))
        {
            int value;
            if (int.TryParse(item, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                && value >= 0
                && value < VRCharacterClothingService.PartCount
                && !parts.Contains(value))
            {
                parts.Add(value);
            }
        }
        return parts.Count > 0 ? parts.ToArray() : new[] { 0 };
    }

    private static string GetCommandLineOption(string prefix)
    {
        string[] arguments = Environment.GetCommandLineArgs();
        if (arguments == null || string.IsNullOrEmpty(prefix))
            return null;
        foreach (string argument in arguments)
        {
            if (argument != null
                && argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return argument.Substring(prefix.Length).Trim();
            }
        }
        return null;
    }

    private static string SanitizeCalibrationName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "card";
        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');
        return value;
    }

    private IEnumerator CaptureClothingOpacitySelfTestFrame(string path)
    {
        yield return new WaitForEndOfFrame();
        Application.CaptureScreenshot(path, 1);
        yield return new WaitForSecondsRealtime(1.5f);
        Logger.LogInfo("[KKVR opacity self-test] Screenshot=" + path);
    }

    private static bool HasExactArgument(string[] arguments, string expected)
    {
        if (arguments == null || string.IsNullOrEmpty(expected))
            return false;

        for (int index = 0; index < arguments.Length; index++)
        {
            if (string.Equals(
                    arguments[index],
                    expected,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static bool HasVrModeNone(string[] arguments)
    {
        if (arguments == null)
            return false;

        for (int index = 0; index < arguments.Length; index++)
        {
            string argument = arguments[index] ?? string.Empty;
            if (string.Equals(argument, "-vrmode", StringComparison.OrdinalIgnoreCase))
            {
                if (index + 1 < arguments.Length
                    && string.Equals(
                        arguments[index + 1],
                        "None",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // Keep scanning in case a launcher appends its enforced
                // -vrmode None after optional user arguments.
                continue;
            }

            const string Prefix = "-vrmode=";
            if (argument.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
                && string.Equals(
                    argument.Substring(Prefix.Length),
                    "None",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    internal static bool TryGetSavedReShadeSettings(out bool enabled, out string presetPath)
    {
        enabled = true;
        presetPath = string.Empty;
        if (_instance == null
            || _instance._desktopReShadeEnabled == null
            || _instance._desktopReShadePreset == null)
        {
            return false;
        }

        enabled = _instance._desktopReShadeEnabled.Value;
        presetPath = VRReShadeRuntimeService.ResolveAvailablePresetPath(
            _instance._desktopReShadePreset.Value);
        return true;
    }

    internal static bool SaveReShadePreference(bool enabled, string presetPath)
    {
        return _instance != null
            && _instance.SaveReShadePreferenceInternal(enabled, presetPath);
    }

    private void InitializeDesktopReShadeControls()
    {
        string[] presets = KKVRReShadeControl.GetPresetNames();
        string configuredPath = VRReShadeRuntimeService.GetConfiguredPresetPath();
        string configuredPreset = string.IsNullOrEmpty(configuredPath)
            ? string.Empty
            : VRReShadeRuntimeService.GetPresetRelativePath(configuredPath);
        if (string.IsNullOrEmpty(configuredPreset) && presets.Length > 0)
            configuredPreset = presets[0];

        _desktopReShadeEnabled = Config.Bind(
            ReShadeSection,
            "01 启用 ReShade",
            true,
            "即时开启或关闭 ReShade 效果；普通工作室和 VR 共用。快捷键默认 F8。");

        ConfigDescription presetDescription = new ConfigDescription(
            presets.Length > 0
                ? "从 reshade-shaders 根目录选择可用预设；运行中新增的 INI 会由 VR 菜单动态读取。快捷键默认 F9。"
                : "reshade-shaders 根目录没有可用预设；可在运行中添加有效 INI 后从 VR 菜单刷新。",
            null,
            new object[0]);
        _desktopReShadePreset = Config.Bind(
            ReShadeSection,
            "02 ReShade 预设",
            configuredPreset,
            presetDescription);

        _desktopReShadeToggleShortcut = Config.Bind(
            ReShadeSection,
            "03 开关快捷键",
            new KeyboardShortcut(KeyCode.F8),
            "无需 VR 头显即可切换 ReShade。设为 None 可禁用。");
        _desktopReShadeNextShortcut = Config.Bind(
            ReShadeSection,
            "04 下一个预设快捷键",
            new KeyboardShortcut(KeyCode.F9),
            "无需 VR 头显即可循环切换预设。设为 None 可禁用。");
        _desktopReShadeTestCommand = Config.Bind(
            ReShadeSection,
            "05 一次性测试指令",
            TestNone,
            new ConfigDescription(
                "选择后立即发送指令并自动恢复为“无”；完整自检会关闭、开启、切换预设并恢复原状态。",
                new AcceptableValueList<string>(
                    TestNone, TestToggle, TestNext, TestPrevious, TestFull),
                new object[0]));

        NormalizeDesktopPresetSelection();

        _desktopReShadeEnabled.SettingChanged += HandleDesktopReShadeEnabledChanged;
        _desktopReShadePreset.SettingChanged += HandleDesktopReShadePresetChanged;
        _desktopReShadeTestCommand.SettingChanged += HandleDesktopReShadeTestCommandChanged;
    }

    private IEnumerator ApplySavedDesktopReShadeSettings()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        bool enabled;
        string presetPath;
        if (!TryGetSavedReShadeSettings(out enabled, out presetPath))
            yield break;

        if (!string.IsNullOrEmpty(presetPath))
        {
            string presetMessage;
            if (!KKVRReShadeControl.TrySelectPreset(
                    presetPath,
                    enabled,
                    out presetMessage))
            {
                Logger.LogWarning("Desktop ReShade preset was not applied: " + presetMessage);
            }
        }
        else
        {
            string toggleMessage;
            if (!KKVRReShadeControl.TrySetEnabled(enabled, out toggleMessage))
                Logger.LogWarning("Desktop ReShade state was not applied: " + toggleMessage);
        }
    }

    private void HandleDesktopReShadeEnabledChanged(object sender, EventArgs args)
    {
        if (_suppressReShadeConfigEvents)
            return;
        string message;
        if (KKVRReShadeControl.TrySetEnabled(_desktopReShadeEnabled.Value, out message))
            Logger.LogInfo("Desktop ReShade effects=" + _desktopReShadeEnabled.Value + ": " + message);
        else
            Logger.LogError("Desktop ReShade toggle failed: " + message);
        NotifyWristReShadeSettingsChanged();
    }

    private void HandleDesktopReShadePresetChanged(object sender, EventArgs args)
    {
        if (_suppressReShadeConfigEvents)
            return;
        string presetPath = NormalizeDesktopPresetSelection();
        if (string.IsNullOrEmpty(presetPath))
        {
            Logger.LogError("Desktop ReShade preset switch failed: no available preset file was found");
            NotifyWristReShadeSettingsChanged();
            return;
        }
        string message;
        if (KKVRReShadeControl.TrySelectPreset(
                presetPath,
                _desktopReShadeEnabled.Value,
                out message))
        {
            Logger.LogInfo("Desktop ReShade preset: " + message);
        }
        else
        {
            Logger.LogError("Desktop ReShade preset switch failed: " + message);
        }
        NotifyWristReShadeSettingsChanged();
    }

    private void HandleDesktopReShadeTestCommandChanged(object sender, EventArgs args)
    {
        if (_suppressReShadeConfigEvents
            || _desktopReShadeTestCommand == null
            || string.Equals(_desktopReShadeTestCommand.Value, TestNone, StringComparison.Ordinal))
        {
            return;
        }

        string command = _desktopReShadeTestCommand.Value;
        ResetDesktopTestCommand();
        if (string.Equals(command, TestToggle, StringComparison.Ordinal))
            _desktopReShadeEnabled.Value = !_desktopReShadeEnabled.Value;
        else if (string.Equals(command, TestNext, StringComparison.Ordinal))
            SelectAdjacentDesktopPreset(1);
        else if (string.Equals(command, TestPrevious, StringComparison.Ordinal))
            SelectAdjacentDesktopPreset(-1);
        else if (string.Equals(command, TestFull, StringComparison.Ordinal) && !_selfTestRunning)
            StartCoroutine(RunReShadeSelfTest());
    }

    private void ResetDesktopTestCommand()
    {
        if (_desktopReShadeTestCommand == null)
            return;
        _suppressReShadeConfigEvents = true;
        _desktopReShadeTestCommand.Value = TestNone;
        _suppressReShadeConfigEvents = false;
    }

    private void SelectAdjacentDesktopPreset(int direction)
    {
        string selected;
        string message;
        if (!KKVRReShadeControl.TrySelectNextPreset(
                direction,
                _desktopReShadePreset != null ? _desktopReShadePreset.Value : string.Empty,
                _desktopReShadeEnabled == null || _desktopReShadeEnabled.Value,
                out selected,
                out message))
        {
            Logger.LogError("Desktop ReShade preset switch failed: " + message);
            return;
        }

        if (!SaveReShadePreferenceInternal(
                _desktopReShadeEnabled == null || _desktopReShadeEnabled.Value,
                selected))
        {
            Logger.LogWarning("Desktop ReShade preset changed but could not be saved");
        }
        Logger.LogInfo("Desktop ReShade preset: " + message);
    }

    private bool SaveReShadePreferenceInternal(bool enabled, string presetPath)
    {
        string resolvedPreset = VRReShadeRuntimeService.ResolveAvailablePresetPath(presetPath);
        try
        {
            _suppressReShadeConfigEvents = true;
            if (_desktopReShadeEnabled != null)
                _desktopReShadeEnabled.Value = enabled;
            if (_desktopReShadePreset != null && !string.IsNullOrEmpty(resolvedPreset))
            {
                _desktopReShadePreset.Value = VRReShadeRuntimeService.GetPresetRelativePath(resolvedPreset);
            }
            Config.Save();
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError("Unable to save ReShade preference: " + ex.Message);
            return false;
        }
        finally
        {
            _suppressReShadeConfigEvents = false;
            NotifyWristReShadeSettingsChanged();
        }
    }

    private string NormalizeDesktopPresetSelection()
    {
        if (_desktopReShadePreset == null)
            return string.Empty;

        string resolved = VRReShadeRuntimeService.ResolveAvailablePresetPath(
            _desktopReShadePreset.Value);
        if (string.IsNullOrEmpty(resolved))
            resolved = VRReShadeRuntimeService.GetConfiguredPresetPath();
        if (string.IsNullOrEmpty(resolved))
        {
            string[] available = KKVRReShadeControl.GetPresetNames();
            if (available.Length > 0)
                resolved = VRReShadeRuntimeService.ResolveAvailablePresetPath(available[0]);
        }

        string normalized = string.IsNullOrEmpty(resolved)
            ? string.Empty
            : VRReShadeRuntimeService.GetPresetRelativePath(resolved);
        if (!string.Equals(_desktopReShadePreset.Value, normalized, StringComparison.Ordinal))
        {
            try
            {
                _suppressReShadeConfigEvents = true;
                _desktopReShadePreset.Value = normalized;
                Config.Save();
                Logger.LogWarning("ReShade preset setting was migrated to an available preset: "
                    + (string.IsNullOrEmpty(normalized) ? "none available" : normalized));
            }
            finally
            {
                _suppressReShadeConfigEvents = false;
            }
        }
        return resolved;
    }

    private static void NotifyWristReShadeSettingsChanged()
    {
        if (VRWristMenuController.Instance != null)
            VRWristMenuController.Instance.NotifyReShadeConfigurationChanged();
    }

    private IEnumerator RunReShadeSelfTest()
    {
        if (_selfTestRunning)
            yield break;
        _selfTestRunning = true;
        Logger.LogInfo("[KKVR ReShade self-test] START (desktop/non-VR compatible)");

        yield return WaitForReShadeState(
            delegate(KKVRReShadeState state) { return state.RuntimeCount > 0; },
            30f);
        if (!_waitPassed || _waitObservedState == null)
        {
            Logger.LogError("[KKVR ReShade self-test] FAIL: no active ReShade runtime within 30 seconds");
            _selfTestRunning = false;
            yield break;
        }

        KKVRReShadeState original = _waitObservedState;
        bool originalEnabled = original.EffectsEnabled.HasValue
            ? original.EffectsEnabled.Value
            : (_desktopReShadeEnabled == null || _desktopReShadeEnabled.Value);
        string originalPreset = original.PresetName;
        if (string.IsNullOrEmpty(originalPreset) || originalPreset == "--")
            originalPreset = _desktopReShadePreset != null ? _desktopReShadePreset.Value : string.Empty;

        bool passed = true;
        string message;
        if (!KKVRReShadeControl.TrySetEnabled(false, out message))
        {
            Logger.LogError("[KKVR ReShade self-test] FAIL disable request: " + message);
            passed = false;
        }
        else
        {
            yield return WaitForReShadeState(
                delegate(KKVRReShadeState state)
                {
                    return state.EffectsEnabled.HasValue && !state.EffectsEnabled.Value && !state.RequestPending;
                },
                15f);
            passed = _waitPassed;
            Logger.Log(passed ? BepInEx.Logging.LogLevel.Info : BepInEx.Logging.LogLevel.Error,
                "[KKVR ReShade self-test] " + (passed ? "PASS" : "FAIL") + " disable/read-back");
        }

        if (passed && KKVRReShadeControl.TrySetEnabled(true, out message))
        {
            yield return WaitForReShadeState(
                delegate(KKVRReShadeState state)
                {
                    return state.EffectsEnabled.HasValue && state.EffectsEnabled.Value && !state.RequestPending;
                },
                15f);
            passed = _waitPassed;
            Logger.Log(passed ? BepInEx.Logging.LogLevel.Info : BepInEx.Logging.LogLevel.Error,
                "[KKVR ReShade self-test] " + (passed ? "PASS" : "FAIL") + " enable/read-back");
        }
        else if (passed)
        {
            Logger.LogError("[KKVR ReShade self-test] FAIL enable request: " + message);
            passed = false;
        }

        string targetPreset = FindDifferentPreset(originalPreset);
        if (passed && !string.IsNullOrEmpty(targetPreset)
            && KKVRReShadeControl.TrySelectPreset(targetPreset, true, out message))
        {
            yield return WaitForReShadeState(
                delegate(KKVRReShadeState state)
                {
                    return string.Equals(state.PresetName, targetPreset, StringComparison.OrdinalIgnoreCase)
                        && !state.RequestPending;
                },
                45f);
            passed = _waitPassed;
            Logger.Log(passed ? BepInEx.Logging.LogLevel.Info : BepInEx.Logging.LogLevel.Error,
                "[KKVR ReShade self-test] " + (passed ? "PASS" : "FAIL")
                + " preset/read-back: " + targetPreset);
        }
        else if (passed)
        {
            Logger.LogError("[KKVR ReShade self-test] FAIL preset request: "
                + (string.IsNullOrEmpty(targetPreset) ? "no alternate preset" : message));
            passed = false;
        }

        // Always restore the user's original preset and effect state.
        if (!string.IsNullOrEmpty(originalPreset) && originalPreset != "--")
        {
            KKVRReShadeControl.TrySelectPreset(originalPreset, originalEnabled, out message);
            yield return WaitForReShadeState(
                delegate(KKVRReShadeState state)
                {
                    return string.Equals(state.PresetName, originalPreset, StringComparison.OrdinalIgnoreCase)
                        && !state.RequestPending;
                },
                45f);
        }
        KKVRReShadeControl.TrySetEnabled(originalEnabled, out message);
        yield return WaitForReShadeState(
            delegate(KKVRReShadeState state)
            {
                return state.EffectsEnabled.HasValue
                    && state.EffectsEnabled.Value == originalEnabled
                    && !state.RequestPending;
            },
            15f);

        Logger.Log(passed ? BepInEx.Logging.LogLevel.Info : BepInEx.Logging.LogLevel.Error,
            "[KKVR ReShade self-test] " + (passed ? "COMPLETE: PASS" : "COMPLETE: FAIL")
            + "; original state restored=" + _waitPassed);
        _selfTestRunning = false;
    }

    private IEnumerator WaitForReShadeState(Func<KKVRReShadeState, bool> predicate, float timeoutSeconds)
    {
        _waitPassed = false;
        _waitObservedState = null;
        float deadline = Time.realtimeSinceStartup + timeoutSeconds;
        while (Time.realtimeSinceStartup < deadline)
        {
            KKVRReShadeState state;
            string ignored;
            if (KKVRReShadeControl.TryGetState(out state, out ignored))
            {
                _waitObservedState = state;
                if (predicate(state))
                {
                    _waitPassed = true;
                    yield break;
                }
            }
            yield return new WaitForSecondsRealtime(0.25f);
        }
    }

    private static string FindDifferentPreset(string originalPreset)
    {
        foreach (string preset in KKVRReShadeControl.GetPresetNames())
        {
            if (!string.Equals(preset, originalPreset, StringComparison.OrdinalIgnoreCase))
                return preset;
        }
        return string.Empty;
    }
}

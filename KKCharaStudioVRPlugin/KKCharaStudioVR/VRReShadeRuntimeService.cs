using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using VRGIN.Core;

namespace KKCharaStudioVR;

internal enum VRReShadeConnectionState
{
    NotInstalled,
    NotLoaded,
    BridgeMissing,
    BridgeUnavailable,
    WaitingForRuntime,
    Ready
}

internal sealed class VRReShadeSnapshot
{
    public VRReShadeConnectionState ConnectionState;
    public int RuntimeCount;
    public int VRRuntimeCount;
    public int EffectsState = -1;
    public int PresetState = -1;
    public bool RequestPending;
    public string PresetPath = string.Empty;
    public string Detail = string.Empty;
}

/// <summary>
/// Managed facade for the optional ReShade add-on bridge. Requests are queued
/// here and applied by the native bridge from each ReShade render callback.
/// </summary>
internal static class VRReShadeRuntimeService
{
    private const string BridgeLibrary = "KKVRReShadeBridge.addon64";
    private const int ExpectedBridgeVersion = 3;
    private const int PresetBufferCapacity = 32768;

    private static readonly string[] ReShadeModuleNames =
    {
        "d3d11.dll",
        "dxgi.dll",
        "ReShade64.dll"
    };

    private static string _lastBridgeError = string.Empty;
    private static string _gameRoot = string.Empty;

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr module, string procedureName);

    [DllImport(BridgeLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern int KKVR_ReShade_GetBridgeVersion();

    [DllImport(BridgeLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern int KKVR_ReShade_RequestEffects(int enabled);

    [DllImport(BridgeLibrary, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern int KKVR_ReShade_RequestPreset(
        [MarshalAs(UnmanagedType.LPWStr)] string presetPath,
        int enableEffects);

    [DllImport(BridgeLibrary, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern int KKVR_ReShade_GetSnapshot(
        out int runtimeCount,
        out int vrRuntimeCount,
        out int effectsState,
        out int presetState,
        out int requestPending,
        [Out] StringBuilder presetPath,
        int presetPathCapacity);

    internal static VRReShadeSnapshot GetSnapshot()
    {
        var snapshot = new VRReShadeSnapshot();
        if (!IsReShadeInstalled())
        {
            snapshot.ConnectionState = VRReShadeConnectionState.NotInstalled;
            return snapshot;
        }

        if (!IsReShadeLoaded())
        {
            snapshot.ConnectionState = VRReShadeConnectionState.NotLoaded;
            return snapshot;
        }

        if (!File.Exists(GetBridgePath()))
        {
            snapshot.ConnectionState = VRReShadeConnectionState.BridgeMissing;
            return snapshot;
        }

        // ReShade add-ons must register during ReShade startup. Never late-load
        // this DLL after the runtime initialization events have already passed.
        if (FindLoadedModuleWithExports("KKVR_ReShade_GetBridgeVersion") == IntPtr.Zero)
        {
            snapshot.ConnectionState = VRReShadeConnectionState.BridgeUnavailable;
            snapshot.Detail = "Bridge is installed but was not loaded at startup; restart CharaStudio";
            return snapshot;
        }

        try
        {
            int version = KKVR_ReShade_GetBridgeVersion();
            if (version != ExpectedBridgeVersion)
            {
                snapshot.ConnectionState = VRReShadeConnectionState.BridgeUnavailable;
                snapshot.Detail = "Bridge API " + version + ", expected " + ExpectedBridgeVersion;
                return snapshot;
            }

            var preset = new StringBuilder(PresetBufferCapacity);
            int pending;
            if (KKVR_ReShade_GetSnapshot(
                    out snapshot.RuntimeCount,
                    out snapshot.VRRuntimeCount,
                    out snapshot.EffectsState,
                    out snapshot.PresetState,
                    out pending,
                    preset,
                    preset.Capacity) == 0)
            {
                snapshot.ConnectionState = VRReShadeConnectionState.BridgeUnavailable;
                snapshot.Detail = "Bridge rejected the snapshot request";
                return snapshot;
            }

            snapshot.RequestPending = pending != 0;
            snapshot.PresetPath = NormalizePath(preset.ToString());
            snapshot.ConnectionState = snapshot.RuntimeCount > 0
                ? VRReShadeConnectionState.Ready
                : VRReShadeConnectionState.WaitingForRuntime;
            _lastBridgeError = string.Empty;
            return snapshot;
        }
        catch (Exception ex)
        {
            snapshot.ConnectionState = VRReShadeConnectionState.BridgeUnavailable;
            snapshot.Detail = DescribeInteropFailure(ex);
            LogBridgeFailureOnce(snapshot.Detail);
            return snapshot;
        }
    }

    internal static bool TrySetEffectsEnabled(
        bool enabled,
        out VRReShadeConnectionState connectionState,
        out bool queuedUntilRuntime,
        out string detail)
    {
        queuedUntilRuntime = false;
        detail = string.Empty;
        VRReShadeSnapshot snapshot = GetSnapshot();
        connectionState = snapshot.ConnectionState;
        if (!CanIssueBridgeRequest(snapshot, out detail))
            return false;

        try
        {
            int result = KKVR_ReShade_RequestEffects(enabled ? 1 : 0);
            if (result <= 0)
            {
                detail = "ReShade bridge rejected the request (" + result + ")";
                return false;
            }
            queuedUntilRuntime = result == 2;
            return true;
        }
        catch (Exception ex)
        {
            detail = DescribeInteropFailure(ex);
            LogBridgeFailureOnce(detail);
            connectionState = VRReShadeConnectionState.BridgeUnavailable;
            return false;
        }
    }

    internal static bool TrySelectPreset(
        string presetNameOrPath,
        bool enableEffects,
        out string presetPath,
        out VRReShadeConnectionState connectionState,
        out bool queuedUntilRuntime,
        out string detail)
    {
        presetPath = ResolvePresetSelection(presetNameOrPath);
        queuedUntilRuntime = false;
        detail = string.Empty;
        VRReShadeSnapshot snapshot = GetSnapshot();
        connectionState = snapshot.ConnectionState;
        if (!CanIssueBridgeRequest(snapshot, out detail))
            return false;
        if (string.IsNullOrEmpty(presetPath))
        {
            detail = "Preset file was not found";
            return false;
        }

        try
        {
            int result = KKVR_ReShade_RequestPreset(presetPath, enableEffects ? 1 : 0);
            if (result <= 0)
            {
                detail = result == -2
                    ? "Preset file is no longer available"
                    : "ReShade bridge rejected the request (" + result + ")";
                return false;
            }
            queuedUntilRuntime = result == 2;
            return true;
        }
        catch (Exception ex)
        {
            detail = DescribeInteropFailure(ex);
            LogBridgeFailureOnce(detail);
            connectionState = VRReShadeConnectionState.BridgeUnavailable;
            return false;
        }
    }

    internal static bool TrySwitchPreset(
        int direction,
        string savedPresetPath,
        bool enableEffects,
        out string presetPath,
        out VRReShadeConnectionState connectionState,
        out bool queuedUntilRuntime,
        out string detail)
    {
        presetPath = string.Empty;
        queuedUntilRuntime = false;
        detail = string.Empty;

        VRReShadeSnapshot snapshot = GetSnapshot();
        connectionState = snapshot.ConnectionState;
        if (!CanIssueBridgeRequest(snapshot, out detail))
            return false;

        List<string> presets = GetPresetFiles();
        if (presets.Count == 0)
        {
            detail = "No ReShade preset files were found";
            return false;
        }

        string current = ResolveExistingPresetPath(snapshot.PresetPath);
        if (string.IsNullOrEmpty(current))
            current = ResolveExistingPresetPath(savedPresetPath);
        if (string.IsNullOrEmpty(current))
            current = ResolveExistingPresetPath(ReadConfiguredPresetPath());

        int index = FindPathIndex(presets, current);
        if (direction < 0)
            index = index <= 0 ? presets.Count - 1 : index - 1;
        else
            index = index < 0 || index >= presets.Count - 1 ? 0 : index + 1;

        return TrySelectPreset(
            presets[index],
            enableEffects,
            out presetPath,
            out connectionState,
            out queuedUntilRuntime,
            out detail);
    }

    internal static List<string> GetPresetFiles()
    {
        // Deliberately rescan on every request. Preset files are commonly added
        // while Studio is running and the catalog is small enough that a
        // top-level directory scan is cheaper and safer than a stale cache.
        return RefreshPresetFiles();
    }

    internal static List<string> RefreshPresetFiles()
    {
        var result = new List<string>();
        try
        {
            string directory = GetPresetDirectory();
            if (!Directory.Exists(directory))
                return result;

            foreach (string candidate in Directory.GetFiles(
                         directory, "*", SearchOption.TopDirectoryOnly))
            {
                if (!string.Equals(
                        Path.GetExtension(candidate), ".ini", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string resolved = ResolveExistingPresetPath(candidate);
                if (!string.IsNullOrEmpty(resolved) && FindPathIndex(result, resolved) < 0)
                    result.Add(resolved);
            }
        }
        catch (Exception ex)
        {
            VRLog.Error("Unable to enumerate ReShade presets: " + ex.Message);
        }

        result.Sort(delegate(string left, string right)
        {
            int byName = StringComparer.CurrentCultureIgnoreCase.Compare(
                Path.GetFileNameWithoutExtension(left),
                Path.GetFileNameWithoutExtension(right));
            return byName != 0
                ? byName
                : StringComparer.OrdinalIgnoreCase.Compare(left, right);
        });
        return result;
    }

    internal static string GetDisplayPresetPath(VRReShadeSnapshot snapshot, string savedPresetPath)
    {
        // The BepInEx setting is the desired state and the single persisted source.
        // Prefer it while the asynchronous native request is still being applied.
        string path = ResolveAvailablePresetPath(savedPresetPath);
        if (string.IsNullOrEmpty(path))
            path = snapshot != null ? ResolveAvailablePresetPath(snapshot.PresetPath) : string.Empty;
        if (string.IsNullOrEmpty(path))
            path = ResolveAvailablePresetPath(ReadConfiguredPresetPath());
        if (string.IsNullOrEmpty(path))
        {
            List<string> presets = GetPresetFiles();
            if (presets.Count > 0)
                path = presets[0];
        }
        return path;
    }

    internal static string GetConfiguredPresetPath()
    {
        return ResolveAvailablePresetPath(ReadConfiguredPresetPath());
    }

    internal static string ResolveAvailablePresetPath(string selection)
    {
        if (string.IsNullOrEmpty(selection))
            return string.Empty;

        // The selectable catalog intentionally contains only top-level files,
        // but ReShade may report its current preset as either a relative or an
        // absolute path outside that catalog. Keep that valid current path
        // usable without recursively adding unrelated presets to the menu.
        return ResolvePresetSelection(selection);
    }

    internal static string GetPresetDisplayName(string path, int maximumLength)
    {
        string name = string.IsNullOrEmpty(path) ? "--" : Path.GetFileNameWithoutExtension(path);
        if (string.IsNullOrEmpty(name))
            name = "--";
        if (maximumLength > 3 && name.Length > maximumLength)
            name = name.Substring(0, maximumLength - 1) + "…";
        return name;
    }

    internal static string GetPresetRelativePath(string path)
    {
        string resolved = ResolveAvailablePresetPath(path);
        if (string.IsNullOrEmpty(resolved))
            return string.Empty;
        try
        {
            string root = NormalizePath(GetPresetDirectory()).TrimEnd(Path.DirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string full = NormalizePath(resolved);
            if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return full.Substring(root.Length);
        }
        catch
        {
            // An external active preset is still valid; retain its absolute path.
        }
        return resolved;
    }

    internal static bool PathsEqual(string left, string right)
    {
        return !string.IsNullOrEmpty(left)
            && !string.IsNullOrEmpty(right)
            && string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanIssueBridgeRequest(VRReShadeSnapshot snapshot, out string detail)
    {
        detail = snapshot != null ? snapshot.Detail : string.Empty;
        if (snapshot == null)
            return false;
        return snapshot.ConnectionState == VRReShadeConnectionState.Ready
            || snapshot.ConnectionState == VRReShadeConnectionState.WaitingForRuntime;
    }

    private static bool IsReShadeLoaded()
    {
        return FindLoadedModuleWithExports("ReShadeVersion", "ReShadeRegisterAddon") != IntPtr.Zero;
    }

    private static IntPtr FindLoadedModuleWithExports(params string[] exportNames)
    {
        try
        {
            foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
            {
                IntPtr baseAddress = module.BaseAddress;
                if (baseAddress == IntPtr.Zero)
                    continue;
                bool matches = true;
                foreach (string exportName in exportNames)
                {
                    if (GetProcAddress(baseAddress, exportName) == IntPtr.Zero)
                    {
                        matches = false;
                        break;
                    }
                }
                if (matches)
                    return baseAddress;
            }
        }
        catch (Exception ex)
        {
            LogBridgeFailureOnce("Unable to inspect loaded modules: " + ex.Message);
        }
        return IntPtr.Zero;
    }

    private static bool IsReShadeInstalled()
    {
        if (IsReShadeLoaded())
            return true;

        string root = GetGameRoot();
        foreach (string name in ReShadeModuleNames)
        {
            string path = Path.Combine(root, name);
            if (!File.Exists(path))
                continue;
            try
            {
                FileVersionInfo version = FileVersionInfo.GetVersionInfo(path);
                if (ContainsIgnoreCase(version.ProductName, "ReShade")
                    || ContainsIgnoreCase(version.FileDescription, "ReShade"))
                {
                    return true;
                }
            }
            catch
            {
                // Continue probing the other known module names.
            }
        }

        return File.Exists(Path.Combine(root, "ReShade.ini"))
            || File.Exists(Path.Combine(root, "ReShadeVR.ini"));
    }

    private static string ResolvePresetSelection(string selection)
    {
        if (string.IsNullOrEmpty(selection))
            return string.Empty;

        string direct = ResolveExistingPresetPath(selection);
        if (!string.IsNullOrEmpty(direct))
            return direct;

        string trimmed;
        string requestedName;
        try
        {
            trimmed = selection.Trim().Trim('"');
            requestedName = string.Equals(
                    Path.GetExtension(trimmed), ".ini", StringComparison.OrdinalIgnoreCase)
                ? Path.GetFileNameWithoutExtension(trimmed)
                : Path.GetFileName(trimmed);
        }
        catch
        {
            return string.Empty;
        }

        if (!string.Equals(
                Path.GetExtension(trimmed), ".ini", StringComparison.OrdinalIgnoreCase))
        {
            direct = ResolveExistingPresetPath(trimmed + ".ini");
            if (!string.IsNullOrEmpty(direct))
                return direct;
        }

        // Display names are persisted by older versions. Resolve them against
        // the live root catalog instead of a fixed list of built-in presets.
        List<string> presets = GetPresetFiles();
        string match = string.Empty;
        foreach (string preset in presets)
        {
            if (!string.Equals(
                    Path.GetFileNameWithoutExtension(preset),
                    requestedName,
                    StringComparison.CurrentCultureIgnoreCase))
            {
                continue;
            }

            // Do not silently choose if names ever become ambiguous (for
            // example on a case-sensitive filesystem).
            if (!string.IsNullOrEmpty(match) && !PathsEqual(match, preset))
                return string.Empty;
            match = preset;
        }
        if (!string.IsNullOrEmpty(match))
            return match;

        // A configured preset can legitimately live outside reshade-shaders.
        // This also lets a legacy display-name setting continue to resolve to
        // the active ReShade preset after an upgrade.
        string configured = ResolveExistingPresetPath(ReadConfiguredPresetPath());
        return !string.IsNullOrEmpty(configured)
            && string.Equals(
                Path.GetFileNameWithoutExtension(configured),
                requestedName,
                StringComparison.CurrentCultureIgnoreCase)
            ? configured
            : string.Empty;
    }

    private static string ReadConfiguredPresetPath()
    {
        string root = GetGameRoot();
        string value = ReadIniValue(Path.Combine(root, "ReShadeVR.ini"), "GENERAL", "PresetPath");
        if (string.IsNullOrEmpty(value))
            value = ReadIniValue(Path.Combine(root, "ReShade.ini"), "GENERAL", "PresetPath");
        return value;
    }

    private static string ReadIniValue(string path, string section, string key)
    {
        if (!File.Exists(path))
            return string.Empty;
        try
        {
            bool inSection = false;
            foreach (string rawLine in File.ReadAllLines(path))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#')
                    continue;
                if (line[0] == '[' && line[line.Length - 1] == ']')
                {
                    inSection = string.Equals(
                        line.Substring(1, line.Length - 2), section, StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (!inSection)
                    continue;
                int equals = line.IndexOf('=');
                if (equals > 0
                    && string.Equals(line.Substring(0, equals).Trim(), key, StringComparison.OrdinalIgnoreCase))
                {
                    return line.Substring(equals + 1).Trim();
                }
            }
        }
        catch (Exception ex)
        {
            VRLog.Error("Unable to read ReShade configuration '" + path + "': " + ex.Message);
        }
        return string.Empty;
    }

    private static string ResolveExistingPresetPath(string path)
    {
        foreach (string resolved in GetPresetPathCandidates(path))
        {
            if (IsValidPresetFile(resolved))
                return resolved;
        }
        return string.Empty;
    }

    private static List<string> GetPresetPathCandidates(string path)
    {
        var candidates = new List<string>();
        if (string.IsNullOrEmpty(path))
            return candidates;

        try
        {
            string trimmed = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
            if (Path.IsPathRooted(trimmed))
            {
                AddRootCandidate(candidates, trimmed);
            }
            else
            {
                AddRootCandidate(candidates, Path.Combine(GetGameRoot(), trimmed));
                AddRootCandidate(candidates, Path.Combine(GetPresetDirectory(), trimmed));
            }
        }
        catch
        {
            // Ignore malformed preset paths.
        }
        return candidates;
    }

    private static bool IsValidPresetFile(string path)
    {
        if (string.IsNullOrEmpty(path)
            || !string.Equals(Path.GetExtension(path), ".ini", StringComparison.OrdinalIgnoreCase)
            || !File.Exists(path))
        {
            return false;
        }

        string fileName = Path.GetFileName(path);
        if (string.Equals(fileName, "ReShade.ini", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileName, "ReShadeVR.ini", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            // Every ReShade preset written by the runtime contains at least
            // one of these top-level catalog keys, including an empty preset.
            foreach (string rawLine in File.ReadAllLines(path))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#')
                    continue;
                int equals = line.IndexOf('=');
                if (equals <= 0)
                    continue;
                string key = line.Substring(0, equals).Trim();
                if (string.Equals(key, "Techniques", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(key, "TechniqueSorting", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            VRLog.Error("Unable to inspect ReShade preset '" + path + "': " + ex.Message);
        }
        return false;
    }

    private static string ResolvePath(string path, bool requireIni)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;
        try
        {
            string trimmed = path.Trim().Trim('"');
            string resolved = Path.IsPathRooted(trimmed)
                ? Path.GetFullPath(trimmed)
                : Path.GetFullPath(Path.Combine(GetGameRoot(), trimmed));
            if (requireIni && !string.Equals(Path.GetExtension(resolved), ".ini", StringComparison.OrdinalIgnoreCase))
                return string.Empty;
            return resolved;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string NormalizePath(string path)
    {
        return ResolvePath(path, false);
    }

    private static int FindPathIndex(List<string> paths, string target)
    {
        for (int i = 0; i < paths.Count; i++)
        {
            if (PathsEqual(paths[i], target))
                return i;
        }
        return -1;
    }

    private static bool ContainsIgnoreCase(string value, string needle)
    {
        return !string.IsNullOrEmpty(value)
            && !string.IsNullOrEmpty(needle)
            && value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string GetGameRoot()
    {
        if (!string.IsNullOrEmpty(_gameRoot))
            return _gameRoot;

        var candidates = new List<string>();
        AddRootCandidate(candidates, AppDomain.CurrentDomain.BaseDirectory);
        try
        {
            Process process = Process.GetCurrentProcess();
            if (process.MainModule != null)
                AddRootCandidate(candidates, Path.GetDirectoryName(process.MainModule.FileName));
        }
        catch
        {
            // Process module access may be restricted; continue with other roots.
        }
        AddRootCandidate(candidates, Environment.CurrentDirectory);
        try
        {
            AddRootCandidate(candidates, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        }
        catch
        {
            // Dynamic assemblies may not expose a location.
        }

        foreach (string candidate in candidates)
        {
            DirectoryInfo directory = new DirectoryInfo(candidate);
            for (int depth = 0; directory != null && depth < 6; depth++, directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "CharaStudio.exe")))
                {
                    _gameRoot = directory.FullName;
                    return _gameRoot;
                }
            }
        }

        foreach (string candidate in candidates)
        {
            if (Directory.Exists(candidate))
            {
                _gameRoot = candidate;
                VRLog.Warn("CharaStudio root marker was not found; using " + _gameRoot);
                return _gameRoot;
            }
        }

        _gameRoot = Path.GetFullPath(".");
        return _gameRoot;
    }

    private static void AddRootCandidate(List<string> candidates, string candidate)
    {
        if (string.IsNullOrEmpty(candidate))
            return;
        try
        {
            string full = Path.GetFullPath(candidate);
            foreach (string existing in candidates)
            {
                if (string.Equals(existing, full, StringComparison.OrdinalIgnoreCase))
                    return;
            }
            candidates.Add(full);
        }
        catch
        {
            // Ignore malformed environment paths.
        }
    }

    private static string GetPresetDirectory()
    {
        return Path.Combine(GetGameRoot(), "reshade-shaders");
    }

    private static string GetBridgePath()
    {
        return Path.Combine(GetGameRoot(), BridgeLibrary);
    }

    private static string DescribeInteropFailure(Exception ex)
    {
        if (ex is DllNotFoundException)
            return "Bridge module is not loaded; restart CharaStudio after installing it";
        if (ex is EntryPointNotFoundException)
            return "Bridge API is incompatible";
        if (ex is BadImageFormatException)
            return "Bridge architecture is incompatible; x64 is required";
        return ex.GetType().Name + ": " + ex.Message;
    }

    private static void LogBridgeFailureOnce(string detail)
    {
        if (string.Equals(detail, _lastBridgeError, StringComparison.Ordinal))
            return;
        _lastBridgeError = detail;
        VRLog.Error("ReShade bridge unavailable: " + detail);
    }
}

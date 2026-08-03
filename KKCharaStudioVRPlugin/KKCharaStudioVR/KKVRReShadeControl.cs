using System;
using System.Collections.Generic;

namespace KKCharaStudioVR;

/// <summary>Read-only state returned by <see cref="KKVRReShadeControl"/>.</summary>
public sealed class KKVRReShadeState
{
    public string Connection { get; internal set; }
    public int RuntimeCount { get; internal set; }
    public int VRRuntimeCount { get; internal set; }
    public bool? EffectsEnabled { get; internal set; }
    public bool EffectsMixed { get; internal set; }
    public bool PresetsMixed { get; internal set; }
    public bool RequestPending { get; internal set; }
    public string PresetPath { get; internal set; }
    public string PresetName { get; internal set; }
    public string Detail { get; internal set; }
}

/// <summary>
/// Public in-process API for other Studio plug-ins. Requests are asynchronous;
/// call <see cref="TryGetState"/> to verify that the rendered view applied them.
/// </summary>
public static class KKVRReShadeControl
{
    public static string[] GetPresetNames()
    {
        return BuildPresetNames(VRReShadeRuntimeService.GetPresetFiles());
    }

    /// <summary>
    /// Rescans the top level of reshade-shaders and returns its current preset
    /// names. Consumers can call this when reopening or explicitly refreshing
    /// a menu; no Studio restart is required for file additions or removals.
    /// </summary>
    public static string[] RefreshPresetNames()
    {
        return BuildPresetNames(VRReShadeRuntimeService.RefreshPresetFiles());
    }

    private static string[] BuildPresetNames(List<string> paths)
    {
        var names = new string[paths.Count];
        for (int i = 0; i < paths.Count; i++)
            names[i] = VRReShadeRuntimeService.GetPresetDisplayName(paths[i], int.MaxValue);
        return names;
    }

    public static bool TryGetState(out KKVRReShadeState state, out string message)
    {
        VRReShadeSnapshot snapshot = VRReShadeRuntimeService.GetSnapshot();
        state = ConvertSnapshot(snapshot);
        message = string.IsNullOrEmpty(snapshot.Detail)
            ? GetConnectionLabel(snapshot.ConnectionState)
            : snapshot.Detail;
        return snapshot.ConnectionState == VRReShadeConnectionState.Ready
            || snapshot.ConnectionState == VRReShadeConnectionState.WaitingForRuntime;
    }

    public static bool TrySetEnabled(bool enabled, out string message)
    {
        VRReShadeConnectionState state;
        bool queued;
        string detail;
        bool success = VRReShadeRuntimeService.TrySetEffectsEnabled(
            enabled, out state, out queued, out detail);
        message = success
            ? (queued ? "Request queued until a ReShade view is rendered" : "Request accepted")
            : (string.IsNullOrEmpty(detail) ? GetConnectionLabel(state) : detail);
        return success;
    }

    public static bool TrySelectPreset(string presetNameOrPath, bool keepEffectsEnabled, out string message)
    {
        string path;
        VRReShadeConnectionState state;
        bool queued;
        string detail;
        bool success = VRReShadeRuntimeService.TrySelectPreset(
            presetNameOrPath,
            keepEffectsEnabled,
            out path,
            out state,
            out queued,
            out detail);
        message = success
            ? VRReShadeRuntimeService.GetPresetDisplayName(path, int.MaxValue)
                + (queued ? " queued until a ReShade view is rendered" : " request accepted")
            : (string.IsNullOrEmpty(detail) ? GetConnectionLabel(state) : detail);
        return success;
    }

    public static bool TrySelectNextPreset(
        int direction,
        string currentOrSavedPreset,
        bool keepEffectsEnabled,
        out string selectedPreset,
        out string message)
    {
        string path;
        VRReShadeConnectionState state;
        bool queued;
        string detail;
        bool success = VRReShadeRuntimeService.TrySwitchPreset(
            direction,
            currentOrSavedPreset,
            keepEffectsEnabled,
            out path,
            out state,
            out queued,
            out detail);
        selectedPreset = VRReShadeRuntimeService.GetPresetDisplayName(path, int.MaxValue);
        message = success
            ? selectedPreset + (queued ? " queued until a ReShade view is rendered" : " request accepted")
            : (string.IsNullOrEmpty(detail) ? GetConnectionLabel(state) : detail);
        return success;
    }

    private static KKVRReShadeState ConvertSnapshot(VRReShadeSnapshot snapshot)
    {
        bool? effects = null;
        if (snapshot.EffectsState == 0)
            effects = false;
        else if (snapshot.EffectsState == 1)
            effects = true;

        return new KKVRReShadeState
        {
            Connection = snapshot.ConnectionState.ToString(),
            RuntimeCount = snapshot.RuntimeCount,
            VRRuntimeCount = snapshot.VRRuntimeCount,
            EffectsEnabled = effects,
            EffectsMixed = snapshot.EffectsState == 2,
            PresetsMixed = snapshot.PresetState == 1,
            RequestPending = snapshot.RequestPending,
            PresetPath = snapshot.PresetPath ?? string.Empty,
            PresetName = VRReShadeRuntimeService.GetPresetDisplayName(snapshot.PresetPath, int.MaxValue),
            Detail = snapshot.Detail ?? string.Empty
        };
    }

    private static string GetConnectionLabel(VRReShadeConnectionState state)
    {
        switch (state)
        {
            case VRReShadeConnectionState.NotInstalled:
                return "ReShade was not detected";
            case VRReShadeConnectionState.NotLoaded:
                return "ReShade is installed but not loaded";
            case VRReShadeConnectionState.BridgeMissing:
                return "The ReShade control bridge is missing";
            case VRReShadeConnectionState.BridgeUnavailable:
                return "The ReShade control bridge is unavailable";
            case VRReShadeConnectionState.WaitingForRuntime:
                return "The bridge is ready and waiting for a rendered view";
            default:
                return "Ready";
        }
    }
}

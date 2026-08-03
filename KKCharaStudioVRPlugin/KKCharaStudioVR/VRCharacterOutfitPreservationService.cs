using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Studio;
using VRGIN.Core;

namespace KKCharaStudioVR;

/// <summary>
/// A short-lived snapshot used while OCIChar.ChangeChara rebuilds one character.
/// The synthetic coordinate keeps the old clothes and their coordinate plug-in
/// data, but only the accessory slots already tracked as safe KKVR additions are
/// selected when the snapshot is restored.
/// </summary>
internal sealed class VRCharacterOutfitPreservationState
{
    internal int ObjectKey;
    internal OCIChar Character;
    internal string TemporaryCoordinatePath;
    internal byte[] ClothesState;
    internal byte ShoesType;
    internal VRTrackedAccessorySnapshot TrackedAccessories;
    internal Dictionary<int, bool> TrackedAccessoryVisibility;
    internal bool RestoreStarted;
    internal bool Disposed;

    public int TrackedAccessoryCount =>
        TrackedAccessories?.Records?.Count ?? 0;
}

internal sealed class VRCharacterOutfitRestoreSession
{
    private readonly VRCharacterOutfitPreservationState _state;
    private readonly VRCoordinateLoadSession _coordinateSession;
    private readonly OCIChar _character;
    private bool _finished;

    internal VRCharacterOutfitRestoreSession(
        VRCharacterOutfitPreservationState state,
        VRCoordinateLoadSession coordinateSession,
        OCIChar character)
    {
        _state = state;
        _coordinateSession = coordinateSession;
        _character = character;
    }

    public VRCoordinateLoadSession CoordinateSession => _coordinateSession;

    public bool CanAbortSafely => !_finished
        && _coordinateSession != null
        && _coordinateSession.CanAbortSafely;

    public bool RequiresBackgroundMonitoring => !_finished
        && _coordinateSession != null
        && _coordinateSession.RequiresBackgroundMonitoring;

    public string TerminalRecoveryStatus =>
        _coordinateSession?.TerminalRecoveryStatus;

    public bool TryGetCompletion(out bool success, out string status)
    {
        if (_finished)
        {
            success = false;
            status = "角色服装保留会话已经结束";
            return true;
        }

        bool complete = _coordinateSession.TryGetCompletion(out success, out status);
        if (!complete)
            return false;

        if (success)
        {
            string runtimeStatus;
            if (!VRCharacterOutfitPreservationService.TryRestoreRuntimeState(
                _character,
                _state,
                out runtimeStatus))
            {
                success = false;
                status = (status ?? "旧服装已恢复")
                    + "；服装穿着状态恢复不完整："
                    + runtimeStatus;
            }
            else
            {
                status = (status ?? "旧服装已恢复")
                    + "；已保留旧服装、材质和穿着状态，并保留新角色自身饰品";
            }
        }

        _finished = true;
        VRCharacterOutfitPreservationService.Discard(_state);
        return true;
    }

    public void Abort()
    {
        if (_finished)
            return;
        _coordinateSession.Abort();
        if (!_coordinateSession.RequiresBackgroundMonitoring)
        {
            _finished = true;
            VRCharacterOutfitPreservationService.Discard(_state);
        }
    }
}

internal static class VRCharacterOutfitPreservationService
{
    private const string ExtendedSaveTypeName = "ExtensibleSaveFormat.ExtendedSave";
    private const string InternalDirectoryName = ".kkvr-runtime";
    private const string CoordinateMarker = "【KoiKatuClothes】";
    private const int CoordinateProductNumber = 100;

    public static bool TryCapture(
        int objectKey,
        OCIChar expectedCharacter,
        out VRCharacterOutfitPreservationState state,
        out string status)
    {
        state = null;
        string temporaryPath = null;
        try
        {
            OCIChar character;
            if (!VRCharacterCardService.TryGetCharacter(objectKey, out character, out status))
                return false;
            if (expectedCharacter != null && !ReferenceEquals(character, expectedCharacter))
            {
                status = "场景角色已变化，请重新选择替换目标";
                return false;
            }
            if (character.charInfo == null
                || !character.charInfo.loadEnd
                || character.charInfo.nowCoordinate == null)
            {
                status = "所选角色仍在读取，无法备份当前服装";
                return false;
            }

            ChaFileCoordinate source = character.charInfo.nowCoordinate;
            string lifecycleError;
            if (!VRCoordinateCardService.TryInvokeCharacterApiCoordinateLifecycle(
                character.charInfo,
                source,
                true,
                out lifecycleError))
            {
                status = "无法同步旧服装的材质和插件状态：" + lifecycleError;
                return false;
            }

            VRTrackedAccessorySnapshot tracked =
                VRCoordinateAccessoryTracker.Capture(character);
            int[] trackedSlots = tracked.Records
                .Select(record => record.SlotIndex)
                .Distinct()
                .OrderBy(slot => slot)
                .ToArray();
            ChaFileAccessory.PartsInfo[] parts = source.accessory?.parts;
            if (trackedSlots.Any(slot => parts == null
                || slot < 0
                || slot >= parts.Length
                || !VRCoordinateAccessoryTracker.IsOccupied(parts[slot])))
            {
                status = "本插件追踪的追加饰品已经变化，请先重新打开饰品管理页";
                return false;
            }

            byte[] extendedData;
            string extendedMarker;
            int extendedDataVersion;
            if (!TryCaptureSanitizedExtendedData(
                source,
                out extendedData,
                out extendedMarker,
                out extendedDataVersion,
                out status))
            {
                return false;
            }

            ChaFileCoordinate snapshot = new ChaFileCoordinate();
            if (!snapshot.LoadBytes(source.SaveBytes(), ChaFileDefine.ChaFileCoordinateVersion))
            {
                status = "无法复制旧角色的当前服装";
                return false;
            }
            snapshot.coordinateName = string.IsNullOrEmpty(source.coordinateName)
                ? "KKVR 角色替换服装快照"
                : source.coordinateName;

            temporaryPath = CreateTemporaryCoordinatePath();
            WriteCoordinateFile(
                temporaryPath,
                snapshot,
                extendedMarker,
                extendedDataVersion,
                extendedData);

            Dictionary<int, bool> trackedVisibility = new Dictionary<int, bool>();
            bool[] showAccessory = character.charFileStatus?.showAccessory;
            foreach (VRTrackedAccessoryRecord record in tracked.Records)
            {
                trackedVisibility[record.SlotIndex] = showAccessory == null
                    || record.SlotIndex >= showAccessory.Length
                    || showAccessory[record.SlotIndex];
            }

            state = new VRCharacterOutfitPreservationState
            {
                ObjectKey = objectKey,
                Character = character,
                TemporaryCoordinatePath = temporaryPath,
                ClothesState = character.charFileStatus?.clothesState != null
                    ? (byte[])character.charFileStatus.clothesState.Clone()
                    : new byte[0],
                ShoesType = character.charFileStatus?.shoesType ?? 0,
                TrackedAccessories = tracked,
                TrackedAccessoryVisibility = trackedVisibility
            };
            status = trackedSlots.Length > 0
                ? "已备份旧服装、材质、穿着状态和本插件追加饰品 "
                    + trackedSlots.Length
                    + " 件"
                : "已备份旧服装、材质和穿着状态";
            return true;
        }
        catch (Exception ex)
        {
            TryDeleteFile(temporaryPath);
            state = null;
            status = "备份角色服装失败："
                + VRCoordinateCardService.Unwrap(ex).Message;
            VRLog.Error(status);
            return false;
        }
    }

    public static bool TryBeginRestore(
        VRCharacterOutfitPreservationState state,
        int objectKey,
        OCIChar expectedCharacter,
        out VRCharacterOutfitRestoreSession session,
        out string status)
    {
        session = null;
        try
        {
            if (state == null || state.Disposed || state.RestoreStarted)
            {
                status = "角色服装保留快照已经失效";
                return false;
            }
            if (state.ObjectKey != objectKey
                || (expectedCharacter != null
                    && !ReferenceEquals(expectedCharacter, state.Character)))
            {
                status = "角色服装快照与当前替换目标不一致";
                return false;
            }
            if (string.IsNullOrEmpty(state.TemporaryCoordinatePath)
                || !File.Exists(state.TemporaryCoordinatePath))
            {
                status = "角色服装保留快照文件已经失效";
                return false;
            }

            OCIChar character;
            if (!VRCharacterCardService.TryGetCharacter(objectKey, out character, out status))
                return false;
            if (!ReferenceEquals(character, state.Character)
                || (expectedCharacter != null && !ReferenceEquals(character, expectedCharacter)))
            {
                status = "场景角色已变化，未把旧服装应用到其他角色";
                return false;
            }
            if (character.charInfo == null || !character.charInfo.loadEnd)
            {
                status = "新角色仍在读取，请稍后恢复旧服装";
                return false;
            }

            // ChangeChara reuses the OCIChar object. Drop any surviving old registry
            // entries before the coordinate session snapshots the new character.
            VRCoordinateAccessoryTracker.Clear(character);
            int[] trackedSlots = state.TrackedAccessories?.Records
                .Select(record => record.SlotIndex)
                .Distinct()
                .OrderBy(slot => slot)
                .ToArray() ?? new int[0];
            VRCoordinateLoadSession coordinateSession;
            if (!VRCoordinateCardService.TryBeginPreservedOutfitRestore(
                objectKey,
                character,
                state.TemporaryCoordinatePath,
                trackedSlots,
                out coordinateSession,
                out status))
            {
                return false;
            }

            state.RestoreStarted = true;
            session = new VRCharacterOutfitRestoreSession(
                state,
                coordinateSession,
                character);
            return true;
        }
        catch (Exception ex)
        {
            status = "启动旧服装恢复失败："
                + VRCoordinateCardService.Unwrap(ex).Message;
            VRLog.Error(status);
            return false;
        }
    }

    public static void Discard(VRCharacterOutfitPreservationState state)
    {
        if (state == null || state.Disposed)
            return;
        state.Disposed = true;
        TryDeleteFile(state.TemporaryCoordinatePath);
        state.TemporaryCoordinatePath = null;
    }

    internal static bool TryRestoreRuntimeState(
        OCIChar character,
        VRCharacterOutfitPreservationState state,
        out string status)
    {
        try
        {
            if (character == null
                || character.charInfo == null
                || state == null
                || !ReferenceEquals(character, state.Character))
            {
                status = "角色服装状态快照与当前角色不一致";
                return false;
            }

            byte[] clothesState = state.ClothesState ?? new byte[0];
            for (int part = 0; part < clothesState.Length; part++)
                character.SetClothesState(part, clothesState[part]);
            character.SetShoesType(state.ShoesType);

            RestoreTrackedAccessoryVisibility(character, state);
            status = null;
            return true;
        }
        catch (Exception ex)
        {
            status = VRCoordinateCardService.Unwrap(ex).Message;
            VRLog.Error("Could not restore preserved outfit runtime state: " + status);
            return false;
        }
    }

    private static void RestoreTrackedAccessoryVisibility(
        OCIChar character,
        VRCharacterOutfitPreservationState state)
    {
        if (state.TrackedAccessories == null
            || state.TrackedAccessoryVisibility == null)
        {
            return;
        }

        VRTrackedAccessorySnapshot current =
            VRCoordinateAccessoryTracker.Capture(character);
        List<VRTrackedAccessoryRecord> available = current.Records
            .Select(record => record.Clone())
            .ToList();
        bool[] visibility = character.charFileStatus?.showAccessory;
        foreach (VRTrackedAccessoryRecord oldRecord in state.TrackedAccessories.Records)
        {
            int matchIndex = available.FindIndex(record =>
                VRCoordinateAccessoryTracker.FingerprintsEqual(
                    oldRecord.Fingerprint,
                    record.Fingerprint));
            if (matchIndex < 0)
                continue;

            VRTrackedAccessoryRecord currentRecord = available[matchIndex];
            available.RemoveAt(matchIndex);
            bool visible;
            if (!state.TrackedAccessoryVisibility.TryGetValue(
                oldRecord.SlotIndex,
                out visible))
            {
                visible = true;
            }
            if (visibility != null
                && currentRecord.SlotIndex >= 0
                && currentRecord.SlotIndex < visibility.Length)
            {
                character.ShowAccessory(currentRecord.SlotIndex, visible);
            }
        }
    }

    private static bool TryCaptureSanitizedExtendedData(
        ChaFileCoordinate coordinate,
        out byte[] serializedData,
        out string marker,
        out int dataVersion,
        out string status)
    {
        serializedData = null;
        marker = null;
        dataVersion = 0;
        try
        {
            Type extendedSaveType = FindLoadedType(ExtendedSaveTypeName);
            if (extendedSaveType == null)
            {
                status = "ExtensibleSaveFormat 未加载，无法保留服装材质";
                return false;
            }

            const BindingFlags flags =
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo getAllData = null;
            MethodInfo serialize = null;
            MethodInfo deserialize = null;
            foreach (MethodInfo method in extendedSaveType.GetMethods(flags))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (string.Equals(method.Name, "GetAllExtendedData", StringComparison.Ordinal)
                    && parameters.Length == 1
                    && parameters[0].ParameterType == typeof(ChaFileCoordinate))
                {
                    getAllData = method;
                }
                else if (string.Equals(method.Name, "MessagePackSerialize", StringComparison.Ordinal)
                    && method.IsGenericMethodDefinition
                    && method.GetGenericArguments().Length == 1
                    && parameters.Length == 1)
                {
                    serialize = method;
                }
                else if (string.Equals(method.Name, "MessagePackDeserialize", StringComparison.Ordinal)
                    && method.IsGenericMethodDefinition
                    && method.GetGenericArguments().Length == 1
                    && parameters.Length == 1
                    && parameters[0].ParameterType == typeof(byte[]))
                {
                    deserialize = method;
                }
            }
            if (getAllData == null || serialize == null || deserialize == null)
            {
                status = "ExtensibleSaveFormat 服装快照接口不兼容";
                return false;
            }

            Type dictionaryType = getAllData.ReturnType;
            object currentData = getAllData.Invoke(null, new object[] { coordinate })
                ?? Activator.CreateInstance(dictionaryType);
            byte[] deepCopy = (byte[])serialize
                .MakeGenericMethod(dictionaryType)
                .Invoke(null, new[] { currentData });
            IDictionary sanitized = deserialize
                .MakeGenericMethod(dictionaryType)
                .Invoke(null, new object[] { deepCopy }) as IDictionary;
            if (sanitized == null)
            {
                status = "无法复制旧服装的扩展材质数据";
                return false;
            }

            string[] boundPluginGuids;
            string boundStatus;
            if (!VRCoordinateCardService.TryGetBoundAccessoryPluginGuids(
                out boundPluginGuids,
                out boundStatus))
            {
                status = boundStatus;
                return false;
            }
            foreach (string pluginGuid in boundPluginGuids)
                sanitized.Remove(pluginGuid);

            serializedData = (byte[])serialize
                .MakeGenericMethod(dictionaryType)
                .Invoke(null, new object[] { sanitized });
            if (serializedData == null)
            {
                status = "旧服装的扩展材质快照为空";
                return false;
            }

            FieldInfo markerField = extendedSaveType.GetField("Marker", flags);
            FieldInfo versionField = extendedSaveType.GetField("DataVersion", flags);
            marker = markerField?.GetValue(null) as string;
            dataVersion = versionField != null
                ? Convert.ToInt32(versionField.GetValue(null))
                : 0;
            if (string.IsNullOrEmpty(marker) || dataVersion <= 0)
            {
                status = "ExtensibleSaveFormat 文件标记接口不兼容";
                return false;
            }

            status = null;
            return true;
        }
        catch (Exception ex)
        {
            status = "捕获旧服装扩展材质失败："
                + VRCoordinateCardService.Unwrap(ex).Message;
            return false;
        }
    }

    private static string CreateTemporaryCoordinatePath()
    {
        string directory = Path.Combine(
            VRCoordinateCardService.Root,
            InternalDirectoryName);
        Directory.CreateDirectory(directory);
        try
        {
            File.SetAttributes(directory, File.GetAttributes(directory) | FileAttributes.Hidden);
        }
        catch (Exception)
        {
            // Hidden is cosmetic; path validation and unique names provide safety.
        }
        return Path.Combine(directory, "outfit_" + Guid.NewGuid().ToString("N") + ".png");
    }

    private static void WriteCoordinateFile(
        string path,
        ChaFileCoordinate coordinate,
        string extendedMarker,
        int extendedDataVersion,
        byte[] extendedData)
    {
        using FileStream stream = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);
        using BinaryWriter writer = new BinaryWriter(stream);
        writer.Write(CoordinateProductNumber);
        writer.Write(CoordinateMarker);
        writer.Write(ChaFileDefine.ChaFileCoordinateVersion.ToString());
        writer.Write(coordinate.coordinateName ?? string.Empty);
        byte[] coordinateBytes = coordinate.SaveBytes();
        writer.Write(coordinateBytes.Length);
        writer.Write(coordinateBytes);
        writer.Write(extendedMarker);
        writer.Write(extendedDataVersion);
        writer.Write(extendedData.Length);
        writer.Write(extendedData);
        writer.Flush();
    }

    private static Type FindLoadedType(string fullName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                Type type = assembly.GetType(fullName, throwOnError: false);
                if (type != null)
                    return type;
            }
            catch (Exception)
            {
                // Continue through optional plug-in assemblies.
            }
        }
        return null;
    }

    private static void TryDeleteFile(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            VRLog.Warn("Could not remove temporary outfit snapshot: "
                + VRCoordinateCardService.Unwrap(ex).Message);
        }
    }
}

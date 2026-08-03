using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Manager;
using Studio;
using UnityEngine;
using UnityEngine.UI;
using VRGIN.Core;

namespace KKCharaStudioVR;

internal enum VRCoordinateReplaceMode
{
    ClothesOnly,
    SelectedAccessories,
    Full,
    BoundCompatibleFull,
    RemoveTrackedAccessories
}

internal sealed class VRCoordinateAccessorySlotInfo
{
    public int SlotIndex;
    public int Type;
    public int Id;
    public string DisplayName;
}

/// <summary>
/// One-shot proof that the user has seen the destructive, slot-aligned accessory
/// replacement warning. The token binds the confirmation to one character, card,
/// file revision, and target coordinate state so a stale second click cannot apply
/// to a different target.
/// </summary>
internal sealed class VRBoundAccessoryReplaceConfirmation
{
    internal int ObjectKey;
    internal OCIChar Character;
    internal string Path;
    internal long FileLength;
    internal long FileWriteTicks;
    internal byte[] TargetCoordinateBytes;
    internal string[] SourcePluginGuids;
    internal string[] TargetPluginGuids;
    internal bool Consumed;

    public string Warning { get; internal set; }

    public string[] SourceBoundPluginGuids => SourcePluginGuids != null
        ? (string[])SourcePluginGuids.Clone()
        : new string[0];

    public string[] TargetBoundPluginGuids => TargetPluginGuids != null
        ? (string[])TargetPluginGuids.Clone()
        : new string[0];
}

internal static class VRCoordinateCardService
{
    private const string CoordinatePluginAssemblyName = "KK_CoordinateLoadOption";
    private static readonly string[] ClothesToggleNames =
    {
        "top",
        "bot",
        "bra",
        "shorts",
        "gloves",
        "panst",
        "socks",
        "shoes_inner",
        "shoes_outer",
        "accessories"
    };
    private static VRCoordinateLoadSession _activeSession;
    private static VRCoordinateUndoState _lastUndoState;

    public static string Root => UserData.Create("coordinate");

    public static bool TryGetBrowserRoot(out string root, out string status)
    {
        try
        {
            root = Root;
            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);
            status = null;
            return true;
        }
        catch (Exception ex)
        {
            root = null;
            status = "无法打开服装卡目录：" + ex.Message;
            return false;
        }
    }

    public static bool TryLoadNativePreview(
        string path,
        out Texture2D texture,
        out string coordinateName,
        out string status)
    {
        texture = null;
        coordinateName = null;
        try
        {
            ChaFileCoordinate coordinate;
            if (!TryLoadCoordinate(path, out coordinate, out status))
                return false;

            texture = PngAssist.LoadTexture(path);
            if (texture == null)
            {
                status = "工作室原生卡面接口未能读取图片";
                return false;
            }

            coordinateName = !string.IsNullOrEmpty(coordinate.coordinateName)
                ? coordinate.coordinateName
                : Path.GetFileNameWithoutExtension(path);
            status = null;
            return true;
        }
        catch (Exception ex)
        {
            if (texture != null)
            {
                UnityEngine.Object.Destroy(texture);
                texture = null;
            }
            status = "服装卡面读取失败：" + ex.Message;
            VRLog.Warn(status);
            return false;
        }
    }

    public static bool TryGetCoordinateAccessorySlots(
        string path,
        out List<VRCoordinateAccessorySlotInfo> slots,
        out string status)
    {
        slots = new List<VRCoordinateAccessorySlotInfo>();
        ChaFileCoordinate coordinate;
        if (!TryLoadCoordinate(path, out coordinate, out status))
            return false;

        try
        {
            ChaFileAccessory.PartsInfo[] parts = coordinate.accessory?.parts;
            if (parts == null)
            {
                status = null;
                return true;
            }

            ChaListControl listControl = Singleton<Manager.Character>.Instance?.chaListCtrl;
            for (int i = 0; i < parts.Length; i++)
            {
                ChaFileAccessory.PartsInfo part = parts[i];
                if (part == null || part.type == 120)
                    continue;

                string name = null;
                try
                {
                    name = listControl?
                        .GetListInfo((ChaListDefine.CategoryNo)part.type, part.id)?
                        .Name;
                }
                catch (Exception)
                {
                    // A missing mod list entry still remains selectable by slot.
                }

                slots.Add(new VRCoordinateAccessorySlotInfo
                {
                    SlotIndex = i,
                    Type = part.type,
                    Id = part.id,
                    DisplayName = string.IsNullOrEmpty(name) || name == "0" ? null : name
                });
            }

            TryAppendLegacyMoreAccessorySlots(coordinate, parts.Length, slots);
            status = null;
            return true;
        }
        catch (Exception ex)
        {
            slots.Clear();
            status = "读取服装卡饰品槽失败：" + Unwrap(ex).Message;
            return false;
        }
    }

    public static bool TryGetTrackedAccessorySlots(
        OCIChar character,
        out List<VRTrackedAccessorySlotInfo> slots,
        out string status)
    {
        slots = new List<VRTrackedAccessorySlotInfo>();
        if (character == null
            || character.charInfo == null
            || character.charInfo.nowCoordinate?.accessory?.parts == null)
        {
            status = "目标角色尚未初始化完成或已经失效";
            return false;
        }

        slots = VRCoordinateAccessoryTracker.GetValidSlots(character);
        status = slots.Count == 0
            ? "这个角色没有由本插件追加且仍可安全管理的饰品"
            : null;
        return true;
    }

    private static void TryAppendLegacyMoreAccessorySlots(
        ChaFileCoordinate coordinate,
        int firstSlot,
        ICollection<VRCoordinateAccessorySlotInfo> slots)
    {
        try
        {
            Assembly pluginAssembly = FindLoadedAssembly(CoordinatePluginAssemblyName);
            Type pluginType = pluginAssembly?.GetType(
                "KK_CoordinateLoadOption.KK_CoordinateLoadOption",
                throwOnError: false);
            FieldInfo modeField = pluginType?.GetField(
                "_isMoreAccessoriesExist",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (modeField == null || Convert.ToInt32(modeField.GetValue(null)) != 1)
                return;

            Type supportType = pluginAssembly.GetType(
                "KK_CoordinateLoadOption.MoreAccessories_Support",
                throwOnError: false);
            MethodInfo loadMethod = supportType?.GetMethod(
                "LoadMoreAcc",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            string[] names = loadMethod?.Invoke(null, new object[] { coordinate }) as string[];
            if (names == null || names.Length == 0)
                return;

            Type stringsType = pluginAssembly.GetType(
                "KK_CoordinateLoadOption.StringResources.StringResourcesManager",
                throwOnError: false);
            MethodInfo getString = stringsType?.GetMethod(
                "GetString",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(string) },
                null);
            string emptyName = getString?.Invoke(null, new object[] { "empty" }) as string;
            if (string.IsNullOrEmpty(emptyName))
                return;

            for (int i = 0; i < names.Length; i++)
            {
                if (string.IsNullOrEmpty(names[i])
                    || string.Equals(names[i], emptyName, StringComparison.Ordinal))
                {
                    continue;
                }

                slots.Add(new VRCoordinateAccessorySlotInfo
                {
                    SlotIndex = firstSlot + i,
                    Type = -1,
                    Id = -1,
                    DisplayName = names[i]
                });
            }
        }
        catch (Exception ex)
        {
            VRLog.Warn("Could not enumerate legacy MoreAccessories slots: "
                + Unwrap(ex).Message);
        }
    }

    public static bool TryPrepareBoundCompatibleReplace(
        int objectKey,
        OCIChar expectedCharacter,
        string path,
        out VRBoundAccessoryReplaceConfirmation confirmation,
        out string status)
    {
        confirmation = null;
        try
        {
            if (!TryEnsureNoActiveSession("确认原槽换装", out status))
                return false;

            ChaFileCoordinate coordinate;
            if (!TryLoadCoordinate(path, out coordinate, out status))
                return false;

            OCIChar character;
            if (!TryResolveReadyCharacter(
                objectKey,
                expectedCharacter,
                "确认原槽换装",
                out character,
                out status))
            {
                return false;
            }

            Assembly pluginAssembly = FindLoadedAssembly(CoordinatePluginAssemblyName);
            if (pluginAssembly == null)
            {
                status = "原槽兼容换装需要 Coordinate Load Option；未执行换装";
                return false;
            }
            if (IsCoordinateLoadOptionBusy(pluginAssembly))
            {
                status = "Coordinate Load Option 正在处理其他换装，请稍后";
                return false;
            }

            string lifecycleError;
            if (!TryInvokeCharacterApiCoordinateLifecycle(
                character.charInfo,
                character.charInfo.nowCoordinate,
                true,
                out lifecycleError))
            {
                status = "无法同步目标角色的饰品绑定状态：" + lifecycleError;
                return false;
            }

            string[] sourcePlugins;
            string[] targetPlugins;
            if (!TryInspectBoundAccessoryData(
                pluginAssembly,
                coordinate,
                character.charInfo.nowCoordinate,
                out sourcePlugins,
                out targetPlugins,
                out status))
            {
                return false;
            }
            if (sourcePlugins.Length == 0 && targetPlugins.Length == 0)
            {
                status = "没有检测到饰品绑定数据；请使用普通整套换装";
                return false;
            }

            FileInfo file = new FileInfo(path);
            confirmation = new VRBoundAccessoryReplaceConfirmation
            {
                ObjectKey = objectKey,
                Character = character,
                Path = Path.GetFullPath(path),
                FileLength = file.Length,
                FileWriteTicks = file.LastWriteTimeUtc.Ticks,
                TargetCoordinateBytes = character.charInfo.nowCoordinate.SaveBytes(),
                SourcePluginGuids = sourcePlugins,
                TargetPluginGuids = targetPlugins,
                Warning = BuildBoundCompatibleWarning(sourcePlugins, targetPlugins)
            };
            status = confirmation.Warning;
            return true;
        }
        catch (Exception ex)
        {
            status = "无法准备原槽兼容换装确认：" + Unwrap(ex).Message;
            VRLog.Error(status);
            return false;
        }
    }

    public static bool TryBeginBoundCompatibleReplace(
        VRBoundAccessoryReplaceConfirmation confirmation,
        out VRCoordinateLoadSession session,
        out string status)
    {
        session = null;
        try
        {
            if (confirmation == null || confirmation.Consumed)
            {
                status = "原槽换装确认已失效，请重新确认";
                return false;
            }
            if (!TryEnsureNoActiveSession("原槽兼容换装", out status))
                return false;

            FileInfo file = new FileInfo(confirmation.Path ?? string.Empty);
            if (!file.Exists
                || file.Length != confirmation.FileLength
                || file.LastWriteTimeUtc.Ticks != confirmation.FileWriteTicks)
            {
                status = "服装卡在确认后已变化，请重新确认";
                return false;
            }

            ChaFileCoordinate coordinate;
            if (!TryLoadCoordinate(confirmation.Path, out coordinate, out status))
                return false;

            OCIChar character;
            if (!TryResolveReadyCharacter(
                confirmation.ObjectKey,
                confirmation.Character,
                "原槽兼容换装",
                out character,
                out status))
            {
                return false;
            }
            byte[] currentTargetBytes = character.charInfo.nowCoordinate.SaveBytes();
            if (confirmation.TargetCoordinateBytes == null
                || currentTargetBytes == null
                || !confirmation.TargetCoordinateBytes.SequenceEqual(currentTargetBytes))
            {
                status = "目标角色服装在确认后已变化，请重新确认";
                return false;
            }

            Assembly pluginAssembly = FindLoadedAssembly(CoordinatePluginAssemblyName);
            if (pluginAssembly == null)
            {
                status = "原槽兼容换装需要 Coordinate Load Option；未执行换装";
                return false;
            }
            if (IsCoordinateLoadOptionBusy(pluginAssembly))
            {
                status = "Coordinate Load Option 正在处理其他换装，请稍后";
                return false;
            }

            string lifecycleError;
            if (!TryInvokeCharacterApiCoordinateLifecycle(
                character.charInfo,
                character.charInfo.nowCoordinate,
                true,
                out lifecycleError))
            {
                status = "无法同步目标角色的饰品绑定状态：" + lifecycleError;
                return false;
            }

            string[] sourcePlugins;
            string[] targetPlugins;
            if (!TryInspectBoundAccessoryData(
                pluginAssembly,
                coordinate,
                character.charInfo.nowCoordinate,
                out sourcePlugins,
                out targetPlugins,
                out status))
            {
                return false;
            }
            if (!SamePluginSet(sourcePlugins, confirmation.SourcePluginGuids)
                || !SamePluginSet(targetPlugins, confirmation.TargetPluginGuids))
            {
                status = "饰品绑定数据在确认后已变化，请重新确认";
                return false;
            }

            int accessoryCount = GetAccessoryCount(pluginAssembly, coordinate);
            string coordinateName = !string.IsNullOrEmpty(coordinate.coordinateName)
                ? coordinate.coordinateName
                : Path.GetFileNameWithoutExtension(confirmation.Path);
            string[] boundPlugins = sourcePlugins
                .Concat(targetPlugins)
                .Where(guid => !string.IsNullOrEmpty(guid))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            VRCoordinateLoadSession candidate = new VRCoordinateLoadSession(
                pluginAssembly,
                character,
                confirmation.Path,
                coordinateName,
                accessoryCount,
                ClothesToggleNames,
                Enumerable.Range(0, accessoryCount),
                false,
                false,
                VRCoordinateReplaceMode.BoundCompatibleFull,
                0,
                true,
                null,
                coordinate,
                boundPlugins);
            if (!candidate.Begin(out status))
            {
                candidate.Abort();
                return false;
            }

            confirmation.Consumed = true;
            session = candidate;
            RegisterSession(session);
            if (string.IsNullOrEmpty(status))
                status = "已确认覆盖风险，正在按原槽完整替换衣服和饰品";
            return true;
        }
        catch (Exception ex)
        {
            status = "原槽兼容换装启动失败：" + Unwrap(ex).Message;
            VRLog.Error(status);
            return false;
        }
    }

    internal static bool TryBeginPreservedOutfitRestore(
        int objectKey,
        OCIChar expectedCharacter,
        string path,
        int[] trackedAccessorySlots,
        out VRCoordinateLoadSession session,
        out string status)
    {
        return TryBeginReplaceCore(
            objectKey,
            expectedCharacter,
            path,
            trackedAccessorySlots != null && trackedAccessorySlots.Length > 0
                ? VRCoordinateReplaceMode.SelectedAccessories
                : VRCoordinateReplaceMode.ClothesOnly,
            trackedAccessorySlots,
            true,
            out session,
            out status);
    }

    public static bool TryBeginReplace(
        int objectKey,
        OCIChar expectedCharacter,
        string path,
        bool protectHairAccessories,
        out VRCoordinateLoadSession session,
        out string status)
    {
        return TryBeginReplace(
            objectKey,
            expectedCharacter,
            path,
            protectHairAccessories
                ? VRCoordinateReplaceMode.ClothesOnly
                : VRCoordinateReplaceMode.Full,
            null,
            out session,
            out status);
    }

    public static bool TryBeginReplace(
        int objectKey,
        OCIChar expectedCharacter,
        string path,
        VRCoordinateReplaceMode mode,
        int[] selectedAccessorySlots,
        out VRCoordinateLoadSession session,
        out string status)
    {
        return TryBeginReplaceCore(
            objectKey,
            expectedCharacter,
            path,
            mode,
            selectedAccessorySlots,
            false,
            out session,
            out status);
    }

    private static bool TryBeginReplaceCore(
        int objectKey,
        OCIChar expectedCharacter,
        string path,
        VRCoordinateReplaceMode mode,
        int[] selectedAccessorySlots,
        bool shieldTargetBoundData,
        out VRCoordinateLoadSession session,
        out string status)
    {
        session = null;
        try
        {
            if (mode == VRCoordinateReplaceMode.BoundCompatibleFull)
            {
                status = "原槽兼容换装必须先取得二次确认令牌";
                return false;
            }

            if (_activeSession != null)
            {
                bool priorSuccess;
                string priorStatus;
                try
                {
                    _activeSession.TryGetCompletion(out priorSuccess, out priorStatus);
                }
                catch (Exception ex)
                {
                    VRLog.Warn("Could not poll the previous outfit load: " + Unwrap(ex).Message);
                    if (_activeSession != null && _activeSession.CanAbortSafely)
                        _activeSession.Abort();
                }
                if (_activeSession != null)
                {
                    status = "已有服装卡正在替换，请稍后";
                    return false;
                }
            }

            ChaFileCoordinate coordinate;
            if (!TryLoadCoordinate(path, out coordinate, out status))
                return false;

            OCIChar character;
            if (!VRCharacterCardService.TryGetCharacter(objectKey, out character, out status))
                return false;
            if (expectedCharacter != null && !ReferenceEquals(character, expectedCharacter))
            {
                status = "场景角色已变化，请重新选择换装目标";
                return false;
            }
            if (character.charInfo == null)
            {
                status = "所选角色尚未初始化完成或已经失效";
                return false;
            }
            if (!character.charInfo.loadEnd)
            {
                status = "所选角色仍在读取，请稍后再换装";
                return false;
            }

            string coordinateName = !string.IsNullOrEmpty(coordinate.coordinateName)
                ? coordinate.coordinateName
                : Path.GetFileNameWithoutExtension(path);
            Assembly pluginAssembly = FindLoadedAssembly(CoordinatePluginAssemblyName);
            if (pluginAssembly != null && IsCoordinateLoadOptionBusy(pluginAssembly))
            {
                status = "Coordinate Load Option 正在处理其他换装，请稍后";
                return false;
            }

            // Full outfit loading uses the same append-only CLO path as the custom
            // accessory picker. Only occupied source slots are selected so empty source
            // slots are never counted as additions and existing target accessories are
            // never overwritten (including accessory-based hair).
            if (mode == VRCoordinateReplaceMode.Full)
            {
                List<VRCoordinateAccessorySlotInfo> sourceAccessorySlots;
                string sourceAccessoryStatus;
                if (!TryGetCoordinateAccessorySlots(
                    path,
                    out sourceAccessorySlots,
                    out sourceAccessoryStatus))
                {
                    status = "无法准备整套换装的饰品追加：" + sourceAccessoryStatus;
                    return false;
                }

                selectedAccessorySlots = sourceAccessorySlots
                    .Select(slot => slot.SlotIndex)
                    .Distinct()
                    .OrderBy(slot => slot)
                    .ToArray();
            }

            int skippedDuplicateAccessories = 0;
            if (mode != VRCoordinateReplaceMode.ClothesOnly
                && selectedAccessorySlots != null
                && selectedAccessorySlots.Length > 0)
            {
                selectedAccessorySlots = VRCoordinateAccessoryTracker.FilterDuplicateSourceSlots(
                    character,
                    coordinate,
                    selectedAccessorySlots,
                    out skippedDuplicateAccessories);
            }
            if (shieldTargetBoundData
                && mode == VRCoordinateReplaceMode.SelectedAccessories
                && (selectedAccessorySlots == null || selectedAccessorySlots.Length == 0))
            {
                // All preserved additions already exist on the new character. Keep
                // the CLO clothing/material path instead of falling back to a base-
                // data-only compatibility assignment.
                mode = VRCoordinateReplaceMode.ClothesOnly;
            }

            int accessoryCount = pluginAssembly != null
                ? GetAccessoryCount(pluginAssembly, coordinate)
                : Math.Max(20, coordinate.accessory?.parts?.Length ?? 0);
            if (selectedAccessorySlots != null && selectedAccessorySlots.Length > 0)
                accessoryCount = Math.Max(accessoryCount, selectedAccessorySlots.Max() + 1);

            if (mode == VRCoordinateReplaceMode.ClothesOnly
                || (mode == VRCoordinateReplaceMode.Full
                    && selectedAccessorySlots != null
                    && selectedAccessorySlots.Length == 0))
            {
                if (pluginAssembly == null)
                {
                    return TryBeginCompatibilityReplace(
                        character,
                        coordinate,
                        coordinateName,
                        "Coordinate Load Option 未安装",
                        skippedDuplicateAccessories,
                        out session,
                        out status);
                }

                VRCoordinateLoadSession clothesOnlyCandidate = new VRCoordinateLoadSession(
                    pluginAssembly,
                    character,
                    path,
                    coordinateName,
                    accessoryCount,
                    ClothesToggleNames,
                    new int[0],
                    true,
                    false,
                    mode,
                    skippedDuplicateAccessories);
                if (!clothesOnlyCandidate.Begin(out status))
                {
                    bool canFallback = !clothesOnlyCandidate.TargetMayHaveChanged;
                    clothesOnlyCandidate.Abort();
                    if (status != null
                        && status.StartsWith(
                            "Coordinate Load Option 正在处理其他换装",
                            StringComparison.Ordinal))
                    {
                        return false;
                    }

                    if (!canFallback)
                        return false;

                    VRLog.Warn(
                        "Coordinate Load Option adapter unavailable before loading began; switching to the clothing-only path: "
                        + status);
                    return TryBeginCompatibilityReplace(
                        character,
                        coordinate,
                        coordinateName,
                        "Coordinate Load Option 接口不可用",
                        skippedDuplicateAccessories,
                        out session,
                        out status);
                }

                session = clothesOnlyCandidate;
                RegisterSession(session);
                if (string.IsNullOrEmpty(status))
                    status = mode == VRCoordinateReplaceMode.Full
                        ? skippedDuplicateAccessories > 0
                            ? "正在完整替换衣服；已跳过重复饰品 "
                                + skippedDuplicateAccessories
                                + " 件"
                            : "正在完整替换衣服；源卡没有饰品，现有饰品保持不变"
                        : "正在通过 Coordinate Load Option 只替换衣服";
                return true;
            }

            int[] normalizedSlots;
            if (selectedAccessorySlots == null)
            {
                normalizedSlots = Enumerable.Range(0, accessoryCount).ToArray();
            }
            else
            {
                normalizedSlots = selectedAccessorySlots
                    .Where(slot => slot >= 0 && slot < accessoryCount)
                    .Distinct()
                    .OrderBy(slot => slot)
                    .ToArray();
            }

            if (normalizedSlots.Length == 0)
            {
                return TryBeginCompatibilityReplace(
                    character,
                    coordinate,
                    coordinateName,
                    null,
                    skippedDuplicateAccessories,
                    out session,
                    out status);
            }

            if (pluginAssembly == null)
            {
                if (selectedAccessorySlots == null)
                {
                    return TryBeginCompatibilityReplace(
                        character,
                        coordinate,
                        coordinateName,
                        "Coordinate Load Option 未安装",
                        out session,
                        out status);
                }

                status = mode == VRCoordinateReplaceMode.Full
                    ? "整套换装的饰品追加需要 Coordinate Load Option；未执行换装"
                    : "自选饰品需要 Coordinate Load Option；未执行换装";
                return false;
            }

            string[] sourceBoundPlugins;
            string[] targetBoundPlugins;
            string boundDataReason;
            if (!TryInspectBoundAccessoryData(
                pluginAssembly,
                coordinate,
                character.charInfo.nowCoordinate,
                out sourceBoundPlugins,
                out targetBoundPlugins,
                out boundDataReason))
            {
                VRLog.Warn(boundDataReason);
                status = "无法检查饰品绑定数据：" + boundDataReason;
                return false;
            }
            if (sourceBoundPlugins.Length > 0
                || (targetBoundPlugins.Length > 0 && !shieldTargetBoundData))
            {
                boundDataReason = BuildBoundDataReason(
                    sourceBoundPlugins,
                    targetBoundPlugins);
                VRLog.Warn(boundDataReason);
                status = mode == VRCoordinateReplaceMode.Full
                    ? "整套换装未执行：" + boundDataReason
                        + "；可在确认后使用原槽兼容换装"
                    : "自选饰品未执行：" + boundDataReason;
                return false;
            }

            VRCoordinateLoadSession candidate = new VRCoordinateLoadSession(
                pluginAssembly,
                character,
                path,
                coordinateName,
                accessoryCount,
                ClothesToggleNames,
                normalizedSlots,
                false,
                GetMoreAccessoriesMode(pluginAssembly) != 1,
                mode,
                skippedDuplicateAccessories,
                false,
                shieldTargetBoundData ? targetBoundPlugins : null,
                null,
                null);
            if (!candidate.Begin(out status))
            {
                bool canFallback = !candidate.TargetMayHaveChanged;
                candidate.Abort();
                if (status != null
                    && status.StartsWith(
                        "Coordinate Load Option 正在处理其他换装",
                        StringComparison.Ordinal))
                {
                    return false;
                }

                if (!canFallback)
                    return false;

                if (mode == VRCoordinateReplaceMode.Full)
                {
                    status = "整套换装未执行：" + status;
                    return false;
                }

                VRLog.Warn(
                    "Coordinate Load Option adapter unavailable before loading began; switching to the clothing-only path: "
                    + status);
                return TryBeginCompatibilityReplace(
                    character,
                    coordinate,
                    coordinateName,
                    "Coordinate Load Option 接口不可用",
                    skippedDuplicateAccessories,
                    out session,
                    out status);
            }

            session = candidate;
            RegisterSession(session);
            if (string.IsNullOrEmpty(status))
                status = mode == VRCoordinateReplaceMode.Full
                    ? "正在完整替换衣服，并把全部源饰品追加到空槽"
                    : "正在换衣服并把所选饰品追加到空槽";
            if (skippedDuplicateAccessories > 0)
            {
                status += "；已跳过重复饰品 "
                    + skippedDuplicateAccessories
                    + " 件";
            }
            return true;
        }
        catch (Exception ex)
        {
            status = "服装卡替换失败：" + Unwrap(ex).Message;
            VRLog.Error(status);
            return false;
        }
    }

    private static bool TryBeginCompatibilityReplace(
        OCIChar character,
        ChaFileCoordinate coordinate,
        string coordinateName,
        string fallbackReason,
        out VRCoordinateLoadSession session,
        out string status)
    {
        return TryBeginCompatibilityReplace(
            character,
            coordinate,
            coordinateName,
            fallbackReason,
            0,
            out session,
            out status);
    }

    private static bool TryBeginCompatibilityReplace(
        OCIChar character,
        ChaFileCoordinate coordinate,
        string coordinateName,
        string fallbackReason,
        int skippedDuplicateAccessories,
        out VRCoordinateLoadSession session,
        out string status)
    {
        try
        {
            // Fail-safe mode deliberately keeps the target coordinate's complete
            // accessory block. This prevents accessory-based hair from disappearing
            // when selective accessory loading cannot be proven safe.
            status = string.IsNullOrEmpty(fallbackReason)
                ? "已用兼容模式只替换衣服，并保留全部现有饰品：" + coordinateName
                : "为保护发饰，已只替换衣服并保留全部现有饰品：" + coordinateName;
            if (skippedDuplicateAccessories > 0)
            {
                status += "；已跳过重复饰品 "
                    + skippedDuplicateAccessories
                    + " 件";
            }
            Exception loadError;
            if (!VRCoordinateLoadSession.TryCreateNativeAndRun(
                character,
                status,
                "兼容模式换装失败：",
                () =>
                {
                    ChaFileCoordinate preserved = new ChaFileCoordinate();
                    if (!preserved.LoadBytes(
                        character.charInfo.nowCoordinate.SaveBytes(),
                        ChaFileDefine.ChaFileCoordinateVersion))
                    {
                        throw new InvalidOperationException("无法锁定目标角色现有饰品");
                    }
                    character.charInfo.nowCoordinate.clothes = coordinate.clothes;
                    character.charInfo.nowCoordinate.accessory = preserved.accessory;
                    character.charInfo.AssignCoordinate(
                        (ChaFileDefine.CoordinateType)character.charInfo.fileStatus.coordinateType);
                    character.SetCoordinateInfo(
                        (ChaFileDefine.CoordinateType)character.charInfo.fileStatus.coordinateType,
                        true);
                },
                true,
                out session,
                out loadError))
            {
                status = "兼容模式换装失败：" + Unwrap(loadError).Message;
                return false;
            }
            RegisterSession(session);
            VRLog.Warn(string.IsNullOrEmpty(fallbackReason)
                ? "Coordinate Load Option was not detected. Used the clothing-only compatibility path."
                : fallbackReason + "; used the clothing-only compatibility path.");
            return true;
        }
        catch (Exception ex)
        {
            session = null;
            status = "兼容模式换装失败：" + Unwrap(ex).Message;
            return false;
        }
    }

    private static bool TryLoadCoordinate(
        string path,
        out ChaFileCoordinate coordinate,
        out string status)
    {
        coordinate = null;
        if (string.IsNullOrEmpty(path)
            || !File.Exists(path)
            || !string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase)
            || !VRWristFileCatalog.IsInsideRoot(Root, path))
        {
            status = "服装卡文件无效";
            return false;
        }

        coordinate = new ChaFileCoordinate();
        if (!coordinate.LoadFile(path))
        {
            status = "文件不是有效的服装卡";
            coordinate = null;
            return false;
        }

        status = null;
        return true;
    }

    private static Assembly FindLoadedAssembly(string simpleName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            AssemblyName name;
            try
            {
                name = assembly.GetName();
            }
            catch (Exception)
            {
                continue;
            }

            if (string.Equals(name.Name, simpleName, StringComparison.OrdinalIgnoreCase))
                return assembly;
        }
        return null;
    }

    private static int GetAccessoryCount(Assembly pluginAssembly, ChaFileCoordinate coordinate)
    {
        int count = coordinate.accessory?.parts?.Length ?? 0;
        try
        {
            int mode = GetMoreAccessoriesMode(pluginAssembly);
            if (mode != 1)
                return Math.Max(20, count);

            Type supportType = pluginAssembly.GetType(
                "KK_CoordinateLoadOption.MoreAccessories_Support",
                throwOnError: false);
            MethodInfo loadMethod = supportType?.GetMethod(
                "LoadMoreAcc",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            ICollection extra = loadMethod?.Invoke(null, new object[] { coordinate }) as ICollection;
            if (extra != null)
                count += extra.Count;
        }
        catch (Exception ex)
        {
            VRLog.Warn("Could not inspect legacy MoreAccessories coordinate slots: " + Unwrap(ex).Message);
        }
        return Math.Max(20, count);
    }

    private static int GetMoreAccessoriesMode(Assembly pluginAssembly)
    {
        try
        {
            Type pluginType = pluginAssembly?.GetType(
                "KK_CoordinateLoadOption.KK_CoordinateLoadOption",
                throwOnError: false);
            FieldInfo moreAccessoriesField = pluginType?.GetField(
                "_isMoreAccessoriesExist",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return moreAccessoriesField != null
                ? Convert.ToInt32(moreAccessoriesField.GetValue(null))
                : 0;
        }
        catch (Exception ex)
        {
            VRLog.Warn("Could not inspect MoreAccessories mode: " + Unwrap(ex).Message);
            return 0;
        }
    }

    private static bool IsCoordinateLoadOptionBusy(Assembly pluginAssembly)
    {
        try
        {
            Type loadType = pluginAssembly.GetType(
                "KK_CoordinateLoadOption.CoordinateLoad",
                throwOnError: false);
            if (loadType == null)
                return false;

            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo totalField = loadType.GetField("totalAmount", flags);
            FieldInfo queueField = loadType.GetField("oCICharQueue", flags);
            FieldInfo temporaryField = loadType.GetField("tmpChaCtrl", flags);
            int total = totalField != null ? Convert.ToInt32(totalField.GetValue(null)) : 0;
            object queue = queueField?.GetValue(null);
            int queueCount = queue is ICollection collection ? collection.Count : 0;
            return total > 0 || queueCount > 0 || temporaryField?.GetValue(null) != null;
        }
        catch (Exception ex)
        {
            VRLog.Warn("Could not inspect Coordinate Load Option activity: " + Unwrap(ex).Message);
            return false;
        }
    }

    internal static bool TryGetBoundAccessoryPluginGuids(
        out string[] pluginGuids,
        out string status)
    {
        pluginGuids = new string[0];
        try
        {
            Assembly pluginAssembly = FindLoadedAssembly(CoordinatePluginAssemblyName);
            Type pluginType = pluginAssembly?.GetType(
                "KK_CoordinateLoadOption.KK_CoordinateLoadOption",
                throwOnError: false);
            FieldInfo boundPluginsField = pluginType?.GetField(
                "pluginBoundAccessories",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            string[] values = boundPluginsField?.GetValue(null) as string[];
            if (values == null)
            {
                status = "无法验证 Coordinate Load Option 的饰品绑定数据";
                return false;
            }

            pluginGuids = values
                .Where(guid => !string.IsNullOrEmpty(guid))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(guid => guid, StringComparer.Ordinal)
                .ToArray();
            status = null;
            return true;
        }
        catch (Exception ex)
        {
            status = "读取饰品绑定插件列表失败：" + Unwrap(ex).Message;
            return false;
        }
    }

    private static bool TryInspectBoundAccessoryData(
        Assembly pluginAssembly,
        ChaFileCoordinate source,
        ChaFileCoordinate target,
        out string[] sourcePluginGuids,
        out string[] targetPluginGuids,
        out string reason)
    {
        sourcePluginGuids = new string[0];
        targetPluginGuids = new string[0];
        reason = null;
        try
        {
            Type pluginType = pluginAssembly?.GetType(
                "KK_CoordinateLoadOption.KK_CoordinateLoadOption",
                throwOnError: false);
            FieldInfo boundPluginsField = pluginType?.GetField(
                "pluginBoundAccessories",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            string[] pluginGuids = boundPluginsField?.GetValue(null) as string[];
            if (pluginGuids == null)
            {
                reason = "无法验证 Coordinate Load Option 的饰品绑定数据";
                return false;
            }

            Type extendedSaveType = FindLoadedType("ExtensibleSaveFormat.ExtendedSave");
            MethodInfo getExtendedData = FindExtendedDataReader(extendedSaveType);
            if (getExtendedData == null)
            {
                reason = "无法读取服装卡的饰品绑定数据";
                return false;
            }

            List<string> sourceMatches = new List<string>();
            List<string> targetMatches = new List<string>();
            foreach (string pluginGuid in pluginGuids
                .Where(guid => !string.IsNullOrEmpty(guid))
                .Distinct(StringComparer.Ordinal))
            {
                if (source != null
                    && getExtendedData.Invoke(null, new object[] { source, pluginGuid }) != null)
                {
                    sourceMatches.Add(pluginGuid);
                }
                if (target != null
                    && getExtendedData.Invoke(null, new object[] { target, pluginGuid }) != null)
                {
                    targetMatches.Add(pluginGuid);
                }
            }

            sourcePluginGuids = sourceMatches.OrderBy(guid => guid, StringComparer.Ordinal).ToArray();
            targetPluginGuids = targetMatches.OrderBy(guid => guid, StringComparer.Ordinal).ToArray();
            return true;
        }
        catch (Exception ex)
        {
            reason = "检查饰品绑定数据失败：" + Unwrap(ex).Message;
            return false;
        }
    }

    private static string BuildBoundDataReason(
        string[] sourcePluginGuids,
        string[] targetPluginGuids)
    {
        if (sourcePluginGuids != null && sourcePluginGuids.Length > 0)
            return "服装卡含饰品绑定数据（" + string.Join("、", sourcePluginGuids) + "）";
        if (targetPluginGuids != null && targetPluginGuids.Length > 0)
            return "目标角色含饰品绑定数据（" + string.Join("、", targetPluginGuids) + "）";
        return "未检测到饰品绑定数据";
    }

    private static string BuildBoundCompatibleWarning(
        string[] sourcePluginGuids,
        string[] targetPluginGuids)
    {
        string details = BuildBoundDataReason(sourcePluginGuids, targetPluginGuids);
        return details
            + "。再次确认后将按原槽完整覆盖目标饰品（包括头饰），并同步绑定数据；可使用一次换装撤销恢复。";
    }

    private static bool SamePluginSet(string[] left, string[] right)
    {
        return new HashSet<string>(left ?? new string[0], StringComparer.Ordinal)
            .SetEquals(right ?? new string[0]);
    }

    private static bool TryEnsureNoActiveSession(string operation, out string status)
    {
        if (_activeSession != null)
        {
            bool ignoredSuccess;
            string ignoredStatus;
            try
            {
                _activeSession.TryGetCompletion(out ignoredSuccess, out ignoredStatus);
            }
            catch (Exception ex)
            {
                VRLog.Warn("Could not poll the previous outfit operation before "
                    + operation
                    + ": "
                    + Unwrap(ex).Message);
                if (_activeSession != null && _activeSession.CanAbortSafely)
                    _activeSession.Abort();
            }
        }
        if (_activeSession != null)
        {
            status = "已有服装卡操作正在进行，请稍后再" + operation;
            return false;
        }

        status = null;
        return true;
    }

    private static bool TryResolveReadyCharacter(
        int objectKey,
        OCIChar expectedCharacter,
        string operation,
        out OCIChar character,
        out string status)
    {
        if (!VRCharacterCardService.TryGetCharacter(objectKey, out character, out status))
            return false;
        if (expectedCharacter != null && !ReferenceEquals(character, expectedCharacter))
        {
            status = "场景角色已变化，请重新" + operation;
            return false;
        }
        if (character.charInfo == null || !character.charInfo.loadEnd)
        {
            status = "所选角色仍在读取，请稍后再" + operation;
            return false;
        }
        return true;
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
                // Ignore assemblies that cannot expose their type table.
            }
        }
        return null;
    }

    internal static MethodInfo FindExtendedDataReader(Type extendedSaveType)
    {
        if (extendedSaveType == null)
            return null;

        foreach (MethodInfo method in extendedSaveType.GetMethods(
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!string.Equals(method.Name, "GetExtendedDataById", StringComparison.Ordinal))
                continue;
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 2
                && parameters[0].ParameterType.IsAssignableFrom(typeof(ChaFileCoordinate))
                && parameters[1].ParameterType == typeof(string))
            {
                return method;
            }
        }
        return null;
    }

    internal static bool TryInvokeCharacterApiCoordinateLifecycle(
        ChaControl character,
        ChaFileCoordinate coordinate,
        bool saving,
        out string error)
    {
        error = null;
        if (character == null || coordinate == null)
        {
            error = "目标角色的插件数据同步参数无效";
            return false;
        }

        try
        {
            Type characterApi = FindLoadedType("KKAPI.Chara.CharacterApi");
            if (characterApi == null)
            {
                error = "KKAPI CharacterApi 未加载";
                return false;
            }

            string methodName = saving
                ? "OnCoordinateBeingSaved"
                : "OnCoordinateBeingLoaded";
            MethodInfo lifecycle = characterApi.GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new[] { typeof(ChaControl), typeof(ChaFileCoordinate) },
                null);
            if (lifecycle == null)
            {
                error = "KKAPI CharacterApi 服装生命周期接口不可用";
                return false;
            }

            // Invoke only KKAPI's controller lifecycle for this exact character.
            // Do not broadcast ExtendedSave CoordinateWrite/ReadEvent here: in Studio
            // those global events call Maker-only MoreAccessories/Sideloader handlers.
            lifecycle.Invoke(null, new object[] { character, coordinate });
            return true;
        }
        catch (Exception ex)
        {
            error = Unwrap(ex).Message;
            return false;
        }
    }

    internal static void RepairStudioCharacterDynamics(OCIChar character)
    {
        if (character == null || character.charInfo == null)
            return;

        try
        {
            AddObjectAssist.InitHairBone(character, Singleton<Info>.Instance.dicBoneInfo);
            character.hairDynamic = AddObjectFemale.GetHairDynamic(character.charInfo.objHair);
            character.skirtDynamic = AddObjectFemale.GetSkirtDynamic(character.charInfo.objClothes);
            character.InitFK(null);
            OIBoneInfo.BoneGroup[] parts = FKCtrl.parts;
            for (int i = 0; i < parts.Length && i < character.oiCharInfo.activeFK.Length; i++)
            {
                character.ActiveFK(
                    parts[i],
                    character.oiCharInfo.activeFK[i],
                    character.oiCharInfo.activeFK[i]);
            }
            character.ActiveKinematicMode(
                OICharInfo.KinematicMode.FK,
                character.oiCharInfo.enableFK,
                true);
            character.UpdateFKColor(OIBoneInfo.BoneGroup.Hair);
        }
        catch (Exception ex)
        {
            VRLog.Error(
                "Could not rebuild Studio hair/skirt dynamics after an interrupted outfit load: "
                + Unwrap(ex).Message);
        }
    }

    internal static Exception Unwrap(Exception exception)
    {
        while (exception is TargetInvocationException invocation
            && invocation.InnerException != null)
        {
            exception = invocation.InnerException;
        }
        return exception;
    }

    public static bool TryBeginRemoveTrackedAccessories(
        int objectKey,
        OCIChar expectedCharacter,
        int[] requestedSlots,
        out VRCoordinateLoadSession session,
        out string status)
    {
        session = null;
        try
        {
            if (_activeSession != null)
            {
                bool ignoredSuccess;
                string ignoredStatus;
                try
                {
                    _activeSession.TryGetCompletion(out ignoredSuccess, out ignoredStatus);
                }
                catch (Exception ex)
                {
                    VRLog.Warn("Could not poll the previous outfit operation before removing tracked accessories: "
                        + Unwrap(ex).Message);
                }
                if (_activeSession != null)
                {
                    status = "已有服装卡操作正在进行，请稍后再管理追加饰品";
                    return false;
                }
            }

            OCIChar character;
            if (!VRCharacterCardService.TryGetCharacter(objectKey, out character, out status))
                return false;
            if (expectedCharacter != null && !ReferenceEquals(character, expectedCharacter))
            {
                status = "场景角色已变化，请重新选择饰品管理目标";
                return false;
            }
            if (character.charInfo == null || !character.charInfo.loadEnd)
            {
                status = "所选角色仍在读取，请稍后再管理追加饰品";
                return false;
            }

            int[] validSlots = VRCoordinateAccessoryTracker.ResolveValidRemovalSlots(
                character,
                requestedSlots);
            if (validSlots.Length == 0)
            {
                status = "所选饰品已被修改或不再由本插件管理；未删除任何饰品";
                return false;
            }

            Exception loadError;
            string completedStatus = "已移除本次追加饰品 " + validSlots.Length + " 件";
            if (!VRCoordinateLoadSession.TryCreateNativeAndRun(
                character,
                completedStatus,
                "移除追加饰品失败：",
                () =>
                {
                    int removed = VRCoordinateAccessoryTracker.ApplyRemoval(
                        character,
                        validSlots);
                    if (removed != validSlots.Length)
                    {
                        throw new InvalidOperationException(
                            "饰品槽在删除前已变化，为避免误删已取消操作");
                    }

                    ChaFileDefine.CoordinateType type =
                        (ChaFileDefine.CoordinateType)character.charInfo.fileStatus.coordinateType;
                    if (!character.charInfo.AssignCoordinate(type))
                        throw new InvalidOperationException("工作室没有接受饰品删除请求");
                    TrySynchronizeMoreAccessoriesArrays(character.charInfo);
                    character.SetCoordinateInfo(type, true);
                },
                false,
                () => VRCoordinateAccessoryTracker.CommitRemoval(character, validSlots),
                out session,
                out loadError))
            {
                status = "移除追加饰品失败：" + Unwrap(loadError).Message;
                return false;
            }

            RegisterSession(session);
            status = "正在移除本次追加饰品 " + validSlots.Length + " 件";
            return true;
        }
        catch (Exception ex)
        {
            session = null;
            status = "移除追加饰品失败：" + Unwrap(ex).Message;
            return false;
        }
    }

    internal static void TrySynchronizeMoreAccessoriesArrays(ChaControl character)
    {
        if (character == null)
            return;

        try
        {
            Type moreAccessories = FindLoadedType("MoreAccessoriesKOI.MoreAccessories");
            MethodInfo arraySync = moreAccessories?.GetMethod(
                "ArraySync",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[] { typeof(ChaControl) },
                null);
            arraySync?.Invoke(null, new object[] { character });
        }
        catch (Exception ex)
        {
            // SetCoordinateInfo remains the fallback. Do not fail a safe base-slot
            // deletion merely because an optional MoreAccessories bridge changed.
            VRLog.Warn("Could not synchronize MoreAccessories arrays after accessory removal: "
                + Unwrap(ex).Message);
        }
    }

    public static bool TryBeginUndoLastReplace(
        int objectKey,
        OCIChar expectedCharacter,
        out VRCoordinateLoadSession session,
        out string status)
    {
        session = null;
        try
        {
            if (_activeSession != null)
            {
                bool ignoredSuccess;
                string ignoredStatus;
                try
                {
                    _activeSession.TryGetCompletion(out ignoredSuccess, out ignoredStatus);
                }
                catch (Exception ex)
                {
                    VRLog.Warn("Could not poll the previous outfit load before undo: "
                        + Unwrap(ex).Message);
                }
                if (_activeSession != null)
                {
                    status = "已有服装卡操作正在进行，请稍后再撤销";
                    return false;
                }
            }

            OCIChar character;
            if (!VRCharacterCardService.TryGetCharacter(objectKey, out character, out status))
                return false;
            if (expectedCharacter != null && !ReferenceEquals(character, expectedCharacter))
            {
                status = "场景角色已变化，请重新选择撤销目标";
                return false;
            }
            if (character.charInfo == null || !character.charInfo.loadEnd)
            {
                status = "所选角色仍在读取，请稍后再撤销";
                return false;
            }

            VRCoordinateUndoState undoState = _lastUndoState;
            if (undoState == null || !ReferenceEquals(undoState.Character, character))
            {
                status = "当前角色没有可撤销的换装记录";
                return false;
            }

            Exception error;
            status = "正在撤销到换装前状态";
            if (!VRCoordinateLoadSession.TryCreateUndoAndRun(
                character,
                undoState,
                "已撤销到换装前状态",
                out session,
                out error))
            {
                status = "撤销换装失败：" + Unwrap(error).Message;
                return false;
            }

            RegisterSession(session);
            return true;
        }
        catch (Exception ex)
        {
            session = null;
            status = "撤销换装失败：" + Unwrap(ex).Message;
            return false;
        }
    }

    internal static void StoreUndoSnapshot(VRCoordinateLoadSession session)
    {
        VRCoordinateUndoState snapshot = session?.CreateUndoSnapshot();
        if (snapshot != null)
            _lastUndoState = snapshot;
    }

    internal static void ConsumeUndoSnapshot(OCIChar character)
    {
        if (_lastUndoState != null
            && ReferenceEquals(_lastUndoState.Character, character))
        {
            _lastUndoState = null;
        }
    }

    public static void AbortActiveSession()
    {
        VRCoordinateLoadSession session = _activeSession;
        if (session == null || !session.CanAbortSafely)
            return;
        session.Abort();
        if (!session.RequiresBackgroundMonitoring)
            ReleaseSession(session);
    }

    internal static void ReleaseSession(VRCoordinateLoadSession session)
    {
        if (ReferenceEquals(_activeSession, session))
            _activeSession = null;
    }

    internal static bool IsActiveSession(VRCoordinateLoadSession session)
    {
        return session != null && ReferenceEquals(_activeSession, session);
    }

    private static void RegisterSession(VRCoordinateLoadSession session)
    {
        _activeSession = session;
    }
}

internal sealed class VRCoordinateUndoState
{
    public OCIChar Character;
    public byte[] CoordinateBytes;
    public Version CoordinateVersion;
    public string CoordinateName;
    public MethodInfo GetAllExtendedCoordinateData;
    public MethodInfo SetExtendedCoordinateData;
    public MethodInfo DeserializeExtendedData;
    public Type ExtendedCoordinateDictionaryType;
    public byte[] ExtendedCoordinateBytes;
    public bool HasExtendedCoordinateData;
    public VRTrackedAccessorySnapshot TrackedAccessories;
    public byte[] ClothesState;
    public bool[] ShowAccessory;
    public byte ShoesType;
}

internal sealed class VRCoordinateLoadSession
{
    private const BindingFlags StaticFields =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    private readonly Assembly _assembly;
    private readonly OCIChar _character;
    private readonly string _path;
    private readonly string _coordinateName;
    private readonly int _accessoryCount;
    private readonly string[] _clothesToggleNames;
    private readonly HashSet<int> _selectedAccessorySlots;
    private readonly Dictionary<FieldInfo, object> _savedValues =
        new Dictionary<FieldInfo, object>();

    private Type _coordinateLoadType;
    private FieldInfo _totalAmountField;
    private FieldInfo _queueField;
    private FieldInfo _finishedCountField;
    private FieldInfo _temporaryCharacterField;
    private MethodInfo _forceEndMethod;
    private GameObject _temporaryToggleRoot;
    private Exception _callbackError;
    private bool _started;
    private bool _targetMayHaveChanged;
    private int _startFrame;
    private bool _restored;
    private bool _repairAttempted;
    private string _nativeStatus;
    private OCIChar _nativeCharacter;
    private int _nativeStartFrame;
    private byte[] _coordinateBackupBytes;
    private Version _coordinateBackupVersion;
    private string _coordinateBackupName;
    private MethodInfo _getAllExtendedCoordinateData;
    private MethodInfo _setExtendedCoordinateData;
    private MethodInfo _serializeExtendedData;
    private MethodInfo _deserializeExtendedData;
    private Type _extendedCoordinateDictionaryType;
    private byte[] _extendedCoordinateBackupBytes;
    private bool _extendedCoordinateSnapshotCaptured;
    private bool _recoveryPending;
    private int _recoveryStartFrame;
    private float _recoveryDeadline;
    private bool _recoveryTimeoutReported;
    private string _recoveryFailureStatus;
    private string _terminalRecoveryStatus;
    private bool _strictPreserveAccessories;
    private byte[] _accessoryBackupBytes;
    private readonly bool _verifySelectedAccessories;
    private int _accessoryOccupiedCountBackup;
    private readonly bool _isUndoOperation;
    private readonly VRCoordinateReplaceMode _replaceMode;
    private readonly int _skippedDuplicateAccessories;
    private bool _coordinateControllerReloadPending;
    private int _completionObservedFrame = -1;
    private byte[][] _accessorySlotFingerprintsBackup;
    private VRTrackedAccessorySnapshot _trackedAccessoryBackup;
    private VRTrackedAccessorySnapshot _trackingSnapshotToRestore;
    private byte[] _runtimeClothesStateBackup;
    private bool[] _runtimeShowAccessoryBackup;
    private byte _runtimeShoesTypeBackup;
    private VRCoordinateUndoState _runtimeStateToRestore;
    private Action _nativeSuccessAction;
    private bool _nativeSuccessActionInvoked;
    private readonly bool _boundCompatibleAccessoryReplace;
    private readonly string[] _targetBoundPluginGuidsToShield;
    private readonly ChaFileCoordinate _expectedBoundSourceCoordinate;
    private readonly string[] _boundVerificationPluginGuids;
    private bool _targetBoundDataShielded;
    private bool _targetBoundDataRestored;

    public VRCoordinateLoadSession(
        Assembly assembly,
        OCIChar character,
        string path,
        string coordinateName,
        int accessoryCount,
        string[] clothesToggleNames,
        IEnumerable<int> selectedAccessorySlots,
        bool strictPreserveAccessories,
        bool verifySelectedAccessories,
        VRCoordinateReplaceMode replaceMode,
        int skippedDuplicateAccessories = 0,
        bool boundCompatibleAccessoryReplace = false,
        IEnumerable<string> targetBoundPluginGuidsToShield = null,
        ChaFileCoordinate expectedBoundSourceCoordinate = null,
        IEnumerable<string> boundVerificationPluginGuids = null)
    {
        _assembly = assembly;
        _character = character;
        _path = path;
        _coordinateName = coordinateName;
        _accessoryCount = accessoryCount;
        _clothesToggleNames = clothesToggleNames;
        _selectedAccessorySlots = new HashSet<int>(selectedAccessorySlots ?? Enumerable.Empty<int>());
        _strictPreserveAccessories = strictPreserveAccessories;
        _verifySelectedAccessories = verifySelectedAccessories;
        _replaceMode = replaceMode;
        _skippedDuplicateAccessories = skippedDuplicateAccessories;
        _boundCompatibleAccessoryReplace = boundCompatibleAccessoryReplace;
        _targetBoundPluginGuidsToShield = (targetBoundPluginGuidsToShield
            ?? Enumerable.Empty<string>())
            .Where(guid => !string.IsNullOrEmpty(guid))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        _expectedBoundSourceCoordinate = expectedBoundSourceCoordinate;
        _boundVerificationPluginGuids = (boundVerificationPluginGuids
            ?? Enumerable.Empty<string>())
            .Where(guid => !string.IsNullOrEmpty(guid))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private VRCoordinateLoadSession(
        OCIChar nativeCharacter,
        string completedStatus,
        bool strictPreserveAccessories,
        bool isUndoOperation)
    {
        _character = nativeCharacter;
        _nativeStatus = completedStatus;
        _strictPreserveAccessories = strictPreserveAccessories;
        _isUndoOperation = isUndoOperation;
        _replaceMode = VRCoordinateReplaceMode.ClothesOnly;
        CaptureTargetCoordinateSnapshot();
    }

    public static bool TryCreateNativeAndRun(
        OCIChar character,
        string status,
        string recoveryFailurePrefix,
        Action loadAction,
        bool strictPreserveAccessories,
        out VRCoordinateLoadSession session,
        out Exception error)
    {
        return TryCreateNativeAndRun(
            character,
            status,
            recoveryFailurePrefix,
            loadAction,
            strictPreserveAccessories,
            null,
            out session,
            out error);
    }

    public static bool TryCreateNativeAndRun(
        OCIChar character,
        string status,
        string recoveryFailurePrefix,
        Action loadAction,
        bool strictPreserveAccessories,
        Action successAction,
        out VRCoordinateLoadSession session,
        out Exception error)
    {
        session = null;
        error = null;
        VRCoordinateLoadSession candidate = null;
        try
        {
            candidate = new VRCoordinateLoadSession(
                character,
                status,
                strictPreserveAccessories,
                false);
            candidate._nativeSuccessAction = successAction;
            loadAction();
            candidate._coordinateControllerReloadPending = true;
            candidate._targetMayHaveChanged = true;
            candidate._nativeCharacter = character;
            candidate._nativeStartFrame = Time.frameCount;
            session = candidate;
            return true;
        }
        catch (Exception ex)
        {
            error = VRCoordinateCardService.Unwrap(ex);
            if (candidate != null)
            {
                candidate._callbackError = error;
                candidate._targetMayHaveChanged = true;
                if (candidate.BeginRecovery(recoveryFailurePrefix + error.Message))
                {
                    session = candidate;
                    error = null;
                    return true;
                }
                if (!string.IsNullOrEmpty(candidate._terminalRecoveryStatus))
                {
                    error = new InvalidOperationException(
                        candidate._terminalRecoveryStatus,
                        error);
                }
            }
            return false;
        }
    }

    public static bool TryCreateUndoAndRun(
        OCIChar character,
        VRCoordinateUndoState undoState,
        string completedStatus,
        out VRCoordinateLoadSession session,
        out Exception error)
    {
        session = null;
        error = null;
        VRCoordinateLoadSession candidate = null;
        try
        {
            candidate = new VRCoordinateLoadSession(
                character,
                completedStatus,
                false,
                true);
            candidate._trackingSnapshotToRestore = undoState.TrackedAccessories;
            candidate._runtimeStateToRestore = undoState;
            string applyError;
            if (!candidate.TryApplyCoordinateSnapshot(undoState, out applyError))
                throw new InvalidOperationException(applyError);

            candidate._targetMayHaveChanged = true;
            candidate._nativeCharacter = character;
            candidate._nativeStartFrame = Time.frameCount;
            session = candidate;
            return true;
        }
        catch (Exception ex)
        {
            error = VRCoordinateCardService.Unwrap(ex);
            if (candidate != null)
            {
                candidate._callbackError = error;
                candidate._targetMayHaveChanged = true;
                if (candidate.BeginRecovery("撤销换装失败：" + error.Message))
                {
                    session = candidate;
                    error = null;
                    return true;
                }
                if (!string.IsNullOrEmpty(candidate._terminalRecoveryStatus))
                {
                    error = new InvalidOperationException(
                        candidate._terminalRecoveryStatus,
                        error);
                }
            }
            return false;
        }
    }

    internal VRCoordinateUndoState CreateUndoSnapshot()
    {
        if (_coordinateBackupBytes == null
            || _coordinateBackupVersion == null
            || _character == null)
        {
            return null;
        }

        return new VRCoordinateUndoState
        {
            Character = _character,
            CoordinateBytes = _coordinateBackupBytes,
            CoordinateVersion = _coordinateBackupVersion,
            CoordinateName = _coordinateBackupName,
            GetAllExtendedCoordinateData = _getAllExtendedCoordinateData,
            SetExtendedCoordinateData = _setExtendedCoordinateData,
            DeserializeExtendedData = _deserializeExtendedData,
            ExtendedCoordinateDictionaryType = _extendedCoordinateDictionaryType,
            ExtendedCoordinateBytes = _extendedCoordinateBackupBytes,
            HasExtendedCoordinateData = _extendedCoordinateSnapshotCaptured,
            TrackedAccessories = _trackedAccessoryBackup,
            ClothesState = _runtimeClothesStateBackup != null
                ? (byte[])_runtimeClothesStateBackup.Clone()
                : null,
            ShowAccessory = _runtimeShowAccessoryBackup != null
                ? (bool[])_runtimeShowAccessoryBackup.Clone()
                : null,
            ShoesType = _runtimeShoesTypeBackup
        };
    }

    private bool AccessoriesMatchBackup()
    {
        try
        {
            if (_accessoryBackupBytes == null
                || _character == null
                || _character.charInfo == null
                || _character.charInfo.nowCoordinate?.accessory == null)
            {
                return false;
            }

            byte[] current = SerializeAccessory(
                _character.charInfo.nowCoordinate.accessory);
            return current != null && current.SequenceEqual(_accessoryBackupBytes);
        }
        catch (Exception ex)
        {
            VRLog.Warn("Could not verify clothes-only accessory preservation: "
                + VRCoordinateCardService.Unwrap(ex).Message);
            return false;
        }
    }

    private bool SelectedAccessoriesWereAdded()
    {
        if (!_verifySelectedAccessories || _selectedAccessorySlots.Count == 0)
            return true;
        return CountOccupiedAccessories(_character?.charInfo?.nowCoordinate?.accessory)
            >= _accessoryOccupiedCountBackup + _selectedAccessorySlots.Count;
    }

    private void ShieldTargetBoundPluginData()
    {
        if (_targetBoundPluginGuidsToShield.Length == 0)
            return;
        if (!_extendedCoordinateSnapshotCaptured
            || _setExtendedCoordinateData == null)
        {
            throw new InvalidOperationException(
                "无法安全暂存目标角色的饰品绑定数据");
        }

        ChaFileCoordinate target = _character?.charInfo?.nowCoordinate;
        if (target == null)
            throw new InvalidOperationException("目标角色服装数据已经失效");

        foreach (string pluginGuid in _targetBoundPluginGuidsToShield)
        {
            _setExtendedCoordinateData.Invoke(
                null,
                new object[] { target, pluginGuid, null });
        }
        _targetBoundDataShielded = true;
        _targetBoundDataRestored = false;
    }

    private void RestoreShieldedTargetBoundPluginData()
    {
        if (!_targetBoundDataShielded || _targetBoundDataRestored)
            return;
        if (!_extendedCoordinateSnapshotCaptured
            || _deserializeExtendedData == null
            || _extendedCoordinateDictionaryType == null
            || _extendedCoordinateBackupBytes == null
            || _setExtendedCoordinateData == null)
        {
            throw new InvalidOperationException(
                "无法恢复目标角色的饰品绑定数据快照");
        }

        IDictionary restoredExtendedData = _deserializeExtendedData
            .MakeGenericMethod(_extendedCoordinateDictionaryType)
            .Invoke(null, new object[] { _extendedCoordinateBackupBytes }) as IDictionary;
        if (restoredExtendedData == null)
            throw new InvalidOperationException("目标角色饰品绑定数据快照为空");

        ChaFileCoordinate target = _character?.charInfo?.nowCoordinate;
        if (target == null)
            throw new InvalidOperationException("目标角色服装数据已经失效");
        foreach (string pluginGuid in _targetBoundPluginGuidsToShield)
        {
            object value = restoredExtendedData.Contains(pluginGuid)
                ? restoredExtendedData[pluginGuid]
                : null;
            _setExtendedCoordinateData.Invoke(
                null,
                new[] { (object)target, pluginGuid, value });
        }

        _targetBoundDataRestored = true;
        _targetBoundDataShielded = false;
        _coordinateControllerReloadPending = true;
    }

    private bool BoundCompatibleResultMatchesSource(out string error)
    {
        error = null;
        if (!_boundCompatibleAccessoryReplace)
            return true;
        if (_expectedBoundSourceCoordinate == null
            || _character == null
            || _character.charInfo == null)
        {
            error = "原槽换装校验数据不可用";
            return false;
        }

        ChaFileAccessory.PartsInfo[] sourceParts =
            _expectedBoundSourceCoordinate.accessory?.parts ?? new ChaFileAccessory.PartsInfo[0];
        ChaFileAccessory.PartsInfo[] targetParts =
            _character.charInfo.nowCoordinate?.accessory?.parts ?? new ChaFileAccessory.PartsInfo[0];
        int count = Math.Max(sourceParts.Length, targetParts.Length);
        for (int slot = 0; slot < count; slot++)
        {
            ChaFileAccessory.PartsInfo source = slot < sourceParts.Length
                ? sourceParts[slot]
                : null;
            ChaFileAccessory.PartsInfo target = slot < targetParts.Length
                ? targetParts[slot]
                : null;
            bool sourceOccupied = VRCoordinateAccessoryTracker.IsOccupied(source);
            bool targetOccupied = VRCoordinateAccessoryTracker.IsOccupied(target);
            if (sourceOccupied != targetOccupied)
            {
                error = "饰品槽 " + (slot + 1) + " 的占用状态与源卡不一致";
                return false;
            }
            if (sourceOccupied
                && !VRCoordinateAccessoryTracker.FingerprintsEqual(
                    VRCoordinateAccessoryTracker.CreateFingerprint(source),
                    VRCoordinateAccessoryTracker.CreateFingerprint(target)))
            {
                error = "饰品槽 " + (slot + 1) + " 未按原槽完整复制";
                return false;
            }
        }

        if (_boundVerificationPluginGuids.Length == 0)
            return true;
        Type extendedSaveType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly =>
            {
                try
                {
                    return assembly.GetType(
                        "ExtensibleSaveFormat.ExtendedSave",
                        throwOnError: false);
                }
                catch (Exception)
                {
                    return null;
                }
            })
            .FirstOrDefault(type => type != null);
        MethodInfo getExtendedData =
            VRCoordinateCardService.FindExtendedDataReader(extendedSaveType);
        if (getExtendedData == null || _serializeExtendedData == null)
        {
            error = "无法校验原槽换装的绑定数据";
            return false;
        }

        foreach (string pluginGuid in _boundVerificationPluginGuids)
        {
            object source = getExtendedData.Invoke(
                null,
                new object[] { _expectedBoundSourceCoordinate, pluginGuid });
            object target = getExtendedData.Invoke(
                null,
                new object[] { _character.charInfo.nowCoordinate, pluginGuid });
            if (source == null || target == null)
            {
                if (!ReferenceEquals(source, target))
                {
                    error = "绑定数据未按源卡同步（" + pluginGuid + "）";
                    return false;
                }
                continue;
            }

            byte[] sourceBytes = (byte[])_serializeExtendedData
                .MakeGenericMethod(source.GetType())
                .Invoke(null, new[] { source });
            byte[] targetBytes = (byte[])_serializeExtendedData
                .MakeGenericMethod(target.GetType())
                .Invoke(null, new[] { target });
            if (sourceBytes == null
                || targetBytes == null
                || !sourceBytes.SequenceEqual(targetBytes))
            {
                error = "绑定数据校验失败（" + pluginGuid + "）";
                return false;
            }
        }
        return true;
    }

    private static int CountOccupiedAccessories(ChaFileAccessory accessory)
    {
        ChaFileAccessory.PartsInfo[] parts = accessory?.parts;
        if (parts == null)
            return 0;

        int count = 0;
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] != null && parts[i].type != 120)
                count++;
        }
        return count;
    }

    private bool TryCompleteCoordinateControllerReload(out string error)
    {
        error = null;
        if (!_coordinateControllerReloadPending)
            return true;
        if (!VRCoordinateCardService.TryInvokeCharacterApiCoordinateLifecycle(
            _character?.charInfo,
            _character?.charInfo?.nowCoordinate,
            false,
            out error))
        {
            return false;
        }

        _coordinateControllerReloadPending = false;
        return true;
    }

    private bool TryApplyPendingRuntimeState(out string error)
    {
        error = null;
        VRCoordinateUndoState snapshot = _runtimeStateToRestore;
        if (snapshot == null)
            return true;
        if (_character == null || _character.charInfo == null)
        {
            error = "目标角色已经失效";
            return false;
        }

        try
        {
            byte[] clothesState = snapshot.ClothesState ?? new byte[0];
            for (int part = 0; part < clothesState.Length; part++)
                _character.SetClothesState(part, clothesState[part]);
            _character.SetShoesType(snapshot.ShoesType);

            bool[] showAccessory = snapshot.ShowAccessory ?? new bool[0];
            bool[] currentVisibility = _character.charFileStatus?.showAccessory;
            int count = currentVisibility == null
                ? 0
                : Math.Min(showAccessory.Length, currentVisibility.Length);
            for (int slot = 0; slot < count; slot++)
                _character.ShowAccessory(slot, showAccessory[slot]);

            _runtimeStateToRestore = null;
            return true;
        }
        catch (Exception ex)
        {
            error = VRCoordinateCardService.Unwrap(ex).Message;
            return false;
        }
    }

    private static byte[] SerializeAccessory(ChaFileAccessory accessory)
    {
        ChaFileCoordinate container = new ChaFileCoordinate
        {
            accessory = accessory
        };
        return container.SaveBytes();
    }

    public bool TargetMayHaveChanged => _targetMayHaveChanged;

    public bool CanAbortSafely => _nativeCharacter == null
        && !_recoveryPending
        && !HasCoordinateLoadOptionFinished();

    public bool RequiresBackgroundMonitoring => _nativeCharacter != null
        || _recoveryPending
        || HasCoordinateLoadOptionFinished();

    public string TerminalRecoveryStatus => _terminalRecoveryStatus;

    public bool Begin(out string status)
    {
        try
        {
            Type patchesType = RequireType("KK_CoordinateLoadOption.Patches");
            _coordinateLoadType = RequireType("KK_CoordinateLoadOption.CoordinateLoad");
            _totalAmountField = RequireField(_coordinateLoadType, "totalAmount");
            _queueField = RequireField(_coordinateLoadType, "oCICharQueue");
            _finishedCountField = RequireField(_coordinateLoadType, "finishedCount");
            _temporaryCharacterField = RequireField(_coordinateLoadType, "tmpChaCtrl");
            _forceEndMethod = _coordinateLoadType.GetMethod("End", StaticFields);

            MethodInfo changeCoordinate = _coordinateLoadType.GetMethod(
                "ChangeCoordinate",
                StaticFields);
            MethodInfo makeTemporaryCharacter = _coordinateLoadType.GetMethod(
                "MakeTmpChara",
                StaticFields);
            if (changeCoordinate == null || makeTemporaryCharacter == null)
                throw new MissingMethodException("Coordinate Load Option load methods were not found");
            if (Camera.main == null)
                throw new InvalidOperationException("工作室主相机尚未就绪");

            CaptureTargetCoordinateSnapshot();

            int activeTotal = Convert.ToInt32(_totalAmountField.GetValue(null));
            object activeQueue = _queueField.GetValue(null);
            int activeQueueCount = activeQueue is ICollection collection ? collection.Count : 0;
            if (activeTotal > 0
                || activeQueueCount > 0
                || _temporaryCharacterField.GetValue(null) != null)
            {
                status = "Coordinate Load Option 正在处理其他换装，请稍后";
                return false;
            }

            if (_targetBoundPluginGuidsToShield.Length > 0)
                ShieldTargetBoundPluginData();

            Toggle[] clothesToggles = CreateToggles(_clothesToggleNames);
            clothesToggles[clothesToggles.Length - 1].isOn = _selectedAccessorySlots.Count > 0;
            string[] accessoryNames = new string[_accessoryCount];
            for (int i = 0; i < accessoryNames.Length; i++)
                accessoryNames[i] = "accessories";
            Toggle[] accessoryToggles = CreateToggles(accessoryNames);
            for (int i = 0; i < accessoryToggles.Length; i++)
                accessoryToggles[i].isOn = _selectedAccessorySlots.Contains(i);

            SetSaved(RequireField(patchesType, "coordinatePath"), _path);
            SetSaved(RequireField(patchesType, "tgls"), clothesToggles);
            SetSaved(RequireField(patchesType, "tgls2"), accessoryToggles);
            SetSaved(
                RequireField(patchesType, "lockHairAcc"),
                !_boundCompatibleAccessoryReplace);
            // Normal loads append selected source slots into empty target slots. The
            // separately confirmed bound-compatible mode deliberately uses CLO's
            // original-slot replacement so slot-indexed plugin payloads stay valid.
            SetSaved(
                RequireField(patchesType, "addAccModeFlag"),
                !_boundCompatibleAccessoryReplace);
            SetSaved(
                RequireField(patchesType, "boundAcc"),
                _boundCompatibleAccessoryReplace);
            SetSaved(
                RequireField(patchesType, "charaOverlay"),
                new[] { false, false, false });
            SetSaved(RequireField(patchesType, "readABMX"), false);

            SetSaved(_totalAmountField, 1);
            SetSaved(_queueField, new Queue<OCIChar>(new[] { _character }));
            SetSaved(_finishedCountField, 0);

            Action<object> callback = target =>
            {
                try
                {
                    changeCoordinate.Invoke(null, new[] { target });
                }
                catch (Exception ex)
                {
                    _callbackError = VRCoordinateCardService.Unwrap(ex);
                    TryForceEnd();
                }
            };

            _targetMayHaveChanged = true;
            _started = true;
            _startFrame = Time.frameCount;
            makeTemporaryCharacter.Invoke(null, new object[] { callback });
            status = null;
            return true;
        }
        catch (Exception ex)
        {
            _callbackError = VRCoordinateCardService.Unwrap(ex);
            status = "Coordinate Load Option 接口不兼容：" + _callbackError.Message;
            if (_targetMayHaveChanged)
            {
                if (BeginRecovery(status))
                {
                    status = "Coordinate Load Option 启动失败，正在恢复角色状态";
                    return true;
                }
                if (!string.IsNullOrEmpty(_terminalRecoveryStatus))
                    status = _terminalRecoveryStatus;
            }
            Restore();
            return false;
        }
    }

    public bool TryGetCompletion(out bool success, out string status)
    {
        if (_recoveryPending)
            return TryGetRecoveryCompletion(out success, out status);

        if (_nativeCharacter != null)
        {
            if (Time.frameCount <= _nativeStartFrame
                || (_nativeCharacter.charInfo != null && !_nativeCharacter.charInfo.loadEnd))
            {
                success = false;
                status = null;
                return false;
            }

            success = _nativeCharacter.charInfo != null;
            if (success && _coordinateControllerReloadPending)
            {
                string controllerError;
                if (!TryCompleteCoordinateControllerReload(out controllerError))
                {
                    _nativeCharacter = null;
                    string failureStatus = (_isUndoOperation
                        ? "撤销换装失败："
                        : "饰品操作失败：")
                        + "插件扩展状态恢复失败："
                        + controllerError;
                    if (BeginRecovery(failureStatus))
                    {
                        status = null;
                        success = false;
                        return false;
                    }

                    success = false;
                    status = _terminalRecoveryStatus ?? failureStatus;
                    return true;
                }
            }
            if (success)
            {
                string runtimeError;
                if (!TryApplyPendingRuntimeState(out runtimeError))
                {
                    _nativeCharacter = null;
                    string failureStatus = (_isUndoOperation
                        ? "撤销换装失败："
                        : "服装操作失败：")
                        + "穿着状态恢复失败："
                        + runtimeError;
                    if (BeginRecovery(failureStatus))
                    {
                        status = null;
                        success = false;
                        return false;
                    }

                    success = false;
                    status = _terminalRecoveryStatus ?? failureStatus;
                    return true;
                }
            }
            if (success && _strictPreserveAccessories && !AccessoriesMatchBackup())
            {
                _nativeCharacter = null;
                string failureStatus = "只换衣服被中止：检测到饰品数据发生变化";
                if (BeginRecovery(failureStatus))
                {
                    status = null;
                    success = false;
                    return false;
                }

                success = false;
                status = _terminalRecoveryStatus ?? failureStatus;
                return true;
            }

            status = success
                ? _nativeStatus
                : "完整替换失败：目标角色已经失效";
            if (success)
            {
                if (_isUndoOperation)
                {
                    VRCoordinateAccessoryTracker.Restore(
                        _character,
                        _trackingSnapshotToRestore);
                    VRCoordinateCardService.ConsumeUndoSnapshot(_character);
                }
                else
                {
                    VRCoordinateCardService.StoreUndoSnapshot(this);
                    if (!_nativeSuccessActionInvoked && _nativeSuccessAction != null)
                    {
                        _nativeSuccessActionInvoked = true;
                        try
                        {
                            _nativeSuccessAction();
                        }
                        catch (Exception ex)
                        {
                            VRLog.Error("Accessory-operation success bookkeeping failed: "
                                + VRCoordinateCardService.Unwrap(ex).Message);
                        }
                    }
                }
            }
            _nativeCharacter = null;
            Restore();
            return true;
        }

        if (!_started)
        {
            success = false;
            string failureStatus = _callbackError != null
                ? "服装卡替换失败：" + _callbackError.Message
                : "服装卡替换没有启动";
            if (_targetMayHaveChanged && BeginRecovery(failureStatus))
            {
                status = null;
                return false;
            }
            status = _terminalRecoveryStatus ?? failureStatus;
            Restore();
            return true;
        }

        try
        {
            int total = Convert.ToInt32(_totalAmountField.GetValue(null));
            object queue = _queueField.GetValue(null);
            int queueCount = queue is ICollection collection ? collection.Count : 0;
            object temporaryCharacter = _temporaryCharacterField.GetValue(null);
            if (_callbackError == null
                && (total > 0 || queueCount > 0 || temporaryCharacter != null))
            {
                success = false;
                status = null;
                return false;
            }

            if (_callbackError == null
                && (Time.frameCount <= _startFrame
                    || (_character != null
                        && _character.charInfo != null
                        && !_character.charInfo.loadEnd)))
            {
                success = false;
                status = null;
                return false;
            }

            if (_callbackError == null && (_character == null || _character.charInfo == null))
                _callbackError = new InvalidOperationException("目标角色已经失效");

            int finished = Convert.ToInt32(_finishedCountField.GetValue(null));
            success = _callbackError == null && finished >= 1;
            if (success)
            {
                if (_completionObservedFrame < 0)
                {
                    _completionObservedFrame = Time.frameCount;
                    success = false;
                    status = null;
                    return false;
                }
                if (Time.frameCount <= _completionObservedFrame + 1)
                {
                    success = false;
                    status = null;
                    return false;
                }

                if (_targetBoundDataShielded)
                {
                    try
                    {
                        RestoreShieldedTargetBoundPluginData();
                    }
                    catch (Exception ex)
                    {
                        string shieldFailureStatus =
                            "服装恢复失败：无法还原新角色的饰品绑定数据："
                            + VRCoordinateCardService.Unwrap(ex).Message;
                        if (BeginRecovery(shieldFailureStatus))
                        {
                            status = null;
                            success = false;
                            return false;
                        }
                        success = false;
                        status = _terminalRecoveryStatus ?? shieldFailureStatus;
                        return true;
                    }
                }
                if (_coordinateControllerReloadPending)
                {
                    string controllerError;
                    if (!TryCompleteCoordinateControllerReload(out controllerError))
                    {
                        string controllerFailureStatus =
                            "服装恢复失败：插件扩展状态重新载入失败："
                            + controllerError;
                        if (BeginRecovery(controllerFailureStatus))
                        {
                            status = null;
                            success = false;
                            return false;
                        }
                        success = false;
                        status = _terminalRecoveryStatus ?? controllerFailureStatus;
                        return true;
                    }
                }

                string boundVerificationError;
                if (!BoundCompatibleResultMatchesSource(out boundVerificationError))
                {
                    string boundFailureStatus =
                        "原槽兼容换装未完成：" + boundVerificationError;
                    if (BeginRecovery(boundFailureStatus))
                    {
                        status = null;
                        success = false;
                        return false;
                    }
                    success = false;
                    status = _terminalRecoveryStatus ?? boundFailureStatus;
                    return true;
                }

                if (_strictPreserveAccessories && !AccessoriesMatchBackup())
                {
                    string accessoryFailureStatus = _replaceMode == VRCoordinateReplaceMode.Full
                        ? "整套换装被中止：检测到现有饰品数据发生变化"
                        : "只换衣服被中止：检测到饰品数据发生变化";
                    if (BeginRecovery(accessoryFailureStatus))
                    {
                        status = null;
                        success = false;
                        return false;
                    }

                    success = false;
                    status = _terminalRecoveryStatus ?? accessoryFailureStatus;
                    return true;
                }

                if (!SelectedAccessoriesWereAdded())
                {
                    string accessoryFailureStatus = _replaceMode == VRCoordinateReplaceMode.Full
                        ? "整套换装未完成：目标角色没有足够的空饰品槽"
                        : "自选饰品未完成：目标角色没有足够的空饰品槽";
                    if (BeginRecovery(accessoryFailureStatus))
                    {
                        status = null;
                        success = false;
                        return false;
                    }

                    success = false;
                    status = _terminalRecoveryStatus ?? accessoryFailureStatus;
                    return true;
                }

                if (!_boundCompatibleAccessoryReplace
                    && _selectedAccessorySlots.Count > 0)
                {
                    VRCoordinateAccessoryTracker.RegisterNewlyAppendedSlots(
                        _character,
                        _accessorySlotFingerprintsBackup);
                }

                status = _replaceMode == VRCoordinateReplaceMode.BoundCompatibleFull
                    ? "已按原槽完整替换衣服、饰品和绑定数据：" + _coordinateName
                    : _replaceMode == VRCoordinateReplaceMode.Full
                    ? _selectedAccessorySlots.Count == 0
                        ? _skippedDuplicateAccessories > 0
                            ? "已完整替换衣服；现有饰品保持不变：" + _coordinateName
                            : "已完整替换衣服；源卡没有饰品，现有饰品保持不变：" + _coordinateName
                        : "已完整替换衣服，并把全部源饰品追加到空槽；现有饰品保持不变："
                            + _coordinateName
                    : _selectedAccessorySlots.Count == 0
                        ? "已只替换衣服，现有饰品保持不变：" + _coordinateName
                        : "已换衣服并把所选饰品追加到空槽：" + _coordinateName;
                if (_skippedDuplicateAccessories > 0)
                {
                    status += "；已跳过重复饰品 "
                        + _skippedDuplicateAccessories
                        + " 件";
                }
                VRCoordinateCardService.StoreUndoSnapshot(this);
                if (_boundCompatibleAccessoryReplace)
                    VRCoordinateAccessoryTracker.Clear(_character);
                Restore();
                return true;
            }

            string failureStatus = "服装卡替换失败："
                + (_callbackError != null
                    ? _callbackError.Message
                    : "Coordinate Load Option 未完成加载");
            if (BeginRecovery(failureStatus))
            {
                status = null;
                return false;
            }
            status = _terminalRecoveryStatus ?? failureStatus;
            return true;
        }
        catch (Exception ex)
        {
            _callbackError = VRCoordinateCardService.Unwrap(ex);
            success = false;
            string failureStatus = "服装卡替换异常：" + _callbackError.Message;
            if (BeginRecovery(failureStatus))
            {
                status = null;
                return false;
            }
            status = _terminalRecoveryStatus ?? failureStatus;
            return true;
        }
    }

    public void Abort()
    {
        if (_nativeCharacter != null)
            return;
        if (_restored || _recoveryPending)
            return;
        if (!_targetMayHaveChanged)
        {
            Restore();
            return;
        }
        if (HasCompletedSuccessfully())
            Restore();
        else
            BeginRecovery("服装卡替换已取消");
    }

    private bool TryGetRecoveryCompletion(out bool success, out string status)
    {
        success = false;
        if (_character == null || _character.charInfo == null)
        {
            _recoveryPending = false;
            status = _recoveryFailureStatus + "；目标角色已经失效";
            Restore();
            return true;
        }

        if (Time.frameCount > _recoveryStartFrame && _character.charInfo.loadEnd)
        {
            _recoveryPending = false;
            string controllerError;
            if (!TryCompleteCoordinateControllerReload(out controllerError))
            {
                status = _recoveryFailureStatus
                    + "；已恢复基础角色状态，但插件扩展状态恢复失败："
                    + controllerError;
                Restore();
                return true;
            }
            string runtimeError;
            if (!TryApplyPendingRuntimeState(out runtimeError))
            {
                status = _recoveryFailureStatus
                    + "；已恢复服装和插件扩展状态，但穿着状态恢复失败："
                    + runtimeError;
                Restore();
                return true;
            }
            VRCoordinateAccessoryTracker.Restore(
                _character,
                _trackedAccessoryBackup);
            if (!_repairAttempted)
            {
                _repairAttempted = true;
                VRCoordinateCardService.RepairStudioCharacterDynamics(_character);
            }
            status = _recoveryFailureStatus + "；已恢复角色状态";
            Restore();
            return true;
        }

        if (!_recoveryTimeoutReported && Time.realtimeSinceStartup >= _recoveryDeadline)
        {
            _recoveryTimeoutReported = true;
            status = _recoveryFailureStatus
                + "；角色恢复读取超时，已解除界面等待；请重载场景后再换装";
            return true;
        }

        status = null;
        return false;
    }

    private bool HasCompletedSuccessfully()
    {
        return HasCoordinateLoadOptionFinished()
            && Time.frameCount > _startFrame
            && _character != null
            && _character.charInfo != null
            && _character.charInfo.loadEnd;
    }

    private bool HasCoordinateLoadOptionFinished()
    {
        try
        {
            if (!_started || _restored || _recoveryPending || _nativeCharacter != null)
                return false;
            if (_finishedCountField == null
                || _totalAmountField == null
                || _queueField == null
                || _temporaryCharacterField == null)
            {
                return false;
            }

            int finished = Convert.ToInt32(_finishedCountField.GetValue(null));
            int total = Convert.ToInt32(_totalAmountField.GetValue(null));
            object queue = _queueField.GetValue(null);
            int queueCount = queue is ICollection collection ? collection.Count : 0;
            return finished >= 1
                && total == 0
                && queueCount == 0
                && _temporaryCharacterField.GetValue(null) == null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool BeginRecovery(string failureStatus)
    {
        if (_restored)
            return false;
        if (_recoveryPending)
            return true;

        _recoveryFailureStatus = string.IsNullOrEmpty(failureStatus)
            ? "服装卡替换失败"
            : failureStatus;
        TryForceEnd();
        TryCleanCoordinateLoadOptionOrphans();
        TryResetHairAccessoryUpdateBlock();
        RestoreCoordinateLoadOptionState();
        string restoreError;
        if (!TryRestoreTargetCoordinateSnapshot(out restoreError))
        {
            _terminalRecoveryStatus = _recoveryFailureStatus
                + "；自动恢复角色状态失败："
                + restoreError;
            if (!_repairAttempted)
            {
                _repairAttempted = true;
                VRCoordinateCardService.RepairStudioCharacterDynamics(_character);
            }
            Restore();
            return false;
        }

        _recoveryPending = true;
        _recoveryStartFrame = Time.frameCount;
        _recoveryDeadline = Time.realtimeSinceStartup + 30f;
        _recoveryTimeoutReported = false;
        return true;
    }

    private void CaptureTargetCoordinateSnapshot()
    {
        ChaFileCoordinate coordinate = _character?.charInfo?.nowCoordinate;
        if (coordinate == null)
            throw new InvalidOperationException("无法备份目标角色当前服装");

        string controllerSyncError;
        if (!VRCoordinateCardService.TryInvokeCharacterApiCoordinateLifecycle(
            _character.charInfo,
            coordinate,
            true,
            out controllerSyncError))
        {
            throw new InvalidOperationException(
                "无法同步目标角色的插件扩展状态：" + controllerSyncError);
        }

        _coordinateBackupName = coordinate.coordinateName;
        _accessoryBackupBytes = SerializeAccessory(coordinate.accessory);
        _accessoryOccupiedCountBackup = CountOccupiedAccessories(coordinate.accessory);
        _accessorySlotFingerprintsBackup =
            VRCoordinateAccessoryTracker.CaptureSlotFingerprints(coordinate.accessory);
        _trackedAccessoryBackup = VRCoordinateAccessoryTracker.Capture(_character);
        _runtimeClothesStateBackup = _character.charFileStatus?.clothesState != null
            ? (byte[])_character.charFileStatus.clothesState.Clone()
            : null;
        _runtimeShowAccessoryBackup = _character.charFileStatus?.showAccessory != null
            ? (bool[])_character.charFileStatus.showAccessory.Clone()
            : null;
        _runtimeShoesTypeBackup = _character.charFileStatus?.shoesType ?? (byte)0;

        Type extendedSaveType = null;
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                extendedSaveType = assembly.GetType(
                    "ExtensibleSaveFormat.ExtendedSave",
                    throwOnError: false);
                if (extendedSaveType != null)
                    break;
            }
            catch (Exception)
            {
                // Continue searching the loaded assemblies.
            }
        }
        if (extendedSaveType != null)
        {
            foreach (MethodInfo method in extendedSaveType.GetMethods(StaticFields))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 1
                    && parameters[0].ParameterType == typeof(ChaFileCoordinate)
                    && string.Equals(method.Name, "GetAllExtendedData", StringComparison.Ordinal))
                {
                    _getAllExtendedCoordinateData = method;
                }
                else if (parameters.Length == 3
                    && parameters[0].ParameterType == typeof(ChaFileCoordinate)
                    && parameters[1].ParameterType == typeof(string)
                    && string.Equals(method.Name, "SetExtendedDataById", StringComparison.Ordinal))
                {
                    _setExtendedCoordinateData = method;
                }
                else if (method.IsGenericMethodDefinition
                    && method.GetGenericArguments().Length == 1
                    && parameters.Length == 1
                    && string.Equals(method.Name, "MessagePackSerialize", StringComparison.Ordinal))
                {
                    _serializeExtendedData = method;
                }
                else if (method.IsGenericMethodDefinition
                    && method.GetGenericArguments().Length == 1
                    && parameters.Length == 1
                    && parameters[0].ParameterType == typeof(byte[])
                    && string.Equals(method.Name, "MessagePackDeserialize", StringComparison.Ordinal))
                {
                    _deserializeExtendedData = method;
                }
            }
            if (_getAllExtendedCoordinateData == null
                || _setExtendedCoordinateData == null
                || _serializeExtendedData == null
                || _deserializeExtendedData == null)
            {
                throw new InvalidOperationException(
                    "ExtensibleSaveFormat 服装快照接口不兼容");
            }

            // Deep-copy the data already attached to the coordinate. Invoking
            // ExtendedSave's lifecycle events manually is unsafe in Studio because
            // plugin handlers expect their own save/load context to be active.
            _extendedCoordinateDictionaryType = _getAllExtendedCoordinateData.ReturnType;
            object extendedData = _getAllExtendedCoordinateData.Invoke(
                null,
                new object[] { coordinate })
                ?? Activator.CreateInstance(_extendedCoordinateDictionaryType);
            _extendedCoordinateBackupBytes = (byte[])_serializeExtendedData
                .MakeGenericMethod(_extendedCoordinateDictionaryType)
                .Invoke(null, new[] { extendedData });
            if (_extendedCoordinateBackupBytes == null)
                throw new InvalidOperationException("服装扩展数据快照为空");
            _extendedCoordinateSnapshotCaptured = true;
        }
        else
        {
            VRLog.Warn(
                "ExtensibleSaveFormat is unavailable; interrupted outfit recovery will restore base coordinate data only.");
        }

        _coordinateBackupBytes = coordinate.SaveBytes();
        _coordinateBackupVersion = new Version(
            ChaFileDefine.ChaFileCoordinateVersion.ToString());
        if (_coordinateBackupBytes == null || _coordinateBackupBytes.Length == 0)
            throw new InvalidOperationException("目标角色当前服装备份为空");
    }

    private bool TryRestoreTargetCoordinateSnapshot(out string error)
    {
        VRCoordinateUndoState snapshot = CreateUndoSnapshot();
        _runtimeStateToRestore = snapshot;
        return TryApplyCoordinateSnapshot(snapshot, out error);
    }

    private bool TryApplyCoordinateSnapshot(
        VRCoordinateUndoState snapshot,
        out string error)
    {
        error = null;
        if (snapshot == null
            || snapshot.CoordinateBytes == null
            || snapshot.CoordinateVersion == null
            || _character == null
            || _character.charInfo == null)
        {
            error = "目标角色服装快照不可用";
            return false;
        }

        try
        {
            ChaFileCoordinate restored = new ChaFileCoordinate();
            if (!restored.LoadBytes(snapshot.CoordinateBytes, snapshot.CoordinateVersion))
                throw new InvalidOperationException("无法读取换装前的角色服装备份");

            ChaFileCoordinate target = _character.charInfo.nowCoordinate;
            target.clothes = restored.clothes;
            target.accessory = restored.accessory;
            target.enableMakeup = restored.enableMakeup;
            target.makeup = restored.makeup;
            target.coordinateName = snapshot.CoordinateName ?? string.Empty;

            if (snapshot.HasExtendedCoordinateData)
            {
                if (snapshot.GetAllExtendedCoordinateData == null
                    || snapshot.SetExtendedCoordinateData == null
                    || snapshot.DeserializeExtendedData == null
                    || snapshot.ExtendedCoordinateDictionaryType == null
                    || snapshot.ExtendedCoordinateBytes == null)
                {
                    throw new InvalidOperationException("服装扩展数据快照接口不可用");
                }

                IDictionary currentExtendedData = snapshot.GetAllExtendedCoordinateData.Invoke(
                    null,
                    new object[] { target }) as IDictionary;
                currentExtendedData?.Clear();
                IDictionary restoredExtendedData = snapshot.DeserializeExtendedData
                    .MakeGenericMethod(snapshot.ExtendedCoordinateDictionaryType)
                    .Invoke(null, new object[] { snapshot.ExtendedCoordinateBytes }) as IDictionary;
                if (restoredExtendedData == null)
                    throw new InvalidOperationException("无法读取换装前的服装扩展数据快照");
                foreach (DictionaryEntry saved in restoredExtendedData)
                {
                    if (saved.Key is string key)
                    {
                        snapshot.SetExtendedCoordinateData.Invoke(
                            null,
                            new[] { (object)target, key, saved.Value });
                    }
                }
                _targetBoundDataRestored = true;
                _targetBoundDataShielded = false;
            }

            ChaFileDefine.CoordinateType type =
                (ChaFileDefine.CoordinateType)_character.charInfo.fileStatus.coordinateType;
            if (!_character.charInfo.AssignCoordinate(type))
                throw new InvalidOperationException("工作室没有接受服装快照恢复请求");
            VRCoordinateCardService.TrySynchronizeMoreAccessoriesArrays(_character.charInfo);
            _character.SetCoordinateInfo(type, true);
            _coordinateControllerReloadPending = true;
            return true;
        }
        catch (Exception ex)
        {
            error = VRCoordinateCardService.Unwrap(ex).Message;
            VRLog.Error(
                "Could not restore the target coordinate after an interrupted outfit load: "
                + error);
            return false;
        }
    }

    private void TryCleanCoordinateLoadOptionOrphans()
    {
        if (_coordinateLoadType == null)
            return;

        try
        {
            FieldInfo temporaryField = _temporaryCharacterField
                ?? _coordinateLoadType.GetField("tmpChaCtrl", StaticFields);
            ChaControl temporaryCharacter = temporaryField?.GetValue(null) as ChaControl;
            if (temporaryCharacter != null)
            {
                try
                {
                    temporaryCharacter.StopAllCoroutines();
                    Singleton<Manager.Character>.Instance.DeleteChara(temporaryCharacter, false);
                }
                catch (Exception ex)
                {
                    VRLog.Warn(
                        "Unable to remove Coordinate Load Option's temporary character: "
                        + VRCoordinateCardService.Unwrap(ex).Message);
                }
            }
            temporaryField?.SetValue(null, null);

            TryClearSupportBackup("hairacc");
            TryClearSupportBackup("coboc");
            _coordinateLoadType.GetField("backupTmpCoordinate", StaticFields)?.SetValue(null, null);
            _coordinateLoadType.GetField("totalAmount", StaticFields)?.SetValue(null, 0);
            object queue = _coordinateLoadType.GetField("oCICharQueue", StaticFields)?.GetValue(null);
            (queue as IList)?.Clear();
            if (queue is ICollection && queue is not IList)
            {
                MethodInfo clear = queue.GetType().GetMethod("Clear", Type.EmptyTypes);
                clear?.Invoke(queue, null);
            }
        }
        catch (Exception ex)
        {
            VRLog.Warn(
                "Unable to clear interrupted Coordinate Load Option state: "
                + VRCoordinateCardService.Unwrap(ex).Message);
        }
    }

    private void TryClearSupportBackup(string fieldName)
    {
        try
        {
            FieldInfo field = _coordinateLoadType.GetField(fieldName, StaticFields);
            object support = field?.GetValue(null);
            if (support != null)
            {
                MethodInfo clearBackup = support.GetType().GetMethod(
                    "ClearBackup",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                clearBackup?.Invoke(support, null);
            }
            field?.SetValue(null, null);
        }
        catch (Exception ex)
        {
            VRLog.Warn(
                "Unable to clear Coordinate Load Option support state "
                + fieldName
                + ": "
                + VRCoordinateCardService.Unwrap(ex).Message);
        }
    }

    private void TryResetHairAccessoryUpdateBlock()
    {
        try
        {
            Type supportType = _assembly?.GetType(
                "KK_CoordinateLoadOption.HairAccessoryCustomizer_CCFCSupport",
                throwOnError: false);
            if (supportType == null)
                return;

            FieldInfo field = supportType.GetField("UpdateBlock", StaticFields);
            if (field != null)
            {
                field.SetValue(null, false);
                return;
            }

            PropertyInfo property = supportType.GetProperty("UpdateBlock", StaticFields);
            if (property != null && property.CanWrite)
                property.SetValue(null, false, null);
        }
        catch (Exception ex)
        {
            VRLog.Warn(
                "Unable to reset HairAccessoryCustomizer after an interrupted outfit load: "
                + VRCoordinateCardService.Unwrap(ex).Message);
        }
    }

    private Toggle[] CreateToggles(string[] names)
    {
        if (_temporaryToggleRoot == null)
        {
            _temporaryToggleRoot = new GameObject("KKVR_CoordinateLoadOptions");
            _temporaryToggleRoot.SetActive(false);
        }

        Toggle[] toggles = new Toggle[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            GameObject toggleObject = new GameObject(names[i]);
            toggleObject.transform.SetParent(_temporaryToggleRoot.transform, false);
            Toggle toggle = toggleObject.AddComponent<Toggle>();
            toggle.isOn = true;
            toggles[i] = toggle;
        }
        return toggles;
    }

    private void SetSaved(FieldInfo field, object value)
    {
        if (!_savedValues.ContainsKey(field))
            _savedValues.Add(field, field.GetValue(null));
        field.SetValue(null, value);
    }

    private Type RequireType(string fullName)
    {
        Type type = _assembly.GetType(fullName, throwOnError: false);
        if (type == null)
            throw new TypeLoadException(fullName);
        return type;
    }

    private static FieldInfo RequireField(Type type, string name)
    {
        FieldInfo field = type.GetField(name, StaticFields);
        if (field == null)
            throw new MissingFieldException(type.FullName, name);
        return field;
    }

    private void TryForceEnd()
    {
        try
        {
            if (_forceEndMethod != null
                && _temporaryCharacterField != null
                && _temporaryCharacterField.GetValue(null) != null)
            {
                _forceEndMethod.Invoke(null, new object[] { true });
            }
        }
        catch (Exception ex)
        {
            VRLog.Warn(
                "Unable to force-stop Coordinate Load Option: "
                + VRCoordinateCardService.Unwrap(ex).Message);
        }
    }

    private void Restore()
    {
        if (_restored)
            return;
        _restored = true;

        if (_targetBoundDataShielded)
        {
            try
            {
                RestoreShieldedTargetBoundPluginData();
            }
            catch (Exception ex)
            {
                VRLog.Error(
                    "Could not restore temporarily shielded accessory binding data: "
                    + VRCoordinateCardService.Unwrap(ex).Message);
            }
        }
        RestoreCoordinateLoadOptionState();
        _coordinateBackupBytes = null;
        _coordinateBackupVersion = null;
        _coordinateBackupName = null;
        _getAllExtendedCoordinateData = null;
        _setExtendedCoordinateData = null;
        _serializeExtendedData = null;
        _deserializeExtendedData = null;
        _extendedCoordinateDictionaryType = null;
        _extendedCoordinateBackupBytes = null;
        _extendedCoordinateSnapshotCaptured = false;
        _accessoryBackupBytes = null;
        _runtimeClothesStateBackup = null;
        _runtimeShowAccessoryBackup = null;
        _runtimeStateToRestore = null;
        _coordinateControllerReloadPending = false;
        _recoveryPending = false;
        VRCoordinateCardService.ReleaseSession(this);
    }

    private void RestoreCoordinateLoadOptionState()
    {

        foreach (KeyValuePair<FieldInfo, object> saved in _savedValues)
        {
            try
            {
                saved.Key.SetValue(null, saved.Value);
            }
            catch (Exception ex)
            {
                VRLog.Warn(
                    "Unable to restore Coordinate Load Option field "
                    + saved.Key.Name
                    + ": "
                    + VRCoordinateCardService.Unwrap(ex).Message);
            }
        }
        _savedValues.Clear();

        if (_temporaryToggleRoot != null)
        {
            UnityEngine.Object.Destroy(_temporaryToggleRoot);
            _temporaryToggleRoot = null;
        }
    }
}

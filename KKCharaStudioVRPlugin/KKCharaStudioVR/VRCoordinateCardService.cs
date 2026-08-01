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
    Full
}

internal sealed class VRCoordinateAccessorySlotInfo
{
    public int SlotIndex;
    public int Type;
    public int Id;
    public string DisplayName;
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
        session = null;
        try
        {
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
                    mode);
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
                        out session,
                        out status);
                }

                session = clothesOnlyCandidate;
                RegisterSession(session);
                if (string.IsNullOrEmpty(status))
                    status = mode == VRCoordinateReplaceMode.Full
                        ? "正在完整替换衣服；源卡没有饰品，现有饰品保持不变"
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

            string boundDataReason;
            if (!CanUseHairProtectionBridge(
                pluginAssembly,
                coordinate,
                character.charInfo.nowCoordinate,
                out boundDataReason))
            {
                VRLog.Warn(boundDataReason);
                if (selectedAccessorySlots == null)
                {
                    return TryBeginCompatibilityReplace(
                        character,
                        coordinate,
                        coordinateName,
                        boundDataReason,
                        out session,
                        out status);
                }

                status = mode == VRCoordinateReplaceMode.Full
                    ? "整套换装未执行：" + boundDataReason
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
                mode);
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
                    out session,
                    out status);
            }

            session = candidate;
            RegisterSession(session);
            if (string.IsNullOrEmpty(status))
                status = mode == VRCoordinateReplaceMode.Full
                    ? "正在完整替换衣服，并把全部源饰品追加到空槽"
                    : "正在换衣服并把所选饰品追加到空槽";
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
        try
        {
            // Fail-safe mode deliberately keeps the target coordinate's complete
            // accessory block. This prevents accessory-based hair from disappearing
            // when selective accessory loading cannot be proven safe.
            status = string.IsNullOrEmpty(fallbackReason)
                ? "已用兼容模式只替换衣服，并保留全部现有饰品：" + coordinateName
                : "为保护发饰，已只替换衣服并保留全部现有饰品：" + coordinateName;
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

    private static bool CanUseHairProtectionBridge(
        Assembly pluginAssembly,
        ChaFileCoordinate source,
        ChaFileCoordinate target,
        out string reason)
    {
        reason = null;
        try
        {
            Type pluginType = pluginAssembly.GetType(
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

            foreach (string pluginGuid in pluginGuids)
            {
                if (string.IsNullOrEmpty(pluginGuid))
                    continue;

                if (getExtendedData.Invoke(null, new object[] { source, pluginGuid }) != null)
                {
                    reason = "服装卡含饰品绑定数据（" + pluginGuid + "）";
                    return false;
                }
                if (target != null
                    && getExtendedData.Invoke(null, new object[] { target, pluginGuid }) != null)
                {
                    reason = "目标角色含饰品绑定数据（" + pluginGuid + "）";
                    return false;
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            reason = "检查饰品绑定数据失败：" + Unwrap(ex).Message;
            return false;
        }
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

    private static MethodInfo FindExtendedDataReader(Type extendedSaveType)
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
    private bool _coordinateControllerReloadPending;
    private int _completionObservedFrame = -1;

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
        VRCoordinateReplaceMode replaceMode)
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
            loadAction();
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
            HasExtendedCoordinateData = _extendedCoordinateSnapshotCaptured
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
            SetSaved(RequireField(patchesType, "lockHairAcc"), true);
            // Selected source slots are appended into empty target slots. This is the
            // only CLO mode that never overwrites an existing accessory (including
            // accessory-based hair that a card did not mark as a hair accessory).
            SetSaved(RequireField(patchesType, "addAccModeFlag"), true);
            SetSaved(RequireField(patchesType, "boundAcc"), false);
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
                    string failureStatus = "撤销换装失败：插件扩展状态恢复失败：" + controllerError;
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
                    VRCoordinateCardService.ConsumeUndoSnapshot(_character);
                else
                    VRCoordinateCardService.StoreUndoSnapshot(this);
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

                status = _replaceMode == VRCoordinateReplaceMode.Full
                    ? _selectedAccessorySlots.Count == 0
                        ? "已完整替换衣服；源卡没有饰品，现有饰品保持不变：" + _coordinateName
                        : "已完整替换衣服，并把全部源饰品追加到空槽；现有饰品保持不变："
                            + _coordinateName
                    : _selectedAccessorySlots.Count == 0
                        ? "已只替换衣服，现有饰品保持不变：" + _coordinateName
                        : "已换衣服并把所选饰品追加到空槽：" + _coordinateName;
                VRCoordinateCardService.StoreUndoSnapshot(this);
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
        return TryApplyCoordinateSnapshot(CreateUndoSnapshot(), out error);
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

            }

            ChaFileDefine.CoordinateType type =
                (ChaFileDefine.CoordinateType)_character.charInfo.fileStatus.coordinateType;
            if (!_character.charInfo.AssignCoordinate(type))
                throw new InvalidOperationException("工作室没有接受服装快照恢复请求");
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

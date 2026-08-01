using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Manager;
using Studio;
using UnityEngine;
using UnityEngine.UI;
using VRGIN.Core;

namespace KKCharaStudioVR;

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

    public static bool TryBeginReplace(
        int objectKey,
        OCIChar expectedCharacter,
        string path,
        bool protectHairAccessories,
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

            // A full replacement deliberately follows Studio's native coordinate path.
            // This is the only reliable way to clear trailing MoreAccessories slots and
            // apply every coordinate-level extension as a complete card.
            if (!protectHairAccessories)
            {
                return TryBeginNativeFullReplace(
                    character,
                    path,
                    coordinateName,
                    out session,
                    out status);
            }

            if (pluginAssembly == null)
            {
                return TryBeginCompatibilityReplace(
                    character,
                    coordinate,
                    coordinateName,
                    null,
                    out session,
                    out status);
            }

            string boundDataReason;
            if (!CanUseHairProtectionBridge(
                pluginAssembly,
                coordinate,
                character.charInfo.nowCoordinate,
                out boundDataReason))
            {
                VRLog.Warn(boundDataReason);
                return TryBeginCompatibilityReplace(
                    character,
                    coordinate,
                    coordinateName,
                    boundDataReason,
                    out session,
                    out status);
            }

            int accessoryCount = GetAccessoryCount(pluginAssembly, coordinate);
            VRCoordinateLoadSession candidate = new VRCoordinateLoadSession(
                pluginAssembly,
                character,
                path,
                coordinateName,
                accessoryCount,
                ClothesToggleNames);
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
                status = "正在通过 Coordinate Load Option 替换服装并保护发饰";
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
                    character.charInfo.nowCoordinate.clothes = coordinate.clothes;
                    character.charInfo.AssignCoordinate(
                        (ChaFileDefine.CoordinateType)character.charInfo.fileStatus.coordinateType);
                    character.SetCoordinateInfo(
                        (ChaFileDefine.CoordinateType)character.charInfo.fileStatus.coordinateType,
                        true);
                },
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

    private static bool TryBeginNativeFullReplace(
        OCIChar character,
        string path,
        string coordinateName,
        out VRCoordinateLoadSession session,
        out string status)
    {
        try
        {
            status = "服装卡已完整替换，饰品、妆容及坐标扩展数据已覆盖：" + coordinateName;
            Exception loadError;
            if (!VRCoordinateLoadSession.TryCreateNativeAndRun(
                character,
                status,
                "完整替换失败：",
                () => character.LoadClothesFile(path),
                out session,
                out loadError))
            {
                status = "完整替换失败：" + Unwrap(loadError).Message;
                return false;
            }
            RegisterSession(session);
            return true;
        }
        catch (Exception ex)
        {
            session = null;
            status = "完整替换失败：" + Unwrap(ex).Message;
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
            Type pluginType = pluginAssembly.GetType(
                "KK_CoordinateLoadOption.KK_CoordinateLoadOption",
                throwOnError: false);
            FieldInfo moreAccessoriesField = pluginType?.GetField(
                "_isMoreAccessoriesExist",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            int mode = moreAccessoriesField != null
                ? Convert.ToInt32(moreAccessoriesField.GetValue(null))
                : 0;
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
    private MethodInfo _coordinateWriteEvent;
    private MethodInfo _coordinateReadEvent;
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

    public VRCoordinateLoadSession(
        Assembly assembly,
        OCIChar character,
        string path,
        string coordinateName,
        int accessoryCount,
        string[] clothesToggleNames)
    {
        _assembly = assembly;
        _character = character;
        _path = path;
        _coordinateName = coordinateName;
        _accessoryCount = accessoryCount;
        _clothesToggleNames = clothesToggleNames;
    }

    private VRCoordinateLoadSession(OCIChar nativeCharacter, string completedStatus)
    {
        _character = nativeCharacter;
        _nativeStatus = completedStatus;
        CaptureTargetCoordinateSnapshot();
    }

    public static bool TryCreateNativeAndRun(
        OCIChar character,
        string status,
        string recoveryFailurePrefix,
        Action loadAction,
        out VRCoordinateLoadSession session,
        out Exception error)
    {
        session = null;
        error = null;
        VRCoordinateLoadSession candidate = null;
        try
        {
            candidate = new VRCoordinateLoadSession(character, status);
            loadAction();
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
            clothesToggles[clothesToggles.Length - 1].isOn = true;
            string[] accessoryNames = new string[_accessoryCount];
            for (int i = 0; i < accessoryNames.Length; i++)
                accessoryNames[i] = "accessories";
            Toggle[] accessoryToggles = CreateToggles(accessoryNames);
            for (int i = 0; i < accessoryToggles.Length; i++)
                accessoryToggles[i].isOn = true;

            SetSaved(RequireField(patchesType, "coordinatePath"), _path);
            SetSaved(RequireField(patchesType, "tgls"), clothesToggles);
            SetSaved(RequireField(patchesType, "tgls2"), accessoryToggles);
            SetSaved(RequireField(patchesType, "lockHairAcc"), true);
            SetSaved(RequireField(patchesType, "addAccModeFlag"), false);
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
            status = success
                ? _nativeStatus
                : "完整替换失败：目标角色已经失效";
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
                status = "服装卡已替换，并保护现有发饰：" + _coordinateName;
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

        _coordinateBackupName = coordinate.coordinateName;

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
                else if (parameters.Length == 1
                    && parameters[0].ParameterType == typeof(ChaFileCoordinate)
                    && string.Equals(method.Name, "CoordinateWriteEvent", StringComparison.Ordinal))
                {
                    _coordinateWriteEvent = method;
                }
                else if (parameters.Length == 1
                    && parameters[0].ParameterType == typeof(ChaFileCoordinate)
                    && string.Equals(method.Name, "CoordinateReadEvent", StringComparison.Ordinal))
                {
                    _coordinateReadEvent = method;
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
                || _coordinateWriteEvent == null
                || _coordinateReadEvent == null
                || _serializeExtendedData == null
                || _deserializeExtendedData == null)
            {
                throw new InvalidOperationException(
                    "ExtensibleSaveFormat 服装快照接口不兼容");
            }

            // Match ExtendedSave's normal coordinate-save lifecycle so plugins first
            // flush their live MaterialEditor/KCOX/accessory state into the coordinate.
            _coordinateWriteEvent.Invoke(null, new object[] { coordinate });
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
        error = null;
        if (_coordinateBackupBytes == null
            || _coordinateBackupVersion == null
            || _character == null
            || _character.charInfo == null)
        {
            error = "目标角色服装快照不可用";
            return false;
        }

        try
        {
            ChaFileCoordinate restored = new ChaFileCoordinate();
            if (!restored.LoadBytes(_coordinateBackupBytes, _coordinateBackupVersion))
                throw new InvalidOperationException("无法读取换装前的角色服装备份");

            ChaFileCoordinate target = _character.charInfo.nowCoordinate;
            target.clothes = restored.clothes;
            target.accessory = restored.accessory;
            target.enableMakeup = restored.enableMakeup;
            target.makeup = restored.makeup;
            target.coordinateName = _coordinateBackupName ?? string.Empty;

            if (_extendedCoordinateSnapshotCaptured)
            {
                IDictionary currentExtendedData = _getAllExtendedCoordinateData.Invoke(
                    null,
                    new object[] { target }) as IDictionary;
                currentExtendedData?.Clear();
                IDictionary restoredExtendedData = _deserializeExtendedData
                    .MakeGenericMethod(_extendedCoordinateDictionaryType)
                    .Invoke(null, new object[] { _extendedCoordinateBackupBytes }) as IDictionary;
                if (restoredExtendedData == null)
                    throw new InvalidOperationException("无法读取换装前的服装扩展数据快照");
                foreach (DictionaryEntry saved in restoredExtendedData)
                {
                    if (saved.Key is string key)
                    {
                        _setExtendedCoordinateData.Invoke(
                            null,
                            new[] { (object)target, key, saved.Value });
                    }
                }

                // Match ExtendedSave's normal coordinate-load lifecycle before the
                // character reload begins so plugin controllers consume the snapshot.
                _coordinateReadEvent.Invoke(null, new object[] { target });
            }

            ChaFileDefine.CoordinateType type =
                (ChaFileDefine.CoordinateType)_character.charInfo.fileStatus.coordinateType;
            if (!_character.charInfo.AssignCoordinate(type))
                throw new InvalidOperationException("工作室没有接受服装快照恢复请求");
            _character.SetCoordinateInfo(type, true);
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
        _coordinateWriteEvent = null;
        _coordinateReadEvent = null;
        _serializeExtendedData = null;
        _deserializeExtendedData = null;
        _extendedCoordinateDictionaryType = null;
        _extendedCoordinateBackupBytes = null;
        _extendedCoordinateSnapshotCaptured = false;
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

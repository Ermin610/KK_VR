using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Studio;
using UnityEngine;
using VRGIN.Core;

namespace KKCharaStudioVR;

internal enum VRCharacterCardMode
{
    AddFemale,
    AddMale
}

internal enum VRCharacterRemovalOutcome
{
    NotRemoved,
    RemovedCleanly,
    PartiallyRemoved
}

internal sealed class VRCharacterReloadObservation
{
    public ChaControl Character;
    public int BaselineGeneration;
    public bool Available;
}

internal static class VRCharacterCardService
{
    private static bool _optionalControllerTypesResolved;
    private static Type _accessoryStatesControllerType;
    private static bool _reloadEventHookAttempted;
    private static bool _reloadEventHookAvailable;
    private static EventInfo _reloadEvent;
    private static Delegate _reloadEventHandler;
    private static readonly Dictionary<int, int> ReloadGenerations =
        new Dictionary<int, int>();
    private static readonly HashSet<int> ControllerRepairAttempts =
        new HashSet<int>();

    public static string FemaleRoot => UserData.Create("chara/female");
    public static string MaleRoot => UserData.Create("chara/male");

    public static bool TryGetBrowserRoot(VRCharacterCardMode mode, out string root, out string status)
    {
        root = null;
        status = null;
        if (mode == VRCharacterCardMode.AddFemale)
        {
            root = FemaleRoot;
            return true;
        }

        if (mode == VRCharacterCardMode.AddMale)
        {
            root = MaleRoot;
            return true;
        }

        status = "未知的角色卡分类";
        return false;
    }

    public static bool ExecuteAdd(VRCharacterCardMode mode, string path, out string status)
    {
        try
        {
            if (!ValidateCardPath(path, out status))
                return false;

            Studio.Studio studio = Singleton<Studio.Studio>.Instance;
            if (studio == null)
            {
                status = "工作室角色功能尚未就绪";
                return false;
            }

            if (mode == VRCharacterCardMode.AddFemale)
            {
                if (!VRWristFileCatalog.IsInsideRoot(FemaleRoot, path))
                {
                    status = "请选择女性角色卡";
                    return false;
                }
                if (!ValidateCardData(path, 1, out status))
                    return false;

                studio.AddFemale(path);
                status = "已添加女角色：" + Path.GetFileNameWithoutExtension(path);
                return true;
            }

            if (mode == VRCharacterCardMode.AddMale)
            {
                if (!VRWristFileCatalog.IsInsideRoot(MaleRoot, path))
                {
                    status = "请选择男性角色卡";
                    return false;
                }
                if (!ValidateCardData(path, 0, out status))
                    return false;

                studio.AddMale(path);
                status = "已添加男角色：" + Path.GetFileNameWithoutExtension(path);
                return true;
            }

            status = "未知的角色卡读取方式";
            return false;
        }
        catch (Exception ex)
        {
            status = "角色卡读取失败：" + ex.Message;
            VRLog.Error(status);
            return false;
        }
    }

    public static bool TryLoadNativePreview(
        VRCharacterCardMode mode,
        string path,
        out Texture2D texture,
        out string characterName,
        out string status)
    {
        texture = null;
        characterName = null;
        try
        {
            if (!ValidateCardPath(path, out status))
                return false;

            byte sex = mode == VRCharacterCardMode.AddMale ? (byte)0 : (byte)1;
            string expectedRoot = sex == 0 ? MaleRoot : FemaleRoot;
            if (!VRWristFileCatalog.IsInsideRoot(expectedRoot, path))
            {
                status = sex == 0 ? "请选择男性角色卡" : "请选择女性角色卡";
                return false;
            }

            ChaFileControl card = new ChaFileControl();
            if (!card.LoadCharaFile(path, sex, noLoadPng: true))
            {
                status = sex == 0
                    ? "文件不是有效的男性角色卡"
                    : "文件不是有效的女性角色卡";
                return false;
            }

            // This is the same thumbnail loader used by Studio.CharaList.
            texture = PngAssist.LoadTexture(path);
            if (texture == null)
            {
                status = "工作室原生卡面接口未能读取图片";
                return false;
            }

            characterName = card.parameter != null
                && !string.IsNullOrEmpty(card.parameter.fullname)
                    ? card.parameter.fullname
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
            status = "工作室原生卡面读取失败：" + ex.Message;
            VRLog.Warn(status);
            return false;
        }
    }

    public static bool ExecuteReplace(int objectKey, string path, out string status)
    {
        try
        {
            if (!ValidateCardPath(path, out status))
                return false;

            OCIChar character;
            if (!TryGetCharacter(objectKey, out character, out status))
                return false;

            string expectedRoot = character.sex == 0 ? MaleRoot : FemaleRoot;
            if (!VRWristFileCatalog.IsInsideRoot(expectedRoot, path))
            {
                status = character.sex == 0 ? "男性角色只能替换为男卡" : "女性角色只能替换为女卡";
                return false;
            }
            if (!ValidateCardData(path, (byte)character.sex, out status))
                return false;

            string previousName = VRCharacterClothingService.GetCharacterName(character);
            Studio.Studio studio = Singleton<Studio.Studio>.Instance;
            if (studio == null || studio.treeNodeCtrl == null || character.treeNodeObject == null)
            {
                status = "工作室角色选择尚未就绪";
                return false;
            }

            // Native CharaList replaces the currently selected OCIChar. Selecting the
            // exact target first preserves plugin hooks that resolve their character
            // through Studio's selection rather than the ChangeChara argument.
            studio.treeNodeCtrl.SelectSingle(character.treeNodeObject, false);
            character.ChangeChara(path);
            TransientHead transientHead = ((UnityEngine.Component)character.charInfo)
                .GetComponent<TransientHead>();
            if (transientHead != null)
                transientHead.RefreshAfterCharacterChange();
            status = previousName + " 已替换为：" + VRCharacterClothingService.GetCharacterName(character);
            return true;
        }
        catch (Exception ex)
        {
            status = "角色替换失败：" + ex.Message;
            VRLog.Error(status);
            return false;
        }
    }

    public static bool TryGetCharacter(int objectKey, out OCIChar character, out string status)
    {
        character = null;
        try
        {
            Studio.Studio studio = Singleton<Studio.Studio>.Instance;
            ObjectCtrlInfo item;
            if (studio == null
                || studio.dicObjectCtrl == null
                || !studio.dicObjectCtrl.TryGetValue(objectKey, out item))
            {
                status = "所选角色已不在当前场景";
                return false;
            }

            character = item as OCIChar;
            if (character == null)
            {
                status = "所选对象不是角色";
                return false;
            }

            status = VRCharacterClothingService.GetCharacterName(character);
            return true;
        }
        catch (Exception ex)
        {
            status = "无法读取场景角色：" + ex.Message;
            return false;
        }
    }

    private static bool ValidateCardPath(string path, out string status)
    {
        if (string.IsNullOrEmpty(path)
            || !File.Exists(path)
            || !string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase))
        {
            status = "角色卡文件无效";
            return false;
        }

        status = null;
        return true;
    }

    private static bool ValidateCardData(string path, byte sex, out string status)
    {
        ChaFileControl card = new ChaFileControl();
        if (!card.LoadCharaFile(path, sex, noLoadPng: true))
        {
            status = sex == 0
                ? "文件不是有效的男性角色卡"
                : "文件不是有效的女性角色卡";
            return false;
        }

        status = null;
        return true;
    }

    public static int CountAttachedTreeNodes(OCIChar character)
    {
        return CountAttachedTreeNodes(character?.treeNodeObject);
    }

    public static bool AreReplacementControllersReady(OCIChar character, out string status)
    {
        status = null;
        if (character == null || character.charInfo == null)
        {
            status = "新角色控制器尚未就绪";
            return false;
        }

        ResolveOptionalControllerTypes();
        if (_accessoryStatesControllerType != null)
        {
            Component controller = ((Component)character.charInfo)
                .GetComponent(_accessoryStatesControllerType);
            if (controller == null)
            {
                status = "Accessory States 角色控制器缺失";
                return false;
            }

            Type controllerType = controller.GetType();
            PropertyInfo registrationProperty = controllerType.GetProperty(
                "ControllerRegistration",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            PropertyInfo startedProperty = controllerType.GetProperty(
                "Started",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            object registration = registrationProperty == null
                ? null
                : registrationProperty.GetValue(controller, null);
            bool started = startedProperty != null
                && (bool)startedProperty.GetValue(controller, null);
            if (registration == null || !started)
            {
                status = "Accessory States 角色控制器尚未完成初始化";
                return false;
            }
        }
        return true;
    }

    public static bool TryBeginReplacementReloadObservation(
        OCIChar character,
        out VRCharacterReloadObservation observation,
        out string status)
    {
        observation = null;
        status = null;
        if (character == null || character.charInfo == null)
        {
            status = "无法观察新角色的 KKAPI 重载状态";
            return false;
        }

        observation = new VRCharacterReloadObservation
        {
            Character = character.charInfo,
            Available = EnsureReloadEventHook()
        };
        if (observation.Available)
        {
            int generation;
            ReloadGenerations.TryGetValue(
                character.charInfo.GetInstanceID(),
                out generation);
            observation.BaselineGeneration = generation;
        }
        return true;
    }

    public static bool HasReplacementReloadCompleted(
        VRCharacterReloadObservation observation,
        OCIChar character,
        out string status)
    {
        status = null;
        if (observation == null
            || character == null
            || character.charInfo == null
            || !object.ReferenceEquals(observation.Character, character.charInfo))
        {
            status = "角色根对象在重载期间发生变化";
            return false;
        }
        if (!observation.Available)
            return character.charInfo.loadEnd;

        int generation;
        ReloadGenerations.TryGetValue(
            character.charInfo.GetInstanceID(),
            out generation);
        if (generation <= observation.BaselineGeneration)
        {
            status = "正在等待 KKAPI 完成角色插件重载";
            return false;
        }
        return true;
    }

    public static bool TryRepairMissingReplacementControllers(
        OCIChar character,
        out bool repairStarted,
        out string status)
    {
        repairStarted = false;
        status = null;
        if (character == null || character.charInfo == null)
        {
            status = "新角色控制器尚未就绪";
            return false;
        }

        ResolveOptionalControllerTypes();
        if (_accessoryStatesControllerType == null)
            return true;

        Component existing = ((Component)character.charInfo)
            .GetComponent(_accessoryStatesControllerType);
        if (existing != null)
        {
            status = "Accessory States 控制器存在但状态异常，已停止自动修复以避免重复订阅";
            return false;
        }

        int characterId = character.charInfo.GetInstanceID();
        if (!ControllerRepairAttempts.Add(characterId))
        {
            status = "Accessory States 控制器自动修复已经尝试过一次";
            return false;
        }

        try
        {
            Type characterApiType = Type.GetType(
                "KKAPI.Chara.CharacterApi, KKAPI",
                false);
            if (characterApiType == null)
            {
                status = "KKAPI 角色控制器接口不可用";
                return false;
            }

            PropertyInfo handlersProperty = characterApiType.GetProperty(
                "RegisteredHandlers",
                BindingFlags.Public | BindingFlags.Static);
            IEnumerable handlers = handlersProperty == null
                ? null
                : handlersProperty.GetValue(null, null) as IEnumerable;
            bool registrationFound = false;
            if (handlers != null)
            {
                foreach (object handler in handlers)
                {
                    if (handler == null)
                        continue;
                    Type handlerType = handler.GetType();
                    PropertyInfo controllerTypeProperty = handlerType.GetProperty(
                        "ControllerType",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    PropertyInfo dataIdProperty = handlerType.GetProperty(
                        "ExtendedDataId",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    Type registeredType = controllerTypeProperty == null
                        ? null
                        : controllerTypeProperty.GetValue(handler, null) as Type;
                    string dataId = dataIdProperty == null
                        ? null
                        : dataIdProperty.GetValue(handler, null) as string;
                    if (registeredType == _accessoryStatesControllerType
                        && string.Equals(dataId, "Accessory_States", StringComparison.Ordinal))
                    {
                        registrationFound = true;
                        break;
                    }
                }
            }
            if (!registrationFound)
            {
                status = "Accessory States 未在 KKAPI 注册，无法安全补建控制器";
                return false;
            }

            MethodInfo createMethod = characterApiType.GetMethod(
                "CreateOrAddBehaviours",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(ChaControl) },
                null);
            if (createMethod == null)
            {
                status = "当前 KKAPI 不提供安全补建角色控制器的接口";
                return false;
            }

            createMethod.Invoke(null, new object[] { character.charInfo });
            repairStarted = true;
            status = "已请求 KKAPI 补建缺失的 Accessory States 控制器";
            return true;
        }
        catch (Exception exception)
        {
            status = "Accessory States 控制器自动修复失败："
                + (exception.InnerException ?? exception).Message;
            return false;
        }
    }

    public static bool ValidateRemovalTarget(
        int objectKey,
        OCIChar expectedCharacter,
        TreeNodeObject expectedNode,
        out string status)
    {
        Studio.Studio studio = Singleton<Studio.Studio>.Instance;
        try
        {
            OCIChar character;
            if (!TryGetCharacter(objectKey, out character, out status))
                return false;
            if (!object.ReferenceEquals(character, expectedCharacter)
                || character.treeNodeObject == null
                || !object.ReferenceEquals(character.treeNodeObject, expectedNode))
            {
                status = "所选角色已经变化，请重新选择";
                return false;
            }
            if (studio == null || studio.treeNodeCtrl == null)
            {
                status = "工作室人物树尚未就绪";
                return false;
            }
            if (!expectedNode.enableDelete)
            {
                status = VRCharacterClothingService.GetCharacterName(character)
                    + "：该人物不能从当前场景移除";
                return false;
            }
            ObjectCtrlInfo mappedInfo;
            if (studio.dicInfo == null
                || !studio.dicInfo.TryGetValue(expectedNode, out mappedInfo)
                || !object.ReferenceEquals(mappedInfo, expectedCharacter))
            {
                status = "人物树与场景数据不一致，请重新选择";
                return false;
            }

            ObjectInfo mappedObject;
            if (studio.sceneInfo == null
                || studio.sceneInfo.dicObject == null
                || expectedCharacter.objectInfo == null
                || !studio.sceneInfo.dicObject.TryGetValue(objectKey, out mappedObject)
                || !object.ReferenceEquals(mappedObject, expectedCharacter.objectInfo))
            {
                status = "人物存档数据与场景对象不一致，请重新选择";
                return false;
            }

            status = VRCharacterClothingService.GetCharacterName(character);
            return true;
        }
        catch (Exception ex)
        {
            status = "无法确认待移除人物：" + ex.Message;
            return false;
        }
    }

    public static VRCharacterRemovalOutcome ExecuteRemove(
        int objectKey,
        OCIChar expectedCharacter,
        TreeNodeObject expectedNode,
        out string status)
    {
        Studio.Studio studio = Singleton<Studio.Studio>.Instance;
        string characterName = expectedCharacter == null
            ? "所选角色"
            : VRCharacterClothingService.GetCharacterName(expectedCharacter);
        bool deleteStarted = false;
        try
        {
            if (!ValidateRemovalTarget(
                objectKey,
                expectedCharacter,
                expectedNode,
                out status))
            {
                return VRCharacterRemovalOutcome.NotRemoved;
            }

            // Mirror WorkspaceCtrl.OnClickDelete: make the confirmed node the only
            // selection, invoke TreeNodeCtrl's no-argument delete path, then clear
            // stale undo commands that may still reference the removed hierarchy.
            studio.treeNodeCtrl.SelectSingle(null, true);
            studio.treeNodeCtrl.SelectSingle(expectedNode, false);
            TreeNodeObject[] selected = studio.treeNodeCtrl.selectNodes;
            if (selected == null
                || selected.Length != 1
                || !object.ReferenceEquals(selected[0], expectedNode))
            {
                status = "无法安全锁定要移除的人物";
                return VRCharacterRemovalOutcome.NotRemoved;
            }

            deleteStarted = true;
            studio.treeNodeCtrl.DeleteNode();
            return EvaluateRemovalOutcome(
                studio,
                objectKey,
                expectedCharacter,
                expectedNode,
                characterName,
                null,
                out status);
        }
        catch (Exception ex)
        {
            VRCharacterRemovalOutcome outcome = EvaluateRemovalOutcome(
                studio,
                objectKey,
                expectedCharacter,
                expectedNode,
                characterName,
                ex,
                out status);
            if (outcome == VRCharacterRemovalOutcome.NotRemoved)
            {
                status = "移除场景人物失败：" + ex.Message;
                VRLog.Error(status);
            }
            else
            {
                VRLog.Warn(status);
            }
            return outcome;
        }
        finally
        {
            if (deleteStarted)
            {
                try
                {
                    if (Singleton<UndoRedoManager>.IsInstance())
                        Singleton<UndoRedoManager>.Instance.Clear();
                }
                catch (Exception ex)
                {
                    VRLog.Warn("Unable to clear undo history after character removal: " + ex.Message);
                }
            }
        }
    }

    private static VRCharacterRemovalOutcome EvaluateRemovalOutcome(
        Studio.Studio studio,
        int objectKey,
        OCIChar expectedCharacter,
        TreeNodeObject expectedNode,
        string characterName,
        Exception exception,
        out string status)
    {
        try
        {
            if (studio == null
                || studio.dicObjectCtrl == null
                || studio.dicInfo == null
                || studio.sceneInfo == null
                || studio.sceneInfo.dicObject == null)
            {
                status = characterName
                    + "：删除已开始，但工作室数据表不可用。请勿保存当前场景，重新载入原场景。";
                return VRCharacterRemovalOutcome.PartiallyRemoved;
            }

            ObjectCtrlInfo liveController;
            bool controllerPresent = studio.dicObjectCtrl.TryGetValue(
                objectKey,
                out liveController);
            ObjectCtrlInfo liveTreeInfo;
            bool treeInfoPresent = studio.dicInfo.TryGetValue(
                expectedNode,
                out liveTreeInfo);
            ObjectInfo liveObject;
            bool sceneObjectPresent = studio.sceneInfo.dicObject.TryGetValue(
                objectKey,
                out liveObject);

            if (!controllerPresent && !treeInfoPresent && !sceneObjectPresent)
            {
                status = "已从场景移除人物：" + characterName;
                if (exception != null)
                    status += "（附加清理出现警告：" + exception.Message + "）";
                return VRCharacterRemovalOutcome.RemovedCleanly;
            }

            bool originalStillIntact = controllerPresent
                && treeInfoPresent
                && sceneObjectPresent
                && object.ReferenceEquals(liveController, expectedCharacter)
                && object.ReferenceEquals(liveTreeInfo, expectedCharacter)
                && object.ReferenceEquals(liveObject, expectedCharacter.objectInfo);
            if (originalStillIntact)
            {
                status = characterName + "：工作室没有开始移除";
                return VRCharacterRemovalOutcome.NotRemoved;
            }

            status = characterName
                + "：人物只完成了部分删除，Timeline 已保持暂停。请勿保存当前场景，重新载入原场景。";
            if (exception != null)
                status += " 原因：" + exception.Message;
            return VRCharacterRemovalOutcome.PartiallyRemoved;
        }
        catch (Exception evaluationException)
        {
            status = characterName
                + "：无法确认人物是否完整移除。请勿保存当前场景，重新载入原场景。原因："
                + evaluationException.Message;
            return VRCharacterRemovalOutcome.PartiallyRemoved;
        }
    }

    private static int CountAttachedTreeNodes(TreeNodeObject node)
    {
        if (node == null || node.child == null)
            return 0;

        int count = 0;
        foreach (TreeNodeObject child in node.child)
        {
            if (child == null)
                continue;
            count++;
            count += CountAttachedTreeNodes(child);
        }
        return count;
    }

    private static bool EnsureReloadEventHook()
    {
        if (_reloadEventHookAttempted)
            return _reloadEventHookAvailable;

        _reloadEventHookAttempted = true;
        try
        {
            Type characterApiType = Type.GetType(
                "KKAPI.Chara.CharacterApi, KKAPI",
                false);
            if (characterApiType == null)
                return false;

            _reloadEvent = characterApiType.GetEvent(
                "CharacterReloaded",
                BindingFlags.Public | BindingFlags.Static);
            if (_reloadEvent == null || _reloadEvent.EventHandlerType == null)
                return false;

            MethodInfo callback = typeof(VRCharacterCardService).GetMethod(
                "OnCharacterReloaded",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (callback == null)
                return false;

            _reloadEventHandler = Delegate.CreateDelegate(
                _reloadEvent.EventHandlerType,
                callback);
            _reloadEvent.AddEventHandler(null, _reloadEventHandler);
            _reloadEventHookAvailable = true;
        }
        catch (Exception exception)
        {
            _reloadEventHookAvailable = false;
            _reloadEvent = null;
            _reloadEventHandler = null;
            VRLog.Warn("Unable to observe KKAPI character reloads: " + exception.Message);
        }
        return _reloadEventHookAvailable;
    }

    private static void OnCharacterReloaded(object sender, EventArgs args)
    {
        ChaControl character = sender as ChaControl;
        if (character == null)
            return;

        int characterId = character.GetInstanceID();
        int generation;
        ReloadGenerations.TryGetValue(characterId, out generation);
        ReloadGenerations[characterId] = generation + 1;
        ControllerRepairAttempts.Remove(characterId);
    }

    private static void ResolveOptionalControllerTypes()
    {
        if (_optionalControllerTypesResolved)
            return;
        _optionalControllerTypesResolved = true;
        _accessoryStatesControllerType = Type.GetType(
            "Accessory_States.CharaEvent, KK_Accessory_States",
            false);
        if (_accessoryStatesControllerType != null)
            return;

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!string.Equals(
                assembly.GetName().Name,
                "KK_Accessory_States",
                StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            _accessoryStatesControllerType = assembly.GetType(
                "Accessory_States.CharaEvent",
                false);
            break;
        }
    }
}

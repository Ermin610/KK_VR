using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Studio;
using UnityEngine;
using UnityEngine.UI;
using VRGIN.Core;

namespace KKCharaStudioVR;

public sealed partial class VRWristMenuController
{
    private GameObject _characterRemoveConfirmPage;
    private Text _characterRemoveNameText;
    private Text _characterRemoveDetailText;
    private VRWristMenuButtonTarget _characterRemoveConfirmButton;

    private int _pendingCharacterRemoveObjectKey = -1;
    private OCIChar _pendingCharacterRemoveCharacter;
    private TreeNodeObject _pendingCharacterRemoveNode;
    private string _pendingCharacterRemoveName;
    private int _pendingCharacterRemoveAttachedCount;

    private BrowserMode _characterRemovalReturnMode = BrowserMode.None;
    private string _characterRemovalReturnRoot;
    private string _characterRemovalReturnDirectory;
    private int _characterRemovalReturnOffset;

    private void BuildCharacterRemoveConfirmPage()
    {
        CreateButton(
            "CharacterRemoveBack",
            "<",
            18f,
            10f,
            new Color(1f, 1f, 1f, 0.08f),
            new Color(1f, 1f, 1f, 0.2f),
            HandleCancelCharacterRemoval,
            _characterRemoveConfirmPage.transform,
            58f,
            38f,
            28);
        CreateText(
            "CharacterRemoveTitle",
            _characterRemoveConfirmPage.transform,
            L("移除场景人物", "シーン人物を削除", "Remove scene character"),
            92f,
            10f,
            444f,
            40f,
            27,
            TextAnchor.MiddleLeft,
            Color.white);

        Image warning = CreateImage(
            "CharacterRemoveWarning",
            _characterRemoveConfirmPage.transform,
            24f,
            72f,
            512f,
            76f,
            new Color(0.34f, 0.065f, 0.075f, 0.48f));
        ApplyGlassEffects(warning, new Color(1f, 0.28f, 0.25f, 0.22f), true, 2f);
        CreateText(
            "CharacterRemoveWarningText",
            warning.transform,
            L(
                "此操作不可撤销。人物、挂在人物下的物体，以及对应 Timeline 轨道会一起移除。",
                "この操作は元に戻せません。人物、子オブジェクト、対応する Timeline トラックも削除されます。",
                "This cannot be undone. The character, attached objects, and matching Timeline tracks will be removed."),
            16f,
            8f,
            480f,
            60f,
            16,
            TextAnchor.MiddleLeft,
            new Color(1f, 0.72f, 0.68f, 1f));

        _characterRemoveNameText = CreateText(
            "CharacterRemoveTarget",
            _characterRemoveConfirmPage.transform,
            L("目标人物：-", "対象人物：-", "Target: -"),
            24f,
            172f,
            512f,
            42f,
            22,
            TextAnchor.MiddleCenter,
            PrimaryTextColor);
        _characterRemoveDetailText = CreateText(
            "CharacterRemoveDetail",
            _characterRemoveConfirmPage.transform,
            string.Empty,
            24f,
            224f,
            512f,
            58f,
            16,
            TextAnchor.MiddleCenter,
            SecondaryTextColor);

        CreateButton(
            "CharacterRemoveCancel",
            L("取消", "キャンセル", "Cancel"),
            24f,
            322f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            HandleCancelCharacterRemoval,
            _characterRemoveConfirmPage.transform,
            248f,
            66f,
            20);
        _characterRemoveConfirmButton = CreateButton(
            "CharacterRemoveConfirm",
            L("确认移除人物", "人物を削除", "Remove character"),
            288f,
            322f,
            new Color(0.4f, 0.055f, 0.07f, 0.66f),
            new Color(0.76f, 0.09f, 0.12f, 0.9f),
            HandleConfirmCharacterRemoval,
            _characterRemoveConfirmPage.transform,
            248f,
            66f,
            20);
    }

    private void HandleOpenCharacterRemoval()
    {
        if (_operationInProgress)
            return;
        if (_browserMode != BrowserMode.AddFemale && _browserMode != BrowserMode.AddMale)
            return;

        _characterRemovalReturnMode = _browserMode;
        _characterRemovalReturnRoot = _browserRoot;
        _characterRemovalReturnDirectory = _browserDirectory;
        _characterRemovalReturnOffset = _browserOffset;

        string status;
        List<VRVmdActorTarget> targets = VRVmdTargetService.GetAllTargets(out status);
        if (targets.Count == 0)
        {
            SetStatus(
                status ?? L("当前场景没有人物", "現在のシーンに人物がいません", "There are no characters in this scene"),
                new Color(1f, 0.72f, 0.25f, 1f),
                6f);
            return;
        }

        OpenActorTargetBrowser(
            BrowserMode.SelectCharacterRemoveActor,
            targets,
            L("请选择要从场景移除的人物", "シーンから削除する人物を選択", "Select the character to remove from the scene"),
            out status);
    }

    private void HandleCharacterRemoveActorSelection(VRWristFileEntry entry)
    {
        if (!entry.IsActorTarget)
            return;

        VRVmdActorTarget target;
        string status;
        if (!VRVmdTargetService.TryGetTarget(entry.ObjectKey, out target, out status)
            || target.Character == null
            || target.Character.treeNodeObject == null
            || !VRCharacterCardService.ValidateRemovalTarget(
                entry.ObjectKey,
                target.Character,
                target.Character.treeNodeObject,
                out status))
        {
            SetStatus(
                status ?? L("所选人物已经失效", "選択した人物は無効です", "The selected character is no longer valid"),
                new Color(1f, 0.38f, 0.34f, 1f),
                7f);
            return;
        }

        _pendingCharacterRemoveObjectKey = entry.ObjectKey;
        _pendingCharacterRemoveCharacter = target.Character;
        _pendingCharacterRemoveNode = target.Character.treeNodeObject;
        _pendingCharacterRemoveName = target.DisplayName;
        _pendingCharacterRemoveAttachedCount =
            VRCharacterCardService.CountAttachedTreeNodes(target.Character);
        ShowPage(WristMenuPage.CharacterRemoveConfirm);
        SetStatus(
            L("请再次确认要移除的人物", "削除する人物をもう一度確認してください", "Confirm the character to remove"),
            new Color(1f, 0.3f, 0.26f, 1f),
            0f);
    }

    private void RefreshCharacterRemoveConfirmPage()
    {
        string ignoredStatus;
        bool valid = _pendingCharacterRemoveObjectKey >= 0
            && VRCharacterCardService.ValidateRemovalTarget(
                _pendingCharacterRemoveObjectKey,
                _pendingCharacterRemoveCharacter,
                _pendingCharacterRemoveNode,
                out ignoredStatus);

        if (_characterRemoveNameText != null)
        {
            _characterRemoveNameText.text = valid
                ? L("目标人物：", "対象人物：", "Target: ") + (_pendingCharacterRemoveName ?? "-")
                : L("目标人物已经失效", "対象人物は無効です", "The target is no longer valid");
            _characterRemoveNameText.color = valid
                ? PrimaryTextColor
                : new Color(1f, 0.42f, 0.36f, 1f);
        }
        if (_characterRemoveDetailText != null)
        {
            _characterRemoveDetailText.text = valid
                ? (_pendingCharacterRemoveAttachedCount > 0
                    ? L("还会同时移除附属树节点：", "同時に削除される子ノード：", "Attached tree nodes also removed: ")
                        + _pendingCharacterRemoveAttachedCount
                    : L("没有挂在人物下的附属树节点", "人物配下の子ノードはありません", "No attached tree nodes"))
                : L("请取消并重新选择人物", "キャンセルして人物を選び直してください", "Cancel and select the character again");
        }
        _characterRemoveConfirmButton?.SetInteractable(valid && !_operationInProgress);
    }

    private void HandleCancelCharacterRemoval()
    {
        if (_operationInProgress)
            return;
        ClearPendingCharacterRemovalConfirmation();
        RestoreCharacterRemovalBrowser(
            L("已取消移除人物", "人物の削除をキャンセルしました", "Character removal cancelled"),
            new Color(1f, 0.72f, 0.25f, 1f));
    }

    private void HandleConfirmCharacterRemoval()
    {
        if (_operationInProgress)
            return;
        if (_pendingCharacterRemoveObjectKey < 0
            || _pendingCharacterRemoveCharacter == null
            || _pendingCharacterRemoveNode == null)
        {
            RefreshCharacterRemoveConfirmPage();
            return;
        }

        string validationStatus;
        if (!VRCharacterCardService.ValidateRemovalTarget(
            _pendingCharacterRemoveObjectKey,
            _pendingCharacterRemoveCharacter,
            _pendingCharacterRemoveNode,
            out validationStatus))
        {
            RefreshCharacterRemoveConfirmPage();
            SetStatus(validationStatus, new Color(1f, 0.38f, 0.34f, 1f), 8f);
            return;
        }

        int liveAttachedCount = VRCharacterCardService.CountAttachedTreeNodes(
            _pendingCharacterRemoveCharacter);
        if (liveAttachedCount != _pendingCharacterRemoveAttachedCount)
        {
            _pendingCharacterRemoveAttachedCount = liveAttachedCount;
            RefreshCharacterRemoveConfirmPage();
            SetStatus(
                L(
                    "人物下的附属节点数量发生变化，请检查列表后再次确认",
                    "人物配下の子ノード数が変わりました。確認してもう一度実行してください",
                    "The attached-node count changed. Review it and confirm again."),
                new Color(1f, 0.72f, 0.25f, 1f),
                0f);
            return;
        }

        StartCoroutine(ExecuteCharacterRemovalAfterFrame(
            _pendingCharacterRemoveObjectKey,
            _pendingCharacterRemoveCharacter,
            _pendingCharacterRemoveNode,
            _pendingCharacterRemoveAttachedCount));
    }

    private IEnumerator ExecuteCharacterRemovalAfterFrame(
        int objectKey,
        OCIChar expectedCharacter,
        TreeNodeObject expectedNode,
        int expectedAttachedCount)
    {
        _operationInProgress = true;
        RefreshCharacterRemoveConfirmPage();
        SetStatus(
            L("正在安全移除场景人物…", "シーン人物を安全に削除しています…", "Safely removing the scene character…"),
            new Color(1f, 0.72f, 0.25f, 1f),
            0f);
        VRTimelineMutationSession timelineSession = null;
        VRMmddCharacterReplacementSession mmddSession = null;
        string status = null;
        bool removalStarted = false;
        bool removedCleanly = false;
        bool timelineHealthy = true;
        try
        {
            yield return null;

            if (!VRCharacterCardService.ValidateRemovalTarget(
                    objectKey,
                    expectedCharacter,
                    expectedNode,
                    out status)
                || VRCharacterCardService.CountAttachedTreeNodes(expectedCharacter)
                    != expectedAttachedCount)
            {
                if (string.IsNullOrEmpty(status))
                    status = L(
                        "人物或附属节点已经变化，请重新确认",
                        "人物または子ノードが変わりました。もう一度確認してください",
                        "The character or its attached nodes changed. Confirm again.");
                yield break;
            }

            bool prepared = VRTimelineService.PauseForSceneMutation(
                out timelineSession,
                out status);
            if (prepared)
                prepared = VRMmddService.TryPrepareCharacterReplacement(
                    objectKey,
                    out mmddSession,
                    out status);

            if (prepared && mmddSession != null)
            {
                yield return new WaitForEndOfFrame();
                yield return null;
            }

            if (prepared
                && (!VRCharacterCardService.ValidateRemovalTarget(
                        objectKey,
                        expectedCharacter,
                        expectedNode,
                        out status)
                    || VRCharacterCardService.CountAttachedTreeNodes(expectedCharacter)
                        != expectedAttachedCount))
            {
                prepared = false;
                if (string.IsNullOrEmpty(status))
                    status = "人物或附属节点在删除前发生变化，已取消操作";
            }

            if (!prepared)
            {
                if (mmddSession != null)
                {
                    string rollbackStatus;
                    if (!VRMmddService.TryRestoreCharacterReplacement(
                        mmddSession,
                        objectKey,
                        out rollbackStatus))
                    {
                        VRMmddService.DiscardCharacterReplacement(mmddSession);
                        status = (status ?? "已取消移除") + "；原 VMD 恢复失败：" + rollbackStatus;
                    }
                }
                yield break;
            }

            VRCharacterRemovalOutcome outcome = VRCharacterCardService.ExecuteRemove(
                objectKey,
                expectedCharacter,
                expectedNode,
                out status);
            removalStarted = outcome != VRCharacterRemovalOutcome.NotRemoved;
            removedCleanly = outcome == VRCharacterRemovalOutcome.RemovedCleanly;
            if (!removalStarted)
            {
                if (mmddSession != null)
                {
                    string rollbackStatus;
                    if (!VRMmddService.TryRestoreCharacterReplacement(
                        mmddSession,
                        objectKey,
                        out rollbackStatus))
                    {
                        VRMmddService.DiscardCharacterReplacement(mmddSession);
                        status += "；原 VMD 恢复失败：" + rollbackStatus;
                    }
                }
                yield break;
            }

            try
            {
                string mmddStatus;
                if (!VRMmddService.CompleteCharacterRemoval(mmddSession, out mmddStatus))
                {
                    VRMmddService.DiscardCharacterReplacement(mmddSession);
                    status += "；" + mmddStatus;
                    VRLog.Warn(mmddStatus);
                }
                else if (!string.IsNullOrEmpty(mmddStatus))
                {
                    status += "；" + mmddStatus;
                }
            }
            catch (Exception exception)
            {
                VRMmddService.DiscardCharacterReplacement(mmddSession);
                status += "；MMDD 清理警告：" + exception.Message;
                VRLog.Warn("MMDD cleanup after character removal failed: " + exception);
            }

            string cleanupWarning = ClearCharacterTargetsAfterRemoval(
                objectKey,
                expectedCharacter);
            if (!string.IsNullOrEmpty(cleanupWarning))
                status += "；" + cleanupWarning;
        }
        finally
        {
            if (timelineSession != null && !timelineSession.Completed)
            {
                string timelineStatus;
                timelineHealthy = VRTimelineService.RestoreAfterSceneMutation(
                    timelineSession,
                    removalStarted,
                    removedCleanly,
                    out timelineStatus);
                if (!string.IsNullOrEmpty(timelineStatus))
                    status = string.IsNullOrEmpty(status)
                        ? timelineStatus
                        : status + "；" + timelineStatus;
                if (!timelineHealthy && removalStarted)
                    VRLog.Warn("Character removal Timeline recovery warning: " + timelineStatus);
            }

            _operationInProgress = false;
            try
            {
                if (removalStarted)
                {
                    ClearPendingCharacterRemovalConfirmation();
                    Color resultColor = removedCleanly && timelineHealthy
                        ? new Color(0.35f, 1f, 0.62f, 1f)
                        : new Color(1f, 0.38f, 0.34f, 1f);
                    RestoreCharacterRemovalBrowser(status, resultColor);
                    if (removedCleanly && timelineHealthy)
                        VRLog.Info(status);
                    else
                        VRLog.Warn(status);
                }
                else
                {
                    RefreshCharacterRemoveConfirmPage();
                    SetStatus(
                        status ?? L("移除场景人物失败", "シーン人物の削除に失敗しました", "Could not remove the scene character"),
                        new Color(1f, 0.38f, 0.34f, 1f),
                        8f);
                    VRLog.Warn(status ?? "Character removal failed");
                }
            }
            catch (Exception exception)
            {
                VRLog.Error("Unable to leave the character-removal operation cleanly: " + exception);
                ShowPage(WristMenuPage.Root);
                SetStatus(
                    status ?? "人物移除流程出现异常，请重新打开人物列表确认场景状态",
                    new Color(1f, 0.38f, 0.34f, 1f),
                    10f);
            }
        }
    }

    private string ClearCharacterTargetsAfterRemoval(int objectKey, OCIChar character)
    {
        List<string> warnings = new List<string>();
        try
        {
            VRCoordinateAccessoryTracker.Clear(character);
            VRCoordinateCardService.ConsumeUndoSnapshot(character);
        }
        catch (Exception exception)
        {
            warnings.Add("换装缓存清理失败：" + exception.Message);
        }
        try
        {
            if (VRMmdPlaybackController.Instance != null)
                VRMmdPlaybackController.Instance.NotifyTargetMotionCleared(objectKey);
        }
        catch (Exception exception)
        {
            warnings.Add("MMD 目标缓存清理失败：" + exception.Message);
        }

        if (_clothingTargetObjectKey == objectKey)
        {
            _clothingTargetObjectKey = -1;
            _clothingTargetCharacter = null;
        }
        if (_highHeelsTargetObjectKey == objectKey)
        {
            _highHeelsTargetObjectKey = -1;
            _highHeelsTargetCharacter = null;
            try
            {
                VRMmddStateBridge.ResetHighHeels();
            }
            catch (Exception exception)
            {
                warnings.Add("高跟鞋状态清理失败：" + exception.Message);
            }
        }
        if (_trackedAccessoryTargetObjectKey == objectKey)
        {
            _trackedAccessoryTargetObjectKey = -1;
            _trackedAccessoryTargetCharacter = null;
            _trackedAccessoryTargetName = null;
            _selectedTrackedAccessorySlots.Clear();
            _trackedAccessoryEntries.Clear();
        }
        if (_lastCoordinateUndoObjectKey == objectKey)
        {
            _lastCoordinateUndoObjectKey = -1;
            _lastCoordinateUndoCharacter = null;
        }
        try
        {
            _pendingVmdActorKeys = (_pendingVmdActorKeys ?? new int[0])
                .Where(key => key != objectKey)
                .ToArray();
        }
        catch (Exception exception)
        {
            warnings.Add("动作选择缓存清理失败：" + exception.Message);
        }
        return warnings.Count == 0
            ? null
            : string.Join("；", warnings.ToArray());
    }

    private void RestoreCharacterRemovalBrowser(string status, Color color)
    {
        BrowserMode mode = _characterRemovalReturnMode;
        string root = _characterRemovalReturnRoot;
        string directory = _characterRemovalReturnDirectory;
        int offset = _characterRemovalReturnOffset;

        bool valid = (mode == BrowserMode.AddFemale || mode == BrowserMode.AddMale)
            && !string.IsNullOrEmpty(root)
            && !string.IsNullOrEmpty(directory)
            && Directory.Exists(root)
            && Directory.Exists(directory)
            && VRWristFileCatalog.IsInsideRoot(root, directory);
        try
        {
            if (valid)
            {
                _browserMode = mode;
                _characterCardMode = mode == BrowserMode.AddMale
                    ? VRCharacterCardMode.AddMale
                    : VRCharacterCardMode.AddFemale;
                _browserRoot = root;
                _browserDirectory = directory;
                _browserFixedEntries = null;
                _browserOffset = offset;
                _browserStickDirection = 0;
                RefreshBrowserEntries();
                ShowPage(WristMenuPage.Browser);
            }
            else
            {
                OpenCharacterBrowser(
                    mode == BrowserMode.AddMale
                        ? VRCharacterCardMode.AddMale
                        : VRCharacterCardMode.AddFemale);
            }
            ClearCharacterRemovalReturnSnapshot();
            SetStatus(status, color, 7f);
        }
        catch (Exception exception)
        {
            VRLog.Warn("Unable to restore the character-card browser after removal: " + exception);
            OpenCharacterBrowser(
                mode == BrowserMode.AddMale
                    ? VRCharacterCardMode.AddMale
                    : VRCharacterCardMode.AddFemale);
            ClearCharacterRemovalReturnSnapshot();
            SetStatus(status, color, 7f);
        }
    }

    private void ClearPendingCharacterRemovalConfirmation()
    {
        _pendingCharacterRemoveObjectKey = -1;
        _pendingCharacterRemoveCharacter = null;
        _pendingCharacterRemoveNode = null;
        _pendingCharacterRemoveName = null;
        _pendingCharacterRemoveAttachedCount = 0;
    }

    private void ClearCharacterRemovalReturnSnapshot()
    {
        _characterRemovalReturnMode = BrowserMode.None;
        _characterRemovalReturnRoot = null;
        _characterRemovalReturnDirectory = null;
        _characterRemovalReturnOffset = 0;
    }
}

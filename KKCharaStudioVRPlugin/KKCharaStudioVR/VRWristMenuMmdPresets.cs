using System;
using UnityEngine;
using UnityEngine.UI;
using VRGIN.Core;

namespace KKCharaStudioVR;

public sealed partial class VRWristMenuController
{
    private GameObject _mmdPresetPage;
    private Text _mmdPresetCurrentText;
    private string[] _mmdPresetIds = new string[0];
    private VRWristMenuButtonTarget[] _mmdPresetButtons =
        new VRWristMenuButtonTarget[0];

    private GameObject _mmdClearConfirmPage;
    private Text _mmdClearTargetText;
    private int _pendingMmdClearObjectKey = -1;
    private Studio.OCIChar _pendingMmdClearCharacter;

    private void BuildMmdPresetPage()
    {
        CreateButton(
            "MmdPresetBack",
            "<",
            18f,
            10f,
            new Color(1f, 1f, 1f, 0.08f),
            new Color(1f, 1f, 1f, 0.2f),
            HandleBackToMmdCues,
            _mmdPresetPage.transform,
            58f,
            38f,
            28);
        CreateText(
            "MmdPresetTitle",
            _mmdPresetPage.transform,
            L("MMD 脱衣预设", "MMD 衣装プリセット", "MMD clothing presets"),
            92f,
            10f,
            444f,
            40f,
            27,
            TextAnchor.MiddleLeft,
            Color.white);

        _mmdPresetCurrentText = CreateText(
            "MmdPresetCurrent",
            _mmdPresetPage.transform,
            L("当前预设：--", "現在のプリセット：--", "Current preset: --"),
            24f,
            60f,
            512f,
            34f,
            15,
            TextAnchor.MiddleLeft,
            SecondaryTextColor);

        _mmdPresetIds = VRMmdCueSheetStore.GetGlobalPresetIds() ?? new string[0];
        _mmdPresetButtons = new VRWristMenuButtonTarget[_mmdPresetIds.Length];
        for (int index = 0; index < _mmdPresetIds.Length; index++)
        {
            int capturedIndex = index;
            string capturedId = _mmdPresetIds[index];
            _mmdPresetButtons[index] = CreateButton(
                "MmdPreset" + index,
                GetMmdPresetDisplayName(index),
                24f,
                104f + index * 50f,
                new Color(0.12f, 0.16f, 0.2f, 0.56f),
                new Color(0.22f, 0.34f, 0.42f, 0.8f),
                () => HandleSelectMmdPreset(capturedId, capturedIndex),
                _mmdPresetPage.transform,
                512f,
                44f,
                14);
        }

        Image noteBackground = CreateImage(
            "MmdPresetNoteBackground",
            _mmdPresetPage.transform,
            24f,
            358f,
            512f,
            44f,
            new Color(0.7f, 0.84f, 0.94f, 0.065f));
        ApplyGlassEffects(
            noteBackground,
            new Color(0.86f, 0.96f, 1f, 0.12f),
            false,
            0f);
        CreateText(
            "MmdPresetNote",
            _mmdPresetPage.transform,
            L(
                "应用于未自定义的 VMD；自定义 VMD 保持自己的事件表。",
                "未カスタムの VMD に適用。カスタム済み VMD は変更しません。",
                "Applies to non-custom VMDs; custom cue sheets stay unchanged."),
            38f,
            362f,
            484f,
            36f,
            13,
            TextAnchor.MiddleLeft,
            TertiaryTextColor);
    }

    private void BuildMmdClearConfirmPage()
    {
        CreateButton(
            "MmdClearBack",
            "<",
            18f,
            10f,
            new Color(1f, 1f, 1f, 0.08f),
            new Color(1f, 1f, 1f, 0.2f),
            HandleCancelMmdClear,
            _mmdClearConfirmPage.transform,
            58f,
            38f,
            28);
        CreateText(
            "MmdClearTitle",
            _mmdClearConfirmPage.transform,
            L("确认清空 MMD 数据", "MMD データ消去の確認", "Confirm MMD data clear"),
            92f,
            10f,
            444f,
            40f,
            26,
            TextAnchor.MiddleLeft,
            Color.white);

        Image warningBackground = CreateImage(
            "MmdClearWarningBackground",
            _mmdClearConfirmPage.transform,
            24f,
            68f,
            512f,
            200f,
            new Color(0.42f, 0.075f, 0.065f, 0.28f));
        ApplyGlassEffects(
            warningBackground,
            new Color(1f, 0.35f, 0.28f, 0.2f),
            false,
            0f);
        _mmdClearTargetText = CreateText(
            "MmdClearTarget",
            _mmdClearConfirmPage.transform,
            L("当前角色：--", "対象キャラ：--", "Character: --"),
            42f,
            82f,
            476f,
            38f,
            18,
            TextAnchor.MiddleLeft,
            new Color(1f, 0.72f, 0.55f, 1f));
        CreateText(
            "MmdClearWarning",
            _mmdClearConfirmPage.transform,
            L(
                "将清除这个角色的骨骼与表情动作，并清空当前 MMDD 的全部镜头。\n\n其他角色动作、Timeline、Studio 相机和音频都会保留。",
                "このキャラのモーションと表情を消去し、現在の MMDD カメラをすべて消去します。\n\n他キャラ、Timeline、Studio カメラ、音声は保持されます。",
                "Clears this character's motion and morph data plus every current MMDD camera.\n\nOther characters, Timeline, Studio cameras, and audio are preserved."),
            42f,
            124f,
            476f,
            126f,
            15,
            TextAnchor.UpperLeft,
            PrimaryTextColor);

        CreateText(
            "MmdClearIrreversible",
            _mmdClearConfirmPage.transform,
            L(
                "此操作无法通过手环撤销。",
                "この操作は手首メニューから取り消せません。",
                "This cannot be undone from the wrist menu."),
            24f,
            282f,
            512f,
            28f,
            15,
            TextAnchor.MiddleLeft,
            new Color(1f, 0.52f, 0.42f, 1f));
        CreateButton(
            "MmdClearConfirm",
            L("确认清空动作与全局镜头", "モーションと全カメラを消去", "Clear motion and global cameras"),
            24f,
            324f,
            new Color(0.42f, 0.075f, 0.065f, 0.78f),
            new Color(0.72f, 0.12f, 0.1f, 0.92f),
            HandleConfirmMmdClear,
            _mmdClearConfirmPage.transform,
            336f,
            58f,
            16);
        CreateButton(
            "MmdClearCancel",
            L("取消", "キャンセル", "Cancel"),
            376f,
            324f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            HandleCancelMmdClear,
            _mmdClearConfirmPage.transform,
            160f,
            58f,
            16);
    }

    private void HandleOpenMmdPresetPage()
    {
        ShowPage(WristMenuPage.MmdCuePresets);
        SetStatus(
            L("选择全局 MMD 脱衣预设", "グローバル衣装プリセットを選択", "Choose the global MMD clothing preset"),
            new Color(1f, 0.72f, 0.25f, 1f),
            0f);
    }

    private void HandleBackToMmdCues()
    {
        ShowPage(WristMenuPage.MmdCues);
        SetStatus(
            L("MMD 服装联动", "MMD 衣装連動", "MMD clothing cues"),
            new Color(0.47f, 0.9f, 0.55f, 1f),
            0f);
    }

    private void RefreshMmdPresetPage()
    {
        ResolveSettings();
        EnsureMmdPresetIds();
        string currentId = VRMmdCueSheetStore.NormalizePresetId(
            _settings != null
                ? _settings.MmdClothingCuePresetId
                : VRMmdCueSheetStore.DefaultPresetId);
        int currentIndex = GetMmdPresetIndex(currentId);

        if (_mmdPresetCurrentText != null)
        {
            _mmdPresetCurrentText.text = L("当前预设：", "現在のプリセット：", "Current preset: ")
                + GetMmdPresetDisplayName(currentIndex);
        }

        int count = Math.Min(_mmdPresetButtons.Length, _mmdPresetIds.Length);
        for (int index = 0; index < count; index++)
        {
            VRWristMenuButtonTarget button = _mmdPresetButtons[index];
            if (button == null)
                continue;
            bool selected = string.Equals(
                currentId,
                _mmdPresetIds[index],
                StringComparison.OrdinalIgnoreCase);
            button.SetLabel(
                (selected ? "●  " : string.Empty)
                + GetMmdPresetDisplayName(index)
                + "\n"
                + GetMmdPresetSummary(index));
            button.SetColors(
                selected
                    ? new Color(0.09f, 0.36f, 0.31f, 0.62f)
                    : new Color(0.12f, 0.16f, 0.2f, 0.56f),
                new Color(0.16f, 0.46f, 0.4f, 0.8f));
        }
    }

    private void HandleSelectMmdPreset(string presetId, int presetIndex)
    {
        ResolveSettings();
        if (_settings == null)
        {
            ReportMmdResult(false, L("设置尚未就绪", "設定が未準備です", "Settings are not ready"));
            return;
        }

        string normalized = VRMmdCueSheetStore.NormalizePresetId(presetId);
        bool changed = !string.Equals(
            normalized,
            _settings.MmdClothingCuePresetId,
            StringComparison.OrdinalIgnoreCase);
        if (!changed)
        {
            ReportMmdResult(
                true,
                L("当前已是预设：", "現在のプリセット：", "Preset already selected: ")
                    + GetMmdPresetDisplayName(presetIndex));
            RefreshMmdPresetPage();
            return;
        }

        _settings.MmdClothingCuePresetId = normalized;
        bool saved = TrySaveInteractionSettings();
        _mmdCuePageIndex = 0;
        _mmdCueSelectedIndex = -1;

        VRMmdPlaybackController controller = VRMmdPlaybackController.Instance;
        if (controller != null)
            controller.NotifyCuePresetChanged();

        string status = L("已切换全局预设：", "グローバルプリセットを変更：", "Global preset selected: ")
            + GetMmdPresetDisplayName(presetIndex);
        if (controller != null && controller.HasCustomCueSheet)
        {
            status += L(
                "；当前 VMD 已自定义，不会被覆盖",
                "。現在の VMD はカスタム済みのため変更しません",
                "; the current custom VMD was not overwritten");
        }
        if (!saved)
            status += L("（本次生效但未保存）", "（今回は有効ですが未保存）", " (active now, but not saved)");
        ReportMmdResult(saved, status);
        RefreshMmdPresetPage();
    }

    private void HandleRequestClearMmdMotionAndCameras()
    {
        if (_operationInProgress)
        {
            ReportMmdResult(
                false,
                L("操作仍在进行，请稍候", "処理中です。しばらくお待ちください", "An operation is still running. Please wait"));
            return;
        }

        int[] selectedKeys = VRVmdTargetService.GetSelectedObjectKeys();
        if (selectedKeys.Length == 0)
        {
            ReportMmdResult(
                false,
                L("请先在工作面板中选择一个角色", "先に作業パネルでキャラを1人選択してください", "Select one character in the workspace first"));
            return;
        }
        if (selectedKeys.Length != 1)
        {
            ReportMmdResult(
                false,
                L("一次只能清空一个角色，请只选择一个角色", "一度に消去できるのは1人です。1人だけ選択してください", "Only one character can be cleared at a time"));
            return;
        }

        VRVmdActorTarget target = null;
        string status;
        if (!VRVmdTargetService.TryGetTarget(selectedKeys[0], out target, out status))
        {
            ReportMmdResult(false, status);
            return;
        }

        _pendingMmdClearObjectKey = target.ObjectKey;
        _pendingMmdClearCharacter = target.Character;
        ShowPage(WristMenuPage.MmdClearConfirm);
        SetStatus(
            L(
                "请确认：角色动作与全局 MMDD 镜头将被清空",
                "確認：キャラモーションと全 MMDD カメラを消去します",
                "Confirm clearing the character motion and all MMDD cameras"),
            new Color(1f, 0.38f, 0.34f, 1f),
            0f);
    }

    private void RefreshMmdClearConfirmPage()
    {
        if (_mmdClearTargetText == null)
            return;

        VRVmdActorTarget target = null;
        string ignoredStatus;
        bool valid = _pendingMmdClearObjectKey >= 0
            && _pendingMmdClearCharacter != null
            && VRVmdTargetService.TryGetTarget(
                _pendingMmdClearObjectKey,
                out target,
                out ignoredStatus)
            && object.ReferenceEquals(_pendingMmdClearCharacter, target.Character);
        _mmdClearTargetText.text = valid
            ? L("当前角色：", "対象キャラ：", "Character: ") + target.DisplayName
            : L("当前角色已失效", "対象キャラは無効です", "The selected character is no longer valid");
    }

    private void HandleConfirmMmdClear()
    {
        int objectKey = _pendingMmdClearObjectKey;
        Studio.OCIChar expectedCharacter = _pendingMmdClearCharacter;
        VRVmdActorTarget target;
        string status;
        if (objectKey < 0
            || expectedCharacter == null
            || !VRVmdTargetService.TryGetTarget(objectKey, out target, out status)
            || !object.ReferenceEquals(expectedCharacter, target.Character))
        {
            ShowPage(WristMenuPage.MmdPlayback);
            ReportMmdResult(
                false,
                L("确认已失效，角色没有被修改", "確認が無効です。キャラは変更されていません", "Confirmation expired; nothing was changed"));
            return;
        }

        bool success;
        _operationInProgress = true;
        try
        {
            success = VRMmddService.TryClearCharacterMmdData(
                objectKey,
                out status);
        }
        catch (Exception exception)
        {
            success = false;
            status = exception.Message;
        }
        finally
        {
            _operationInProgress = false;
        }

        if (success && VRMmdPlaybackController.Instance != null)
        {
            try
            {
                VRMmdPlaybackController.Instance.NotifyTargetMotionCleared(objectKey);
            }
            catch (Exception exception)
            {
                VRLog.Warn("MMD clear-state notification failed: " + exception.Message);
            }
        }

        ShowPage(WristMenuPage.MmdPlayback);
        ReportMmdResult(
            success,
            success
                ? L(
                    "已清空角色动作与全局 MMDD 镜头；其他角色、Timeline 和音频已保留",
                    "キャラモーションと全 MMDD カメラを消去しました。他キャラ、Timeline、音声は保持されています",
                    "Character motion and global MMDD cameras cleared; other characters, Timeline, and audio were preserved")
                : L("清空动作与镜头失败：", "モーションとカメラの消去に失敗：", "Could not clear motion and cameras: ")
                    + (status ?? L("未知错误", "不明なエラー", "Unknown error")));
    }

    private void HandleCancelMmdClear()
    {
        ShowPage(WristMenuPage.MmdPlayback);
        SetStatus(
            L("已取消，MMD 数据保持不变", "キャンセルしました。MMD データは変更されていません", "Cancelled; MMD data was not changed"),
            new Color(1f, 0.72f, 0.25f, 1f),
            4f);
    }

    private void ClearPendingMmdClearConfirmation()
    {
        _pendingMmdClearObjectKey = -1;
        _pendingMmdClearCharacter = null;
        if (_mmdClearTargetText != null)
        {
            _mmdClearTargetText.text = L(
                "当前角色：--",
                "対象キャラ：--",
                "Character: --");
        }
    }

    private void EnsureMmdPresetIds()
    {
        if (_mmdPresetIds == null || _mmdPresetIds.Length == 0)
            _mmdPresetIds = VRMmdCueSheetStore.GetGlobalPresetIds() ?? new string[0];
    }

    private int GetMmdPresetIndex(string presetId)
    {
        EnsureMmdPresetIds();
        string normalized = VRMmdCueSheetStore.NormalizePresetId(presetId);
        for (int index = 0; index < _mmdPresetIds.Length; index++)
        {
            if (string.Equals(
                normalized,
                _mmdPresetIds[index],
                StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }
        return 0;
    }

    private string GetCurrentMmdPresetDisplayName()
    {
        ResolveSettings();
        string presetId = _settings != null
            ? _settings.MmdClothingCuePresetId
            : VRMmdCueSheetStore.DefaultPresetId;
        return GetMmdPresetDisplayName(GetMmdPresetIndex(presetId));
    }

    private string GetMmdPresetDisplayName(int index)
    {
        string[] chinese = { "标准渐脱", "轻柔展示", "慢速层次", "强节拍切换", "纯透明渐变" };
        string[] japanese = { "標準フェード", "やさしい表示", "低速レイヤー", "強拍切替", "透明度のみ" };
        string[] english = { "Standard fade", "Gentle showcase", "Slow layers", "Strong beat", "Transparency only" };
        index = Mathf.Clamp(index, 0, chinese.Length - 1);
        return L(chinese[index], japanese[index], english[index]);
    }

    private string GetMmdPresetSummary(int index)
    {
        string[] chinese =
        {
            "均衡渐变 · AZ 校准默认",
            "低透明度 · 轻柔展示",
            "多阶段脱衣 · 慢速层次",
            "短渐变窗口 · 强节拍",
            "只改透明度 · 不切换穿脱"
        };
        string[] japanese =
        {
            "バランス型・AZ 調整済み",
            "低い透明度・やさしい表示",
            "多段階・ゆっくり変化",
            "短いフェード・強いビート",
            "透明度のみ・着脱変更なし"
        };
        string[] english =
        {
            "Balanced · AZ-calibrated default",
            "Low transparency · gentle presentation",
            "Multi-stage · slower layering",
            "Short fades · strong beat timing",
            "Transparency only · no dress-state changes"
        };
        index = Mathf.Clamp(index, 0, chinese.Length - 1);
        return L(chinese[index], japanese[index], english[index]);
    }

    private void ClearMmdExtendedMenuReferences()
    {
        _mmdPlaybackPage = null;
        _mmdCameraPage = null;
        _mmdCuePage = null;
        _mmdCueToggleButton = null;
        _mmdCuePresetButton = null;
        _mmdCuePageText = null;
        _mmdCueCustomButton = null;
        _mmdCueRestoreButton = null;
        _mmdCuePartButton = null;
        _mmdCueStateButton = null;
        for (int index = 0; index < _mmdCueRows.Length; index++)
            _mmdCueRows[index] = null;

        _mmdPresetPage = null;
        _mmdPresetCurrentText = null;
        if (_mmdPresetButtons != null)
        {
            for (int index = 0; index < _mmdPresetButtons.Length; index++)
                _mmdPresetButtons[index] = null;
        }
        _mmdPresetButtons = new VRWristMenuButtonTarget[0];
        _mmdPresetIds = new string[0];

        _mmdClearConfirmPage = null;
        _mmdClearTargetText = null;
        _pendingMmdClearObjectKey = -1;
        _pendingMmdClearCharacter = null;
    }
}

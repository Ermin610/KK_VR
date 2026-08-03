using System;
using UnityEngine;
using UnityEngine.UI;

namespace KKCharaStudioVR;

public sealed partial class VRWristMenuController
{
    private const int MmdCueRowsPerPage = 3;
    private GameObject _mmdCuePage;
    private VRWristMenuButtonTarget _mmdCueToggleButton;
    private VRWristMenuButtonTarget _mmdCuePresetButton;
    private readonly VRWristMenuButtonTarget[] _mmdCueRows =
        new VRWristMenuButtonTarget[MmdCueRowsPerPage];
    private Text _mmdCuePageText;
    private VRWristMenuButtonTarget _mmdCueCustomButton;
    private VRWristMenuButtonTarget _mmdCueRestoreButton;
    private VRWristMenuButtonTarget _mmdCuePartButton;
    private VRWristMenuButtonTarget _mmdCueStateButton;
    private int _mmdCuePageIndex;
    private int _mmdCueSelectedIndex = -1;

    private void BuildMmdCuePage()
    {
        CreateButton(
            "MmdCueBack", "<", 18f, 10f,
            new Color(1f, 1f, 1f, 0.08f),
            new Color(1f, 1f, 1f, 0.2f),
            HandleBackToMmdSettings,
            _mmdCuePage.transform, 58f, 38f, 28);
        CreateText(
            "MmdCueTitle", _mmdCuePage.transform,
            L("MMD 服装联动", "MMD 衣装連動", "MMD clothing cues"),
            92f, 10f, 444f, 40f, 27,
            TextAnchor.MiddleLeft, Color.white);

        _mmdCueToggleButton = CreateButton(
            "MmdCueToggle",
            L("服装联动  关", "衣装連動  オフ", "Clothing cues  Off"),
            24f, 60f,
            new Color(0.075f, 0.24f, 0.14f, 0.48f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            HandleToggleMmdCues,
            _mmdCuePage.transform, 176f, 42f, 15);
        _mmdCuePresetButton = CreateButton(
            "OpenMmdCuePresets",
            L("全局预设  ›", "グローバルプリセット  ›", "Global preset  ›"),
            216f, 60f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            HandleOpenMmdPresetPage,
            _mmdCuePage.transform, 320f, 42f, 14);

        CreateText(
            "MmdCueHint", _mmdCuePage.transform,
            L(
                "所有 VMD 默认共用预设；只有点过“自定义”的 VMD 会单独保存。",
                "全 VMD は既定プリセットを使用し、カスタムした VMD だけ個別保存されます。",
                "All VMDs use the global preset; only customized VMDs save an override."),
            24f, 106f, 512f, 32f, 13,
            TextAnchor.MiddleLeft, TertiaryTextColor);

        for (int row = 0; row < MmdCueRowsPerPage; row++)
        {
            int capturedRow = row;
            _mmdCueRows[row] = CreateButton(
                "MmdCueRow" + row,
                "--",
                24f, 142f + row * 44f,
                new Color(0.12f, 0.16f, 0.2f, 0.56f),
                new Color(0.22f, 0.34f, 0.42f, 0.8f),
                () => HandleSelectMmdCue(capturedRow),
                _mmdCuePage.transform, 512f, 38f, 15);
        }

        CreateButton(
            "MmdCuePrevious", "<", 24f, 274f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            () => HandleMmdCuePage(-1),
            _mmdCuePage.transform, 62f, 32f, 20);
        _mmdCuePageText = CreateText(
            "MmdCuePageText", _mmdCuePage.transform, "1 / 1",
            94f, 274f, 372f, 32f, 14,
            TextAnchor.MiddleCenter, SecondaryTextColor);
        CreateButton(
            "MmdCueNext", ">", 474f, 274f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            () => HandleMmdCuePage(1),
            _mmdCuePage.transform, 62f, 32f, 20);

        _mmdCueCustomButton = CreateButton(
            "MmdCueCustomize",
            L("自定义当前 VMD", "現在の VMD をカスタム", "Customize current VMD"),
            24f, 314f,
            new Color(0.28f, 0.2f, 0.07f, 0.48f),
            new Color(0.55f, 0.36f, 0.1f, 0.74f),
            HandleCustomizeCurrentVmd,
            _mmdCuePage.transform, 248f, 38f, 15);
        _mmdCueRestoreButton = CreateButton(
            "MmdCueRestoreGlobal",
            L("恢复全局预设", "グローバルに戻す", "Restore global preset"),
            288f, 314f,
            new Color(0.075f, 0.24f, 0.14f, 0.48f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            HandleRestoreGlobalMmdCues,
            _mmdCuePage.transform, 248f, 38f, 15);

        CreateButton(
            "MmdCuePercentMinus", "-5%", 24f, 360f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            () => HandleAdjustMmdCuePercent(-5f),
            _mmdCuePage.transform, 54f, 42f, 15);
        CreateButton(
            "MmdCuePercentPlus", "+5%", 84f, 360f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            () => HandleAdjustMmdCuePercent(5f),
            _mmdCuePage.transform, 54f, 42f, 15);
        _mmdCuePartButton = CreateButton(
            "MmdCuePart", L("部位", "部位", "Part"), 144f, 360f,
            new Color(0.07f, 0.21f, 0.25f, 0.52f),
            new Color(0.09f, 0.42f, 0.49f, 0.78f),
            HandleCycleMmdCuePart,
            _mmdCuePage.transform, 104f, 42f, 14);
        _mmdCueStateButton = CreateButton(
            "MmdCueState", L("状态", "状態", "State"), 254f, 360f,
            new Color(0.075f, 0.24f, 0.14f, 0.48f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            HandleCycleMmdCueState,
            _mmdCuePage.transform, 94f, 42f, 14);
        CreateButton(
            "MmdCueAdd", L("新增", "追加", "Add"), 354f, 360f,
            new Color(0.28f, 0.2f, 0.07f, 0.48f),
            new Color(0.55f, 0.36f, 0.1f, 0.74f),
            HandleAddMmdCue,
            _mmdCuePage.transform, 88f, 42f, 14);
        CreateButton(
            "MmdCueDelete", L("删除", "削除", "Delete"), 448f, 360f,
            new Color(0.42f, 0.075f, 0.065f, 0.72f),
            new Color(0.72f, 0.12f, 0.1f, 0.88f),
            HandleDeleteMmdCue,
            _mmdCuePage.transform, 88f, 42f, 14);
    }

    private void RefreshMmdCuePage()
    {
        ResolveSettings();
        VRMmdPlaybackController controller = VRMmdPlaybackController.Instance;
        if (controller != null)
            controller.ReloadCueSheet();

        bool enabled = _settings != null && _settings.MmdClothingCueEnabled;
        if (_mmdCueToggleButton != null)
        {
            _mmdCueToggleButton.SetLabel(enabled
                ? L("服装联动  开", "衣装連動  オン", "Clothing cues  On")
                : L("服装联动  关", "衣装連動  オフ", "Clothing cues  Off"));
        }

        VRMmdCueSheet sheet = controller?.EffectiveCueSheet;
        bool custom = controller != null && controller.HasCustomCueSheet;
        if (_mmdCuePresetButton != null)
        {
            string presetName = GetCurrentMmdPresetDisplayName();
            _mmdCuePresetButton.SetLabel(
                controller == null || string.IsNullOrEmpty(controller.CurrentVmdPath)
                    ? L("全局预设：", "グローバル：", "Global preset: ") + presetName + "  ›"
                    : custom
                        ? L("当前 VMD 自定义\n全局：", "現在の VMD はカスタム\nグローバル：", "Current VMD custom\nGlobal: ") + presetName + "  ›"
                        : L("使用全局：", "グローバル：", "Using global: ") + presetName + "  ›");
        }

        int count = sheet?.Cues?.Count ?? 0;
        int pageCount = Math.Max(1, (count + MmdCueRowsPerPage - 1) / MmdCueRowsPerPage);
        _mmdCuePageIndex = Mathf.Clamp(_mmdCuePageIndex, 0, pageCount - 1);
        if (_mmdCueSelectedIndex >= count)
            _mmdCueSelectedIndex = count - 1;
        if (_mmdCuePageText != null)
            _mmdCuePageText.text = (_mmdCuePageIndex + 1) + " / " + pageCount;

        for (int row = 0; row < MmdCueRowsPerPage; row++)
        {
            int index = _mmdCuePageIndex * MmdCueRowsPerPage + row;
            VRWristMenuButtonTarget button = _mmdCueRows[row];
            if (button == null)
                continue;
            if (sheet == null || sheet.Cues == null || index >= sheet.Cues.Count)
            {
                button.SetLabel("--");
                button.SetColors(
                    new Color(0.12f, 0.16f, 0.2f, 0.32f),
                    new Color(0.18f, 0.22f, 0.28f, 0.42f));
                continue;
            }

            VRMmdCue cue = sheet.Cues[index];
            button.SetLabel(GetMmdCueRowLabel(cue, index == _mmdCueSelectedIndex));
            button.SetColors(
                index == _mmdCueSelectedIndex
                    ? new Color(0.09f, 0.36f, 0.31f, 0.62f)
                    : new Color(0.12f, 0.16f, 0.2f, 0.56f),
                new Color(0.16f, 0.46f, 0.4f, 0.8f));
        }

        VRMmdCue selected = GetSelectedMmdCue();
        if (_mmdCuePartButton != null)
            _mmdCuePartButton.SetLabel(selected == null ? L("部位", "部位", "Part") : GetMmdCuePartLabel(selected.PartId));
        if (_mmdCueStateButton != null)
            _mmdCueStateButton.SetLabel(
                selected == null
                    ? L("状态", "状態", "State")
                    : selected.ApplyState
                        ? GetMmdCueStateLabel(selected.State)
                        : L("不改穿脱", "着脱変更なし", "No state change"));
        if (_mmdCueCustomButton != null)
            _mmdCueCustomButton.SetLabel(custom
                ? L("当前 VMD 已自定义", "現在の VMD はカスタム済み", "Current VMD customized")
                : L("自定义当前 VMD", "現在の VMD をカスタム", "Customize current VMD"));
    }

    private void HandleToggleMmdCues()
    {
        ResolveSettings();
        if (_settings == null)
        {
            ReportMmdResult(false, L("设置尚未就绪", "設定が未準備です", "Settings are not ready"));
            return;
        }
        _settings.MmdClothingCueEnabled = !_settings.MmdClothingCueEnabled;
        bool saved = TrySaveInteractionSettings();
        VRMmdPlaybackController.Instance?.NotifyCueSettingChanged(_settings.MmdClothingCueEnabled);
        ReportMmdResult(saved,
            _settings.MmdClothingCueEnabled
                ? L("服装联动已开启：未自定义 VMD 使用全局预设", "衣装連動を有効化しました", "Clothing cues enabled with the global default")
                : L("服装联动已关闭，并恢复播放前造型", "衣装連動を無効化し、元の衣装に戻しました", "Clothing cues disabled and baseline outfit restored"));
        RefreshMmdCuePage();
    }

    private void HandleSelectMmdCue(int row)
    {
        int index = _mmdCuePageIndex * MmdCueRowsPerPage + row;
        VRMmdCueSheet sheet = VRMmdPlaybackController.Instance?.EffectiveCueSheet;
        if (sheet?.Cues == null || index < 0 || index >= sheet.Cues.Count)
            return;
        _mmdCueSelectedIndex = index;
        RefreshMmdCuePage();
    }

    private void HandleMmdCuePage(int direction)
    {
        VRMmdCueSheet sheet = VRMmdPlaybackController.Instance?.EffectiveCueSheet;
        int count = sheet?.Cues?.Count ?? 0;
        int pageCount = Math.Max(1, (count + MmdCueRowsPerPage - 1) / MmdCueRowsPerPage);
        _mmdCuePageIndex = (_mmdCuePageIndex + direction + pageCount) % pageCount;
        RefreshMmdCuePage();
    }

    private void HandleCustomizeCurrentVmd()
    {
        VRMmdPlaybackController controller = VRMmdPlaybackController.Instance;
        string status = null;
        bool success = controller != null && (controller.HasCustomCueSheet
            || controller.CreateCustomCueSheet(out status));
        if (controller != null && controller.HasCustomCueSheet)
            status = L("当前 VMD 自定义已就绪", "現在の VMD カスタムを編集できます", "Current VMD customization is ready");
        else if (controller == null)
            status = L("MMD 控制器尚未就绪", "MMD 制御が未準備です", "MMD controller is not ready");
        ReportMmdResult(success, status);
        RefreshMmdCuePage();
    }

    private void HandleRestoreGlobalMmdCues()
    {
        VRMmdPlaybackController controller = VRMmdPlaybackController.Instance;
        string status = null;
        bool success = controller != null && controller.RestoreGlobalCueSheet(out status);
        if (controller == null)
            status = L("MMD 控制器尚未就绪", "MMD 制御が未準備です", "MMD controller is not ready");
        _mmdCueSelectedIndex = -1;
        _mmdCuePageIndex = 0;
        ReportMmdResult(success, status);
        RefreshMmdCuePage();
    }

    private void HandleAdjustMmdCuePercent(float delta)
    {
        VRMmdCue cue;
        VRMmdPlaybackController controller;
        if (!TryGetEditableMmdCue(out controller, out cue))
            return;
        float previousEnd = cue.Percent;
        cue.Percent = Mathf.Clamp(previousEnd + delta, 0f, 100f);
        if (cue.FadeStartPercent >= 0f)
        {
            float appliedDelta = cue.Percent - previousEnd;
            cue.FadeStartPercent = Mathf.Clamp(
                cue.FadeStartPercent + appliedDelta,
                0f,
                cue.Percent);
        }
        SaveEditedMmdCues(controller);
    }

    private void HandleCycleMmdCuePart()
    {
        VRMmdCue cue;
        VRMmdPlaybackController controller;
        if (!TryGetEditableMmdCue(out controller, out cue))
            return;
        cue.PartId = (cue.PartId + 1) % VRCharacterClothingService.PartCount;
        SaveEditedMmdCues(controller);
    }

    private void HandleCycleMmdCueState()
    {
        VRMmdCue cue;
        VRMmdPlaybackController controller;
        if (!TryGetEditableMmdCue(out controller, out cue))
            return;
        if (!cue.ApplyState)
        {
            cue.ApplyState = true;
            cue.State = 0;
        }
        else if (cue.State >= 3)
        {
            cue.ApplyState = false;
            cue.State = 0;
        }
        else
        {
            cue.State++;
        }
        SaveEditedMmdCues(controller);
    }

    private void HandleAddMmdCue()
    {
        VRMmdPlaybackController controller = VRMmdPlaybackController.Instance;
        if (controller == null || !controller.HasCustomCueSheet || controller.EffectiveCueSheet?.Cues == null)
        {
            ReportMmdResult(false, L("请先自定义当前 VMD", "先に現在の VMD をカスタムしてください", "Customize the current VMD first"));
            return;
        }
        controller.EffectiveCueSheet.Cues.Add(new VRMmdCue
        {
            Percent = 50f,
            PartId = 0,
            State = 0,
            Order = controller.EffectiveCueSheet.Cues.Count
        });
        _mmdCueSelectedIndex = controller.EffectiveCueSheet.Cues.Count - 1;
        SaveEditedMmdCues(controller);
    }

    private void HandleDeleteMmdCue()
    {
        VRMmdPlaybackController controller = VRMmdPlaybackController.Instance;
        if (controller == null || !controller.HasCustomCueSheet
            || controller.EffectiveCueSheet?.Cues == null
            || _mmdCueSelectedIndex < 0
            || _mmdCueSelectedIndex >= controller.EffectiveCueSheet.Cues.Count)
        {
            ReportMmdResult(false, L("请选择一个自定义事件", "カスタムイベントを選択してください", "Select a custom cue"));
            return;
        }
        controller.EffectiveCueSheet.Cues.RemoveAt(_mmdCueSelectedIndex);
        _mmdCueSelectedIndex = Math.Min(
            _mmdCueSelectedIndex,
            controller.EffectiveCueSheet.Cues.Count - 1);
        SaveEditedMmdCues(controller);
    }

    private bool TryGetEditableMmdCue(
        out VRMmdPlaybackController controller,
        out VRMmdCue cue)
    {
        controller = VRMmdPlaybackController.Instance;
        cue = GetSelectedMmdCue();
        if (controller == null || !controller.HasCustomCueSheet || cue == null)
        {
            ReportMmdResult(false, L("请先自定义当前 VMD 并选择一个事件", "現在の VMD をカスタムしてイベントを選択してください", "Customize the current VMD and select a cue first"));
            return false;
        }
        return true;
    }

    private VRMmdCue GetSelectedMmdCue()
    {
        VRMmdCueSheet sheet = VRMmdPlaybackController.Instance?.EffectiveCueSheet;
        return sheet?.Cues != null
            && _mmdCueSelectedIndex >= 0
            && _mmdCueSelectedIndex < sheet.Cues.Count
                ? sheet.Cues[_mmdCueSelectedIndex]
                : null;
    }

    private void SaveEditedMmdCues(VRMmdPlaybackController controller)
    {
        string status;
        bool success = controller.SaveCurrentCustomCueSheet(out status);
        ReportMmdResult(success, status);
        RefreshMmdCuePage();
    }

    private string GetMmdCuePartLabel(int partId)
    {
        switch (partId)
        {
            case 0: return L("上衣", "トップス", "Top");
            case 1: return L("下装", "ボトムス", "Bottom");
            case 2: return L("胸罩", "ブラ", "Bra");
            case 3: return L("内裤", "ショーツ", "Underwear");
            case 4: return L("手套", "手袋", "Gloves");
            case 5: return L("裤袜", "パンスト", "Pantyhose");
            case 6: return L("袜子", "靴下", "Socks");
            case 7: return L("鞋子", "靴", "Shoes");
            default: return L("服装", "衣装", "Clothing");
        }
    }

    private string GetMmdCueRowLabel(VRMmdCue cue, bool selected)
    {
        string timing = cue.FadeStartPercent >= 0f
            && cue.TargetTransparency >= 0f
            && cue.FadeStartPercent < cue.Percent
                ? cue.FadeStartPercent.ToString("0") + "–" + cue.Percent.ToString("0") + "%"
                : cue.Percent.ToString("0") + "%";
        string label = (selected ? "●  " : string.Empty)
            + timing
            + "  "
            + GetMmdCuePartLabel(cue.PartId);
        if (cue.TargetTransparency >= 0f)
        {
            label += L(" · 透明 ", "・透明 ", " · Transparency ")
                + cue.TargetTransparency.ToString("0")
                + "%";
        }
        if (cue.ApplyState)
            label += " · " + GetMmdCueStateLabel(cue.State);
        return label;
    }

    private string GetMmdCueStateLabel(byte state)
    {
        switch (state)
        {
            case 0: return L("穿好", "着用", "Worn");
            case 1: return L("半脱", "半脱ぎ", "Half off");
            case 2: return L("半脱 2", "半脱ぎ 2", "Half off 2");
            case 3: return L("脱下", "脱ぐ", "Off");
            default: return L("未知", "不明", "Unknown");
        }
    }
}

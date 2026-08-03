using UnityEngine;
using UnityEngine.UI;

namespace KKCharaStudioVR;

public sealed partial class VRWristMenuController
{
    private GameObject _timelinePage;
    private VRWristMenuButtonTarget _timelineFovModeButton;
    private Text _timelineFovValueText;
    private Text _timelineFovStateText;

    private void BuildTimelinePage()
    {
        CreateButton(
            "TimelineBack",
            "<",
            18f,
            10f,
            new Color(1f, 1f, 1f, 0.08f),
            new Color(1f, 1f, 1f, 0.2f),
            HandleBackToRoot,
            _timelinePage.transform,
            58f,
            38f,
            28);
        CreateText(
            "TimelineTitle",
            _timelinePage.transform,
            "Timeline",
            92f,
            10f,
            444f,
            40f,
            28,
            TextAnchor.MiddleLeft,
            Color.white);

        CreateText(
            "TimelinePlaybackHeading",
            _timelinePage.transform,
            L("播放控制", "再生コントロール", "Playback controls"),
            24f,
            62f,
            512f,
            22f,
            17,
            TextAnchor.MiddleLeft,
            new Color(0.42f, 0.72f, 1f, 1f));
        _timelineButton = CreateButton(
            "TimelinePlayPause",
            L("Timeline\n播放或暂停", "Timeline\n再生・一時停止", "Timeline\nPlay or pause"),
            24f,
            90f,
            new Color(0.12f, 0.17f, 0.3f, 0.5f),
            new Color(0.18f, 0.36f, 0.62f, 0.8f),
            HandleToggleTimeline,
            _timelinePage.transform,
            248f,
            62f,
            19);
        CreateButton(
            "TimelineSaveControls",
            L("保存参数", "設定を保存", "Save setup"),
            288f,
            90f,
            new Color(0.12f, 0.17f, 0.3f, 0.5f),
            new Color(0.18f, 0.36f, 0.62f, 0.8f),
            HandleSaveTimelineControlPreset,
            _timelinePage.transform,
            116f,
            62f,
            17);
        CreateButton(
            "TimelineLoadControls",
            L("读取参数", "設定を読込", "Load setup"),
            420f,
            90f,
            new Color(0.12f, 0.17f, 0.3f, 0.5f),
            new Color(0.18f, 0.36f, 0.62f, 0.8f),
            HandleLoadTimelineControlPreset,
            _timelinePage.transform,
            116f,
            62f,
            17);
        CreateText(
            "TimelinePlaybackHint",
            _timelinePage.transform,
            L(
                "右摇杆按下播放/暂停；上下调前后构图。按住 Grip：上下调高度，左右线性旋转。",
                "右スティック押下で再生・一時停止。上下で構図、Grip中は高さ／ヨーを連続調整。",
                "Right-stick click: play/pause. Y adjusts framing; hold Grip for height and linear yaw."),
            24f,
            160f,
            512f,
            34f,
            13,
            TextAnchor.MiddleLeft,
            SecondaryTextColor);

        CreateText(
            "TimelineFovHeading",
            _timelinePage.transform,
            L("Timeline VR 控制空间", "Timeline VR コントロール", "Timeline VR control space"),
            24f,
            210f,
            512f,
            22f,
            17,
            TextAnchor.MiddleLeft,
            new Color(0.42f, 0.72f, 1f, 1f));
        _timelineFovModeButton = CreateButton(
            "TimelineFovMode",
            L("构图匹配", "構図合わせ", "Composition match"),
            24f,
            242f,
            new Color(0.12f, 0.17f, 0.3f, 0.5f),
            new Color(0.18f, 0.36f, 0.62f, 0.8f),
            HandleToggleTimelineFovOverride,
            _timelinePage.transform,
            150f,
            52f,
            17);

        CreateButton(
            "TimelineFovMinusOne",
            "−",
            190f,
            242f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            () => HandleAdjustTimelineFov(-1f),
            _timelinePage.transform,
            54f,
            52f,
            26);
        _timelineFovValueText = CreateText(
            "TimelineFovValue",
            _timelinePage.transform,
            "53.1°",
            252f,
            242f,
            100f,
            52f,
            23,
            TextAnchor.MiddleCenter,
            Color.white);
        CreateButton(
            "TimelineFovPlusOne",
            "+",
            360f,
            242f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            () => HandleAdjustTimelineFov(1f),
            _timelinePage.transform,
            54f,
            52f,
            26);
        CreateButton(
            "TimelineFovReset",
            L("全部复位\nFOV / 高度 / 旋转", "すべてリセット", "Reset all"),
            426f,
            242f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            HandleResetTimelineFov,
            _timelinePage.transform,
            110f,
            52f,
            14);

        Image stateBackground = CreateImage(
            "TimelineFovStateBackground",
            _timelinePage.transform,
            24f,
            306f,
            512f,
            44f,
            new Color(0.7f, 0.84f, 0.94f, 0.065f));
        ApplyGlassEffects(stateBackground, new Color(0.86f, 0.96f, 1f, 0.12f), false, 0f);
        _timelineFovStateText = CreateText(
            "TimelineFovState",
            _timelinePage.transform,
            "FOV 53.1°  |  Y +0.00m  |  Yaw +0.0°",
            38f,
            310f,
            484f,
            36f,
            16,
            TextAnchor.MiddleLeft,
            new Color(0.76f, 0.88f, 0.94f, 1f));
    }

    private void HandleOpenTimeline()
    {
        ShowPage(WristMenuPage.Timeline);
        SetStatus("Timeline", new Color(0.42f, 0.72f, 1f, 1f), 0f);
    }

    private void RefreshTimelinePage()
    {
        RefreshTimelineButton();
        RefreshTimelineFovVisuals();
    }

    internal void RefreshTimelineFovVisuals()
    {
        ResolveSettings();
        bool overrideEnabled = _settings != null && _settings.TimelineFovOverrideEnabled;
        float fieldOfView;
        bool available = TryGetConfiguredTimelineFov(out fieldOfView);

        if (_timelineFovModeButton != null)
        {
            _timelineFovModeButton.SetLabel(
                overrideEnabled
                    ? L("构图匹配  开", "構図合わせ  オン", "Composition  On")
                    : L("构图匹配  关", "構図合わせ  オフ", "Composition  Off"));
        }
        if (_timelineFovValueText != null)
            _timelineFovValueText.text = available ? fieldOfView.ToString("F1") + "°" : "--";
        if (_timelineFovStateText != null)
        {
            float vertical = _settings == null ? 0f : _settings.TimelineVerticalOffset;
            float yaw = _settings == null ? 0f : _settings.TimelineYawOffset;
            string saved = _settings != null && _settings.TimelineSavedControlPresetAvailable
                ? L("  已存", "  保存済", "  Saved")
                : string.Empty;
            _timelineFovStateText.text =
                "FOV " + (available ? fieldOfView.ToString("F1") + "°" : "--")
                + "  |  Y " + vertical.ToString("+0.00;-0.00;0.00") + "m"
                + "  |  Yaw " + yaw.ToString("+0.0;-0.0;0.0") + "°"
                + saved;
        }
    }

    private bool TryGetConfiguredTimelineFov(out float fieldOfView)
    {
        fieldOfView = VRTimelineService.DefaultCameraFov;
        if (_settings == null)
            return false;
        fieldOfView = _settings.TimelineFovOverrideValue;
        return true;
    }

    private void HandleToggleTimelineFovOverride()
    {
        ResolveSettings();
        if (_settings == null)
        {
            SetStatus(
                L("设置尚未就绪", "設定がまだ準備できていません", "Settings are not ready"),
                new Color(1f, 0.38f, 0.34f, 1f),
                5f);
            return;
        }

        if (_settings.TimelineFovOverrideEnabled)
        {
            _settings.TimelineFovOverrideEnabled = false;
            bool saved = TrySaveInteractionSettings();
            bool disabledApplied = VRTimelineCameraFollowController.ApplyTimelineCompositionNow();
            RefreshTimelineFovVisuals();
            SetStatus(
                disabledApplied
                    ? L("Timeline VR 构图匹配已关闭", "Timeline VR 構図合わせを無効化しました", "Timeline VR composition disabled")
                    : L("设置已保存；CameraSync 尚未就绪", "設定を保存しました；CameraSync は未準備です", "Setting saved; CameraSync is not ready"),
                saved && disabledApplied ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.72f, 0.25f, 1f),
                saved && disabledApplied ? 4f : 7f);
            return;
        }

        _settings.TimelineFovOverrideEnabled = true;
        float current = _settings.TimelineFovOverrideValue;
        bool applied = VRTimelineCameraFollowController.ApplyTimelineCompositionNow();
        bool persisted = TrySaveInteractionSettings();
        RefreshTimelineFovVisuals();
        bool active = applied
            && VRTimelineCameraFollowController.IsTimelineCameraWriterActive;
        SetStatus(
            active
                ? L("VR 构图正在生效：", "VR 構図を適用中：", "VR composition active: ") + current.ToString("F1") + "°"
                : applied
                    ? L("参考值已保存；播放 Timeline 时生效", "基準値を保存；Timeline 再生中に適用", "Reference saved; applies while Timeline is playing")
                : L("参考值已保存；CameraSync 尚未就绪", "基準値を保存しました；CameraSync は未準備です", "Reference saved; CameraSync is not ready"),
            active && persisted ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.72f, 0.25f, 1f),
            active && persisted ? 4f : 7f);
    }

    private void HandleAdjustTimelineFov(float delta)
    {
        ResolveSettings();
        if (_settings == null)
            return;

        float current = _settings.TimelineFovOverrideValue;
        _settings.TimelineFovOverrideValue = current + delta;
        _settings.TimelineFovOverrideEnabled = true;
        float adjusted = _settings.TimelineFovOverrideValue;
        bool applied = VRTimelineCameraFollowController.ApplyTimelineCompositionNow();
        bool saved = TrySaveInteractionSettings();
        RefreshTimelineFovVisuals();
        bool active = applied
            && VRTimelineCameraFollowController.IsTimelineCameraWriterActive;
        SetStatus(
            active
                ? L("VR 构图正在生效：", "VR 構図を適用中：", "VR composition active: ") + adjusted.ToString("F1") + "°"
                : applied
                    ? L("参考值已保存；播放 Timeline 时生效", "基準値を保存；Timeline 再生中に適用", "Reference saved; applies while Timeline is playing")
                : L("参考值已保存；CameraSync 尚未就绪", "基準値を保存しました；CameraSync は未準備です", "Reference saved; CameraSync is not ready"),
            active && saved ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.72f, 0.25f, 1f),
            active && saved ? 3f : 7f);
    }

    private void HandleResetTimelineFov()
    {
        ResolveSettings();
        if (_settings == null)
            return;

        _settings.TimelineFovOverrideValue = VRTimelineService.DefaultCameraFov;
        _settings.TimelineFovOverrideEnabled = true;
        _settings.TimelineVerticalOffset = 0f;
        _settings.TimelineYawOffset = 0f;
        bool applied = VRTimelineCameraFollowController.ApplyTimelineCompositionNow();
        bool saved = TrySaveInteractionSettings();
        RefreshTimelineFovVisuals();
        bool active = applied
            && VRTimelineCameraFollowController.IsTimelineCameraWriterActive;
        SetStatus(
            active
                ? L("Timeline 参数已全部复位", "Timeline 設定をすべてリセット", "Timeline controls reset")
                : applied
                    ? L("默认参数已保存；播放 Timeline 时生效", "既定値を保存；Timeline 再生中に適用", "Defaults saved; applies during Timeline playback")
                    : L("默认参数已保存；CameraSync 尚未就绪", "既定値を保存しました；CameraSync は未準備です", "Defaults saved; CameraSync is not ready"),
            active && saved ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.72f, 0.25f, 1f),
            active && saved ? 4f : 7f);
    }

    private void HandleSaveTimelineControlPreset()
    {
        ResolveSettings();
        if (_settings == null)
            return;

        _settings.TimelineSavedFovOverrideEnabled = _settings.TimelineFovOverrideEnabled;
        _settings.TimelineSavedFovOverrideValue = _settings.TimelineFovOverrideValue;
        _settings.TimelineSavedVerticalOffset = _settings.TimelineVerticalOffset;
        _settings.TimelineSavedYawOffset = _settings.TimelineYawOffset;
        _settings.TimelineSavedControlPresetAvailable = true;
        bool saved = TrySaveInteractionSettings();
        RefreshTimelineFovVisuals();
        SetStatus(
            saved
                ? L("Timeline 参数已保存，可随时读取", "Timeline 設定を保存しました", "Timeline setup saved")
                : L("Timeline 参数保存失败", "Timeline 設定の保存に失敗", "Could not save Timeline setup"),
            saved ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.38f, 0.34f, 1f),
            saved ? 4f : 7f);
    }

    private void HandleLoadTimelineControlPreset()
    {
        ResolveSettings();
        if (_settings == null)
            return;
        if (!_settings.TimelineSavedControlPresetAvailable)
        {
            SetStatus(
                L("还没有保存过 Timeline 参数", "保存済みの Timeline 設定がありません", "No saved Timeline setup yet"),
                new Color(1f, 0.72f, 0.25f, 1f),
                5f);
            return;
        }

        _settings.TimelineFovOverrideEnabled = _settings.TimelineSavedFovOverrideEnabled;
        _settings.TimelineFovOverrideValue = _settings.TimelineSavedFovOverrideValue;
        _settings.TimelineVerticalOffset = _settings.TimelineSavedVerticalOffset;
        _settings.TimelineYawOffset = _settings.TimelineSavedYawOffset;
        bool applied = VRTimelineCameraFollowController.ApplyTimelineCompositionNow();
        bool saved = TrySaveInteractionSettings();
        RefreshTimelineFovVisuals();
        SetStatus(
            applied
                ? L("已读取并应用 Timeline 参数", "Timeline 設定を読込んで適用しました", "Timeline setup loaded and applied")
                : L("参数已读取；播放 Timeline 时应用", "設定を読込みました；再生時に適用", "Setup loaded; applies during Timeline playback"),
            applied && saved ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.72f, 0.25f, 1f),
            applied && saved ? 4f : 7f);
    }
}

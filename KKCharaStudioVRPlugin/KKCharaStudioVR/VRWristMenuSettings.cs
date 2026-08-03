using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KKCharaStudioVR;

public sealed partial class VRWristMenuController
{
    private enum WristSettingsSection
    {
        General,
        Interaction,
        Mmd,
        Visual,
        ReShade,
        CharacterLight,
        Audio
    }

    private static readonly VRAudioChannel[] AudioChannels =
    {
        VRAudioChannel.Master,
        VRAudioChannel.Bgm,
        VRAudioChannel.Environment,
        VRAudioChannel.SystemEffects,
        VRAudioChannel.GameEffects
    };

    private GameObject _settingsPage;
    private GameObject _settingsGeneralPanel;
    private GameObject _settingsInteractionPanel;
    private GameObject _settingsMmdPanel;
    private GameObject _settingsVisualPanel;
    private GameObject _settingsReShadePanel;
    private GameObject _settingsCharacterLightPanel;
    private GameObject _settingsAudioPanel;
    private Text _settingsTitleText;
    private VRWristMenuButtonTarget _timelineButton;
    private VRWristMenuButtonTarget _settingsGeneralTab;
    private VRWristMenuButtonTarget _settingsVisualTab;
    private VRWristMenuButtonTarget _settingsReShadeTab;
    private VRWristMenuButtonTarget _settingsCharacterLightTab;
    private VRWristMenuButtonTarget _settingsAudioTab;
    private VRWristMenuButtonTarget _languageChineseButton;
    private VRWristMenuButtonTarget _languageJapaneseButton;
    private VRWristMenuButtonTarget _languageEnglishButton;
    private readonly VRWristMenuButtonTarget[] _controllerLayoutButtons =
        new VRWristMenuButtonTarget[3];
    private Text _mainGuiDistanceText;
    private Text _mainGuiScaleText;
    private VRWristMenuButtonTarget _timelineCameraFollowButton;
    private VRWristMenuButtonTarget _mmdPresentationHideButton;
    private VRWristMenuButtonTarget _mmdAutoHighHeelsButton;
    private Text _mmdFovSpeedText;
    private VRWristMenuButtonTarget _characterLightToggle;
    private VRWristMenuButtonTarget _characterLightShadowToggle;
    private const int ReShadePresetRowsPerPage = 3;
    private VRWristMenuButtonTarget _reshadeToggleButton;
    private VRWristMenuButtonTarget _reshadePresetDropdownButton;
    private GameObject _reshadePresetDropdownPanel;
    private readonly VRWristMenuButtonTarget[] _reshadePresetOptionButtons =
        new VRWristMenuButtonTarget[ReShadePresetRowsPerPage];
    private VRWristMenuButtonTarget _reshadePresetPreviousButton;
    private VRWristMenuButtonTarget _reshadePresetNextButton;
    private Text _reshadePresetPageText;
    private readonly List<string> _reshadePresetOptions = new List<string>();
    private bool _reshadePresetDropdownOpen;
    private int _reshadePresetPage;
    private Text _backgroundValueText;
    private Text _characterLightIntensityText;
    private Text _reshadeAvailabilityText;
    private readonly Text[] _audioVolumeTexts = new Text[AudioChannels.Length];
    private readonly VRWristMenuButtonTarget[] _audioToggleButtons =
        new VRWristMenuButtonTarget[AudioChannels.Length];
    private WristSettingsSection _settingsSection;
    private float _nextTimelineRefresh;
    private bool _reshadeRefreshLoopRunning;

    private void BuildSettingsPage()
    {
        CreateButton(
            "SettingsBack",
            "<",
            18f,
            10f,
            new Color(1f, 1f, 1f, 0.08f),
            new Color(1f, 1f, 1f, 0.2f),
            HandleBackToRoot,
            _settingsPage.transform,
            58f,
            38f,
            28);
        _settingsTitleText = CreateText(
            "SettingsTitle",
            _settingsPage.transform,
            L("设置", "設定", "Settings"),
            92f,
            10f,
            444f,
            40f,
            28,
            TextAnchor.MiddleLeft,
            Color.white);

        _settingsGeneralTab = CreateButton(
            "SettingsGeneralTab",
            L("通用", "一般", "General"),
            24f,
            62f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            () => ShowSettingsSection(WristSettingsSection.General),
            _settingsPage.transform,
            96f,
            42f,
            15);
        _settingsVisualTab = CreateButton(
            "SettingsVisualTab",
            L("画面", "画面", "Visual"),
            128f,
            62f,
            new Color(0.07f, 0.21f, 0.25f, 0.52f),
            new Color(0.09f, 0.42f, 0.49f, 0.78f),
            () => ShowSettingsSection(WristSettingsSection.Visual),
            _settingsPage.transform,
            96f,
            42f,
            15);
        _settingsReShadeTab = CreateButton(
            "SettingsReShadeTab",
            "ReShade",
            232f,
            62f,
            new Color(0.18f, 0.1f, 0.25f, 0.52f),
            new Color(0.48f, 0.2f, 0.64f, 0.78f),
            () => ShowSettingsSection(WristSettingsSection.ReShade),
            _settingsPage.transform,
            96f,
            42f,
            15);
        _settingsCharacterLightTab = CreateButton(
            "SettingsCharacterLightTab",
            L("角色光", "キャラ光", "Lighting"),
            336f,
            62f,
            new Color(0.28f, 0.2f, 0.07f, 0.48f),
            new Color(0.55f, 0.36f, 0.1f, 0.74f),
            () => ShowSettingsSection(WristSettingsSection.CharacterLight),
            _settingsPage.transform,
            96f,
            42f,
            15);
        _settingsAudioTab = CreateButton(
            "SettingsAudioTab",
            L("声音", "サウンド", "Audio"),
            440f,
            62f,
            new Color(0.075f, 0.24f, 0.14f, 0.5f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            () => ShowSettingsSection(WristSettingsSection.Audio),
            _settingsPage.transform,
            96f,
            42f,
            15);

        _settingsGeneralPanel = CreateRectObject(
            "SettingsGeneralPanel", _settingsPage.transform, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _settingsInteractionPanel = CreateRectObject(
            "SettingsInteractionPanel", _settingsPage.transform, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _settingsMmdPanel = CreateRectObject(
            "SettingsMmdPanel", _settingsPage.transform, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _settingsVisualPanel = CreateRectObject(
            "SettingsVisualPanel", _settingsPage.transform, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _settingsReShadePanel = CreateRectObject(
            "SettingsReShadePanel", _settingsPage.transform, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _settingsCharacterLightPanel = CreateRectObject(
            "SettingsCharacterLightPanel", _settingsPage.transform, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _settingsAudioPanel = CreateRectObject(
            "SettingsAudioPanel", _settingsPage.transform, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);

        BuildGeneralSettings();
        BuildInteractionSettings();
        BuildMmdPlaybackSettings();
        BuildVisualSettings();
        BuildReShadeSettings();
        BuildCharacterLightSettings();
        BuildAudioSettings();
        ShowSettingsSection(WristSettingsSection.General);
    }

    private void BuildGeneralSettings()
    {
        CreateText(
            "LanguageHeading",
            _settingsGeneralPanel.transform,
            L("界面语言", "表示言語", "Interface language"),
            24f,
            122f,
            512f,
            28f,
            20,
            TextAnchor.MiddleLeft,
            new Color(0.25f, 0.86f, 0.94f, 1f));
        CreateText(
            "LanguageDescription",
            _settingsGeneralPanel.transform,
            L(
                "选择后立即应用，并在下次启动时保持",
                "選択後すぐに反映され、次回起動時も保持されます",
                "Applies immediately and is remembered for the next launch"),
            24f,
            154f,
            512f,
            34f,
            16,
            TextAnchor.MiddleLeft,
            new Color(0.72f, 0.82f, 0.88f, 1f));

        _languageChineseButton = CreateButton(
            "LanguageChinese",
            "简体中文",
            24f,
            202f,
            new Color(0.07f, 0.21f, 0.25f, 0.52f),
            new Color(0.09f, 0.42f, 0.49f, 0.78f),
            () => HandleSetLanguage(ChineseLanguage),
            _settingsGeneralPanel.transform,
            160f,
            60f,
            18);
        _languageJapaneseButton = CreateButton(
            "LanguageJapanese",
            "日本語",
            200f,
            202f,
            new Color(0.28f, 0.2f, 0.07f, 0.48f),
            new Color(0.55f, 0.36f, 0.1f, 0.74f),
            () => HandleSetLanguage(JapaneseLanguage),
            _settingsGeneralPanel.transform,
            160f,
            60f,
            18);
        _languageEnglishButton = CreateButton(
            "LanguageEnglish",
            "English",
            376f,
            202f,
            new Color(0.075f, 0.24f, 0.14f, 0.5f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            () => HandleSetLanguage(EnglishLanguage),
            _settingsGeneralPanel.transform,
            160f,
            60f,
            18);

        Image noteBackground = CreateImage(
            "LanguageNoteBackground",
            _settingsGeneralPanel.transform,
            24f,
            286f,
            512f,
            72f,
            new Color(0.7f, 0.84f, 0.94f, 0.065f));
        ApplyGlassEffects(noteBackground, new Color(0.86f, 0.96f, 1f, 0.12f), false, 0f);
        CreateText(
            "LanguageNote",
            _settingsGeneralPanel.transform,
            L(
                "腕表中的标题、按钮和功能说明将统一使用所选语言。",
                "リストメニューの見出し、ボタン、機能説明を選択した言語に統一します。",
                "Titles, buttons, and descriptions use the selected language throughout the wrist menu."),
            40f,
            294f,
            480f,
            56f,
            17,
            TextAnchor.MiddleLeft,
            new Color(0.76f, 0.88f, 0.94f, 1f));

        CreateButton(
            "OpenInteractionSettings",
            L("自定义操作与 Timeline  ›", "操作と Timeline の設定  ›", "Controls & Timeline  ›"),
            24f,
            370f,
            new Color(0.1f, 0.18f, 0.3f, 0.42f),
            new Color(0.15f, 0.38f, 0.64f, 0.72f),
            () => ShowSettingsSection(WristSettingsSection.Interaction),
            _settingsGeneralPanel.transform,
            248f,
            36f,
            15);
        CreateButton(
            "OpenMmdPlaybackSettings",
            L("MMD 播放设置  ›", "MMD 再生設定  ›", "MMD playback  ›"),
            288f,
            370f,
            new Color(0.28f, 0.2f, 0.07f, 0.48f),
            new Color(0.55f, 0.36f, 0.1f, 0.74f),
            () => ShowSettingsSection(WristSettingsSection.Mmd),
            _settingsGeneralPanel.transform,
            248f,
            36f,
            15);
    }

    private void BuildMmdPlaybackSettings()
    {
        CreateButton(
            "MmdPlaybackSettingsBack",
            L("‹  返回通用", "‹  一般へ", "‹  General"),
            432f, 114f,
            new Color(1f, 1f, 1f, 0.08f),
            new Color(1f, 1f, 1f, 0.2f),
            () => ShowSettingsSection(WristSettingsSection.General),
            _settingsMmdPanel.transform, 104f, 38f, 16);
        CreateText(
            "MmdPlaybackSettingsHeading", _settingsMmdPanel.transform,
            L("MMD 播放与展示", "MMD 再生と表示", "MMD playback and presentation"),
            24f, 116f, 392f, 28f, 20,
            TextAnchor.MiddleLeft, new Color(1f, 0.72f, 0.25f, 1f));

        _mmdPresentationHideButton = CreateButton(
            "MmdPresentationHide",
            L("播放时隐藏手柄与 UI", "再生中に手と UI を隠す", "Hide hands and UI during playback"),
            24f, 164f,
            new Color(0.1f, 0.18f, 0.3f, 0.54f),
            new Color(0.15f, 0.38f, 0.64f, 0.8f),
            HandleToggleMmdPresentationHide,
            _settingsMmdPanel.transform, 512f, 58f, 17);
        CreateText(
            "MmdPresentationHideHint", _settingsMmdPanel.transform,
            L(
                "隐藏后：左摇杆播放/暂停；右摇杆上下调 FOV、左右线性旋转；右 A 复位。",
                "非表示中：左スティックで再生/停止、右スティックで FOV/回転、右 A でリセット。",
                "Hidden: left-stick click plays/pauses; right stick adjusts FOV/yaw; right A resets."),
            36f, 226f, 488f, 36f, 14,
            TextAnchor.MiddleLeft, TertiaryTextColor);

        _mmdAutoHighHeelsButton = CreateButton(
            "MmdAutoHighHeels",
            L("自动识别角色高跟鞋缓存", "キャラ別ハイヒール設定を自動読込", "Auto-load character high-heel presets"),
            24f, 270f,
            new Color(0.075f, 0.24f, 0.14f, 0.48f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            HandleToggleMmdAutoHighHeels,
            _settingsMmdPanel.transform, 512f, 52f, 16);

        CreateText(
            "MmdFovSpeedHeading", _settingsMmdPanel.transform,
            L("摇杆 FOV 调整速度", "スティック FOV 調整速度", "Stick FOV adjustment speed"),
            24f, 336f, 250f, 42f, 17,
            TextAnchor.MiddleLeft, Color.white);
        CreateButton(
            "MmdFovSpeedMinus", "−", 286f, 336f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            () => HandleAdjustMmdFovSpeed(-5f),
            _settingsMmdPanel.transform, 54f, 42f, 24);
        _mmdFovSpeedText = CreateText(
            "MmdFovSpeedValue", _settingsMmdPanel.transform,
            "20°/s", 348f, 336f, 118f, 42f, 18,
            TextAnchor.MiddleCenter, PrimaryTextColor);
        CreateButton(
            "MmdFovSpeedPlus", "+", 482f, 336f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            () => HandleAdjustMmdFovSpeed(5f),
            _settingsMmdPanel.transform, 54f, 42f, 24);
    }

    private void BuildInteractionSettings()
    {
        CreateButton(
            "InteractionSettingsBack",
            L("‹  返回通用", "‹  一般へ", "‹  General"),
            432f,
            114f,
            new Color(1f, 1f, 1f, 0.08f),
            new Color(1f, 1f, 1f, 0.2f),
            () => ShowSettingsSection(WristSettingsSection.General),
            _settingsInteractionPanel.transform,
            104f,
            38f,
            16);
        CreateText(
            "ControllerLayoutHeading",
            _settingsInteractionPanel.transform,
            L("手柄快捷键布局", "コントローラーボタン配置", "Controller button layout"),
            24f,
            116f,
            392f,
            26f,
            19,
            TextAnchor.MiddleLeft,
            new Color(0.4f, 0.72f, 1f, 1f));
        CreateText(
            "ControllerLayoutDescription",
            _settingsInteractionPanel.transform,
            L(
                "前键：打开手环\n后键：显示 / 隐藏工作室 GUI",
                "前側：リストを開く\n後側：Studio GUIを表示 / 非表示",
                "First: open wrist menu\nSecond: show / hide Studio GUI"),
            24f,
            142f,
            392f,
            32f,
            14,
            TextAnchor.MiddleLeft,
            new Color(0.68f, 0.78f, 0.86f, 1f));

        _controllerLayoutButtons[0] = CreateButton(
            "ControllerLayoutSplit",
            L("双手：X / A", "両手：X / A", "Split: X / A"),
            24f,
            178f,
            new Color(0.1f, 0.18f, 0.3f, 0.54f),
            new Color(0.15f, 0.38f, 0.64f, 0.8f),
            () => HandleSetControllerLayout(KKCharaStudioVRSettings.ControllerLayoutSplitHands),
            _settingsInteractionPanel.transform,
            160f,
            44f,
            15);
        _controllerLayoutButtons[1] = CreateButton(
            "ControllerLayoutLeft",
            L("左手：X / Y", "左手：X / Y", "Left: X / Y"),
            200f,
            178f,
            new Color(0.1f, 0.18f, 0.3f, 0.54f),
            new Color(0.15f, 0.38f, 0.64f, 0.8f),
            () => HandleSetControllerLayout(KKCharaStudioVRSettings.ControllerLayoutLeftHand),
            _settingsInteractionPanel.transform,
            160f,
            44f,
            15);
        _controllerLayoutButtons[2] = CreateButton(
            "ControllerLayoutRight",
            L("右手：A / B", "右手：A / B", "Right: A / B"),
            376f,
            178f,
            new Color(0.1f, 0.18f, 0.3f, 0.54f),
            new Color(0.15f, 0.38f, 0.64f, 0.8f),
            () => HandleSetControllerLayout(KKCharaStudioVRSettings.ControllerLayoutRightHand),
            _settingsInteractionPanel.transform,
            160f,
            44f,
            15);

        CreateText(
            "MainGuiHeading",
            _settingsInteractionPanel.transform,
            L("主工作室 GUI", "メインStudio GUI", "Main Studio GUI"),
            24f,
            228f,
            512f,
            24f,
            18,
            TextAnchor.MiddleLeft,
            new Color(0.25f, 0.86f, 0.94f, 1f));

        _mainGuiDistanceText = CreateText(
            "MainGuiDistance",
            _settingsInteractionPanel.transform,
            L("距离", "距離", "Distance"),
            24f,
            258f,
            174f,
            40f,
            17,
            TextAnchor.MiddleLeft,
            Color.white);
        CreateButton(
            "MainGuiDistanceMinus",
            "−",
            204f,
            258f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            () => HandleAdjustMainGuiDistance(-0.1f),
            _settingsInteractionPanel.transform,
            54f,
            40f,
            24);
        CreateButton(
            "MainGuiDistancePlus",
            "+",
            266f,
            258f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            () => HandleAdjustMainGuiDistance(0.1f),
            _settingsInteractionPanel.transform,
            54f,
            40f,
            24);

        _mainGuiScaleText = CreateText(
            "MainGuiScale",
            _settingsInteractionPanel.transform,
            L("大小", "サイズ", "Size"),
            24f,
            306f,
            174f,
            40f,
            17,
            TextAnchor.MiddleLeft,
            Color.white);
        CreateButton(
            "MainGuiScaleMinus",
            "−",
            204f,
            306f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            () => HandleAdjustMainGuiScale(-0.1f),
            _settingsInteractionPanel.transform,
            54f,
            40f,
            24);
        CreateButton(
            "MainGuiScalePlus",
            "+",
            266f,
            306f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            () => HandleAdjustMainGuiScale(0.1f),
            _settingsInteractionPanel.transform,
            54f,
            40f,
            24);
        CreateButton(
            "MainGuiApply",
            L("召回\n主界面", "メインGUI\n呼び戻す", "Recall\nmain GUI"),
            338f,
            258f,
            new Color(0.075f, 0.24f, 0.14f, 0.5f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            HandleApplyMainGuiPlacement,
            _settingsInteractionPanel.transform,
            198f,
            88f,
            17);

        CreateText(
            "TimelineCameraHeading",
            _settingsInteractionPanel.transform,
            L("Timeline 镜头", "Timeline カメラ", "Timeline camera"),
            24f,
            362f,
            182f,
            42f,
            17,
            TextAnchor.MiddleLeft,
            new Color(0.72f, 0.82f, 0.9f, 1f));
        _timelineCameraFollowButton = CreateButton(
            "TimelineCameraFollow",
            L("跟随摄像机", "カメラに追従", "Follow camera"),
            214f,
            362f,
            new Color(0.12f, 0.17f, 0.3f, 0.5f),
            new Color(0.18f, 0.36f, 0.62f, 0.8f),
            HandleToggleTimelineCameraFollow,
            _settingsInteractionPanel.transform,
            322f,
            42f,
            16);
    }

    private void BuildVisualSettings()
    {
        CreateText(
            "BackgroundHeading",
            _settingsVisualPanel.transform,
            L("背景颜色", "背景色", "Background color"),
            24f,
            122f,
            512f,
            28f,
            19,
            TextAnchor.MiddleLeft,
            new Color(0.25f, 0.86f, 0.94f, 1f));

        const float swatchWidth = 76f;
        const float swatchSpacing = 87.2f;
        for (int i = 0; i < VRStudioSettingsService.BackgroundPresetCount; i++)
        {
            int preset = i;
            Color color = VRStudioSettingsService.GetBackgroundPresetColor(i);
            CreateButton(
                "BackgroundPreset" + i,
                GetBackgroundPresetLabel(i),
                24f + i * swatchSpacing,
                160f,
                Darken(color, 0.78f),
                Brighten(color, 0.18f),
                () => HandleBackgroundPreset(preset),
                _settingsVisualPanel.transform,
                swatchWidth,
                70f,
                16);
        }

        Image valueBackground = CreateImage(
            "BackgroundValuePanel",
            _settingsVisualPanel.transform,
            24f,
            252f,
            512f,
            58f,
            new Color(0.7f, 0.84f, 0.94f, 0.065f));
        ApplyGlassEffects(valueBackground, new Color(0.86f, 0.96f, 1f, 0.12f), false, 0f);
        _backgroundValueText = CreateText(
            "BackgroundValue",
            _settingsVisualPanel.transform,
            L("当前背景色", "現在の背景色", "Current background"),
            38f,
            258f,
            484f,
            46f,
            18,
            TextAnchor.MiddleLeft,
            new Color(0.76f, 0.88f, 0.94f, 1f));
    }

    private void BuildReShadeSettings()
    {
        CreateText(
            "ReShadeHeading",
            _settingsReShadePanel.transform,
            L("ReShade 画面效果", "ReShade 画面効果", "ReShade effects"),
            24f,
            118f,
            512f,
            28f,
            19,
            TextAnchor.MiddleLeft,
            new Color(0.78f, 0.52f, 1f, 1f));
        CreateText(
            "ReShadeDescription",
            _settingsReShadePanel.transform,
            L(
                "单独开关效果，预设选择不会再绑定画质档位。",
                "効果のオン・オフとプリセットを個別に選択できます。",
                "Toggle effects and choose a preset independently."),
            24f,
            148f,
            512f,
            38f,
            15,
            TextAnchor.MiddleLeft,
            new Color(0.72f, 0.82f, 0.88f, 1f));

        _reshadeToggleButton = CreateButton(
            "ReShadeToggle",
            L("ReShade  开", "ReShade  オン", "ReShade  On"),
            24f,
            196f,
            new Color(0.075f, 0.24f, 0.14f, 0.5f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            HandleToggleReShade,
            _settingsReShadePanel.transform,
            160f,
            56f,
            17);
        _reshadePresetDropdownButton = CreateButton(
            "ReShadePresetDropdown",
            L("预设  --  ▼", "プリセット  --  ▼", "Preset  --  ▼"),
            196f,
            196f,
            new Color(0.18f, 0.1f, 0.25f, 0.52f),
            new Color(0.48f, 0.2f, 0.64f, 0.78f),
            HandleToggleReShadePresetDropdown,
            _settingsReShadePanel.transform,
            340f,
            56f,
            16);

        _reshadeAvailabilityText = CreateText(
            "ReShadeAvailability",
            _settingsReShadePanel.transform,
            L("正在检测 ReShade…", "ReShade を確認中…", "Checking ReShade…"),
            24f,
            270f,
            512f,
            70f,
            15,
            TextAnchor.MiddleLeft,
            new Color(0.72f, 0.82f, 0.88f, 1f));

        CreateText(
            "ReShadeDesktopHint",
            _settingsReShadePanel.transform,
            L(
                "桌面测试：F1 打开插件设置；F8 开关，F9 切换预设。",
                "デスクトップ：F1 設定、F8 オン/オフ、F9 プリセット切替。",
                "Desktop: F1 settings, F8 toggle, F9 next preset."),
            24f,
            352f,
            512f,
            40f,
            14,
            TextAnchor.MiddleLeft,
            new Color(0.62f, 0.72f, 0.8f, 1f));

        BuildReShadePresetDropdown();
    }

    private void BuildReShadePresetDropdown()
    {
        _reshadePresetDropdownPanel = CreateRectObject(
            "ReShadePresetDropdownPanel",
            _settingsReShadePanel.transform,
            196f,
            258f,
            340f,
            148f,
            _visibleLayer);
        Image background = CreateImage(
            "ReShadePresetDropdownBackground",
            _reshadePresetDropdownPanel.transform,
            0f,
            0f,
            340f,
            148f,
            new Color(0.035f, 0.045f, 0.065f, 0.98f));
        ApplyGlassEffects(background, new Color(0.78f, 0.52f, 1f, 0.28f), true, 3f);

        for (int i = 0; i < ReShadePresetRowsPerPage; i++)
        {
            int optionIndex = i;
            _reshadePresetOptionButtons[i] = CreateButton(
                "ReShadePresetOption" + i,
                "--",
                6f,
                6f + i * 36f,
                new Color(0.08f, 0.1f, 0.14f, 0.96f),
                new Color(0.34f, 0.16f, 0.46f, 0.98f),
                () => HandleSelectReShadePresetOption(optionIndex),
                _reshadePresetDropdownPanel.transform,
                328f,
                32f,
                14);
        }

        _reshadePresetPreviousButton = CreateButton(
            "ReShadePresetPrevious",
            "‹",
            6f,
            116f,
            new Color(0.08f, 0.1f, 0.14f, 0.96f),
            new Color(0.34f, 0.16f, 0.46f, 0.98f),
            () => HandleChangeReShadePresetPage(-1),
            _reshadePresetDropdownPanel.transform,
            54f,
            26f,
            17);
        _reshadePresetPageText = CreateText(
            "ReShadePresetPage",
            _reshadePresetDropdownPanel.transform,
            "1 / 1",
            68f,
            116f,
            204f,
            26f,
            13,
            TextAnchor.MiddleCenter,
            new Color(0.78f, 0.84f, 0.92f, 1f));
        _reshadePresetNextButton = CreateButton(
            "ReShadePresetNext",
            "›",
            280f,
            116f,
            new Color(0.08f, 0.1f, 0.14f, 0.96f),
            new Color(0.34f, 0.16f, 0.46f, 0.98f),
            () => HandleChangeReShadePresetPage(1),
            _reshadePresetDropdownPanel.transform,
            54f,
            26f,
            17);
        _reshadePresetDropdownPanel.SetActive(false);
    }

    private void BuildCharacterLightSettings()
    {
        CreateText(
            "CharacterLightHeading",
            _settingsCharacterLightPanel.transform,
            L("角色光", "キャラクターライト", "Character light"),
            24f,
            118f,
            512f,
            28f,
            19,
            TextAnchor.MiddleLeft,
            new Color(1f, 0.72f, 0.25f, 1f));

        _characterLightToggle = CreateButton(
            "CharacterLightToggle",
            L("角色光", "キャラ光", "Character light"),
            24f,
            154f,
            new Color(0.28f, 0.2f, 0.07f, 0.48f),
            new Color(0.55f, 0.36f, 0.1f, 0.74f),
            HandleToggleCharacterLight,
            _settingsCharacterLightPanel.transform,
            150f,
            54f,
            17);
        CreateButton(
            "CharacterLightMinus",
            "-",
            186f,
            154f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            () => HandleAdjustCharacterLight(-0.1f),
            _settingsCharacterLightPanel.transform,
            54f,
            54f,
            26);
        _characterLightIntensityText = CreateText(
            "CharacterLightIntensity",
            _settingsCharacterLightPanel.transform,
            "1.0",
            248f,
            154f,
            82f,
            54f,
            22,
            TextAnchor.MiddleCenter,
            Color.white);
        CreateButton(
            "CharacterLightPlus",
            "+",
            338f,
            154f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            () => HandleAdjustCharacterLight(0.1f),
            _settingsCharacterLightPanel.transform,
            54f,
            54f,
            26);
        _characterLightShadowToggle = CreateButton(
            "CharacterLightShadow",
            L("阴影", "影", "Shadows"),
            404f,
            154f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            HandleToggleCharacterLightShadow,
            _settingsCharacterLightPanel.transform,
            132f,
            54f,
            17);

        CreateText(
            "CharacterLightColorHeading",
            _settingsCharacterLightPanel.transform,
            L("灯光颜色", "ライト色", "Light color"),
            24f,
            226f,
            512f,
            25f,
            18,
            TextAnchor.MiddleLeft,
            new Color(0.72f, 0.82f, 0.88f, 1f));

        const float swatchWidth = 94f;
        for (int i = 0; i < VRStudioSettingsService.CharacterLightPresetCount; i++)
        {
            int preset = i;
            Color color = VRStudioSettingsService.GetCharacterLightPresetColor(i);
            CreateButton(
                "CharacterLightPreset" + i,
                GetCharacterLightPresetLabel(i),
                24f + i * 104.5f,
                260f,
                Darken(color, 0.58f),
                Brighten(color, 0.08f),
                () => HandleCharacterLightPreset(preset),
                _settingsCharacterLightPanel.transform,
                swatchWidth,
                64f,
                17);
        }
    }

    private void BuildAudioSettings()
    {
        CreateText(
            "AudioHeading",
            _settingsAudioPanel.transform,
            L("声音配置", "サウンド設定", "Audio settings"),
            24f,
            112f,
            512f,
            26f,
            19,
            TextAnchor.MiddleLeft,
            new Color(0.47f, 0.9f, 0.55f, 1f));

        for (int i = 0; i < AudioChannels.Length; i++)
        {
            VRAudioChannel channel = AudioChannels[i];
            float y = 146f + i * 49f;
            CreateText(
                "AudioName" + i,
                _settingsAudioPanel.transform,
                GetAudioLabel(channel),
                24f,
                y,
                118f,
                40f,
                16,
                TextAnchor.MiddleLeft,
                Color.white);
            CreateButton(
                "AudioMinus" + i,
                "-",
                146f,
                y,
                new Color(0.08f, 0.15f, 0.18f, 0.52f),
                new Color(0.12f, 0.32f, 0.39f, 0.76f),
                () => HandleAdjustAudio(channel, -10),
                _settingsAudioPanel.transform,
                50f,
                40f,
                23);
            _audioVolumeTexts[i] = CreateText(
                "AudioVolume" + i,
                _settingsAudioPanel.transform,
                "100%",
                202f,
                y,
                78f,
                40f,
                17,
                TextAnchor.MiddleCenter,
                new Color(0.76f, 0.88f, 0.94f, 1f));
            CreateButton(
                "AudioPlus" + i,
                "+",
                286f,
                y,
                new Color(0.08f, 0.15f, 0.18f, 0.52f),
                new Color(0.12f, 0.32f, 0.39f, 0.76f),
                () => HandleAdjustAudio(channel, 10),
                _settingsAudioPanel.transform,
                50f,
                40f,
                23);
            _audioToggleButtons[i] = CreateButton(
                "AudioToggle" + i,
                L("开启", "オン", "On"),
                344f,
                y,
                new Color(0.075f, 0.24f, 0.14f, 0.5f),
                new Color(0.11f, 0.46f, 0.24f, 0.76f),
                () => HandleToggleAudio(channel),
                _settingsAudioPanel.transform,
                192f,
                40f,
                16);
        }
    }

    private void HandleOpenSettings()
    {
        ShowPage(WristMenuPage.Settings);
        ShowSettingsSection(_settingsSection);
        SetStatus(L("设置", "設定", "Settings"), new Color(0.25f, 0.86f, 0.94f, 1f), 0f);
    }

    private void ShowSettingsSection(WristSettingsSection section)
    {
        _settingsSection = section;
        if (_settingsTitleText != null)
        {
            _settingsTitleText.text = section == WristSettingsSection.Interaction
                ? L("设置 / 操作", "設定 / 操作", "Settings / Controls")
                : section == WristSettingsSection.Mmd
                    ? L("设置 / MMD", "設定 / MMD", "Settings / MMD")
                    : L("设置", "設定", "Settings");
        }
        if (section != WristSettingsSection.ReShade)
            SetReShadePresetDropdownOpen(false);
        if (_settingsGeneralPanel != null)
            _settingsGeneralPanel.SetActive(section == WristSettingsSection.General);
        if (_settingsInteractionPanel != null)
            _settingsInteractionPanel.SetActive(section == WristSettingsSection.Interaction);
        if (_settingsMmdPanel != null)
            _settingsMmdPanel.SetActive(section == WristSettingsSection.Mmd);
        if (_settingsVisualPanel != null)
            _settingsVisualPanel.SetActive(section == WristSettingsSection.Visual);
        if (_settingsReShadePanel != null)
            _settingsReShadePanel.SetActive(section == WristSettingsSection.ReShade);
        if (_settingsCharacterLightPanel != null)
            _settingsCharacterLightPanel.SetActive(section == WristSettingsSection.CharacterLight);
        if (_settingsAudioPanel != null)
            _settingsAudioPanel.SetActive(section == WristSettingsSection.Audio);

        if (_settingsGeneralTab != null)
            _settingsGeneralTab.SetLabel(
                section == WristSettingsSection.General
                    || section == WristSettingsSection.Interaction
                    || section == WristSettingsSection.Mmd
                    ? L("通用  ●", "一般  ●", "General  ●")
                    : L("通用", "一般", "General"));
        if (_settingsVisualTab != null)
            _settingsVisualTab.SetLabel(
                section == WristSettingsSection.Visual
                    ? L("画面  ●", "画面  ●", "Visual  ●")
                    : L("画面", "画面", "Visual"));
        if (_settingsReShadeTab != null)
            _settingsReShadeTab.SetLabel(
                section == WristSettingsSection.ReShade ? "ReShade  ●" : "ReShade");
        if (_settingsCharacterLightTab != null)
            _settingsCharacterLightTab.SetLabel(
                section == WristSettingsSection.CharacterLight
                    ? L("角色光  ●", "キャラ光  ●", "Lighting  ●")
                    : L("角色光", "キャラ光", "Lighting"));
        if (_settingsAudioTab != null)
            _settingsAudioTab.SetLabel(
                section == WristSettingsSection.Audio
                    ? L("声音  ●", "サウンド  ●", "Audio  ●")
                    : L("声音", "サウンド", "Audio"));
        RefreshSettingsPage();
        if (section == WristSettingsSection.ReShade)
            StartReShadeRefreshLoop();
    }

    private void RefreshSettingsPage()
    {
        switch (_settingsSection)
        {
            case WristSettingsSection.General:
                RefreshLanguageSettings();
                break;
            case WristSettingsSection.Interaction:
                RefreshInteractionSettings();
                break;
            case WristSettingsSection.Mmd:
                RefreshMmdPlaybackSettings();
                break;
            case WristSettingsSection.Visual:
                int preset = VRStudioSettingsService.FindClosestBackgroundPreset();
                if (_backgroundValueText != null)
                    _backgroundValueText.text = L("当前背景色  ", "現在の背景色  ", "Current background  ")
                        + GetBackgroundPresetLabel(preset);
                break;
            case WristSettingsSection.ReShade:
                RefreshReShadeSettings();
                break;
            case WristSettingsSection.CharacterLight:
                RefreshCharacterLightSettings();
                break;
            case WristSettingsSection.Audio:
                RefreshAudioSettings();
                break;
        }
    }

    private void RefreshMmdPlaybackSettings()
    {
        ResolveSettings();
        if (_settings == null)
            return;
        if (_mmdPresentationHideButton != null)
        {
            _mmdPresentationHideButton.SetLabel(_settings.HideHandsAndUiDuringMmd
                ? L("播放时隐藏手柄与 UI  开", "再生中に手と UI を隠す  オン", "Hide hands and UI during playback  On")
                : L("播放时隐藏手柄与 UI  关", "再生中に手と UI を隠す  オフ", "Hide hands and UI during playback  Off"));
        }
        if (_mmdAutoHighHeelsButton != null)
        {
            _mmdAutoHighHeelsButton.SetLabel(_settings.AutoApplyHighHeelsPreset
                ? L("自动识别角色高跟鞋缓存  开", "ハイヒール設定の自動読込  オン", "Auto-load high-heel presets  On")
                : L("自动识别角色高跟鞋缓存  关", "ハイヒール設定の自動読込  オフ", "Auto-load high-heel presets  Off"));
        }
        if (_mmdFovSpeedText != null)
            _mmdFovSpeedText.text = _settings.MmdFovAdjustSpeed.ToString("0") + "°/s";
    }

    private void HandleToggleMmdPresentationHide()
    {
        ResolveSettings();
        if (_settings == null)
            return;
        _settings.HideHandsAndUiDuringMmd = !_settings.HideHandsAndUiDuringMmd;
        bool saved = TrySaveInteractionSettings();
        RefreshMmdPlaybackSettings();
        SetStatus(
            _settings.HideHandsAndUiDuringMmd
                ? L("MMD 展示隐藏已开启", "MMD 表示非表示を有効化しました", "MMD presentation hiding enabled")
                : L("MMD 展示隐藏已关闭", "MMD 表示非表示を無効化しました", "MMD presentation hiding disabled"),
            saved ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.72f, 0.25f, 1f),
            saved ? 4f : 7f);
    }

    private void HandleToggleMmdAutoHighHeels()
    {
        ResolveSettings();
        if (_settings == null)
            return;
        _settings.AutoApplyHighHeelsPreset = !_settings.AutoApplyHighHeelsPreset;
        bool saved = TrySaveInteractionSettings();
        if (_settings.AutoApplyHighHeelsPreset && VRMmdPlaybackController.Instance != null)
            VRMmdPlaybackController.Instance.RequestHighHeelsRefresh(VRVmdTargetService.GetSelectedObjectKeys());
        RefreshMmdPlaybackSettings();
        SetStatus(
            _settings.AutoApplyHighHeelsPreset
                ? L("高跟鞋缓存自动识别已开启", "ハイヒール設定の自動読込を有効化しました", "Automatic high-heel preset loading enabled")
                : L("高跟鞋缓存自动识别已关闭", "ハイヒール設定の自動読込を無効化しました", "Automatic high-heel preset loading disabled"),
            saved ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.72f, 0.25f, 1f),
            saved ? 4f : 7f);
    }

    private void HandleAdjustMmdFovSpeed(float delta)
    {
        ResolveSettings();
        if (_settings == null)
            return;
        _settings.MmdFovAdjustSpeed = _settings.MmdFovAdjustSpeed + delta;
        bool saved = TrySaveInteractionSettings();
        RefreshMmdPlaybackSettings();
        SetStatus(
            L("FOV 调整速度：", "FOV 調整速度：", "FOV adjustment speed: ")
                + _settings.MmdFovAdjustSpeed.ToString("0") + "°/s",
            saved ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.72f, 0.25f, 1f),
            saved ? 3f : 7f);
    }

    private void RefreshTimelineButton()
    {
        if (_timelineButton == null)
            return;
        bool isPlaying;
        if (!VRTimelineService.TryGetIsPlaying(out isPlaying))
        {
            _timelineButton.SetLabel(L("时间轴\n不可用", "タイムライン\n利用不可", "Timeline\nUnavailable"));
            return;
        }
        _timelineButton.SetLabel(
            isPlaying
                ? L("时间轴\n暂停", "タイムライン\n一時停止", "Timeline\nPause")
                : L("时间轴\n播放", "タイムライン\n再生", "Timeline\nPlay"));
    }

    private void RefreshCharacterLightSettings()
    {
        float intensity;
        bool shadow;
        Color color;
        bool available = VRStudioSettingsService.TryGetCharacterLight(out intensity, out shadow, out color);
        if (_characterLightToggle != null)
            _characterLightToggle.SetLabel(
                available && intensity > 0.01f
                    ? L("角色光  开", "キャラ光  オン", "Character light  On")
                    : L("角色光  关", "キャラ光  オフ", "Character light  Off"));
        if (_characterLightIntensityText != null)
            _characterLightIntensityText.text = available ? intensity.ToString("F1") : "--";
        if (_characterLightShadowToggle != null)
            _characterLightShadowToggle.SetLabel(
                available && shadow
                    ? L("阴影  开", "影  オン", "Shadows  On")
                    : L("阴影  关", "影  オフ", "Shadows  Off"));
    }

    private void RefreshAudioSettings()
    {
        for (int i = 0; i < AudioChannels.Length; i++)
        {
            int volume;
            bool enabled;
            bool available = VRStudioSettingsService.TryGetAudio(AudioChannels[i], out volume, out enabled);
            if (_audioVolumeTexts[i] != null)
                _audioVolumeTexts[i].text = available ? volume + "%" : "--";
            if (_audioToggleButtons[i] != null)
                _audioToggleButtons[i].SetLabel(
                    available && enabled
                        ? L("开启", "オン", "On")
                        : L("静音", "ミュート", "Mute"));
        }
    }

    private void RefreshLanguageSettings()
    {
        string language = CurrentLanguage;
        if (_languageChineseButton != null)
            _languageChineseButton.SetLabel(language == ChineseLanguage ? "简体中文  ●" : "简体中文");
        if (_languageJapaneseButton != null)
            _languageJapaneseButton.SetLabel(language == JapaneseLanguage ? "日本語  ●" : "日本語");
        if (_languageEnglishButton != null)
            _languageEnglishButton.SetLabel(language == EnglishLanguage ? "English  ●" : "English");
    }

    private void RefreshInteractionSettings()
    {
        ResolveSettings();
        if (_settings == null)
            return;

        string[] layouts =
        {
            KKCharaStudioVRSettings.ControllerLayoutSplitHands,
            KKCharaStudioVRSettings.ControllerLayoutLeftHand,
            KKCharaStudioVRSettings.ControllerLayoutRightHand
        };
        for (int i = 0; i < _controllerLayoutButtons.Length; i++)
        {
            VRWristMenuButtonTarget button = _controllerLayoutButtons[i];
            if (button == null)
                continue;
            bool selected = _settings.ControllerFaceButtonLayout == layouts[i];
            button.SetLabel(GetControllerLayoutLabel(i) + (selected ? "  ●" : string.Empty));
            button.SetColors(
                selected
                    ? new Color(0.1f, 0.28f, 0.48f, 0.7f)
                    : new Color(0.1f, 0.18f, 0.3f, 0.54f),
                selected
                    ? new Color(0.18f, 0.48f, 0.78f, 0.9f)
                    : new Color(0.15f, 0.38f, 0.64f, 0.8f));
        }

        if (_mainGuiDistanceText != null)
        {
            _mainGuiDistanceText.text = L("距离  ", "距離  ", "Distance  ")
                + Mathf.Clamp(
                    _settings.UISpawnDistance,
                    VRCameraMoveHelper.MinMainUIDistance,
                    VRCameraMoveHelper.MaxMainUIDistance).ToString("F1")
                + " m";
        }
        if (_mainGuiScaleText != null)
        {
            _mainGuiScaleText.text = L("大小  ", "サイズ  ", "Size  ")
                + Mathf.Clamp(
                    _settings.UISpawnScale,
                    VRCameraMoveHelper.MinMainUIScale,
                    VRCameraMoveHelper.MaxMainUIScale).ToString("F1")
                + "×";
        }
        if (_timelineCameraFollowButton != null)
        {
            bool follows = _settings.TimelineFollowCamera;
            _timelineCameraFollowButton.SetLabel(
                follows
                    ? L("跟随摄像机  ●", "カメラに追従  ●", "Follow camera  ●")
                    : L("只播放动画", "アニメのみ再生", "Animation only"));
            _timelineCameraFollowButton.SetColors(
                follows
                    ? new Color(0.12f, 0.24f, 0.44f, 0.64f)
                    : new Color(0.075f, 0.24f, 0.14f, 0.5f),
                follows
                    ? new Color(0.18f, 0.42f, 0.72f, 0.86f)
                    : new Color(0.11f, 0.46f, 0.24f, 0.76f));
        }
    }

    private VRReShadeSnapshot RefreshReShadeSettings()
    {
        ResolveSettings();
        VRReShadeSnapshot snapshot = VRReShadeRuntimeService.GetSnapshot();
        bool enabled = GetDesiredReShadeEnabled(snapshot);

        if (_reshadeToggleButton != null)
        {
            _reshadeToggleButton.SetLabel(
                enabled
                    ? L("ReShade  开  ●", "ReShade  オン  ●", "ReShade  On  ●")
                    : L("ReShade  关", "ReShade  オフ", "ReShade  Off"));
            _reshadeToggleButton.SetColors(
                enabled
                    ? new Color(0.075f, 0.24f, 0.14f, 0.5f)
                    : new Color(0.12f, 0.16f, 0.2f, 0.56f),
                enabled
                    ? new Color(0.11f, 0.46f, 0.24f, 0.76f)
                    : new Color(0.22f, 0.34f, 0.42f, 0.8f));
        }

        bool savedEnabled;
        string savedPresetPath;
        KKCharaStudioVRPlugin.TryGetSavedReShadeSettings(out savedEnabled, out savedPresetPath);
        string presetPath = VRReShadeRuntimeService.GetDisplayPresetPath(snapshot, savedPresetPath);
        if (_reshadePresetDropdownButton != null)
        {
            _reshadePresetDropdownButton.SetLabel(
                L("预设  ", "プリセット  ", "Preset  ")
                + VRReShadeRuntimeService.GetPresetDisplayName(presetPath, 27)
                + (_reshadePresetDropdownOpen ? "  ▲" : "  ▼"));
        }
        RefreshReShadePresetDropdown(presetPath);

        if (_reshadeAvailabilityText == null)
            return snapshot;

        Color color;
        string label;
        switch (snapshot.ConnectionState)
        {
            case VRReShadeConnectionState.NotInstalled:
                color = new Color(1f, 0.38f, 0.34f, 1f);
                label = L(
                    "未检测到 ReShade，开关与预设不可用。",
                    "ReShade が見つからないため、操作できません。",
                    "ReShade was not detected; controls are unavailable.");
                break;
            case VRReShadeConnectionState.NotLoaded:
                color = new Color(1f, 0.72f, 0.25f, 1f);
                label = L(
                    "ReShade 已安装，但本次没有随工作室加载。",
                    "ReShade はインストール済みですが、今回は読み込まれていません。",
                    "ReShade is installed but was not loaded for this session.");
                break;
            case VRReShadeConnectionState.BridgeMissing:
                color = new Color(1f, 0.38f, 0.34f, 1f);
                label = L(
                    "缺少 ReShade 控制桥；安装后重启工作室。",
                    "ReShade 制御ブリッジがありません。導入後にStudioを再起動してください。",
                    "The ReShade control bridge is missing; install it and restart Studio.");
                break;
            case VRReShadeConnectionState.BridgeUnavailable:
                color = new Color(1f, 0.72f, 0.25f, 1f);
                label = L(
                    "控制桥未在本次启动加载，请重启工作室。",
                    "制御ブリッジが今回読み込まれていません。Studioを再起動してください。",
                    "The bridge was not loaded this session; restart Studio.");
                break;
            case VRReShadeConnectionState.WaitingForRuntime:
                color = new Color(1f, 0.72f, 0.25f, 1f);
                label = L(
                    "控制桥已就绪；等待工作室画面开始渲染。",
                    "制御ブリッジは準備完了。画面の描画を待っています。",
                    "The bridge is ready and waiting for a rendered view.");
                break;
            default:
                color = new Color(0.35f, 1f, 0.62f, 1f);
                if (snapshot.RequestPending)
                {
                    label = L(
                        "请求已发送，正在应用到画面…",
                        "要求を送信しました。画面に適用中です…",
                        "Request sent; applying it to the rendered view…");
                }
                else if (snapshot.PresetState == 1)
                {
                    color = new Color(1f, 0.72f, 0.25f, 1f);
                    label = L(
                        "多个画面的预设不一致，请重新选择一个预设。",
                        "複数画面のプリセットが一致しません。選び直してください。",
                        "Rendered views use different presets; select one again.");
                }
                else if (snapshot.EffectsState == 2)
                {
                    color = new Color(1f, 0.72f, 0.25f, 1f);
                    label = L(
                        "多个画面的效果状态不一致，请重新切换一次。",
                        "複数画面の効果状態が一致しません。切り替え直してください。",
                        "Rendered views use different effect states; toggle once more.");
                }
                else
                {
                    label = L("已连接 ", "接続済み ", "Connected to ")
                        + snapshot.RuntimeCount
                        + L(
                            snapshot.EffectsState == 0 ? " 个画面 · 效果关闭" : " 个画面 · 效果开启",
                            snapshot.EffectsState == 0 ? " 画面 · エフェクト オフ" : " 画面 · エフェクト オン",
                            snapshot.EffectsState == 0 ? " view(s) · Effects off" : " view(s) · Effects on");
                    if (snapshot.VRRuntimeCount > 0)
                        label += L("（其中 VR ", "（VR ", " (VR: ") + snapshot.VRRuntimeCount + L(" 个）", "）", ")");
                }
                break;
        }

        _reshadeAvailabilityText.text = label;
        _reshadeAvailabilityText.color = color;
        return snapshot;
    }

    private bool GetDesiredReShadeEnabled(VRReShadeSnapshot snapshot)
    {
        bool savedEnabled;
        string ignoredPreset;
        if (KKCharaStudioVRPlugin.TryGetSavedReShadeSettings(out savedEnabled, out ignoredPreset))
            return savedEnabled;
        if (snapshot != null && snapshot.EffectsState == 0)
            return false;
        if (snapshot != null && snapshot.EffectsState == 1)
            return true;
        return true;
    }

    private void RefreshReShadePresetDropdown(string currentPresetPath)
    {
        _reshadePresetOptions.Clear();
        _reshadePresetOptions.AddRange(VRReShadeRuntimeService.GetPresetFiles());
        int pageCount = Mathf.Max(1, Mathf.CeilToInt(_reshadePresetOptions.Count / (float)ReShadePresetRowsPerPage));
        _reshadePresetPage = Mathf.Clamp(_reshadePresetPage, 0, pageCount - 1);

        for (int row = 0; row < ReShadePresetRowsPerPage; row++)
        {
            VRWristMenuButtonTarget button = _reshadePresetOptionButtons[row];
            if (button == null)
                continue;
            int index = _reshadePresetPage * ReShadePresetRowsPerPage + row;
            bool visible = index < _reshadePresetOptions.Count;
            button.SetVisible(visible);
            if (!visible)
                continue;
            string path = _reshadePresetOptions[index];
            button.SetLabel(
                (VRReShadeRuntimeService.PathsEqual(path, currentPresetPath) ? "●  " : string.Empty)
                + VRReShadeRuntimeService.GetPresetDisplayName(path, 35));
        }

        if (_reshadePresetPageText != null)
            _reshadePresetPageText.text = (_reshadePresetPage + 1) + " / " + pageCount;
        if (_reshadePresetPreviousButton != null)
            _reshadePresetPreviousButton.SetVisible(pageCount > 1);
        if (_reshadePresetNextButton != null)
            _reshadePresetNextButton.SetVisible(pageCount > 1);
    }

    private void HandleToggleReShadePresetDropdown()
    {
        SetReShadePresetDropdownOpen(!_reshadePresetDropdownOpen);
        RefreshReShadeSettings();
    }

    private void SetReShadePresetDropdownOpen(bool open)
    {
        _reshadePresetDropdownOpen = open && _reshadePresetDropdownPanel != null;
        if (_reshadePresetDropdownPanel != null)
            _reshadePresetDropdownPanel.SetActive(_reshadePresetDropdownOpen);
    }

    private void HandleChangeReShadePresetPage(int direction)
    {
        int pageCount = Mathf.Max(1, Mathf.CeilToInt(_reshadePresetOptions.Count / (float)ReShadePresetRowsPerPage));
        _reshadePresetPage += direction;
        if (_reshadePresetPage < 0)
            _reshadePresetPage = pageCount - 1;
        else if (_reshadePresetPage >= pageCount)
            _reshadePresetPage = 0;
        RefreshReShadeSettings();
    }

    private void HandleSelectReShadePresetOption(int row)
    {
        int index = _reshadePresetPage * ReShadePresetRowsPerPage + row;
        if (index < 0 || index >= _reshadePresetOptions.Count)
            return;
        string path = _reshadePresetOptions[index];
        SetReShadePresetDropdownOpen(false);
        HandleSelectReShadePreset(path);
    }

    private void StartReShadeRefreshLoop()
    {
        if (_reshadeRefreshLoopRunning)
            return;
        _reshadeRefreshLoopRunning = true;
        StartCoroutine(RefreshReShadeUntilSettled());
    }

    private System.Collections.IEnumerator RefreshReShadeUntilSettled()
    {
        float deadline = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (_page != WristMenuPage.Settings || _settingsSection != WristSettingsSection.ReShade)
                break;

            VRReShadeSnapshot snapshot = RefreshReShadeSettings();
            if (!snapshot.RequestPending
                && snapshot.ConnectionState != VRReShadeConnectionState.WaitingForRuntime)
            {
                break;
            }
            yield return new WaitForSecondsRealtime(0.25f);
        }
        _reshadeRefreshLoopRunning = false;
    }

    private void HandleToggleReShade()
    {
        ResolveSettings();
        if (_settings == null)
        {
            SetStatus(
                L("VR 设置尚未初始化", "VR設定が初期化されていません", "VR settings are not initialized"),
                new Color(1f, 0.38f, 0.34f, 1f),
                6f);
            return;
        }

        VRReShadeSnapshot snapshot = VRReShadeRuntimeService.GetSnapshot();
        bool enabled = !GetDesiredReShadeEnabled(snapshot);
        VRReShadeConnectionState connectionState;
        bool queuedUntilRuntime;
        string detail;
        bool success = VRReShadeRuntimeService.TrySetEffectsEnabled(
            enabled, out connectionState, out queuedUntilRuntime, out detail);
        if (!success)
        {
            SetStatus(
                GetReShadeFailureLabel(connectionState, detail),
                new Color(1f, 0.38f, 0.34f, 1f),
                8f);
            RefreshReShadeSettings();
            return;
        }

        bool savedState;
        string savedPresetPath;
        KKCharaStudioVRPlugin.TryGetSavedReShadeSettings(out savedState, out savedPresetPath);
        bool saved = SaveReShadePreference(enabled, savedPresetPath);
        string status = queuedUntilRuntime
            ? L(
                "开关已保存，等待 ReShade 画面运行时",
                "設定を保存し、ReShade ランタイムを待機しています",
                "Toggle saved; waiting for a ReShade runtime")
            : enabled
                ? L("已发送开启请求，正在回读状态", "オン要求を送信しました", "Enable request sent; verifying")
                : L("已发送关闭请求，正在回读状态", "オフ要求を送信しました", "Disable request sent; verifying");
        if (!saved)
        {
            status += L(
                "；但无法保存到下次启动",
                "；次回起動用に保存できませんでした",
                "; it could not be saved for the next launch");
        }

        SetStatus(
            status,
            saved ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.72f, 0.25f, 1f),
            saved ? 5f : 8f);
        RefreshReShadeSettings();
        StartReShadeRefreshLoop();
    }

    private void HandleSelectReShadePreset(string presetPath)
    {
        ResolveSettings();
        if (_settings == null)
            return;

        VRReShadeSnapshot snapshot = VRReShadeRuntimeService.GetSnapshot();
        bool enabled = GetDesiredReShadeEnabled(snapshot);
        string resolvedPath;
        VRReShadeConnectionState connectionState;
        bool queuedUntilRuntime;
        string detail;
        bool success = VRReShadeRuntimeService.TrySelectPreset(
            presetPath,
            enabled,
            out resolvedPath,
            out connectionState,
            out queuedUntilRuntime,
            out detail);
        if (!success)
        {
            SetStatus(
                GetReShadeFailureLabel(connectionState, detail),
                new Color(1f, 0.38f, 0.34f, 1f),
                8f);
            RefreshReShadeSettings();
            return;
        }

        bool saved = SaveReShadePreference(enabled, resolvedPath);
        string status = L("已切换预设：", "プリセットを変更：", "Preset selected: ")
            + VRReShadeRuntimeService.GetPresetDisplayName(resolvedPath, 30);
        if (queuedUntilRuntime)
            status += L("（等待画面运行时）", "（画面を待機中）", " (waiting for a rendered view)");
        if (!saved)
            status += L("；无法保存", "；保存できません", "; could not save");
        SetStatus(
            status,
            saved ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.72f, 0.25f, 1f),
            saved ? 5f : 8f);
        RefreshReShadeSettings();
        StartReShadeRefreshLoop();
    }

    private bool SaveReShadePreference(bool enabled, string presetPath)
    {
        return KKCharaStudioVRPlugin.SaveReShadePreference(enabled, presetPath);
    }

    internal void NotifyReShadeConfigurationChanged()
    {
        if (_settingsPage == null || !_settingsPage.activeInHierarchy)
            return;
        RefreshReShadeSettings();
        if (_settingsSection == WristSettingsSection.ReShade)
            StartReShadeRefreshLoop();
    }

    private string GetReShadeFailureLabel(VRReShadeConnectionState state, string detail)
    {
        switch (state)
        {
            case VRReShadeConnectionState.NotInstalled:
                return L("未检测到 ReShade", "ReShade が見つかりません", "ReShade was not detected");
            case VRReShadeConnectionState.NotLoaded:
                return L(
                    "ReShade 本次没有加载，请检查安装后重启工作室",
                    "ReShade が読み込まれていません。導入を確認してStudioを再起動してください",
                    "ReShade is not loaded; check the installation and restart Studio");
            case VRReShadeConnectionState.BridgeMissing:
                return L(
                    "缺少 ReShade 控制桥，安装后重启工作室",
                    "ReShade 制御ブリッジを導入してStudioを再起動してください",
                    "Install the ReShade control bridge, then restart Studio");
            case VRReShadeConnectionState.BridgeUnavailable:
                return L(
                    "控制桥未加载或版本不兼容，请重启工作室",
                    "制御ブリッジが未読込か非対応です。Studioを再起動してください",
                    "The bridge is not loaded or is incompatible; restart Studio");
            default:
                return L(
                    "无法控制 ReShade：",
                    "ReShade を操作できません：",
                    "Could not control ReShade: ") + detail;
        }
    }

    private string GetBackgroundPresetLabel(int index)
    {
        string[] chinese = { "墨黑", "深灰", "中灰", "深蓝", "浅灰", "绿幕" };
        string[] japanese = { "黒", "濃い灰", "灰", "濃い青", "薄い灰", "グリーンバック" };
        string[] english = { "Black", "Dark gray", "Gray", "Dark blue", "Light gray", "Green screen" };
        index = Mathf.Clamp(index, 0, chinese.Length - 1);
        return L(chinese[index], japanese[index], english[index]);
    }

    private string GetControllerLayoutLabel(int index)
    {
        switch (index)
        {
            case 1:
                return L("左手：X / Y", "左手：X / Y", "Left: X / Y");
            case 2:
                return L("右手：A / B", "右手：A / B", "Right: A / B");
            default:
                return L("双手：X / A", "両手：X / A", "Split: X / A");
        }
    }

    private string GetCharacterLightPresetLabel(int index)
    {
        string[] chinese = { "暖", "白", "冷", "红", "绿" };
        string[] japanese = { "暖色", "白", "寒色", "赤", "緑" };
        string[] english = { "Warm", "White", "Cool", "Red", "Green" };
        index = Mathf.Clamp(index, 0, chinese.Length - 1);
        return L(chinese[index], japanese[index], english[index]);
    }

    private string GetAudioLabel(VRAudioChannel channel)
    {
        switch (channel)
        {
            case VRAudioChannel.Master:
                return L("主音量", "マスター", "Master");
            case VRAudioChannel.Bgm:
                return L("背景音乐", "BGM", "Music");
            case VRAudioChannel.Environment:
                return L("环境声音", "環境音", "Environment");
            case VRAudioChannel.SystemEffects:
                return L("系统音效", "システム音", "System effects");
            default:
                return L("游戏音效", "ゲーム音", "Game effects");
        }
    }

    private void HandleSetControllerLayout(string layout)
    {
        ResolveSettings();
        if (_settings == null)
            return;

        _settings.ControllerFaceButtonLayout = layout;
        bool saved = TrySaveInteractionSettings();
        RefreshInteractionSettings();
        int index = layout == KKCharaStudioVRSettings.ControllerLayoutLeftHand
            ? 1
            : layout == KKCharaStudioVRSettings.ControllerLayoutRightHand
                ? 2
                : 0;
        SetStatus(
            L("手柄布局：", "ボタン配置：", "Button layout: ") + GetControllerLayoutLabel(index)
                + (saved ? string.Empty : L("（未保存）", "（未保存）", " (not saved)")),
            saved ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.72f, 0.25f, 1f),
            saved ? 4f : 7f);
    }

    private void HandleAdjustMainGuiDistance(float delta)
    {
        ResolveSettings();
        if (_settings == null)
            return;

        float next = Mathf.Round((_settings.UISpawnDistance + delta) * 10f) / 10f;
        _settings.UISpawnDistance = Mathf.Clamp(
            next,
            VRCameraMoveHelper.MinMainUIDistance,
            VRCameraMoveHelper.MaxMainUIDistance);
        bool saved = TrySaveInteractionSettings();
        ApplyMainGuiPlacement(false);
        RefreshInteractionSettings();
        SetStatus(
            saved
                ? L("主界面距离已应用", "メインGUIの距離を適用しました", "Main GUI distance applied")
                : L("距离已应用，但无法保存", "距離を適用しましたが保存できません", "Distance applied but could not be saved"),
            saved ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.72f, 0.25f, 1f),
            saved ? 3f : 7f);
    }

    private void HandleAdjustMainGuiScale(float delta)
    {
        ResolveSettings();
        if (_settings == null)
            return;

        float next = Mathf.Round((_settings.UISpawnScale + delta) * 10f) / 10f;
        _settings.UISpawnScale = Mathf.Clamp(
            next,
            VRCameraMoveHelper.MinMainUIScale,
            VRCameraMoveHelper.MaxMainUIScale);
        bool saved = TrySaveInteractionSettings();
        ApplyMainGuiPlacement(false);
        RefreshInteractionSettings();
        SetStatus(
            saved
                ? L("主界面大小已应用", "メインGUIのサイズを適用しました", "Main GUI size applied")
                : L("大小已应用，但无法保存", "サイズを適用しましたが保存できません", "Size applied but could not be saved"),
            saved ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.72f, 0.25f, 1f),
            saved ? 3f : 7f);
    }

    private void HandleApplyMainGuiPlacement()
    {
        ResolveSettings();
        if (_settings == null)
            return;
        bool recalled = ApplyMainGuiPlacement(true);
        SetStatus(
            recalled
                ? L("主工作室界面已召回", "メインStudio GUIを呼び戻しました", "Main Studio GUI recalled")
                : L("未找到可召回的主界面", "呼び戻せるメインGUIが見つかりません", "No main Studio GUI was available to recall"),
            recalled ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.72f, 0.25f, 1f),
            recalled ? 4f : 7f);
    }

    private bool ApplyMainGuiPlacement(bool reveal)
    {
        if (VRQuickActions.Instance != null)
        {
            if (reveal)
                return VRQuickActions.Instance.SummonMainGUI();

            VRQuickActions.Instance.RepositionMainGUIWithoutChangingVisibility();
            return true;
        }

        VRCameraMoveHelper.RepositionMainUI(
            _settings.UISpawnDistance,
            _settings.UISpawnScale,
            reveal);
        var mainGui = VRCameraMoveHelper.GetMainUI();
        return !reveal || (mainGui != null && mainGui.gameObject.activeSelf);
    }

    private void HandleToggleTimelineCameraFollow()
    {
        ResolveSettings();
        if (_settings == null)
            return;

        _settings.TimelineFollowCamera = !_settings.TimelineFollowCamera;
        bool saved = TrySaveInteractionSettings();
        bool cameraSyncAvailable = VRTimelineCameraFollowController.ApplyNow();
        RefreshInteractionSettings();

        string message;
        if (!cameraSyncAvailable)
        {
            message = _settings.TimelineFollowCamera
                ? L(
                    "设置已保存；未检测到 CameraSync，暂时无法跟随镜头",
                    "設定を保存しました。CameraSyncがないため追従できません",
                    "Saved; CameraSync is unavailable, so camera follow cannot run")
                : L(
                    "只播放动画已保存；未检测到 CameraSync，当前不会接管镜头",
                    "アニメのみを保存。CameraSyncがないため視点は動きません",
                    "Animation-only saved; CameraSync is absent, so the view will not be controlled");
        }
        else
        {
            message = _settings.TimelineFollowCamera
                ? L("Timeline 将跟随摄像机", "Timelineはカメラに追従します", "Timeline will follow the camera")
                : L(
                    "Timeline 只播放动画，VR 视角保持不动",
                    "Timelineはアニメのみ再生し、VR視点は動きません",
                    "Timeline will play animation without moving the VR view");
        }
        if (!saved)
            message += L("（未保存）", "（未保存）", " (not saved)");
        SetStatus(
            message,
            saved && cameraSyncAvailable
                ? new Color(0.35f, 1f, 0.62f, 1f)
                : new Color(1f, 0.72f, 0.25f, 1f),
            saved && cameraSyncAvailable ? 5f : 8f);
    }

    private bool TrySaveInteractionSettings()
    {
        try
        {
            _settings.Save();
            return true;
        }
        catch (System.Exception exception)
        {
            VRGIN.Core.VRLog.Error("Unable to save interaction settings: " + exception.Message);
            return false;
        }
    }

    private void HandleBackgroundPreset(int preset)
    {
        string status;
        ReportSettingsResult(VRStudioSettingsService.SetBackgroundPreset(preset, out status), status);
        RefreshSettingsPage();
    }

    private void HandleToggleCharacterLight()
    {
        string status;
        ReportSettingsResult(VRStudioSettingsService.ToggleCharacterLight(out status), status);
        RefreshCharacterLightSettings();
    }

    private void HandleAdjustCharacterLight(float delta)
    {
        string status;
        ReportSettingsResult(VRStudioSettingsService.AdjustCharacterLight(delta, out status), status);
        RefreshCharacterLightSettings();
    }

    private void HandleCharacterLightPreset(int preset)
    {
        string status;
        ReportSettingsResult(VRStudioSettingsService.SetCharacterLightColor(preset, out status), status);
        RefreshCharacterLightSettings();
    }

    private void HandleToggleCharacterLightShadow()
    {
        string status;
        ReportSettingsResult(VRStudioSettingsService.ToggleCharacterLightShadow(out status), status);
        RefreshCharacterLightSettings();
    }

    private void HandleAdjustAudio(VRAudioChannel channel, int delta)
    {
        string status;
        ReportSettingsResult(VRStudioSettingsService.AdjustAudio(channel, delta, out status), status);
        RefreshAudioSettings();
    }

    private void HandleToggleAudio(VRAudioChannel channel)
    {
        string status;
        ReportSettingsResult(VRStudioSettingsService.ToggleAudio(channel, out status), status);
        RefreshAudioSettings();
    }

    private void ReportSettingsResult(bool success, string status)
    {
        SetStatus(
            status,
            success ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.38f, 0.34f, 1f),
            4f);
    }

    private static Color Darken(Color color, float factor)
    {
        return new Color(color.r * factor, color.g * factor, color.b * factor, 0.82f);
    }

    private static Color Brighten(Color color, float amount)
    {
        return new Color(
            Mathf.Clamp01(color.r + amount),
            Mathf.Clamp01(color.g + amount),
            Mathf.Clamp01(color.b + amount),
            0.94f);
    }
}

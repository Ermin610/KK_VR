using UnityEngine;
using UnityEngine.UI;

namespace KKCharaStudioVR;

public sealed partial class VRWristMenuController
{
    private enum WristSettingsSection
    {
        General,
        Visual,
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
    private GameObject _settingsVisualPanel;
    private GameObject _settingsCharacterLightPanel;
    private GameObject _settingsAudioPanel;
    private VRWristMenuButtonTarget _timelineButton;
    private VRWristMenuButtonTarget _settingsGeneralTab;
    private VRWristMenuButtonTarget _settingsVisualTab;
    private VRWristMenuButtonTarget _settingsCharacterLightTab;
    private VRWristMenuButtonTarget _settingsAudioTab;
    private VRWristMenuButtonTarget _languageChineseButton;
    private VRWristMenuButtonTarget _languageJapaneseButton;
    private VRWristMenuButtonTarget _languageEnglishButton;
    private VRWristMenuButtonTarget _characterLightToggle;
    private VRWristMenuButtonTarget _characterLightShadowToggle;
    private Text _backgroundValueText;
    private Text _characterLightIntensityText;
    private readonly Text[] _audioVolumeTexts = new Text[AudioChannels.Length];
    private readonly VRWristMenuButtonTarget[] _audioToggleButtons =
        new VRWristMenuButtonTarget[AudioChannels.Length];
    private WristSettingsSection _settingsSection;
    private float _nextTimelineRefresh;

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
        CreateText(
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
            116f,
            42f,
            17);
        _settingsVisualTab = CreateButton(
            "SettingsVisualTab",
            L("画面", "画面", "Visual"),
            156f,
            62f,
            new Color(0.07f, 0.21f, 0.25f, 0.52f),
            new Color(0.09f, 0.42f, 0.49f, 0.78f),
            () => ShowSettingsSection(WristSettingsSection.Visual),
            _settingsPage.transform,
            116f,
            42f,
            17);
        _settingsCharacterLightTab = CreateButton(
            "SettingsCharacterLightTab",
            L("角色光", "キャラ光", "Lighting"),
            288f,
            62f,
            new Color(0.28f, 0.2f, 0.07f, 0.48f),
            new Color(0.55f, 0.36f, 0.1f, 0.74f),
            () => ShowSettingsSection(WristSettingsSection.CharacterLight),
            _settingsPage.transform,
            116f,
            42f,
            17);
        _settingsAudioTab = CreateButton(
            "SettingsAudioTab",
            L("声音", "サウンド", "Audio"),
            420f,
            62f,
            new Color(0.075f, 0.24f, 0.14f, 0.5f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            () => ShowSettingsSection(WristSettingsSection.Audio),
            _settingsPage.transform,
            116f,
            42f,
            17);

        _settingsGeneralPanel = CreateRectObject(
            "SettingsGeneralPanel", _settingsPage.transform, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _settingsVisualPanel = CreateRectObject(
            "SettingsVisualPanel", _settingsPage.transform, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _settingsCharacterLightPanel = CreateRectObject(
            "SettingsCharacterLightPanel", _settingsPage.transform, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _settingsAudioPanel = CreateRectObject(
            "SettingsAudioPanel", _settingsPage.transform, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);

        BuildGeneralSettings();
        BuildVisualSettings();
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
        if (_settingsGeneralPanel != null)
            _settingsGeneralPanel.SetActive(section == WristSettingsSection.General);
        if (_settingsVisualPanel != null)
            _settingsVisualPanel.SetActive(section == WristSettingsSection.Visual);
        if (_settingsCharacterLightPanel != null)
            _settingsCharacterLightPanel.SetActive(section == WristSettingsSection.CharacterLight);
        if (_settingsAudioPanel != null)
            _settingsAudioPanel.SetActive(section == WristSettingsSection.Audio);

        if (_settingsGeneralTab != null)
            _settingsGeneralTab.SetLabel(
                section == WristSettingsSection.General
                    ? L("通用  ●", "一般  ●", "General  ●")
                    : L("通用", "一般", "General"));
        if (_settingsVisualTab != null)
            _settingsVisualTab.SetLabel(
                section == WristSettingsSection.Visual
                    ? L("画面  ●", "画面  ●", "Visual  ●")
                    : L("画面", "画面", "Visual"));
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
    }

    private void RefreshSettingsPage()
    {
        switch (_settingsSection)
        {
            case WristSettingsSection.General:
                RefreshLanguageSettings();
                break;
            case WristSettingsSection.Visual:
                int preset = VRStudioSettingsService.FindClosestBackgroundPreset();
                if (_backgroundValueText != null)
                    _backgroundValueText.text = L("当前背景色  ", "現在の背景色  ", "Current background  ")
                        + GetBackgroundPresetLabel(preset);
                break;
            case WristSettingsSection.CharacterLight:
                RefreshCharacterLightSettings();
                break;
            case WristSettingsSection.Audio:
                RefreshAudioSettings();
                break;
        }
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

    private string GetBackgroundPresetLabel(int index)
    {
        string[] chinese = { "墨黑", "深灰", "中灰", "深蓝", "浅灰", "绿幕" };
        string[] japanese = { "黒", "濃い灰", "灰", "濃い青", "薄い灰", "グリーンバック" };
        string[] english = { "Black", "Dark gray", "Gray", "Dark blue", "Light gray", "Green screen" };
        index = Mathf.Clamp(index, 0, chinese.Length - 1);
        return L(chinese[index], japanese[index], english[index]);
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

using UnityEngine;
using UnityEngine.UI;

namespace KKCharaStudioVR;

public sealed partial class VRWristMenuController
{
    private enum WristSettingsSection
    {
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
    private GameObject _settingsVisualPanel;
    private GameObject _settingsCharacterLightPanel;
    private GameObject _settingsAudioPanel;
    private VRWristMenuButtonTarget _timelineButton;
    private VRWristMenuButtonTarget _settingsVisualTab;
    private VRWristMenuButtonTarget _settingsCharacterLightTab;
    private VRWristMenuButtonTarget _settingsAudioTab;
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
            "工作室设置",
            92f,
            10f,
            444f,
            40f,
            28,
            TextAnchor.MiddleLeft,
            Color.white);

        _settingsVisualTab = CreateButton(
            "SettingsVisualTab",
            "画面",
            24f,
            62f,
            new Color(0.07f, 0.21f, 0.25f, 0.52f),
            new Color(0.09f, 0.42f, 0.49f, 0.78f),
            () => ShowSettingsSection(WristSettingsSection.Visual),
            _settingsPage.transform,
            160f,
            42f,
            18);
        _settingsCharacterLightTab = CreateButton(
            "SettingsCharacterLightTab",
            "角色光",
            200f,
            62f,
            new Color(0.28f, 0.2f, 0.07f, 0.48f),
            new Color(0.55f, 0.36f, 0.1f, 0.74f),
            () => ShowSettingsSection(WristSettingsSection.CharacterLight),
            _settingsPage.transform,
            160f,
            42f,
            18);
        _settingsAudioTab = CreateButton(
            "SettingsAudioTab",
            "声音",
            376f,
            62f,
            new Color(0.075f, 0.24f, 0.14f, 0.5f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            () => ShowSettingsSection(WristSettingsSection.Audio),
            _settingsPage.transform,
            160f,
            42f,
            18);

        _settingsVisualPanel = CreateRectObject(
            "SettingsVisualPanel", _settingsPage.transform, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _settingsCharacterLightPanel = CreateRectObject(
            "SettingsCharacterLightPanel", _settingsPage.transform, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _settingsAudioPanel = CreateRectObject(
            "SettingsAudioPanel", _settingsPage.transform, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);

        BuildVisualSettings();
        BuildCharacterLightSettings();
        BuildAudioSettings();
        ShowSettingsSection(WristSettingsSection.Visual);
    }

    private void BuildVisualSettings()
    {
        CreateText(
            "BackgroundHeading",
            _settingsVisualPanel.transform,
            "背景颜色  BACKGROUND",
            24f,
            122f,
            512f,
            28f,
            19,
            TextAnchor.MiddleLeft,
            new Color(0.25f, 0.86f, 0.94f, 1f));

        const float swatchWidth = 94f;
        for (int i = 0; i < VRStudioSettingsService.BackgroundPresetCount; i++)
        {
            int preset = i;
            Color color = VRStudioSettingsService.GetBackgroundPresetColor(i);
            CreateButton(
                "BackgroundPreset" + i,
                VRStudioSettingsService.GetBackgroundPresetName(i),
                24f + i * 104.5f,
                160f,
                Darken(color, 0.78f),
                Brighten(color, 0.18f),
                () => HandleBackgroundPreset(preset),
                _settingsVisualPanel.transform,
                swatchWidth,
                70f,
                17);
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
            "当前背景色",
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
            "角色光  CHARACTER LIGHT",
            24f,
            118f,
            512f,
            28f,
            19,
            TextAnchor.MiddleLeft,
            new Color(1f, 0.72f, 0.25f, 1f));

        _characterLightToggle = CreateButton(
            "CharacterLightToggle",
            "角色光",
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
            "阴影",
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
            "灯光颜色  COLOR",
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
                VRStudioSettingsService.GetCharacterLightPresetName(i),
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
            "声音配置  AUDIO",
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
                VRStudioSettingsService.GetAudioName(channel),
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
                "开启",
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
        SetStatus("工作室设置", new Color(0.25f, 0.86f, 0.94f, 1f), 0f);
    }

    private void ShowSettingsSection(WristSettingsSection section)
    {
        _settingsSection = section;
        if (_settingsVisualPanel != null)
            _settingsVisualPanel.SetActive(section == WristSettingsSection.Visual);
        if (_settingsCharacterLightPanel != null)
            _settingsCharacterLightPanel.SetActive(section == WristSettingsSection.CharacterLight);
        if (_settingsAudioPanel != null)
            _settingsAudioPanel.SetActive(section == WristSettingsSection.Audio);

        if (_settingsVisualTab != null)
            _settingsVisualTab.SetLabel(section == WristSettingsSection.Visual ? "画面  ●" : "画面");
        if (_settingsCharacterLightTab != null)
            _settingsCharacterLightTab.SetLabel(section == WristSettingsSection.CharacterLight ? "角色光  ●" : "角色光");
        if (_settingsAudioTab != null)
            _settingsAudioTab.SetLabel(section == WristSettingsSection.Audio ? "声音  ●" : "声音");
        RefreshSettingsPage();
    }

    private void RefreshSettingsPage()
    {
        switch (_settingsSection)
        {
            case WristSettingsSection.Visual:
                int preset = VRStudioSettingsService.FindClosestBackgroundPreset();
                if (_backgroundValueText != null)
                    _backgroundValueText.text = "当前背景色  " + VRStudioSettingsService.GetBackgroundPresetName(preset);
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
            _timelineButton.SetLabel("TIMELINE\n不可用");
            return;
        }
        _timelineButton.SetLabel(isPlaying ? "TIMELINE\n暂停" : "TIMELINE\n播放");
    }

    private void RefreshCharacterLightSettings()
    {
        float intensity;
        bool shadow;
        Color color;
        bool available = VRStudioSettingsService.TryGetCharacterLight(out intensity, out shadow, out color);
        if (_characterLightToggle != null)
            _characterLightToggle.SetLabel(available && intensity > 0.01f ? "角色光  开" : "角色光  关");
        if (_characterLightIntensityText != null)
            _characterLightIntensityText.text = available ? intensity.ToString("F1") : "--";
        if (_characterLightShadowToggle != null)
            _characterLightShadowToggle.SetLabel(available && shadow ? "阴影  开" : "阴影  关");
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
                _audioToggleButtons[i].SetLabel(available && enabled ? "开启" : "静音");
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

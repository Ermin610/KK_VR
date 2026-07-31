using UnityEngine;
using UnityEngine.UI;

namespace KKCharaStudioVR;

public sealed partial class VRWristMenuController
{
    private GameObject _mmdPage;
    private GameObject _highHeelsPage;
    private VRWristMenuButtonTarget _fixedFovToggleButton;
    private Text _fixedFovValueText;
    private Text _fixedFovCameraCountText;
    private Text _highHeelsTargetText;
    private VRWristMenuButtonTarget _highHeelsModeButton;
    private VRWristMenuButtonTarget _highHeelsShoesDetectButton;
    private VRWristMenuButtonTarget _shoesOffsetToggleButton;
    private readonly Text[] _highHeelsAngleTexts = new Text[3];
    private readonly Text[] _shoesOffsetTexts = new Text[2];

    private void BuildMmdSettingsPage()
    {
        CreateButton(
            "MmdBack",
            "<",
            18f,
            10f,
            new Color(1f, 1f, 1f, 0.08f),
            new Color(1f, 1f, 1f, 0.2f),
            HandleBackToRoot,
            _mmdPage.transform,
            58f,
            38f,
            28);
        CreateText(
            "MmdTitle",
            _mmdPage.transform,
            "完美 MMD",
            92f,
            10f,
            444f,
            40f,
            28,
            TextAnchor.MiddleLeft,
            Color.white);

        CreateButton(
            "MmdPlayPause",
            "MMDD\n播放 / 暂停",
            24f,
            72f,
            new Color(0.28f, 0.2f, 0.07f, 0.48f),
            new Color(0.55f, 0.36f, 0.1f, 0.74f),
            HandleToggleMmd,
            _mmdPage.transform,
            248f,
            64f,
            19);
        CreateButton(
            "OpenHighHeels",
            "高跟鞋  >\nHIGH HEELS",
            288f,
            72f,
            new Color(0.075f, 0.24f, 0.14f, 0.48f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            HandleOpenHighHeels,
            _mmdPage.transform,
            248f,
            64f,
            19);

        CreateText(
            "FixedFovHeading",
            _mmdPage.transform,
            "VR 镜头  FIXED FOV",
            24f,
            160f,
            512f,
            28f,
            19,
            TextAnchor.MiddleLeft,
            new Color(1f, 0.72f, 0.25f, 1f));

        _fixedFovToggleButton = CreateButton(
            "FixedFovToggle",
            "Fixed FOV",
            24f,
            198f,
            new Color(0.28f, 0.2f, 0.07f, 0.48f),
            new Color(0.55f, 0.36f, 0.1f, 0.74f),
            HandleToggleFixedFov,
            _mmdPage.transform,
            150f,
            58f,
            17);
        CreateButton(
            "FixedFovMinus",
            "-",
            190f,
            198f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            () => HandleAdjustFixedFov(-1f),
            _mmdPage.transform,
            54f,
            58f,
            26);
        _fixedFovValueText = CreateText(
            "FixedFovValue",
            _mmdPage.transform,
            "53.13",
            252f,
            198f,
            100f,
            58f,
            23,
            TextAnchor.MiddleCenter,
            Color.white);
        CreateButton(
            "FixedFovPlus",
            "+",
            360f,
            198f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            () => HandleAdjustFixedFov(1f),
            _mmdPage.transform,
            54f,
            58f,
            26);
        CreateButton(
            "FixedFovReset",
            "重置\n53.13",
            426f,
            198f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            HandleResetFixedFov,
            _mmdPage.transform,
            110f,
            58f,
            16);

        Image stateBackground = CreateImage(
            "FixedFovStateBackground",
            _mmdPage.transform,
            24f,
            278f,
            512f,
            58f,
            new Color(0.7f, 0.84f, 0.94f, 0.065f));
        ApplyGlassEffects(stateBackground, new Color(0.86f, 0.96f, 1f, 0.12f), false, 0f);
        _fixedFovCameraCountText = CreateText(
            "FixedFovState",
            _mmdPage.transform,
            "MMDD 镜头控制器  --",
            38f,
            284f,
            484f,
            46f,
            18,
            TextAnchor.MiddleLeft,
            new Color(0.76f, 0.88f, 0.94f, 1f));
    }

    private void BuildHighHeelsPage()
    {
        CreateButton(
            "HighHeelsBack",
            "<",
            18f,
            10f,
            new Color(1f, 1f, 1f, 0.08f),
            new Color(1f, 1f, 1f, 0.2f),
            HandleBackToMmdSettings,
            _highHeelsPage.transform,
            58f,
            38f,
            28);
        CreateText(
            "HighHeelsTitle",
            _highHeelsPage.transform,
            "MMDD 高跟鞋",
            92f,
            10f,
            444f,
            40f,
            27,
            TextAnchor.MiddleLeft,
            Color.white);

        Image targetBackground = CreateImage(
            "HighHeelsTargetBackground",
            _highHeelsPage.transform,
            24f,
            60f,
            512f,
            42f,
            new Color(0.55f, 0.9f, 0.7f, 0.065f));
        ApplyGlassEffects(targetBackground, new Color(0.72f, 1f, 0.82f, 0.13f), false, 0f);
        _highHeelsTargetText = CreateText(
            "HighHeelsTarget",
            _highHeelsPage.transform,
            "目标角色  --",
            38f,
            63f,
            484f,
            36f,
            16,
            TextAnchor.MiddleLeft,
            new Color(0.7f, 0.8f, 0.74f, 1f));

        _highHeelsModeButton = CreateButton(
            "HighHeelsMode",
            "模式  --",
            24f,
            112f,
            new Color(0.28f, 0.2f, 0.07f, 0.48f),
            new Color(0.55f, 0.36f, 0.1f, 0.74f),
            HandleToggleHighHeelsMode,
            _highHeelsPage.transform,
            248f,
            46f,
            17);
        CreateButton(
            "HighHeelsReset",
            "恢复鞋子默认角度",
            288f,
            112f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            HandleResetHighHeels,
            _highHeelsPage.transform,
            248f,
            46f,
            16);

        _shoesOffsetToggleButton = CreateButton(
            "ShoesOffsetToggle",
            "鞋高偏移  --",
            24f,
            168f,
            new Color(0.075f, 0.24f, 0.14f, 0.48f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            HandleToggleShoesOffset,
            _highHeelsPage.transform,
            248f,
            42f,
            16);
        _highHeelsShoesDetectButton = CreateButton(
            "HighHeelsShoesDetect",
            "鞋子检测  --",
            288f,
            168f,
            new Color(0.075f, 0.24f, 0.14f, 0.48f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            HandleToggleHighHeelsShoesDetect,
            _highHeelsPage.transform,
            248f,
            42f,
            16);

        string[] angleNames = { "脚踝", "鞋跟", "脚趾" };
        for (int i = 0; i < angleNames.Length; i++)
        {
            int component = i;
            float y = 220f + i * 40f;
            CreateText(
                "HighHeelsAngleName" + i,
                _highHeelsPage.transform,
                angleNames[i],
                24f,
                y,
                92f,
                34f,
                16,
                TextAnchor.MiddleLeft,
                Color.white);
            CreateButton(
                "HighHeelsAngleMinus" + i,
                "-",
                124f,
                y,
                new Color(0.08f, 0.15f, 0.18f, 0.52f),
                new Color(0.12f, 0.32f, 0.39f, 0.76f),
                () => HandleAdjustHighHeelsRotation(component, -1f),
                _highHeelsPage.transform,
                52f,
                34f,
                22);
            _highHeelsAngleTexts[i] = CreateText(
                "HighHeelsAngleValue" + i,
                _highHeelsPage.transform,
                "--",
                184f,
                y,
                112f,
                34f,
                18,
                TextAnchor.MiddleCenter,
                new Color(0.76f, 0.88f, 0.94f, 1f));
            CreateButton(
                "HighHeelsAnglePlus" + i,
                "+",
                304f,
                y,
                new Color(0.08f, 0.15f, 0.18f, 0.52f),
                new Color(0.12f, 0.32f, 0.39f, 0.76f),
                () => HandleAdjustHighHeelsRotation(component, 1f),
                _highHeelsPage.transform,
                52f,
                34f,
                22);
        }

        BuildShoesOffsetRow("ShoesOnOffset", "穿鞋高度", true, 344f, 0);
        BuildShoesOffsetRow("ShoesOffOffset", "脱鞋高度", false, 378f, 1);
    }

    private void BuildShoesOffsetRow(
        string name,
        string label,
        bool shoesOn,
        float y,
        int valueIndex)
    {
        CreateText(
            name + "Name",
            _highHeelsPage.transform,
            label,
            24f,
            y,
            100f,
            28f,
            15,
            TextAnchor.MiddleLeft,
            Color.white);
        CreateButton(
            name + "Minus",
            "-",
            132f,
            y,
            new Color(0.08f, 0.15f, 0.18f, 0.52f),
            new Color(0.12f, 0.32f, 0.39f, 0.76f),
            () => HandleAdjustShoesOffset(shoesOn, -0.005f),
            _highHeelsPage.transform,
            46f,
            28f,
            20);
        _shoesOffsetTexts[valueIndex] = CreateText(
            name + "Value",
            _highHeelsPage.transform,
            "--",
            186f,
            y,
            112f,
            28f,
            16,
            TextAnchor.MiddleCenter,
            new Color(0.76f, 0.88f, 0.94f, 1f));
        CreateButton(
            name + "Plus",
            "+",
            306f,
            y,
            new Color(0.08f, 0.15f, 0.18f, 0.52f),
            new Color(0.12f, 0.32f, 0.39f, 0.76f),
            () => HandleAdjustShoesOffset(shoesOn, 0.005f),
            _highHeelsPage.transform,
            46f,
            28f,
            20);
    }

    private void HandleOpenMmdSettings()
    {
        SetStatus("MMDD 设置", new Color(1f, 0.72f, 0.25f, 1f), 0f);
        ShowPage(WristMenuPage.MmdSettings);
    }

    private void HandleBackToMmdSettings()
    {
        SetStatus("MMDD 设置", new Color(1f, 0.72f, 0.25f, 1f), 0f);
        ShowPage(WristMenuPage.MmdSettings);
    }

    private void HandleOpenHighHeels()
    {
        SetStatus("正在读取 MMDD 高跟鞋状态", new Color(0.47f, 0.9f, 0.55f, 1f), 0f);
        ShowPage(WristMenuPage.HighHeels);
    }

    private void RefreshMmdSettingsPage()
    {
        string status;
        bool success = VRMmddService.RefreshFixedFov(out status);
        RefreshFixedFovVisuals();
        if (!success)
            ReportMmdResult(false, status);
    }

    private void RefreshFixedFovVisuals()
    {
        bool available = VRMmddStateBridge.FixedFovReported;
        if (_fixedFovToggleButton != null)
        {
            _fixedFovToggleButton.SetLabel(
                available
                    ? (VRMmddStateBridge.FixedFovEnabled ? "Fixed FOV  开" : "Fixed FOV  关")
                    : "Fixed FOV  --");
        }
        if (_fixedFovValueText != null)
        {
            _fixedFovValueText.text = available
                ? VRMmddStateBridge.FixedFovValue.ToString("F2")
                : "--";
        }
        if (_fixedFovCameraCountText != null)
        {
            _fixedFovCameraCountText.text = available
                ? "MMDD 镜头控制器  " + VRMmddStateBridge.CameraControllerCount
                : "MMDD 镜头控制器  --";
        }
    }

    private void HandleToggleFixedFov()
    {
        string status;
        ReportMmdResult(VRMmddService.ToggleFixedFov(out status), status);
        RefreshFixedFovVisuals();
    }

    private void HandleAdjustFixedFov(float delta)
    {
        float current = VRMmddStateBridge.FixedFovReported
            ? VRMmddStateBridge.FixedFovValue
            : VRMmddService.DefaultFixedFov;
        string status;
        ReportMmdResult(VRMmddService.SetFixedFov(current + delta, out status), status);
        RefreshFixedFovVisuals();
    }

    private void HandleResetFixedFov()
    {
        string status;
        ReportMmdResult(
            VRMmddService.SetFixedFov(VRMmddService.DefaultFixedFov, out status),
            status);
        RefreshFixedFovVisuals();
    }

    private void RefreshHighHeelsPage()
    {
        string status;
        bool success = VRMmddService.RefreshHighHeels(out status);
        RefreshHighHeelsVisuals();
        if (!success)
            ReportMmdResult(false, status);
    }

    private void RefreshHighHeelsVisuals()
    {
        bool available = VRMmddStateBridge.HighHeelsReported;
        if (_highHeelsTargetText != null)
        {
            _highHeelsTargetText.text = available
                ? "目标  " + VRMmddStateBridge.HighHeelsTargetName
                    + "  /  " + VRMmddStateBridge.HighHeelsPluginName
                : "目标角色  --";
        }
        if (_highHeelsModeButton != null)
        {
            _highHeelsModeButton.SetLabel(
                available
                    ? (VRMmddStateBridge.HighHeelsAutoMode ? "模式  自动  AUTO" : "模式  手动  MANUAL")
                    : "模式  --");
        }
        if (_highHeelsShoesDetectButton != null)
        {
            _highHeelsShoesDetectButton.SetLabel(
                available
                    ? (VRMmddStateBridge.HighHeelsShoesDetect ? "鞋子检测  开" : "鞋子检测  关")
                    : "鞋子检测  --");
        }
        if (_shoesOffsetToggleButton != null)
        {
            _shoesOffsetToggleButton.SetLabel(
                available
                    ? (VRMmddStateBridge.ShoesOffsetEnabled ? "鞋高偏移  开" : "鞋高偏移  关")
                    : "鞋高偏移  --");
        }

        float[] angles =
        {
            VRMmddStateBridge.HighHeelsAnkle,
            VRMmddStateBridge.HighHeelsHeel,
            VRMmddStateBridge.HighHeelsToes
        };
        for (int i = 0; i < _highHeelsAngleTexts.Length; i++)
        {
            if (_highHeelsAngleTexts[i] != null)
                _highHeelsAngleTexts[i].text = available ? angles[i].ToString("F1") : "--";
        }
        if (_shoesOffsetTexts[0] != null)
            _shoesOffsetTexts[0].text = available ? VRMmddStateBridge.ShoesOnOffset.ToString("F3") : "--";
        if (_shoesOffsetTexts[1] != null)
            _shoesOffsetTexts[1].text = available ? VRMmddStateBridge.ShoesOffOffset.ToString("F3") : "--";
    }

    private void HandleToggleHighHeelsMode()
    {
        string status;
        ReportMmdResult(VRMmddService.ToggleHighHeelsMode(out status), status);
        RefreshHighHeelsVisuals();
    }

    private void HandleResetHighHeels()
    {
        string status;
        ReportMmdResult(VRMmddService.ResetHighHeels(out status), status);
        RefreshHighHeelsVisuals();
    }

    private void HandleToggleHighHeelsShoesDetect()
    {
        string status;
        ReportMmdResult(VRMmddService.ToggleHighHeelsShoesDetect(out status), status);
        RefreshHighHeelsVisuals();
    }

    private void HandleToggleShoesOffset()
    {
        string status;
        ReportMmdResult(VRMmddService.ToggleShoesOffset(out status), status);
        RefreshHighHeelsVisuals();
    }

    private void HandleAdjustHighHeelsRotation(int component, float delta)
    {
        string status;
        ReportMmdResult(
            VRMmddService.AdjustHighHeelsRotation(component, delta, out status),
            status);
        RefreshHighHeelsVisuals();
    }

    private void HandleAdjustShoesOffset(bool shoesOn, float delta)
    {
        string status;
        ReportMmdResult(VRMmddService.AdjustShoesOffset(shoesOn, delta, out status), status);
        RefreshHighHeelsVisuals();
    }

    private void ReportMmdResult(bool success, string status)
    {
        SetStatus(
            status,
            success ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.38f, 0.34f, 1f),
            success ? 4f : 7f);
    }
}

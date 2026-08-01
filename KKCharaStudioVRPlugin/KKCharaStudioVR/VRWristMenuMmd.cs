using UnityEngine;
using UnityEngine.UI;
using VRGIN.Core;

namespace KKCharaStudioVR;

public sealed partial class VRWristMenuController
{
    private static readonly Color VmdRootReadyColor = new Color(0.28f, 0.2f, 0.07f, 0.48f);
    private static readonly Color VmdRootReadyHoverColor = new Color(0.55f, 0.36f, 0.1f, 0.74f);
    private static readonly Color VmdRootMissingColor = new Color(0.42f, 0.075f, 0.065f, 0.72f);
    private static readonly Color VmdRootMissingHoverColor = new Color(0.72f, 0.12f, 0.1f, 0.88f);

    private GameObject _mmdPage;
    private GameObject _highHeelsPage;
    private VRWristMenuButtonTarget _vmdRootButton;
    private VRWristMenuButtonTarget _fixedFovToggleButton;
    private Text _fixedFovValueText;
    private Text _fixedFovCameraCountText;
    private VRWristMenuButtonTarget _highHeelsTargetButton;
    private VRWristMenuButtonTarget _highHeelsModeButton;
    private VRWristMenuButtonTarget _highHeelsShoesDetectButton;
    private VRWristMenuButtonTarget _shoesOffsetToggleButton;
    private VRWristMenuButtonTarget _highHeelsLoadPresetButton;
    private readonly Text[] _highHeelsAngleTexts = new Text[3];
    private readonly Text[] _shoesOffsetTexts = new Text[2];
    private int _highHeelsTargetObjectKey = -1;
    private Studio.OCIChar _highHeelsTargetCharacter;

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
            L("MMD 工具", "MMD ツール", "MMD tools"),
            92f,
            10f,
            444f,
            40f,
            28,
            TextAnchor.MiddleLeft,
            Color.white);

        CreateText(
            "MmdPlaybackHeading",
            _mmdPage.transform,
            L("播放与动作", "再生とモーション", "Playback and motion"),
            24f,
            60f,
            512f,
            20f,
            17,
            TextAnchor.MiddleLeft,
            new Color(1f, 0.72f, 0.25f, 1f));

        CreateButton(
            "ToggleMmd",
            L("MMDD\n播放或暂停", "MMDD\n再生・一時停止", "MMDD\nPlay or pause"),
            24f,
            86f,
            new Color(0.28f, 0.2f, 0.07f, 0.48f),
            new Color(0.55f, 0.36f, 0.1f, 0.74f),
            HandleToggleMmd,
            _mmdPage.transform,
            116f,
            56f,
            15);
        _timelineButton = CreateButton(
            "ToggleTimeline",
            L("时间轴\n播放或暂停", "タイムライン\n再生・一時停止", "Timeline\nPlay or pause"),
            156f,
            86f,
            new Color(0.12f, 0.17f, 0.3f, 0.5f),
            new Color(0.18f, 0.36f, 0.62f, 0.8f),
            HandleToggleTimeline,
            _mmdPage.transform,
            116f,
            56f,
            15);
        _loadVmdButton = CreateButton(
            "LoadVmd",
            L("VMD\n读取动作", "VMD\nモーション読込", "VMD\nLoad motion"),
            288f,
            86f,
            VmdRootMissingColor,
            VmdRootMissingHoverColor,
            HandleLoadVmd,
            _mmdPage.transform,
            116f,
            56f,
            15);
        CreateButton(
            "OpenHighHeels",
            L("高跟鞋\n参数  ›", "ハイヒール\n設定  ›", "High heels\nSettings  ›"),
            420f,
            86f,
            new Color(0.075f, 0.24f, 0.14f, 0.48f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            HandleOpenHighHeels,
            _mmdPage.transform,
            116f,
            56f,
            15);

        _vmdRootButton = CreateButton(
            "VmdRoot",
            L("动作目录\n未设置 · 点击选择", "モーションフォルダー\n未設定・選択", "Motion folder\nNot set · Select"),
            24f,
            154f,
            VmdRootMissingColor,
            VmdRootMissingHoverColor,
            HandleChangeVmdRoot,
            _mmdPage.transform,
            512f,
            44f,
            16);

        CreateText(
            "FixedFovHeading",
            _mmdPage.transform,
            L("VR 镜头视野", "VRカメラ視野", "VR camera field of view"),
            24f,
            210f,
            512f,
            24f,
            17,
            TextAnchor.MiddleLeft,
            new Color(1f, 0.72f, 0.25f, 1f));

        _fixedFovToggleButton = CreateButton(
            "FixedFovToggle",
            L("固定视野", "固定視野", "Fixed FOV"),
            24f,
            242f,
            new Color(0.28f, 0.2f, 0.07f, 0.48f),
            new Color(0.55f, 0.36f, 0.1f, 0.74f),
            HandleToggleFixedFov,
            _mmdPage.transform,
            150f,
            52f,
            17);
        CreateButton(
            "FixedFovMinus",
            "-",
            190f,
            242f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            () => HandleAdjustFixedFov(-1f),
            _mmdPage.transform,
            54f,
            52f,
            26);
        _fixedFovValueText = CreateText(
            "FixedFovValue",
            _mmdPage.transform,
            "53.13",
            252f,
            242f,
            100f,
            52f,
            23,
            TextAnchor.MiddleCenter,
            Color.white);
        CreateButton(
            "FixedFovPlus",
            "+",
            360f,
            242f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            () => HandleAdjustFixedFov(1f),
            _mmdPage.transform,
            54f,
            52f,
            26);
        CreateButton(
            "FixedFovReset",
            L("恢复默认\n53.13", "既定値\n53.13", "Default\n53.13"),
            426f,
            242f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            HandleResetFixedFov,
            _mmdPage.transform,
            110f,
            52f,
            16);

        Image stateBackground = CreateImage(
            "FixedFovStateBackground",
            _mmdPage.transform,
            24f,
            306f,
            512f,
            44f,
            new Color(0.7f, 0.84f, 0.94f, 0.065f));
        ApplyGlassEffects(stateBackground, new Color(0.86f, 0.96f, 1f, 0.12f), false, 0f);
        _fixedFovCameraCountText = CreateText(
            "FixedFovState",
            _mmdPage.transform,
            L("MMDD 镜头控制器  --", "MMDD カメラ制御  --", "MMDD camera controllers  --"),
            38f,
            310f,
            484f,
            36f,
            16,
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
            L("MMDD 高跟鞋", "MMDD ハイヒール", "MMDD high heels"),
            92f,
            10f,
            444f,
            40f,
            27,
            TextAnchor.MiddleLeft,
            Color.white);

        _highHeelsTargetButton = CreateButton(
            "HighHeelsTarget",
            L("选择场景角色  >", "シーンのキャラを選択  >", "Select a scene character  >"),
            24f,
            60f,
            new Color(0.075f, 0.24f, 0.14f, 0.48f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            HandleChooseHighHeelsTarget,
            _highHeelsPage.transform,
            512f,
            42f,
            16);

        _highHeelsModeButton = CreateButton(
            "HighHeelsMode",
            L("模式  --", "モード  --", "Mode  --"),
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
            L("恢复鞋子默认角度", "靴の角度を初期値に戻す", "Restore default shoe angles"),
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
            L("鞋高偏移  --", "靴の高さ補正  --", "Shoe height offset  --"),
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
            L("鞋子检测  --", "靴の検出  --", "Shoe detection  --"),
            288f,
            168f,
            new Color(0.075f, 0.24f, 0.14f, 0.48f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            HandleToggleHighHeelsShoesDetect,
            _highHeelsPage.transform,
            248f,
            42f,
            16);

        string[] angleNames =
        {
            L("脚踝", "足首", "Ankle"),
            L("鞋跟", "かかと", "Heel"),
            L("脚趾", "つま先", "Toes")
        };
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

        BuildShoesOffsetRow("ShoesOnOffset", L("穿鞋高度", "着靴時の高さ", "Shoes-on height"), true, 344f, 0);
        BuildShoesOffsetRow("ShoesOffOffset", L("脱鞋高度", "脱靴時の高さ", "Shoes-off height"), false, 378f, 1);

        CreateButton(
            "HighHeelsSavePreset",
            L("保存参数", "設定を保存", "Save preset"),
            368f,
            344f,
            new Color(0.075f, 0.24f, 0.14f, 0.48f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            HandleSaveHighHeelsPreset,
            _highHeelsPage.transform,
            168f,
            28f,
            15);
        _highHeelsLoadPresetButton = CreateButton(
            "HighHeelsLoadPreset",
            L("读取参数", "設定を読込", "Load preset"),
            368f,
            378f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            HandleLoadHighHeelsPreset,
            _highHeelsPage.transform,
            168f,
            28f,
            15);
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
        SetStatus(L("MMD 工具", "MMD ツール", "MMD tools"), new Color(1f, 0.72f, 0.25f, 1f), 0f);
        ShowPage(WristMenuPage.MmdSettings);
    }

    private void HandleChangeVmdRoot()
    {
        OpenVmdRootPicker(false, true);
    }

    private void HandleBackToMmdSettings()
    {
        SetStatus(L("MMD 工具", "MMD ツール", "MMD tools"), new Color(1f, 0.72f, 0.25f, 1f), 0f);
        ShowPage(WristMenuPage.MmdSettings);
    }

    private void HandleOpenHighHeels()
    {
        VRVmdActorTarget target;
        string status;
        if (_highHeelsTargetObjectKey >= 0
            && VRVmdTargetService.TryGetTarget(
                _highHeelsTargetObjectKey,
                out target,
                out status)
            && object.ReferenceEquals(_highHeelsTargetCharacter, target.Character))
        {
            SetStatus(
                L("正在读取 ", "読込中：", "Reading high-heel state: ")
                    + target.DisplayName
                    + L(" 的高跟鞋状态", "", ""),
                new Color(0.47f, 0.9f, 0.55f, 1f),
                0f);
            ShowPage(WristMenuPage.HighHeels);
            return;
        }

        _highHeelsTargetObjectKey = -1;
        _highHeelsTargetCharacter = null;
        HandleChooseHighHeelsTarget();
    }

    private void HandleChooseHighHeelsTarget()
    {
        string status;
        var targets = VRVmdTargetService.GetAllTargets(out status);
        if (targets.Count == 0)
        {
            ReportMmdResult(false, status ?? "当前场景没有角色");
            return;
        }

        OpenActorTargetBrowser(
            BrowserMode.SelectHighHeelsActor,
            targets,
            "请选择要调整高跟鞋的角色",
            out status);
    }

    private void HandleHighHeelsActorSelection(VRWristFileEntry entry)
    {
        if (!entry.IsActorTarget)
            return;

        VRVmdActorTarget target;
        string status;
        if (!VRVmdTargetService.TryGetTarget(entry.ObjectKey, out target, out status))
        {
            ReportMmdResult(false, status);
            return;
        }

        _highHeelsTargetObjectKey = entry.ObjectKey;
        _highHeelsTargetCharacter = target.Character;
        VRMmddStateBridge.ResetHighHeels();
        SetStatus(L("高跟鞋目标：", "ハイヒール対象：", "High-heel target: ") + target.DisplayName,
            new Color(0.35f, 1f, 0.62f, 1f), 0f);
        ShowPage(WristMenuPage.HighHeels);
    }

    private void RefreshMmdSettingsPage()
    {
        RefreshVmdRootVisuals();
        string status;
        bool success = VRMmddService.RefreshFixedFov(out status);
        RefreshFixedFovVisuals();
        if (!success)
            ReportMmdResult(false, status);
    }

    private void RefreshVmdRootVisuals()
    {
        string root = GetConfiguredVmdRoot();
        bool configured = !string.IsNullOrEmpty(root);

        if (_loadVmdButton != null)
        {
            _loadVmdButton.SetLabel(
                configured
                    ? L("VMD\n读取动作", "VMD\nモーション読込", "VMD\nLoad motion")
                    : L("VMD\n未设置目录", "VMD\nフォルダー未設定", "VMD\nFolder not set"));
            _loadVmdButton.SetColors(
                configured ? VmdRootReadyColor : VmdRootMissingColor,
                configured ? VmdRootReadyHoverColor : VmdRootMissingHoverColor);
        }

        if (_vmdRootButton != null)
        {
            _vmdRootButton.SetLabel(
                configured
                    ? L("动作目录\n已保存 · 点击更换", "モーションフォルダー\n設定済み・変更", "Motion folder\nSaved · Change")
                    : L("动作目录\n未设置 · 点击选择", "モーションフォルダー\n未設定・選択", "Motion folder\nNot set · Select"));
            _vmdRootButton.SetColors(
                configured ? VmdRootReadyColor : VmdRootMissingColor,
                configured ? VmdRootReadyHoverColor : VmdRootMissingHoverColor);
        }
    }

    private void RefreshFixedFovVisuals()
    {
        bool available = VRMmddStateBridge.FixedFovReported;
        if (_fixedFovToggleButton != null)
        {
            _fixedFovToggleButton.SetLabel(
                available
                    ? (VRMmddStateBridge.FixedFovEnabled
                        ? L("固定视野  开", "固定視野  オン", "Fixed FOV  On")
                        : L("固定视野  关", "固定視野  オフ", "Fixed FOV  Off"))
                    : L("固定视野  --", "固定視野  --", "Fixed FOV  --"));
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
                ? L("MMDD 镜头控制器  ", "MMDD カメラ制御  ", "MMDD camera controllers  ")
                    + VRMmddStateBridge.CameraControllerCount
                : L("MMDD 镜头控制器  --", "MMDD カメラ制御  --", "MMDD camera controllers  --");
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
        int objectKey;
        if (!TryGetHighHeelsTarget(out objectKey, out status))
        {
            VRMmddStateBridge.ResetHighHeels();
            RefreshHighHeelsVisuals();
            ReportMmdResult(false, status);
            return;
        }

        bool success = VRMmddService.RefreshHighHeels(objectKey, out status);
        RefreshHighHeelsVisuals();
        if (!success)
            ReportMmdResult(false, status);
    }

    private void RefreshHighHeelsVisuals()
    {
        bool available = VRMmddStateBridge.HighHeelsReported;
        if (_highHeelsTargetButton != null)
        {
            string targetLabel = L("选择场景角色  >", "シーンのキャラを選択  >", "Select a scene character  >");
            if (available)
            {
                targetLabel = VRMmddStateBridge.HighHeelsTargetName
                    + "  /  " + VRMmddStateBridge.HighHeelsPluginName
                    + "  >";
            }
            else if (_highHeelsTargetObjectKey >= 0)
            {
                VRVmdActorTarget target;
                string status;
                if (VRVmdTargetService.TryGetTarget(
                    _highHeelsTargetObjectKey,
                    out target,
                    out status)
                    && object.ReferenceEquals(_highHeelsTargetCharacter, target.Character))
                {
                    targetLabel = target.DisplayName + "  >";
                }
            }
            _highHeelsTargetButton.SetLabel(targetLabel);
        }
        if (_highHeelsModeButton != null)
        {
            _highHeelsModeButton.SetLabel(
                available
                    ? (VRMmddStateBridge.HighHeelsAutoMode
                        ? L("模式  自动", "モード  自動", "Mode  Automatic")
                        : L("模式  手动", "モード  手動", "Mode  Manual"))
                    : L("模式  --", "モード  --", "Mode  --"));
        }
        if (_highHeelsShoesDetectButton != null)
        {
            _highHeelsShoesDetectButton.SetLabel(
                available
                    ? (VRMmddStateBridge.HighHeelsShoesDetect
                        ? L("鞋子检测  开", "靴の検出  オン", "Shoe detection  On")
                        : L("鞋子检测  关", "靴の検出  オフ", "Shoe detection  Off"))
                    : L("鞋子检测  --", "靴の検出  --", "Shoe detection  --"));
        }
        if (_shoesOffsetToggleButton != null)
        {
            _shoesOffsetToggleButton.SetLabel(
                available
                    ? (VRMmddStateBridge.ShoesOffsetEnabled
                        ? L("鞋高偏移  开", "靴の高さ補正  オン", "Shoe height offset  On")
                        : L("鞋高偏移  关", "靴の高さ補正  オフ", "Shoe height offset  Off"))
                    : L("鞋高偏移  --", "靴の高さ補正  --", "Shoe height offset  --"));
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

        if (_highHeelsLoadPresetButton != null)
        {
            bool hasPreset = false;
            VRVmdActorTarget target;
            string ignoredStatus;
            if (_highHeelsTargetObjectKey >= 0
                && VRVmdTargetService.TryGetTarget(
                    _highHeelsTargetObjectKey,
                    out target,
                    out ignoredStatus)
                && object.ReferenceEquals(_highHeelsTargetCharacter, target.Character))
            {
                hasPreset = VRHighHeelsPresetStore.HasPreset(target.Character);
            }
            _highHeelsLoadPresetButton.SetLabel(
                hasPreset
                    ? L("读取参数\n已有记录", "設定を読込\n保存あり", "Load preset\nSaved")
                    : L("读取参数\n无记录", "設定を読込\n保存なし", "Load preset\nNone saved"));
        }
    }

    private void HandleToggleHighHeelsMode()
    {
        string status;
        int objectKey;
        if (!TryGetHighHeelsTarget(out objectKey, out status))
        {
            ReportMmdResult(false, status);
            return;
        }
        ReportMmdResult(VRMmddService.ToggleHighHeelsMode(objectKey, out status), status);
        RefreshHighHeelsVisuals();
    }

    private void HandleResetHighHeels()
    {
        string status;
        int objectKey;
        if (!TryGetHighHeelsTarget(out objectKey, out status))
        {
            ReportMmdResult(false, status);
            return;
        }
        ReportMmdResult(VRMmddService.ResetHighHeels(objectKey, out status), status);
        RefreshHighHeelsVisuals();
    }

    private void HandleToggleHighHeelsShoesDetect()
    {
        string status;
        int objectKey;
        if (!TryGetHighHeelsTarget(out objectKey, out status))
        {
            ReportMmdResult(false, status);
            return;
        }
        ReportMmdResult(
            VRMmddService.ToggleHighHeelsShoesDetect(objectKey, out status),
            status);
        RefreshHighHeelsVisuals();
    }

    private void HandleToggleShoesOffset()
    {
        string status;
        int objectKey;
        if (!TryGetHighHeelsTarget(out objectKey, out status))
        {
            ReportMmdResult(false, status);
            return;
        }
        ReportMmdResult(VRMmddService.ToggleShoesOffset(objectKey, out status), status);
        RefreshHighHeelsVisuals();
    }

    private void HandleAdjustHighHeelsRotation(int component, float delta)
    {
        string status;
        int objectKey;
        if (!TryGetHighHeelsTarget(out objectKey, out status))
        {
            ReportMmdResult(false, status);
            return;
        }
        ReportMmdResult(
            VRMmddService.AdjustHighHeelsRotation(
                objectKey,
                component,
                delta,
                out status),
            status);
        RefreshHighHeelsVisuals();
    }

    private void HandleAdjustShoesOffset(bool shoesOn, float delta)
    {
        string status;
        int objectKey;
        if (!TryGetHighHeelsTarget(out objectKey, out status))
        {
            ReportMmdResult(false, status);
            return;
        }
        ReportMmdResult(
            VRMmddService.AdjustShoesOffset(objectKey, shoesOn, delta, out status),
            status);
        RefreshHighHeelsVisuals();
    }

    private void HandleSaveHighHeelsPreset()
    {
        string status;
        int objectKey;
        if (!TryGetHighHeelsTarget(out objectKey, out status))
        {
            ReportMmdResult(false, status);
            return;
        }

        ReportMmdResult(VRMmddService.SaveHighHeelsPreset(objectKey, out status), status);
        RefreshHighHeelsVisuals();
    }

    private void HandleLoadHighHeelsPreset()
    {
        string status;
        int objectKey;
        if (!TryGetHighHeelsTarget(out objectKey, out status))
        {
            ReportMmdResult(false, status);
            return;
        }

        ReportMmdResult(VRMmddService.LoadHighHeelsPreset(objectKey, out status), status);
        RefreshHighHeelsVisuals();
    }

    private bool TryGetHighHeelsTarget(out int objectKey, out string status)
    {
        objectKey = _highHeelsTargetObjectKey;
        if (objectKey < 0)
        {
            status = "请先在手环中选择场景角色";
            return false;
        }

        VRVmdActorTarget target;
        if (VRVmdTargetService.TryGetTarget(objectKey, out target, out status)
            && object.ReferenceEquals(_highHeelsTargetCharacter, target.Character))
            return true;

        status = "场景角色已变化，请重新选择高跟鞋目标";
        _highHeelsTargetObjectKey = -1;
        _highHeelsTargetCharacter = null;
        VRMmddStateBridge.ResetHighHeels();
        return false;
    }

    private void ReportMmdResult(bool success, string status)
    {
        if (success)
            VRLog.Info(status);
        else
            VRLog.Warn(status);
        SetStatus(
            status,
            success ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.38f, 0.34f, 1f),
            success ? 4f : 7f);
    }
}

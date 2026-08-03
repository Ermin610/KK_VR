using UnityEngine;
using UnityEngine.UI;
using VRGIN.Core;

namespace KKCharaStudioVR;

public sealed partial class VRWristMenuController
{
    private GameObject _clothingOpacityPage;
    private GameObject _clothingOpacityQuickPage;
    private Text _clothingOpacityTargetText;
    private Text _clothingOpacitySummaryText;
    private Text _clothingOpacityQuickSummaryText;
    private Text _clothingOpacityHintText;
    private readonly VRWristMenuButtonTarget[] _clothingOpacityPartButtons =
        new VRWristMenuButtonTarget[VRCharacterClothingService.PartCount];
    private readonly VRWristMenuButtonTarget[] _clothingOpacityPresetButtons =
        new VRWristMenuButtonTarget[5];
    private VRWristMenuButtonTarget _clothingOpacityMinusButton;
    private VRWristMenuButtonTarget _clothingOpacityPlusButton;
    private VRWristMenuButtonTarget _clothingOpacityFineMinusButton;
    private VRWristMenuButtonTarget _clothingOpacityFinePlusButton;
    private VRWristMenuButtonTarget _clothingOpacityResetButton;
    private VRWristMenuButtonTarget _clothingOpacityCompatibilityButton;
    private int _clothingOpacitySelectedPart;
    private bool _clothingOpacityCompatibilityEnabled = true;

    private void BuildClothingOpacityPage()
    {
        CreateButton(
            "ClothingOpacityBack",
            "<",
            18f,
            10f,
            new Color(1f, 1f, 1f, 0.08f),
            new Color(1f, 1f, 1f, 0.2f),
            HandleBackToClothing,
            _clothingOpacityPage.transform,
            58f,
            38f,
            28);
        CreateText(
            "ClothingOpacityTitle",
            _clothingOpacityPage.transform,
            L(
                "透明度 · 部位与微调",
                "透明度・部位と微調整",
                "Transparency · Part and fine tuning"),
            92f,
            10f,
            444f,
            40f,
            21,
            TextAnchor.MiddleLeft,
            Color.white);

        _clothingOpacityTargetText = CreateText(
            "ClothingOpacityTarget",
            _clothingOpacityPage.transform,
            L("尚未选择角色", "キャラ未選択", "No character selected"),
            24f,
            62f,
            512f,
            24f,
            15,
            TextAnchor.MiddleLeft,
            SecondaryTextColor);

        CreateText(
            "ClothingOpacityPartHeading",
            _clothingOpacityPage.transform,
            L("选择部位", "部位を選択", "Select part"),
            24f,
            91f,
            512f,
            20f,
            17,
            TextAnchor.MiddleLeft,
            new Color(0.25f, 0.86f, 0.94f, 1f));
        for (int index = 0; index < _clothingOpacityPartButtons.Length; index++)
        {
            int partId = index;
            float x = index % 2 == 0 ? 24f : 288f;
            float y = 116f + index / 2 * 34f;
            _clothingOpacityPartButtons[index] = CreateButton(
                "ClothingOpacityPart" + index,
                GetClothingPartLabel(index),
                x,
                y,
                new Color(0.08f, 0.15f, 0.18f, 0.5f),
                new Color(0.12f, 0.32f, 0.39f, 0.76f),
                () => HandleSelectClothingOpacityPart(partId),
                _clothingOpacityPage.transform,
                248f,
                30f,
                15);
        }

        _clothingOpacitySummaryText = CreateText(
            "ClothingOpacitySummary",
            _clothingOpacityPage.transform,
            "--",
            24f,
            256f,
            512f,
            42f,
            15,
            TextAnchor.MiddleLeft,
            PrimaryTextColor);

        CreateText(
            "ClothingOpacityFineHeading",
            _clothingOpacityPage.transform,
            L("透明度微调", "透明度の微調整", "Fine transparency adjustment"),
            24f,
            302f,
            512f,
            20f,
            16,
            TextAnchor.MiddleLeft,
            new Color(0.25f, 0.86f, 0.94f, 1f));

        _clothingOpacityMinusButton = CreateButton(
            "ClothingOpacityMinus",
            "-5%",
            24f,
            326f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            () => HandleAdjustClothingTransparency(-0.05f),
            _clothingOpacityPage.transform,
            122f,
            36f,
            15);
        _clothingOpacityPlusButton = CreateButton(
            "ClothingOpacityPlus",
            "+5%",
            154f,
            326f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            () => HandleAdjustClothingTransparency(0.05f),
            _clothingOpacityPage.transform,
            122f,
            36f,
            15);
        _clothingOpacityFineMinusButton = CreateButton(
            "ClothingOpacityFineMinus",
            "-1%",
            284f,
            326f,
            new Color(0.1f, 0.14f, 0.18f, 0.5f),
            new Color(0.18f, 0.3f, 0.38f, 0.76f),
            () => HandleAdjustClothingTransparency(-0.01f),
            _clothingOpacityPage.transform,
            122f,
            36f,
            14);
        _clothingOpacityFinePlusButton = CreateButton(
            "ClothingOpacityFinePlus",
            "+1%",
            414f,
            326f,
            new Color(0.1f, 0.14f, 0.18f, 0.5f),
            new Color(0.18f, 0.3f, 0.38f, 0.76f),
            () => HandleAdjustClothingTransparency(0.01f),
            _clothingOpacityPage.transform,
            122f,
            36f,
            14);

        CreateButton(
            "OpenClothingOpacityQuick",
            L(
                "快捷与兼容  ›    预设值、恢复与材质说明",
                "クイック・互換  ›    プリセット・復元・材質説明",
                "Quick and compatibility  ›    Presets, reset, and material guide"),
            24f,
            370f,
            new Color(0.12f, 0.17f, 0.3f, 0.5f),
            new Color(0.18f, 0.36f, 0.62f, 0.8f),
            HandleOpenClothingOpacityQuick,
            _clothingOpacityPage.transform,
            512f,
            38f,
            14);
    }

    private void BuildClothingOpacityQuickPage()
    {
        CreateButton(
            "ClothingOpacityQuickBack",
            "<",
            18f,
            10f,
            new Color(1f, 1f, 1f, 0.08f),
            new Color(1f, 1f, 1f, 0.2f),
            HandleBackToClothingOpacity,
            _clothingOpacityQuickPage.transform,
            58f,
            38f,
            28);
        CreateText(
            "ClothingOpacityQuickTitle",
            _clothingOpacityQuickPage.transform,
            L(
                "透明度 · 快捷与兼容",
                "透明度・クイックと互換",
                "Transparency · Quick and compatibility"),
            92f,
            10f,
            444f,
            40f,
            20,
            TextAnchor.MiddleLeft,
            Color.white);

        Image summaryBackground = CreateImage(
            "ClothingOpacityQuickSummaryBackground",
            _clothingOpacityQuickPage.transform,
            24f,
            68f,
            512f,
            52f,
            new Color(0.075f, 0.24f, 0.14f, 0.32f));
        ApplyGlassEffects(
            summaryBackground,
            new Color(0.47f, 0.9f, 0.55f, 0.14f),
            false,
            0f);
        _clothingOpacityQuickSummaryText = CreateText(
            "ClothingOpacityQuickSummary",
            _clothingOpacityQuickPage.transform,
            "--",
            38f,
            70f,
            484f,
            48f,
            14,
            TextAnchor.MiddleLeft,
            PrimaryTextColor);

        CreateText(
            "ClothingOpacityQuickHeading",
            _clothingOpacityQuickPage.transform,
            L("快捷透明度", "クイック透明度", "Quick transparency"),
            24f,
            130f,
            512f,
            20f,
            17,
            TextAnchor.MiddleLeft,
            new Color(0.25f, 0.86f, 0.94f, 1f));
        float[] presets = { 0f, 0.25f, 0.5f, 0.75f, 1f };
        for (int index = 0; index < presets.Length; index++)
        {
            float captured = presets[index];
            _clothingOpacityPresetButtons[index] = CreateButton(
                "ClothingOpacityPreset" + index,
                Mathf.RoundToInt(captured * 100f) + "%",
                24f + index * 103f,
                156f,
                new Color(0.075f, 0.24f, 0.14f, 0.48f),
                new Color(0.11f, 0.46f, 0.24f, 0.76f),
                () => HandleSetClothingTransparency(captured),
                _clothingOpacityQuickPage.transform,
                100f,
                40f,
                15);
        }

        _clothingOpacityResetButton = CreateButton(
            "ClothingOpacityReset",
            L("恢复本部位", "この部位を元に戻す", "Reset this part"),
            24f,
            208f,
            new Color(0.28f, 0.2f, 0.07f, 0.48f),
            new Color(0.55f, 0.36f, 0.1f, 0.74f),
            HandleResetClothingOpacity,
            _clothingOpacityQuickPage.transform,
            248f,
            44f,
            15);
        _clothingOpacityCompatibilityButton = CreateButton(
            "ClothingOpacityCompatibility",
            L("自动兼容  关", "自動互換  オフ", "Auto compatibility  Off"),
            288f,
            208f,
            new Color(0.3f, 0.09f, 0.12f, 0.46f),
            new Color(0.58f, 0.15f, 0.2f, 0.74f),
            HandleToggleClothingOpacityCompatibility,
            _clothingOpacityQuickPage.transform,
            248f,
            44f,
            14);

        CreateText(
            "ClothingOpacityGuideHeading",
            _clothingOpacityQuickPage.transform,
            L("完整说明", "詳しい説明", "Full guide"),
            24f,
            264f,
            512f,
            20f,
            17,
            TextAnchor.MiddleLeft,
            new Color(1f, 0.72f, 0.25f, 1f));
        Image hintBackground = CreateImage(
            "ClothingOpacityHintBackground",
            _clothingOpacityQuickPage.transform,
            24f,
            290f,
            512f,
            112f,
            new Color(0.86f, 0.96f, 1f, 0.08f));
        ApplyGlassEffects(
            hintBackground,
            new Color(0.86f, 0.96f, 1f, 0.12f),
            false,
            0f);
        _clothingOpacityHintText = CreateText(
            "ClothingOpacityHint",
            _clothingOpacityQuickPage.transform,
            L(
                "• 数值越高，衣服越透明；100% 为完全透明。\n• AZ 材质优先切换同族 Alpha，保留原有质感参数。\n• 自动兼容只处理原生不支持透明的材质；关闭时会跳过它们。\n• 恢复本部位会撤销本插件的透明修改并还原原材质。",
                "• 数値が高いほど透明になり、100% で完全透明です。\n• AZ は同系統 Alpha を優先し、元の質感設定を保ちます。\n• 自動互換は透明非対応材質だけを処理し、オフではスキップします。\n• 部位の復元で本プラグインの変更を取り消し、元の材質へ戻します。",
                "• Higher values are more transparent; 100% is fully transparent.\n• AZ uses its matching Alpha shader and keeps the original look settings.\n• Auto compatibility only handles opaque shaders; when off, they are skipped.\n• Reset this part removes this plug-in's changes and restores its materials."),
            38f,
            296f,
            484f,
            100f,
            12,
            TextAnchor.MiddleLeft,
            SecondaryTextColor);
    }

    private void RefreshClothingOpacityPage()
    {
        Studio.OCIChar character;
        string targetStatus;
        bool hasTarget = TryGetClothingTarget(out character, out targetStatus);
        bool[] availableParts = new bool[_clothingOpacityPartButtons.Length];
        int firstAvailablePart = -1;
        if (hasTarget && character.charInfo != null)
        {
            for (int index = 0; index < availableParts.Length; index++)
            {
                availableParts[index] = VRClothingOpacityService.HasPart(
                    character.charInfo,
                    index);
                if (availableParts[index] && firstAvailablePart < 0)
                    firstAvailablePart = index;
            }
        }
        if ((_clothingOpacitySelectedPart < 0
                || _clothingOpacitySelectedPart >= availableParts.Length
                || !availableParts[_clothingOpacitySelectedPart])
            && firstAvailablePart >= 0)
        {
            _clothingOpacitySelectedPart = firstAvailablePart;
        }
        bool selectedPartAvailable = firstAvailablePart >= 0
            && availableParts[_clothingOpacitySelectedPart];
        if (_clothingOpacityTargetText != null)
        {
            _clothingOpacityTargetText.text = hasTarget
                ? L("当前角色  ", "現在のキャラ  ", "Current character  ") + targetStatus
                : L("尚未选择角色", "キャラ未選択", "No character selected");
            _clothingOpacityTargetText.color = hasTarget
                ? SecondaryTextColor
                : new Color(1f, 0.48f, 0.4f, 1f);
        }

        for (int index = 0; index < _clothingOpacityPartButtons.Length; index++)
        {
            VRWristMenuButtonTarget button = _clothingOpacityPartButtons[index];
            if (button == null)
                continue;
            bool available = availableParts[index];
            bool selected = available && index == _clothingOpacitySelectedPart;
            button.SetLabel((selected ? "●  " : string.Empty) + GetClothingPartLabel(index));
            button.SetColors(
                selected
                    ? new Color(0.09f, 0.36f, 0.31f, 0.62f)
                    : new Color(0.08f, 0.15f, 0.18f, 0.5f),
                new Color(0.16f, 0.46f, 0.4f, 0.8f));
            button.SetInteractable(available);
        }

        RefreshClothingOpacityCompatibilityButton();
        if (_clothingOpacityCompatibilityButton != null)
            _clothingOpacityCompatibilityButton.SetInteractable(selectedPartAvailable);
        foreach (VRWristMenuButtonTarget button in _clothingOpacityPresetButtons)
        {
            if (button != null)
                button.SetInteractable(selectedPartAvailable);
        }
        if (_clothingOpacityMinusButton != null)
            _clothingOpacityMinusButton.SetInteractable(selectedPartAvailable);
        if (_clothingOpacityPlusButton != null)
            _clothingOpacityPlusButton.SetInteractable(selectedPartAvailable);
        if (_clothingOpacityFineMinusButton != null)
            _clothingOpacityFineMinusButton.SetInteractable(selectedPartAvailable);
        if (_clothingOpacityFinePlusButton != null)
            _clothingOpacityFinePlusButton.SetInteractable(selectedPartAvailable);
        if (_clothingOpacityResetButton != null)
            _clothingOpacityResetButton.SetInteractable(selectedPartAvailable);
        if (!hasTarget)
        {
            SetClothingOpacitySummary(
                targetStatus,
                new Color(1f, 0.48f, 0.4f, 1f));
            return;
        }
        if (!selectedPartAvailable)
        {
            SetClothingOpacitySummary(
                L(
                    "当前角色没有可调整的服装部位",
                    "現在のキャラには調整可能な衣装部位がありません",
                    "The current character has no adjustable clothing parts"),
                TertiaryTextColor);
            return;
        }

        VRClothingOpacityInfo info;
        string status;
        if (!VRClothingOpacityService.TryInspect(
                character.charInfo,
                _clothingOpacitySelectedPart,
                out info,
                out status))
        {
            SetClothingOpacitySummary(
                status,
                new Color(1f, 0.48f, 0.4f, 1f));
            return;
        }

        float minimumTransparency = 1f - info.MaximumOpacity;
        float maximumTransparency = 1f - info.MinimumOpacity;
        string transparency = info.Mixed
            ? L("混合 ", "混在 ", "Mixed ")
                + Mathf.RoundToInt(minimumTransparency * 100f) + "–"
                + Mathf.RoundToInt(maximumTransparency * 100f) + "%"
            : Mathf.RoundToInt((1f - info.Opacity) * 100f) + "%";
        string summary = GetClothingPartLabel(_clothingOpacitySelectedPart)
            + L("  透明度 ", "  透明度 ", "  Transparency ") + transparency + "\n"
            + L("安全支持 ", "安全対応 ", "Safe ") + info.SupportedCount + "/" + info.MaterialCount
            + L("  可兼容 ", "  互換可能 ", "  Compatible ") + info.CompatibleCount
            + (info.AzPairedCount > 0
                ? L("  AZ同族 ", "  AZ同系統 ", "  AZ pair ") + info.AzPairedCount
                : string.Empty)
            + L("  已保护 ", "  保護済み ", "  Protected ") + info.ProtectedCount
            + L("  不支持 ", "  非対応 ", "  Unsupported ") + info.UnsupportedCount;
        SetClothingOpacitySummary(summary, PrimaryTextColor);
    }

    private void SetClothingOpacitySummary(string value, Color color)
    {
        if (_clothingOpacitySummaryText != null)
        {
            _clothingOpacitySummaryText.text = value;
            _clothingOpacitySummaryText.color = color;
        }
        if (_clothingOpacityQuickSummaryText != null)
        {
            _clothingOpacityQuickSummaryText.text = value;
            _clothingOpacityQuickSummaryText.color = color;
        }
    }

    private void RefreshClothingOpacityCompatibilityButton()
    {
        if (_clothingOpacityCompatibilityButton == null)
            return;
        _clothingOpacityCompatibilityButton.SetLabel(_clothingOpacityCompatibilityEnabled
            ? L("自动兼容  开", "自動互換  オン", "Auto compatibility  On")
            : L("自动兼容  关", "自動互換  オフ", "Auto compatibility  Off"));
        _clothingOpacityCompatibilityButton.SetColors(
            _clothingOpacityCompatibilityEnabled
                ? new Color(0.28f, 0.2f, 0.07f, 0.58f)
                : new Color(0.3f, 0.09f, 0.12f, 0.46f),
            _clothingOpacityCompatibilityEnabled
                ? new Color(0.55f, 0.36f, 0.1f, 0.8f)
                : new Color(0.58f, 0.15f, 0.2f, 0.74f));
    }

    private void HandleOpenClothingOpacity()
    {
        ShowPage(WristMenuPage.ClothingOpacity);
        SetStatus(
            L("选择服装部位并调整透明度", "衣装部位を選んで透明度を調整", "Select a clothing part and adjust transparency"),
            new Color(0.25f, 0.86f, 0.94f, 1f),
            0f);
    }

    private void HandleOpenClothingOpacityQuick()
    {
        ShowPage(WristMenuPage.ClothingOpacityQuick);
        SetStatus(
            L(
                "使用快捷透明度，或调整材质兼容方式",
                "クイック透明度または材質の互換方式を調整します",
                "Use a quick transparency value or adjust material compatibility"),
            new Color(0.25f, 0.86f, 0.94f, 1f),
            0f);
    }

    private void HandleBackToClothingOpacity()
    {
        ShowPage(WristMenuPage.ClothingOpacity);
        SetStatus(
            L(
                "选择服装部位并微调透明度",
                "衣装部位を選んで透明度を微調整します",
                "Select a clothing part and fine-tune transparency"),
            new Color(0.25f, 0.86f, 0.94f, 1f),
            0f);
    }

    private void HandleSelectClothingOpacityPart(int partId)
    {
        Studio.OCIChar character;
        string status;
        if (!TryGetClothingTarget(out character, out status)
            || character.charInfo == null
            || !VRClothingOpacityService.HasPart(character.charInfo, partId))
        {
            SetStatus(
                L("这个部位没有衣服", "この部位には衣装がありません", "There is no clothing in this part"),
                TertiaryTextColor,
                3f);
            return;
        }
        _clothingOpacitySelectedPart = Mathf.Clamp(
            partId,
            0,
            VRCharacterClothingService.PartCount - 1);
        RefreshClothingOpacityPage();
    }

    private void HandleSetClothingTransparency(float transparency)
    {
        Studio.OCIChar character;
        string status;
        bool hasTarget = TryGetClothingTarget(out character, out status);
        VRClothingOpacityInfo info;
        float materialOpacity = 1f - Mathf.Clamp01(transparency);
        bool success = hasTarget && VRClothingOpacityService.TrySetPartOpacity(
            character.charInfo,
            _clothingOpacitySelectedPart,
            materialOpacity,
            _clothingOpacityCompatibilityEnabled,
            out info,
            out status);
        SetStatus(
            status,
            success ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.38f, 0.34f, 1f),
            5f);
        if (success)
            VRLog.Info(status);
        else
            VRLog.Warn(status);
        RefreshClothingOpacityPage();
    }

    private void HandleAdjustClothingTransparency(float delta)
    {
        Studio.OCIChar character;
        string status;
        if (!TryGetClothingTarget(out character, out status))
        {
            SetStatus(status, new Color(1f, 0.38f, 0.34f, 1f), 5f);
            return;
        }

        VRClothingOpacityInfo info;
        if (!VRClothingOpacityService.TryInspect(
                character.charInfo,
                _clothingOpacitySelectedPart,
                out info,
                out status))
        {
            SetStatus(status, new Color(1f, 0.38f, 0.34f, 1f), 5f);
            RefreshClothingOpacityPage();
            return;
        }
        float currentTransparency = 1f - Mathf.Clamp01(info.Opacity);
        HandleSetClothingTransparency(Mathf.Clamp01(currentTransparency + delta));
    }

    private void HandleResetClothingOpacity()
    {
        Studio.OCIChar character;
        string status;
        bool hasTarget = TryGetClothingTarget(out character, out status);
        VRClothingOpacityInfo info;
        bool success = hasTarget && VRClothingOpacityService.TryResetPart(
            character.charInfo,
            _clothingOpacitySelectedPart,
            out info,
            out status);
        SetStatus(
            status,
            success ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.62f, 0.24f, 1f),
            5f);
        if (success)
            VRLog.Info(status);
        else
            VRLog.Warn(status);
        RefreshClothingOpacityPage();
    }

    private void HandleToggleClothingOpacityCompatibility()
    {
        _clothingOpacityCompatibilityEnabled = !_clothingOpacityCompatibilityEnabled;
        RefreshClothingOpacityCompatibilityButton();
        SetStatus(
            _clothingOpacityCompatibilityEnabled
                ? L(
                    "自动兼容已开启：AZ 使用同族 Alpha，其他材质使用安全兼容",
                    "自動互換オン：AZ は同系統 Alpha、その他は安全な互換処理",
                    "Auto compatibility enabled: AZ uses matching Alpha; other materials use the safe fallback")
                : L(
                    "自动兼容已关闭：只调整原生支持透明的材质",
                    "自動互換を無効化：対応材質のみ調整",
                    "Auto compatibility disabled: only natively transparent materials will change"),
            _clothingOpacityCompatibilityEnabled
                ? new Color(1f, 0.72f, 0.25f, 1f)
                : new Color(0.78f, 0.86f, 0.9f, 1f),
            5f);
    }

    private void ClearClothingOpacityMenuReferences()
    {
        _clothingOpacityPage = null;
        _clothingOpacityQuickPage = null;
        _clothingOpacityTargetText = null;
        _clothingOpacitySummaryText = null;
        _clothingOpacityQuickSummaryText = null;
        _clothingOpacityHintText = null;
        for (int index = 0; index < _clothingOpacityPartButtons.Length; index++)
            _clothingOpacityPartButtons[index] = null;
        for (int index = 0; index < _clothingOpacityPresetButtons.Length; index++)
            _clothingOpacityPresetButtons[index] = null;
        _clothingOpacityMinusButton = null;
        _clothingOpacityPlusButton = null;
        _clothingOpacityFineMinusButton = null;
        _clothingOpacityFinePlusButton = null;
        _clothingOpacityResetButton = null;
        _clothingOpacityCompatibilityButton = null;
    }
}

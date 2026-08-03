using System.Collections;
using Studio;
using UnityEngine;
using UnityEngine.UI;
using VRGIN.Core;

namespace KKCharaStudioVR;

public sealed partial class VRWristMenuController
{
    private const int CoordinateVisibleSlots = 8;

    private readonly VRWristMenuButtonTarget[] _coordinateButtons =
        new VRWristMenuButtonTarget[CoordinateVisibleSlots];
    private GameObject _coordinatePage;
    private Text _coordinateTargetText;
    private Text _coordinatePageText;
    private VRWristMenuButtonTarget _coordinatePresetButton;
    private VRWristMenuButtonTarget _coordinatePreviousButton;
    private VRWristMenuButtonTarget _coordinateNextButton;
    private int _coordinatePageIndex;

    private void BuildCoordinatePage()
    {
        CreateButton(
            "CoordinateBack",
            "<",
            18f,
            10f,
            new Color(1f, 1f, 1f, 0.08f),
            new Color(1f, 1f, 1f, 0.2f),
            HandleBackToClothing,
            _coordinatePage.transform,
            58f,
            38f,
            28);
        CreateText(
            "CoordinateTitle",
            _coordinatePage.transform,
            L("角色预设服装", "キャラ衣装プリセット", "Character outfit presets"),
            92f,
            10f,
            444f,
            40f,
            27,
            TextAnchor.MiddleLeft,
            Color.white);

        Image targetBackground = CreateImage(
            "CoordinateTargetBackground",
            _coordinatePage.transform,
            24f,
            68f,
            512f,
            40f,
            new Color(0.075f, 0.24f, 0.14f, 0.32f));
        ApplyGlassEffects(targetBackground, new Color(0.47f, 0.9f, 0.55f, 0.14f), false, 0f);
        _coordinateTargetText = CreateText(
            "CoordinateTarget",
            _coordinatePage.transform,
            L("当前角色：-", "現在のキャラ：-", "Current character: -"),
            38f,
            70f,
            484f,
            36f,
            17,
            TextAnchor.MiddleLeft,
            new Color(0.72f, 0.95f, 0.78f, 1f));

        CreateText(
            "CoordinateHint",
            _coordinatePage.transform,
            L(
                "切换角色卡内置服装；衣服、鞋子与配饰会一起读取",
                "カード内の衣装を切替（衣服・靴・アクセサリーを一括読込）",
                "Switch card outfits; clothes, shoes, and accessories load together"),
            24f,
            116f,
            512f,
            24f,
            16,
            TextAnchor.MiddleLeft,
            new Color(0.64f, 0.72f, 0.8f, 1f));

        for (int i = 0; i < _coordinateButtons.Length; i++)
        {
            int coordinateIndex = i;
            float x = i % 2 == 0 ? 24f : 288f;
            float y = 148f + i / 2 * 58f;
            _coordinateButtons[i] = CreateButton(
                "CoordinatePreset" + i,
                GetCoordinatePresetLabel(i),
                x,
                y,
                new Color(0.08f, 0.15f, 0.18f, 0.5f),
                new Color(0.12f, 0.32f, 0.39f, 0.76f),
                () => HandleCoordinatePreset(coordinateIndex),
                _coordinatePage.transform,
                248f,
                48f,
                18);
        }

        _coordinatePreviousButton = CreateButton(
            "CoordinatePrevious",
            L("‹ 上一页", "‹ 前へ", "‹ Previous"),
            24f,
            378f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            HandlePreviousCoordinatePage,
            _coordinatePage.transform,
            112f,
            30f,
            15);
        _coordinatePageText = CreateText(
            "CoordinatePageNumber",
            _coordinatePage.transform,
            string.Empty,
            144f,
            380f,
            272f,
            26f,
            14,
            TextAnchor.MiddleCenter,
            new Color(0.48f, 0.54f, 0.63f, 1f));
        _coordinateNextButton = CreateButton(
            "CoordinateNext",
            L("下一页 ›", "次へ ›", "Next ›"),
            424f,
            378f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            HandleNextCoordinatePage,
            _coordinatePage.transform,
            112f,
            30f,
            15);
    }

    private string GetCoordinatePresetLabel(int coordinateIndex, OCIChar character = null)
    {
        switch (coordinateIndex)
        {
            case 0:
                return L("校服 1", "制服 1", "School 1");
            case 1:
                return L("校服 2", "制服 2", "School 2");
            case 2:
                return L("运动服", "体操着", "Gym clothes");
            case 3:
                return L("泳装", "水着", "Swimwear");
            case 4:
                return L("社团服", "部活着", "Club outfit");
            case 5:
                return L("便服", "私服", "Casual");
            case 6:
                return L("睡衣", "パジャマ", "Pajamas");
            default:
                string name = VRCharacterClothingService.GetCoordinateName(character, coordinateIndex);
                string numberedFallback = "服装 " + (coordinateIndex + 1);
                return string.Equals(name, numberedFallback)
                    ? L(numberedFallback, "衣装 " + (coordinateIndex + 1), "Outfit " + (coordinateIndex + 1))
                    : name;
        }
    }

    private void HandleOpenCoordinatePresets()
    {
        OCIChar character;
        string status;
        if (!TryGetClothingTarget(out character, out status))
        {
            SetStatus(status, new Color(1f, 0.38f, 0.34f, 1f), 6f);
            return;
        }

        int currentCoordinate = VRCharacterClothingService.GetCurrentCoordinateIndex(character);
        _coordinatePageIndex = currentCoordinate >= 0
            ? currentCoordinate / CoordinateVisibleSlots
            : 0;
        ShowPage(WristMenuPage.Coordinate);
        SetStatus(
            L("请选择角色预设服装", "衣装プリセットを選択してください", "Select a character outfit preset"),
            new Color(0.47f, 0.9f, 0.55f, 1f),
            0f);
    }

    private void HandlePreviousCoordinatePage()
    {
        if (_operationInProgress || _coordinatePageIndex <= 0)
            return;

        _coordinatePageIndex--;
        RefreshCoordinatePage();
    }

    private void HandleNextCoordinatePage()
    {
        if (_operationInProgress)
            return;

        OCIChar character;
        string status;
        if (!TryGetClothingTarget(out character, out status))
        {
            SetStatus(status, new Color(1f, 0.38f, 0.34f, 1f), 6f);
            RefreshCoordinatePage();
            return;
        }

        int count = VRCharacterClothingService.GetCoordinateCount(character);
        int pageCount = Mathf.Max(1, (count + CoordinateVisibleSlots - 1) / CoordinateVisibleSlots);
        if (_coordinatePageIndex + 1 >= pageCount)
            return;

        _coordinatePageIndex++;
        RefreshCoordinatePage();
    }

    private void HandleBackToClothing()
    {
        ShowPage(WristMenuPage.Clothing);
        SetStatus(L("换装", "着替え", "Outfits"), new Color(0.47f, 0.9f, 0.55f, 1f), 0f);
    }

    private void HandleCoordinatePreset(int visibleSlot)
    {
        if (_operationInProgress)
            return;

        OCIChar character;
        string status;
        if (!TryGetClothingTarget(out character, out status))
        {
            SetStatus(status, new Color(1f, 0.38f, 0.34f, 1f), 6f);
            RefreshCoordinatePage();
            return;
        }
        if (character.charInfo != null && !character.charInfo.loadEnd)
        {
            SetStatus("预设服装仍在读取，请稍后", new Color(1f, 0.72f, 0.25f, 1f), 6f);
            return;
        }

        int coordinateIndex = _coordinatePageIndex * CoordinateVisibleSlots + visibleSlot;
        StartCoroutine(ApplyCoordinatePresetAfterFrame(coordinateIndex, character));
    }

    private IEnumerator ApplyCoordinatePresetAfterFrame(int coordinateIndex, OCIChar character)
    {
        _operationInProgress = true;
        SetStatus(
            L("正在切换预设服装…", "衣装プリセットを切り替えています…", "Switching outfit preset…"),
            new Color(1f, 0.72f, 0.25f, 1f),
            0f);
        yield return null;

        string status;
        OCIChar characterBeforeSwitch;
        string characterBeforeSwitchStatus;
        bool targetStillMatches = TryGetClothingTarget(
                out characterBeforeSwitch,
                out characterBeforeSwitchStatus)
            && object.ReferenceEquals(character, characterBeforeSwitch);
        bool success;
        if (!targetStillMatches)
        {
            success = false;
            status = characterBeforeSwitchStatus;
        }
        else
        {
            success = VRCharacterClothingService.TrySetCoordinate(
                _clothingTargetObjectKey,
                coordinateIndex,
                out status);
        }
        bool timedOut = false;
        if (success && character != null && character.charInfo != null)
        {
            // SetCoordinateInfo starts Studio's asynchronous clothing reload. Give it
            // one frame to enter the loading state, then keep the menu locked until it
            // finishes so rapid clicks cannot overlap two coordinate reloads.
            yield return null;
            float deadline = Time.realtimeSinceStartup + 15f;
            while (character != null
                && character.charInfo != null
                && !character.charInfo.loadEnd
                && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            timedOut = character != null
                && character.charInfo != null
                && !character.charInfo.loadEnd;
            yield return null;

            OCIChar liveCharacter;
            string liveStatus;
            if (!TryGetClothingTarget(out liveCharacter, out liveStatus)
                || !object.ReferenceEquals(character, liveCharacter))
            {
                success = false;
                status = liveStatus;
            }
            else if (!timedOut
                && VRCharacterClothingService.GetCurrentCoordinateIndex(liveCharacter) != coordinateIndex)
            {
                success = false;
                status = "预设服装切换失败：工作室没有接受切换请求";
            }
            else if (!timedOut)
            {
                VRCharacterClothingService.RefreshStudioCharacterPanel(liveCharacter);
                if (VRMmdPlaybackController.Instance != null)
                    VRMmdPlaybackController.Instance.RequestHighHeelsRefresh(_clothingTargetObjectKey);
            }
        }

        _operationInProgress = false;
        RefreshCoordinatePage();
        RefreshClothingPage();
        if (success)
            VRLog.Info(status);
        SetStatus(
            timedOut ? "预设服装仍在读取，请稍后" : status,
            !success
                ? new Color(1f, 0.38f, 0.34f, 1f)
                : timedOut
                    ? new Color(1f, 0.72f, 0.25f, 1f)
                    : new Color(0.35f, 1f, 0.62f, 1f),
            timedOut ? 7f : success ? 5f : 7f);
    }

    private void RefreshCoordinatePage()
    {
        OCIChar character;
        string status;
        bool hasCharacter = TryGetClothingTarget(out character, out status);
        int coordinateCount = hasCharacter
            ? VRCharacterClothingService.GetCoordinateCount(character)
            : 0;
        int currentCoordinate = hasCharacter
            ? VRCharacterClothingService.GetCurrentCoordinateIndex(character)
            : -1;
        int pageCount = Mathf.Max(1, (coordinateCount + CoordinateVisibleSlots - 1) / CoordinateVisibleSlots);
        _coordinatePageIndex = Mathf.Clamp(_coordinatePageIndex, 0, pageCount - 1);
        int coordinateOffset = _coordinatePageIndex * CoordinateVisibleSlots;

        if (_coordinateTargetText != null)
        {
            _coordinateTargetText.text = hasCharacter
                ? L("当前角色：", "現在のキャラ：", "Current character: ") + status
                    + (currentCoordinate >= 0
                        ? L(" · 当前：", " · 現在：", " · Current: ")
                            + GetCoordinatePresetLabel(currentCoordinate, character)
                        : string.Empty)
                : L("当前角色：未选择", "現在のキャラ：未選択", "Current character: Not selected");
        }

        for (int i = 0; i < _coordinateButtons.Length; i++)
        {
            VRWristMenuButtonTarget button = _coordinateButtons[i];
            if (button == null)
                continue;

            int coordinateIndex = coordinateOffset + i;
            bool visible = hasCharacter && coordinateIndex < coordinateCount;
            button.SetVisible(visible);
            if (!visible)
                continue;

            bool selected = coordinateIndex == currentCoordinate;
            button.SetLabel((selected ? "●  " : string.Empty)
                + GetCoordinatePresetLabel(coordinateIndex, character));
            button.SetColors(
                selected
                    ? new Color(0.075f, 0.24f, 0.14f, 0.66f)
                    : new Color(0.08f, 0.15f, 0.18f, 0.5f),
                selected
                    ? new Color(0.11f, 0.46f, 0.24f, 0.82f)
                    : new Color(0.12f, 0.32f, 0.39f, 0.76f));
        }

        bool showPager = hasCharacter && pageCount > 1;
        _coordinatePreviousButton?.SetVisible(showPager && _coordinatePageIndex > 0);
        _coordinateNextButton?.SetVisible(showPager && _coordinatePageIndex + 1 < pageCount);
        if (_coordinatePageText != null)
        {
            _coordinatePageText.gameObject.SetActive(showPager);
            if (showPager)
            {
                int first = coordinateOffset + 1;
                int last = Mathf.Min(coordinateOffset + CoordinateVisibleSlots, coordinateCount);
                _coordinatePageText.text = (_coordinatePageIndex + 1) + " / " + pageCount
                    + "  ·  " + first + "–" + last + " / " + coordinateCount;
            }
        }
    }

    private void ClearCoordinateMenuReferences()
    {
        _coordinatePage = null;
        _coordinateTargetText = null;
        _coordinatePageText = null;
        _coordinatePresetButton = null;
        _coordinatePreviousButton = null;
        _coordinateNextButton = null;
        _coordinatePageIndex = 0;
        for (int i = 0; i < _coordinateButtons.Length; i++)
            _coordinateButtons[i] = null;
    }
}

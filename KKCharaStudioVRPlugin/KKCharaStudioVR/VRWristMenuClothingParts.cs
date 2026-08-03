using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

namespace KKCharaStudioVR;

public sealed partial class VRWristMenuController
{
    private const float ClothingScrollViewportTop = 158f;
    private const float ClothingScrollViewportHeight = 250f;
    private const float ClothingScrollContentHeight = 350f;
    private const float ClothingScrollStep = 48f;

    private GameObject _clothingPartsPage;
    private Text _clothingPartsTargetText;
    private RectTransform _clothingScrollViewport;
    private RectTransform _clothingScrollContent;
    private RectTransform _clothingScrollThumb;
    private float _clothingScrollOffset;
    private int _clothingStickDirection;
    private float _nextClothingStickScroll;

    private void BuildClothingScrollViewport()
    {
        GameObject viewportObject = CreateRectObject(
            "ClothingScrollViewport",
            _clothingPage.transform,
            0f,
            ClothingScrollViewportTop,
            MenuWidth,
            ClothingScrollViewportHeight,
            _visibleLayer);
        _clothingScrollViewport = viewportObject.GetComponent<RectTransform>();
        viewportObject.AddComponent<RectMask2D>();

        GameObject contentObject = CreateRectObject(
            "ClothingScrollContent",
            _clothingScrollViewport,
            0f,
            0f,
            MenuWidth,
            ClothingScrollContentHeight,
            _visibleLayer);
        _clothingScrollContent = contentObject.GetComponent<RectTransform>();

        Image track = CreateImage(
            "ClothingScrollTrack",
            _clothingPage.transform,
            545f,
            ClothingScrollViewportTop + 4f,
            4f,
            ClothingScrollViewportHeight - 8f,
            new Color(0.72f, 0.84f, 0.94f, 0.1f),
            false);
        Image thumb = CreateImage(
            "ClothingScrollThumb",
            track.transform,
            0f,
            0f,
            4f,
            72f,
            new Color(0.35f, 0.9f, 0.94f, 0.72f),
            false);
        _clothingScrollThumb = thumb.rectTransform;
        SetClothingScrollOffset(0f);
    }

    private void BuildClothingPartsPage()
    {
        CreateText(
            "ClothingPartsHeading",
            _clothingScrollContent,
            L("点击部位循环切换穿脱状态", "部位を押して着脱状態を順番に切替", "Press a part to cycle its dress state"),
            24f,
            100f,
            512f,
            22f,
            16,
            TextAnchor.MiddleLeft,
            new Color(0.25f, 0.86f, 0.94f, 1f));

        for (int index = 0; index < _clothingPartButtons.Length; index++)
        {
            int partId = index;
            float x = index % 2 == 0 ? 24f : 288f;
            float y = 126f + index / 2 * 48f;
            _clothingPartButtons[index] = CreateButton(
                "ClothingPart" + index,
                GetClothingPartLabel(index) + "  -",
                x,
                y,
                new Color(0.08f, 0.15f, 0.18f, 0.5f),
                new Color(0.12f, 0.32f, 0.39f, 0.76f),
                () => HandleCycleClothingPart(partId),
                _clothingScrollContent,
                248f,
                42f,
                16);
        }

        CreateText(
            "ClothingPartsHint",
            _clothingScrollContent,
            L(
                "状态会按该服装实际支持的顺序循环；没有对应衣物的部位会显示不可用。",
                "状態は衣装が対応する順番で循環し、該当衣装がない部位は利用不可と表示されます。",
                "States cycle through those supported by the item; missing parts are shown as unavailable."),
            24f,
            322f,
            512f,
            28f,
            12,
            TextAnchor.MiddleLeft,
            TertiaryTextColor);
    }

    private void RefreshClothingPartsPage()
    {
        Studio.OCIChar character;
        string selectionStatus;
        bool hasCharacter = TryGetClothingTarget(
            out character,
            out selectionStatus);
        if (_clothingPartsTargetText != null)
        {
            _clothingPartsTargetText.text = hasCharacter
                ? L("当前角色：", "現在のキャラ：", "Current character: ") + selectionStatus
                : L("尚未选择角色", "キャラ未選択", "No character selected");
            _clothingPartsTargetText.color = hasCharacter
                ? new Color(0.72f, 0.95f, 0.78f, 1f)
                : new Color(1f, 0.48f, 0.4f, 1f);
        }

        for (int index = 0; index < _clothingPartButtons.Length; index++)
        {
            VRWristMenuButtonTarget button = _clothingPartButtons[index];
            if (button == null)
                continue;
            string state = hasCharacter
                ? LocalizeClothingStateLabel(
                    VRCharacterClothingService.GetPartStateLabel(character, index))
                : "-";
            bool available = hasCharacter
                && VRCharacterClothingService.HasPart(character, index);
            button.SetLabel(GetClothingPartLabel(index) + "  " + state);
            button.SetColors(
                available
                    ? new Color(0.08f, 0.15f, 0.18f, 0.5f)
                    : new Color(0.16f, 0.17f, 0.19f, 0.34f),
                available
                    ? new Color(0.12f, 0.32f, 0.39f, 0.76f)
                    : new Color(0.16f, 0.17f, 0.19f, 0.34f));
            button.SetInteractable(available);
        }
    }

    private void UpdateClothingScroll(
        SteamVR_Controller.Device rightDevice,
        bool pointerInsideViewport)
    {
        if (_page != WristMenuPage.Clothing
            || _clothingScrollContent == null
            || !pointerInsideViewport)
        {
            _clothingStickDirection = 0;
            return;
        }

        float wheel = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(wheel) > 0.001f)
            SetClothingScrollOffset(_clothingScrollOffset - wheel * 480f);

        float axis = rightDevice.GetAxis(EVRButtonId.k_EButton_Axis0).y;
        int direction = axis > BrowserStickThreshold
            ? -1
            : axis < -BrowserStickThreshold
                ? 1
                : 0;
        if (Mathf.Abs(axis) < BrowserStickReleaseThreshold)
        {
            _clothingStickDirection = 0;
            return;
        }
        if (direction == 0)
            return;

        bool shouldScroll = direction != _clothingStickDirection
            || Time.unscaledTime >= _nextClothingStickScroll;
        if (!shouldScroll)
            return;

        float previous = _clothingScrollOffset;
        SetClothingScrollOffset(previous + direction * ClothingScrollStep);
        bool changed = !Mathf.Approximately(previous, _clothingScrollOffset);
        _clothingStickDirection = direction;
        _nextClothingStickScroll = Time.unscaledTime + (changed ? 0.13f : 0.22f);
        if (changed)
            rightDevice.TriggerHapticPulse(120, EVRButtonId.k_EButton_Axis0);
    }

    private bool IsPointInsideClothingScrollViewport(Vector3 point)
    {
        if (_page != WristMenuPage.Clothing || _menuRect == null)
            return false;
        Vector3 local = _menuRect.InverseTransformPoint(point);
        float logicalY = -local.y;
        return local.x >= 0f
            && local.x <= MenuWidth
            && logicalY >= ClothingScrollViewportTop
            && logicalY <= ClothingScrollViewportTop + ClothingScrollViewportHeight;
    }

    private void SetClothingScrollOffset(float offset)
    {
        float maximum = Mathf.Max(0f, ClothingScrollContentHeight - ClothingScrollViewportHeight);
        _clothingScrollOffset = Mathf.Clamp(offset, 0f, maximum);
        if (_clothingScrollContent != null)
            _clothingScrollContent.anchoredPosition = new Vector2(0f, _clothingScrollOffset);

        if (_clothingScrollThumb == null)
            return;
        RectTransform track = _clothingScrollThumb.parent as RectTransform;
        if (track == null)
            return;
        float trackHeight = track.rect.height;
        float thumbHeight = Mathf.Clamp(
            trackHeight * ClothingScrollViewportHeight / ClothingScrollContentHeight,
            28f,
            trackHeight);
        _clothingScrollThumb.sizeDelta = new Vector2(_clothingScrollThumb.sizeDelta.x, thumbHeight);
        float travel = Mathf.Max(0f, trackHeight - thumbHeight);
        float normalized = maximum <= 0f ? 0f : _clothingScrollOffset / maximum;
        _clothingScrollThumb.anchoredPosition = new Vector2(0f, -travel * normalized);
    }

    private bool IsMenuButtonHitVisible(VRWristMenuButtonTarget candidate, Vector3 hitPoint)
    {
        if (_page != WristMenuPage.Clothing
            || candidate == null
            || _clothingScrollContent == null
            || !candidate.transform.IsChildOf(_clothingScrollContent))
        {
            return true;
        }

        Vector3 local = _menuRect.InverseTransformPoint(hitPoint);
        float logicalY = -local.y;
        return logicalY >= ClothingScrollViewportTop
            && logicalY <= ClothingScrollViewportTop + ClothingScrollViewportHeight;
    }

    private void HandleOpenClothingParts()
    {
        ShowPage(WristMenuPage.Clothing);
        SetStatus(
            L(
                "选择服装部位并切换穿脱状态",
                "衣装部位を選んで着脱状態を切り替えます",
                "Select a clothing part and cycle its dress state"),
            new Color(0.25f, 0.86f, 0.94f, 1f),
            0f);
    }

    private void ClearClothingSubmenuReferences()
    {
        _clothingPartsPage = null;
        _clothingPartsTargetText = null;
        _clothingScrollViewport = null;
        _clothingScrollContent = null;
        _clothingScrollThumb = null;
        _clothingScrollOffset = 0f;
        _clothingStickDirection = 0;
        ClearClothingOpacityMenuReferences();
    }
}

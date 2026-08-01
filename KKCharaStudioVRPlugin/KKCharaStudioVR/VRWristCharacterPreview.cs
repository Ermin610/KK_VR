using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using VRGIN.Core;

namespace KKCharaStudioVR;

public sealed partial class VRWristMenuController
{
    private enum CardPreviewKind
    {
        Character,
        Coordinate
    }

    private const float CharacterPreviewAreaX = 168f;
    private const float CharacterPreviewAreaY = 58f;
    private const float CharacterPreviewAreaWidth = 224f;
    private const float CharacterPreviewAreaHeight = 260f;

    private RawImage _characterPreviewImage;
    private RectTransform _characterPreviewRect;
    private Text _characterPreviewMessageText;
    private Text _characterPreviewNameText;
    private Text _characterPreviewTitleText;
    private VRWristMenuButtonTarget _characterPreviewLoadButton;
    private VRWristMenuButtonTarget _characterPreviewReplaceButton;
    private VRWristMenuButtonTarget _coordinateClothesOnlyButton;
    private VRWristMenuButtonTarget _coordinateCustomButton;
    private VRWristMenuButtonTarget _coordinateFullButton;
    private VRWristMenuButtonTarget _coordinateUndoButton;
    private Texture2D _characterPreviewTexture;
    private string _pendingCharacterCardPath;
    private int _characterPreviewRequestId;
    private bool _characterPreviewLoading;
    private BrowserMode _characterCardListMode;
    private string _characterCardListRoot;
    private string _characterCardListDirectory;
    private int _characterCardListOffset;
    private CardPreviewKind _cardPreviewKind;
    private VRCoordinateReplaceMode _pendingCoordinateReplaceMode = VRCoordinateReplaceMode.ClothesOnly;
    private int[] _pendingCoordinateAccessorySlots = new int[0];
    private int _lastCoordinateUndoObjectKey = -1;
    private Studio.OCIChar _lastCoordinateUndoCharacter;

    private void BuildCharacterPreviewPage()
    {
        CreateButton(
            "CharacterPreviewBack",
            "<",
            18f,
            10f,
            new Color(1f, 1f, 1f, 0.08f),
            new Color(1f, 1f, 1f, 0.2f),
            HandleCharacterPreviewBack,
            _characterPreviewPage.transform,
            58f,
            38f,
            28);
        _characterPreviewTitleText = CreateText(
            "CharacterPreviewTitle",
            _characterPreviewPage.transform,
            L("角色卡预览", "キャラカードのプレビュー", "Character card preview"),
            92f,
            10f,
            444f,
            40f,
            28,
            TextAnchor.MiddleLeft,
            Color.white);

        Image previewBackground = CreateImage(
            "CharacterPreviewBackground",
            _characterPreviewPage.transform,
            CharacterPreviewAreaX,
            CharacterPreviewAreaY,
            CharacterPreviewAreaWidth,
            CharacterPreviewAreaHeight,
            new Color(0.02f, 0.03f, 0.035f, 0.72f));
        ApplyGlassEffects(
            previewBackground,
            new Color(0.82f, 0.96f, 1f, 0.16f),
            true,
            2f);

        GameObject previewObject = CreateRectObject(
            "CharacterPreviewImage",
            _characterPreviewPage.transform,
            CharacterPreviewAreaX,
            CharacterPreviewAreaY,
            CharacterPreviewAreaWidth,
            CharacterPreviewAreaHeight,
            _visibleLayer);
        _characterPreviewRect = previewObject.GetComponent<RectTransform>();
        _characterPreviewImage = previewObject.AddComponent<RawImage>();
        _characterPreviewImage.color = Color.white;
        _characterPreviewImage.raycastTarget = false;
        _characterPreviewImage.enabled = false;

        _characterPreviewMessageText = CreateText(
            "CharacterPreviewMessage",
            _characterPreviewPage.transform,
            L("请选择角色卡", "キャラカードを選択", "Select a character card"),
            CharacterPreviewAreaX + 12f,
            CharacterPreviewAreaY + 12f,
            CharacterPreviewAreaWidth - 24f,
            CharacterPreviewAreaHeight - 24f,
            18,
            TextAnchor.MiddleCenter,
            new Color(0.72f, 0.84f, 0.9f, 1f));

        _characterPreviewNameText = CreateText(
            "CharacterPreviewName",
            _characterPreviewPage.transform,
            L("尚未选择角色卡", "キャラカード未選択", "No character card selected"),
            24f,
            322f,
            512f,
            28f,
            17,
            TextAnchor.MiddleCenter,
            new Color(0.72f, 0.9f, 0.96f, 1f));

        _characterPreviewLoadButton = CreateButton(
            "CharacterPreviewLoad",
            L("添加为新角色", "新しいキャラとして追加", "Add as a new character"),
            24f,
            356f,
            new Color(0.075f, 0.24f, 0.14f, 0.5f),
            new Color(0.11f, 0.46f, 0.24f, 0.78f),
            HandleCharacterPreviewLoad,
            _characterPreviewPage.transform,
            248f,
            50f,
            17);
        _characterPreviewReplaceButton = CreateButton(
            "CharacterPreviewReplace",
            L("替换场景角色", "シーンのキャラを置換", "Replace a scene character"),
            288f,
            356f,
            new Color(0.08f, 0.15f, 0.18f, 0.5f),
            new Color(0.12f, 0.32f, 0.39f, 0.76f),
            HandleCharacterPreviewReplace,
            _characterPreviewPage.transform,
            248f,
            50f,
            17);

        const float coordinateButtonWidth = 122f;
        _coordinateClothesOnlyButton = CreateButton(
            "CoordinateClothesOnly",
            L("只换衣服\n不动饰品", "服だけ変更\nアクセは維持", "Clothes only\nKeep accessories"),
            24f,
            356f,
            new Color(0.075f, 0.24f, 0.14f, 0.5f),
            new Color(0.11f, 0.46f, 0.24f, 0.78f),
            HandleCoordinateClothesOnly,
            _characterPreviewPage.transform,
            coordinateButtonWidth,
            50f,
            15);
        _coordinateCustomButton = CreateButton(
            "CoordinateCustomAccessories",
            L("自选饰品\n追加空槽", "アクセ選択\n空き枠へ追加", "Pick accessories\nAdd to empty slots"),
            154f,
            356f,
            new Color(0.07f, 0.19f, 0.24f, 0.5f),
            new Color(0.1f, 0.4f, 0.5f, 0.78f),
            HandleCoordinateCustomAccessories,
            _characterPreviewPage.transform,
            coordinateButtonWidth,
            50f,
            15);
        _coordinateFullButton = CreateButton(
            "CoordinateFullReplace",
            L("完整替换\n会覆盖饰品", "完全適用\nアクセ上書き", "Full replace\nOverwrite accessories"),
            284f,
            356f,
            new Color(0.28f, 0.13f, 0.08f, 0.5f),
            new Color(0.58f, 0.25f, 0.1f, 0.8f),
            HandleCoordinateFullReplace,
            _characterPreviewPage.transform,
            coordinateButtonWidth,
            50f,
            14);
        _coordinateUndoButton = CreateButton(
            "CoordinateUndo",
            L("撤销\n上次换装", "元に戻す\n直前の着替え", "Undo\nLast outfit"),
            414f,
            356f,
            new Color(0.17f, 0.15f, 0.23f, 0.5f),
            new Color(0.34f, 0.28f, 0.52f, 0.78f),
            HandleCoordinateUndo,
            _characterPreviewPage.transform,
            coordinateButtonWidth,
            50f,
            15);

        SetCoordinatePreviewActionsVisible(false);
    }

    private void OpenCharacterCardPreview(string path)
    {
        OpenCardPreview(path, CardPreviewKind.Character);
    }

    private void OpenCoordinateCardPreview(string path)
    {
        OpenCardPreview(path, CardPreviewKind.Coordinate);
    }

    private void OpenCardPreview(string path, CardPreviewKind kind)
    {
        if (_operationInProgress)
        {
            SetStatus(
                "上一项操作尚未完成",
                new Color(1f, 0.72f, 0.25f, 1f),
                4f);
            return;
        }

        _cardPreviewKind = kind;
        _characterCardListMode = _browserMode;
        _characterCardListRoot = _browserRoot;
        _characterCardListDirectory = _browserDirectory;
        _characterCardListOffset = _browserOffset;
        int requestId = ++_characterPreviewRequestId;
        _characterPreviewLoading = true;
        _pendingCharacterCardPath = null;
        ReleaseCharacterPreviewTexture();
        ResetCharacterPreviewRect();
        if (_characterPreviewTitleText != null)
        {
            _characterPreviewTitleText.text = kind == CardPreviewKind.Coordinate
                ? L("服装卡预览", "衣装カードのプレビュー", "Outfit card preview")
                : L("角色卡预览", "キャラカードのプレビュー", "Character card preview");
        }
        _characterPreviewNameText.text = string.IsNullOrEmpty(path)
            ? (kind == CardPreviewKind.Coordinate
                ? L("无效服装卡", "無効な衣装カード", "Invalid outfit card")
                : L("无效角色卡", "無効なキャラカード", "Invalid character card"))
            : Path.GetFileNameWithoutExtension(path);
        _characterPreviewMessageText.text = L("正在读取卡面预览…", "カード画像を読込中…", "Loading card preview…");
        _characterPreviewMessageText.gameObject.SetActive(true);
        SetCharacterPreviewActionsVisible(false);
        ShowPage(WristMenuPage.CharacterPreview);
        SetStatus(
            L("正在读取卡面预览…", "カード画像を読込中…", "Loading card preview…"),
            new Color(0.47f, 0.9f, 0.55f, 1f),
            0f);
        StartCoroutine(LoadCharacterCardPreviewAfterFrame(path, requestId));
    }

    private IEnumerator LoadCharacterCardPreviewAfterFrame(string path, int requestId)
    {
        yield return null;

        Texture2D texture = null;
        try
        {
            if (requestId != _characterPreviewRequestId)
                yield break;

            string characterName;
            string previewStatus;
            bool loaded = _cardPreviewKind == CardPreviewKind.Coordinate
                ? VRCoordinateCardService.TryLoadNativePreview(
                    path,
                    out texture,
                    out characterName,
                    out previewStatus)
                : VRCharacterCardService.TryLoadNativePreview(
                    _characterCardMode,
                    path,
                    out texture,
                    out characterName,
                    out previewStatus);
            if (!loaded)
            {
                throw new IOException(
                    previewStatus
                    ?? (_cardPreviewKind == CardPreviewKind.Coordinate
                        ? "无法读取服装卡预览图"
                        : "无法读取角色卡预览图"));
            }

            if (requestId != _characterPreviewRequestId)
            {
                Destroy(texture);
                texture = null;
                yield break;
            }

            ReleaseCharacterPreviewTexture();
            _characterPreviewTexture = texture;
            texture = null;
            _characterPreviewTexture.name = _cardPreviewKind == CardPreviewKind.Coordinate
                ? "KKVR_CoordinateCardPreview"
                : "KKVR_CharacterCardPreview";
            _characterPreviewTexture.filterMode = FilterMode.Bilinear;
            _characterPreviewTexture.wrapMode = TextureWrapMode.Clamp;
            _characterPreviewImage.texture = _characterPreviewTexture;
            _characterPreviewImage.enabled = true;
            FitCharacterPreview(
                _characterPreviewTexture.width,
                _characterPreviewTexture.height);

            _pendingCharacterCardPath = path;
            _characterPreviewNameText.text = characterName;
            _characterPreviewMessageText.gameObject.SetActive(false);
            if (_cardPreviewKind == CardPreviewKind.Coordinate)
            {
                SetCoordinatePreviewActionsVisible(true);
            }
            else
            {
                _characterPreviewLoadButton.SetLabel(GetCharacterPreviewLoadLabel());
                _characterPreviewReplaceButton.SetLabel(
                    L("替换场景角色", "シーンのキャラを置換", "Replace a scene character"));
            }
            SetCharacterPreviewActionsVisible(true);
            SetStatus(
                _cardPreviewKind == CardPreviewKind.Coordinate
                    ? L(
                        "服装卡预览已就绪，默认使用“只换衣服”最安全",
                        "衣装カードの準備完了。「服だけ変更」が最も安全です",
                        "Outfit preview is ready; Clothes only is the safest option")
                    : L("角色卡预览已就绪", "プレビューの準備完了", "Character preview is ready"),
                new Color(0.35f, 1f, 0.62f, 1f),
                0f);
        }
        catch (Exception ex)
        {
            if (texture != null)
                Destroy(texture);
            if (requestId != _characterPreviewRequestId)
                yield break;

            _characterPreviewMessageText.text = L(
                "卡面读取失败\n请返回并选择其他卡片",
                "カード画像の読込に失敗\n戻って別のカードを選択してください",
                "Could not load the card preview\nGo back and choose another card");
            _characterPreviewMessageText.gameObject.SetActive(true);
            VRLog.Warn(
                (_cardPreviewKind == CardPreviewKind.Coordinate
                    ? "Outfit card preview failed for "
                    : "Character card preview failed for ")
                + path
                + ": "
                + ex.Message);
            SetStatus(
                "卡面预览失败：" + ex.Message,
                new Color(1f, 0.38f, 0.34f, 1f),
                8f);
        }
        finally
        {
            if (requestId == _characterPreviewRequestId)
                _characterPreviewLoading = false;
        }
    }

    private void ResetCharacterPreviewRect()
    {
        if (_characterPreviewRect == null)
            return;

        _characterPreviewRect.anchoredPosition = new Vector2(
            CharacterPreviewAreaX,
            -CharacterPreviewAreaY);
        _characterPreviewRect.sizeDelta = new Vector2(
            CharacterPreviewAreaWidth,
            CharacterPreviewAreaHeight);
    }

    private void SetCharacterPreviewActionsVisible(bool visible)
    {
        bool showCharacterActions = visible && _cardPreviewKind == CardPreviewKind.Character;
        _characterPreviewLoadButton?.SetVisible(showCharacterActions);
        _characterPreviewReplaceButton?.SetVisible(showCharacterActions);
        SetCoordinatePreviewActionsVisible(visible && _cardPreviewKind == CardPreviewKind.Coordinate);
    }

    private void SetCoordinatePreviewActionsVisible(bool visible)
    {
        _coordinateClothesOnlyButton?.SetVisible(visible);
        _coordinateCustomButton?.SetVisible(visible);
        _coordinateFullButton?.SetVisible(visible);
        _coordinateUndoButton?.SetVisible(visible);
    }

    private void FitCharacterPreview(int textureWidth, int textureHeight)
    {
        if (_characterPreviewRect == null || textureWidth <= 0 || textureHeight <= 0)
            return;

        float scale = Mathf.Min(
            CharacterPreviewAreaWidth / textureWidth,
            CharacterPreviewAreaHeight / textureHeight);
        float width = textureWidth * scale;
        float height = textureHeight * scale;
        _characterPreviewRect.anchoredPosition = new Vector2(
            CharacterPreviewAreaX + (CharacterPreviewAreaWidth - width) * 0.5f,
            -(CharacterPreviewAreaY + (CharacterPreviewAreaHeight - height) * 0.5f));
        _characterPreviewRect.sizeDelta = new Vector2(width, height);
    }

    private string GetCharacterPreviewLoadLabel()
    {
        return _characterCardMode == VRCharacterCardMode.AddMale
            ? L("添加为新男角色", "新しい男性キャラとして追加", "Add as a new male character")
            : L("添加为新女角色", "新しい女性キャラとして追加", "Add as a new female character");
    }

    private void HandleCharacterPreviewLoad()
    {
        if (!HasPendingCharacterCard())
            return;

        if (_cardPreviewKind == CardPreviewKind.Coordinate)
        {
            OpenCoordinateTargetBrowser(
                VRCoordinateReplaceMode.ClothesOnly,
                new int[0]);
            return;
        }

        StartCoroutine(ExecuteCharacterAddAfterFrame(_pendingCharacterCardPath));
    }

    private void HandleCharacterPreviewReplace()
    {
        if (!HasPendingCharacterCard())
            return;

        if (_cardPreviewKind == CardPreviewKind.Coordinate)
        {
            OpenCoordinateTargetBrowser(
                VRCoordinateReplaceMode.Full,
                new int[0]);
            return;
        }

        string targetStatus;
        List<VRVmdActorTarget> targets = VRVmdTargetService.GetAllTargets(out targetStatus);
        int expectedSex = _characterCardMode == VRCharacterCardMode.AddMale ? 0 : 1;
        targets.RemoveAll(target => target.Character == null || target.Character.sex != expectedSex);
        if (targets.Count == 0)
        {
            SetStatus(
                expectedSex == 0
                    ? "当前场景没有可替换的男角色"
                    : "当前场景没有可替换的女角色",
                new Color(1f, 0.38f, 0.34f, 1f),
                7f);
            return;
        }

        OpenActorTargetBrowser(
            BrowserMode.SelectCharacterReplaceActor,
            targets,
            "请选择要替换的场景角色",
            out targetStatus);
    }

    private void HandleCoordinateClothesOnly()
    {
        if (!HasPendingCharacterCard())
            return;
        OpenCoordinateTargetBrowser(VRCoordinateReplaceMode.ClothesOnly, new int[0]);
    }

    private void HandleCoordinateCustomAccessories()
    {
        if (!HasPendingCharacterCard())
            return;
        OpenCoordinateAccessorySelector();
    }

    private void HandleCoordinateFullReplace()
    {
        if (!HasPendingCharacterCard())
            return;
        OpenCoordinateTargetBrowser(VRCoordinateReplaceMode.Full, new int[0]);
    }

    private void HandleCoordinateUndo()
    {
        if (_operationInProgress)
            return;
        if (_lastCoordinateUndoObjectKey < 0 || _lastCoordinateUndoCharacter == null)
        {
            SetStatus(
                L("还没有可撤销的换装", "元に戻せる着替えはありません", "There is no outfit change to undo"),
                new Color(1f, 0.72f, 0.25f, 1f),
                6f);
            return;
        }

        StartCoroutine(ExecuteCoordinateReplacementAfterFrame(
            null,
            _lastCoordinateUndoObjectKey,
            _lastCoordinateUndoCharacter,
            VRCoordinateReplaceMode.ClothesOnly,
            new int[0],
            true));
    }

    private void OpenCoordinateTargetBrowser(
        VRCoordinateReplaceMode mode,
        int[] selectedAccessorySlots)
    {
        string targetStatus;
        List<VRVmdActorTarget> targets = VRVmdTargetService.GetAllTargets(out targetStatus);
        if (targets.Count == 0)
        {
            SetStatus(
                targetStatus ?? "当前场景没有可替换服装的角色",
                new Color(1f, 0.38f, 0.34f, 1f),
                7f);
            return;
        }

        _pendingCoordinateReplaceMode = mode;
        _pendingCoordinateAccessorySlots = selectedAccessorySlots ?? new int[0];
        OpenActorTargetBrowser(
            BrowserMode.SelectCoordinateReplaceActor,
            targets,
            mode == VRCoordinateReplaceMode.ClothesOnly
                ? L(
                    "选择角色：只换衣服，现有饰品保持不变",
                    "キャラを選択：服だけ変更し、現在のアクセは維持します",
                    "Select a character: change clothes only and keep every current accessory")
                : mode == VRCoordinateReplaceMode.SelectedAccessories
                    ? L(
                        "选择角色：已选饰品只追加到空槽，不覆盖现有饰品",
                        "キャラを選択：選択アクセは空き枠へ追加し、現在のアクセは上書きしません",
                        "Select a character: picked accessories are added to empty slots without overwriting existing ones")
                    : L(
                    "选择角色：将覆盖全部饰品（含发饰）、妆容及坐标扩展数据",
                    "キャラを選択：全アクセ（髪アクセ含む）、メイク、コーディネート拡張データを上書きします",
                    "Select a character: all accessories (including hair), makeup, and coordinate extension data will be overwritten"),
            out targetStatus);
    }

    private void HandleCharacterReplaceActorSelection(VRWristFileEntry entry)
    {
        if (!entry.IsActorTarget || !HasPendingCharacterCard())
            return;

        VRVmdActorTarget target;
        string status;
        if (!VRVmdTargetService.TryGetTarget(entry.ObjectKey, out target, out status))
        {
            SetStatus(status, new Color(1f, 0.38f, 0.34f, 1f), 7f);
            return;
        }

        int expectedSex = _characterCardMode == VRCharacterCardMode.AddMale ? 0 : 1;
        if (target.Character.sex != expectedSex)
        {
            SetStatus(
                "角色卡与目标角色性别不一致",
                new Color(1f, 0.38f, 0.34f, 1f),
                7f);
            return;
        }

        StartCoroutine(ExecuteCharacterReplacementAfterFrame(
            _pendingCharacterCardPath,
            entry.ObjectKey));
    }

    private void HandleCoordinateReplaceActorSelection(VRWristFileEntry entry)
    {
        if (!entry.IsActorTarget || !HasPendingCharacterCard())
            return;

        VRVmdActorTarget target;
        string status;
        if (!VRVmdTargetService.TryGetTarget(entry.ObjectKey, out target, out status))
        {
            SetStatus(status, new Color(1f, 0.38f, 0.34f, 1f), 7f);
            return;
        }

        StartCoroutine(ExecuteCoordinateReplacementAfterFrame(
            _pendingCharacterCardPath,
            entry.ObjectKey,
            target.Character,
            _pendingCoordinateReplaceMode,
            _pendingCoordinateAccessorySlots,
            false));
    }

    private void HandleCharacterPreviewBack()
    {
        if (_operationInProgress)
        {
            SetStatus(
                L("换装仍在进行，请稍候", "着替え中です。しばらくお待ちください", "The outfit change is still running"),
                new Color(1f, 0.72f, 0.25f, 1f),
                4f);
            return;
        }

        ++_characterPreviewRequestId;
        _characterPreviewLoading = false;

        if (_characterCardListMode != BrowserMode.AddFemale
            && _characterCardListMode != BrowserMode.AddMale
            && _characterCardListMode != BrowserMode.CoordinateCards)
        {
            ShowPage(
                _cardPreviewKind == CardPreviewKind.Coordinate
                    ? WristMenuPage.Clothing
                    : WristMenuPage.CharacterCards);
            return;
        }

        _browserMode = _characterCardListMode;
        _browserRoot = _characterCardListRoot;
        _browserDirectory = _characterCardListDirectory;
        _browserFixedEntries = null;
        _browserOffset = _characterCardListOffset;
        _browserStickDirection = 0;
        RefreshBrowserEntries();
        ShowPage(WristMenuPage.Browser);
        SetStatus(
            _cardPreviewKind == CardPreviewKind.Coordinate
                ? L("服装卡列表", "衣装カード一覧", "Outfit card list")
                : L("角色卡列表", "キャラカード一覧", "Character card list"),
            new Color(0.47f, 0.9f, 0.55f, 1f),
            0f);
    }

    private bool HasPendingCharacterCard()
    {
        if (_operationInProgress)
            return false;
        if (_characterPreviewLoading)
        {
            SetStatus(
                _cardPreviewKind == CardPreviewKind.Coordinate
                    ? L(
                        "服装卡面仍在读取，请稍候",
                        "衣装カード画像を読込中です。お待ちください",
                        "The outfit-card preview is still loading")
                    : L(
                        "角色卡面仍在读取，请稍候",
                        "キャラカード画像を読込中です。お待ちください",
                        "The character-card preview is still loading"),
                new Color(1f, 0.72f, 0.25f, 1f),
                4f);
            return false;
        }
        if (!string.IsNullOrEmpty(_pendingCharacterCardPath))
            return true;

        SetStatus(
            _cardPreviewKind == CardPreviewKind.Coordinate
                ? L("尚未选择服装卡", "衣装カードが未選択です", "No outfit card is selected")
                : L("尚未选择角色卡", "キャラカードが未選択です", "No character card is selected"),
            new Color(1f, 0.38f, 0.34f, 1f),
            5f);
        return false;
    }

    private void ReleaseCharacterPreviewTexture()
    {
        if (_characterPreviewImage != null)
        {
            _characterPreviewImage.texture = null;
            _characterPreviewImage.enabled = false;
        }
        if (_characterPreviewTexture != null)
        {
            Destroy(_characterPreviewTexture);
            _characterPreviewTexture = null;
        }
    }
}

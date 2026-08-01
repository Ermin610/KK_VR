using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using VRGIN.Core;

namespace KKCharaStudioVR;

public sealed partial class VRWristMenuController
{
    private const int BrowserVisibleRows = 6;
    private const float BrowserStickThreshold = 0.55f;
    private const float BrowserStickReleaseThreshold = 0.3f;

    private enum BrowserMode
    {
        None,
        LoadVmd,
        SelectVmdRoot,
        SelectVmdActor,
        SelectVmdCamera,
        SelectClothingActor,
        SelectHighHeelsActor,
        SelectCharacterReplaceActor,
        SelectCoordinateReplaceActor,
        SelectCoordinateAccessories,
        CoordinateCards,
        AddFemale,
        AddMale
    }

    private readonly VRWristMenuButtonTarget[] _browserEntryButtons =
        new VRWristMenuButtonTarget[BrowserVisibleRows];
    private List<VRWristFileEntry> _browserEntries = new List<VRWristFileEntry>();
    private List<VRWristFileEntry> _browserFixedEntries;
    private Text _browserTitleText;
    private Text _browserPathText;
    private Text _browserScrollText;
    private VRWristMenuButtonTarget _browserHeaderActionButton;
    private BrowserMode _browserMode;
    private VRCharacterCardMode _characterCardMode;
    private string _browserRoot;
    private string _browserDirectory;
    private string _pendingMotionPath;
    private string _pendingAudioPath;
    private string _vmdMotionDirectory;
    private int[] _pendingVmdActorKeys = new int[0];
    private bool _pendingMotionHasCamera;
    private int _browserOffset;
    private int _browserStickDirection;
    private float _nextBrowserStickScroll;
    private bool _operationInProgress;
    private bool _returnToVmdBrowserAfterRootPicker;
    private bool _returnToMmdSettingsAfterRootPicker;
    private string _vmdRootPickerPreviousRoot;
    private string _vmdRootPickerPreviousDirectory;
    private int _vmdRootPickerPreviousOffset;
    private VRWristMenuButtonTarget _browserGenderSwitchButton;
    private readonly HashSet<int> _selectedCoordinateAccessorySlots = new HashSet<int>();
    private List<VRWristFileEntry> _coordinateAccessoryEntries =
        new List<VRWristFileEntry>();
    private int _coordinateAccessoryOffset;

    private void BuildBrowserPage()
    {
        CreateButton(
            "BrowserBack",
            "<",
            18f,
            10f,
            new Color(1f, 1f, 1f, 0.08f),
            new Color(1f, 1f, 1f, 0.2f),
            HandleBrowserBack,
            _browserPage.transform,
            58f,
            38f,
            28);
        _browserTitleText = CreateText(
            "BrowserTitle",
            _browserPage.transform,
            L("文件", "ファイル", "Files"),
            92f,
            10f,
            258f,
            40f,
            27,
            TextAnchor.MiddleLeft,
            Color.white);
        _browserHeaderActionButton = CreateButton(
            "BrowserHeaderAction",
            L("更换目录", "フォルダー変更", "Change folder"),
            362f,
            10f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            HandleBrowserHeaderAction,
            _browserPage.transform,
            174f,
            38f,
            16);
        _browserHeaderActionButton.SetVisible(false);
        _browserGenderSwitchButton = CreateButton(
            "BrowserGenderSwitch",
            L("男卡", "男性", "Male"),
            466f,
            10f,
            new Color(0.12f, 0.16f, 0.2f, 0.42f),
            new Color(0.22f, 0.34f, 0.42f, 0.72f),
            HandleBrowserGenderSwitch,
            _browserPage.transform,
            70f,
            38f,
            14);
        _browserGenderSwitchButton.SetVisible(false);

        Image pathBackground = CreateImage(
            "BrowserPathBackground",
            _browserPage.transform,
            24f,
            58f,
            512f,
            36f,
            new Color(0.7f, 0.84f, 0.94f, 0.065f));
        ApplyGlassEffects(pathBackground, new Color(0.86f, 0.96f, 1f, 0.12f), false, 0f);
        _browserPathText = CreateText(
            "BrowserPath",
            _browserPage.transform,
            string.Empty,
            36f,
            60f,
            488f,
            32f,
            16,
            TextAnchor.MiddleLeft,
            new Color(0.76f, 0.88f, 0.94f, 1f));

        for (int i = 0; i < _browserEntryButtons.Length; i++)
        {
            int row = i;
            _browserEntryButtons[i] = CreateButton(
                "BrowserEntry" + i,
                "-",
                24f,
                100f + i * 48f,
                new Color(0.08f, 0.15f, 0.18f, 0.5f),
                new Color(0.12f, 0.36f, 0.43f, 0.78f),
                () => HandleBrowserEntry(row),
                _browserPage.transform,
                512f,
                42f,
                17);
        }

        _browserScrollText = CreateText(
            "BrowserScroll",
            _browserPage.transform,
            "↑                           ↓",
            24f,
            383f,
            512f,
            25f,
            15,
            TextAnchor.MiddleCenter,
            new Color(0.58f, 0.7f, 0.76f, 1f));
    }

    private void BuildSceneSavePage()
    {
        CreateButton(
            "SceneSaveBack",
            "<",
            18f,
            10f,
            new Color(1f, 1f, 1f, 0.08f),
            new Color(1f, 1f, 1f, 0.2f),
            HandleBackToRoot,
            _sceneSavePage.transform,
            58f,
            38f,
            28);
        CreateText(
            "SceneSaveTitle",
            _sceneSavePage.transform,
            L("保存场景卡", "シーンカードを保存", "Save scene card"),
            92f,
            10f,
            444f,
            40f,
            28,
            TextAnchor.MiddleLeft,
            Color.white);

        Image destinationBackground = CreateImage(
            "SceneSaveDestinationBackground",
            _sceneSavePage.transform,
            24f,
            74f,
            512f,
            62f,
            new Color(0.24f, 0.75f, 0.84f, 0.08f));
        ApplyGlassEffects(destinationBackground, new Color(0.6f, 0.94f, 1f, 0.14f), false, 0f);
        CreateText(
            "SceneSaveDestination",
            _sceneSavePage.transform,
            L("保存位置", "保存先", "Save location") + "\nUserData\\studio\\scene",
            38f,
            78f,
            484f,
            54f,
            18,
            TextAnchor.MiddleLeft,
            new Color(0.68f, 0.92f, 0.96f, 1f));

        CreateButton(
            "SceneSaveNow",
            L("保存为新场景卡", "新しいシーンカードとして保存", "Save as a new scene card"),
            24f,
            164f,
            new Color(0.07f, 0.21f, 0.25f, 0.5f),
            new Color(0.09f, 0.42f, 0.49f, 0.78f),
            HandleSaveSceneNow,
            _sceneSavePage.transform,
            512f,
            86f,
            22);
    }

    private void BuildCharacterCardsPage()
    {
        CreateButton(
            "CharacterCardsBack",
            "<",
            18f,
            10f,
            new Color(1f, 1f, 1f, 0.08f),
            new Color(1f, 1f, 1f, 0.2f),
            HandleBackToRoot,
            _characterCardsPage.transform,
            58f,
            38f,
            28);
        CreateText(
            "CharacterCardsTitle",
            _characterCardsPage.transform,
            L("角色卡", "キャラカード", "Character cards"),
            92f,
            10f,
            444f,
            40f,
            28,
            TextAnchor.MiddleLeft,
            Color.white);

        CreateText(
            "CharacterCardLibrarySection",
            _characterCardsPage.transform,
            L("角色卡分类", "キャラカード分類", "Character card categories"),
            24f,
            76f,
            512f,
            28f,
            18,
            TextAnchor.MiddleLeft,
            new Color(0.47f, 0.9f, 0.55f, 1f));
        CreateButton(
            "CharacterAddFemale",
            L("女性角色卡  >\n浏览并预览", "女性キャラカード  >\n閲覧・プレビュー", "Female character cards  >\nBrowse and preview"),
            24f,
            116f,
            new Color(0.075f, 0.24f, 0.14f, 0.48f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            () => OpenCharacterBrowser(VRCharacterCardMode.AddFemale),
            _characterCardsPage.transform,
            512f,
            96f,
            22);
        CreateButton(
            "CharacterAddMale",
            L("男性角色卡  >\n浏览并预览", "男性キャラカード  >\n閲覧・プレビュー", "Male character cards  >\nBrowse and preview"),
            24f,
            232f,
            new Color(0.075f, 0.24f, 0.14f, 0.48f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            () => OpenCharacterBrowser(VRCharacterCardMode.AddMale),
            _characterCardsPage.transform,
            512f,
            96f,
            22);
    }

    private void HandleOpenCharacterCards()
    {
        OpenCharacterBrowser(VRCharacterCardMode.AddFemale);
    }

    private void OpenCharacterBrowser(VRCharacterCardMode mode)
    {
        SetStatus(
            mode == VRCharacterCardMode.AddFemale
                ? L("女性角色卡", "女性キャラカード", "Female character cards")
                : L("男性角色卡", "男性キャラカード", "Male character cards"),
            new Color(0.47f, 0.9f, 0.55f, 1f),
            0f);
        string root;
        string status;
        if (!VRCharacterCardService.TryGetBrowserRoot(mode, out root, out status))
        {
            SetStatus(status, new Color(1f, 0.38f, 0.34f, 1f), 5f);
            return;
        }

        _characterCardMode = mode;
        BrowserMode browserMode = mode == VRCharacterCardMode.AddFemale
            ? BrowserMode.AddFemale
            : BrowserMode.AddMale;
        OpenFileBrowser(browserMode, root, root);
        if (!string.IsNullOrEmpty(status))
            SetStatus(status, new Color(0.47f, 0.9f, 0.55f, 1f), 0f);
    }

    private void HandleOpenCoordinateCards()
    {
        string root;
        string status;
        if (!VRCoordinateCardService.TryGetBrowserRoot(out root, out status))
        {
            SetStatus(status, new Color(1f, 0.38f, 0.34f, 1f), 6f);
            return;
        }

        OpenFileBrowser(BrowserMode.CoordinateCards, root, root);
        SetStatus(
            L(
                "选择服装卡后可预览并安全替换",
                "衣装カードを選ぶとプレビューして安全に着替えられます",
                "Choose an outfit card to preview and apply it safely"),
            new Color(0.47f, 0.9f, 0.55f, 1f),
            0f);
    }

    private void OpenCoordinateAccessorySelector()
    {
        if (string.IsNullOrEmpty(_pendingCharacterCardPath))
        {
            SetStatus(
                L("尚未选择服装卡", "衣装カードが未選択です", "No outfit card is selected"),
                new Color(1f, 0.38f, 0.34f, 1f),
                5f);
            return;
        }

        List<VRCoordinateAccessorySlotInfo> slots;
        string status;
        if (!VRCoordinateCardService.TryGetCoordinateAccessorySlots(
            _pendingCharacterCardPath,
            out slots,
            out status))
        {
            SetStatus(status, new Color(1f, 0.38f, 0.34f, 1f), 8f);
            return;
        }
        if (slots.Count == 0)
        {
            SetStatus(
                L("这张服装卡没有可选饰品", "この衣装カードには選択できるアクセがありません", "This outfit card has no selectable accessories"),
                new Color(1f, 0.72f, 0.25f, 1f),
                7f);
            return;
        }

        _coordinateAccessoryEntries = new List<VRWristFileEntry>(slots.Count);
        _selectedCoordinateAccessorySlots.Clear();
        foreach (VRCoordinateAccessorySlotInfo slot in slots)
        {
            string slotNumber = (slot.SlotIndex + 1).ToString("00");
            string displayName = string.IsNullOrEmpty(slot.DisplayName)
                ? L("饰品 ", "アクセ ", "Accessory ") + slotNumber
                : slot.DisplayName;
            _coordinateAccessoryEntries.Add(new VRWristFileEntry
            {
                DisplayName = L("槽位 ", "スロット ", "Slot ")
                    + slotNumber
                    + "  "
                    + displayName,
                ObjectKey = slot.SlotIndex
            });
        }

        ShowCoordinateAccessorySelector(0);
        SetStatus(
            L(
                "点击饰品进行勾选；应用后只会追加到空槽",
                "アクセを選択してください。適用時は空き枠にのみ追加します",
                "Pick accessories; they will only be added to empty slots"),
            new Color(0.47f, 0.9f, 0.55f, 1f),
            0f);
    }

    private void ShowCoordinateAccessorySelector(int offset)
    {
        _browserMode = BrowserMode.SelectCoordinateAccessories;
        _browserRoot = null;
        _browserDirectory = null;
        _browserFixedEntries = _coordinateAccessoryEntries;
        _browserEntries = _coordinateAccessoryEntries;
        _browserOffset = Mathf.Clamp(
            offset,
            0,
            Math.Max(0, _browserEntries.Count - BrowserVisibleRows));
        _browserStickDirection = 0;
        RefreshBrowserVisuals();
        ShowPage(WristMenuPage.Browser);
    }

    private void HandleCoordinateAccessoryToggle(VRWristFileEntry entry)
    {
        if (entry == null)
            return;
        if (!_selectedCoordinateAccessorySlots.Add(entry.ObjectKey))
            _selectedCoordinateAccessorySlots.Remove(entry.ObjectKey);
        RefreshBrowserVisuals();
    }

    private void HandleConfirmCoordinateAccessories()
    {
        if (_selectedCoordinateAccessorySlots.Count == 0)
        {
            SetStatus(
                L("请至少选择一个饰品", "アクセを1つ以上選択してください", "Select at least one accessory"),
                new Color(1f, 0.72f, 0.25f, 1f),
                5f);
            return;
        }

        int[] selected = new int[_selectedCoordinateAccessorySlots.Count];
        _selectedCoordinateAccessorySlots.CopyTo(selected);
        Array.Sort(selected);
        _coordinateAccessoryOffset = _browserOffset;
        OpenCoordinateTargetBrowser(VRCoordinateReplaceMode.SelectedAccessories, selected);
    }

    private void OpenFileBrowser(BrowserMode mode, string root, string directory)
    {
        try
        {
            string fullRoot = Path.GetFullPath(root);
            string fullDirectory = Path.GetFullPath(directory);
            if (!Directory.Exists(fullRoot)
                || !Directory.Exists(fullDirectory)
                || !VRWristFileCatalog.IsInsideRoot(fullRoot, fullDirectory))
            {
                SetStatus(L("目录不存在：", "フォルダーがありません：", "Folder does not exist: ") + root,
                    new Color(1f, 0.38f, 0.34f, 1f), 6f);
                return;
            }

            _browserMode = mode;
            _browserRoot = fullRoot;
            _browserDirectory = fullDirectory;
            _browserFixedEntries = null;
            _browserOffset = 0;
            _browserStickDirection = 0;
            RefreshBrowserEntries();
            ShowPage(WristMenuPage.Browser);
        }
        catch (Exception ex)
        {
            SetStatus(L("无法打开目录：", "フォルダーを開けません：", "Could not open folder: ") + ex.Message,
                new Color(1f, 0.38f, 0.34f, 1f), 6f);
        }
    }

    private void RefreshBrowserEntries()
    {
        if (_browserMode == BrowserMode.SelectVmdRoot)
        {
            _browserEntries = string.IsNullOrEmpty(_browserDirectory)
                ? VRWristFileCatalog.ListDrives()
                : VRWristFileCatalog.ListDirectories(_browserDirectory);
        }
        else if ((_browserMode == BrowserMode.SelectVmdActor
                || _browserMode == BrowserMode.SelectVmdCamera
                || _browserMode == BrowserMode.SelectClothingActor
                || _browserMode == BrowserMode.SelectHighHeelsActor
                || _browserMode == BrowserMode.SelectCharacterReplaceActor
                || _browserMode == BrowserMode.SelectCoordinateReplaceActor
                || _browserMode == BrowserMode.SelectCoordinateAccessories)
            && _browserFixedEntries != null)
        {
            _browserEntries = _browserFixedEntries;
        }
        else
        {
            string[] extensions;
            bool inspectVmd = false;
            Func<string, bool> filter = null;
            switch (_browserMode)
            {
                case BrowserMode.LoadVmd:
                    extensions = new[] { ".vmd" };
                    inspectVmd = true;
                    break;
                case BrowserMode.AddFemale:
                case BrowserMode.AddMale:
                case BrowserMode.CoordinateCards:
                    extensions = new[] { ".png" };
                    break;
                default:
                    extensions = new string[0];
                    break;
            }

            _browserEntries = VRWristFileCatalog.ListDirectory(
                _browserRoot,
                _browserDirectory,
                extensions,
                inspectVmd,
                filter);
        }

        int maximumOffset = Math.Max(0, _browserEntries.Count - BrowserVisibleRows);
        _browserOffset = Mathf.Clamp(_browserOffset, 0, maximumOffset);
        RefreshBrowserVisuals();
    }

    private void RefreshBrowserVisuals()
    {
        if (_browserTitleText == null)
            return;

        _browserTitleText.text = GetBrowserTitle();
        bool hasCurrentFolder = _browserMode == BrowserMode.SelectVmdRoot
            && !string.IsNullOrEmpty(_browserDirectory)
            && Directory.Exists(_browserDirectory);
        bool showHeaderAction = _browserMode == BrowserMode.LoadVmd
            || hasCurrentFolder
            || _browserMode == BrowserMode.SelectCoordinateAccessories;
        _browserHeaderActionButton.SetVisible(showHeaderAction);
        bool showGenderSwitch = _browserMode == BrowserMode.AddFemale
            || _browserMode == BrowserMode.AddMale;
        _browserGenderSwitchButton.SetVisible(showGenderSwitch);
        if (showGenderSwitch)
        {
            _browserGenderSwitchButton.SetLabel(
                _browserMode == BrowserMode.AddFemale
                    ? L("男卡", "男性", "Male")
                    : L("女卡", "女性", "Female"));
        }
        if (showHeaderAction)
        {
            if (_browserMode == BrowserMode.SelectCoordinateAccessories)
            {
                _browserHeaderActionButton.SetLabel(
                    L("下一步 ", "次へ ", "Next ") + _selectedCoordinateAccessorySlots.Count);
            }
            else
            {
                _browserHeaderActionButton.SetLabel(
                    _browserMode == BrowserMode.LoadVmd
                        ? L("更换目录", "フォルダー変更", "Change folder")
                        : L("使用此目录", "このフォルダーを使用", "Use this folder"));
            }
        }
        int firstVisible = _browserEntries.Count == 0 ? 0 : _browserOffset + 1;
        int lastVisible = Math.Min(_browserEntries.Count, _browserOffset + BrowserVisibleRows);
        _browserPathText.text = BuildBrowserPathLabel()
            + "    "
            + firstVisible
            + "-"
            + lastVisible
            + "/"
            + _browserEntries.Count;

        for (int row = 0; row < _browserEntryButtons.Length; row++)
        {
            int entryIndex = _browserOffset + row;
            VRWristMenuButtonTarget button = _browserEntryButtons[row];
            bool visible = entryIndex >= 0 && entryIndex < _browserEntries.Count;
            button.SetVisible(visible);
            if (visible)
                button.SetLabel(BuildEntryLabel(_browserEntries[entryIndex]));
        }

        bool canScroll = _browserEntries.Count > BrowserVisibleRows;
        _browserScrollText.text = canScroll
            ? "↑                           ↓"
            : (_browserEntries.Count == 0
                ? L("此目录没有可用文件", "利用できるファイルがありません", "No usable files in this folder")
                : string.Empty);
    }

    private string GetBrowserTitle()
    {
        switch (_browserMode)
        {
            case BrowserMode.LoadVmd:
                return L("读取 VMD", "VMDを読込", "Load VMD");
            case BrowserMode.SelectVmdRoot:
                return L("动作数据目录", "モーションデータフォルダー", "Motion data folder");
            case BrowserMode.SelectVmdActor:
                return L("选择动作目标", "モーション対象を選択", "Select motion targets");
            case BrowserMode.SelectVmdCamera:
                return L("选择镜头 VMD", "カメラVMDを選択", "Select camera VMD");
            case BrowserMode.SelectHighHeelsActor:
                return L("高跟鞋：选择角色", "ハイヒール：キャラ選択", "High heels: Select character");
            case BrowserMode.SelectClothingActor:
                return L("服装：选择角色", "服装：キャラ選択", "Clothing: Select character");
            case BrowserMode.SelectCharacterReplaceActor:
                return L("替换：选择场景角色", "置換：シーンのキャラを選択", "Replace: Select scene character");
            case BrowserMode.SelectCoordinateReplaceActor:
                return L("服装卡：选择场景角色", "衣装カード：キャラ選択", "Outfit card: Select character");
            case BrowserMode.SelectCoordinateAccessories:
                return L("自选饰品", "アクセ選択", "Pick accessories");
            case BrowserMode.CoordinateCards:
                return L("服装卡", "衣装カード", "Outfit cards");
            case BrowserMode.AddFemale:
                return L("女性角色卡", "女性キャラカード", "Female character cards");
            case BrowserMode.AddMale:
                return L("男性角色卡", "男性キャラカード", "Male character cards");
            default:
                return L("文件", "ファイル", "Files");
        }
    }

    private string BuildBrowserPathLabel()
    {
        if (_browserMode == BrowserMode.SelectVmdActor
            || _browserMode == BrowserMode.SelectClothingActor
            || _browserMode == BrowserMode.SelectHighHeelsActor
            || _browserMode == BrowserMode.SelectCharacterReplaceActor
            || _browserMode == BrowserMode.SelectCoordinateReplaceActor)
            return L("当前场景角色", "現在のシーンキャラ", "Current scene characters");
        if (_browserMode == BrowserMode.SelectCoordinateAccessories)
        {
            return L("仅追加到空槽  ·  已选 ", "空き枠にのみ追加  ·  選択 ", "Empty slots only  ·  Selected ")
                + _selectedCoordinateAccessorySlots.Count;
        }
        if (_browserMode == BrowserMode.SelectVmdCamera)
            return L("同目录镜头", "同じフォルダーのカメラ", "Cameras in this folder");
        if (_browserMode == BrowserMode.SelectVmdRoot)
            return string.IsNullOrEmpty(_browserDirectory)
                ? L("选择磁盘", "ドライブを選択", "Select a drive")
                : _browserDirectory;

        string relative = _browserDirectory.Substring(_browserRoot.Length)
            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string rootName = Path.GetFileName(
            _browserRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return string.IsNullOrEmpty(relative)
            ? rootName
            : rootName + Path.DirectorySeparatorChar + relative;
    }

    private string BuildEntryLabel(VRWristFileEntry entry)
    {
        if (_browserMode == BrowserMode.SelectCoordinateAccessories)
        {
            return (_selectedCoordinateAccessorySlots.Contains(entry.ObjectKey) ? "[✓]  " : "[ ]  ")
                + entry.DisplayName;
        }
        if (entry.IsSkipAction)
            return L("不载入镜头\n仅使用角色动作", "カメラを読込まない\nモーションのみ", "Do not load camera\nMotion only");
        if (entry.IsActorTarget)
            return L("[角色]  ", "[キャラ]  ", "[Character]  ") + entry.DisplayName;
        if (entry.IsDirectory)
            return L("[目录]  ", "[フォルダー]  ", "[Folder]  ") + entry.DisplayName + "  >";
        if (entry.HasVmdMetadata)
        {
            bool actor = entry.VmdMetadata.HasActorData;
            bool camera = entry.VmdMetadata.HasCameraData;
            string kind = actor && camera
                ? L("混合", "複合", "Mixed")
                : camera
                    ? L("镜头", "カメラ", "Camera")
                    : (entry.VmdMetadata.Content & VRVmdContent.Motion) != 0
                        ? L("动作", "モーション", "Motion")
                        : L("表情", "表情", "Face");
            return "[" + kind + "]  " + entry.DisplayName;
        }

        return _browserMode == BrowserMode.CoordinateCards
            ? L("[服装卡]  ", "[衣装カード]  ", "[Outfit card]  ") + entry.DisplayName
            : L("[角色卡]  ", "[キャラカード]  ", "[Character card]  ") + entry.DisplayName;
    }

    private void HandleBrowserBack()
    {
        if (_operationInProgress)
        {
            SetStatus(
                L("换装仍在进行，请稍候", "着替え中です。しばらくお待ちください", "The outfit change is still running"),
                new Color(1f, 0.72f, 0.25f, 1f),
                4f);
            return;
        }

        if (_browserMode == BrowserMode.SelectVmdRoot)
        {
            HandleVmdRootPickerBack();
            return;
        }

        if (_browserMode == BrowserMode.SelectClothingActor)
        {
            Studio.OCIChar character;
            string status;
            if (TryGetClothingTarget(out character, out status))
                ShowPage(WristMenuPage.Clothing);
            else
                HandleBackToRoot();
            return;
        }

        if (_browserMode == BrowserMode.SelectHighHeelsActor)
        {
            int objectKey;
            string status;
            if (TryGetHighHeelsTarget(out objectKey, out status))
                ShowPage(WristMenuPage.HighHeels);
            else
                ShowPage(WristMenuPage.MmdSettings);
            return;
        }

        if (_browserMode == BrowserMode.SelectCharacterReplaceActor)
        {
            ShowPage(WristMenuPage.CharacterPreview);
            SetStatus(L("角色卡预览", "キャラカードのプレビュー", "Character card preview"),
                new Color(0.47f, 0.9f, 0.55f, 1f), 0f);
            return;
        }

        if (_browserMode == BrowserMode.SelectCoordinateReplaceActor)
        {
            if (_pendingCoordinateReplaceMode == VRCoordinateReplaceMode.SelectedAccessories
                && _coordinateAccessoryEntries.Count > 0)
            {
                ShowCoordinateAccessorySelector(_coordinateAccessoryOffset);
                SetStatus(
                    L("自选饰品", "アクセ選択", "Pick accessories"),
                    new Color(0.47f, 0.9f, 0.55f, 1f),
                    0f);
            }
            else
            {
                ShowPage(WristMenuPage.CharacterPreview);
                SetStatus(L("服装卡预览", "衣装カードのプレビュー", "Outfit card preview"),
                    new Color(0.47f, 0.9f, 0.55f, 1f), 0f);
            }
            return;
        }

        if (_browserMode == BrowserMode.SelectCoordinateAccessories)
        {
            ShowPage(WristMenuPage.CharacterPreview);
            SetStatus(
                L("未应用任何饰品", "アクセは適用されていません", "No accessories were applied"),
                new Color(0.47f, 0.9f, 0.55f, 1f),
                4f);
            return;
        }

        if (_browserMode == BrowserMode.SelectVmdActor
            || _browserMode == BrowserMode.SelectVmdCamera)
        {
            string root = GetConfiguredVmdRoot();
            if (string.IsNullOrEmpty(root))
            {
                OpenVmdRootPicker(false);
                return;
            }
            OpenFileBrowser(
                BrowserMode.LoadVmd,
                root,
                string.IsNullOrEmpty(_vmdMotionDirectory)
                    ? root
                    : _vmdMotionDirectory);
            return;
        }

        if (!string.Equals(_browserDirectory, _browserRoot, StringComparison.OrdinalIgnoreCase))
        {
            string parent = Path.GetDirectoryName(_browserDirectory);
            if (!string.IsNullOrEmpty(parent) && VRWristFileCatalog.IsInsideRoot(_browserRoot, parent))
            {
                _browserDirectory = parent;
                _browserOffset = 0;
                RefreshBrowserEntries();
                return;
            }
        }

        if (_browserMode == BrowserMode.AddMale)
        {
            OpenCharacterBrowser(VRCharacterCardMode.AddFemale);
        }
        else if (_browserMode == BrowserMode.AddFemale)
        {
            HandleBackToRoot();
        }
        else if (_browserMode == BrowserMode.CoordinateCards)
        {
            ShowPage(WristMenuPage.Clothing);
            SetStatus(L("服装卡", "衣装カード", "Outfit cards"),
                new Color(0.47f, 0.9f, 0.55f, 1f), 0f);
        }
        else
        {
            HandleBackToRoot();
        }
    }

    private void HandleBrowserEntry(int row)
    {
        if (_operationInProgress)
            return;

        int entryIndex = _browserOffset + row;
        if (entryIndex < 0 || entryIndex >= _browserEntries.Count)
            return;

        VRWristFileEntry entry = _browserEntries[entryIndex];
        if (entry.IsDirectory)
        {
            _browserDirectory = entry.FullPath;
            _browserOffset = 0;
            RefreshBrowserEntries();
            return;
        }

        switch (_browserMode)
        {
            case BrowserMode.LoadVmd:
                HandleVmdSelection(entry);
                break;
            case BrowserMode.SelectVmdActor:
                HandleVmdActorSelection(entry);
                break;
            case BrowserMode.SelectVmdCamera:
                StartVmdLoad(
                    _pendingMotionPath,
                    entry.IsSkipAction ? null : entry.FullPath,
                    _pendingAudioPath,
                    _pendingVmdActorKeys);
                break;
            case BrowserMode.SelectClothingActor:
                HandleClothingActorSelection(entry);
                break;
            case BrowserMode.SelectHighHeelsActor:
                HandleHighHeelsActorSelection(entry);
                break;
            case BrowserMode.SelectCharacterReplaceActor:
                HandleCharacterReplaceActorSelection(entry);
                break;
            case BrowserMode.SelectCoordinateReplaceActor:
                HandleCoordinateReplaceActorSelection(entry);
                break;
            case BrowserMode.SelectCoordinateAccessories:
                HandleCoordinateAccessoryToggle(entry);
                break;
            case BrowserMode.CoordinateCards:
                OpenCoordinateCardPreview(entry.FullPath);
                break;
            case BrowserMode.AddFemale:
            case BrowserMode.AddMale:
                OpenCharacterCardPreview(entry.FullPath);
                break;
        }
    }

    private void HandleBrowserHeaderAction()
    {
        if (_browserMode == BrowserMode.SelectCoordinateAccessories)
        {
            HandleConfirmCoordinateAccessories();
            return;
        }

        if (_browserMode == BrowserMode.LoadVmd)
        {
            OpenVmdRootPicker(true);
            return;
        }

        if (_browserMode == BrowserMode.SelectVmdRoot)
            HandleConfirmVmdRoot();
    }

    private string GetConfiguredVmdRoot()
    {
        ResolveSettings();
        if (_settings == null || string.IsNullOrEmpty(_settings.VmdRootPath))
            return null;

        try
        {
            string root = Path.GetFullPath(_settings.VmdRootPath);
            return Directory.Exists(root) ? root : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void HandleBrowserGenderSwitch()
    {
        if (_browserMode == BrowserMode.AddFemale)
            OpenCharacterBrowser(VRCharacterCardMode.AddMale);
        else if (_browserMode == BrowserMode.AddMale)
            OpenCharacterBrowser(VRCharacterCardMode.AddFemale);
    }

    private void OpenVmdRootPicker(bool returnToVmdBrowser, bool returnToMmdSettings = false)
    {
        _returnToMmdSettingsAfterRootPicker = returnToMmdSettings;
        _returnToVmdBrowserAfterRootPicker = returnToVmdBrowser
            && !returnToMmdSettings
            && _browserMode == BrowserMode.LoadVmd
            && !string.IsNullOrEmpty(_browserRoot)
            && Directory.Exists(_browserRoot);
        _vmdRootPickerPreviousRoot = _returnToVmdBrowserAfterRootPicker
            ? _browserRoot
            : null;
        _vmdRootPickerPreviousDirectory = _returnToVmdBrowserAfterRootPicker
            ? _browserDirectory
            : null;
        _vmdRootPickerPreviousOffset = _returnToVmdBrowserAfterRootPicker
            ? _browserOffset
            : 0;

        string startDirectory = GetConfiguredVmdRoot();
        if (string.IsNullOrEmpty(startDirectory)
            && Directory.Exists(VRMmddService.SuggestedVmdRoot))
        {
            startDirectory = Path.GetFullPath(VRMmddService.SuggestedVmdRoot);
        }
        if (string.IsNullOrEmpty(startDirectory))
        {
            try
            {
                string gameRoot = Path.GetPathRoot(AppDomain.CurrentDomain.BaseDirectory);
                if (!string.IsNullOrEmpty(gameRoot) && Directory.Exists(gameRoot))
                    startDirectory = gameRoot;
            }
            catch (Exception)
            {
                startDirectory = null;
            }
        }

        _browserMode = BrowserMode.SelectVmdRoot;
        _browserRoot = string.Empty;
        _browserDirectory = startDirectory;
        _browserFixedEntries = null;
        _browserOffset = 0;
        _browserStickDirection = 0;
        RefreshBrowserEntries();
        ShowPage(WristMenuPage.Browser);
        SetStatus(
            "请选择动作、镜头和音频所在的根目录",
            new Color(1f, 0.72f, 0.25f, 1f),
            0f);
    }

    private void HandleConfirmVmdRoot()
    {
        if (string.IsNullOrEmpty(_browserDirectory) || !Directory.Exists(_browserDirectory))
        {
            SetStatus(L("请先进入一个可用目录", "利用可能なフォルダーを開いてください", "Open a usable folder first"),
                new Color(1f, 0.38f, 0.34f, 1f), 5f);
            return;
        }

        ResolveSettings();
        if (_settings == null)
        {
            SetStatus(L("VR 设置尚未初始化", "VR設定が初期化されていません", "VR settings are not initialized"),
                new Color(1f, 0.38f, 0.34f, 1f), 6f);
            return;
        }

        try
        {
            string root = Path.GetFullPath(_browserDirectory);
            _settings.VmdRootPath = root;
            _settings.Save();
            _vmdMotionDirectory = root;
            bool returnToMmdSettings = _returnToMmdSettingsAfterRootPicker;
            _returnToVmdBrowserAfterRootPicker = false;
            _returnToMmdSettingsAfterRootPicker = false;
            _vmdRootPickerPreviousRoot = null;
            _vmdRootPickerPreviousDirectory = null;
            if (returnToMmdSettings)
                ShowPage(WristMenuPage.MmdSettings);
            else
                OpenFileBrowser(BrowserMode.LoadVmd, root, root);
            SetStatus(L("动作目录已保存：", "モーションフォルダーを保存：", "Motion folder saved: ") + root,
                new Color(0.35f, 1f, 0.62f, 1f), 5f);
        }
        catch (Exception ex)
        {
            SetStatus(L("动作目录保存失败：", "モーションフォルダーの保存に失敗：", "Could not save motion folder: ") + ex.Message,
                new Color(1f, 0.38f, 0.34f, 1f), 7f);
        }
    }

    private void HandleVmdRootPickerBack()
    {
        if (!string.IsNullOrEmpty(_browserDirectory))
        {
            try
            {
                string current = Path.GetFullPath(_browserDirectory);
                string driveRoot = Path.GetPathRoot(current);
                if (!string.IsNullOrEmpty(driveRoot)
                    && string.Equals(
                        current.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                        driveRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                        StringComparison.OrdinalIgnoreCase))
                {
                    _browserDirectory = null;
                }
                else
                {
                    _browserDirectory = Path.GetDirectoryName(current);
                }
                _browserOffset = 0;
                RefreshBrowserEntries();
                return;
            }
            catch (Exception)
            {
                _browserDirectory = null;
                _browserOffset = 0;
                RefreshBrowserEntries();
                return;
            }
        }

        if (_returnToMmdSettingsAfterRootPicker)
        {
            _returnToMmdSettingsAfterRootPicker = false;
            ShowPage(WristMenuPage.MmdSettings);
            SetStatus(L("MMD 工具", "MMD ツール", "MMD tools"), new Color(1f, 0.72f, 0.25f, 1f), 0f);
            return;
        }

        if (_returnToVmdBrowserAfterRootPicker
            && !string.IsNullOrEmpty(_vmdRootPickerPreviousRoot)
            && Directory.Exists(_vmdRootPickerPreviousRoot))
        {
            string directory = Directory.Exists(_vmdRootPickerPreviousDirectory)
                && VRWristFileCatalog.IsInsideRoot(
                    _vmdRootPickerPreviousRoot,
                    _vmdRootPickerPreviousDirectory)
                    ? _vmdRootPickerPreviousDirectory
                    : _vmdRootPickerPreviousRoot;
            int previousOffset = _vmdRootPickerPreviousOffset;
            OpenFileBrowser(
                BrowserMode.LoadVmd,
                _vmdRootPickerPreviousRoot,
                directory);
            _browserOffset = previousOffset;
            RefreshBrowserEntries();
            return;
        }

        HandleBackToRoot();
    }

    private bool OpenActorTargetBrowser(
        BrowserMode mode,
        List<VRVmdActorTarget> targets,
        string prompt,
        out string status)
    {
        if (targets == null || targets.Count == 0)
        {
            status = "当前场景没有可选角色";
            return false;
        }

        _browserMode = mode;
        _browserRoot = string.Empty;
        _browserDirectory = string.Empty;
        _browserFixedEntries = new List<VRWristFileEntry>();
        foreach (VRVmdActorTarget target in targets)
        {
            _browserFixedEntries.Add(new VRWristFileEntry
            {
                DisplayName = target.DisplayName,
                IsActorTarget = true,
                ObjectKey = target.ObjectKey
            });
        }

        _browserOffset = 0;
        _browserStickDirection = 0;
        RefreshBrowserEntries();
        ShowPage(WristMenuPage.Browser);
        status = prompt;
        SetStatus(prompt, new Color(1f, 0.72f, 0.25f, 1f), 0f);
        return true;
    }

    private void HandleVmdSelection(VRWristFileEntry entry)
    {
        VRVmdMetadata metadata = entry.VmdMetadata;
        string audioPath = VRWristFileCatalog.FindBestRelatedAudio(entry.FullPath);

        if (!metadata.HasActorData)
        {
            StartVmdLoad(null, entry.FullPath, audioPath, new int[0]);
            return;
        }

        _pendingMotionPath = entry.FullPath;
        _pendingAudioPath = audioPath;
        _vmdMotionDirectory = Path.GetDirectoryName(entry.FullPath);
        _pendingMotionHasCamera = metadata.HasCameraData;
        _pendingVmdActorKeys = VRVmdTargetService.GetSelectedObjectKeys();
        if (_pendingVmdActorKeys.Length > 0)
        {
            ContinuePendingVmdSelection();
            return;
        }

        string targetStatus;
        List<VRVmdActorTarget> targets = VRVmdTargetService.GetAllTargets(out targetStatus);
        if (targets.Count == 0)
        {
            SetStatus(
                targetStatus,
                new Color(1f, 0.38f, 0.34f, 1f),
                8f);
            return;
        }
        if (targets.Count == 1)
        {
            if (VRVmdTargetService.TrySelectTarget(targets[0].ObjectKey, out targetStatus))
            {
                _pendingVmdActorKeys = new[] { targets[0].ObjectKey };
                ContinuePendingVmdSelection();
            }
            else
            {
                SetStatus(
                    targetStatus,
                    new Color(1f, 0.38f, 0.34f, 1f),
                    8f);
            }
            return;
        }

        _browserMode = BrowserMode.SelectVmdActor;
        _browserRoot = _vmdMotionDirectory;
        _browserDirectory = _vmdMotionDirectory;
        _browserFixedEntries = new List<VRWristFileEntry>();
        foreach (VRVmdActorTarget target in targets)
        {
            _browserFixedEntries.Add(new VRWristFileEntry
            {
                DisplayName = target.DisplayName,
                IsActorTarget = true,
                ObjectKey = target.ObjectKey
            });
        }
        _browserOffset = 0;
        RefreshBrowserEntries();
        ShowPage(WristMenuPage.Browser);
        SetStatus(
            "请选择接收动作的角色",
            new Color(1f, 0.72f, 0.25f, 1f),
            0f);
    }

    private void HandleVmdActorSelection(VRWristFileEntry entry)
    {
        if (!entry.IsActorTarget)
            return;

        string status;
        if (!VRVmdTargetService.TrySelectTarget(entry.ObjectKey, out status))
        {
            SetStatus(
                status,
                new Color(1f, 0.38f, 0.34f, 1f),
                8f);
            return;
        }

        _pendingVmdActorKeys = new[] { entry.ObjectKey };
        SetStatus(status, new Color(0.35f, 1f, 0.62f, 1f), 0f);
        ContinuePendingVmdSelection();
    }

    private void ContinuePendingVmdSelection()
    {
        if (_pendingMotionHasCamera)
        {
            StartVmdLoad(
                _pendingMotionPath,
                _pendingMotionPath,
                _pendingAudioPath,
                _pendingVmdActorKeys);
            return;
        }

        List<VRWristFileEntry> cameras =
            VRWristFileCatalog.FindRelatedCameraFiles(_pendingMotionPath);
        if (cameras.Count == 0)
        {
            StartVmdLoad(
                _pendingMotionPath,
                null,
                _pendingAudioPath,
                _pendingVmdActorKeys);
            return;
        }

        _browserMode = BrowserMode.SelectVmdCamera;
        _browserRoot = _vmdMotionDirectory;
        _browserDirectory = _vmdMotionDirectory;
        _browserFixedEntries = new List<VRWristFileEntry>
        {
            new VRWristFileEntry
            {
                DisplayName = "不载入镜头",
                IsSkipAction = true
            }
        };
        _browserFixedEntries.AddRange(cameras);
        _browserOffset = 0;
        RefreshBrowserEntries();
        ShowPage(WristMenuPage.Browser);

        string audioStatus = string.IsNullOrEmpty(_pendingAudioPath)
            ? "未找到音频"
            : "音频自动：" + Path.GetFileNameWithoutExtension(_pendingAudioPath);
        SetStatus(
            "动作：" + Path.GetFileNameWithoutExtension(_pendingMotionPath) + " | " + audioStatus,
            new Color(1f, 0.72f, 0.25f, 1f),
            0f);
    }

    private void StartVmdLoad(
        string motionPath,
        string cameraPath,
        string audioPath,
        int[] actorObjectKeys)
    {
        if (_operationInProgress)
            return;
        _operationInProgress = true;
        SetStatus(L("正在载入 VMD…", "VMDを読込中…", "Loading VMD…"),
            new Color(1f, 0.72f, 0.25f, 1f), 0f);
        StartCoroutine(LoadVmdAfterFrame(
            motionPath,
            cameraPath,
            audioPath,
            actorObjectKeys));
    }

    private IEnumerator LoadVmdAfterFrame(
        string motionPath,
        string cameraPath,
        string audioPath,
        int[] actorObjectKeys)
    {
        yield return null;
        int requestedTargetCount = actorObjectKeys != null ? actorObjectKeys.Length : 0;
        actorObjectKeys = VRVmdTargetService.ResolveLiveObjectKeys(actorObjectKeys);
        if (requestedTargetCount > 0 && actorObjectKeys.Length != requestedTargetCount)
        {
            VRLog.Warn(
                "VMD target selection changed after character initialization; refreshed live targets before loading.");
        }

        string status;
        bool success = VRMmddService.LoadVmdPackage(
            motionPath,
            cameraPath,
            audioPath,
            actorObjectKeys,
            GetConfiguredVmdRoot(),
            out status);
        _operationInProgress = false;
        if (success)
        {
            ShowPage(WristMenuPage.Root);
            VRLog.Info(status);
        }

        SetStatus(
            status,
            success ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.38f, 0.34f, 1f),
            success ? 7f : 8f);
    }

    private IEnumerator ExecuteCharacterAddAfterFrame(string path)
    {
        if (_operationInProgress)
            yield break;

        _operationInProgress = true;
        SetStatus(L("正在读取角色卡…", "キャラカードを読込中…", "Loading character card…"),
            new Color(0.47f, 0.9f, 0.55f, 1f), 0f);
        yield return null;

        string status;
        bool success = VRCharacterCardService.ExecuteAdd(_characterCardMode, path, out status);
        _operationInProgress = false;
        ShowPage(WristMenuPage.CharacterPreview);
        if (success)
            VRLog.Info(status);
        SetStatus(
            status,
            success ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.38f, 0.34f, 1f),
            success ? 6f : 8f);
    }

    private IEnumerator ExecuteCharacterReplacementAfterFrame(string path, int objectKey)
    {
        if (_operationInProgress)
            yield break;

        _operationInProgress = true;
        SetStatus(L("正在替换场景角色…", "シーンのキャラを置換中…", "Replacing scene character…"),
            new Color(0.47f, 0.9f, 0.55f, 1f), 0f);
        yield return null;

        string status;
        bool success = VRCharacterCardService.ExecuteReplace(objectKey, path, out status);
        _operationInProgress = false;
        ShowPage(WristMenuPage.CharacterPreview);
        if (success)
            VRLog.Info(status);
        SetStatus(
            status,
            success ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.38f, 0.34f, 1f),
            success ? 6f : 8f);
    }

    private IEnumerator ExecuteCoordinateReplacementAfterFrame(
        string path,
        int objectKey,
        Studio.OCIChar expectedCharacter,
        VRCoordinateReplaceMode mode,
        int[] selectedAccessorySlots,
        bool undo)
    {
        if (_operationInProgress)
            yield break;

        _operationInProgress = true;
        SetStatus(
            undo
                ? L(
                    "正在撤销上次换装…",
                    "直前の着替えを元に戻しています…",
                    "Undoing the last outfit change…")
                : mode == VRCoordinateReplaceMode.ClothesOnly
                    ? L(
                        "正在只替换衣服，现有饰品保持不变…",
                        "服だけ変更し、現在のアクセを維持しています…",
                        "Changing clothes while keeping current accessories…")
                    : mode == VRCoordinateReplaceMode.SelectedAccessories
                        ? L(
                            "正在换衣服，并把所选饰品追加到空槽…",
                            "服を変更し、選択アクセを空き枠へ追加しています…",
                            "Changing clothes and adding selected accessories to empty slots…")
                        : L(
                            "正在完整替换服装与饰品…",
                            "衣装とアクセを完全に置き換えています…",
                            "Replacing the full outfit and accessories…"),
            new Color(0.47f, 0.9f, 0.55f, 1f),
            0f);
        VRCoordinateLoadSession session = null;
        string status = undo ? "撤销换装没有启动" : "服装卡替换没有启动";
        bool success = false;
        try
        {
            yield return null;

            bool started;
            try
            {
                started = undo
                    ? VRCoordinateCardService.TryBeginUndoLastReplace(
                        objectKey,
                        expectedCharacter,
                        out session,
                        out status)
                    : VRCoordinateCardService.TryBeginReplace(
                        objectKey,
                        expectedCharacter,
                        path,
                        mode,
                        selectedAccessorySlots,
                        out session,
                        out status);
            }
            catch (Exception ex)
            {
                started = false;
                status = (undo ? "撤销换装失败：" : "服装卡替换失败：") + ex.Message;
            }

            if (started)
            {
                float warningDeadline = Time.realtimeSinceStartup + 30f;
                float hardDeadline = Time.realtimeSinceStartup + 90f;
                bool nativeWaitWarningShown = false;
                while (true)
                {
                    bool completed;
                    try
                    {
                        completed = session.TryGetCompletion(out success, out status);
                    }
                    catch (Exception ex)
                    {
                        if (session.CanAbortSafely)
                        {
                            try
                            {
                                session.Abort();
                            }
                            catch (Exception abortException)
                            {
                                VRLog.Warn("Outfit-load recovery also failed: " + abortException.Message);
                            }
                            if (session.RequiresBackgroundMonitoring)
                            {
                                SetStatus(
                                    L(
                                        "换装读取异常，正在恢复角色状态…",
                                        "着替えの読込エラー。キャラ状態を復元中…",
                                        "Outfit loading failed; restoring the character…"),
                                    new Color(1f, 0.72f, 0.25f, 1f),
                                    0f);
                                continue;
                            }
                            status = session.TerminalRecoveryStatus
                                ?? ("服装卡替换异常，已恢复角色状态：" + ex.Message);
                        }
                        else
                        {
                            status = L(
                                "换装操作状态异常；可能仍在后台执行，完成或重载场景前不能再次换装：",
                                "着替え操作の状態を確認できません。バックグラウンドで継続中の可能性があるため、完了またはシーン再読込まで再度着替えできません：",
                                "Could not read the outfit-operation state. It may still be running in the background; do not change outfits again until it finishes or the scene is reloaded: ")
                                + ex.Message;
                            StartCoroutine(MonitorCoordinateReplacementInBackground(
                                session,
                                objectKey,
                                expectedCharacter,
                                undo));
                        }
                        success = false;
                        break;
                    }

                    if (completed)
                    {
                        if (session.RequiresBackgroundMonitoring)
                        {
                            StartCoroutine(MonitorCoordinateReplacementInBackground(
                                session,
                                objectKey,
                                expectedCharacter,
                                undo));
                        }
                        break;
                    }

                    if (Time.realtimeSinceStartup >= warningDeadline)
                    {
                        if (session.CanAbortSafely)
                        {
                            session.Abort();
                            if (session.RequiresBackgroundMonitoring)
                            {
                                SetStatus(
                                    L(
                                        "换装超时，正在恢复角色状态…",
                                        "着替えがタイムアウトしました。キャラ状態を復元中…",
                                        "The outfit load timed out; restoring the character…"),
                                    new Color(1f, 0.72f, 0.25f, 1f),
                                    0f);
                                warningDeadline = Time.realtimeSinceStartup + 30f;
                                continue;
                            }
                            success = false;
                            status = session.TerminalRecoveryStatus
                                ?? "服装卡替换超时，已恢复角色状态";
                            break;
                        }

                        if (Time.realtimeSinceStartup >= hardDeadline)
                        {
                            success = false;
                            status = L(
                                "换装操作时间过长，已解除界面等待；完成或重载场景前不能再次换装",
                                "着替え操作に時間がかかっているため、画面の待機を解除しました。完了またはシーン再読込までは再度着替えできません",
                                "The outfit operation is taking too long, so the UI wait was released. Do not change outfits again until it finishes or the scene is reloaded");
                            StartCoroutine(MonitorCoordinateReplacementInBackground(
                                session,
                                objectKey,
                                expectedCharacter,
                                undo));
                            break;
                        }

                        if (!nativeWaitWarningShown)
                        {
                            nativeWaitWarningShown = true;
                            SetStatus(
                                L(
                                    "换装操作仍在执行，完成前不会开始下一次换装",
                                    "着替え操作を実行中です。完了まで次の着替えは開始しません",
                                    "The outfit operation is still running; another outfit change will not start yet"),
                                new Color(1f, 0.72f, 0.25f, 1f),
                                0f);
                        }
                        warningDeadline = Time.realtimeSinceStartup + 30f;
                    }
                    yield return null;
                }
            }
        }
        finally
        {
            _operationInProgress = false;
        }

        ShowPage(WristMenuPage.CharacterPreview);
        if (success)
        {
            Studio.OCIChar character;
            string ignoredStatus;
            if (VRCharacterCardService.TryGetCharacter(objectKey, out character, out ignoredStatus)
                && ReferenceEquals(character, expectedCharacter))
            {
                _clothingTargetObjectKey = objectKey;
                _clothingTargetCharacter = character;
                if (undo)
                {
                    _lastCoordinateUndoObjectKey = -1;
                    _lastCoordinateUndoCharacter = null;
                }
                else
                {
                    _lastCoordinateUndoObjectKey = objectKey;
                    _lastCoordinateUndoCharacter = character;
                }
                VRCharacterClothingService.RefreshStudioCharacterPanel(character);
                string highHeelStatus;
                if (VRMmddService.RefreshHighHeels(objectKey, out highHeelStatus))
                    VRLog.Info(highHeelStatus);
            }
            else
            {
                success = false;
                status = "场景角色已变化，换装结果未继续绑定到新角色";
            }
            if (success)
                VRLog.Info(status);
        }
        else
        {
            VRLog.Warn(status);
        }

        SetStatus(
            status,
            success ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.38f, 0.34f, 1f),
            success ? 8f : 10f);
    }

    private IEnumerator MonitorCoordinateReplacementInBackground(
        VRCoordinateLoadSession session,
        int objectKey,
        Studio.OCIChar expectedCharacter,
        bool undo)
    {
        if (session == null)
            yield break;

        int pollingFailures = 0;
        float retryAfter = 0f;
        while (VRCoordinateCardService.IsActiveSession(session))
        {
            yield return null;
            if (!VRCoordinateCardService.IsActiveSession(session))
                yield break;
            if (Time.realtimeSinceStartup < retryAfter)
                continue;

            bool completed;
            bool success;
            string status;
            try
            {
                completed = session.TryGetCompletion(out success, out status);
                pollingFailures = 0;
            }
            catch (Exception ex)
            {
                pollingFailures++;
                if (pollingFailures <= 3 || pollingFailures % 20 == 0)
                    VRLog.Warn("Background outfit-load monitor will retry: " + ex.Message);

                if (session.CanAbortSafely)
                {
                    try
                    {
                        session.Abort();
                    }
                    catch (Exception abortException)
                    {
                        VRLog.Warn("Background outfit-load recovery also failed: "
                            + abortException.Message);
                    }

                    if (!VRCoordinateCardService.IsActiveSession(session))
                    {
                        string terminalStatus = session.TerminalRecoveryStatus
                            ?? ("后台换装监控失败：" + ex.Message);
                        VRLog.Warn(terminalStatus);
                        if (!_operationInProgress)
                        {
                            SetStatus(
                                terminalStatus,
                                new Color(1f, 0.38f, 0.34f, 1f),
                                10f);
                        }
                        yield break;
                    }
                }

                retryAfter = Time.realtimeSinceStartup + Mathf.Min(2f, 0.25f * pollingFailures);
                continue;
            }

            if (!completed || session.RequiresBackgroundMonitoring)
                continue;

            if (success)
            {
                Studio.OCIChar character;
                string ignoredStatus;
                if (VRCharacterCardService.TryGetCharacter(objectKey, out character, out ignoredStatus)
                    && ReferenceEquals(character, expectedCharacter))
                {
                    _clothingTargetObjectKey = objectKey;
                    _clothingTargetCharacter = character;
                    if (undo)
                    {
                        _lastCoordinateUndoObjectKey = -1;
                        _lastCoordinateUndoCharacter = null;
                    }
                    else
                    {
                        _lastCoordinateUndoObjectKey = objectKey;
                        _lastCoordinateUndoCharacter = character;
                    }
                    VRCharacterClothingService.RefreshStudioCharacterPanel(character);
                    string highHeelStatus;
                    if (VRMmddService.RefreshHighHeels(objectKey, out highHeelStatus))
                        VRLog.Info(highHeelStatus);
                }
                else
                {
                    success = false;
                    status = "场景角色已变化，后台换装结果未继续绑定到新角色";
                }
            }

            if (success)
                VRLog.Info("Background outfit load completed: " + status);
            else
                VRLog.Warn("Background outfit load finished with an error: " + status);
            if (!_operationInProgress)
            {
                SetStatus(
                    status,
                    success ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.38f, 0.34f, 1f),
                    10f);
            }
            yield break;
        }
    }

    private void HandleSaveSceneNow()
    {
        if (_operationInProgress)
            return;

        string status;
        bool success = VRSceneActions.SaveScene(out status);
        SetStatus(
            status,
            success ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.38f, 0.34f, 1f),
            success ? 5f : 8f);
        if (success)
            VRLog.Info(status);
    }

    private void UpdateBrowserStickScroll(SteamVR_Controller.Device rightDevice)
    {
        if (_page != WristMenuPage.Browser || _browserEntries.Count <= BrowserVisibleRows)
        {
            _browserStickDirection = 0;
            return;
        }

        float axis = rightDevice.GetAxis(EVRButtonId.k_EButton_Axis0).y;
        int direction = axis > BrowserStickThreshold
            ? -1
            : axis < -BrowserStickThreshold
                ? 1
                : 0;

        if (Mathf.Abs(axis) < BrowserStickReleaseThreshold)
        {
            _browserStickDirection = 0;
            return;
        }
        if (direction == 0)
            return;

        bool shouldScroll = direction != _browserStickDirection
            || Time.unscaledTime >= _nextBrowserStickScroll;
        if (!shouldScroll)
            return;

        bool changed = ScrollBrowser(direction);
        _browserStickDirection = direction;
        _nextBrowserStickScroll = Time.unscaledTime + (changed ? 0.13f : 0.22f);
        if (changed)
            rightDevice.TriggerHapticPulse(120, EVRButtonId.k_EButton_Axis0);
    }

    private bool ScrollBrowser(int direction)
    {
        int maximumOffset = Math.Max(0, _browserEntries.Count - BrowserVisibleRows);
        int nextOffset = Mathf.Clamp(_browserOffset + direction, 0, maximumOffset);
        if (nextOffset == _browserOffset)
            return false;

        _browserOffset = nextOffset;
        RefreshBrowserVisuals();
        return true;
    }
}

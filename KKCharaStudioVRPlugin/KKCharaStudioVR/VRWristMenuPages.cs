using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private const int BrowserPathMaxUiUnits = 56;
    private const int BrowserEntryMaxUiUnits = 54;
    private const int CharacterPreviewNameMaxUiUnits = 50;

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
        SelectCharacterRemoveActor,
        SelectCoordinateReplaceActor,
        SelectCoordinateAccessories,
        SelectTrackedAccessoryActor,
        ManageTrackedAccessories,
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
    private VRWristMenuButtonTarget _browserRemoveCharacterButton;
    private readonly HashSet<int> _selectedCoordinateAccessorySlots = new HashSet<int>();
    private List<VRWristFileEntry> _coordinateAccessoryEntries =
        new List<VRWristFileEntry>();
    private int _coordinateAccessoryOffset;
    private readonly HashSet<int> _selectedTrackedAccessorySlots = new HashSet<int>();
    private List<VRWristFileEntry> _trackedAccessoryEntries =
        new List<VRWristFileEntry>();
    private int _trackedAccessoryOffset;
    private int _trackedAccessoryTargetObjectKey = -1;
    private Studio.OCIChar _trackedAccessoryTargetCharacter;
    private string _trackedAccessoryTargetName;

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
        _browserRemoveCharacterButton = CreateButton(
            "BrowserRemoveCharacter",
            L("移除人物", "人物を削除", "Remove"),
            362f,
            10f,
            new Color(0.34f, 0.07f, 0.09f, 0.58f),
            new Color(0.68f, 0.11f, 0.14f, 0.84f),
            HandleOpenCharacterRemoval,
            _browserPage.transform,
            96f,
            38f,
            14);
        _browserRemoveCharacterButton.SetVisible(false);
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
        _browserPathText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _browserPathText.verticalOverflow = VerticalWrapMode.Truncate;

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

    private void HandleOpenTrackedAccessoryManager()
    {
        string targetStatus;
        List<VRVmdActorTarget> targets = VRVmdTargetService.GetAllTargets(out targetStatus);
        targets.RemoveAll(target =>
        {
            if (target?.Character == null)
                return true;
            List<VRTrackedAccessorySlotInfo> slots;
            string ignoredStatus;
            return !VRCoordinateCardService.TryGetTrackedAccessorySlots(
                    target.Character,
                    out slots,
                    out ignoredStatus)
                || slots.Count == 0;
        });

        if (targets.Count == 0)
        {
            SetStatus(
                L(
                    "当前场景没有可管理的追加饰品",
                    "現在のシーンに管理できる追加アクセはありません",
                    "There are no appended accessories to manage in this scene"),
                new Color(1f, 0.72f, 0.25f, 1f),
                6f);
            return;
        }

        if (targets.Count == 1)
        {
            OpenTrackedAccessoryManager(
                targets[0].ObjectKey,
                targets[0].Character,
                targets[0].DisplayName,
                0);
            return;
        }

        OpenActorTargetBrowser(
            BrowserMode.SelectTrackedAccessoryActor,
            targets,
            L(
                "选择要管理追加饰品的角色",
                "追加アクセを管理するキャラを選択",
                "Select the character whose appended accessories you want to manage"),
            out targetStatus);
    }

    private void HandleTrackedAccessoryActorSelection(VRWristFileEntry entry)
    {
        if (entry == null || !entry.IsActorTarget)
            return;

        VRVmdActorTarget target;
        string status;
        if (!VRVmdTargetService.TryGetTarget(entry.ObjectKey, out target, out status))
        {
            SetStatus(status, new Color(1f, 0.38f, 0.34f, 1f), 7f);
            return;
        }

        OpenTrackedAccessoryManager(
            entry.ObjectKey,
            target.Character,
            target.DisplayName,
            0);
    }

    private void OpenTrackedAccessoryManager(
        int objectKey,
        Studio.OCIChar character,
        string displayName,
        int offset)
    {
        List<VRTrackedAccessorySlotInfo> slots;
        string status;
        if (!VRCoordinateCardService.TryGetTrackedAccessorySlots(
            character,
            out slots,
            out status)
            || slots.Count == 0)
        {
            SetStatus(
                status ?? L(
                    "这个角色没有可管理的追加饰品",
                    "このキャラに管理できる追加アクセはありません",
                    "This character has no appended accessories to manage"),
                new Color(1f, 0.72f, 0.25f, 1f),
                6f);
            return;
        }

        _trackedAccessoryTargetObjectKey = objectKey;
        _trackedAccessoryTargetCharacter = character;
        _trackedAccessoryTargetName = displayName;
        _selectedTrackedAccessorySlots.Clear();
        _trackedAccessoryEntries = new List<VRWristFileEntry>(slots.Count + 1)
        {
            new VRWristFileEntry
            {
                DisplayName = L(
                    "一键移除全部追加饰品",
                    "追加アクセをすべて削除",
                    "Remove all appended accessories"),
                ObjectKey = -1
            }
        };
        foreach (VRTrackedAccessorySlotInfo slot in slots)
        {
            string slotNumber = (slot.SlotIndex + 1).ToString("00");
            string accessoryName = string.IsNullOrEmpty(slot.DisplayName)
                ? L("追加饰品", "追加アクセ", "Appended accessory")
                : slot.DisplayName;
            _trackedAccessoryEntries.Add(new VRWristFileEntry
            {
                DisplayName = L("槽位 ", "スロット ", "Slot ")
                    + slotNumber
                    + "  "
                    + accessoryName,
                ObjectKey = slot.SlotIndex
            });
        }

        ShowTrackedAccessoryManager(offset);
        SetStatus(
            L(
                "只会移除本插件追加且指纹未变化的饰品",
                "このプラグインが追加し、内容が変わっていないアクセだけを削除します",
                "Only unchanged accessories appended by this plugin can be removed"),
            new Color(0.47f, 0.9f, 0.55f, 1f),
            0f);
    }

    private void ShowTrackedAccessoryManager(int offset)
    {
        _browserMode = BrowserMode.ManageTrackedAccessories;
        _browserRoot = null;
        _browserDirectory = null;
        _browserFixedEntries = _trackedAccessoryEntries;
        _browserEntries = _trackedAccessoryEntries;
        _browserOffset = Mathf.Clamp(
            offset,
            0,
            Math.Max(0, _browserEntries.Count - BrowserVisibleRows));
        _browserStickDirection = 0;
        RefreshBrowserVisuals();
        ShowPage(WristMenuPage.Browser);
    }

    private void HandleTrackedAccessoryEntry(VRWristFileEntry entry)
    {
        if (entry == null)
            return;
        if (entry.ObjectKey < 0)
        {
            BeginTrackedAccessoryRemoval(true);
            return;
        }

        if (!_selectedTrackedAccessorySlots.Add(entry.ObjectKey))
            _selectedTrackedAccessorySlots.Remove(entry.ObjectKey);
        RefreshBrowserVisuals();
    }

    private void BeginTrackedAccessoryRemoval(bool removeAll)
    {
        if (_operationInProgress)
            return;
        if (_trackedAccessoryTargetObjectKey < 0
            || _trackedAccessoryTargetCharacter == null)
        {
            SetStatus(
                L("饰品管理目标已失效", "アクセ管理対象が無効です", "The accessory-management target is no longer valid"),
                new Color(1f, 0.38f, 0.34f, 1f),
                6f);
            return;
        }

        int[] slots;
        if (removeAll)
        {
            List<VRTrackedAccessorySlotInfo> current;
            string status;
            if (!VRCoordinateCardService.TryGetTrackedAccessorySlots(
                _trackedAccessoryTargetCharacter,
                out current,
                out status))
            {
                SetStatus(status, new Color(1f, 0.38f, 0.34f, 1f), 7f);
                return;
            }
            slots = current.Select(slot => slot.SlotIndex).ToArray();
        }
        else
        {
            slots = _selectedTrackedAccessorySlots.OrderBy(slot => slot).ToArray();
        }

        if (slots.Length == 0)
        {
            SetStatus(
                L("请至少勾选一个饰品", "アクセを1つ以上選択してください", "Select at least one accessory"),
                new Color(1f, 0.72f, 0.25f, 1f),
                5f);
            return;
        }

        _trackedAccessoryOffset = _browserOffset;
        StartCoroutine(ExecuteCoordinateReplacementAfterFrame(
            null,
            _trackedAccessoryTargetObjectKey,
            _trackedAccessoryTargetCharacter,
            VRCoordinateReplaceMode.RemoveTrackedAccessories,
            slots,
            false));
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
                || _browserMode == BrowserMode.SelectCharacterRemoveActor
                || _browserMode == BrowserMode.SelectCoordinateReplaceActor
                || _browserMode == BrowserMode.SelectCoordinateAccessories
                || _browserMode == BrowserMode.SelectTrackedAccessoryActor
                || _browserMode == BrowserMode.ManageTrackedAccessories)
            && _browserFixedEntries != null)
        {
            _browserEntries = _browserFixedEntries;
        }
        else
        {
            string[] extensions;
            bool inspectVmd = false;
            bool filesBeforeDirectories = false;
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
                filter,
                filesBeforeDirectories);
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
            || _browserMode == BrowserMode.SelectCoordinateAccessories
            || _browserMode == BrowserMode.ManageTrackedAccessories;
        _browserHeaderActionButton.SetVisible(showHeaderAction);
        bool showGenderSwitch = _browserMode == BrowserMode.AddFemale
            || _browserMode == BrowserMode.AddMale;
        _browserGenderSwitchButton.SetVisible(showGenderSwitch);
        _browserRemoveCharacterButton.SetVisible(showGenderSwitch);
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
            else if (_browserMode == BrowserMode.ManageTrackedAccessories)
            {
                _browserHeaderActionButton.SetLabel(
                    L("移除所选 ", "選択を削除 ", "Remove ")
                    + _selectedTrackedAccessorySlots.Count);
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
        string rangeLabel = firstVisible
            + "-"
            + lastVisible
            + "/"
            + _browserEntries.Count;
        int pathBudget = Math.Max(
            10,
            BrowserPathMaxUiUnits - GetUiVisualUnits(rangeLabel) - 4);
        _browserPathText.text = EllipsizeMiddleForUi(
                BuildBrowserPathLabel(),
                pathBudget)
            + "    "
            + rangeLabel;

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
            case BrowserMode.SelectCharacterRemoveActor:
                return L("移除：选择场景人物", "削除：シーンの人物を選択", "Remove: Select scene character");
            case BrowserMode.SelectCoordinateReplaceActor:
                return L("服装卡：选择场景角色", "衣装カード：キャラ選択", "Outfit card: Select character");
            case BrowserMode.SelectCoordinateAccessories:
                return L("自选饰品", "アクセ選択", "Pick accessories");
            case BrowserMode.SelectTrackedAccessoryActor:
                return L("选择管理角色", "管理キャラ選択", "Select character");
            case BrowserMode.ManageTrackedAccessories:
                return L("管理追加饰品", "追加アクセ管理", "Manage appended accessories");
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
            || _browserMode == BrowserMode.SelectCharacterRemoveActor
            || _browserMode == BrowserMode.SelectCoordinateReplaceActor
            || _browserMode == BrowserMode.SelectTrackedAccessoryActor)
            return L("当前场景角色", "現在のシーンキャラ", "Current scene characters");
        if (_browserMode == BrowserMode.SelectCoordinateAccessories)
        {
            return L("仅追加到空槽  ·  已选 ", "空き枠にのみ追加  ·  選択 ", "Empty slots only  ·  Selected ")
                + _selectedCoordinateAccessorySlots.Count;
        }
        if (_browserMode == BrowserMode.ManageTrackedAccessories)
        {
            return L("角色：", "キャラ：", "Character: ")
                + (_trackedAccessoryTargetName ?? "-")
                + L("  ·  已选 ", "  ·  選択 ", "  ·  Selected ")
                + _selectedTrackedAccessorySlots.Count;
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
            return BuildSingleLineEntryLabel(
                _selectedCoordinateAccessorySlots.Contains(entry.ObjectKey) ? "[✓]  " : "[ ]  ",
                entry.DisplayName,
                null);
        }
        if (_browserMode == BrowserMode.ManageTrackedAccessories)
        {
            if (entry.ObjectKey < 0)
            {
                return BuildSingleLineEntryLabel(
                    L("[全部]  ", "[すべて]  ", "[All]  "),
                    entry.DisplayName,
                    null);
            }
            return BuildSingleLineEntryLabel(
                _selectedTrackedAccessorySlots.Contains(entry.ObjectKey) ? "[✓]  " : "[ ]  ",
                entry.DisplayName,
                null);
        }
        if (entry.IsSkipAction)
        {
            return EllipsizeTailForUi(
                L(
                    "不载入镜头 · 仅使用角色动作",
                    "カメラを読込まない · モーションのみ",
                    "Do not load camera · Motion only"),
                BrowserEntryMaxUiUnits);
        }
        if (entry.IsActorTarget)
        {
            return BuildSingleLineEntryLabel(
                L("[角色]  ", "[キャラ]  ", "[Character]  "),
                entry.DisplayName,
                null);
        }
        if (entry.IsDirectory)
        {
            return BuildSingleLineEntryLabel(
                L("[目录]  ", "[フォルダー]  ", "[Folder]  "),
                entry.DisplayName,
                "  >");
        }
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
            return BuildSingleLineEntryLabel(
                "[" + kind + "]  ",
                entry.DisplayName,
                null);
        }

        return BuildSingleLineEntryLabel(
            _browserMode == BrowserMode.CoordinateCards
                ? L("[服装卡]  ", "[衣装カード]  ", "[Outfit card]  ")
                : L("[角色卡]  ", "[キャラカード]  ", "[Character card]  "),
            entry.DisplayName,
            null);
    }

    private static string BuildSingleLineEntryLabel(
        string prefix,
        string value,
        string suffix)
    {
        string safePrefix = NormalizeUiSingleLine(prefix);
        string safeSuffix = NormalizeUiSingleLine(suffix);
        int nameBudget = BrowserEntryMaxUiUnits
            - GetUiVisualUnits(safePrefix)
            - GetUiVisualUnits(safeSuffix);
        if (nameBudget < 3)
        {
            return EllipsizeTailForUi(
                safePrefix + NormalizeUiSingleLine(value) + safeSuffix,
                BrowserEntryMaxUiUnits);
        }

        return safePrefix
            + EllipsizeTailForUi(value, nameBudget)
            + safeSuffix;
    }

    private static string NormalizeUiSingleLine(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);
        bool previousWhitespace = false;
        foreach (char character in value)
        {
            bool whitespace = char.IsWhiteSpace(character);
            if (whitespace)
            {
                if (!previousWhitespace && builder.Length > 0)
                    builder.Append(' ');
            }
            else
            {
                builder.Append(character);
            }
            previousWhitespace = whitespace;
        }
        return builder.ToString().Trim();
    }

    private static string EllipsizeTailForUi(string value, int maximumUnits)
    {
        string safeValue = NormalizeUiSingleLine(value);
        if (maximumUnits <= 0)
            return string.Empty;
        if (GetUiVisualUnits(safeValue) <= maximumUnits)
            return safeValue;
        if (maximumUnits == 1)
            return "…";
        return TakeUiTextFromStart(safeValue, maximumUnits - 1) + "…";
    }

    private static string EllipsizeMiddleForUi(string value, int maximumUnits)
    {
        string safeValue = NormalizeUiSingleLine(value);
        if (maximumUnits <= 0)
            return string.Empty;
        if (GetUiVisualUnits(safeValue) <= maximumUnits)
            return safeValue;
        if (maximumUnits == 1)
            return "…";

        int availableUnits = maximumUnits - 1;
        int startUnits = Math.Max(1, availableUnits * 2 / 5);
        int endUnits = Math.Max(1, availableUnits - startUnits);
        return TakeUiTextFromStart(safeValue, startUnits)
            + "…"
            + TakeUiTextFromEnd(safeValue, endUnits);
    }

    private static int GetUiVisualUnits(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        int units = 0;
        int[] indexes = System.Globalization.StringInfo.ParseCombiningCharacters(value);
        for (int i = 0; i < indexes.Length; i++)
        {
            int length = (i + 1 < indexes.Length ? indexes[i + 1] : value.Length) - indexes[i];
            string element = value.Substring(indexes[i], length);
            units += IsAsciiUiElement(element) ? 1 : 2;
        }
        return units;
    }

    private static string TakeUiTextFromStart(string value, int maximumUnits)
    {
        if (string.IsNullOrEmpty(value) || maximumUnits <= 0)
            return string.Empty;

        int[] indexes = System.Globalization.StringInfo.ParseCombiningCharacters(value);
        int usedUnits = 0;
        int endIndex = 0;
        for (int i = 0; i < indexes.Length; i++)
        {
            int nextIndex = i + 1 < indexes.Length ? indexes[i + 1] : value.Length;
            string element = value.Substring(indexes[i], nextIndex - indexes[i]);
            int elementUnits = IsAsciiUiElement(element) ? 1 : 2;
            if (usedUnits + elementUnits > maximumUnits)
                break;
            usedUnits += elementUnits;
            endIndex = nextIndex;
        }
        return value.Substring(0, endIndex);
    }

    private static string TakeUiTextFromEnd(string value, int maximumUnits)
    {
        if (string.IsNullOrEmpty(value) || maximumUnits <= 0)
            return string.Empty;

        int[] indexes = System.Globalization.StringInfo.ParseCombiningCharacters(value);
        int usedUnits = 0;
        int startIndex = value.Length;
        for (int i = indexes.Length - 1; i >= 0; i--)
        {
            int nextIndex = i + 1 < indexes.Length ? indexes[i + 1] : value.Length;
            string element = value.Substring(indexes[i], nextIndex - indexes[i]);
            int elementUnits = IsAsciiUiElement(element) ? 1 : 2;
            if (usedUnits + elementUnits > maximumUnits)
                break;
            usedUnits += elementUnits;
            startIndex = indexes[i];
        }
        return value.Substring(startIndex);
    }

    private static bool IsAsciiUiElement(string element)
    {
        if (string.IsNullOrEmpty(element))
            return true;
        for (int i = 0; i < element.Length; i++)
        {
            if (element[i] > 0x7f)
                return false;
        }
        return true;
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

        if (_browserMode == BrowserMode.SelectTrackedAccessoryActor
            || _browserMode == BrowserMode.ManageTrackedAccessories)
        {
            ShowPage(WristMenuPage.CharacterPreview);
            SetStatus(
                L("服装卡预览", "衣装カードのプレビュー", "Outfit card preview"),
                new Color(0.47f, 0.9f, 0.55f, 1f),
                0f);
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

        if (_browserMode == BrowserMode.SelectCharacterRemoveActor)
        {
            RestoreCharacterRemovalBrowser(
                L("已取消移除人物", "人物の削除をキャンセルしました", "Character removal cancelled"),
                new Color(1f, 0.72f, 0.25f, 1f));
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
        else if (_browserMode == BrowserMode.LoadVmd)
        {
            ShowPage(WristMenuPage.MmdPlayback);
            SetStatus(
                L("MMD 动作与播放", "MMD モーションと再生", "MMD motion and playback"),
                new Color(1f, 0.72f, 0.25f, 1f),
                0f);
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
            SetStatus(
                L("当前目录：", "現在のフォルダー：", "Current folder: ") + entry.FullPath,
                new Color(0.47f, 0.9f, 0.55f, 1f),
                5f);
            return;
        }

        SetStatus(
            L("已选择：", "選択：", "Selected: ") + NormalizeUiSingleLine(entry.DisplayName),
            new Color(0.47f, 0.9f, 0.55f, 1f),
            3f);

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
            case BrowserMode.SelectCharacterRemoveActor:
                HandleCharacterRemoveActorSelection(entry);
                break;
            case BrowserMode.SelectCoordinateReplaceActor:
                HandleCoordinateReplaceActorSelection(entry);
                break;
            case BrowserMode.SelectCoordinateAccessories:
                HandleCoordinateAccessoryToggle(entry);
                break;
            case BrowserMode.SelectTrackedAccessoryActor:
                HandleTrackedAccessoryActorSelection(entry);
                break;
            case BrowserMode.ManageTrackedAccessories:
                HandleTrackedAccessoryEntry(entry);
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
        if (_browserMode == BrowserMode.ManageTrackedAccessories)
        {
            BeginTrackedAccessoryRemoval(false);
            return;
        }

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
                ShowPage(WristMenuPage.MmdPlayback);
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
            ShowPage(WristMenuPage.MmdPlayback);
            SetStatus(
                L("MMD 动作与播放", "MMD モーションと再生", "MMD motion and playback"),
                new Color(1f, 0.72f, 0.25f, 1f),
                0f);
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
            // Return to the dedicated transport page so playback, rewind, and
            // maintenance controls remain immediately available after loading.
            ShowPage(WristMenuPage.MmdPlayback);
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
        string ignoredTargetStatus;
        HashSet<int> existingCharacterKeys = new HashSet<int>(
            VRVmdTargetService.GetAllTargets(out ignoredTargetStatus)
                .Select(target => target.ObjectKey));
        SetStatus(L("正在读取角色卡…", "キャラカードを読込中…", "Loading character card…"),
            new Color(0.47f, 0.9f, 0.55f, 1f), 0f);
        yield return null;

        string status;
        bool success = VRCharacterCardService.ExecuteAdd(_characterCardMode, path, out status);
        if (success && VRMmdPlaybackController.Instance != null)
        {
            // Studio.AddFemale/AddMale returns before the new ChaControl always
            // becomes a live actor. Resolve only keys created by this operation,
            // then let the high-heel controller retry until MMDD is ready.
            float deadline = Time.realtimeSinceStartup + 15f;
            int[] addedKeys = new int[0];
            do
            {
                yield return null;
                List<VRVmdActorTarget> liveTargets =
                    VRVmdTargetService.GetAllTargets(out ignoredTargetStatus);
                addedKeys = liveTargets
                    .Where(target => !existingCharacterKeys.Contains(target.ObjectKey))
                    .Select(target => target.ObjectKey)
                    .ToArray();
            }
            while (addedKeys.Length == 0 && Time.realtimeSinceStartup < deadline);

            if (addedKeys.Length > 0)
                VRMmdPlaybackController.Instance.RequestHighHeelsRefresh(addedKeys);
            else
                VRLog.Warn("Added character did not become ready in time for automatic high-heel preset detection.");
        }
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
        VRTimelineMutationSession timelineSession = null;
        bool characterReplaced = false;
        bool timelineTargetReady = false;
        try
        {
            SetStatus(L("正在替换场景角色…", "シーンのキャラを置換中…", "Replacing scene character…"),
                new Color(0.47f, 0.9f, 0.55f, 1f), 0f);
            yield return null;

            ResolveSettings();
            string status;
            bool prepared = VRTimelineService.PauseForSceneMutation(
                out timelineSession,
                out status);
        if (prepared && VRMmdPlaybackController.Instance != null)
            VRMmdPlaybackController.Instance.PrepareForCharacterReplacement();
        VRMmddCharacterReplacementSession vmdSession = null;
        if (prepared)
        {
            prepared = VRMmddService.TryPrepareCharacterReplacement(
                objectKey,
                out vmdSession,
                out status);
        }
        VRCharacterOutfitPreservationState outfitState = null;
        bool preserveOutfit = _settings != null
            && _settings.PreserveOutfitOnCharacterReplace;

        if (prepared && preserveOutfit)
        {
            Studio.OCIChar originalCharacter;
            string captureStatus;
            if (!VRCharacterCardService.TryGetCharacter(
                    objectKey,
                    out originalCharacter,
                    out captureStatus)
                || !VRCharacterOutfitPreservationService.TryCapture(
                    objectKey,
                    originalCharacter,
                    out outfitState,
                    out captureStatus))
            {
                prepared = false;
                status = L(
                    "无法安全保留当前服装，角色未替换：",
                    "現在の衣装を安全に保持できないため、キャラを置換しません：",
                    "Could not safely preserve the current outfit; the character was not replaced: ")
                    + captureStatus;
                if (vmdSession != null)
                {
                    string rollbackStatus;
                    if (!VRMmddService.TryRestoreCharacterReplacement(
                        vmdSession,
                        objectKey,
                        out rollbackStatus))
                    {
                        status += "；原 VMD 恢复失败：" + rollbackStatus;
                        VRMmddService.DiscardCharacterReplacement(vmdSession);
                    }
                }
            }
        }

        if (prepared && vmdSession != null)
        {
            // MMDD removes its controller dictionaries synchronously, but Unity
            // destroys the pose controller and generated bones at the end of the
            // frame. Do not let ChangeChara rebuild the face/body while those old
            // components can still write to the character hierarchy.
            yield return new WaitForEndOfFrame();
            yield return null;
        }

        bool success = false;
        if (prepared)
        {
            if (vmdSession != null || outfitState != null)
            {
                SetStatus(
                    L(
                        preserveOutfit
                            ? "已清理旧 VMD 并备份当前服装，正在替换角色…"
                            : "已清理旧 VMD，正在替换角色…",
                        preserveOutfit
                            ? "古い VMD を解除して現在の衣装を保存し、キャラを置換しています…"
                            : "古い VMD を解除し、キャラを置換しています…",
                        preserveOutfit
                            ? "Cleared the old VMD and saved the current outfit; replacing the character…"
                            : "Cleared the old VMD; replacing the character…"),
                    new Color(0.47f, 0.9f, 0.55f, 1f),
                    0f);
            }

            string replacementStatus;
            success = VRCharacterCardService.ExecuteReplace(
                objectKey,
                path,
                out replacementStatus);
            characterReplaced = success;
            status = replacementStatus;

            if (!success)
            {
                VRCharacterOutfitPreservationService.Discard(outfitState);
                if (vmdSession != null)
                {
                    string rollbackStatus;
                    if (VRMmddService.TryRestoreCharacterReplacement(
                        vmdSession,
                        objectKey,
                        out rollbackStatus))
                    {
                        status += "；原 VMD 已恢复";
                    }
                    else
                    {
                        status += "；原 VMD 恢复失败：" + rollbackStatus;
                        VRMmddService.DiscardCharacterReplacement(vmdSession);
                    }
                }
            }
            else
            {
                SetStatus(
                    L(
                        preserveOutfit
                            ? "角色已替换，正在等待新骨架并合并旧服装…"
                            : vmdSession != null
                                ? "角色已替换，正在等待新骨架并重新绑定 VMD…"
                                : "角色已替换，正在等待新骨架稳定…",
                        preserveOutfit
                            ? "キャラを置換しました。新しい骨格を待って衣装を統合しています…"
                            : vmdSession != null
                                ? "キャラを置換しました。新しい骨格を待って VMD を再接続しています…"
                                : "キャラを置換しました。新しい骨格の安定を待っています…",
                        preserveOutfit
                            ? "Character replaced; waiting for the new skeleton and merging the old outfit…"
                            : vmdSession != null
                                ? "Character replaced; waiting for the new skeleton and rebinding VMD…"
                                : "Character replaced; waiting for the new skeleton to stabilize…"),
                    new Color(0.47f, 0.9f, 0.55f, 1f),
                    0f);

                // ChangeChara rebuilds the character over several frames. Let it enter
                // the loading state first, then wait for its new bones before MMDD is
                // allowed to create fresh motion and morph controllers.
                yield return null;
                yield return null;
                float deadline = Time.realtimeSinceStartup + 30f;
                Studio.OCIChar liveCharacter = null;
                string liveStatus = null;
                bool characterReady = false;
                while (Time.realtimeSinceStartup < deadline)
                {
                    if (!VRCharacterCardService.TryGetCharacter(
                        objectKey,
                        out liveCharacter,
                        out liveStatus))
                    {
                        break;
                    }

                    if (liveCharacter.charInfo != null
                        && liveCharacter.charInfo.loadEnd)
                    {
                        characterReady = true;
                        break;
                    }
                    yield return null;
                }

                if (!characterReady)
                {
                    success = false;
                    status = string.IsNullOrEmpty(liveStatus)
                        ? replacementStatus
                            + "；新角色读取超时，旧 VMD 已安全清理但未重新绑定"
                        : replacementStatus + "；" + liveStatus;
                    VRCharacterOutfitPreservationService.Discard(outfitState);
                    if (vmdSession != null)
                        VRMmddService.DiscardCharacterReplacement(vmdSession);
                }
                else
                {
                    // loadEnd can become true before every child bone has completed its
                    // Start/OnEnable pass. A short stabilization window avoids binding
                    // MMDD to transient transforms.
                    for (int frame = 0; frame < 6; frame++)
                        yield return null;

                    // Accessory States is registered through KKAPI and can be added a
                    // few frames after ChaControl.loadEnd. Timeline clothing tracks
                    // call it from a postfix without a null check, so wait for the
                    // registered controller instead of resuming into a frame-wide NRE.
                    string controllerStatus;
                    float controllerDeadline = Time.realtimeSinceStartup + 5f;
                    while (!VRCharacterCardService.AreReplacementControllersReady(
                            liveCharacter,
                            out controllerStatus)
                        && Time.realtimeSinceStartup < controllerDeadline)
                    {
                        yield return null;
                    }
                    bool controllersReady = VRCharacterCardService.AreReplacementControllersReady(
                        liveCharacter,
                        out controllerStatus);
                    if (!controllersReady)
                    {
                        VRLog.Warn(controllerStatus);
                        success = false;
                        status = replacementStatus + "；" + controllerStatus
                            + "，Timeline 已保持暂停，避免当前帧写入未完成的新角色";
                        VRCharacterOutfitPreservationService.Discard(outfitState);
                        if (vmdSession != null)
                            VRMmddService.DiscardCharacterReplacement(vmdSession);
                    }
                    else
                    {
                        timelineTargetReady = true;
                        status = replacementStatus;
                        if (outfitState != null)
                        {
                            VRCharacterOutfitRestoreSession outfitSession;
                            string outfitStatus;
                            bool outfitStarted = VRCharacterOutfitPreservationService.TryBeginRestore(
                                outfitState,
                                objectKey,
                                liveCharacter,
                                out outfitSession,
                                out outfitStatus);
                            if (outfitStarted)
                            {
                                VRCharacterOutfitRestoreResult outfitResult =
                                    new VRCharacterOutfitRestoreResult();
                                yield return StartCoroutine(MonitorCharacterOutfitRestore(
                                    outfitSession,
                                    outfitResult));
                                outfitStatus = outfitResult.Status;
                                outfitStarted = outfitResult.Success;
                            }
                            else
                            {
                                VRCharacterOutfitPreservationService.Discard(outfitState);
                            }

                            status += "；" + outfitStatus;
                            if (!outfitStarted)
                                success = false;
                        }

                        if (vmdSession != null)
                        {
                            string restoreStatus;
                            bool restored = VRMmddService.TryRestoreCharacterReplacement(
                                vmdSession,
                                objectKey,
                                true,
                                out restoreStatus);
                            if (!restored)
                            {
                                SetStatus(
                                    L(
                                        "新骨架尚未接受 VMD，正在重试…",
                                        "新しい骨格へ VMD を再接続できないため、再試行しています…",
                                        "The new skeleton did not accept VMD yet; retrying…"),
                                    new Color(1f, 0.72f, 0.25f, 1f),
                                    0f);
                                for (int frame = 0; frame < 6; frame++)
                                    yield return null;
                                restored = VRMmddService.TryRestoreCharacterReplacement(
                                    vmdSession,
                                    objectKey,
                                    true,
                                    out restoreStatus);
                            }

                            if (restored)
                            {
                                status += "；" + restoreStatus;
                                if (VRMmdPlaybackController.Instance != null)
                                    VRMmdPlaybackController.Instance.NotifyCharacterReplacementRebound();
                            }
                            else
                            {
                                success = false;
                                status += "；VMD 重新绑定失败，旧绑定已清理：" + restoreStatus;
                                VRMmddService.DiscardCharacterReplacement(vmdSession);
                            }
                        }
                    }
                }
            }
        }

        string timelineRestoreStatus;
        bool timelineRestored = VRTimelineService.RestoreAfterSceneMutation(
            timelineSession,
            characterReplaced,
            !characterReplaced || timelineTargetReady,
            out timelineRestoreStatus);
        if (!string.IsNullOrEmpty(timelineRestoreStatus))
            status = string.IsNullOrEmpty(status)
                ? timelineRestoreStatus
                : status + "；" + timelineRestoreStatus;
        if (!timelineRestored)
            success = false;

        if (characterReplaced && VRMmdPlaybackController.Instance != null)
            VRMmdPlaybackController.Instance.RequestHighHeelsRefresh(objectKey);

        _operationInProgress = false;
        ShowPage(WristMenuPage.CharacterPreview);
        if (success)
            VRLog.Info(status);
        else
            VRLog.Warn(status);
        SetStatus(
            status,
            success ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.38f, 0.34f, 1f),
            success ? 6f : 8f);
        }
        finally
        {
            if (timelineSession != null && !timelineSession.Completed)
            {
                string emergencyStatus;
                VRTimelineService.RestoreAfterSceneMutation(
                    timelineSession,
                    characterReplaced,
                    !characterReplaced || timelineTargetReady,
                    out emergencyStatus);
                if (!string.IsNullOrEmpty(emergencyStatus))
                    VRLog.Warn("Timeline replacement cleanup: " + emergencyStatus);
            }
            _operationInProgress = false;
        }
    }

    private IEnumerator ExecuteCoordinateReplacementAfterFrame(
        string path,
        int objectKey,
        Studio.OCIChar expectedCharacter,
        VRCoordinateReplaceMode mode,
        int[] selectedAccessorySlots,
        bool undo,
        VRBoundAccessoryReplaceConfirmation boundConfirmation = null)
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
                : mode == VRCoordinateReplaceMode.BoundCompatibleFull
                    ? L(
                        "已确认风险，正在按原槽完整替换并保留绑定数据…",
                        "確認済み：元スロットへ完全置換し、連動データを移行しています…",
                        "Confirmed: replacing exact accessory slots with their bound data…")
                : mode == VRCoordinateReplaceMode.RemoveTrackedAccessories
                    ? L(
                        "正在安全移除所选追加饰品…",
                        "選択した追加アクセを安全に削除しています…",
                        "Safely removing the selected appended accessories…")
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
                            "正在完整替换衣服，并把全部源饰品追加到空槽…",
                            "衣服を一式変更し、元カードの全アクセを空き枠へ追加しています…",
                            "Replacing all clothes and adding every source accessory to empty slots…"),
            new Color(0.47f, 0.9f, 0.55f, 1f),
            0f);
        VRCoordinateLoadSession session = null;
        string status = undo
            ? "撤销换装没有启动"
            : mode == VRCoordinateReplaceMode.RemoveTrackedAccessories
                ? "移除追加饰品没有启动"
                : "服装卡替换没有启动";
        bool success = false;
        try
        {
            yield return null;

            bool started;
            try
            {
                started = boundConfirmation != null
                    ? VRCoordinateCardService.TryBeginBoundCompatibleReplace(
                        boundConfirmation,
                        out session,
                        out status)
                    : undo
                    ? VRCoordinateCardService.TryBeginUndoLastReplace(
                        objectKey,
                        expectedCharacter,
                        out session,
                        out status)
                    : mode == VRCoordinateReplaceMode.RemoveTrackedAccessories
                        ? VRCoordinateCardService.TryBeginRemoveTrackedAccessories(
                            objectKey,
                            expectedCharacter,
                            selectedAccessorySlots,
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
                status = (undo
                    ? "撤销换装失败："
                    : mode == VRCoordinateReplaceMode.RemoveTrackedAccessories
                        ? "移除追加饰品失败："
                        : "服装卡替换失败：")
                    + ex.Message;
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
                if (VRMmdPlaybackController.Instance != null)
                    VRMmdPlaybackController.Instance.RequestHighHeelsRefresh(objectKey);
                if (mode == VRCoordinateReplaceMode.RemoveTrackedAccessories)
                {
                    List<VRTrackedAccessorySlotInfo> remaining;
                    string remainingStatus;
                    if (VRCoordinateCardService.TryGetTrackedAccessorySlots(
                        character,
                        out remaining,
                        out remainingStatus)
                        && remaining.Count > 0)
                    {
                        OpenTrackedAccessoryManager(
                            objectKey,
                            character,
                            _trackedAccessoryTargetName,
                            _trackedAccessoryOffset);
                    }
                }
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
                    if (VRMmdPlaybackController.Instance != null)
                        VRMmdPlaybackController.Instance.RequestHighHeelsRefresh(objectKey);
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

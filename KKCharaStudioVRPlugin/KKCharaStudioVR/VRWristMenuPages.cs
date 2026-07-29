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
        SelectVmdActor,
        SelectVmdCamera,
        AddFemale,
        AddMale,
        ReplaceCharacter
    }

    private readonly VRWristMenuButtonTarget[] _browserEntryButtons =
        new VRWristMenuButtonTarget[BrowserVisibleRows];
    private List<VRWristFileEntry> _browserEntries = new List<VRWristFileEntry>();
    private List<VRWristFileEntry> _browserFixedEntries;
    private Text _browserTitleText;
    private Text _browserPathText;
    private Text _browserScrollText;
    private Text _characterSelectionText;
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
    private float _nextCharacterRefresh;
    private bool _operationInProgress;

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
            "文件",
            92f,
            10f,
            444f,
            40f,
            27,
            TextAnchor.MiddleLeft,
            Color.white);

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
            "保存场景卡",
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
            "保存位置\nUserData\\studio\\scene",
            38f,
            78f,
            484f,
            54f,
            18,
            TextAnchor.MiddleLeft,
            new Color(0.68f, 0.92f, 0.96f, 1f));

        CreateButton(
            "SceneSaveNow",
            "保存新场景卡\nSAVE NEW SCENE",
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
            "角色卡",
            92f,
            10f,
            444f,
            40f,
            28,
            TextAnchor.MiddleLeft,
            Color.white);

        Image selectionBackground = CreateImage(
            "CharacterCardSelectionBackground",
            _characterCardsPage.transform,
            24f,
            66f,
            512f,
            42f,
            new Color(0.55f, 0.9f, 0.7f, 0.065f));
        ApplyGlassEffects(selectionBackground, new Color(0.72f, 1f, 0.82f, 0.13f), false, 0f);
        _characterSelectionText = CreateText(
            "CharacterCardSelection",
            _characterCardsPage.transform,
            "未选择角色",
            38f,
            70f,
            484f,
            34f,
            18,
            TextAnchor.MiddleLeft,
            new Color(0.7f, 0.8f, 0.74f, 1f));

        CreateText(
            "CharacterAddSection",
            _characterCardsPage.transform,
            "添加角色  ADD",
            24f,
            122f,
            512f,
            24f,
            18,
            TextAnchor.MiddleLeft,
            new Color(0.47f, 0.9f, 0.55f, 1f));
        CreateButton(
            "CharacterAddFemale",
            "添加女角色\nFEMALE",
            24f,
            152f,
            new Color(0.075f, 0.24f, 0.14f, 0.48f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            () => OpenCharacterBrowser(VRCharacterCardMode.AddFemale),
            _characterCardsPage.transform,
            248f,
            62f,
            19);
        CreateButton(
            "CharacterAddMale",
            "添加男角色\nMALE",
            288f,
            152f,
            new Color(0.075f, 0.24f, 0.14f, 0.48f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            () => OpenCharacterBrowser(VRCharacterCardMode.AddMale),
            _characterCardsPage.transform,
            248f,
            62f,
            19);

        CreateText(
            "CharacterReplaceSection",
            _characterCardsPage.transform,
            "替换当前角色  REPLACE",
            24f,
            234f,
            512f,
            24f,
            18,
            TextAnchor.MiddleLeft,
            new Color(0.25f, 0.86f, 0.94f, 1f));
        CreateButton(
            "CharacterReplaceSelected",
            "替换选中角色  >\nREPLACE SELECTED",
            24f,
            264f,
            new Color(0.08f, 0.15f, 0.18f, 0.5f),
            new Color(0.12f, 0.32f, 0.39f, 0.76f),
            () => OpenCharacterBrowser(VRCharacterCardMode.ReplaceSelected),
            _characterCardsPage.transform,
            512f,
            72f,
            20);
    }

    private void HandleOpenCharacterCards()
    {
        ShowPage(WristMenuPage.CharacterCards);
        SetStatus("角色卡", new Color(0.47f, 0.9f, 0.55f, 1f), 0f);
    }

    private void RefreshCharacterCardsPage()
    {
        if (_characterSelectionText == null)
            return;

        Studio.OCIChar character;
        string selectionStatus;
        bool hasCharacter = VRCharacterClothingService.TryGetSelectedCharacter(
            out character,
            out selectionStatus);
        _characterSelectionText.text = hasCharacter ? "当前角色  " + selectionStatus : selectionStatus;
        _characterSelectionText.color = hasCharacter
            ? new Color(0.62f, 1f, 0.72f, 1f)
            : new Color(1f, 0.45f, 0.4f, 1f);
    }

    private void OpenCharacterBrowser(VRCharacterCardMode mode)
    {
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
            : mode == VRCharacterCardMode.AddMale
                ? BrowserMode.AddMale
                : BrowserMode.ReplaceCharacter;
        OpenFileBrowser(browserMode, root, root);
        if (!string.IsNullOrEmpty(status))
            SetStatus(status, new Color(0.47f, 0.9f, 0.55f, 1f), 0f);
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
                SetStatus("目录不存在：" + root, new Color(1f, 0.38f, 0.34f, 1f), 6f);
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
            SetStatus("无法打开目录：" + ex.Message, new Color(1f, 0.38f, 0.34f, 1f), 6f);
        }
    }

    private void RefreshBrowserEntries()
    {
        if ((_browserMode == BrowserMode.SelectVmdActor
                || _browserMode == BrowserMode.SelectVmdCamera)
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
                case BrowserMode.ReplaceCharacter:
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
            : (_browserEntries.Count == 0 ? "此目录没有可用文件" : string.Empty);
    }

    private string GetBrowserTitle()
    {
        switch (_browserMode)
        {
            case BrowserMode.LoadVmd:
                return "读取 VMD";
            case BrowserMode.SelectVmdActor:
                return "选择动作目标";
            case BrowserMode.SelectVmdCamera:
                return "选择镜头 VMD";
            case BrowserMode.AddFemale:
                return "添加女角色";
            case BrowserMode.AddMale:
                return "添加男角色";
            case BrowserMode.ReplaceCharacter:
                return "替换角色卡";
            default:
                return "文件";
        }
    }

    private string BuildBrowserPathLabel()
    {
        if (_browserMode == BrowserMode.SelectVmdActor)
            return "当前场景角色";
        if (_browserMode == BrowserMode.SelectVmdCamera)
            return "同目录镜头";

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
        if (entry.IsSkipAction)
            return "不载入镜头\nMOTION ONLY";
        if (entry.IsActorTarget)
            return "[角色]  " + entry.DisplayName;
        if (entry.IsDirectory)
            return "[目录]  " + entry.DisplayName + "  >";
        if (entry.HasVmdMetadata)
        {
            bool actor = entry.VmdMetadata.HasActorData;
            bool camera = entry.VmdMetadata.HasCameraData;
            string kind = actor && camera
                ? "混合"
                : camera
                    ? "镜头"
                    : (entry.VmdMetadata.Content & VRVmdContent.Motion) != 0
                        ? "动作"
                        : "表情";
            return "[" + kind + "]  " + entry.DisplayName;
        }

        return "[角色卡]  " + entry.DisplayName;
    }

    private void HandleBrowserBack()
    {
        if (_browserMode == BrowserMode.SelectVmdActor
            || _browserMode == BrowserMode.SelectVmdCamera)
        {
            OpenFileBrowser(
                BrowserMode.LoadVmd,
                VRMmddService.DefaultVmdRoot,
                string.IsNullOrEmpty(_vmdMotionDirectory)
                    ? VRMmddService.DefaultVmdRoot
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

        if (_browserMode == BrowserMode.AddFemale
            || _browserMode == BrowserMode.AddMale
            || _browserMode == BrowserMode.ReplaceCharacter)
        {
            ShowPage(WristMenuPage.CharacterCards);
            SetStatus("角色卡", new Color(0.47f, 0.9f, 0.55f, 1f), 0f);
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
            case BrowserMode.AddFemale:
            case BrowserMode.AddMale:
            case BrowserMode.ReplaceCharacter:
                OpenCharacterCardPreview(entry.FullPath);
                break;
        }
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
        SetStatus("正在载入 VMD…", new Color(1f, 0.72f, 0.25f, 1f), 0f);
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
        string status;
        bool success = VRMmddService.LoadVmdPackage(
            motionPath,
            cameraPath,
            audioPath,
            actorObjectKeys,
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

    private IEnumerator ExecuteCharacterCardAfterFrame(string path)
    {
        _operationInProgress = true;
        SetStatus("正在读取角色卡…", new Color(0.47f, 0.9f, 0.55f, 1f), 0f);
        yield return null;

        string status;
        bool success = VRCharacterCardService.Execute(_characterCardMode, path, out status);
        _operationInProgress = false;
        SetStatus(
            status,
            success ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.38f, 0.34f, 1f),
            success ? 6f : 8f);
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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using VRGIN.Core;

namespace KKCharaStudioVR;

public sealed partial class VRWristMenuController : MonoBehaviour
{
    private const float MenuPressMaxDuration = 0.45f;
    private const float TrackingLossCloseDelay = 0.35f;
    private const float BaseMenuScale = 0.00045f;
    private const float MenuWidth = 560f;
    private const float MenuHeight = 500f;

    private static readonly Color GlassPanelColor = new Color(0.022f, 0.03f, 0.044f, 0.91f);
    private static readonly Color GlassHeaderColor = new Color(0.68f, 0.78f, 0.92f, 0.065f);
    private static readonly Color GlassSurfaceColor = new Color(0.34f, 0.42f, 0.54f, 0.16f);
    private static readonly Color GlassOutlineColor = new Color(0.78f, 0.88f, 1f, 0.2f);
    private static readonly Color GlassShadowColor = new Color(0f, 0f, 0f, 0.56f);
    private static readonly Color PrimaryTextColor = new Color(0.965f, 0.975f, 1f, 1f);
    private static readonly Color SecondaryTextColor = new Color(0.67f, 0.72f, 0.8f, 1f);
    private static readonly Color TertiaryTextColor = new Color(0.48f, 0.54f, 0.63f, 1f);

    private enum WristMenuPage
    {
        Root,
        Clothing,
        ClothingParts,
        ClothingOpacity,
        ClothingOpacityQuick,
        Coordinate,
        Browser,
        SceneSave,
        CharacterCards,
        CharacterPreview,
        CharacterRemoveConfirm,
        Timeline,
        MmdSettings,
        MmdPlayback,
        MmdCamera,
        MmdCues,
        MmdCuePresets,
        MmdClearConfirm,
        HighHeels,
        Settings
    }

    public static VRWristMenuController Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance._isOpen && !Instance._presentationSuppressed;
    internal static bool IsTimelinePageOpen =>
        IsOpen && Instance._page == WristMenuPage.Timeline;

    private KKCharaStudioVRSettings _settings;
    private GameObject _menuRoot;
    private GameObject _rootPage;
    private GameObject _clothingPage;
    private GameObject _browserPage;
    private GameObject _sceneSavePage;
    private GameObject _characterCardsPage;
    private GameObject _characterPreviewPage;
    private RectTransform _menuRect;
    private Text _statusText;
    private Image _statusIndicator;
    private VRWristMenuButtonTarget _clothingTargetButton;
    private VRWristMenuButtonTarget _rootLoadVmdButton;
    private VRWristMenuButtonTarget _loadVmdButton;
    private Texture2D _roundedTexture;
    private Sprite _roundedSprite;
    private Texture2D _cursorTexture;
    private Sprite _cursorSprite;
    private readonly VRWristMenuButtonTarget[] _clothingPartButtons =
        new VRWristMenuButtonTarget[VRCharacterClothingService.PartCount];
    private int _clothingTargetObjectKey = -1;
    private Studio.OCIChar _clothingTargetCharacter;
    private VRWristMenuButtonTarget _hoveredButton;
    private Font _font;
    private Font _emphasizedFont;
    private bool _ownsFont;
    private bool _ownsEmphasizedFont;
    private LineRenderer _laser;
    private Image _surfaceCursor;
    private Image _surfaceCursorInner;
    private Image _surfaceCursorDot;
    private Material _laserMaterial;
    private Transform _rightTransform;
    private int _visibleLayer;
    private int _colliderLayer;
    private int _pointerMask;
    private bool _isOpen;
    private bool _presentationSuppressed;
    private bool _menuPressActive;
    private bool _menuPressChorded;
    private bool _poseInitialized;
    private float _menuPressStarted;
    private float _statusUntil;
    private float _nextClothingRefresh;
    private float _trackingUnavailableSince = -1f;
    private WristMenuPage _page;
    private WristMenuPage _lastStablePage = WristMenuPage.Root;

    private void Awake()
    {
        Instance = this;
        ResolveSettings();
    }

    private void Update()
    {
        ResolveSettings();
        if (_presentationSuppressed)
        {
            if (_menuRoot != null && _menuRoot.activeSelf)
                _menuRoot.SetActive(false);
            SetPointerVisible(false);
            return;
        }
        HandleMenuButton();

        if (_settings != null && !_settings.WristMenuEnabled)
        {
            if (_isOpen)
                SetOpen(false);
            return;
        }

        if (!_isOpen)
            return;

        if (!HasTrackedMenuControllers())
        {
            SetHoveredButton(null);
            SetPointerVisible(false);
            if (_trackingUnavailableSince < 0f)
                _trackingUnavailableSince = Time.unscaledTime;
            else if (Time.unscaledTime - _trackingUnavailableSince >= TrackingLossCloseDelay)
            {
                VRLog.Warn("Wrist menu closed after controller tracking was lost.");
                SetOpen(false);
            }
            return;
        }
        _trackingUnavailableSince = -1f;

        if (!EnsureMenu())
        {
            SetOpen(false);
            return;
        }

        UpdatePose();
        UpdateTransientState();
        UpdatePointer();
    }

    private void OnDisable()
    {
        VRCoordinateCardService.AbortActiveSession();
        _operationInProgress = false;
        SetOpen(false);
    }

    private void OnDestroy()
    {
        SetOpen(false);
        if (_menuRoot != null)
            Destroy(_menuRoot);
        DestroyPointerVisuals();
        if (_roundedSprite != null)
            Destroy(_roundedSprite);
        if (_roundedTexture != null)
            Destroy(_roundedTexture);
        if (_cursorSprite != null)
            Destroy(_cursorSprite);
        if (_cursorTexture != null)
            Destroy(_cursorTexture);
        if (_ownsFont && _font != null)
            Destroy(_font);
        if (_ownsEmphasizedFont && _emphasizedFont != null && _emphasizedFont != _font)
            Destroy(_emphasizedFont);
        ReleaseCharacterPreviewTexture();
        if (Instance == this)
            Instance = null;
    }

    public void ToggleMenu()
    {
        SetOpen(!_isOpen);
    }

    private void ResolveSettings()
    {
        if (_settings == null && VR.Manager != null && VR.Manager.Context != null)
            _settings = VR.Manager.Context.Settings as KKCharaStudioVRSettings;
    }

    private void HandleMenuButton()
    {
        // While open, accept the configured primary face button even if positional tracking is temporarily invalid so
        // the menu can always release its input lock.
        bool useRightHand = _settings != null
            && _settings.ControllerFaceButtonLayout == KKCharaStudioVRSettings.ControllerLayoutRightHand;
        SteamVR_Controller.Device menuDevice = GetDevice(
            useRightHand ? VR.Mode?.Right : VR.Mode?.Left,
            !_isOpen);
        if (menuDevice == null)
        {
            _menuPressActive = false;
            return;
        }

        if (menuDevice.GetPressDown(EVRButtonId.k_EButton_A))
        {
            _menuPressActive = true;
            _menuPressChorded = false;
            _menuPressStarted = Time.unscaledTime;
        }

        if (_menuPressActive && menuDevice.GetPress(EVRButtonId.k_EButton_A))
        {
            _menuPressChorded |= menuDevice.GetPress(EVRButtonId.k_EButton_Grip)
                || menuDevice.GetPress(EVRButtonId.k_EButton_Axis1);
        }

        if (!_menuPressActive || !menuDevice.GetPressUp(EVRButtonId.k_EButton_A))
            return;

        float duration = Time.unscaledTime - _menuPressStarted;
        _menuPressActive = false;
        if (!_menuPressChorded && duration <= MenuPressMaxDuration
            && (_settings == null || _settings.WristMenuEnabled))
        {
            ToggleMenu();
            menuDevice.TriggerHapticPulse(500, EVRButtonId.k_EButton_Axis0);
        }
    }

    internal void SetPresentationSuppressed(bool suppressed)
    {
        if (_presentationSuppressed == suppressed)
            return;
        _presentationSuppressed = suppressed;
        _menuPressActive = false;
        SetHoveredButton(null);
        SetPointerVisible(false);
        if (_menuRoot != null)
            _menuRoot.SetActive(!suppressed && _isOpen);
    }

    private void SetOpen(bool open)
    {
        if (open && (!EnsureMenu() || !EnsurePointer()))
        {
            _isOpen = false;
            SetPointerVisible(false);
            if (_menuRoot != null)
                _menuRoot.SetActive(false);
            VRLog.Error("Wrist menu stayed closed because its controller pointer could not initialize.");
            return;
        }

        _isOpen = open;
        _poseInitialized = false;
        _trackingUnavailableSince = -1f;
        SetHoveredButton(null);
        SetPointerVisible(false);

        if (_menuRoot != null)
            _menuRoot.SetActive(open);
        if (open)
        {
            if (_operationInProgress)
            {
                SetStatus(
                    L(
                        "操作仍在进行，请稍候",
                        "処理中です。しばらくお待ちください",
                        "An operation is still running. Please wait"),
                    new Color(1f, 0.72f, 0.25f, 1f),
                    0f);
            }
            else
            {
                ShowPage(_lastStablePage);
                SetStatus(L("就绪", "準備完了", "Ready"), new Color(0.78f, 0.86f, 0.9f, 1f), 0f);
            }
        }

        VRLog.Info("Wrist menu " + (open ? "opened." : "closed."));
    }

    private bool EnsureMenu()
    {
        if (_menuRoot != null)
            return true;
        if (VR.Mode == null || VR.Mode.Left == null || VR.Camera == null || VR.Camera.Head == null)
            return false;

        _visibleLayer = LayerMask.NameToLayer(VR.Context.GuiLayer);
        if (_visibleLayer < 0)
            _visibleLayer = 0;
        _colliderLayer = LayerMask.NameToLayer(VR.Context.InvisibleLayer);
        if (_colliderLayer < 0)
            _colliderLayer = 2;
        _pointerMask = 1 << _colliderLayer;
        if (_font == null)
            _font = ResolveFont(false, out _ownsFont);
        if (_emphasizedFont == null)
            _emphasizedFont = ResolveFont(true, out _ownsEmphasizedFont);
        EnsureGlassAssets();

        _menuRoot = new GameObject("KKVR_WristMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        _menuRoot.layer = _visibleLayer;
        _menuRoot.transform.SetParent(transform, false);
        _menuRect = _menuRoot.GetComponent<RectTransform>();
        _menuRect.sizeDelta = new Vector2(MenuWidth, MenuHeight);
        _menuRect.localScale = Vector3.one * BaseMenuScale;

        Canvas canvas = _menuRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 30000;
        if (VR.Camera.SteamCam != null)
            canvas.worldCamera = VR.Camera.SteamCam.camera;

        CanvasScaler scaler = _menuRoot.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        CreateImage(
            "PanelShadowFar",
            _menuRect,
            6f,
            9f,
            MenuWidth,
            MenuHeight,
            new Color(0f, 0f, 0f, 0.2f));
        CreateImage(
            "PanelShadowNear",
            _menuRect,
            3f,
            5f,
            MenuWidth,
            MenuHeight,
            new Color(0f, 0f, 0f, 0.28f));
        Image glassPanel = CreateImage("Background", _menuRect, 0f, 0f, MenuWidth, MenuHeight, GlassPanelColor);
        ApplyGlassEffects(glassPanel, GlassOutlineColor, true, 6f);
        Image glassHeader = CreateImage("Header", _menuRect, 0f, 0f, MenuWidth, 58f, GlassHeaderColor);
        ApplyGlassEffects(glassHeader, new Color(1f, 1f, 1f, 0.07f), false, 0f);
        CreateImage("TopGlint", _menuRect, 22f, 7f, 516f, 1f, new Color(1f, 1f, 1f, 0.2f), false);
        CreateImage("HeaderDivider", _menuRect, 24f, 57f, 512f, 1f, new Color(0.72f, 0.84f, 1f, 0.12f), false);

        _rootPage = CreateRectObject("RootPage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _clothingPage = CreateRectObject("ClothingPage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _clothingPartsPage = CreateRectObject("ClothingPartsPage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _clothingOpacityPage = CreateRectObject("ClothingOpacityPage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _clothingOpacityQuickPage = CreateRectObject("ClothingOpacityQuickPage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _coordinatePage = CreateRectObject("CoordinatePage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _browserPage = CreateRectObject("BrowserPage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _sceneSavePage = CreateRectObject("SceneSavePage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _characterCardsPage = CreateRectObject("CharacterCardsPage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _characterPreviewPage = CreateRectObject("CharacterPreviewPage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _characterRemoveConfirmPage = CreateRectObject("CharacterRemoveConfirmPage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _timelinePage = CreateRectObject("TimelinePage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _mmdPage = CreateRectObject("MmdPage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _mmdPlaybackPage = CreateRectObject("MmdPlaybackPage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _mmdCameraPage = CreateRectObject("MmdCameraPage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _mmdCuePage = CreateRectObject("MmdCuePage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _mmdPresetPage = CreateRectObject("MmdPresetPage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _mmdClearConfirmPage = CreateRectObject("MmdClearConfirmPage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _highHeelsPage = CreateRectObject("HighHeelsPage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _settingsPage = CreateRectObject("SettingsPage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);

        CreateText("Title", _rootPage.transform, "KK VR", 24f, 7f, 220f, 31f, 28,
            TextAnchor.MiddleLeft, PrimaryTextColor);
        CreateText("TitleCaption", _rootPage.transform,
            L("工作室快捷控制", "スタジオクイック操作", "Studio quick controls"), 25f, 35f, 260f, 15f, 11,
            TextAnchor.MiddleLeft, TertiaryTextColor);
        CreateButton(
            "OpenSettings",
            L("设置  ›", "設定  ›", "Settings  ›"),
            418f,
            12f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            HandleOpenSettings,
            _rootPage.transform,
            118f,
            34f,
            17);

        CreateText("SceneSection", _rootPage.transform, L("场景", "シーン", "Scene"), 24f, 68f, 512f, 24f, 20,
            TextAnchor.MiddleLeft, new Color(0.25f, 0.86f, 0.94f, 1f));
        CreateButton(
            "LoadScene",
            L("载入场景  ›\n浏览并打开场景卡", "シーンを読込  ›\nシーンカードを選択", "Load scene  ›\nBrowse and open a scene card"),
            24f,
            98f,
            new Color(0.07f, 0.21f, 0.25f, 0.5f),
            new Color(0.09f, 0.42f, 0.49f, 0.78f),
            HandleLoadScene,
            _rootPage.transform,
            248f,
            68f,
            20);
        CreateButton(
            "SaveScene",
            L("保存场景  ›\n新建场景卡", "シーンを保存  ›\n新しいカードを作成", "Save scene  ›\nCreate a new scene card"),
            288f,
            98f,
            new Color(0.07f, 0.21f, 0.25f, 0.5f),
            new Color(0.09f, 0.42f, 0.49f, 0.78f),
            HandleSaveScene,
            _rootPage.transform,
            248f,
            68f,
            20);

        CreateText("MmdSection", _rootPage.transform, "MMD", 24f, 178f, 512f, 24f, 20,
            TextAnchor.MiddleLeft, new Color(1f, 0.72f, 0.25f, 1f));
        _rootLoadVmdButton = CreateButton(
            "QuickLoadVmd",
            L("载入动作  ›\n选择 VMD 文件", "動作を読込  ›\nVMDを選択", "Load motion  ›\nChoose VMD"),
            24f,
            208f,
            VmdRootMissingColor,
            VmdRootMissingHoverColor,
            HandleLoadVmd,
            _rootPage.transform,
            160f,
            68f,
            16);
        CreateButton(
            "OpenMmdSettings",
            L("MMD 工具  ›\n播放与镜头设置", "MMD ツール  ›\n再生・カメラ設定", "MMD tools  ›\nPlayback and camera"),
            200f,
            208f,
            new Color(0.28f, 0.2f, 0.07f, 0.46f),
            new Color(0.55f, 0.36f, 0.1f, 0.74f),
            HandleOpenMmdSettings,
            _rootPage.transform,
            160f,
            68f,
            16);
        CreateButton(
            "OpenTimeline",
            L("Timeline  ›\n播放与镜头", "Timeline  ›\n再生・カメラ", "Timeline  ›\nPlayback and camera"),
            376f,
            208f,
            new Color(0.12f, 0.17f, 0.3f, 0.5f),
            new Color(0.18f, 0.36f, 0.62f, 0.8f),
            HandleOpenTimeline,
            _rootPage.transform,
            160f,
            68f,
            16);

        CreateText("CharacterSection", _rootPage.transform, L("角色", "キャラクター", "Character"), 24f, 290f, 512f, 24f, 20,
            TextAnchor.MiddleLeft, new Color(0.47f, 0.9f, 0.55f, 1f));
        CreateButton(
            "OpenCharacterCards",
            L("角色卡  ›\n添加或替换角色", "キャラカード  ›\n追加・置き換え", "Character cards  ›\nAdd or replace a character"),
            24f,
            320f,
            new Color(0.075f, 0.24f, 0.14f, 0.48f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            HandleOpenCharacterCards,
            _rootPage.transform,
            160f,
            62f,
            17);
        CreateButton(
            "OpenClothing",
            L("换装  ›\n预设服装与穿脱状态", "着替え  ›\n衣装プリセット・着脱", "Outfits  ›\nPresets and dress states"),
            200f,
            320f,
            new Color(0.075f, 0.24f, 0.14f, 0.48f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            HandleOpenClothing,
            _rootPage.transform,
            160f,
            62f,
            17);
        CreateButton(
            "ToggleIkVisibility",
            L("IK 控制器\n显示或隐藏辅助点", "IK コントローラー\n補助点を表示・非表示", "IK controllers\nShow or hide helper points"),
            376f,
            320f,
            new Color(0.075f, 0.24f, 0.14f, 0.48f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            HandleToggleIkVisibility,
            _rootPage.transform,
            160f,
            62f,
            17);

        BuildClothingPage();
        BuildClothingPartsPage();
        BuildClothingOpacityPage();
        BuildClothingOpacityQuickPage();
        BuildCoordinatePage();
        BuildBrowserPage();
        BuildSceneSavePage();
        BuildCharacterCardsPage();
        BuildCharacterPreviewPage();
        BuildCharacterRemoveConfirmPage();
        BuildTimelinePage();
        BuildMmdSettingsPage();
        BuildMmdPlaybackPage();
        BuildMmdCameraPage();
        BuildMmdCuePage();
        BuildMmdPresetPage();
        BuildMmdClearConfirmPage();
        BuildHighHeelsPage();
        BuildSettingsPage();

        Image statusBackground = CreateImage("StatusBackground", _menuRect, 24f, 416f, 512f, 58f, GlassSurfaceColor);
        ApplyGlassEffects(statusBackground, new Color(0.82f, 0.9f, 1f, 0.1f), true, 2f);
        _statusIndicator = CreateImage(
            "StatusIndicator",
            statusBackground.transform,
            16f,
            24f,
            8f,
            8f,
            new Color(0.42f, 0.82f, 1f, 1f));
        _statusText = CreateText("Status", statusBackground.transform, L("就绪", "準備完了", "Ready"), 36f, 5f, 462f, 48f, 17,
            TextAnchor.MiddleLeft, SecondaryTextColor);
        CreateImage("FooterDivider", _menuRect, 338f, 484f, 54f, 1f, new Color(0.72f, 0.84f, 1f, 0.1f), false);
        CreateText("AuthorCaption", _menuRect, L("作者", "制作", "Designed by"), 400f, 477f, 72f, 17f, 9,
            TextAnchor.MiddleRight, TertiaryTextColor);
        CreateText("AuthorMark", _menuRect, "ERMIN", 478f, 477f, 58f, 17f, 12,
            TextAnchor.MiddleRight, SecondaryTextColor);

        _clothingPage.SetActive(false);
        _clothingPartsPage.SetActive(false);
        _clothingOpacityPage.SetActive(false);
        _clothingOpacityQuickPage.SetActive(false);
        _coordinatePage.SetActive(false);
        _browserPage.SetActive(false);
        _sceneSavePage.SetActive(false);
        _characterCardsPage.SetActive(false);
        _characterPreviewPage.SetActive(false);
        _characterRemoveConfirmPage.SetActive(false);
        _timelinePage.SetActive(false);
        _mmdPage.SetActive(false);
        _mmdPlaybackPage.SetActive(false);
        _mmdCameraPage.SetActive(false);
        _mmdCuePage.SetActive(false);
        _mmdPresetPage.SetActive(false);
        _mmdClearConfirmPage.SetActive(false);
        _highHeelsPage.SetActive(false);
        _settingsPage.SetActive(false);
        _menuRoot.SetActive(false);
        RefreshTimelineButton();
        return true;
    }

    private void BuildClothingPage()
    {
        CreateButton(
            "ClothingBack",
            "<",
            18f,
            10f,
            new Color(1f, 1f, 1f, 0.08f),
            new Color(1f, 1f, 1f, 0.2f),
            HandleBackToRoot,
            _clothingPage.transform,
            58f,
            38f,
            28);
        CreateText("ClothingTitle", _clothingPage.transform, L("换装", "着替え", "Outfits"), 92f, 10f, 250f, 40f, 28,
            TextAnchor.MiddleLeft, Color.white);
        CreateButton(
            "OpenCoordinateCards",
            L("服装卡  ›", "衣装カード  ›", "Outfit cards  ›"),
            358f,
            10f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            HandleOpenCoordinateCards,
            _clothingPage.transform,
            178f,
            38f,
            16);

        _clothingTargetButton = CreateButton(
            "ClothingTarget",
            L("选择场景角色  >", "シーンのキャラを選択  >", "Select a scene character  >"),
            24f,
            64f,
            new Color(0.075f, 0.24f, 0.14f, 0.48f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            HandleChooseClothingTarget,
            _clothingPage.transform,
            512f,
            36f,
            17);

        _coordinatePresetButton = CreateButton(
            "OpenCoordinatePresets",
            L(
                "角色预设服装  ›\n校服、便服、泳装等",
                "キャラ衣装プリセット  ›\n制服・私服・水着など",
                "Character outfit presets  ›\nSchool, casual, swimwear, and more"),
            24f,
            106f,
            new Color(0.075f, 0.24f, 0.14f, 0.48f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            HandleOpenCoordinatePresets,
            _clothingPage.transform,
            344f,
            48f,
            16);
        CreateButton(
            "OpenClothingOpacity",
            L(
                "服装透明度  ›\n按部位调整",
                "衣装の透明度  ›\n部位ごとに調整",
                "Clothing transparency  ›\nAdjust by part"),
            376f,
            106f,
            new Color(0.12f, 0.17f, 0.3f, 0.5f),
            new Color(0.18f, 0.36f, 0.62f, 0.8f),
            HandleOpenClothingOpacity,
            _clothingPage.transform,
            160f,
            48f,
            14);

        BuildClothingScrollViewport();

        CreateText("ClothingPresetSection", _clothingScrollContent, L("穿脱状态", "着脱状態", "Dress states"), 24f, 0f, 512f, 20f, 17,
            TextAnchor.MiddleLeft, new Color(0.47f, 0.9f, 0.55f, 1f));
        CreateButton(
            "ClothingDressed",
            L("全部穿好", "すべて着る", "Fully dressed"),
            24f,
            24f,
            new Color(0.075f, 0.24f, 0.14f, 0.48f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            () => HandleClothingPreset(0),
            _clothingScrollContent,
            165f,
            40f,
            16);
        CreateButton(
            "ClothingHalf",
            L("半脱状态", "半脱ぎ", "Half dressed"),
            198f,
            24f,
            new Color(0.28f, 0.2f, 0.07f, 0.46f),
            new Color(0.55f, 0.36f, 0.1f, 0.74f),
            () => HandleClothingPreset(1),
            _clothingScrollContent,
            164f,
            40f,
            16);
        CreateButton(
            "ClothingUndressed",
            L("全部脱下", "すべて脱ぐ", "Undressed"),
            371f,
            24f,
            new Color(0.3f, 0.09f, 0.12f, 0.46f),
            new Color(0.58f, 0.15f, 0.2f, 0.74f),
            () => HandleClothingPreset(3),
            _clothingScrollContent,
            165f,
            40f,
            16);

        CreateText("ClothingPartsSection", _clothingScrollContent, L("精细调整", "詳細調整", "Fine controls"), 24f, 78f, 512f, 20f, 17,
            TextAnchor.MiddleLeft, new Color(0.25f, 0.86f, 0.94f, 1f));
    }

    private void EnsureGlassAssets()
    {
        if (_roundedSprite != null)
            return;

        const int size = 48;
        const float radius = 8f;
        const float border = 12f;
        float half = size * 0.5f;
        float inner = half - radius;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - half) - inner;
                float qy = Mathf.Abs(y + 0.5f - half) - inner;
                float ox = Mathf.Max(qx, 0f);
                float oy = Mathf.Max(qy, 0f);
                float distance = Mathf.Sqrt(ox * ox + oy * oy)
                    + Mathf.Min(Mathf.Max(qx, qy), 0f)
                    - radius;
                float alpha = Mathf.Clamp01(0.5f - distance);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        _roundedTexture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        _roundedTexture.name = "KKVR_GlassRoundedTexture";
        _roundedTexture.filterMode = FilterMode.Bilinear;
        _roundedTexture.wrapMode = TextureWrapMode.Clamp;
        _roundedTexture.hideFlags = HideFlags.HideAndDontSave;
        _roundedTexture.SetPixels(pixels);
        _roundedTexture.Apply(false, true);

        _roundedSprite = Sprite.Create(
            _roundedTexture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(border, border, border, border));
        _roundedSprite.name = "KKVR_GlassRoundedSprite";
        _roundedSprite.hideFlags = HideFlags.HideAndDontSave;

        const int cursorSize = 32;
        float cursorCenter = (cursorSize - 1f) * 0.5f;
        float cursorRadius = cursorSize * 0.5f - 1f;
        Color[] cursorPixels = new Color[cursorSize * cursorSize];
        for (int y = 0; y < cursorSize; y++)
        {
            for (int x = 0; x < cursorSize; x++)
            {
                float dx = x - cursorCenter;
                float dy = y - cursorCenter;
                float alpha = Mathf.Clamp01(cursorRadius + 0.5f - Mathf.Sqrt(dx * dx + dy * dy));
                cursorPixels[y * cursorSize + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        _cursorTexture = new Texture2D(cursorSize, cursorSize, TextureFormat.ARGB32, false);
        _cursorTexture.name = "KKVR_SurfaceCursorTexture";
        _cursorTexture.filterMode = FilterMode.Bilinear;
        _cursorTexture.wrapMode = TextureWrapMode.Clamp;
        _cursorTexture.hideFlags = HideFlags.HideAndDontSave;
        _cursorTexture.SetPixels(cursorPixels);
        _cursorTexture.Apply(false, true);

        _cursorSprite = Sprite.Create(
            _cursorTexture,
            new Rect(0f, 0f, cursorSize, cursorSize),
            new Vector2(0.5f, 0.5f),
            100f);
        _cursorSprite.name = "KKVR_SurfaceCursorSprite";
        _cursorSprite.hideFlags = HideFlags.HideAndDontSave;
    }

    private Image CreateImage(
        string name,
        Transform parent,
        float x,
        float y,
        float width,
        float height,
        Color color,
        bool rounded = true)
    {
        GameObject go = CreateRectObject(name, parent, x, y, width, height, _visibleLayer);
        Image image = go.AddComponent<Image>();
        if (rounded && _roundedSprite != null)
        {
            image.sprite = _roundedSprite;
            image.type = Image.Type.Sliced;
        }
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void ApplyGlassEffects(
        Image image,
        Color outlineColor,
        bool elevated,
        float shadowOffset)
    {
        if (image == null)
            return;

        if (elevated)
        {
            Shadow shadow = image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = shadowOffset >= 4f
                ? GlassShadowColor
                : new Color(0f, 0f, 0f, 0.26f);
            shadow.effectDistance = new Vector2(0f, -shadowOffset);
            shadow.useGraphicAlpha = true;
        }

        Outline outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = true;
    }

    private static void ApplyControlEffects(Image image)
    {
        if (image == null)
            return;

        Shadow shadow = image.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.2f);
        shadow.effectDistance = new Vector2(0f, -1.5f);
        shadow.useGraphicAlpha = true;

        Outline outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.82f, 0.9f, 1f, 0.1f);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = true;
    }

    private Text CreateText(
        string name,
        Transform parent,
        string value,
        float x,
        float y,
        float width,
        float height,
        int fontSize,
        TextAnchor alignment,
        Color color)
    {
        GameObject go = CreateRectObject(name, parent, x, y, width, height, _visibleLayer);
        Text text = go.AddComponent<Text>();
        bool emphasized = name.IndexOf("Title", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Section", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Heading", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("AuthorMark", StringComparison.OrdinalIgnoreCase) >= 0
            || fontSize >= 24;
        text.font = emphasized && _emphasizedFont != null ? _emphasizedFont : _font;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.lineSpacing = 0.95f;
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Math.Min(12, fontSize);
        text.resizeTextMaxSize = fontSize;
        text.raycastTarget = false;

        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
        shadow.effectDistance = new Vector2(0f, -0.75f);
        shadow.useGraphicAlpha = true;
        return text;
    }

    private VRWristMenuButtonTarget CreateButton(
        string name,
        string label,
        float x,
        float y,
        Color normalColor,
        Color hoverColor,
        Action onClick,
        Transform parent = null,
        float width = 248f,
        float height = 78f,
        int fontSize = 21)
    {
        Transform buttonParent = parent ?? _menuRect;
        if (label == "<")
            label = "‹";
        else
            label = label.Replace(">", "›");

        Image background = CreateImage(name, buttonParent, x, y, width, height, normalColor);
        ApplyControlEffects(background);
        CreateImage(
            name + "Glint",
            background.transform,
            10f,
            5f,
            width - 20f,
            1f,
            new Color(1f, 1f, 1f, 0.08f),
            false);
        Text buttonText = CreateText(name + "Label", background.transform, label, 10f, 5f, width - 20f, height - 10f, fontSize,
            TextAnchor.MiddleCenter, Color.white);

        GameObject hitbox = new GameObject(name + "Hitbox");
        hitbox.layer = _colliderLayer;
        hitbox.transform.SetParent(background.transform, false);
        hitbox.transform.localPosition = new Vector3(width * 0.5f, -height * 0.5f, 0f);
        hitbox.transform.localRotation = Quaternion.identity;
        hitbox.transform.localScale = Vector3.one;

        BoxCollider collider = hitbox.AddComponent<BoxCollider>();
        collider.size = new Vector3(width, height, 12f);
        collider.isTrigger = true;

        VRWristMenuButtonTarget target = hitbox.AddComponent<VRWristMenuButtonTarget>();
        target.Configure(background, buttonText, normalColor, hoverColor, onClick);
        return target;
    }

    private static GameObject CreateRectObject(
        string name,
        Transform parent,
        float x,
        float y,
        float width,
        float height,
        int layer)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = layer;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        return go;
    }

    private void UpdatePose()
    {
        Transform left = ((Component)VR.Mode.Left).transform;
        Transform head = VR.Camera.Head;
        float configuredScale = _settings != null ? Mathf.Clamp(_settings.WristMenuScale, 0.7f, 1.5f) : 1f;
        Vector3 targetPosition = left.position + head.up * 0.105f + head.forward * 0.035f;
        Quaternion targetRotation = Quaternion.LookRotation(head.forward, head.up);

        _menuRect.localScale = Vector3.one * (BaseMenuScale * configuredScale);
        if (!_poseInitialized)
        {
            _menuRect.position = targetPosition;
            _menuRect.rotation = targetRotation;
            _poseInitialized = true;
            return;
        }

        float blend = Mathf.Clamp01(Time.unscaledDeltaTime * 20f);
        _menuRect.position = Vector3.Lerp(_menuRect.position, targetPosition, blend);
        _menuRect.rotation = Quaternion.Slerp(_menuRect.rotation, targetRotation, blend);
    }

    private void UpdatePointer()
    {
        SteamVR_Controller.Device rightDevice = GetDevice(VR.Mode?.Right);
        if (rightDevice == null)
        {
            SetHoveredButton(null);
            SetPointerVisible(false);
            return;
        }
        if (!EnsurePointer())
        {
            VRLog.Error("Wrist menu closed after its controller pointer became unavailable.");
            SetOpen(false);
            return;
        }

        UpdateBrowserStickScroll(rightDevice);
        Vector3 origin = _laser.transform.position;
        Vector3 direction = _laser.transform.forward;
        Ray pointerRay = new Ray(origin, direction);
        Vector3 end = origin + direction * 2f;
        VRWristMenuButtonTarget target = null;
        float nearest = float.MaxValue;
        Vector3 menuSurfacePoint;
        float menuSurfaceDistance;
        bool hasMenuSurfaceHit = TryGetMenuSurfaceHit(
            pointerRay,
            2f,
            out menuSurfacePoint,
            out menuSurfaceDistance);
        if (hasMenuSurfaceHit)
            end = menuSurfacePoint;
        UpdateClothingScroll(
            rightDevice,
            hasMenuSurfaceHit && IsPointInsideClothingScrollViewport(menuSurfacePoint));

        RaycastHit[] hits = Physics.RaycastAll(
            pointerRay,
            2f,
            _pointerMask,
            QueryTriggerInteraction.Collide);
        foreach (RaycastHit hit in hits)
        {
            VRWristMenuButtonTarget candidate = hit.collider.GetComponent<VRWristMenuButtonTarget>();
            if (candidate != null
                && candidate.IsInteractable
                && hit.distance < nearest
                && IsMenuButtonHitVisible(candidate, hit.point)
                && (!hasMenuSurfaceHit || hit.distance <= menuSurfaceDistance + 0.01f))
            {
                nearest = hit.distance;
                target = candidate;
                end = hit.point;
            }
        }

        if (target != _hoveredButton && target != null)
            rightDevice.TriggerHapticPulse(140, EVRButtonId.k_EButton_Axis0);
        SetHoveredButton(target);
        SetPointerVisible(true);

        Color pointerColor = target != null
            ? new Color(0.25f, 1f, 0.82f, 1f)
            : new Color(0.2f, 0.78f, 0.95f, 1f);
        _laser.SetPosition(0, origin);
        _laser.SetPosition(1, end);
        _laser.SetColors(pointerColor, pointerColor);
        UpdateSurfaceCursor(
            hasMenuSurfaceHit || target != null,
            hasMenuSurfaceHit ? menuSurfacePoint : end,
            pointerColor);

        if (target != null && rightDevice.GetPressDown(EVRButtonId.k_EButton_Axis1))
        {
            rightDevice.TriggerHapticPulse(800, EVRButtonId.k_EButton_Axis0);
            if (_operationInProgress)
            {
                SetStatus(
                    L(
                        "操作仍在进行，请稍候",
                        "処理中です。しばらくお待ちください",
                        "An operation is still running. Please wait"),
                    new Color(1f, 0.72f, 0.25f, 1f),
                    4f);
                return;
            }
            target.Activate();
        }
    }

    private bool EnsurePointer()
    {
        if (VR.Mode == null || VR.Mode.Right == null)
            return false;

        Transform right = ((Component)VR.Mode.Right).transform;
        if (_laser == null)
        {
            try
            {
                GameObject laserObject = new GameObject("KKVR_WristMenuLaser");
                laserObject.SetActive(false);
                laserObject.layer = _visibleLayer;
                _laser = laserObject.AddComponent<LineRenderer>();
                _laser.useWorldSpace = true;
                _laser.SetVertexCount(2);
                _laser.SetWidth(0.0025f, 0.0025f);
                _laserMaterial = VRPointerVisuals.CreateMaterial(
                    "KKVR Wrist Menu Laser",
                    new Color(0.2f, 0.78f, 0.95f, 1f));
                if (_laserMaterial == null)
                    throw new InvalidOperationException("No pointer shader is available.");
                _laser.sharedMaterial = _laserMaterial;
            }
            catch (Exception ex)
            {
                VRLog.Error("Failed to initialize wrist pointer: " + ex.Message);
                DestroyPointerVisuals();
                return false;
            }
        }

        if (!EnsureSurfaceCursor())
            return false;

        if (_rightTransform != right)
        {
            _rightTransform = right;
            _laser.transform.SetParent(right, false);
            Quaternion pointerRotation = Quaternion.Euler(60f, 0f, 0f);
            _laser.transform.localRotation = pointerRotation;
            _laser.transform.localPosition = pointerRotation * Vector3.forward * 0.07f;
        }

        return true;
    }

    private bool EnsureSurfaceCursor()
    {
        if (_surfaceCursor != null)
            return true;
        if (_menuRect == null || _cursorSprite == null)
            return false;

        _surfaceCursor = CreateImage(
            "PointerReticle",
            _menuRect,
            0f,
            0f,
            20f,
            20f,
            new Color(0.2f, 0.78f, 0.95f, 0.98f),
            false);
        _surfaceCursor.sprite = _cursorSprite;
        _surfaceCursor.type = Image.Type.Simple;
        RectTransform cursorRect = _surfaceCursor.rectTransform;
        cursorRect.anchorMin = _menuRect.pivot;
        cursorRect.anchorMax = _menuRect.pivot;
        cursorRect.pivot = new Vector2(0.5f, 0.5f);
        cursorRect.anchoredPosition = Vector2.zero;

        _surfaceCursorInner = CreateImage(
            "PointerReticleInner",
            _surfaceCursor.transform,
            5f,
            5f,
            10f,
            10f,
            new Color(0.018f, 0.026f, 0.04f, 0.96f),
            false);
        _surfaceCursorInner.sprite = _cursorSprite;
        _surfaceCursorInner.type = Image.Type.Simple;
        _surfaceCursorDot = CreateImage(
            "PointerReticleDot",
            _surfaceCursorInner.transform,
            3f,
            3f,
            4f,
            4f,
            Color.white,
            false);
        _surfaceCursorDot.sprite = _cursorSprite;
        _surfaceCursorDot.type = Image.Type.Simple;
        _surfaceCursor.transform.SetAsLastSibling();
        _surfaceCursor.gameObject.SetActive(false);
        return true;
    }

    private void UpdateSurfaceCursor(bool visible, Vector3 worldPoint, Color color)
    {
        if (_surfaceCursor == null)
            return;

        _surfaceCursor.gameObject.SetActive(visible);
        if (!visible)
            return;

        Vector3 localPoint = _menuRect.InverseTransformPoint(worldPoint);
        _surfaceCursor.rectTransform.anchoredPosition = new Vector2(localPoint.x, localPoint.y);
        _surfaceCursor.color = color;
        _surfaceCursor.transform.SetAsLastSibling();
    }

    private void DestroyPointerVisuals()
    {
        if (_laser != null)
        {
            Destroy(_laser.gameObject);
            _laser = null;
        }
        if (_surfaceCursor != null)
        {
            Destroy(_surfaceCursor.gameObject);
            _surfaceCursor = null;
        }
        _surfaceCursorInner = null;
        _surfaceCursorDot = null;
        _rightTransform = null;
        VRPointerVisuals.DestroyMaterial(ref _laserMaterial);
    }

    private void SetPointerVisible(bool visible)
    {
        if (_laser != null)
            _laser.gameObject.SetActive(visible);
        if (_surfaceCursor != null)
            _surfaceCursor.gameObject.SetActive(false);
    }

    private void SetHoveredButton(VRWristMenuButtonTarget target)
    {
        if (_hoveredButton == target)
            return;
        if (_hoveredButton != null)
            _hoveredButton.SetHovered(false);
        _hoveredButton = target;
        if (_hoveredButton != null)
            _hoveredButton.SetHovered(true);
    }

    private void ShowPage(WristMenuPage page)
    {
        if (_page == WristMenuPage.MmdClearConfirm
            && page != WristMenuPage.MmdClearConfirm)
        {
            ClearPendingMmdClearConfirmation();
        }
        if (_page == WristMenuPage.CharacterRemoveConfirm
            && page != WristMenuPage.CharacterRemoveConfirm)
        {
            ClearPendingCharacterRemovalConfirmation();
        }

        _page = page;
        if (IsStablePage(page))
            _lastStablePage = page;
        SetHoveredButton(null);

        if (_rootPage != null)
            _rootPage.SetActive(page == WristMenuPage.Root);
        if (_clothingPage != null)
            _clothingPage.SetActive(page == WristMenuPage.Clothing);
        if (_clothingPartsPage != null)
            _clothingPartsPage.SetActive(page == WristMenuPage.ClothingParts);
        if (_clothingOpacityPage != null)
            _clothingOpacityPage.SetActive(page == WristMenuPage.ClothingOpacity);
        if (_clothingOpacityQuickPage != null)
            _clothingOpacityQuickPage.SetActive(page == WristMenuPage.ClothingOpacityQuick);
        if (_coordinatePage != null)
            _coordinatePage.SetActive(page == WristMenuPage.Coordinate);
        if (_browserPage != null)
            _browserPage.SetActive(page == WristMenuPage.Browser);
        if (_sceneSavePage != null)
            _sceneSavePage.SetActive(page == WristMenuPage.SceneSave);
        if (_characterCardsPage != null)
            _characterCardsPage.SetActive(page == WristMenuPage.CharacterCards);
        if (_characterPreviewPage != null)
            _characterPreviewPage.SetActive(page == WristMenuPage.CharacterPreview);
        if (_characterRemoveConfirmPage != null)
            _characterRemoveConfirmPage.SetActive(page == WristMenuPage.CharacterRemoveConfirm);
        if (_timelinePage != null)
            _timelinePage.SetActive(page == WristMenuPage.Timeline);
        if (_mmdPage != null)
            _mmdPage.SetActive(page == WristMenuPage.MmdSettings);
        if (_mmdPlaybackPage != null)
            _mmdPlaybackPage.SetActive(page == WristMenuPage.MmdPlayback);
        if (_mmdCameraPage != null)
            _mmdCameraPage.SetActive(page == WristMenuPage.MmdCamera);
        if (_mmdCuePage != null)
            _mmdCuePage.SetActive(page == WristMenuPage.MmdCues);
        if (_mmdPresetPage != null)
            _mmdPresetPage.SetActive(page == WristMenuPage.MmdCuePresets);
        if (_mmdClearConfirmPage != null)
            _mmdClearConfirmPage.SetActive(page == WristMenuPage.MmdClearConfirm);
        if (_highHeelsPage != null)
            _highHeelsPage.SetActive(page == WristMenuPage.HighHeels);
        if (_settingsPage != null)
            _settingsPage.SetActive(page == WristMenuPage.Settings);

        if (page == WristMenuPage.Root)
        {
            RefreshVmdRootVisuals();
        }
        else if (page == WristMenuPage.Clothing)
        {
            RefreshClothingPage();
            _nextClothingRefresh = Time.unscaledTime + 0.25f;
        }
        else if (page == WristMenuPage.ClothingParts)
        {
            RefreshClothingPartsPage();
            _nextClothingRefresh = Time.unscaledTime + 0.25f;
        }
        else if (page == WristMenuPage.ClothingOpacity)
        {
            RefreshClothingOpacityPage();
            _nextClothingRefresh = Time.unscaledTime + 0.25f;
        }
        else if (page == WristMenuPage.ClothingOpacityQuick)
        {
            RefreshClothingOpacityPage();
            _nextClothingRefresh = Time.unscaledTime + 0.25f;
        }
        else if (page == WristMenuPage.Coordinate)
        {
            RefreshCoordinatePage();
            _nextClothingRefresh = Time.unscaledTime + 0.25f;
        }
        else if (page == WristMenuPage.Timeline)
        {
            RefreshTimelinePage();
            _nextTimelineRefresh = Time.unscaledTime + 0.25f;
        }
        else if (page == WristMenuPage.MmdSettings)
        {
            RefreshMmdSettingsPage();
        }
        else if (page == WristMenuPage.MmdPlayback)
        {
            RefreshMmdPlaybackPage();
        }
        else if (page == WristMenuPage.MmdCamera)
        {
            RefreshMmdCameraPage();
        }
        else if (page == WristMenuPage.HighHeels)
        {
            RefreshHighHeelsPage();
        }
        else if (page == WristMenuPage.MmdCues)
        {
            RefreshMmdCuePage();
        }
        else if (page == WristMenuPage.MmdCuePresets)
        {
            RefreshMmdPresetPage();
        }
        else if (page == WristMenuPage.MmdClearConfirm)
        {
            RefreshMmdClearConfirmPage();
        }
        else if (page == WristMenuPage.CharacterRemoveConfirm)
        {
            RefreshCharacterRemoveConfirmPage();
        }
        else if (page == WristMenuPage.Settings)
        {
            RefreshSettingsPage();
        }
    }

    private static bool IsStablePage(WristMenuPage page)
    {
        return page == WristMenuPage.Root
            || page == WristMenuPage.Clothing
            || page == WristMenuPage.ClothingOpacity
            || page == WristMenuPage.ClothingOpacityQuick
            || page == WristMenuPage.Coordinate
            || page == WristMenuPage.CharacterCards
            || page == WristMenuPage.Timeline
            || page == WristMenuPage.MmdSettings
            || page == WristMenuPage.MmdPlayback
            || page == WristMenuPage.MmdCamera
            || page == WristMenuPage.MmdCues
            || page == WristMenuPage.MmdCuePresets
            || page == WristMenuPage.HighHeels
            || page == WristMenuPage.Settings;
    }

    private bool TryGetMenuSurfaceHit(
        Ray ray,
        float maxDistance,
        out Vector3 hitPoint,
        out float hitDistance)
    {
        hitPoint = Vector3.zero;
        hitDistance = 0f;
        if (_menuRect == null || !_menuRect.gameObject.activeInHierarchy)
            return false;

        Plane menuPlane = new Plane(_menuRect.forward, _menuRect.position);
        if (!menuPlane.Raycast(ray, out hitDistance)
            || hitDistance < 0f
            || hitDistance > maxDistance)
        {
            return false;
        }

        hitPoint = ray.GetPoint(hitDistance);
        Vector3 localPoint = _menuRect.InverseTransformPoint(hitPoint);
        return _menuRect.rect.Contains(new Vector2(localPoint.x, localPoint.y));
    }

    private void RefreshClothingPage()
    {
        if (_clothingTargetButton == null)
            return;

        Studio.OCIChar character;
        string selectionStatus;
        bool hasCharacter = TryGetClothingTarget(out character, out selectionStatus);
        _clothingTargetButton.SetLabel(
            hasCharacter
                ? L("当前角色  ", "現在のキャラ  ", "Current character  ") + selectionStatus + "  >"
                : L("选择场景角色  >", "シーンのキャラを選択  >", "Select a scene character  >"));

        if (_coordinatePresetButton != null)
        {
            int coordinateIndex = hasCharacter
                ? VRCharacterClothingService.GetCurrentCoordinateIndex(character)
                : -1;
            _coordinatePresetButton.SetLabel(
                coordinateIndex >= 0
                    ? L("角色预设服装  ›\n当前：", "キャラ衣装プリセット  ›\n現在：", "Character outfit presets  ›\nCurrent: ")
                        + GetCoordinatePresetLabel(coordinateIndex, character)
                    : L(
                        "角色预设服装  ›\n校服、便服、泳装等",
                        "キャラ衣装プリセット  ›\n制服・私服・水着など",
                        "Character outfit presets  ›\nSchool, casual, swimwear, and more"));
        }

        RefreshClothingPartsPage();
    }

    private void HandleOpenClothing()
    {
        HandleChooseClothingTarget();
    }

    private void HandleChooseClothingTarget()
    {
        string status;
        var targets = VRVmdTargetService.GetAllTargets(out status);
        if (targets.Count == 0)
        {
            SetStatus(
                status ?? "当前场景没有角色",
                new Color(1f, 0.38f, 0.34f, 1f),
                7f);
            return;
        }

        OpenActorTargetBrowser(
            BrowserMode.SelectClothingActor,
            targets,
            L("请选择要换装的场景角色", "服装を変更するキャラを選択", "Select the character whose clothing you want to change"),
            out status);
    }

    private void HandleClothingActorSelection(VRWristFileEntry entry)
    {
        if (!entry.IsActorTarget)
            return;

        VRVmdActorTarget target;
        string status;
        if (!VRVmdTargetService.TryGetTarget(entry.ObjectKey, out target, out status))
        {
            SetStatus(status, new Color(1f, 0.38f, 0.34f, 1f), 7f);
            return;
        }

        _clothingTargetObjectKey = entry.ObjectKey;
        _clothingTargetCharacter = target.Character;
        SetStatus(L("服装目标：", "服装対象：", "Clothing target: ") + target.DisplayName,
            new Color(0.35f, 1f, 0.62f, 1f), 0f);
        ShowPage(WristMenuPage.Clothing);
    }

    private bool TryGetClothingTarget(out Studio.OCIChar character, out string status)
    {
        character = null;
        if (_clothingTargetObjectKey < 0)
        {
            status = "请先在手环中选择场景角色";
            return false;
        }

        VRVmdActorTarget target;
        if (VRVmdTargetService.TryGetTarget(
                _clothingTargetObjectKey,
                out target,
                out status)
            && object.ReferenceEquals(_clothingTargetCharacter, target.Character))
        {
            character = target.Character;
            status = target.DisplayName;
            return true;
        }

        _clothingTargetObjectKey = -1;
        _clothingTargetCharacter = null;
        status = "场景角色已变化，请重新选择换装目标";
        return false;
    }

    private void HandleToggleIkVisibility()
    {
        string status;
        bool success;
        if (VRQuickActions.Instance == null)
        {
            status = "IK 控制器尚未初始化";
            success = false;
        }
        else
        {
            success = VRQuickActions.Instance.ToggleIkControls(out status);
        }
        SetStatus(
            status,
            success ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.38f, 0.34f, 1f),
            4f);
    }

    private void HandleBackToRoot()
    {
        ShowPage(WristMenuPage.Root);
        SetStatus(L("就绪", "準備完了", "Ready"), new Color(0.78f, 0.86f, 0.9f, 1f), 0f);
    }

    private void HandleClothingPreset(byte state)
    {
        string status;
        Studio.OCIChar character;
        bool hasTarget = TryGetClothingTarget(out character, out status);
        bool success = hasTarget
            && VRCharacterClothingService.TrySetAll(_clothingTargetObjectKey, state, out status);
        if (success && VRMmdPlaybackController.Instance != null)
            VRMmdPlaybackController.Instance.RequestHighHeelsRefresh(_clothingTargetObjectKey);
        SetStatus(
            status,
            success ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.38f, 0.34f, 1f),
            4f);
        RefreshClothingPage();
    }

    private void HandleCycleClothingPart(int partId)
    {
        string status;
        Studio.OCIChar character;
        bool hasTarget = TryGetClothingTarget(out character, out status);
        bool success = hasTarget
            && VRCharacterClothingService.TryCyclePart(
                _clothingTargetObjectKey,
                partId,
                out status);
        if (success && VRMmdPlaybackController.Instance != null)
            VRMmdPlaybackController.Instance.RequestHighHeelsRefresh(_clothingTargetObjectKey);
        SetStatus(
            status,
            success ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.38f, 0.34f, 1f),
            4f);
        RefreshClothingPartsPage();
    }

    private void HandleLoadScene()
    {
        SetOpen(false);
        string status;
        if (VRSceneActions.OpenLoadScene(out status))
        {
            VRLog.Info(status);
            StartCoroutine(SummonMainGuiAfterDelay(0.35f));
            return;
        }

        SetOpen(true);
        SetStatus(status, new Color(1f, 0.38f, 0.34f, 1f), 6f);
    }

    private void HandleSaveScene()
    {
        ShowPage(WristMenuPage.SceneSave);
        SetStatus(L("保存到场景根目录", "シーンのルートフォルダーに保存", "Save to the scene root folder"),
            new Color(0.25f, 0.86f, 0.94f, 1f), 0f);
    }

    private void HandleToggleMmd()
    {
        VRMmdPlaybackController.CancelLeftStickTransportResume();
        string status;
        bool success = VRMmddService.TogglePlayPause(out status);
        SetStatus(
            status,
            success ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.38f, 0.34f, 1f),
            4f);
    }

    private void HandleLoadVmd()
    {
        string root = GetConfiguredVmdRoot();
        if (string.IsNullOrEmpty(root))
        {
            OpenVmdRootPicker(false);
            return;
        }

        OpenFileBrowser(BrowserMode.LoadVmd, root, root);
    }

    private void HandleToggleTimeline()
    {
        string status;
        bool success = VRTimelineService.TogglePlayPause(out status);
        if (success)
        {
            VRTimelineCameraFollowController.ApplyNow();
        }
        RefreshTimelineButton();
        SetStatus(
            status,
            success ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.38f, 0.34f, 1f),
            4f);
    }

    private IEnumerator SummonMainGuiAfterDelay(float delay)
    {
        float deadline = Time.unscaledTime + delay;
        while (Time.unscaledTime < deadline)
            yield return null;
        if (VRQuickActions.Instance != null)
            VRQuickActions.Instance.SummonMainGUI();
    }

    private void UpdateTransientState()
    {
        if (_statusUntil > 0f && Time.unscaledTime > _statusUntil)
            SetStatus(L("就绪", "準備完了", "Ready"), new Color(0.78f, 0.86f, 0.9f, 1f), 0f);
        if ((_page == WristMenuPage.Clothing
                || _page == WristMenuPage.ClothingParts
                || _page == WristMenuPage.ClothingOpacity
                || _page == WristMenuPage.ClothingOpacityQuick
                || _page == WristMenuPage.Coordinate)
            && Time.unscaledTime >= _nextClothingRefresh)
        {
            if (_page == WristMenuPage.Clothing)
                RefreshClothingPage();
            else if (_page == WristMenuPage.ClothingParts)
                RefreshClothingPartsPage();
            else if (_page == WristMenuPage.ClothingOpacity
                || _page == WristMenuPage.ClothingOpacityQuick)
                RefreshClothingOpacityPage();
            else
                RefreshCoordinatePage();
            _nextClothingRefresh = Time.unscaledTime + 0.25f;
        }
        if (_page == WristMenuPage.Timeline && Time.unscaledTime >= _nextTimelineRefresh)
        {
            RefreshTimelinePage();
            _nextTimelineRefresh = Time.unscaledTime + 0.25f;
        }
    }

    private void SetStatus(string message, Color color, float duration)
    {
        if (_statusText == null)
            return;
        _statusText.text = LocalizeStatus(message);
        _statusText.color = color;
        if (_statusIndicator != null)
            _statusIndicator.color = new Color(color.r, color.g, color.b, 1f);
        _statusUntil = duration > 0f ? Time.unscaledTime + duration : 0f;
    }

    private bool HasTrackedMenuControllers()
    {
        return GetDevice(VR.Mode?.Left, true) != null
            && GetDevice(VR.Mode?.Right, true) != null;
    }

    private static SteamVR_Controller.Device GetDevice(
        VRGIN.Controls.Controller controller,
        bool requireTracking = true)
    {
        if (controller == null)
            return null;
        SteamVR_TrackedObject tracked = ((Component)controller).GetComponent<SteamVR_TrackedObject>();
        if (tracked == null || tracked.index == SteamVR_TrackedObject.EIndex.None)
            return null;

        try
        {
            SteamVR_Controller.Device device = SteamVR_Controller.Input((int)tracked.index);
            if (device == null || !device.connected)
                return null;
            if (!requireTracking)
                return device;
            if (controller.IsTracking || tracked.isValid || device.hasTracking)
                return device;

            bool hasFallbackPose = tracked.transform.localPosition.sqrMagnitude >= 0.000001f;
            return hasFallbackPose
                && !device.outOfRange
                && !device.calibrating
                && !device.uninitialized
                ? device
                : null;
        }
        catch (Exception ex)
        {
            VRLog.Error("Unable to read controller state: " + ex.Message);
            return null;
        }
    }

    private Font ResolveFont(bool emphasized, out bool ownsFont)
    {
        ownsFont = false;
        string[] installed = Font.GetOSInstalledFontNames();
        string latin = FindInstalledFont(
            installed,
            emphasized
                ? new[] { "Segoe UI Semibold", "Segoe UI", "Arial" }
                : new[] { "Segoe UI", "Segoe UI Semilight", "Arial" });
        string chinese = FindInstalledFont(
            installed,
            emphasized
                ? new[] { "Microsoft YaHei UI Bold", "Microsoft YaHei Bold", "Microsoft YaHei UI", "Microsoft YaHei" }
                : new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial Unicode MS" });
        string japanese = FindInstalledFont(
            installed,
            emphasized
                ? new[] { "Yu Gothic UI Semibold", "Yu Gothic Bold", "Meiryo UI", "Meiryo", "Noto Sans CJK JP" }
                : new[] { "Yu Gothic UI", "Yu Gothic", "Meiryo UI", "Meiryo", "Noto Sans CJK JP" });

        List<string> fallbacks = new List<string>();
        AddFontFallback(fallbacks, latin);
        if (IsJapanese)
        {
            AddFontFallback(fallbacks, japanese);
            AddFontFallback(fallbacks, chinese);
        }
        else
        {
            AddFontFallback(fallbacks, chinese);
            AddFontFallback(fallbacks, japanese);
        }
        if (fallbacks.Count > 0)
        {
            ownsFont = true;
            return Font.CreateDynamicFontFromOSFont(fallbacks.ToArray(), 32);
        }

        foreach (Font font in Resources.FindObjectsOfTypeAll<Font>())
        {
            if (font != null && font.HasCharacter('场') && font.HasCharacter('あ'))
                return font;
        }

        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static void AddFontFallback(List<string> fallbacks, string name)
    {
        if (string.IsNullOrEmpty(name))
            return;
        foreach (string existing in fallbacks)
        {
            if (string.Equals(existing, name, StringComparison.OrdinalIgnoreCase))
                return;
        }
        fallbacks.Add(name);
    }

    private static string FindInstalledFont(string[] installed, string[] candidates)
    {
        foreach (string candidate in candidates)
        {
            foreach (string name in installed)
            {
                if (string.Equals(name, candidate, StringComparison.OrdinalIgnoreCase))
                    return name;
            }
        }

        return null;
    }
}

internal sealed class VRWristMenuButtonTarget : MonoBehaviour
{
    private Image _background;
    private Text _label;
    private Outline _outline;
    private Color _normalColor;
    private Color _hoverColor;
    private Action _onClick;
    private bool _hovered;
    private bool _interactable = true;

    public void Configure(Image background, Text label, Color normalColor, Color hoverColor, Action onClick)
    {
        _background = background;
        _label = label;
        _outline = background != null ? background.GetComponent<Outline>() : null;
        _normalColor = RefineColor(normalColor, false);
        _hoverColor = RefineColor(hoverColor, true);
        _onClick = onClick;
        SetHovered(false);
    }

    public void SetHovered(bool hovered)
    {
        if (!_interactable)
        {
            _hovered = false;
            if (_background != null)
                _background.color = new Color(0.075f, 0.08f, 0.095f, 0.62f);
            if (_label != null)
                _label.color = new Color(0.48f, 0.5f, 0.54f, 0.72f);
            if (_outline != null)
                _outline.effectColor = new Color(0.3f, 0.32f, 0.36f, 0.08f);
            return;
        }
        _hovered = hovered;
        if (_background != null)
            _background.color = hovered ? _hoverColor : _normalColor;
        if (_label != null)
            _label.color = hovered
                ? Color.white
                : new Color(0.92f, 0.94f, 0.98f, 1f);
        if (_outline != null)
            _outline.effectColor = hovered
                ? new Color(0.74f, 0.86f, 1f, 0.26f)
                : new Color(0.82f, 0.9f, 1f, 0.1f);
    }

    public void SetColors(Color normalColor, Color hoverColor)
    {
        _normalColor = RefineColor(normalColor, false);
        _hoverColor = RefineColor(hoverColor, true);
        SetHovered(_hovered);
    }

    public void SetLabel(string value)
    {
        if (_label != null)
            _label.text = value;
    }

    public void SetVisible(bool visible)
    {
        if (_background != null)
            _background.gameObject.SetActive(visible);
    }

    public void SetInteractable(bool interactable)
    {
        if (_interactable == interactable)
            return;
        _interactable = interactable;
        SetHovered(false);
    }

    public bool IsInteractable => _interactable;

    public void Activate()
    {
        if (_interactable)
            _onClick?.Invoke();
    }

    private static Color RefineColor(Color accent, bool hovered)
    {
        float brightest = Mathf.Max(accent.r, Mathf.Max(accent.g, accent.b));
        Color tint = brightest > 0.001f
            ? new Color(accent.r / brightest, accent.g / brightest, accent.b / brightest, 1f)
            : new Color(0.68f, 0.76f, 0.86f, 1f);
        Color neutral = hovered
            ? new Color(0.075f, 0.09f, 0.12f, 1f)
            : new Color(0.045f, 0.056f, 0.078f, 1f);
        Color result = Color.Lerp(neutral, tint, hovered ? 0.3f : 0.18f);
        result.a = hovered ? 0.94f : 0.86f;
        return result;
    }
}

using System;
using System.Collections;
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

    private static readonly Color GlassPanelColor = new Color(0.035f, 0.045f, 0.055f, 0.82f);
    private static readonly Color GlassHeaderColor = new Color(0.88f, 0.94f, 1f, 0.075f);
    private static readonly Color GlassSurfaceColor = new Color(0.78f, 0.86f, 0.92f, 0.075f);
    private static readonly Color GlassOutlineColor = new Color(0.92f, 0.97f, 1f, 0.2f);
    private static readonly Color GlassShadowColor = new Color(0f, 0f, 0f, 0.48f);

    private enum WristMenuPage
    {
        Root,
        Clothing,
        Browser,
        SceneSave,
        CharacterCards,
        CharacterPreview,
        MmdSettings,
        HighHeels,
        Settings
    }

    public static VRWristMenuController Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance._isOpen;

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
    private VRWristMenuButtonTarget _clothingTargetButton;
    private VRWristMenuButtonTarget _loadVmdButton;
    private Texture2D _roundedTexture;
    private Sprite _roundedSprite;
    private readonly VRWristMenuButtonTarget[] _clothingPartButtons =
        new VRWristMenuButtonTarget[VRCharacterClothingService.PartCount];
    private int _clothingTargetObjectKey = -1;
    private Studio.OCIChar _clothingTargetCharacter;
    private VRWristMenuButtonTarget _hoveredButton;
    private Font _font;
    private LineRenderer _laser;
    private GameObject _cursor;
    private Renderer _cursorRenderer;
    private Material _laserMaterial;
    private Material _cursorMaterial;
    private Transform _rightTransform;
    private int _visibleLayer;
    private int _colliderLayer;
    private int _pointerMask;
    private bool _isOpen;
    private bool _menuPressActive;
    private bool _menuPressChorded;
    private bool _poseInitialized;
    private float _menuPressStarted;
    private float _statusUntil;
    private float _nextClothingRefresh;
    private float _trackingUnavailableSince = -1f;
    private WristMenuPage _page;

    private void Awake()
    {
        Instance = this;
        ResolveSettings();
    }

    private void Update()
    {
        ResolveSettings();
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
        // While open, accept X even if positional tracking is temporarily invalid so
        // the menu can always release its input lock.
        SteamVR_Controller.Device leftDevice = GetDevice(VR.Mode?.Left, !_isOpen);
        if (leftDevice == null)
        {
            _menuPressActive = false;
            return;
        }

        if (leftDevice.GetPressDown(EVRButtonId.k_EButton_A))
        {
            _menuPressActive = true;
            _menuPressChorded = false;
            _menuPressStarted = Time.unscaledTime;
        }

        if (_menuPressActive && leftDevice.GetPress(EVRButtonId.k_EButton_A))
        {
            _menuPressChorded |= leftDevice.GetPress(EVRButtonId.k_EButton_Grip)
                || leftDevice.GetPress(EVRButtonId.k_EButton_Axis1);
        }

        if (!_menuPressActive || !leftDevice.GetPressUp(EVRButtonId.k_EButton_A))
            return;

        float duration = Time.unscaledTime - _menuPressStarted;
        _menuPressActive = false;
        if (!_menuPressChorded && duration <= MenuPressMaxDuration
            && (_settings == null || _settings.WristMenuEnabled))
        {
            ToggleMenu();
            leftDevice.TriggerHapticPulse(500, EVRButtonId.k_EButton_Axis0);
        }
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
            ShowPage(WristMenuPage.Root);
            SetStatus("就绪", new Color(0.78f, 0.86f, 0.9f, 1f), 0f);
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
        _font = ResolveFont();
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

        Image glassPanel = CreateImage("Background", _menuRect, 0f, 0f, MenuWidth, MenuHeight, GlassPanelColor);
        ApplyGlassEffects(glassPanel, GlassOutlineColor, true, 6f);
        Image glassHeader = CreateImage("Header", _menuRect, 0f, 0f, MenuWidth, 58f, GlassHeaderColor);
        ApplyGlassEffects(glassHeader, new Color(1f, 1f, 1f, 0.1f), false, 0f);
        CreateImage("TopGlint", _menuRect, 18f, 7f, 524f, 2f, new Color(1f, 1f, 1f, 0.28f), false);
        CreateImage("HeaderDivider", _menuRect, 24f, 57f, 512f, 1f, new Color(0.72f, 0.9f, 1f, 0.16f), false);

        _rootPage = CreateRectObject("RootPage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _clothingPage = CreateRectObject("ClothingPage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _browserPage = CreateRectObject("BrowserPage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _sceneSavePage = CreateRectObject("SceneSavePage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _characterCardsPage = CreateRectObject("CharacterCardsPage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _characterPreviewPage = CreateRectObject("CharacterPreviewPage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _mmdPage = CreateRectObject("MmdPage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _highHeelsPage = CreateRectObject("HighHeelsPage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);
        _settingsPage = CreateRectObject("SettingsPage", _menuRect, 0f, 0f, MenuWidth, MenuHeight, _visibleLayer);

        CreateText("Title", _rootPage.transform, "KK VR 快捷菜单", 22f, 10f, 382f, 40f, 28,
            TextAnchor.MiddleLeft, Color.white);
        CreateButton(
            "OpenSettings",
            "设置  >",
            418f,
            12f,
            new Color(0.12f, 0.16f, 0.2f, 0.56f),
            new Color(0.22f, 0.34f, 0.42f, 0.8f),
            HandleOpenSettings,
            _rootPage.transform,
            118f,
            34f,
            17);

        CreateText("SceneSection", _rootPage.transform, "场景  SCENE", 24f, 68f, 512f, 24f, 20,
            TextAnchor.MiddleLeft, new Color(0.25f, 0.86f, 0.94f, 1f));
        CreateButton(
            "LoadScene",
            "大屏幕读取  >\nSCENE PREVIEW",
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
            "保存场景  >\nSAVE SCENE",
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
        CreateButton(
            "ToggleMmd",
            "MMDD\n播放 / 暂停",
            24f,
            208f,
            new Color(0.28f, 0.2f, 0.07f, 0.46f),
            new Color(0.55f, 0.36f, 0.1f, 0.74f),
            HandleToggleMmd,
            _rootPage.transform,
            116f,
            68f,
            16);
        _timelineButton = CreateButton(
            "ToggleTimeline",
            "TIMELINE\n播放",
            156f,
            208f,
            new Color(0.12f, 0.17f, 0.3f, 0.5f),
            new Color(0.18f, 0.36f, 0.62f, 0.8f),
            HandleToggleTimeline,
            _rootPage.transform,
            116f,
            68f,
            16);
        _loadVmdButton = CreateButton(
            "LoadVmd",
            "VMD\n读取文件",
            288f,
            208f,
            new Color(0.28f, 0.2f, 0.07f, 0.46f),
            new Color(0.55f, 0.36f, 0.1f, 0.74f),
            HandleLoadVmd,
            _rootPage.transform,
            116f,
            68f,
            16);
        CreateButton(
            "OpenMmdSettings",
            "MMD 设置  >\n镜头 / 高跟鞋",
            420f,
            208f,
            new Color(0.28f, 0.2f, 0.07f, 0.46f),
            new Color(0.55f, 0.36f, 0.1f, 0.74f),
            HandleOpenMmdSettings,
            _rootPage.transform,
            116f,
            68f,
            15);

        CreateText("CharacterSection", _rootPage.transform, "角色  CHARACTER", 24f, 290f, 512f, 24f, 20,
            TextAnchor.MiddleLeft, new Color(0.47f, 0.9f, 0.55f, 1f));
        CreateButton(
            "OpenCharacterCards",
            "角色卡  >\nLOAD / REPLACE",
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
            "角色换装  >\nCLOTHING",
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
            "IK 控制器\n显示 / 隐藏",
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
        BuildBrowserPage();
        BuildSceneSavePage();
        BuildCharacterCardsPage();
        BuildCharacterPreviewPage();
        BuildMmdSettingsPage();
        BuildHighHeelsPage();
        BuildSettingsPage();

        Image statusBackground = CreateImage("StatusBackground", _menuRect, 24f, 414f, 512f, 62f, GlassSurfaceColor);
        ApplyGlassEffects(statusBackground, new Color(0.9f, 0.97f, 1f, 0.12f), true, 2f);
        _statusText = CreateText("Status", _menuRect, "就绪", 38f, 420f, 484f, 50f, 18,
            TextAnchor.MiddleLeft, new Color(0.78f, 0.86f, 0.9f, 1f));

        _clothingPage.SetActive(false);
        _browserPage.SetActive(false);
        _sceneSavePage.SetActive(false);
        _characterCardsPage.SetActive(false);
        _characterPreviewPage.SetActive(false);
        _mmdPage.SetActive(false);
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
        CreateText("ClothingTitle", _clothingPage.transform, "角色换装", 92f, 10f, 444f, 40f, 28,
            TextAnchor.MiddleLeft, Color.white);

        _clothingTargetButton = CreateButton(
            "ClothingTarget",
            "选择场景角色  >",
            24f,
            66f,
            new Color(0.075f, 0.24f, 0.14f, 0.48f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            HandleChooseClothingTarget,
            _clothingPage.transform,
            512f,
            38f,
            17);

        CreateText("ClothingPresetSection", _clothingPage.transform, "整套预设  PRESET", 24f, 112f, 512f, 24f, 18,
            TextAnchor.MiddleLeft, new Color(0.47f, 0.9f, 0.55f, 1f));
        CreateButton(
            "ClothingDressed",
            "穿好\nDRESSED",
            24f,
            140f,
            new Color(0.075f, 0.24f, 0.14f, 0.48f),
            new Color(0.11f, 0.46f, 0.24f, 0.76f),
            () => HandleClothingPreset(0),
            _clothingPage.transform,
            165f,
            48f,
            17);
        CreateButton(
            "ClothingHalf",
            "半脱\nHALF",
            198f,
            140f,
            new Color(0.28f, 0.2f, 0.07f, 0.46f),
            new Color(0.55f, 0.36f, 0.1f, 0.74f),
            () => HandleClothingPreset(1),
            _clothingPage.transform,
            164f,
            48f,
            17);
        CreateButton(
            "ClothingUndressed",
            "脱下\nUNDRESS",
            371f,
            140f,
            new Color(0.3f, 0.09f, 0.12f, 0.46f),
            new Color(0.58f, 0.15f, 0.2f, 0.74f),
            () => HandleClothingPreset(3),
            _clothingPage.transform,
            165f,
            48f,
            17);

        CreateText("ClothingPartsSection", _clothingPage.transform, "单独部位  PARTS", 24f, 198f, 512f, 24f, 18,
            TextAnchor.MiddleLeft, new Color(0.25f, 0.86f, 0.94f, 1f));
        for (int i = 0; i < _clothingPartButtons.Length; i++)
        {
            int partId = i;
            float x = i % 2 == 0 ? 24f : 288f;
            float y = 226f + i / 2 * 45f;
            _clothingPartButtons[i] = CreateButton(
                "ClothingPart" + i,
                VRCharacterClothingService.GetPartName(i) + "  -",
                x,
                y,
                new Color(0.08f, 0.15f, 0.18f, 0.5f),
                new Color(0.12f, 0.32f, 0.39f, 0.76f),
                () => HandleCycleClothingPart(partId),
                _clothingPage.transform,
                248f,
                39f,
                17);
        }
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
        text.font = _font;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 12;
        text.resizeTextMaxSize = fontSize;
        text.raycastTarget = false;

        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(0f, -1f);
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
        Image background = CreateImage(name, buttonParent, x, y, width, height, normalColor);
        ApplyGlassEffects(background, new Color(0.94f, 0.98f, 1f, 0.14f), true, 2f);
        CreateImage(
            name + "Glint",
            background.transform,
            10f,
            5f,
            width - 20f,
            1f,
            new Color(1f, 1f, 1f, 0.16f),
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
        Vector3 end = origin + direction * 2f;
        VRWristMenuButtonTarget target = null;
        float nearest = float.MaxValue;

        RaycastHit[] hits = Physics.RaycastAll(
            new Ray(origin, direction),
            2f,
            _pointerMask,
            QueryTriggerInteraction.Collide);
        foreach (RaycastHit hit in hits)
        {
            VRWristMenuButtonTarget candidate = hit.collider.GetComponent<VRWristMenuButtonTarget>();
            if (candidate != null && hit.distance < nearest)
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

        _laser.SetPosition(0, origin);
        _laser.SetPosition(1, end);
        _cursor.SetActive(target != null);
        if (target != null)
            _cursor.transform.position = end;

        Color pointerColor = target != null
            ? new Color(0.25f, 1f, 0.82f, 1f)
            : new Color(0.2f, 0.78f, 0.95f, 1f);
        _laser.SetColors(pointerColor, pointerColor);
        if (_cursorMaterial != null && _cursorMaterial.HasProperty("_Color"))
            _cursorMaterial.color = pointerColor;

        if (target != null && rightDevice.GetPressDown(EVRButtonId.k_EButton_Axis1))
        {
            rightDevice.TriggerHapticPulse(800, EVRButtonId.k_EButton_Axis0);
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
                _laser.SetWidth(0.002f, 0.002f);
                _laserMaterial = VRPointerVisuals.CreateMaterial(
                    "KKVR Wrist Menu Laser",
                    new Color(0.2f, 0.78f, 0.95f, 1f));
                if (_laserMaterial == null)
                    throw new InvalidOperationException("No pointer shader is available.");
                _laser.sharedMaterial = _laserMaterial;

                _cursor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _cursor.name = "KKVR_WristMenuCursor";
                _cursor.SetActive(false);
                _cursor.layer = _visibleLayer;
                _cursor.transform.SetParent(transform, false);
                _cursor.transform.localScale = Vector3.one * 0.008f;
                Collider cursorCollider = _cursor.GetComponent<Collider>();
                if (cursorCollider != null)
                    Destroy(cursorCollider);
                _cursorRenderer = _cursor.GetComponent<Renderer>();
                _cursorMaterial = VRPointerVisuals.CreateMaterial(
                    "KKVR Wrist Menu Cursor",
                    new Color(0.2f, 0.78f, 0.95f, 1f));
                if (_cursorRenderer == null || _cursorMaterial == null)
                    throw new InvalidOperationException("No cursor renderer or material is available.");
                _cursorRenderer.sharedMaterial = _cursorMaterial;
            }
            catch (Exception ex)
            {
                VRLog.Error("Failed to initialize wrist pointer: " + ex.Message);
                DestroyPointerVisuals();
                return false;
            }
        }

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

    private void DestroyPointerVisuals()
    {
        if (_laser != null)
        {
            Destroy(_laser.gameObject);
            _laser = null;
        }
        if (_cursor != null)
        {
            Destroy(_cursor);
            _cursor = null;
        }
        _cursorRenderer = null;
        _rightTransform = null;
        VRPointerVisuals.DestroyMaterial(ref _laserMaterial);
        VRPointerVisuals.DestroyMaterial(ref _cursorMaterial);
    }

    private void SetPointerVisible(bool visible)
    {
        if (_laser != null)
            _laser.gameObject.SetActive(visible);
        if (_cursor != null)
            _cursor.SetActive(false);
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
        _page = page;
        SetHoveredButton(null);

        if (_rootPage != null)
            _rootPage.SetActive(page == WristMenuPage.Root);
        if (_clothingPage != null)
            _clothingPage.SetActive(page == WristMenuPage.Clothing);
        if (_browserPage != null)
            _browserPage.SetActive(page == WristMenuPage.Browser);
        if (_sceneSavePage != null)
            _sceneSavePage.SetActive(page == WristMenuPage.SceneSave);
        if (_characterCardsPage != null)
            _characterCardsPage.SetActive(page == WristMenuPage.CharacterCards);
        if (_characterPreviewPage != null)
            _characterPreviewPage.SetActive(page == WristMenuPage.CharacterPreview);
        if (_mmdPage != null)
            _mmdPage.SetActive(page == WristMenuPage.MmdSettings);
        if (_highHeelsPage != null)
            _highHeelsPage.SetActive(page == WristMenuPage.HighHeels);
        if (_settingsPage != null)
            _settingsPage.SetActive(page == WristMenuPage.Settings);

        if (page == WristMenuPage.Root)
        {
            RefreshTimelineButton();
            RefreshVmdRootVisuals();
            _nextTimelineRefresh = Time.unscaledTime + 0.25f;
        }
        else if (page == WristMenuPage.Clothing)
        {
            RefreshClothingPage();
            _nextClothingRefresh = Time.unscaledTime + 0.25f;
        }
        else if (page == WristMenuPage.MmdSettings)
        {
            RefreshMmdSettingsPage();
        }
        else if (page == WristMenuPage.HighHeels)
        {
            RefreshHighHeelsPage();
        }
        else if (page == WristMenuPage.Settings)
        {
            RefreshSettingsPage();
        }
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
                ? "当前角色  " + selectionStatus + "  >"
                : "选择场景角色  >");

        for (int i = 0; i < _clothingPartButtons.Length; i++)
        {
            if (_clothingPartButtons[i] == null)
                continue;
            string state = hasCharacter
                ? VRCharacterClothingService.GetPartStateLabel(character, i)
                : "-";
            _clothingPartButtons[i].SetLabel(
                VRCharacterClothingService.GetPartName(i) + "  " + state);
        }
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
            "请选择要换装的场景角色",
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
        SetStatus("换装目标：" + target.DisplayName, new Color(0.35f, 1f, 0.62f, 1f), 0f);
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
        SetStatus("就绪", new Color(0.78f, 0.86f, 0.9f, 1f), 0f);
    }

    private void HandleClothingPreset(byte state)
    {
        string status;
        Studio.OCIChar character;
        bool hasTarget = TryGetClothingTarget(out character, out status);
        bool success = hasTarget
            && VRCharacterClothingService.TrySetAll(_clothingTargetObjectKey, state, out status);
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
        SetStatus(
            status,
            success ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.38f, 0.34f, 1f),
            4f);
        RefreshClothingPage();
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
        SetStatus("保存到场景根目录", new Color(0.25f, 0.86f, 0.94f, 1f), 0f);
    }

    private void HandleToggleMmd()
    {
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
            SetStatus("就绪", new Color(0.78f, 0.86f, 0.9f, 1f), 0f);
        if (_page == WristMenuPage.Clothing && Time.unscaledTime >= _nextClothingRefresh)
        {
            RefreshClothingPage();
            _nextClothingRefresh = Time.unscaledTime + 0.25f;
        }
        if (_page == WristMenuPage.Root && Time.unscaledTime >= _nextTimelineRefresh)
        {
            RefreshTimelineButton();
            _nextTimelineRefresh = Time.unscaledTime + 0.25f;
        }
    }

    private void SetStatus(string message, Color color, float duration)
    {
        if (_statusText == null)
            return;
        _statusText.text = message;
        _statusText.color = color;
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

    private static Font ResolveFont()
    {
        string[] candidates = { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial Unicode MS" };
        string[] installed = Font.GetOSInstalledFontNames();
        foreach (string candidate in candidates)
        {
            foreach (string name in installed)
            {
                if (string.Equals(name, candidate, StringComparison.OrdinalIgnoreCase))
                    return Font.CreateDynamicFontFromOSFont(name, 32);
            }
        }

        foreach (Font font in Resources.FindObjectsOfTypeAll<Font>())
        {
            if (font != null && font.HasCharacter('场'))
                return font;
        }

        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}

internal sealed class VRWristMenuButtonTarget : MonoBehaviour
{
    private Image _background;
    private Text _label;
    private Color _normalColor;
    private Color _hoverColor;
    private Action _onClick;
    private bool _hovered;

    public void Configure(Image background, Text label, Color normalColor, Color hoverColor, Action onClick)
    {
        _background = background;
        _label = label;
        _normalColor = normalColor;
        _hoverColor = hoverColor;
        _onClick = onClick;
        SetHovered(false);
    }

    public void SetHovered(bool hovered)
    {
        _hovered = hovered;
        if (_background != null)
            _background.color = hovered ? _hoverColor : _normalColor;
    }

    public void SetColors(Color normalColor, Color hoverColor)
    {
        _normalColor = normalColor;
        _hoverColor = hoverColor;
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

    public void Activate()
    {
        _onClick?.Invoke();
    }
}

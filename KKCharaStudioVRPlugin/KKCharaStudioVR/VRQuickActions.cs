using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRGIN.Core;
using VRGIN.Visuals;
using Valve.VR;
using Studio;

namespace KKCharaStudioVR;

public class VRQuickActions : MonoBehaviour
{
    private const float GuiTogglePressMaxDuration = 0.45f;

    public static VRQuickActions Instance { get; private set; }

    private bool uiVisible = true;
    private Dictionary<GUIQuad, bool> previousStates = new Dictionary<GUIQuad, bool>();
    private float lastToggleTime = 0f;
    private Dictionary<GUIQuad, Vector3> originalScales = new Dictionary<GUIQuad, Vector3>();
    private Dictionary<GUIQuad, Coroutine> scaleCoroutines = new Dictionary<GUIQuad, Coroutine>();
    private bool _guiTogglePressActive;
    private bool _guiTogglePressChorded;
    private float _guiTogglePressStarted;
    private bool _presentationSuppressed;
    private readonly Dictionary<GUIQuad, bool> _presentationStates = new Dictionary<GUIQuad, bool>();

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        SetPresentationSuppressed(false);
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (VR.Mode == null) return;
        if (_presentationSuppressed)
        {
            SuppressNewPresentationGuiQuads();
            return;
        }
        if (VRMmdPlaybackController.ConsumedPlaybackClickThisFrame
            || VRTimelineCameraFollowController.ConsumedRightStickTransportThisFrame
            || VRMmdPlaybackController.BlocksNormalInput)
            return;
        var left = VR.Mode.Left;
        var right = VR.Mode.Right;

        SteamVR_Controller.Device leftController = GetDevice(left);
        SteamVR_Controller.Device rightController = GetDevice(right);

        // GUI 显隐按键可使用当前双手布局，或集中到左手 X/Y、右手 A/B。
        KKCharaStudioVRSettings settings = GetSettings();
        HandleGuiToggleButton(settings, leftController, rightController);

        // 左摇杆按下逻辑分支
        if (leftController != null && leftController.GetPressDown(EVRButtonId.k_EButton_Axis0))
        {
            bool isGripPressed = leftController.GetPress(EVRButtonId.k_EButton_Grip);
            bool isTriggerPressed = leftController.GetPress(EVRButtonId.k_EButton_Axis1);
            bool isMenuPressed = leftController.GetPress(EVRButtonId.k_EButton_ApplicationMenu);

            // The left stick belongs exclusively to MMD playback transport.
            if (!isGripPressed && !isTriggerPressed && !isMenuPressed
                && VRMmdPlaybackController.TryHandleLeftStickPlaybackToggle())
            {
                return;
            }

            if (isGripPressed && isTriggerPressed)
            {
                // 左摇杆按下 + 中指 Grip + 扳机 Trigger —— 切换 ReShade (模拟 Home 键)
                ToggleReShade();
            }
            else if (isGripPressed)
            {
                // 左摇杆按下 + 中指 Grip (未按扳机) —— 召唤主菜单到面前
                SummonMainGUI();
            }
            else if (!isTriggerPressed && !isMenuPressed)
            {
                // 左摇杆按下 (且未握住 Grip 和 Trigger) —— 切换 MMDD 播放/暂停
                VRMmdPlaybackController.ConsumeLeftStickPlaybackClick();
                ToggleMMDDPlayPause();
            }
        }

        // 右摇杆按下优先控制 Timeline；不在 Timeline 控制空间时仍为撤销。
        if (rightController != null && rightController.GetPressDown(EVRButtonId.k_EButton_Axis0))
        {
            bool isChorded = rightController.GetPress(EVRButtonId.k_EButton_Grip)
                || rightController.GetPress(EVRButtonId.k_EButton_Axis1)
                || rightController.GetPress(EVRButtonId.k_EButton_ApplicationMenu);
            if (!isChorded)
            {
                if (VRTimelineCameraFollowController.TryHandleRightStickPlaybackToggle())
                    return;
                if (VRTimelineCameraFollowController.ShouldClaimRightStickTransport)
                    return;
            }

            TryUndo();
        }
    }

    private SteamVR_Controller.Device GetDevice(VRGIN.Controls.Controller controller)
    {
        if (controller != null && controller.IsTracking)
        {
            SteamVR_TrackedObject trackedObj = ((Component)controller).GetComponent<SteamVR_TrackedObject>();
            if (trackedObj != null)
            {
                return SteamVR_Controller.Input((int)trackedObj.index);
            }
        }
        return null;
    }

    internal void SetPresentationSuppressed(bool suppressed)
    {
        if (_presentationSuppressed == suppressed)
            return;
        _presentationSuppressed = suppressed;
        _guiTogglePressActive = false;

        if (suppressed)
        {
            _presentationStates.Clear();
            SuppressNewPresentationGuiQuads();
            return;
        }

        foreach (KeyValuePair<GUIQuad, bool> pair in new Dictionary<GUIQuad, bool>(_presentationStates))
        {
            GUIQuad quad = pair.Key;
            if (quad != null && quad.gameObject != null)
                quad.gameObject.SetActive(pair.Value);
        }
        _presentationStates.Clear();
    }

    private void SuppressNewPresentationGuiQuads()
    {
        foreach (GUIQuad quad in new List<GUIQuad>(GUIQuadRegistry.Quads))
        {
            if (quad == null || quad.gameObject == null)
                continue;
            if (!_presentationStates.ContainsKey(quad))
                _presentationStates[quad] = quad.gameObject.activeSelf;
            if (quad.gameObject.activeSelf)
                quad.gameObject.SetActive(false);
        }
    }

    private static KKCharaStudioVRSettings GetSettings()
    {
        if (VR.Manager == null || VR.Manager.Context == null)
            return null;
        return VR.Manager.Context.Settings as KKCharaStudioVRSettings;
    }

    private void HandleGuiToggleButton(
        KKCharaStudioVRSettings settings,
        SteamVR_Controller.Device leftController,
        SteamVR_Controller.Device rightController)
    {
        string layout = settings != null
            ? settings.ControllerFaceButtonLayout
            : KKCharaStudioVRSettings.ControllerLayoutSplitHands;

        SteamVR_Controller.Device device;
        EVRButtonId button;

        if (layout == KKCharaStudioVRSettings.ControllerLayoutLeftHand)
        {
            device = leftController;
            button = EVRButtonId.k_EButton_ApplicationMenu;
        }
        else if (layout == KKCharaStudioVRSettings.ControllerLayoutRightHand)
        {
            device = rightController;
            button = EVRButtonId.k_EButton_ApplicationMenu;
        }
        else
        {
            device = rightController;
            button = EVRButtonId.k_EButton_A;
        }

        if (device == null)
        {
            _guiTogglePressActive = false;
            return;
        }

        if (device.GetPressDown(button))
        {
            _guiTogglePressActive = true;
            _guiTogglePressChorded = false;
            _guiTogglePressStarted = Time.unscaledTime;
        }

        if (_guiTogglePressActive && device.GetPress(button))
        {
            // ApplicationMenu is also used for long-hold reset and Grip/Trigger
            // chords. Only an unchorded short release may toggle GUI visibility.
            _guiTogglePressChorded |= device.GetPress(EVRButtonId.k_EButton_Grip)
                || device.GetPress(EVRButtonId.k_EButton_Axis1);
        }

        if (!_guiTogglePressActive || !device.GetPressUp(button))
            return;

        float duration = Time.unscaledTime - _guiTogglePressStarted;
        _guiTogglePressActive = false;
        if (!_guiTogglePressChorded && duration <= GuiTogglePressMaxDuration)
        {
            ToggleAllGUI();
        }
    }

    private bool ToggleAllGUI(bool bypassDebounce = false)
    {
        if (!bypassDebounce && Time.time - lastToggleTime < 0.5f)
            return false;
        lastToggleTime = Time.time;

        uiVisible = !uiVisible;

        if (!uiVisible)
        {
            // HIDE: snapshot registry to avoid modification during iteration
            // (SetActive(false) in coroutine triggers OnDisable → Unregister)
            var currentQuads = new List<GUIQuad>(GUIQuadRegistry.Quads);
            foreach (var quad in currentQuads)
            {
                if (quad == null || quad.gameObject == null) continue;

                if (!originalScales.ContainsKey(quad) && quad.transform.localScale != Vector3.zero)
                {
                    originalScales[quad] = quad.transform.localScale;
                }

                Vector3 targetScale = originalScales.ContainsKey(quad) ? originalScales[quad] : Vector3.one;

                if (scaleCoroutines.ContainsKey(quad) && scaleCoroutines[quad] != null)
                {
                    StopCoroutine(scaleCoroutines[quad]);
                }

                previousStates[quad] = quad.gameObject.activeSelf;
                if (quad.gameObject.activeSelf)
                {
                    scaleCoroutines[quad] = StartCoroutine(ScaleAnimation(quad, Vector3.zero, true, targetScale));
                }
            }
        }
        else
        {
            // SHOW: iterate previousStates — quads have been unregistered from
            // GUIQuadRegistry when SetActive(false) triggered OnDisable → Unregister,
            // so GUIQuadRegistry.Quads is empty. We must use our saved references.
            Transform head = VR.Camera.Head;
            KKCharaStudioVRSettings settings = GetSettings();
            float guiDistance = settings != null ? settings.UISpawnDistance : 2.0f;
            float guiDrop = 0.05f;

            foreach (var kvp in new Dictionary<GUIQuad, bool>(previousStates))
            {
                var quad = kvp.Key;
                bool wasActive = kvp.Value;
                if (quad == null || quad.gameObject == null) continue;
                if (!wasActive) continue;

                if (!originalScales.ContainsKey(quad) && quad.transform.localScale != Vector3.zero)
                {
                    originalScales[quad] = quad.transform.localScale;
                }
                Vector3 targetScale = originalScales.ContainsKey(quad) ? originalScales[quad] : Vector3.one;

                // Reposition in front of head (VAM-style)
                if (head != null)
                {
                    Vector3 forward = head.forward;
                    forward.y = 0f;
                    if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
                    forward.Normalize();

                    quad.transform.position = head.position + forward * guiDistance - Vector3.up * guiDrop;
                    quad.transform.rotation = Quaternion.LookRotation(forward);
                }

                if (scaleCoroutines.ContainsKey(quad) && scaleCoroutines[quad] != null)
                {
                    StopCoroutine(scaleCoroutines[quad]);
                }

                quad.gameObject.SetActive(true);
                quad.transform.localScale = Vector3.zero;
                scaleCoroutines[quad] = StartCoroutine(ScaleAnimation(quad, targetScale, false, targetScale));
            }
            previousStates.Clear();
        }
        VRLog.Info($"Toggled UI Visibility to: {uiVisible}");
        return true;
    }

    private IEnumerator ScaleAnimation(GUIQuad quad, Vector3 targetScale, bool hideAfter, Vector3 restoreScale)
    {
        Vector3 startScale = quad.transform.localScale;
        float elapsed = 0f;
        float duration = 0.2f;
        
        while (elapsed < duration)
        {
            if (quad == null || quad.gameObject == null) yield break;
            elapsed += Time.deltaTime;
            quad.transform.localScale = Vector3.Lerp(startScale, targetScale, elapsed / duration);
            yield return null;
        }
        
        if (quad != null && quad.gameObject != null)
        {
            quad.transform.localScale = targetScale;
            if (hideAfter)
            {
                quad.gameObject.SetActive(false);
                quad.transform.localScale = restoreScale;
            }
        }
    }

    public void ForceHideUI()
    {
        if (uiVisible)
        {
            ToggleAllGUI();
        }
    }

    public bool SummonMainGUI()
    {
        return PlaceMainGUI(true);
    }

    public void RepositionMainGUIWithoutChangingVisibility()
    {
        PlaceMainGUI(false);
    }

    internal void RememberMainGUIScale(GUIQuad mainQuad)
    {
        if (mainQuad != null && mainQuad.gameObject != null
            && mainQuad.transform.localScale != Vector3.zero)
        {
            originalScales[mainQuad] = mainQuad.transform.localScale;
        }
    }

    private bool PlaceMainGUI(bool reveal)
    {
        // Search both registry (active quads) and previousStates (hidden quads)
        GUIQuad mainQuad = VRCameraMoveHelper.GetMainUI();
        float maxSize = mainQuad != null ? float.MaxValue : -1f;

        foreach (var quad in GUIQuadRegistry.Quads)
        {
            if (quad == null || quad.gameObject == null) continue;
            float size = quad.transform.localScale.x * quad.transform.localScale.y;
            if (size > maxSize)
            {
                maxSize = size;
                mainQuad = quad;
            }
        }

        // Also check hidden quads saved in previousStates
        foreach (var kvp in previousStates)
        {
            var quad = kvp.Key;
            if (quad == null || quad.gameObject == null) continue;
            if (!kvp.Value) continue; // was not active
            // Use originalScales for size since hidden quads have scale=0
            Vector3 s = originalScales.ContainsKey(quad) ? originalScales[quad] : Vector3.one;
            float size = s.x * s.y;
            if (size > maxSize)
            {
                maxSize = size;
                mainQuad = quad;
            }
        }

        if (mainQuad != null)
        {
            Transform head = VR.Camera.Head;
            if (head != null)
            {
                KKCharaStudioVRSettings settings = GetSettings();
                float guiDistance = settings != null ? settings.UISpawnDistance : 2.0f;
                float guiScale = settings != null ? settings.UISpawnScale : 1.0f;

                Vector3 forward = head.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
                forward.Normalize();

                mainQuad.transform.position = head.position + forward * guiDistance - Vector3.up * 0.05f;
                mainQuad.transform.rotation = Quaternion.LookRotation(forward);
                VRLog.Info(reveal
                    ? "Summoned main GUI panel to face"
                    : "Updated main GUI placement without changing visibility");

                Vector3 targetScale = VRCameraMoveHelper.GetMainUIScale(guiScale);
                originalScales[mainQuad] = targetScale;

                if (!uiVisible)
                {
                    mainQuad.transform.localScale = targetScale;
                    if (reveal)
                    {
                        // Explicit Recall reveals the prior UI set; +/- previews do not.
                        if (!ToggleAllGUI(true))
                            return false;
                    }
                }
                else
                {
                    if (reveal)
                        mainQuad.gameObject.SetActive(true);

                    if (!mainQuad.gameObject.activeSelf)
                    {
                        mainQuad.transform.localScale = targetScale;
                        return false;
                    }

                    if (scaleCoroutines.ContainsKey(mainQuad) && scaleCoroutines[mainQuad] != null)
                    {
                        StopCoroutine(scaleCoroutines[mainQuad]);
                    }
                    mainQuad.transform.localScale = Vector3.zero;
                    scaleCoroutines[mainQuad] = StartCoroutine(ScaleAnimation(mainQuad, targetScale, false, targetScale));
                }
                return !reveal || (uiVisible && mainQuad.gameObject.activeSelf);
            }
        }
        return false;
    }

    private void TryUndo()
    {
        try
        {
            if (Singleton<Studio.Studio>.Instance != null)
            {
                Singleton<UndoRedoManager>.Instance.Undo();
                VRLog.Info("Undo executed");
            }
        }
        catch (Exception e)
        {
            VRLog.Warn("Undo failed: " + e.Message);
        }
    }

    public static bool ikVisible = true;

    private void ToggleIKVisibility()
    {
        ikVisible = !ikVisible;

        // Only character guide handles have a guideObject. GUI panels also use
        // MoveableGUIObject and must remain visible and interactive.
        MoveableGUIObject[] mgos = FindObjectsOfType<MoveableGUIObject>();
        foreach (var mgo in mgos)
        {
            if (mgo != null && mgo.gameObject != null && mgo.guideObject != null)
            {
                Renderer[] rs = mgo.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rs)
                {
                    r.enabled = ikVisible;
                }

                Collider[] cs = mgo.GetComponentsInChildren<Collider>(true);
                foreach (var c in cs)
                {
                    c.enabled = ikVisible;
                }
            }
        }

        // 2. 隐藏/显示游戏自带的坐标轴控制箭头 (move, rotation, scale)
        if (Singleton<GuideObjectManager>.Instance != null)
        {
            var manager = Singleton<GuideObjectManager>.Instance;
            var field = typeof(GuideObjectManager).GetField("dicGuideObject", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (field != null)
            {
                var dic = field.GetValue(manager) as System.Collections.IDictionary;
                if (dic != null)
                {
                    foreach (System.Collections.DictionaryEntry entry in dic)
                    {
                        var go = entry.Value as GuideObject;
                        if (go != null && go.gameObject != null)
                        {
                            Transform move = go.transform.Find("move");
                            if (move != null) move.gameObject.SetActive(ikVisible);
                            
                            Transform rotation = go.transform.Find("rotation");
                            if (rotation != null) rotation.gameObject.SetActive(ikVisible);
                            
                            Transform scale = go.transform.Find("scale");
                            if (scale != null) scale.gameObject.SetActive(ikVisible);
                        }
                    }
                }
            }
        }
        VRLog.Info($"Toggled IK Controls visibility to: {ikVisible}");
    }

    public bool ToggleIkControls(out string status)
    {
        ToggleIKVisibility();
        status = ikVisible ? "IK 控制器已显示" : "IK 控制器已隐藏";
        return true;
    }

    private void ToggleMMDDPlayPause()
    {
        string status;
        if (!VRMmddService.TogglePlayPause(out status))
            VRLog.Warn(status);
    }

    private void ToggleReShade()
    {
        VRLog.Info("Left controller Left Joystick Click + Grip + Trigger pressed! Toggling ReShade (End key)...");
        KeyboradSimulatorUtil.PressEndKey();
    }
}

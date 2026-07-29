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
    private bool uiVisible = true;
    private Dictionary<GUIQuad, bool> previousStates = new Dictionary<GUIQuad, bool>();
    private float lastToggleTime = 0f;
    private Dictionary<GUIQuad, Vector3> originalScales = new Dictionary<GUIQuad, Vector3>();
    private Dictionary<GUIQuad, Coroutine> scaleCoroutines = new Dictionary<GUIQuad, Coroutine>();

    private void Update()
    {
        if (VR.Mode == null) return;
        var left = VR.Mode.Left;
        var right = VR.Mode.Right;

        SteamVR_Controller.Device leftController = GetDevice(left);
        SteamVR_Controller.Device rightController = GetDevice(right);

        // A 按钮（右手） —— 切换所有 UI
        if (rightController != null && rightController.GetPressDown(EVRButtonId.k_EButton_A))
        {
            ToggleAllGUI();
        }

        // X 按钮（左手） —— 隐藏/显示所有 IK 控制器轴向与抓取标记
        if (leftController != null && leftController.GetPressDown(EVRButtonId.k_EButton_A))
        {
            ToggleIKVisibility();
        }

        // 左摇杆按下逻辑分支
        if (leftController != null && leftController.GetPressDown(EVRButtonId.k_EButton_Axis0))
        {
            bool isGripPressed = leftController.GetPress(EVRButtonId.k_EButton_Grip);
            bool isTriggerPressed = leftController.GetPress(EVRButtonId.k_EButton_Axis1);

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
            else
            {
                // 左摇杆按下 (且未握住 Grip 和 Trigger) —— 切换 MMDD 播放/暂停
                ToggleMMDDPlayPause();
            }
        }

        // 右摇杆按下 —— 撤销上一步操作
        if (rightController != null && rightController.GetPressDown(EVRButtonId.k_EButton_Axis0))
        {
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

    private void ToggleAllGUI()
    {
        if (Time.time - lastToggleTime < 0.5f) return;
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
            KKCharaStudioVRSettings settings = null;
            if (VR.Manager != null && VR.Manager.Context != null)
            {
                settings = VR.Manager.Context.Settings as KKCharaStudioVRSettings;
            }
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

    private void SummonMainGUI()
    {
        // Search both registry (active quads) and previousStates (hidden quads)
        GUIQuad mainQuad = null;
        float maxSize = -1;

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
                KKCharaStudioVRSettings settings = null;
                if (VR.Manager != null && VR.Manager.Context != null)
                {
                    settings = VR.Manager.Context.Settings as KKCharaStudioVRSettings;
                }
                float guiDistance = settings != null ? settings.UISpawnDistance : 2.0f;

                Vector3 forward = head.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
                forward.Normalize();

                mainQuad.transform.position = head.position + forward * guiDistance - Vector3.up * 0.05f;
                mainQuad.transform.rotation = Quaternion.LookRotation(forward);
                VRLog.Info("Summoned main GUI panel to face");

                if (!originalScales.ContainsKey(mainQuad) && mainQuad.transform.localScale != Vector3.zero)
                {
                    originalScales[mainQuad] = mainQuad.transform.localScale;
                }
                Vector3 targetScale = originalScales.ContainsKey(mainQuad) ? originalScales[mainQuad] : Vector3.one;

                if (!uiVisible)
                {
                    // If UI is hidden, toggle all on (which repositions everything)
                    ToggleAllGUI();
                }
                else
                {
                    mainQuad.gameObject.SetActive(true);

                    if (scaleCoroutines.ContainsKey(mainQuad) && scaleCoroutines[mainQuad] != null)
                    {
                        StopCoroutine(scaleCoroutines[mainQuad]);
                    }
                    mainQuad.transform.localScale = Vector3.zero;
                    scaleCoroutines[mainQuad] = StartCoroutine(ScaleAnimation(mainQuad, targetScale, false, targetScale));
                }
            }
        }
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

    private static object _mainEngine;
    private static System.Reflection.MethodInfo _createScriptSourceMethod;
    private static System.Reflection.MethodInfo _compileMethod;
    private static System.Reflection.MethodInfo _executeMethod;
    private static bool _pythonInitialized = false;

    private void InitPython()
    {
        if (_pythonInitialized) return;
        try
        {
            System.Reflection.Assembly consoleAssembly = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "Unity.Console")
                {
                    consoleAssembly = assembly;
                    break;
                }
            }

            if (consoleAssembly != null)
            {
                Type programType = consoleAssembly.GetType("Unity.Console.Program");
                if (programType != null)
                {
                    var getMainEngine = programType.GetMethod("get_MainEngine", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (getMainEngine != null)
                    {
                        _mainEngine = getMainEngine.Invoke(null, null);
                        if (_mainEngine != null)
                        {
                            Type engineType = _mainEngine.GetType();
                            _createScriptSourceMethod = engineType.GetMethod("CreateScriptSourceFromString", new Type[] { typeof(string) });
                            
                            if (_createScriptSourceMethod != null)
                            {
                                VRLog.Info("Successfully resolved CreateScriptSourceFromString method on Python Engine!");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            VRLog.Error($"InitPython failed: {ex}");
        }
        _pythonInitialized = true;
    }

    private void ExecutePythonCode(string code)
    {
        InitPython();
        if (_mainEngine == null || _createScriptSourceMethod == null)
        {
            VRLog.Warn("Python engine not found or not initialized. Cannot execute python code.");
            return;
        }

        try
        {
            object scriptSource = _createScriptSourceMethod.Invoke(_mainEngine, new object[] { code });
            if (scriptSource != null)
            {
                if (_compileMethod == null)
                {
                    _compileMethod = scriptSource.GetType().GetMethod("Compile", new Type[] { });
                }

                if (_compileMethod != null)
                {
                    object compiledCode = _compileMethod.Invoke(scriptSource, null);
                    if (compiledCode != null)
                    {
                        if (_executeMethod == null)
                        {
                            _executeMethod = compiledCode.GetType().GetMethod("Execute", new Type[] { });
                        }

                        if (_executeMethod != null)
                        {
                            _executeMethod.Invoke(compiledCode, null);
                            VRLog.Info("Executed MMDD Play/Pause Python code successfully!");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            VRLog.Error($"ExecutePythonCode failed: {ex}");
        }
    }

    private void ToggleMMDDPlayPause()
    {
        VRLog.Info("Left controller ApplicationMenu (Y button) pressed! Toggling MMDD playback...");
        string code = "from vngameengine import vnge_game\nif hasattr(vnge_game.gdata, 'mmdd') and vnge_game.gdata.mmdd is not None:\n    vnge_game.gdata.mmdd.startStop()";
        ExecutePythonCode(code);
    }

    private void ToggleReShade()
    {
        VRLog.Info("Left controller Left Joystick Click + Grip + Trigger pressed! Toggling ReShade (End key)...");
        KeyboradSimulatorUtil.PressEndKey();
    }
}

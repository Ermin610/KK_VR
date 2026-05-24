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

        // A 按钮（右手）或 X 按钮（左手）—— 切换所有 UI
        // 不再响应 Axis0（摇杆按下），留给快捷功能
        if ((rightController != null && rightController.GetPressDown(EVRButtonId.k_EButton_A)) ||
            (leftController != null && leftController.GetPressDown(EVRButtonId.k_EButton_A)))
        {
            ToggleAllGUI();
        }

        // 左摇杆按下 —— 召唤主菜单到面前（替代之前冲突的 Menu 键）
        if (leftController != null && leftController.GetPressDown(EVRButtonId.k_EButton_Axis0))
        {
            SummonMainGUI();
        }

        // 右摇杆按下 —— 撤销上一步操作
        if (rightController != null && rightController.GetPressDown(EVRButtonId.k_EButton_Axis0))
        {
            TryUndo();
        }
    }

    private SteamVR_Controller.Device GetDevice(VRGIN.Controls.IController controller)
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
        
        foreach (var quad in GUIQuadRegistry.Quads)
        {
            if (quad == null || quad.gameObject == null) continue;
            
            if (quad.gameObject.GetComponent<MoveableGUIObject>() != null && quad.IsOwned)
            {
                continue;
            }

            if (!originalScales.ContainsKey(quad) && quad.transform.localScale != Vector3.zero)
            {
                originalScales[quad] = quad.transform.localScale;
            }

            Vector3 targetScale = originalScales.ContainsKey(quad) ? originalScales[quad] : Vector3.one;

            if (scaleCoroutines.ContainsKey(quad) && scaleCoroutines[quad] != null)
            {
                StopCoroutine(scaleCoroutines[quad]);
            }

            if (!uiVisible)
            {
                previousStates[quad] = quad.gameObject.activeSelf;
                if (quad.gameObject.activeSelf)
                {
                    scaleCoroutines[quad] = StartCoroutine(ScaleAnimation(quad, Vector3.zero, true, targetScale));
                }
            }
            else
            {
                if (previousStates.TryGetValue(quad, out bool wasActive))
                {
                    if (wasActive)
                    {
                        quad.gameObject.SetActive(true);
                        quad.transform.localScale = Vector3.zero;
                        scaleCoroutines[quad] = StartCoroutine(ScaleAnimation(quad, targetScale, false, targetScale));
                    }
                }
                else
                {
                    quad.gameObject.SetActive(true);
                    quad.transform.localScale = Vector3.zero;
                    scaleCoroutines[quad] = StartCoroutine(ScaleAnimation(quad, targetScale, false, targetScale));
                }
            }
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
        GUIQuad mainQuad = null;
        float maxSize = -1;

        foreach (var quad in GUIQuadRegistry.Quads)
        {
            if (quad == null || quad.gameObject == null) continue;
            
            if (quad.gameObject.GetComponent<MoveableGUIObject>() != null && quad.IsOwned)
            {
                continue; 
            }

            float size = quad.transform.localScale.x * quad.transform.localScale.y;
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
                mainQuad.transform.position = head.TransformPoint(new Vector3(0f, 0f, 0.5f));
                mainQuad.transform.rotation = Quaternion.LookRotation(head.TransformVector(new Vector3(0f, 0f, 1f)));
                VRLog.Info("Summoned main GUI panel to face");
                
                if (!originalScales.ContainsKey(mainQuad) && mainQuad.transform.localScale != Vector3.zero)
                {
                    originalScales[mainQuad] = mainQuad.transform.localScale;
                }
                Vector3 targetScale = originalScales.ContainsKey(mainQuad) ? originalScales[mainQuad] : Vector3.one;

                if (!uiVisible)
                {
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
}

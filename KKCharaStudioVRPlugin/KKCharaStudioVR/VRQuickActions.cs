using System;
using System.Collections.Generic;
using UnityEngine;
using VRGIN.Core;
using VRGIN.Visuals;
using Valve.VR;

namespace KKCharaStudioVR;

public class VRQuickActions : MonoBehaviour
{
    private bool uiVisible = true;
    private Dictionary<GUIQuad, bool> previousStates = new Dictionary<GUIQuad, bool>();
    private float menuDownTime;

    private void Update()
    {
        var left = VR.Mode.Left;
        var right = VR.Mode.Right;

        SteamVR_Controller.Device leftController = GetDevice(left);
        SteamVR_Controller.Device rightController = GetDevice(right);

        if (rightController != null && (rightController.GetPressDown(EVRButtonId.k_EButton_A) || rightController.GetPressDown(EVRButtonId.k_EButton_Axis0)))
        {
            ToggleAllGUI();
        }

        CheckMenuSummon(leftController);
        CheckMenuSummon(rightController);
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
        uiVisible = !uiVisible;
        
        foreach (var quad in GUIQuadRegistry.Quads)
        {
            if (quad == null || quad.gameObject == null) continue;
            
            if (quad.gameObject.GetComponent<MoveableGUIObject>() != null && quad.IsOwned)
            {
                continue;
            }

            if (!uiVisible)
            {
                previousStates[quad] = quad.gameObject.activeSelf;
                quad.gameObject.SetActive(false);
            }
            else
            {
                if (previousStates.TryGetValue(quad, out bool wasActive))
                {
                    quad.gameObject.SetActive(wasActive);
                }
                else
                {
                    quad.gameObject.SetActive(true);
                }
            }
        }
        VRLog.Info($"Toggled UI Visibility to: {uiVisible}");
    }

    public void ForceHideUI()
    {
        if (uiVisible)
        {
            ToggleAllGUI();
        }
    }

    private void CheckMenuSummon(SteamVR_Controller.Device controller)
    {
        if (controller == null) return;

        if (controller.GetPressDown(EVRButtonId.k_EButton_ApplicationMenu))
        {
            menuDownTime = Time.time;
        }

        if (controller.GetPressUp(EVRButtonId.k_EButton_ApplicationMenu))
        {
            if (Time.time - menuDownTime < 0.5f)
            {
                SummonMainGUI();
            }
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
                
                if (!uiVisible)
                {
                    ToggleAllGUI(); 
                }
                else
                {
                    mainQuad.gameObject.SetActive(true);
                }
            }
        }
    }
}

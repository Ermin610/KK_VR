using UnityEngine;
using VRGIN.Controls;

namespace KKCharaStudioVR;

internal static class ControllerExtensions
{
    public static void SetRenderModelVisible(this Controller controller, bool visible)
    {
        if (controller.Model != null)
        {
            Renderer[] renderers = ((Component)controller.Model).GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                r.enabled = visible;
            }
        }
    }
}

using UnityEngine;
using VRGIN.Core;

namespace KKCharaStudioVR;

internal static class VRPointerVisuals
{
    public static Material CreateMaterial(string name, Color color)
    {
        Material source = null;
        try
        {
            if (VR.Context != null && VR.Context.Materials != null)
                source = VR.Context.Materials.UnlitTransparent;
        }
        catch
        {
            source = null;
        }

        Material material = null;
        if (source != null && source.shader != null)
        {
            material = new Material(source);
        }
        else
        {
            Shader shader = Shader.Find("Unlit/Color")
                ?? Shader.Find("Unlit/Transparent")
                ?? Shader.Find("Sprites/Default");
            if (shader != null)
                material = new Material(shader);
        }

        if (material == null)
            return null;

        material.name = name;
        material.hideFlags = HideFlags.HideAndDontSave;
        material.renderQueue = 5000;
        if (material.HasProperty("_Color"))
            material.color = color;
        return material;
    }

    public static void DestroyMaterial(ref Material material)
    {
        if (material == null)
            return;
        Object.Destroy(material);
        material = null;
    }
}

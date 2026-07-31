using UnityEngine;
using VRGIN.Core;
using VRGIN.Helpers;

namespace VRGIN.Visuals;

public class DefaultMaterialPalette : IMaterialPalette
{
	public Material UnlitTransparentCombined { get; set; }

	public Material Sprite { get; set; }

	public Shader StandardShader { get; set; }

	public Material Unlit { get; set; }

	public Material UnlitTransparent { get; set; }

	public DefaultMaterialPalette()
	{
		Unlit = CreateUnlit();
		UnlitTransparent = CreateUnlitTransparent();
		UnlitTransparentCombined = CreateUnlitTransparentCombined();
		StandardShader = CreateStandardShader();
		Sprite = CreateSprite();
		if (!(Unlit != null) || !(Unlit.shader != null))
		{
			VRLog.Error("Could not load Unlit material!");
		}
		if (!(UnlitTransparent != null) || !(UnlitTransparent.shader != null))
		{
			VRLog.Error("Could not load UnlitTransparent material!");
		}
		if (!(UnlitTransparentCombined != null) || !(UnlitTransparentCombined.shader != null))
		{
			VRLog.Error("Could not load UnlitTransparentCombined material!");
		}
		if (!(StandardShader != null))
		{
			VRLog.Error("Could not load StandardShader material!");
		}
		if (!(Sprite != null) || !(Sprite.shader != null))
		{
			VRLog.Error("Could not load Sprite material!");
			Sprite = UnlitTransparent;
		}
	}

	private Material CreateUnlitTransparentCombined()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		return new Material(UnityHelper.GetShader("UnlitTransparentCombined"));
	}

	private Material CreateSprite()
	{
		return UnlitTransparent != null ? new Material(UnlitTransparent) : null;
	}

	private Shader CreateStandardShader()
	{
		return Shader.Find("Standard");
	}

	private Material CreateUnlit()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		return new Material(UnityHelper.GetShader("Unlit"));
	}

	private Material CreateUnlitTransparent()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		return new Material(UnityHelper.GetShader("UnlitTransparent"));
	}
}

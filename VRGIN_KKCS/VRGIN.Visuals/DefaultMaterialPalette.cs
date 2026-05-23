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
		if (!Object.op_Implicit((Object)(object)Unlit) || !Object.op_Implicit((Object)(object)Unlit.shader))
		{
			VRLog.Error("Could not load Unlit material!");
		}
		if (!Object.op_Implicit((Object)(object)UnlitTransparent) || !Object.op_Implicit((Object)(object)UnlitTransparent.shader))
		{
			VRLog.Error("Could not load UnlitTransparent material!");
		}
		if (!Object.op_Implicit((Object)(object)UnlitTransparentCombined) || !Object.op_Implicit((Object)(object)UnlitTransparentCombined.shader))
		{
			VRLog.Error("Could not load UnlitTransparentCombined material!");
		}
		if (!Object.op_Implicit((Object)(object)StandardShader))
		{
			VRLog.Error("Could not load StandardShader material!");
		}
		if (!Object.op_Implicit((Object)(object)Sprite) || !Object.op_Implicit((Object)(object)Sprite.shader))
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
		return Resources.GetBuiltinResource<Material>("Sprites-Default.mat");
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

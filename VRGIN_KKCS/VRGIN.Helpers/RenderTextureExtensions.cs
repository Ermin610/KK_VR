using System.IO;
using UnityEngine;

namespace VRGIN.Helpers;

public static class RenderTextureExtensions
{
	public static void SaveToFile(this RenderTexture renderTexture, string name)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		RenderTexture active = RenderTexture.active;
		RenderTexture.active = renderTexture;
		Texture2D val = new Texture2D(((Texture)renderTexture).width, ((Texture)renderTexture).height);
		val.ReadPixels(new Rect(0f, 0f, (float)((Texture)val).width, (float)((Texture)val).height), 0, 0);
		byte[] bytes = val.EncodeToPNG();
		File.WriteAllBytes(name, bytes);
		Object.Destroy((Object)(object)val);
		RenderTexture.active = active;
	}
}

using UnityEngine;

namespace VRUtil;

public class VRIMGUIUtil
{
	private static GUISkin _guiSkin = null;

	private static Color windowTextColor = new Color(0.1f, 0.5f, 0.1f, 1f);

	private static Color buttonTextColor = new Color(0.3f, 0.9f, 0.3f, 1f);

	public static GUISkin VRGUISkin
	{
		get
		{
			if ((Object)(object)_guiSkin == (Object)null)
			{
				_guiSkin = CreateVRGUISkin(GUI.skin);
			}
			return _guiSkin;
		}
		set
		{
			if ((Object)(object)value != (Object)null)
			{
				_guiSkin = value;
			}
		}
	}

	public static GUISkin CreateVRGUISkin(GUISkin cloneFrom)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected O, but got Unknown
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c1: Unknown result type (might be due to invalid IL or missing references)
		GUISkin val = Object.Instantiate<GUISkin>(cloneFrom);
		GUIStyle val2 = new GUIStyle(val.window);
		Texture2D val3 = new Texture2D(1, 1);
		val3.SetPixel(0, 0, new Color(0.9f, 0.9f, 0.9f, 1f));
		val3.Apply();
		val2.normal.textColor = windowTextColor;
		val2.normal.background = val3;
		val2.onNormal.textColor = windowTextColor;
		val2.onNormal.background = val3;
		val.window = val2;
		val.button.normal.textColor = buttonTextColor;
		val.button.onNormal.textColor = buttonTextColor;
		val.button.normal.background = CreateColorInvertedTexture((Texture)(object)val.button.normal.background);
		val.button.onNormal.background = CreateColorInvertedTexture((Texture)(object)val.button.onNormal.background);
		val.label.normal.textColor = windowTextColor;
		val.label.onNormal.textColor = windowTextColor;
		val.toggle.normal.textColor = windowTextColor;
		val.toggle.onNormal.textColor = windowTextColor;
		val.toggle.normal.background = CreateColorInvertedTexture((Texture)(object)val.toggle.normal.background);
		val.toggle.onNormal.background = CreateColorInvertedTexture((Texture)(object)val.toggle.normal.background);
		val.settings.selectionColor = windowTextColor;
		val.textField.normal.textColor = buttonTextColor;
		val.textField.onNormal.textColor = buttonTextColor;
		val.textField.focused.textColor = buttonTextColor;
		val.textField.onFocused.textColor = buttonTextColor;
		val.textField.normal.background = CreateColorInvertedTexture((Texture)(object)val.textField.normal.background);
		val.textField.onNormal.background = CreateColorInvertedTexture((Texture)(object)val.textField.onNormal.background);
		val.textField.focused.background = CreateColorInvertedTexture((Texture)(object)val.textField.focused.background);
		val.textField.onFocused.background = CreateColorInvertedTexture((Texture)(object)val.textField.onFocused.background);
		val.textArea.normal.textColor = buttonTextColor;
		val.textArea.onNormal.textColor = buttonTextColor;
		val.textArea.focused.textColor = buttonTextColor;
		val.textArea.onFocused.textColor = buttonTextColor;
		val.textArea.normal.background = CreateColorInvertedTexture((Texture)(object)val.textArea.normal.background);
		val.textArea.onNormal.background = CreateColorInvertedTexture((Texture)(object)val.textArea.onNormal.background);
		val.textArea.focused.background = CreateColorInvertedTexture((Texture)(object)val.textArea.focused.background);
		val.textArea.onFocused.background = CreateColorInvertedTexture((Texture)(object)val.textArea.onFocused.background);
		val.box.normal.background = val3;
		val.box.onNormal.background = val3;
		val.box.normal.textColor = windowTextColor;
		val.box.onNormal.textColor = windowTextColor;
		val.horizontalSlider.normal.background = CreateColorInvertedTexture((Texture)(object)val.horizontalSlider.normal.background);
		val.horizontalSlider.onNormal.background = CreateColorInvertedTexture((Texture)(object)val.horizontalSlider.onNormal.background);
		val.verticalSlider.normal.background = CreateColorInvertedTexture((Texture)(object)val.verticalSlider.normal.background);
		val.verticalSlider.onNormal.background = CreateColorInvertedTexture((Texture)(object)val.verticalSlider.onNormal.background);
		val.horizontalSliderThumb.normal.background = CreateColorInvertedTexture((Texture)(object)val.horizontalSliderThumb.normal.background);
		val.horizontalSliderThumb.onNormal.background = CreateColorInvertedTexture((Texture)(object)val.horizontalSliderThumb.onNormal.background);
		val.verticalSliderThumb.normal.background = CreateColorInvertedTexture((Texture)(object)val.verticalSliderThumb.normal.background);
		val.verticalSliderThumb.onNormal.background = CreateColorInvertedTexture((Texture)(object)val.verticalSliderThumb.onNormal.background);
		return val;
	}

	public static Texture2D CreateColorInvertedTexture(Texture tex, bool isLiner = false)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Expected O, but got Unknown
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		float num = 0.5f;
		float num2 = 0.5f;
		if ((Object)(object)tex == (Object)null)
		{
			return null;
		}
		RenderTextureReadWrite val = (RenderTextureReadWrite)1;
		if (!isLiner)
		{
			val = (RenderTextureReadWrite)2;
		}
		RenderTexture val2 = new RenderTexture(tex.width, tex.height, 0, (RenderTextureFormat)0, val);
		bool sRGBWrite = GL.sRGBWrite;
		GL.sRGBWrite = !isLiner;
		Graphics.SetRenderTarget(val2);
		GL.Clear(false, true, new Color(0f, 0f, 0f, 0f));
		Graphics.SetRenderTarget((RenderTexture)null);
		Graphics.Blit(tex, val2);
		GL.sRGBWrite = sRGBWrite;
		Texture2D val3 = new Texture2D(((Texture)val2).width, ((Texture)val2).height, (TextureFormat)5, isLiner);
		RenderTexture.active = val2;
		val3.ReadPixels(new Rect(0f, 0f, (float)((Texture)val2).width, (float)((Texture)val2).height), 0, 0);
		val3.Apply();
		RenderTexture.active = null;
		val2.Release();
		Color[] pixels = val3.GetPixels();
		for (int i = 0; i < pixels.Length; i++)
		{
			pixels[i].r = Mathf.Clamp01(pixels[i].r + num);
			pixels[i].g = Mathf.Clamp01(pixels[i].g + num);
			pixels[i].b = Mathf.Clamp01(pixels[i].b + num);
			if (pixels[i].a != 0f)
			{
				pixels[i].a = Mathf.Clamp01(pixels[i].a + num2);
			}
		}
		val3.SetPixels(pixels);
		val3.Apply();
		((Texture)val3).wrapMode = tex.wrapMode;
		return val3;
	}
}

using UnityEngine;
using Valve.VR;

public class SteamVR_Fade : MonoBehaviour
{
	private Color currentColor = new Color(0f, 0f, 0f, 0f);

	private Color targetColor = new Color(0f, 0f, 0f, 0f);

	private Color deltaColor = new Color(0f, 0f, 0f, 0f);

	private bool fadeOverlay;

	private static Material fadeMaterial = null;

	private static int fadeMaterialColorID = -1;

	public static void Start(Color newColor, float duration, bool fadeOverlay = false)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		SteamVR_Events.Fade.Send(newColor, duration, fadeOverlay);
	}

	public static void View(Color newColor, float duration)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		OpenVR.Compositor?.FadeToColor(duration, newColor.r, newColor.g, newColor.b, newColor.a, bBackground: false);
	}

	public void OnStartFade(Color newColor, float duration, bool fadeOverlay)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		if (duration > 0f)
		{
			targetColor = newColor;
			deltaColor = (targetColor - currentColor) / duration;
		}
		else
		{
			currentColor = newColor;
		}
	}

	private void OnEnable()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		if (fadeMaterial == null)
		{
			fadeMaterial = new Material(Shader.Find("Custom/SteamVR_Fade"));
			fadeMaterialColorID = Shader.PropertyToID("fadeColor");
		}
		SteamVR_Events.Fade.Listen(OnStartFade);
		SteamVR_Events.FadeReady.Send();
	}

	private void OnDisable()
	{
		SteamVR_Events.Fade.Remove(OnStartFade);
	}

	private void OnPostRender()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		if (currentColor != targetColor)
		{
			if (Mathf.Abs(currentColor.a - targetColor.a) < Mathf.Abs(deltaColor.a) * Time.deltaTime)
			{
				currentColor = targetColor;
				deltaColor = new Color(0f, 0f, 0f, 0f);
			}
			else
			{
				currentColor += deltaColor * Time.deltaTime;
			}
			if (fadeOverlay)
			{
				SteamVR_Overlay instance = SteamVR_Overlay.instance;
				if (instance != null)
				{
					instance.alpha = 1f - currentColor.a;
				}
			}
		}
		if (currentColor.a > 0f && (fadeMaterial != null))
		{
			fadeMaterial.SetColor(fadeMaterialColorID, currentColor);
			fadeMaterial.SetPass(0);
			GL.Begin(7);
			GL.Vertex3(-1f, -1f, 0f);
			GL.Vertex3(1f, -1f, 0f);
			GL.Vertex3(1f, 1f, 0f);
			GL.Vertex3(-1f, 1f, 0f);
			GL.End();
		}
	}
}

using System.Runtime.InteropServices;
using UnityEngine;
using Valve.VR;

public class SteamVR_Stats : MonoBehaviour
{
	public GUIText text;

	public Color fadeColor = Color.black;

	public float fadeDuration = 1f;

	private double lastUpdate;

	private void Awake()
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)text == (Object)null)
		{
			text = ((Component)this).GetComponent<GUIText>();
			((Behaviour)text).enabled = false;
		}
		if (fadeDuration > 0f)
		{
			SteamVR_Fade.Start(fadeColor, 0f);
			SteamVR_Fade.Start(Color.clear, fadeDuration);
		}
	}

	private void Update()
	{
		if (!((Object)(object)text != (Object)null))
		{
			return;
		}
		if (Input.GetKeyDown((KeyCode)105))
		{
			((Behaviour)text).enabled = !((Behaviour)text).enabled;
		}
		if (!((Behaviour)text).enabled)
		{
			return;
		}
		CVRCompositor compositor = OpenVR.Compositor;
		if (compositor != null)
		{
			Compositor_FrameTiming pTiming = default(Compositor_FrameTiming);
			pTiming.m_nSize = (uint)Marshal.SizeOf(typeof(Compositor_FrameTiming));
			compositor.GetFrameTiming(ref pTiming, 0u);
			double flSystemTimeInSeconds = pTiming.m_flSystemTimeInSeconds;
			if (flSystemTimeInSeconds > lastUpdate)
			{
				double num = ((lastUpdate > 0.0) ? (1.0 / (flSystemTimeInSeconds - lastUpdate)) : 0.0);
				lastUpdate = flSystemTimeInSeconds;
				text.text = $"framerate: {num:N0}\ndropped frames: {(int)pTiming.m_nNumDroppedFrames}";
			}
			else
			{
				lastUpdate = flSystemTimeInSeconds;
			}
		}
	}
}

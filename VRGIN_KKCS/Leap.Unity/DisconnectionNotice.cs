using UnityEngine;
using UnityEngine.UI;

namespace Leap.Unity;

public class DisconnectionNotice : MonoBehaviour
{
	public float fadeInTime = 1f;

	public float fadeOutTime = 1f;

	public AnimationCurve fade;

	public int waitFrames = 10;

	public Sprite embeddedReplacementImage;

	public Color onColor = Color.white;

	private Controller leap_controller_;

	private float fadedIn;

	private int frames_disconnected_;

	private void Start()
	{
		leap_controller_ = new Controller();
		SetAlpha(0f);
	}

	private void SetAlpha(float alpha)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		((Graphic)((Component)this).GetComponent<Image>()).color = Color.Lerp(Color.clear, onColor, alpha);
	}

	private bool IsConnected()
	{
		return leap_controller_.IsConnected;
	}

	private bool IsEmbedded()
	{
		DeviceList devices = leap_controller_.Devices;
		if (devices.Count == 0)
		{
			return false;
		}
		return devices[0].IsEmbedded;
	}

	private void Update()
	{
		if ((Object)(object)embeddedReplacementImage != (Object)null && IsEmbedded())
		{
			((Component)this).GetComponent<Image>().sprite = embeddedReplacementImage;
		}
		if (IsConnected())
		{
			frames_disconnected_ = 0;
		}
		else
		{
			frames_disconnected_++;
		}
		if (frames_disconnected_ < waitFrames)
		{
			fadedIn -= Time.deltaTime / fadeOutTime;
		}
		else
		{
			fadedIn += Time.deltaTime / fadeInTime;
		}
		fadedIn = Mathf.Clamp(fadedIn, 0f, 1f);
		SetAlpha(fade.Evaluate(fadedIn));
	}
}

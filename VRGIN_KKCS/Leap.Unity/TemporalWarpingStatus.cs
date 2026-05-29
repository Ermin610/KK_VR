using UnityEngine;
using UnityEngine.UI;

namespace Leap.Unity;

public class TemporalWarpingStatus : MonoBehaviour
{
	public LeapVRTemporalWarping cameraAlignment;

	protected Text textField;

	protected SmoothedFloat _imageLatency = new SmoothedFloat();

	protected SmoothedFloat _frameDelta = new SmoothedFloat();

	[SerializeField]
	private LeapProvider Provider;

	private void Start()
	{
		textField = ((Component)this).GetComponent<Text>();
		if (textField == null)
		{
			((Component)this).gameObject.SetActive(false);
		}
		_imageLatency.delay = 0.1f;
		_frameDelta.delay = 0.1f;
	}

	private void Update()
	{
	}
}

using UnityEngine;

namespace Leap.Unity;

public class StretchToScreen : MonoBehaviour
{
	private void Awake()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		((Component)this).GetComponent<GUITexture>().pixelInset = new Rect(0f, 0f, (float)Screen.width, (float)Screen.height);
	}
}

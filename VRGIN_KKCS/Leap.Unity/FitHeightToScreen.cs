using UnityEngine;

namespace Leap.Unity;

public class FitHeightToScreen : MonoBehaviour
{
	private void Awake()
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		float num = (float)(((Component)this).GetComponent<GUITexture>().texture.width / ((Component)this).GetComponent<GUITexture>().texture.height) * (float)Screen.height;
		float num2 = ((float)Screen.width - num) / 2f;
		((Component)this).GetComponent<GUITexture>().pixelInset = new Rect(num2, 0f, num, (float)Screen.height);
	}
}

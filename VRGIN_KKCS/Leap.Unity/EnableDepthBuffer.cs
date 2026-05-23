using UnityEngine;

namespace Leap.Unity;

[ExecuteInEditMode]
public class EnableDepthBuffer : MonoBehaviour
{
	public const string DEPTH_TEXTURE_VARIANT_NAME = "USE_DEPTH_TEXTURE";

	[SerializeField]
	private DepthTextureMode _depthTextureMode = (DepthTextureMode)1;

	private void Awake()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		((Component)this).GetComponent<Camera>().depthTextureMode = _depthTextureMode;
		if (SystemInfo.SupportsRenderTextureFormat((RenderTextureFormat)1) && (int)_depthTextureMode != 0)
		{
			Shader.EnableKeyword("USE_DEPTH_TEXTURE");
		}
		else
		{
			Shader.DisableKeyword("USE_DEPTH_TEXTURE");
		}
	}
}

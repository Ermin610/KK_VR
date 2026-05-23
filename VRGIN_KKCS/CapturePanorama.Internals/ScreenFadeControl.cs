using UnityEngine;

namespace CapturePanorama.Internals;

public class ScreenFadeControl : MonoBehaviour
{
	public Material fadeMaterial;

	private void OnPostRender()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		fadeMaterial.SetPass(0);
		GL.PushMatrix();
		GL.LoadOrtho();
		GL.Color(fadeMaterial.color);
		GL.Begin(7);
		GL.Vertex3(0f, 0f, -12f);
		GL.Vertex3(0f, 1f, -12f);
		GL.Vertex3(1f, 1f, -12f);
		GL.Vertex3(1f, 0f, -12f);
		GL.End();
		GL.PopMatrix();
	}
}

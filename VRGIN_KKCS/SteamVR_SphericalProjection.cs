using System;
using UnityEngine;

[ExecuteInEditMode]
public class SteamVR_SphericalProjection : MonoBehaviour
{
	private static Material material;

	public void Set(Vector3 N, float phi0, float phi1, float theta0, float theta1, Vector3 uAxis, Vector3 uOrigin, float uScale, Vector3 vAxis, Vector3 vOrigin, float vScale)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		if ((Object)(object)material == (Object)null)
		{
			material = new Material(Shader.Find("Custom/SteamVR_SphericalProjection"));
		}
		material.SetVector("_N", new Vector4(N.x, N.y, N.z));
		material.SetFloat("_Phi0", phi0 * ((float)Math.PI / 180f));
		material.SetFloat("_Phi1", phi1 * ((float)Math.PI / 180f));
		material.SetFloat("_Theta0", theta0 * ((float)Math.PI / 180f) + (float)Math.PI / 2f);
		material.SetFloat("_Theta1", theta1 * ((float)Math.PI / 180f) + (float)Math.PI / 2f);
		material.SetVector("_UAxis", Vector4.op_Implicit(uAxis));
		material.SetVector("_VAxis", Vector4.op_Implicit(vAxis));
		material.SetVector("_UOrigin", Vector4.op_Implicit(uOrigin));
		material.SetVector("_VOrigin", Vector4.op_Implicit(vOrigin));
		material.SetFloat("_UScale", uScale);
		material.SetFloat("_VScale", vScale);
	}

	private void OnRenderImage(RenderTexture src, RenderTexture dest)
	{
		Graphics.Blit((Texture)(object)src, dest, material);
	}
}

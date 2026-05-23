using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Valve.VR;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class SteamVR_PlayArea : MonoBehaviour
{
	public enum Size
	{
		Calibrated,
		_400x300,
		_300x225,
		_200x150
	}

	public float borderThickness = 0.15f;

	public float wireframeHeight = 2f;

	public bool drawWireframeWhenSelectedOnly;

	public bool drawInGame = true;

	public Size size;

	public Color color = Color.cyan;

	[HideInInspector]
	public Vector3[] vertices;

	public static bool GetBounds(Size size, ref HmdQuad_t pRect)
	{
		bool flag;
		int num;
		if (size == Size.Calibrated)
		{
			flag = !SteamVR.active && !SteamVR.usingNativeSupport;
			if (flag)
			{
				EVRInitError peError = EVRInitError.None;
				OpenVR.Init(ref peError, EVRApplicationType.VRApplication_Utility);
			}
			CVRChaperone chaperone = OpenVR.Chaperone;
			if (chaperone != null)
			{
				num = (chaperone.GetPlayAreaRect(ref pRect) ? 1 : 0);
				if (num != 0)
				{
					goto IL_0044;
				}
			}
			else
			{
				num = 0;
			}
			Debug.LogWarning((object)"Failed to get Calibrated Play Area bounds!  Make sure you have tracking first, and that your space is calibrated.");
			goto IL_0044;
		}
		try
		{
			string[] array = size.ToString().Substring(1).Split(new char[1] { 'x' }, 2);
			float num2 = float.Parse(array[0]) / 200f;
			float num3 = float.Parse(array[1]) / 200f;
			pRect.vCorners0.v0 = num2;
			pRect.vCorners0.v1 = 0f;
			pRect.vCorners0.v2 = 0f - num3;
			pRect.vCorners1.v0 = 0f - num2;
			pRect.vCorners1.v1 = 0f;
			pRect.vCorners1.v2 = 0f - num3;
			pRect.vCorners2.v0 = 0f - num2;
			pRect.vCorners2.v1 = 0f;
			pRect.vCorners2.v2 = num3;
			pRect.vCorners3.v0 = num2;
			pRect.vCorners3.v1 = 0f;
			pRect.vCorners3.v2 = num3;
			return true;
		}
		catch
		{
		}
		return false;
		IL_0044:
		if (flag)
		{
			OpenVR.Shutdown();
		}
		return (byte)num != 0;
	}

	public void BuildMesh()
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0302: Unknown result type (might be due to invalid IL or missing references)
		//IL_0307: Unknown result type (might be due to invalid IL or missing references)
		//IL_0334: Unknown result type (might be due to invalid IL or missing references)
		//IL_0339: Unknown result type (might be due to invalid IL or missing references)
		//IL_0366: Unknown result type (might be due to invalid IL or missing references)
		//IL_036b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0372: Unknown result type (might be due to invalid IL or missing references)
		//IL_0379: Expected O, but got Unknown
		//IL_03bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c7: Expected O, but got Unknown
		HmdQuad_t pRect = default(HmdQuad_t);
		if (!GetBounds(size, ref pRect))
		{
			return;
		}
		HmdVector3_t[] array = new HmdVector3_t[4] { pRect.vCorners0, pRect.vCorners1, pRect.vCorners2, pRect.vCorners3 };
		vertices = (Vector3[])(object)new Vector3[array.Length * 2];
		for (int i = 0; i < array.Length; i++)
		{
			HmdVector3_t hmdVector3_t = array[i];
			vertices[i] = new Vector3(hmdVector3_t.v0, 0.01f, hmdVector3_t.v2);
		}
		if (borderThickness == 0f)
		{
			((Component)this).GetComponent<MeshFilter>().mesh = null;
			return;
		}
		for (int j = 0; j < array.Length; j++)
		{
			int num = (j + 1) % array.Length;
			int num2 = (j + array.Length - 1) % array.Length;
			Vector3 val = vertices[num] - vertices[j];
			Vector3 normalized = ((Vector3)(ref val)).normalized;
			val = vertices[num2] - vertices[j];
			Vector3 normalized2 = ((Vector3)(ref val)).normalized;
			Vector3 val2 = vertices[j];
			val2 += Vector3.Cross(normalized, Vector3.up) * borderThickness;
			val2 += Vector3.Cross(normalized2, Vector3.down) * borderThickness;
			vertices[array.Length + j] = val2;
		}
		int[] triangles = new int[24]
		{
			0, 4, 1, 1, 4, 5, 1, 5, 2, 2,
			5, 6, 2, 6, 3, 3, 6, 7, 3, 7,
			0, 0, 7, 4
		};
		Vector2[] uv = (Vector2[])(object)new Vector2[8]
		{
			new Vector2(0f, 0f),
			new Vector2(1f, 0f),
			new Vector2(0f, 0f),
			new Vector2(1f, 0f),
			new Vector2(0f, 1f),
			new Vector2(1f, 1f),
			new Vector2(0f, 1f),
			new Vector2(1f, 1f)
		};
		Color[] colors = (Color[])(object)new Color[8]
		{
			color,
			color,
			color,
			color,
			new Color(color.r, color.g, color.b, 0f),
			new Color(color.r, color.g, color.b, 0f),
			new Color(color.r, color.g, color.b, 0f),
			new Color(color.r, color.g, color.b, 0f)
		};
		Mesh val3 = new Mesh();
		((Component)this).GetComponent<MeshFilter>().mesh = val3;
		val3.vertices = vertices;
		val3.uv = uv;
		val3.colors = colors;
		val3.triangles = triangles;
		MeshRenderer component = ((Component)this).GetComponent<MeshRenderer>();
		((Renderer)component).material = new Material(Shader.Find("Sprites/Default"));
		((Renderer)component).reflectionProbeUsage = (ReflectionProbeUsage)0;
		((Renderer)component).shadowCastingMode = (ShadowCastingMode)0;
		((Renderer)component).receiveShadows = false;
		((Renderer)component).lightProbeUsage = (LightProbeUsage)0;
	}

	private void OnDrawGizmos()
	{
		if (!drawWireframeWhenSelectedOnly)
		{
			DrawWireframe();
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (drawWireframeWhenSelectedOnly)
		{
			DrawWireframe();
		}
	}

	public void DrawWireframe()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		if (vertices != null && vertices.Length != 0)
		{
			Vector3 val = ((Component)this).transform.TransformVector(Vector3.up * wireframeHeight);
			for (int i = 0; i < 4; i++)
			{
				int num = (i + 1) % 4;
				Vector3 val2 = ((Component)this).transform.TransformPoint(vertices[i]);
				Vector3 val3 = val2 + val;
				Vector3 val4 = ((Component)this).transform.TransformPoint(vertices[num]);
				Vector3 val5 = val4 + val;
				Gizmos.DrawLine(val2, val3);
				Gizmos.DrawLine(val2, val4);
				Gizmos.DrawLine(val3, val5);
			}
		}
	}

	public void OnEnable()
	{
		if (Application.isPlaying)
		{
			((Renderer)((Component)this).GetComponent<MeshRenderer>()).enabled = drawInGame;
			((Behaviour)this).enabled = false;
			if (drawInGame && size == Size.Calibrated)
			{
				((MonoBehaviour)this).StartCoroutine(UpdateBounds());
			}
		}
	}

	private IEnumerator UpdateBounds()
	{
		((Component)this).GetComponent<MeshFilter>().mesh = null;
		CVRChaperone chaperone = OpenVR.Chaperone;
		if (chaperone != null)
		{
			while (chaperone.GetCalibrationState() != ChaperoneCalibrationState.OK)
			{
				yield return null;
			}
			BuildMesh();
		}
	}
}

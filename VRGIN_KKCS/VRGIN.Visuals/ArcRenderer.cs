using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using VRGIN.Core;

namespace VRGIN.Visuals;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class ArcRenderer : MonoBehaviour
{
	public int VertexCount = 50;

	public float UvSpeed = 5f;

	public float Velocity = 6f;

	private MeshFilter _MeshFilter;

	private Renderer _Renderer;

	public Vector3 target;

	public float Offset;

	public float Scale = 1f;

	private Mesh _mesh;

	protected virtual void Awake()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		_MeshFilter = ((Component)this).GetComponent<MeshFilter>();
		_Renderer = ((Component)this).GetComponent<Renderer>();
		_mesh = new Mesh();
		_Renderer.material = VRManager.Instance.Context.Materials.Sprite;
		_Renderer.shadowCastingMode = (ShadowCastingMode)0;
		_Renderer.receiveShadows = false;
		_Renderer.useLightProbes = false;
		_Renderer.material.color = VRManager.Instance.Context.PrimaryColor;
	}

	public virtual void Update()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		Vector3 forward = ((Component)this).transform.forward;
		List<Vector3> list = new List<Vector3>();
		Vector3 position = ((Component)this).transform.position;
		float num = (0f - (Velocity * ((Component)this).transform.forward).y) * Scale;
		float num2 = Physics.gravity.y * Scale;
		float num3 = position.y - Offset;
		float num4 = (Mathf.Sqrt(num * num - 2f * num2 * num3) + num) / num2;
		float num5 = (num - Mathf.Sqrt(num * num - 2f * num2 * num3)) / num2;
		float num6 = Mathf.Max(num4, num5);
		num6 = Mathf.Abs(num6);
		float num7 = num6 / (float)VertexCount;
		for (int j = 0; j <= VertexCount; j++)
		{
			float num8 = Mathf.Clamp((float)j / ((float)VertexCount - 1f) * num6 + Time.time * UvSpeed % 2f * num7 - num7, 0f, num6);
			list.Add(((Component)this).transform.InverseTransformPoint(position + (forward * Velocity * num8 + 0.5f * Physics.gravity * num8 * num8) * Scale));
		}
		target = ((Component)this).transform.position + (forward * Velocity * num6 + 0.5f * Physics.gravity * num6 * num6) * Scale;
		target.y = 0f;
		Material material = ((Component)this).GetComponent<Renderer>().material;
		material.mainTextureOffset += new Vector2(UvSpeed * Time.deltaTime, 0f);
		_mesh.vertices = list.ToArray();
		_mesh.SetIndices((from i in list.Take(list.Count - 1).Select((Vector3 ve, int i) => i)
			where i % 2 == 0
			select i).SelectMany((int i) => new int[2]
		{
			i,
			i + 1
		}).ToArray(), (MeshTopology)3, 0);
		_MeshFilter.mesh = _mesh;
	}

	private void OnEnable()
	{
		((Component)this).GetComponent<Renderer>().enabled = true;
	}

	private void OnDisable()
	{
		((Component)this).GetComponent<Renderer>().enabled = false;
	}
}

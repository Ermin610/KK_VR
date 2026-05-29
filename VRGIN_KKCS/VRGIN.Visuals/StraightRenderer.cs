using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using VRGIN.Core;

namespace VRGIN.Visuals;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class StraightRenderer : ArcRenderer
{
	private MeshFilter _MeshFilter;

	private Renderer _Renderer;

	private Mesh _mesh;

	protected override void Awake()
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

	public override void Update()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		Vector3 forward = ((Component)this).transform.forward;
		List<Vector3> list = new List<Vector3>();
		Plane val = default(Plane);
		val = new Plane(Vector3.up, 0f);
		Vector3 position = ((Component)this).transform.position;
		Ray val2 = default(Ray);
		val2 = new Ray(position, forward);
		float num = default(float);
		if (val.Raycast(val2, out num))
		{
			target = position + forward * num;
			target.y = 0f;
			Material material = ((Component)this).GetComponent<Renderer>().material;
			material.mainTextureOffset += new Vector2(UvSpeed * Time.deltaTime, 0f);
			for (int j = 0; j <= VertexCount; j++)
			{
				float num2 = (float)j / ((float)VertexCount - 1f) * num;
				list.Add(((Component)this).transform.InverseTransformPoint(position + forward * num2));
				_mesh.vertices = list.ToArray();
			}
			_mesh.SetIndices((from i in list.Take(list.Count - 1).Select((Vector3 ve, int i) => i)
				where i % 2 == 0
				select i).SelectMany((int i) => new int[2]
			{
				i,
				i + 1
			}).ToArray(), (MeshTopology)3, 0);
			_MeshFilter.mesh = _mesh;
		}
		else
		{
			target = position;
			target.y = 0f;
		}
	}
}

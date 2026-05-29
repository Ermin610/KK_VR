using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Leap.Unity;

public class PolyFinger : FingerModel
{
	private const int MAX_SIDES = 30;

	private const int TRIANGLE_INDICES_PER_QUAD = 6;

	private const int VERTICES_PER_QUAD = 4;

	public int sides = 4;

	public bool smoothNormals;

	public float startingAngle;

	public float[] widths = new float[3];

	protected Vector3[] vertices_;

	protected Vector3[] normals_;

	protected Vector3[] joint_vertices_;

	protected Mesh mesh_;

	protected Mesh cap_mesh_;

	protected Vector3[] cap_vertices_;

	public override void InitFinger()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		InitJointVertices();
		InitCapsMesh();
		InitMesh();
		((Component)this).GetComponent<MeshFilter>().mesh = new Mesh();
		UpdateFinger();
	}

	public override void UpdateFinger()
	{
		UpdateMesh();
		UpdateCapMesh();
		if (vertices_ != null)
		{
			mesh_.vertices = vertices_;
			if (smoothNormals)
			{
				mesh_.normals = normals_;
			}
			else
			{
				mesh_.RecalculateNormals();
			}
			cap_mesh_.vertices = cap_vertices_;
			cap_mesh_.RecalculateNormals();
			CombineInstance[] array = (CombineInstance[])(object)new CombineInstance[2];
			array[0].mesh = mesh_;
			array[1].mesh = cap_mesh_;
			((Component)this).GetComponent<MeshFilter>().sharedMesh.CombineMeshes(array, true, false);
			((Component)this).GetComponent<MeshFilter>().sharedMesh.RecalculateBounds();
		}
	}

	private void OnDestroy()
	{
		Object.Destroy((Object)(object)mesh_);
		Object.Destroy((Object)(object)cap_mesh_);
		Object.Destroy((Object)(object)((Component)this).GetComponent<MeshFilter>().mesh);
	}

	private void Update()
	{
	}

	protected Quaternion GetJointRotation(int joint)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		if (joint <= 0)
		{
			return GetBoneRotation(joint);
		}
		if (joint >= 4)
		{
			return GetBoneRotation(joint - 1);
		}
		return Quaternion.Slerp(GetBoneRotation(joint - 1), GetBoneRotation(joint), 0.5f);
	}

	protected void InitJointVertices()
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		joint_vertices_ = (Vector3[])(object)new Vector3[sides];
		for (int i = 0; i < sides; i++)
		{
			float num = startingAngle + (float)i * 360f / (float)sides;
			joint_vertices_[i] = Quaternion.AngleAxis(num, -Vector3.forward) * Vector3.up;
		}
	}

	protected void UpdateMesh()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		for (int i = 0; i < 4; i++)
		{
			Vector3 val = ((Component)this).transform.InverseTransformPoint(GetJointPosition(i));
			Vector3 val2 = ((Component)this).transform.InverseTransformPoint(GetJointPosition(i + 1));
			Quaternion val3 = Quaternion.Inverse(((Component)this).transform.rotation) * GetJointRotation(i);
			Quaternion val4 = Quaternion.Inverse(((Component)this).transform.rotation) * GetJointRotation(i + 1);
			for (int j = 0; j < sides; j++)
			{
				int num2 = (j + 1) % sides;
				if (smoothNormals)
				{
					Vector3 val5 = val3 * joint_vertices_[j];
					Vector3 val6 = val3 * joint_vertices_[num2];
					normals_[num] = (normals_[num + 2] = val5);
					normals_[num + 1] = (normals_[num + 3] = val6);
				}
				Vector3 val7 = val3 * (widths[i] * joint_vertices_[j]);
				vertices_[num++] = val + val7;
				val7 = val3 * (widths[i] * joint_vertices_[num2]);
				vertices_[num++] = val + val7;
				val7 = val4 * (widths[i + 1] * joint_vertices_[j]);
				vertices_[num++] = val2 + val7;
				val7 = val4 * (widths[i + 1] * joint_vertices_[num2]);
				vertices_[num++] = val2 + val7;
			}
		}
	}

	protected void UpdateCapMesh()
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = ((Component)this).transform.InverseTransformPoint(GetJointPosition(0));
		Vector3 val2 = ((Component)this).transform.InverseTransformPoint(GetJointPosition(2));
		Quaternion val3 = Quaternion.Inverse(((Component)this).transform.rotation) * GetJointRotation(0);
		Quaternion val4 = Quaternion.Inverse(((Component)this).transform.rotation) * GetJointRotation(2);
		for (int i = 0; i < sides; i++)
		{
			cap_vertices_[i] = val + val3 * (widths[0] * joint_vertices_[i]);
			cap_vertices_[sides + i] = val2 + val4 * (widths[2] * joint_vertices_[i]);
		}
	}

	protected void InitMesh()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		mesh_ = new Mesh();
		mesh_.MarkDynamic();
		int num = 0;
		int num2 = 4 * sides * 4;
		vertices_ = (Vector3[])(object)new Vector3[num2];
		normals_ = (Vector3[])(object)new Vector3[num2];
		Vector2[] array = (Vector2[])(object)new Vector2[num2];
		int num3 = 0;
		int[] array2 = new int[6 * sides * 4];
		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < sides; j++)
			{
				array2[num3++] = num;
				array2[num3++] = num + 2;
				array2[num3++] = num + 1;
				array2[num3++] = num + 2;
				array2[num3++] = num + 3;
				array2[num3++] = num + 1;
				array[num] = ((Vector2)(new Vector3(1f * (float)j / (float)sides, 1f * (float)i / 4f)));
				array[num + 1] = ((Vector2)(new Vector3((1f + (float)j) / (float)sides, 1f * (float)i / 4f)));
				array[num + 2] = ((Vector2)(new Vector3(1f * (float)j / (float)sides, (1f + (float)i) / 4f)));
				array[num + 3] = ((Vector2)(new Vector3((1f + (float)j) / (float)sides, (1f + (float)i) / 4f)));
				vertices_[num++] = new Vector3(0f, 0f, 0f);
				vertices_[num++] = new Vector3(0f, 0f, 0f);
				vertices_[num++] = new Vector3(0f, 0f, 0f);
				vertices_[num++] = new Vector3(0f, 0f, 0f);
			}
		}
		mesh_.vertices = vertices_;
		mesh_.normals = normals_;
		mesh_.uv = array;
		mesh_.triangles = array2;
	}

	protected void InitCapsMesh()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		cap_mesh_ = new Mesh();
		cap_mesh_.MarkDynamic();
		cap_vertices_ = cap_mesh_.vertices;
		int num = 2 * sides;
		if (num != cap_vertices_.Length)
		{
			Array.Resize(ref cap_vertices_, num);
		}
		Vector2[] array = cap_mesh_.uv;
		if (array.Length != num)
		{
			Array.Resize(ref array, num);
		}
		int num2 = 0;
		int[] array2 = cap_mesh_.triangles;
		int num3 = 6 * (sides - 2);
		if (num3 != array2.Length)
		{
			Array.Resize(ref array2, num3);
		}
		for (int i = 0; i < sides; i++)
		{
			cap_vertices_[i] = new Vector3(0f, 0f, 0f);
			cap_vertices_[i + sides] = new Vector3(0f, 0f, 0f);
			array[i] = ((Vector2)(0.5f * joint_vertices_[i]));
			ref Vector2 reference = ref array[i];
			reference += new Vector2(0.5f, 0.5f);
			array[i + sides] = ((Vector2)(0.5f * joint_vertices_[i]));
			ref Vector2 reference2 = ref array[i + sides];
			reference2 += new Vector2(0.5f, 0.5f);
		}
		for (int j = 0; j < sides - 2; j++)
		{
			array2[num2++] = 0;
			array2[num2++] = j + 1;
			array2[num2++] = j + 2;
			array2[num2++] = sides;
			array2[num2++] = sides + j + 2;
			array2[num2++] = sides + j + 1;
		}
		cap_mesh_.vertices = cap_vertices_;
		cap_mesh_.uv = array;
		cap_mesh_.triangles = array2;
	}
}

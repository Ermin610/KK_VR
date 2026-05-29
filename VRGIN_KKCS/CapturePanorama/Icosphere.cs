using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CapturePanorama;

public static class Icosphere
{
	public static Mesh BuildIcosphere(float radius, int iterations)
	{
		Mesh val = BuildIcosahedron(radius);
		for (int i = 0; i < iterations; i++)
		{
			Refine(val);
		}
		return val;
	}

	public static Mesh BuildIcosahedron(float radius)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		Mesh val = new Mesh();
		float num = (float)((1.0 + Math.Sqrt(5.0)) / 2.0);
		Vector3[] array = (Vector3[])(object)new Vector3[12]
		{
			new Vector3(-1f, num, 0f),
			new Vector3(1f, num, 0f),
			new Vector3(-1f, 0f - num, 0f),
			new Vector3(1f, 0f - num, 0f),
			new Vector3(0f, -1f, num),
			new Vector3(0f, 1f, num),
			new Vector3(0f, -1f, 0f - num),
			new Vector3(0f, 1f, 0f - num),
			new Vector3(num, 0f, -1f),
			new Vector3(num, 0f, 1f),
			new Vector3(0f - num, 0f, -1f),
			new Vector3(0f - num, 0f, 1f)
		};
		Vector3 val2 = new Vector3(1f, num, 0f);
		float num2 = radius / val2.magnitude;
		for (int i = 0; i < array.Length; i++)
		{
			ref Vector3 reference = ref array[i];
			reference *= num2;
		}
		val.vertices = array;
		val.triangles = new int[60]
		{
			0, 11, 5, 0, 5, 1, 0, 1, 7, 0,
			7, 10, 0, 10, 11, 1, 5, 9, 5, 11,
			4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
			3, 9, 4, 3, 4, 2, 3, 2, 6, 3,
			6, 8, 3, 8, 9, 4, 9, 5, 2, 4,
			11, 6, 2, 10, 8, 6, 7, 9, 8, 1
		};
		return val;
	}

	private static void Refine(Mesh m)
	{
		throw new Exception("TODO");
	}
}

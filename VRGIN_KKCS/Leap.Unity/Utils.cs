using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Leap.Unity;

public static class Utils
{
	public static void IgnoreCollisions(GameObject first, GameObject second, bool ignore = true)
	{
		if (first == null || second == null)
		{
			return;
		}
		Collider[] componentsInChildren = first.GetComponentsInChildren<Collider>();
		Collider[] componentsInChildren2 = second.GetComponentsInChildren<Collider>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			for (int j = 0; j < componentsInChildren2.Length; j++)
			{
				if (componentsInChildren[i] != componentsInChildren2[j] && componentsInChildren[i].enabled && componentsInChildren2[j].enabled)
				{
					Physics.IgnoreCollision(componentsInChildren[i], componentsInChildren2[j], ignore);
				}
			}
		}
	}

	public static void DrawCircle(Vector3 center, Vector3 normal, float radius, Color color, int quality = 32, float duration = 0f, bool depthTest = true)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		Vector3 forward = Vector3.Slerp(normal, -normal, 0.5f);
		DrawArc(360f, center, forward, normal, radius, color, quality, duration, depthTest);
	}

	public static void DrawArc(float arc, Vector3 center, Vector3 forward, Vector3 normal, float radius, Color color, int quality = 32, float duration = 0f, bool depthTest = true)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = Vector3.Cross(normal, forward);
		Vector3 normalized = val.normalized;
		float num = arc / (float)quality;
		Vector3 val2 = center + forward * radius;
		Vector3 val3 = default(Vector3);
		for (float num2 = 0f; Mathf.Abs(num2) <= Mathf.Abs(arc); num2 += num)
		{
			float num3 = Mathf.Cos(num2 * ((float)Math.PI / 180f));
			float num4 = Mathf.Sin(num2 * ((float)Math.PI / 180f));
			val3.x = center.x + radius * (num3 * forward.x + num4 * normalized.x);
			val3.y = center.y + radius * (num3 * forward.y + num4 * normalized.y);
			val3.z = center.z + radius * (num3 * forward.z + num4 * normalized.z);
			Debug.DrawLine(val2, val3, color, duration, depthTest);
			val2 = val3;
		}
	}

	public static void DrawCone(Vector3 origin, Vector3 direction, float angle, float height, Color color, int quality = 4, float duration = 0f, bool depthTest = true)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		float num = height / (float)quality;
		for (float num2 = num; num2 <= height; num2 += num)
		{
			DrawCircle(origin + direction * num2, direction, Mathf.Tan(angle * ((float)Math.PI / 180f)) * num2, color, quality * 8, duration, depthTest);
		}
	}
}

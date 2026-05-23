using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRGIN.Core;

namespace VRGIN.Helpers;

public static class Calculator
{
	public static float Distance(float worldValue)
	{
		return worldValue / VR.Settings.IPDScale * VR.Context.UnitToMeter;
	}

	public static float Angle(Vector3 v1, Vector3 v2)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Atan2(v1.x, v1.z) * 57.29578f;
		float num2 = Mathf.Atan2(v2.x, v2.z) * 57.29578f;
		return Mathf.DeltaAngle(num, num2);
	}

	public static Vector3 GetForwardVector(Quaternion rotation)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = rotation * Vector3.forward;
		Vector3 val2 = ((IEnumerable<Vector3>)(object)new Vector3[2]
		{
			Vector3.ProjectOnPlane(val, Vector3.up),
			Vector3.ProjectOnPlane(rotation * ((val.y > 0f) ? Vector3.down : Vector3.up), Vector3.up)
		}).OrderByDescending((Vector3 v) => ((Vector3)(ref v)).sqrMagnitude).First();
		return ((Vector3)(ref val2)).normalized;
	}
}

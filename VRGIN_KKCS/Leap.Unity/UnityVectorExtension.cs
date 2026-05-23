using LeapInternal;
using UnityEngine;

namespace Leap.Unity;

public static class UnityVectorExtension
{
	public static Vector3 ToVector3(this Vector vector)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		return new Vector3(vector.x, vector.y, vector.z);
	}

	public static Vector3 ToVector3(this LEAP_VECTOR vector)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		return new Vector3(vector.x, vector.y, vector.z);
	}

	public static Vector4 ToVector4(this Vector vector)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		return new Vector4(vector.x, vector.y, vector.z, 0f);
	}

	public static Vector ToVector(this Vector3 vector)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		return new Vector(vector.x, vector.y, vector.z);
	}

	public static LEAP_VECTOR ToCVector(this Vector3 vector)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		LEAP_VECTOR result = default(LEAP_VECTOR);
		result.x = vector.x;
		result.y = vector.y;
		result.z = vector.z;
		return result;
	}
}

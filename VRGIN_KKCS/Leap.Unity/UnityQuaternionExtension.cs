using LeapInternal;
using UnityEngine;

namespace Leap.Unity;

public static class UnityQuaternionExtension
{
	public static Quaternion ToQuaternion(this LeapQuaternion q)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		return new Quaternion(q.x, q.y, q.z, q.w);
	}

	public static Quaternion ToQuaternion(this LEAP_QUATERNION q)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		return new Quaternion(q.x, q.y, q.z, q.w);
	}

	public static LeapQuaternion ToLeapQuaternion(this Quaternion q)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		return new LeapQuaternion(q.x, q.y, q.z, q.w);
	}

	public static LEAP_QUATERNION ToCQuaternion(this Quaternion q)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		LEAP_QUATERNION result = default(LEAP_QUATERNION);
		result.x = q.x;
		result.y = q.y;
		result.z = q.z;
		result.w = q.w;
		return result;
	}
}

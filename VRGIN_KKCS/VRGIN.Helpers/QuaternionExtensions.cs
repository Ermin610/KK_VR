using UnityEngine;

namespace VRGIN.Helpers;

public static class QuaternionExtensions
{
	public static Vector3 ToPitchYawRollRad(this Quaternion rotation)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Atan2(2f * rotation.y * rotation.w - 2f * rotation.x * rotation.z, 1f - 2f * rotation.y * rotation.y - 2f * rotation.z * rotation.z);
		float num2 = Mathf.Atan2(2f * rotation.x * rotation.w - 2f * rotation.y * rotation.z, 1f - 2f * rotation.x * rotation.x - 2f * rotation.z * rotation.z);
		float num3 = Mathf.Asin(2f * rotation.x * rotation.y + 2f * rotation.z * rotation.w);
		return new Vector3(num2, num3, num);
	}
}

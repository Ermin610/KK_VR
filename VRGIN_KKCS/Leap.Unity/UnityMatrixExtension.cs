using UnityEngine;

namespace Leap.Unity;

public static class UnityMatrixExtension
{
	public static readonly Vector LEAP_UP = new Vector(0f, 1f, 0f);

	public static readonly Vector LEAP_FORWARD = new Vector(0f, 0f, -1f);

	public static readonly Vector LEAP_ORIGIN = new Vector(0f, 0f, 0f);

	public static readonly float MM_TO_M = 0.001f;

	public static Quaternion CalculateRotation(this LeapTransform trs)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = trs.yBasis.ToVector3();
		return Quaternion.LookRotation(-trs.zBasis.ToVector3(), val);
	}

	public static LeapTransform GetLeapMatrix(this Transform t)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		LeapTransform result = new LeapTransform(scale: new Vector(t.lossyScale.x * MM_TO_M, t.lossyScale.y * MM_TO_M, t.lossyScale.z * MM_TO_M), translation: t.position.ToVector(), rotation: t.rotation.ToLeapQuaternion());
		result.MirrorZ();
		return result;
	}
}

using UnityEngine;

namespace Leap.Unity;

public class CameraFollower : MonoBehaviour
{
	public Vector3 objectForward = Vector3.forward;

	public AnimationCurve Ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

	[Range(1f, 20f)]
	public float Speed = 10f;

	public bool FreezeX;

	public bool FreezeY;

	public bool FreezeZ;

	private Quaternion offset;

	private Quaternion startingLocalRotation;

	private void Awake()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		offset = Quaternion.Inverse(Quaternion.LookRotation(objectForward));
		startingLocalRotation = ((Component)this).transform.localRotation;
	}

	private void Update()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = ((Component)Camera.main).transform.position - ((Component)this).transform.position;
		Vector3 normalized = val.normalized;
		float num = Vector3.Angle(((Component)this).transform.TransformDirection(objectForward), normalized);
		float num2 = Ease.Evaluate(Speed * num / 360f);
		Quaternion val2 = Quaternion.LookRotation(normalized);
		val2 *= offset;
		((Component)this).transform.rotation = Quaternion.Slerp(((Component)this).transform.rotation, val2, num2);
		Vector3 eulerAngles = startingLocalRotation.eulerAngles;
		Vector3 localEulerAngles = ((Component)this).transform.localEulerAngles;
		float num3 = ((!FreezeX) ? localEulerAngles.x : eulerAngles.x);
		float num4 = ((!FreezeY) ? localEulerAngles.y : eulerAngles.y);
		float num5 = ((!FreezeZ) ? localEulerAngles.z : eulerAngles.z);
		((Component)this).transform.localEulerAngles = new Vector3(num3, num4, num5);
	}
}

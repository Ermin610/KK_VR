using UnityEngine;

namespace Leap.Unity;

public class SkeletalHand : HandModel
{
	protected const float PALM_CENTER_OFFSET = 0.015f;

	public override ModelType HandModelType => ModelType.Graphics;

	private void Start()
	{
		Utils.IgnoreCollisions(((Component)this).gameObject, ((Component)this).gameObject);
		for (int i = 0; i < fingers.Length; i++)
		{
			if ((Object)(object)fingers[i] != (Object)null)
			{
				fingers[i].fingerType = (Finger.FingerType)i;
			}
		}
	}

	public override void UpdateHand()
	{
		SetPositions();
	}

	protected Vector3 GetPalmCenter()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = 0.015f * Vector3.Scale(GetPalmDirection(), ((Component)this).transform.lossyScale);
		return GetPalmPosition() - val;
	}

	protected void SetPositions()
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		Debug.Log((object)"SkeletalHand.SetPositions()");
		for (int i = 0; i < fingers.Length; i++)
		{
			if ((Object)(object)fingers[i] != (Object)null)
			{
				fingers[i].UpdateFinger();
			}
		}
		if ((Object)(object)palm != (Object)null)
		{
			palm.position = GetPalmCenter();
			palm.rotation = GetPalmRotation();
		}
		if ((Object)(object)wristJoint != (Object)null)
		{
			wristJoint.position = GetWristPosition();
			wristJoint.rotation = GetPalmRotation();
		}
		if ((Object)(object)forearm != (Object)null)
		{
			forearm.position = GetArmCenter();
			forearm.rotation = GetArmRotation();
		}
	}
}

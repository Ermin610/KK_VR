using UnityEngine;

namespace Leap.Unity;

public class SkeletalFinger : FingerModel
{
	public override void InitFinger()
	{
		SetPositions();
	}

	public override void UpdateFinger()
	{
		Debug.Log((object)"SkeletalFinger.SetPositions()");
		SetPositions();
	}

	protected void SetPositions()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < bones.Length; i++)
		{
			if (bones[i] != null)
			{
				((Component)bones[i]).transform.position = GetBoneCenter(i);
				((Component)bones[i]).transform.rotation = GetBoneRotation(i);
			}
		}
		for (int j = 0; j < joints.Length; j++)
		{
			if (joints[j] != null)
			{
				((Component)joints[j]).transform.position = GetJointPosition(j + 1);
				((Component)joints[j]).transform.rotation = GetBoneRotation(j + 1);
			}
		}
	}
}

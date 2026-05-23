using UnityEngine;

namespace Leap.Unity;

public class RigidFinger : SkeletalFinger
{
	public float filtering = 0.5f;

	private void Start()
	{
		for (int i = 0; i < bones.Length; i++)
		{
			if ((Object)(object)bones[i] != (Object)null)
			{
				((Component)bones[i]).GetComponent<Rigidbody>().maxAngularVelocity = float.PositiveInfinity;
			}
		}
	}

	public override void UpdateFinger()
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < bones.Length; i++)
		{
			if ((Object)(object)bones[i] != (Object)null)
			{
				CapsuleCollider component = ((Component)bones[i]).GetComponent<CapsuleCollider>();
				if ((Object)(object)component != (Object)null)
				{
					component.direction = 2;
					bones[i].localScale = new Vector3(1f / ((Component)this).transform.lossyScale.x, 1f / ((Component)this).transform.lossyScale.y, 1f / ((Component)this).transform.lossyScale.z);
					component.radius = GetBoneWidth(i) / 2f;
					component.height = GetBoneLength(i) + GetBoneWidth(i);
				}
				Rigidbody component2 = ((Component)bones[i]).GetComponent<Rigidbody>();
				if (Object.op_Implicit((Object)(object)component2))
				{
					component2.MovePosition(GetBoneCenter(i));
					component2.MoveRotation(GetBoneRotation(i));
				}
				else
				{
					bones[i].position = GetBoneCenter(i);
					bones[i].rotation = GetBoneRotation(i);
				}
			}
		}
	}
}

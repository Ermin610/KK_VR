using UnityEngine;

namespace Leap.Unity;

public class RigidHand : SkeletalHand
{
	public float filtering = 0.5f;

	public override ModelType HandModelType => ModelType.Physics;

	public override bool SupportsEditorPersistence()
	{
		return true;
	}

	public override void InitHand()
	{
		base.InitHand();
	}

	public override void UpdateHand()
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < fingers.Length; i++)
		{
			if ((Object)(object)fingers[i] != (Object)null)
			{
				fingers[i].UpdateFinger();
			}
		}
		if ((Object)(object)palm != (Object)null)
		{
			Rigidbody component = ((Component)palm).GetComponent<Rigidbody>();
			if (Object.op_Implicit((Object)(object)component))
			{
				component.MovePosition(GetPalmCenter());
				component.MoveRotation(GetPalmRotation());
			}
			else
			{
				palm.position = GetPalmCenter();
				palm.rotation = GetPalmRotation();
			}
		}
		if ((Object)(object)forearm != (Object)null)
		{
			CapsuleCollider component2 = ((Component)forearm).GetComponent<CapsuleCollider>();
			if ((Object)(object)component2 != (Object)null)
			{
				component2.direction = 2;
				forearm.localScale = new Vector3(1f / ((Component)this).transform.lossyScale.x, 1f / ((Component)this).transform.lossyScale.y, 1f / ((Component)this).transform.lossyScale.z);
				component2.radius = GetArmWidth() / 2f;
				component2.height = GetArmLength() + GetArmWidth();
			}
			Rigidbody component3 = ((Component)forearm).GetComponent<Rigidbody>();
			if (Object.op_Implicit((Object)(object)component3))
			{
				component3.MovePosition(GetArmCenter());
				component3.MoveRotation(GetArmRotation());
			}
			else
			{
				forearm.position = GetArmCenter();
				forearm.rotation = GetArmRotation();
			}
		}
	}
}

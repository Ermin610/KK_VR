using System.Runtime.InteropServices;
using UnityEngine;

namespace Leap.Unity;

[Guid("a77b65c1-ae87-b436-881a-a63bc80f4894")]
public class RiggedFinger : FingerModel
{
	public bool deformPosition;

	public Vector3 modelFingerPointing = Vector3.forward;

	public Vector3 modelPalmFacing = -Vector3.up;

	private RiggedHand riggedHand;

	public Quaternion Reorientation()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		return Quaternion.Inverse(Quaternion.LookRotation(modelFingerPointing, -modelPalmFacing));
	}

	public override void UpdateFinger()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < bones.Length; i++)
		{
			if (bones[i] != null)
			{
				bones[i].rotation = GetBoneRotation(i) * Reorientation();
				if (deformPosition)
				{
					bones[i].position = GetJointPosition(i);
				}
			}
		}
	}

	public void SetupRiggedFinger(bool useMetaCarpals)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		findBoneTransforms(useMetaCarpals);
		modelFingerPointing = calulateModelFingerPointing();
	}

	private void findBoneTransforms(bool useMetaCarpals)
	{
		if (!useMetaCarpals || fingerType == Finger.FingerType.TYPE_THUMB)
		{
			bones[1] = ((Component)this).transform;
			bones[2] = ((Component)((Component)this).transform.GetChild(0)).transform;
			bones[3] = ((Component)((Component)((Component)this).transform.GetChild(0)).transform.GetChild(0)).transform;
		}
		else
		{
			bones[0] = ((Component)this).transform;
			bones[1] = ((Component)((Component)this).transform.GetChild(0)).transform;
			bones[2] = ((Component)((Component)((Component)this).transform.GetChild(0)).transform.GetChild(0)).transform;
			bones[3] = ((Component)((Component)((Component)((Component)this).transform.GetChild(0)).transform.GetChild(0)).transform.GetChild(0)).transform;
		}
	}

	private Vector3 calulateModelFingerPointing()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		return RiggedHand.CalculateZeroedVector(((Component)this).transform.InverseTransformPoint(((Component)this).transform.position) - ((Component)this).transform.InverseTransformPoint(((Component)((Component)this).transform.GetChild(0)).transform.position));
	}
}

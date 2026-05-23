using System.Runtime.InteropServices;
using UnityEngine;

namespace Leap.Unity;

[Guid("a77b65c1-ae87-b436-881a-a63bc80f4894")]
public class RiggedHand : HandModel
{
	[Tooltip("Hands are typically rigged in 3D packages with the palm transform near the wrist. Uncheck this is your model's palm transform is at the center of the palm similar to Leap's API drives")]
	public bool ModelPalmAtLeapWrist = true;

	public bool UseMetaCarpals;

	public Vector3 modelFingerPointing = new Vector3(0f, 0f, 0f);

	public Vector3 modelPalmFacing = new Vector3(0f, 0f, 0f);

	public override ModelType HandModelType => ModelType.Graphics;

	public override bool SupportsEditorPersistence()
	{
		return true;
	}

	public override void InitHand()
	{
		UpdateHand();
	}

	public Quaternion Reorientation()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		return Quaternion.Inverse(Quaternion.LookRotation(modelFingerPointing, -modelPalmFacing));
	}

	public override void UpdateHand()
	{
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)palm != (Object)null)
		{
			if (ModelPalmAtLeapWrist)
			{
				palm.position = GetWristPosition();
			}
			else
			{
				palm.position = GetPalmPosition();
				if (Object.op_Implicit((Object)(object)wristJoint))
				{
					wristJoint.position = GetWristPosition();
				}
			}
			palm.rotation = GetRiggedPalmRotation() * Reorientation();
		}
		if ((Object)(object)forearm != (Object)null)
		{
			forearm.rotation = GetArmRotation() * Reorientation();
		}
		for (int i = 0; i < fingers.Length; i++)
		{
			if ((Object)(object)fingers[i] != (Object)null)
			{
				fingers[i].fingerType = (Finger.FingerType)i;
				fingers[i].UpdateFinger();
			}
		}
	}

	public Quaternion GetRiggedPalmRotation()
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		if (hand_ != null)
		{
			LeapTransform basis = hand_.Basis;
			return CalculateRotation(basis);
		}
		if (Object.op_Implicit((Object)(object)palm))
		{
			return palm.rotation;
		}
		return Quaternion.identity;
	}

	private Quaternion CalculateRotation(LeapTransform trs)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = trs.yBasis.ToVector3();
		return Quaternion.LookRotation(trs.zBasis.ToVector3(), val);
	}

	[ContextMenu("Setup Rigged Hand")]
	public void SetupRiggedHand()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		modelFingerPointing = new Vector3(0f, 0f, 0f);
		modelPalmFacing = new Vector3(0f, 0f, 0f);
		findFingerModels();
		modelPalmFacing = calculateModelPalmFacing();
		modelFingerPointing = calculateModelFingerPointing();
		setFingerPalmFacing();
	}

	private void findFingerModels()
	{
		RiggedFinger[] componentsInChildren = ((Component)this).GetComponentsInChildren<RiggedFinger>();
		for (int i = 0; i < 5; i++)
		{
			int num = componentsInChildren[i].fingerType.indexOf();
			fingers[num] = componentsInChildren[i];
			componentsInChildren[i].SetupRiggedFinger(UseMetaCarpals);
		}
	}

	private void setFingerPalmFacing()
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		RiggedFinger[] componentsInChildren = ((Component)this).GetComponentsInChildren<RiggedFinger>();
		for (int i = 0; i < 5; i++)
		{
			int num = componentsInChildren[i].fingerType.indexOf();
			fingers[num] = componentsInChildren[i];
			componentsInChildren[i].modelPalmFacing = modelPalmFacing;
		}
	}

	private Vector3 calculateModelPalmFacing()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = ((Component)this).transform.InverseTransformPoint(palm.position);
		Vector3 val2 = ((Component)this).transform.InverseTransformPoint(((Component)fingers[2]).transform.position);
		Vector3 val3 = ((Component)this).transform.InverseTransformPoint(((Component)fingers[1]).transform.position);
		Vector3 val4 = val2 - val;
		Vector3 val5 = val3 - val;
		Vector3 vectorToZero = ((Handedness != 0) ? Vector3.Cross(val4, val5) : Vector3.Cross(val5, val4));
		return CalculateZeroedVector(vectorToZero);
	}

	private Vector3 calculateModelFingerPointing()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		return CalculateZeroedVector(((Component)this).transform.InverseTransformPoint(((Component)((Component)fingers[2]).transform.GetChild(0)).transform.position) - ((Component)this).transform.InverseTransformPoint(palm.position));
	}

	public static Vector3 CalculateZeroedVector(Vector3 vectorToZero)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		Vector3 result = default(Vector3);
		float num = Mathf.Max(new float[3]
		{
			Mathf.Abs(vectorToZero.x),
			Mathf.Abs(vectorToZero.y),
			Mathf.Abs(vectorToZero.z)
		});
		if (Mathf.Abs(vectorToZero.x) == num)
		{
			result = ((vectorToZero.x < 0f) ? new Vector3(1f, 0f, 0f) : new Vector3(-1f, 0f, 0f));
		}
		if (Mathf.Abs(vectorToZero.y) == num)
		{
			result = ((vectorToZero.y < 0f) ? new Vector3(0f, 1f, 0f) : new Vector3(0f, -1f, 0f));
		}
		if (Mathf.Abs(vectorToZero.z) == num)
		{
			result = ((vectorToZero.y < 0f) ? new Vector3(0f, 0f, 1f) : new Vector3(0f, 0f, -1f));
		}
		return result;
	}
}

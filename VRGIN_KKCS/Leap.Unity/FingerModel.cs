using UnityEngine;

namespace Leap.Unity;

public abstract class FingerModel : MonoBehaviour
{
	public const int NUM_BONES = 4;

	public const int NUM_JOINTS = 3;

	public Finger.FingerType fingerType = Finger.FingerType.TYPE_INDEX;

	public Transform[] bones = (Transform[])(object)new Transform[4];

	public Transform[] joints = (Transform[])(object)new Transform[3];

	protected Hand hand_;

	protected Finger finger_;

	public void SetLeapHand(Hand hand)
	{
		hand_ = hand;
		if (hand_ != null)
		{
			finger_ = hand.Fingers[(int)fingerType];
		}
	}

	public Hand GetLeapHand()
	{
		return hand_;
	}

	public Finger GetLeapFinger()
	{
		return finger_;
	}

	public virtual void InitFinger()
	{
		UpdateFinger();
	}

	public abstract void UpdateFinger();

	public Vector3 GetTipPosition()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		if (finger_ != null)
		{
			return finger_.Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
		}
		if (Object.op_Implicit((Object)(object)bones[3]) && Object.op_Implicit((Object)(object)joints[1]))
		{
			return 2f * bones[3].position - joints[1].position;
		}
		return Vector3.zero;
	}

	public Vector3 GetJointPosition(int joint)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		if (joint >= 4)
		{
			return GetTipPosition();
		}
		if (finger_ != null)
		{
			return finger_.Bone((Bone.BoneType)joint).PrevJoint.ToVector3();
		}
		if (Object.op_Implicit((Object)(object)joints[joint]))
		{
			return joints[joint].position;
		}
		return Vector3.zero;
	}

	public Ray GetRay()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		return new Ray(GetTipPosition(), GetBoneDirection(3));
	}

	public Vector3 GetBoneCenter(int bone_type)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		if (finger_ != null)
		{
			return finger_.Bone((Bone.BoneType)bone_type).Center.ToVector3();
		}
		if (Object.op_Implicit((Object)(object)bones[bone_type]))
		{
			return bones[bone_type].position;
		}
		return Vector3.zero;
	}

	public Vector3 GetBoneDirection(int bone_type)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		if (finger_ != null)
		{
			Vector3 val = GetJointPosition(bone_type + 1) - GetJointPosition(bone_type);
			return ((Vector3)(ref val)).normalized;
		}
		if (Object.op_Implicit((Object)(object)bones[bone_type]))
		{
			return bones[bone_type].forward;
		}
		return Vector3.forward;
	}

	public Quaternion GetBoneRotation(int bone_type)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		if (finger_ != null)
		{
			return finger_.Bone((Bone.BoneType)bone_type).Rotation.ToQuaternion();
		}
		if (Object.op_Implicit((Object)(object)bones[bone_type]))
		{
			return bones[bone_type].rotation;
		}
		return Quaternion.identity;
	}

	public float GetBoneLength(int bone_type)
	{
		return finger_.Bone((Bone.BoneType)bone_type).Length;
	}

	public float GetBoneWidth(int bone_type)
	{
		return finger_.Bone((Bone.BoneType)bone_type).Width;
	}

	public float GetFingerJointStretchMecanim(int joint_type)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		Quaternion val = Quaternion.identity;
		if (finger_ != null)
		{
			val = Quaternion.Inverse(finger_.Bone((Bone.BoneType)joint_type).Rotation.ToQuaternion()) * finger_.Bone((Bone.BoneType)(joint_type + 1)).Rotation.ToQuaternion();
		}
		else if (Object.op_Implicit((Object)(object)bones[joint_type]) && Object.op_Implicit((Object)(object)bones[joint_type + 1]))
		{
			val = Quaternion.Inverse(GetBoneRotation(joint_type)) * GetBoneRotation(joint_type + 1);
		}
		float num = 0f - ((Quaternion)(ref val)).eulerAngles.x;
		if (num <= -180f)
		{
			num += 360f;
		}
		return num;
	}

	public float GetFingerJointSpreadMecanim()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		Quaternion val = Quaternion.identity;
		if (finger_ != null)
		{
			val = Quaternion.Inverse(finger_.Bone(Bone.BoneType.TYPE_METACARPAL).Rotation.ToQuaternion()) * finger_.Bone(Bone.BoneType.TYPE_PROXIMAL).Rotation.ToQuaternion();
		}
		else if (Object.op_Implicit((Object)(object)bones[0]) && Object.op_Implicit((Object)(object)bones[1]))
		{
			val = Quaternion.Inverse(GetBoneRotation(0)) * GetBoneRotation(1);
		}
		float num = 0f;
		Finger.FingerType fingerType = this.fingerType;
		if (finger_ != null)
		{
			this.fingerType = finger_.Type;
		}
		if (fingerType == Finger.FingerType.TYPE_INDEX || fingerType == Finger.FingerType.TYPE_MIDDLE)
		{
			num = ((Quaternion)(ref val)).eulerAngles.y;
			if (num > 180f)
			{
				num -= 360f;
			}
		}
		if (fingerType == Finger.FingerType.TYPE_THUMB || fingerType == Finger.FingerType.TYPE_RING || fingerType == Finger.FingerType.TYPE_PINKY)
		{
			num = 0f - ((Quaternion)(ref val)).eulerAngles.y;
			if (num <= -180f)
			{
				num += 360f;
			}
		}
		return num;
	}
}

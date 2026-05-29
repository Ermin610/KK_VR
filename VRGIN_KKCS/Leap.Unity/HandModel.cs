using UnityEngine;

namespace Leap.Unity;

public abstract class HandModel : IHandModel
{
	[SerializeField]
	public Chirality handedness;

	private ModelType handModelType;

	public const int NUM_FINGERS = 5;

	public float handModelPalmWidth = 0.085f;

	public FingerModel[] fingers = new FingerModel[5];

	public Transform palm;

	public Transform forearm;

	public Transform wristJoint;

	public Transform elbowJoint;

	protected Hand hand_;

	public override Chirality Handedness => handedness;

	public abstract override ModelType HandModelType { get; }

	public Vector3 GetPalmPosition()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		return hand_.PalmPosition.ToVector3();
	}

	public Quaternion GetPalmRotation()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		if (hand_ != null)
		{
			return hand_.Basis.CalculateRotation();
		}
		if ((palm != null))
		{
			return palm.rotation;
		}
		return Quaternion.identity;
	}

	public Vector3 GetPalmDirection()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		if (hand_ != null)
		{
			return hand_.Direction.ToVector3();
		}
		if ((palm != null))
		{
			return palm.forward;
		}
		return Vector3.forward;
	}

	public Vector3 GetPalmNormal()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		if (hand_ != null)
		{
			return hand_.PalmNormal.ToVector3();
		}
		if ((palm != null))
		{
			return -palm.up;
		}
		return -Vector3.up;
	}

	public Vector3 GetArmDirection()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		if (hand_ != null)
		{
			return hand_.Arm.Direction.ToVector3();
		}
		if ((forearm != null))
		{
			return forearm.forward;
		}
		return Vector3.forward;
	}

	public Vector3 GetArmCenter()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		if (hand_ != null)
		{
			return (0.5f * (hand_.Arm.WristPosition + hand_.Arm.ElbowPosition)).ToVector3();
		}
		if ((forearm != null))
		{
			return forearm.position;
		}
		return Vector3.zero;
	}

	public float GetArmLength()
	{
		return (hand_.Arm.WristPosition - hand_.Arm.ElbowPosition).Magnitude;
	}

	public float GetArmWidth()
	{
		return hand_.Arm.Width;
	}

	public Vector3 GetElbowPosition()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		if (hand_ != null)
		{
			return hand_.Arm.ElbowPosition.ToVector3();
		}
		if ((elbowJoint != null))
		{
			return elbowJoint.position;
		}
		return Vector3.zero;
	}

	public Vector3 GetWristPosition()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		if (hand_ != null)
		{
			return hand_.Arm.WristPosition.ToVector3();
		}
		if ((wristJoint != null))
		{
			return wristJoint.position;
		}
		return Vector3.zero;
	}

	public Quaternion GetArmRotation()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		if (hand_ != null)
		{
			return hand_.Arm.Rotation.ToQuaternion();
		}
		if ((forearm != null))
		{
			return forearm.rotation;
		}
		return Quaternion.identity;
	}

	public override Hand GetLeapHand()
	{
		return hand_;
	}

	public override void SetLeapHand(Hand hand)
	{
		hand_ = hand;
		for (int i = 0; i < fingers.Length; i++)
		{
			if (fingers[i] != null)
			{
				fingers[i].SetLeapHand(hand_);
			}
		}
	}

	public override void InitHand()
	{
		for (int i = 0; i < fingers.Length; i++)
		{
			if (fingers[i] != null)
			{
				fingers[i].fingerType = (Finger.FingerType)i;
				fingers[i].InitFinger();
			}
		}
	}

	public int LeapID()
	{
		if (hand_ != null)
		{
			return hand_.Id;
		}
		return -1;
	}

	public abstract override void UpdateHand();
}

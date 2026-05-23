using UnityEngine;

namespace Leap.Unity;

public class HandAttachments : IHandModel
{
	[Tooltip("The palm of the hand.")]
	public Transform Palm;

	[Tooltip("The center of the forearm.")]
	public Transform Arm;

	[Tooltip("The tip of the thumb.")]
	public Transform Thumb;

	[Tooltip("The pont between the thumb and index finger.")]
	public Transform PinchPoint;

	[Tooltip("The tip of the index finger.")]
	public Transform Index;

	[Tooltip("The tip of the middle finger.")]
	public Transform Middle;

	[Tooltip("The tip of the ring finger.")]
	public Transform Ring;

	[Tooltip("The tip of the little finger.")]
	public Transform Pinky;

	[Tooltip("The point midway between the finger tips.")]
	public Transform GrabPoint;

	private Hand _hand;

	[Tooltip("Whether to use this for right or left hands")]
	[SerializeField]
	public Chirality _handedness;

	public override ModelType HandModelType => ModelType.Graphics;

	public override Chirality Handedness => _handedness;

	public override void SetLeapHand(Hand hand)
	{
		_hand = hand;
	}

	public override Hand GetLeapHand()
	{
		return _hand;
	}

	public override void UpdateHand()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_034d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0354: Unknown result type (might be due to invalid IL or missing references)
		//IL_0359: Unknown result type (might be due to invalid IL or missing references)
		//IL_0448: Unknown result type (might be due to invalid IL or missing references)
		//IL_045a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0461: Unknown result type (might be due to invalid IL or missing references)
		//IL_0466: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)Palm != (Object)null)
		{
			Palm.position = _hand.PalmPosition.ToVector3();
			Palm.rotation = _hand.Basis.rotation.ToQuaternion();
		}
		if ((Object)(object)Arm != (Object)null)
		{
			Arm.position = _hand.Arm.Center.ToVector3();
			Arm.rotation = _hand.Arm.Basis.rotation.ToQuaternion();
		}
		if ((Object)(object)Thumb != (Object)null)
		{
			Thumb.position = _hand.Fingers[0].Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
			Thumb.rotation = _hand.Fingers[0].Bone(Bone.BoneType.TYPE_DISTAL).Rotation.ToQuaternion();
		}
		if ((Object)(object)Index != (Object)null)
		{
			Index.position = _hand.Fingers[1].Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
			Index.rotation = _hand.Fingers[1].Bone(Bone.BoneType.TYPE_DISTAL).Rotation.ToQuaternion();
		}
		if ((Object)(object)Middle != (Object)null)
		{
			Middle.position = _hand.Fingers[2].Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
			Middle.rotation = _hand.Fingers[2].Bone(Bone.BoneType.TYPE_DISTAL).Rotation.ToQuaternion();
		}
		if ((Object)(object)Ring != (Object)null)
		{
			Ring.position = _hand.Fingers[3].Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
			Ring.rotation = _hand.Fingers[3].Bone(Bone.BoneType.TYPE_DISTAL).Rotation.ToQuaternion();
		}
		if ((Object)(object)Pinky != (Object)null)
		{
			Pinky.position = _hand.Fingers[4].Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
			Pinky.rotation = _hand.Fingers[4].Bone(Bone.BoneType.TYPE_DISTAL).Rotation.ToQuaternion();
		}
		if ((Object)(object)PinchPoint != (Object)null)
		{
			Vector tipPosition = _hand.Fingers[0].TipPosition;
			Vector tipPosition2 = _hand.Fingers[1].TipPosition;
			Vector vector = Vector.Lerp(tipPosition, tipPosition2, 0.5f);
			PinchPoint.position = vector.ToVector3();
			Vector vector2 = vector - _hand.Fingers[1].Bone(Bone.BoneType.TYPE_PROXIMAL).PrevJoint;
			Vector vector3 = _hand.Fingers[1].Bone(Bone.BoneType.TYPE_PROXIMAL).Direction.Cross(vector2);
			PinchPoint.rotation = Quaternion.LookRotation(vector2.ToVector3(), vector3.ToVector3());
		}
		if ((Object)(object)GrabPoint != (Object)null)
		{
			Vector zero = Vector.Zero;
			for (int i = 0; i < _hand.Fingers.Count; i++)
			{
				zero += _hand.Fingers[i].TipPosition;
			}
			Vector normalized = (_hand.Fingers[2].TipPosition - _hand.WristPosition).Normalized;
			Vector other = _hand.Fingers[0].TipPosition - _hand.Fingers[4].TipPosition;
			Vector normalized2 = normalized.Cross(other).Normalized;
			zero /= 5f;
			GrabPoint.position = zero.ToVector3();
			GrabPoint.rotation = Quaternion.LookRotation(normalized.ToVector3(), normalized2.ToVector3());
		}
	}
}

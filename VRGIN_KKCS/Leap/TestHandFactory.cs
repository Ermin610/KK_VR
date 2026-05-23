using System;
using System.Collections.Generic;
using Leap.Unity;
using UnityEngine;

namespace Leap;

public class TestHandFactory
{
	public static Frame MakeTestFrame(int frameId, bool leftHandIncluded, bool rightHandIncluded)
	{
		Frame frame = new Frame(frameId, 0L, 120f, new InteractionBox(), new List<Hand>());
		if (leftHandIncluded)
		{
			frame.Hands.Add(MakeTestHand(frameId, 10, isLeft: true));
		}
		if (rightHandIncluded)
		{
			frame.Hands.Add(MakeTestHand(frameId, 20, isLeft: false));
		}
		return frame;
	}

	public static Hand MakeTestHand(int frameId, int handId, bool isLeft)
	{
		List<Finger> list = new List<Finger>(5);
		list.Add(MakeThumb(frameId, handId, isLeft));
		list.Add(MakeIndexFinger(frameId, handId, isLeft));
		list.Add(MakeMiddleFinger(frameId, handId, isLeft));
		list.Add(MakeRingFinger(frameId, handId, isLeft));
		list.Add(MakePinky(frameId, handId, isLeft));
		Vector vector = new Vector(-7.0580993f, 4f, 50f);
		Vector vector2 = vector + 250f * Vector.Backward;
		Arm arm = new Arm(vector2, vector, (vector2 + vector) / 2f, Vector.Forward, 250f, 41f, LeapQuaternion.Identity);
		Hand hand = new Hand(frameId, handId, 1f, 0f, 0f, 0f, 0f, 85f, isLeft, 0f, arm, list, new Vector(0f, 0f, 0f), new Vector(0f, 0f, 0f), new Vector(0f, 0f, 0f), Vector.Down, Vector.Forward, new Vector(-4.3638577f, 6.5f, 31.011135f));
		LeapTransform identity = LeapTransform.Identity;
		identity.rotation = AngleAxis((float)Math.PI, Vector.Forward);
		if (isLeft)
		{
			identity.translation = new Vector(80f, 120f, 0f);
		}
		else
		{
			identity.translation = new Vector(-80f, 120f, 0f);
			identity.MirrorX();
		}
		return hand.TransformedCopy(identity);
	}

	private static LeapQuaternion AngleAxis(float angle, Vector axis)
	{
		if (!axis.MagnitudeSquared.NearlyEquals(1f))
		{
			throw new ArgumentException("Axis must be a unit vector.");
		}
		float num = Mathf.Sin(angle / 2f);
		return new LeapQuaternion(num * axis.x, num * axis.y, num * axis.z, Mathf.Cos(angle / 2f)).Normalized;
	}

	private static LeapQuaternion RotationBetween(Vector fromDirection, Vector toDirection)
	{
		float num = Mathf.Sqrt(2f + 2f * fromDirection.Dot(toDirection));
		Vector vector = 1f / num * fromDirection.Cross(toDirection);
		return new LeapQuaternion(vector.x, vector.y, vector.z, 0.5f * num);
	}

	private static Finger MakeThumb(int frameId, int handId, bool isLeft)
	{
		Vector position = new Vector(19.33826f, -6f, 53.168484f);
		Vector forward = new Vector(0.6363291f, -0.5f, -0.8997871f);
		Vector up = new Vector(0.80479395f, 0.44721392f, 0.39026454f);
		float[] jointLengths = new float[4] { 0f, 46.22f, 31.57f, 21.67f };
		return MakeFinger(Finger.FingerType.TYPE_THUMB, position, forward, up, jointLengths, frameId, handId, handId, isLeft);
	}

	private static Finger MakeIndexFinger(int frameId, int handId, bool isLeft)
	{
		Vector position = new Vector(23.181286f, 2f, -23.149345f);
		Vector forward = new Vector(0.16604431f, -0.14834045f, -0.97489715f);
		Vector up = new Vector(0.024906646f, 0.98893636f, -0.14623457f);
		float[] jointLengths = new float[4] { 68.12f, 39.78f, 22.38f, 15.82f };
		return MakeFinger(Finger.FingerType.TYPE_INDEX, position, forward, up, jointLengths, frameId, handId, handId + 1, isLeft);
	}

	private static Finger MakeMiddleFinger(int frameId, int handId, bool isLeft)
	{
		Vector position = new Vector(2.7887783f, 4f, -23.252106f);
		Vector forward = new Vector(0.029520785f, -0.14834045f, -0.98849565f);
		Vector up = new Vector(-0.14576527f, 0.97771597f, -0.15107597f);
		float[] jointLengths = new float[4] { 64.6f, 44.63f, 26.33f, 17.4f };
		return MakeFinger(Finger.FingerType.TYPE_MIDDLE, position, forward, up, jointLengths, frameId, handId, handId + 2, isLeft);
	}

	private static Finger MakeRingFinger(int frameId, int handId, bool isLeft)
	{
		Vector position = new Vector(-17.447168f, 4f, -17.279144f);
		Vector forward = new Vector(-0.12131794f, -0.14834034f, -0.9814668f);
		Vector up = new Vector(-0.21691047f, 0.96883494f, -0.1196191f);
		float[] jointLengths = new float[4] { 58f, 41.37f, 25.65f, 17.3f };
		return MakeFinger(Finger.FingerType.TYPE_RING, position, forward, up, jointLengths, frameId, handId, handId + 3, isLeft);
	}

	private static Finger MakePinky(int frameId, int handId, bool isLeft)
	{
		Vector position = new Vector(-35.33744f, 0f, -9.728714f);
		Vector forward = new Vector(-277f / (340f * (float)Math.PI), -0.105851226f, -0.95997083f);
		Vector up = new Vector(-0.35335022f, 0.9354595f, -0.007693566f);
		float[] jointLengths = new float[4] { 53.69f, 32.74f, 18.11f, 15.96f };
		return MakeFinger(Finger.FingerType.TYPE_PINKY, position, forward, up, jointLengths, frameId, handId, handId + 4, isLeft);
	}

	private static Finger MakeFinger(Finger.FingerType name, Vector position, Vector forward, Vector up, float[] jointLengths, int frameId, int handId, int fingerId, bool isLeft)
	{
		forward = forward.Normalized;
		up = up.Normalized;
		Bone[] array = new Bone[5];
		float num = 0f - jointLengths[0];
		Bone bone = MakeBone(Bone.BoneType.TYPE_METACARPAL, position + forward * num, jointLengths[0], 8f, forward, up, isLeft);
		num += jointLengths[0];
		array[0] = bone;
		Bone bone2 = MakeBone(Bone.BoneType.TYPE_PROXIMAL, position + forward * num, jointLengths[1], 8f, forward, up, isLeft);
		num += jointLengths[1];
		array[1] = bone2;
		Bone bone3 = MakeBone(Bone.BoneType.TYPE_INTERMEDIATE, position + forward * num, jointLengths[2], 8f, forward, up, isLeft);
		num += jointLengths[2];
		array[2] = bone3;
		Bone bone4 = MakeBone(Bone.BoneType.TYPE_DISTAL, position + forward * num, jointLengths[3], 8f, forward, up, isLeft);
		array[3] = bone4;
		return new Finger(frameId, handId, fingerId, 0f, position, new Vector(0f, 0f, 0f), forward, position, 8f, jointLengths[1] + jointLengths[2] + jointLengths[3], isExtended: true, name, array[0], array[1], array[2], array[3]);
	}

	private static Bone MakeBone(Bone.BoneType name, Vector proximalPosition, float length, float width, Vector direction, Vector up, bool isLeft)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		LeapQuaternion rotation = Quaternion.LookRotation(-direction.ToVector3(), up.ToVector3()).ToLeapQuaternion();
		return new Bone(proximalPosition, proximalPosition + direction * length, Vector.Lerp(proximalPosition, proximalPosition + direction * length, 0.5f), direction, length, width, name, rotation);
	}
}

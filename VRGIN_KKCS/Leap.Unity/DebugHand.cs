using UnityEngine;

namespace Leap.Unity;

public class DebugHand : IHandModel
{
	private Hand hand_;

	[SerializeField]
	private bool visualizeBasis = true;

	protected Color[] colors = (Color[])(object)new Color[4]
	{
		Color.gray,
		Color.yellow,
		Color.cyan,
		Color.magenta
	};

	[SerializeField]
	private Chirality handedness;

	public bool VisualizeBasis
	{
		get
		{
			return visualizeBasis;
		}
		set
		{
			visualizeBasis = value;
		}
	}

	public override ModelType HandModelType => ModelType.Graphics;

	public override Chirality Handedness => handedness;

	public override Hand GetLeapHand()
	{
		return hand_;
	}

	public override void SetLeapHand(Hand hand)
	{
		hand_ = hand;
	}

	public override bool SupportsEditorPersistence()
	{
		return true;
	}

	public override void InitHand()
	{
		DrawDebugLines();
	}

	public override void UpdateHand()
	{
		DrawDebugLines();
	}

	protected void DrawDebugLines()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		Hand leapHand = GetLeapHand();
		Debug.DrawLine(leapHand.Arm.ElbowPosition.ToVector3(), leapHand.Arm.WristPosition.ToVector3(), Color.red);
		Debug.DrawLine(leapHand.WristPosition.ToVector3(), leapHand.PalmPosition.ToVector3(), Color.white);
		Debug.DrawLine(leapHand.PalmPosition.ToVector3(), (leapHand.PalmPosition + leapHand.PalmNormal * leapHand.PalmWidth / 2f).ToVector3(), Color.black);
		if (VisualizeBasis)
		{
			DrawBasis(leapHand.PalmPosition, leapHand.Basis, leapHand.PalmWidth / 4f);
			DrawBasis(leapHand.Arm.ElbowPosition, leapHand.Arm.Basis, 0.01f);
		}
		for (int i = 0; i < 5; i++)
		{
			Finger finger = leapHand.Fingers[i];
			for (int j = 0; j < 4; j++)
			{
				Bone bone = finger.Bone((Bone.BoneType)j);
				Debug.DrawLine(bone.PrevJoint.ToVector3(), bone.PrevJoint.ToVector3() + bone.Direction.ToVector3() * bone.Length, colors[j]);
				if (VisualizeBasis)
				{
					DrawBasis(bone.PrevJoint, bone.Basis, 0.01f);
				}
			}
		}
	}

	public void DrawBasis(Vector position, LeapTransform basis, float scale)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = position.ToVector3();
		Debug.DrawLine(val, val + basis.xBasis.ToVector3() * scale, Color.red);
		Debug.DrawLine(val, val + basis.yBasis.ToVector3() * scale, Color.green);
		Debug.DrawLine(val, val + basis.zBasis.ToVector3() * scale, Color.blue);
	}
}

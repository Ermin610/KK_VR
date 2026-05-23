using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity;

public class MinimalHand : IHandModel
{
	[SerializeField]
	private Mesh _palmMesh;

	[SerializeField]
	private float _palmScale = 0.02f;

	[SerializeField]
	private Material _palmMat;

	[SerializeField]
	private Mesh _jointMesh;

	[SerializeField]
	private float _jointScale = 0.01f;

	[SerializeField]
	private Material _jointMat;

	private Hand _hand;

	private Transform _palm;

	private Transform[] _joints;

	public override Chirality Handedness => Chirality.Either;

	public override ModelType HandModelType => ModelType.Graphics;

	public override bool SupportsEditorPersistence()
	{
		return true;
	}

	public override void SetLeapHand(Hand hand)
	{
		_hand = hand;
	}

	public override Hand GetLeapHand()
	{
		return _hand;
	}

	public override void InitHand()
	{
		_joints = (Transform[])(object)new Transform[20];
		for (int i = 0; i < 20; i++)
		{
			_joints[i] = createRenderer("Joint", _jointMesh, _jointScale, _jointMat);
		}
		_palm = createRenderer("Palm", _palmMesh, _palmScale, _palmMat);
	}

	public override void UpdateHand()
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		List<Finger> fingers = _hand.Fingers;
		int num = 0;
		for (int i = 0; i < 5; i++)
		{
			Finger finger = fingers[i];
			for (int j = 0; j < 4; j++)
			{
				_joints[num++].position = finger.Bone((Bone.BoneType)j).NextJoint.ToVector3();
			}
		}
		_palm.position = _hand.PalmPosition.ToVector3();
	}

	private Transform createRenderer(string name, Mesh mesh, float scale, Material mat)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name);
		val.AddComponent<MeshFilter>().mesh = mesh;
		((Renderer)val.AddComponent<MeshRenderer>()).sharedMaterial = mat;
		val.transform.parent = ((Component)this).transform;
		val.transform.localScale = Vector3.one * scale;
		return val.transform;
	}
}

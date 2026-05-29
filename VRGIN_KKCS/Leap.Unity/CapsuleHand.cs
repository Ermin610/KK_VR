using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
using VRGIN.Core;

namespace Leap.Unity;

public class CapsuleHand : IHandModel
{
	private const int THUMB_BASE_INDEX = 0;

	private const int PINKY_BASE_INDEX = 16;

	private const float SPHERE_RADIUS = 0.008f;

	private const float CYLINDER_RADIUS = 0.006f;

	private const float PALM_RADIUS = 0.015f;

	private static int _leftColorIndex = 0;

	private static int _rightColorIndex = 0;

	private static Color[] _leftColorList = (Color[])(object)new Color[3]
	{
		new Color(0f, 0f, 1f),
		new Color(0.2f, 0f, 0.4f),
		new Color(0f, 0.2f, 0.2f)
	};

	private static Color[] _rightColorList = (Color[])(object)new Color[3]
	{
		new Color(1f, 0f, 0f),
		new Color(1f, 1f, 0f),
		new Color(1f, 0.5f, 0f)
	};

	[SerializeField]
	public Chirality handedness;

	[SerializeField]
	public bool _showArm = true;

	[SerializeField]
	public Material _material;

	[SerializeField]
	public Mesh _sphereMesh;

	[SerializeField]
	private int _cylinderResolution = 12;

	private bool _hasGeneratedMeshes;

	private Material jointMat;

	[SerializeField]
	[HideInInspector]
	private List<Transform> _serializedTransforms;

	public Transform[] _jointSpheres;

	private Transform mockThumbJointSphere;

	private Transform palmPositionSphere;

	private Transform wristPositionSphere;

	private List<Renderer> _armRenderers;

	private List<Transform> _cylinderTransforms;

	private List<Transform> _sphereATransforms;

	private List<Transform> _sphereBTransforms;

	private Transform armFrontLeft;

	private Transform armFrontRight;

	private Transform armBackLeft;

	private Transform armBackRight;

	private Hand hand_;

	private static MethodInfo _SetVertices = typeof(Mesh).GetMethod("SetVertices", BindingFlags.Instance | BindingFlags.Public);

	private static PropertyInfo _Vertices = typeof(Mesh).GetProperty("vertices", BindingFlags.Instance | BindingFlags.Public);

	public override ModelType HandModelType => ModelType.Graphics;

	public override Chirality Handedness => handedness;

	public override bool SupportsEditorPersistence()
	{
		return true;
	}

	public override Hand GetLeapHand()
	{
		return hand_;
	}

	public override void SetLeapHand(Hand hand)
	{
		hand_ = hand;
	}

	private void OnValidate()
	{
		_cylinderResolution = Mathf.Max(3, _cylinderResolution);
		if (_armRenderers != null)
		{
			updateArmVisibility();
		}
	}

	public override void InitHand()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		if (_material != null)
		{
			jointMat = new Material(_material);
		}
		if (_serializedTransforms != null)
		{
			for (int i = 0; i < _serializedTransforms.Count; i++)
			{
				Transform val = _serializedTransforms[i];
				if (val != null)
				{
					Object.DestroyImmediate((Object)(object)((Component)val).gameObject);
				}
			}
			_serializedTransforms.Clear();
		}
		else
		{
			_serializedTransforms = new List<Transform>();
		}
		_jointSpheres = (Transform[])(object)new Transform[20];
		_armRenderers = new List<Renderer>();
		_cylinderTransforms = new List<Transform>();
		_sphereATransforms = new List<Transform>();
		_sphereBTransforms = new List<Transform>();
		createSpheres();
		createCylinders();
		updateArmVisibility();
		_hasGeneratedMeshes = false;
	}

	public override void BeginHand()
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		base.BeginHand();
		if (hand_.IsLeft)
		{
			jointMat.color = _leftColorList[_leftColorIndex];
			_leftColorIndex = (_leftColorIndex + 1) % _leftColorList.Length;
		}
		else
		{
			jointMat.color = _rightColorList[_rightColorIndex];
			_rightColorIndex = (_rightColorIndex + 1) % _rightColorList.Length;
		}
	}

	public override void UpdateHand()
	{
		updateSpheres();
		if (_showArm)
		{
			updateArm();
		}
		updateCylinders();
	}

	private void updateSpheres()
	{
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		List<Finger> fingers = hand_.Fingers;
		for (int i = 0; i < fingers.Count; i++)
		{
			Finger finger = fingers[i];
			for (int j = 0; j < 4; j++)
			{
				int fingerJointIndex = getFingerJointIndex((int)finger.Type, j);
				_jointSpheres[fingerJointIndex].position = finger.Bone((Bone.BoneType)j).NextJoint.ToVector3();
			}
		}
		palmPositionSphere.position = hand_.PalmPosition.ToVector3();
		Vector3 position = hand_.PalmPosition.ToVector3();
		wristPositionSphere.position = position;
		Vector3 val = _jointSpheres[0].position - hand_.PalmPosition.ToVector3();
		mockThumbJointSphere.position = hand_.PalmPosition.ToVector3() + Vector3.Reflect(val, hand_.Basis.xBasis.ToVector3());
	}

	private void updateArm()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		Arm arm = hand_.Arm;
		Vector3 val = arm.Basis.xBasis.ToVector3() * arm.Width * 0.7f * 0.5f;
		Vector3 val2 = arm.WristPosition.ToVector3();
		Vector3 val3 = arm.ElbowPosition.ToVector3();
		float num = Vector3.Distance(val2, val3);
		val2 -= arm.Direction.ToVector3() * num * 0.05f;
		armFrontRight.position = val2 + val;
		armFrontLeft.position = val2 - val;
		armBackRight.position = val3 + val;
		armBackLeft.position = val3 - val;
	}

	private void updateCylinders()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < _cylinderTransforms.Count; i++)
		{
			Transform val = _cylinderTransforms[i];
			Transform val2 = _sphereATransforms[i];
			Transform val3 = _sphereBTransforms[i];
			Vector3 val4 = val2.position - val3.position;
			if (!_hasGeneratedMeshes)
			{
				((Component)val).GetComponent<MeshFilter>().sharedMesh = generateCylinderMesh(val4.magnitude / ((Component)this).transform.lossyScale.x);
			}
			val.position = val2.position;
			if (!(val4.sqrMagnitude <= Mathf.Epsilon))
			{
				val.LookAt(val3);
			}
		}
		_hasGeneratedMeshes = true;
	}

	private void updateArmVisibility()
	{
		for (int i = 0; i < _armRenderers.Count; i++)
		{
			_armRenderers[i].enabled = _showArm;
		}
	}

	private void createSpheres()
	{
		List<Finger> fingers = hand_.Fingers;
		for (int i = 0; i < fingers.Count; i++)
		{
			Finger finger = fingers[i];
			for (int j = 0; j < 4; j++)
			{
				int fingerJointIndex = getFingerJointIndex((int)finger.Type, j);
				_jointSpheres[fingerJointIndex] = createSphere("Joint", 0.008f);
			}
		}
		mockThumbJointSphere = createSphere("MockJoint", 0.008f);
		palmPositionSphere = createSphere("PalmPosition", 0.015f);
		wristPositionSphere = createSphere("WristPosition", 0.008f);
		armFrontLeft = createSphere("ArmFrontLeft", 0.008f, isPartOfArm: true);
		armFrontRight = createSphere("ArmFrontRight", 0.008f, isPartOfArm: true);
		armBackLeft = createSphere("ArmBackLeft", 0.008f, isPartOfArm: true);
		armBackRight = createSphere("ArmBackRight", 0.008f, isPartOfArm: true);
	}

	private void createCylinders()
	{
		for (int i = 0; i < 5; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				int fingerJointIndex = getFingerJointIndex(i, j);
				int fingerJointIndex2 = getFingerJointIndex(i, j + 1);
				Transform jointA = _jointSpheres[fingerJointIndex];
				Transform jointB = _jointSpheres[fingerJointIndex2];
				createCylinder("Finger Joint", jointA, jointB);
			}
		}
		for (int k = 0; k < 4; k++)
		{
			int fingerJointIndex3 = getFingerJointIndex(k, 0);
			int fingerJointIndex4 = getFingerJointIndex(k + 1, 0);
			Transform jointA2 = _jointSpheres[fingerJointIndex3];
			Transform jointB2 = _jointSpheres[fingerJointIndex4];
			createCylinder("Hand Joints", jointA2, jointB2);
		}
		Transform jointA3 = _jointSpheres[0];
		Transform jointA4 = _jointSpheres[16];
		createCylinder("Hand Bottom", jointA3, mockThumbJointSphere);
		createCylinder("Hand Side", jointA4, mockThumbJointSphere);
		createCylinder("ArmFront", armFrontLeft, armFrontRight, isPartOfArm: true);
		createCylinder("ArmBack", armBackLeft, armBackRight, isPartOfArm: true);
		createCylinder("ArmLeft", armFrontLeft, armBackLeft, isPartOfArm: true);
		createCylinder("ArmRight", armFrontRight, armBackRight, isPartOfArm: true);
	}

	private int getFingerJointIndex(int fingerIndex, int jointIndex)
	{
		return fingerIndex * 4 + jointIndex;
	}

	private Transform createSphere(string name, float radius, bool isPartOfArm = false)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name);
		_serializedTransforms.Add(val.transform);
		val.AddComponent<MeshFilter>().mesh = _sphereMesh;
		((Renderer)val.AddComponent<MeshRenderer>()).sharedMaterial = jointMat;
		val.transform.parent = ((Component)this).transform;
		val.transform.localScale = Vector3.one * radius * 2f;
		((Object)val).hideFlags = (HideFlags)55;
		val.layer = ((Component)this).gameObject.layer;
		if (isPartOfArm)
		{
			_armRenderers.Add(val.GetComponent<Renderer>());
		}
		return val.transform;
	}

	private void createCylinder(string name, Transform jointA, Transform jointB, bool isPartOfArm = false)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		GameObject val = new GameObject(name);
		_serializedTransforms.Add(val.transform);
		val.AddComponent<MeshFilter>();
		((Renderer)val.AddComponent<MeshRenderer>()).sharedMaterial = _material;
		val.transform.parent = ((Component)this).transform;
		_cylinderTransforms.Add(val.transform);
		_sphereATransforms.Add(jointA);
		_sphereBTransforms.Add(jointB);
		val.gameObject.layer = ((Component)this).gameObject.layer;
		((Object)val).hideFlags = (HideFlags)55;
		if (isPartOfArm)
		{
			_armRenderers.Add(val.GetComponent<Renderer>());
		}
	}

	private Mesh generateCylinderMesh(float length)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		Mesh val = new Mesh();
		((Object)val).name = "GeneratedCylinder";
		((Object)val).hideFlags = (HideFlags)52;
		List<Vector3> list = new List<Vector3>();
		List<Color> list2 = new List<Color>();
		List<int> list3 = new List<int>();
		Vector3 zero = Vector3.zero;
		Vector3 val2 = Vector3.forward * length;
		Vector3 val3 = default(Vector3);
		for (int i = 0; i < _cylinderResolution; i++)
		{
			float num = (float)Math.PI * 2f * (float)i / (float)_cylinderResolution;
			float num2 = 0.006f * Mathf.Cos(num);
			float num3 = 0.006f * Mathf.Sin(num);
			val3 = new Vector3(num2, num3, 0f);
			list.Add(zero + val3);
			list.Add(val2 + val3);
			list2.Add(Color.white);
			list2.Add(Color.white);
			int count = list.Count;
			int num4 = _cylinderResolution * 2;
			list3.Add(count % num4);
			list3.Add((count + 2) % num4);
			list3.Add((count + 1) % num4);
			list3.Add((count + 2) % num4);
			list3.Add((count + 3) % num4);
			list3.Add((count + 1) % num4);
		}
		SetVertices(val, list);
		val.SetIndices(list3.ToArray(), (MeshTopology)0, 0);
		val.RecalculateBounds();
		val.RecalculateNormals();
		val.UploadMeshData(true);
		return val;
	}

	private void SetVertices(Mesh mesh, List<Vector3> verts)
	{
		if (_SetVertices != null)
		{
			_SetVertices.Invoke(mesh, new object[1] { verts });
		}
		else if (_Vertices != null)
		{
			_Vertices.SetValue(mesh, verts, null);
		}
		else
		{
			VRLog.Error("Could not find a way to set mesh vertices!");
		}
	}
}

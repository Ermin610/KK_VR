using UnityEngine;
using VRGIN.Core;
using VRGIN.Visuals;

namespace VRGIN.U46.Helpers;

public class GuiScaler
{
	private GUIQuad _Gui;

	private Vector3? _StartLeft;

	private Vector3? _StartRight;

	private Vector3? _StartScale;

	private Quaternion? _StartRotation;

	private Vector3? _StartPosition;

	private Quaternion _StartRotationController;

	private Vector3? _OffsetFromCenter;

	private Transform _Left;

	private Transform _Right;

	private Vector3 TopLeft => _Left.position;

	private Vector3 BottomRight => _Right.position;

	private Vector3 Center => Vector3.Lerp(TopLeft, BottomRight, 0.5f);

	private Vector3 Up
	{
		get
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			Vector3 val = VR.Camera.Head.position - TopLeft;
			Vector3 normalized = ((Vector3)(ref val)).normalized;
			val = VR.Camera.Head.position - BottomRight;
			return Vector3.Lerp(normalized, ((Vector3)(ref val)).normalized, 0.5f);
		}
	}

	public GuiScaler(GUIQuad gui, Transform left, Transform right)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		_Gui = gui;
		_Left = left;
		_Right = right;
		_StartLeft = left.position;
		_StartRight = right.position;
		_StartScale = ((Component)_Gui).transform.localScale;
		_StartRotation = ((Component)_Gui).transform.localRotation;
		_StartPosition = ((Component)_Gui).transform.position;
		_StartRotationController = GetAverageRotation();
		Vector3.Distance(_StartLeft.Value, _StartRight.Value);
		Vector3 val = _StartRight.Value - _StartLeft.Value;
		Vector3 val2 = _StartLeft.Value + val * 0.5f;
		_OffsetFromCenter = ((Component)_Gui).transform.position - val2;
	}

	public void Update()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)_Left) && Object.op_Implicit((Object)(object)_Right))
		{
			float num = Vector3.Distance(_Left.position, _Right.position);
			float num2 = Vector3.Distance(_StartLeft.Value, _StartRight.Value);
			Vector3 val = _Right.position - _Left.position;
			Vector3 val2 = _Left.position + val * 0.5f;
			Quaternion val3 = Quaternion.Inverse(VR.Camera.SteamCam.origin.rotation);
			Quaternion averageRotation = GetAverageRotation();
			Quaternion val4 = val3 * averageRotation * Quaternion.Inverse(val3 * _StartRotationController);
			((Component)_Gui).transform.localScale = num / num2 * _StartScale.Value;
			((Component)_Gui).transform.localRotation = val4 * _StartRotation.Value;
			((Component)_Gui).transform.position = val2 + averageRotation * Quaternion.Inverse(_StartRotationController) * _OffsetFromCenter.Value;
		}
	}

	private Quaternion GetAverageRotation()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = _Right.position - _Left.position;
		Vector3 normalized = ((Vector3)(ref val)).normalized;
		Vector3 val2 = Vector3.Lerp(_Left.forward, _Right.forward, 0.5f);
		val = Vector3.Cross(normalized, val2);
		return Quaternion.LookRotation(((Vector3)(ref val)).normalized, val2);
	}
}

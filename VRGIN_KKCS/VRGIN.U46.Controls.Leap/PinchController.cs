using Leap.Unity;
using UnityEngine;
using UnityEngine.Events;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.U46.Helpers;
using VRGIN.Visuals;

namespace VRGIN.U46.Controls.Leap;

public class PinchController : ProtectedBehaviour
{
	private PinchDetector _Left;

	private PinchDetector _Right;

	private ProximityDetector _Proximity;

	private DetectorLogicGate _StartDetector;

	private DetectorLogicGate _Detector;

	private bool _Pinching;

	private GUIQuad _Current;

	private GuiScaler _Scaler;

	private const float MIN_SCALE = 0.3f;

	protected override void OnStart()
	{
		base.OnStart();
		SetUpDetectors();
	}

	private void SetUpDetectors()
	{
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Expected O, but got Unknown
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Expected O, but got Unknown
		_Left = ((Component)UnityHelper.CreateGameObjectAsChild("Pinch Detector", ((Component)this).transform)).gameObject.AddComponent<PinchDetector>();
		_Right = ((Component)UnityHelper.CreateGameObjectAsChild("Pinch Detector", ((Component)this).transform)).gameObject.AddComponent<PinchDetector>();
		_Proximity = ((Component)VR.Mode.LeftHand.PinchPoint).gameObject.AddComponent<ProximityDetector>();
		_Proximity.TargetObjects = (GameObject[])(object)new GameObject[1] { ((Component)VR.Mode.RightHand.PinchPoint).gameObject };
		_Proximity.OnDistance = 0.1f;
		_Proximity.OffDistance = 0.11f;
		_Detector = ((Component)this).gameObject.AddComponent<DetectorLogicGate>();
		_StartDetector = ((Component)this).gameObject.AddComponent<DetectorLogicGate>();
		_Left._handModel = VR.Mode.LeftHand;
		_Right._handModel = VR.Mode.RightHand;
		_StartDetector.AddDetector(_Left);
		_StartDetector.AddDetector(_Right);
		_StartDetector.AddDetector(_Proximity);
		_Detector.AddDetector(_Left);
		_Detector.AddDetector(_Right);
		_StartDetector.OnActivate.AddListener(new UnityAction(OnStartPinch));
		_Detector.OnDeactivate.AddListener(new UnityAction(OnStopPinch));
	}

	private void OnStartPinch()
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		_Pinching = true;
		if ((_Current != null))
		{
			Object.DestroyImmediate((Object)(object)((Component)_Current).gameObject);
		}
		_Current = GUIQuad.Create();
		((Component)_Current).transform.SetParent(VR.Camera.Origin, false);
		Object.DontDestroyOnLoad((Object)(object)_Current);
		((Component)_Current).transform.position = Vector3.Lerp(VR.Mode.LeftHand.PinchPoint.position, VR.Mode.RightHand.PinchPoint.position, 0.5f);
		((Component)_Current).transform.rotation = Quaternion.Slerp(VR.Mode.LeftHand.PinchPoint.rotation, VR.Mode.RightHand.PinchPoint.rotation, 0.5f) * Quaternion.Euler(0f, 0f, 90f);
		Transform transform = ((Component)_Current).transform;
		transform.localScale *= Vector3.Distance(VR.Mode.LeftHand.PinchPoint.position, VR.Mode.RightHand.PinchPoint.position);
		_Scaler = new GuiScaler(_Current, VR.Mode.LeftHand.PinchPoint, VR.Mode.RightHand.PinchPoint);
	}

	private void OnStopPinch()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		if (!_Pinching)
		{
			return;
		}
		_Pinching = false;
		if ((_Current != null))
		{
			Vector3 localScale = ((Component)_Current).transform.localScale;
			if (localScale.magnitude < 0.3f)
			{
				Object.DestroyImmediate((Object)(object)((Component)_Current).gameObject);
			}
		}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (_Pinching)
		{
			_Scaler.Update();
		}
	}
}

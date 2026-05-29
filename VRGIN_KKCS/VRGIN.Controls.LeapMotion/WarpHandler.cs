using Leap;
using Leap.Unity;
using UnityEngine;
using UnityEngine.Events;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.U46.Visuals;

namespace VRGIN.Controls.LeapMotion;

public class WarpHandler : ProtectedBehaviour
{
	private PlayAreaVisualization _Visualization;

	private PalmDirectionDetector _PalmDownwardsDetector;

	private ExtendedFingerDetector _ExtendedFingerDetector;

	private DetectorLogicGate _OpenPalmDownwardsDetector;

	private ExtendedFingerDetector _Fistdetector;

	private HandAttachments _Hand;

	private float _LastFist;

	private float _LastShow;

	private Vector3 _PrevPosition;

	private bool _MoveHeight;

	private float _HeightChange;

	private const float TIME_THRESHOLD = 0.3f;

	private bool _Showing;

	protected override void OnStart()
	{
		base.OnStart();
		_Visualization = PlayAreaVisualization.Create();
		Object.DontDestroyOnLoad((Object)(object)((Component)_Visualization).gameObject);
		_Visualization.Disable();
		_Hand = ((Component)this).GetComponent<HandAttachments>();
		SetUpDetectors();
	}

	protected virtual void OnDestroy()
	{
		Object.Destroy((Object)(object)((Component)_Visualization).gameObject);
	}

	protected override void OnUpdate()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		base.OnUpdate();
		if (_Showing)
		{
			_Hand.GetLeapHand();
			Vector3 position = _Hand.Palm.position;
			Vector3 val = position - _PrevPosition;
			if (val.magnitude < 0.1f)
			{
				float num = 0f;
				PlayArea area = _Visualization.Area;
				area.Position += Vector3.Scale(new Vector3(val.x, num, val.z), new Vector3(10f, 5f, 10f));
				_PrevPosition = position;
			}
		}
	}

	private void SetUpDetectors()
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Expected O, but got Unknown
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Expected O, but got Unknown
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Expected O, but got Unknown
		GameObject gameObject = ((Component)UnityHelper.CreateGameObjectAsChild("Warp Detector Holder", ((Component)this).transform)).gameObject;
		_OpenPalmDownwardsDetector = gameObject.AddComponent<DetectorLogicGate>();
		_PalmDownwardsDetector = gameObject.AddComponent<PalmDirectionDetector>();
		_PalmDownwardsDetector.HandModel = _Hand;
		PalmDirectionDetector palmDownwardsDetector = _PalmDownwardsDetector;
		Vector3 val = new Vector3(0f, -1f, 0.5f);
		palmDownwardsDetector.PointingDirection = val.normalized;
		_PalmDownwardsDetector.OnAngle = 10f;
		_ExtendedFingerDetector = ((Component)UnityHelper.CreateGameObjectAsChild("_ExtendedFingerDetector", gameObject.transform)).gameObject.AddComponent<ExtendedFingerDetector>();
		_ExtendedFingerDetector.HandModel = _Hand;
		_ExtendedFingerDetector.Thumb = PointingState.Extended;
		_ExtendedFingerDetector.Index = PointingState.Extended;
		_ExtendedFingerDetector.Middle = PointingState.Extended;
		_ExtendedFingerDetector.Ring = PointingState.Extended;
		_ExtendedFingerDetector.Pinky = PointingState.Extended;
		_Fistdetector = ((Component)UnityHelper.CreateGameObjectAsChild("_Fistdetector", gameObject.transform)).gameObject.AddComponent<ExtendedFingerDetector>();
		_Fistdetector.HandModel = _Hand;
		_Fistdetector.Thumb = PointingState.Either;
		_Fistdetector.Index = PointingState.NotExtended;
		_Fistdetector.Middle = PointingState.NotExtended;
		_Fistdetector.Ring = PointingState.NotExtended;
		_Fistdetector.Pinky = PointingState.NotExtended;
		_OpenPalmDownwardsDetector.AddDetector(_PalmDownwardsDetector);
		_OpenPalmDownwardsDetector.AddDetector(_ExtendedFingerDetector);
		_OpenPalmDownwardsDetector.OnActivate.AddListener(new UnityAction(OnOpenPalmDownwardStart));
		_OpenPalmDownwardsDetector.OnDeactivate.AddListener(new UnityAction(OnOpenPalmDownwardEnd));
		_Fistdetector.OnActivate.AddListener(new UnityAction(OnFist));
	}

	private void OnFist()
	{
		VRLog.Info("Fist");
		if (Time.unscaledTime - _LastShow < 0.3f)
		{
			_Visualization.Area.Apply();
		}
		else
		{
			_LastFist = Time.unscaledTime;
		}
	}

	private void OnOpenPalmDownwardEnd()
	{
		if (_Showing)
		{
			VRLog.Info("Stop!");
			_LastShow = Time.unscaledTime;
			_Visualization.Disable();
			_Showing = false;
		}
	}

	private void OnOpenPalmDownwardStart()
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		VRLog.Info("Palm");
		if (!_Showing && Time.unscaledTime - _LastFist < 0.3f)
		{
			VRLog.Info("Visualize!");
			Hand leapHand = _Hand.GetLeapHand();
			_Visualization.Area.Height = VR.Camera.Origin.position.y;
			Plane val = default(Plane);
			val = new Plane(Vector3.up, _Visualization.Area.Position);
			_Visualization.Enable();
			_Showing = true;
			_MoveHeight = false;
			_HeightChange = 0f;
			_PrevPosition = _Hand.Palm.position;
			Ray val2 = default(Ray);
			val2 = new Ray(leapHand.StabilizedPalmPosition.ToVector3(), leapHand.PalmNormal.ToVector3());
			float num = default(float);
			if (val.Raycast(val2, out num) && num < 5f)
			{
				_Visualization.Area.Position = val2.origin + val2.direction * num;
			}
		}
	}
}

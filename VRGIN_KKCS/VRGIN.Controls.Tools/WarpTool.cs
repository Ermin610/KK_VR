using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.Modes;
using VRGIN.U46.Visuals;
using VRGIN.Visuals;
using Valve.VR;

namespace VRGIN.Controls.Tools;

public class WarpTool : Tool
{
	private enum WarpState
	{
		None,
		Rotating,
		Transforming,
		Grabbing
	}

	private ArcRenderer ArcRenderer;

	private PlayAreaVisualization _Visualization;

	private PlayArea _ProspectedPlayArea = new PlayArea();

	private const float SCALE_THRESHOLD = 0.05f;

	private const float TRANSLATE_THRESHOLD = 0.05f;

	private WarpState State;

	private TravelDistanceRumble _TravelRumble;

	private Vector3 _PrevPoint;

	private float? _GripStartTime;

	private float? _TriggerDownTime;

	private bool Showing;

	private List<Vector2> _Points = new List<Vector2>();

	private const float GRIP_TIME_THRESHOLD = 0.1f;

	private const float GRIP_DIFF_THRESHOLD = 0.01f;

	private const float EXACT_IMPERSONATION_TIME = 1f;

	private Vector3 _PrevControllerPos;

	private Quaternion _PrevControllerRot;

	private Controller.Lock _OtherLock;

	private float _InitialControllerDistance;

	private float _InitialIPD;

	private Vector3 _PrevFromTo;

	private const EVRButtonId SECONDARY_SCALE_BUTTON = EVRButtonId.k_EButton_Axis1;

	private const EVRButtonId SECONDARY_ROTATE_BUTTON = EVRButtonId.k_EButton_Grip;

	private float _IPDOnStart;

	private bool _ScaleInitialized;

	private bool _RotationInitialized;

	public override Texture2D Image => UnityHelper.LoadImage("icon_warp.png");

	protected override void OnAwake()
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		VRLog.Info("Awake!");
		if (VR.Settings.UseStraightWarp)
		{
			ArcRenderer = new GameObject("Straight Renderer").AddComponent<StraightRenderer>();
			ArcRenderer.Velocity = VR.Settings.ArcWarpVelocity;
		}
		else
		{
			ArcRenderer = new GameObject("Arc Renderer").AddComponent<ArcRenderer>();
		}
		((Component)ArcRenderer).transform.SetParent(((Component)this).transform, false);
		((Component)ArcRenderer).gameObject.SetActive(false);
		_TravelRumble = new TravelDistanceRumble(500, 0.1f, ((Component)this).transform);
		_TravelRumble.UseLocalPosition = true;
		_Visualization = PlayAreaVisualization.Create(_ProspectedPlayArea);
		Object.DontDestroyOnLoad((Object)(object)((Component)_Visualization).gameObject);
		SetVisibility(visible: false);
	}

	protected override void OnDestroy()
	{
		VRLog.Info("Destroy!");
		Object.DestroyImmediate((Object)(object)((Component)_Visualization).gameObject);
	}

	protected override void OnStart()
	{
		VRLog.Info("Start!");
		base.OnStart();
		_IPDOnStart = VR.Settings.IPDScale;
		ResetPlayArea(_ProspectedPlayArea);
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		SetVisibility(visible: false);
		ResetPlayArea(_ProspectedPlayArea);
	}

	public void OnPlayAreaUpdated()
	{
		ResetPlayArea(_ProspectedPlayArea);
	}

	private void SetVisibility(bool visible)
	{
		Showing = visible;
		if (visible)
		{
			ArcRenderer.Update();
			UpdateProspectedArea();
			_Visualization.UpdatePosition();
		}
		((Component)ArcRenderer).gameObject.SetActive(visible);
		((Component)_Visualization).gameObject.SetActive(visible);
	}

	private void ResetPlayArea(PlayArea area)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		area.Position = VR.Camera.SteamCam.origin.position;
		area.Scale = VR.Settings.IPDScale;
		Quaternion rotation = VR.Camera.SteamCam.origin.rotation;
		area.Rotation = rotation.eulerAngles.y;
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		EnterState(WarpState.None);
		SetVisibility(visible: false);
		Owner.StopRumble(_TravelRumble);
	}

	protected override void OnLateUpdate()
	{
		if (Showing)
		{
			UpdateProspectedArea();
		}
	}

	private void UpdateProspectedArea()
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		ArcRenderer.Offset = _ProspectedPlayArea.Height;
		ArcRenderer.Scale = VR.Settings.IPDScale;
		_ProspectedPlayArea.Position = new Vector3(ArcRenderer.target.x, _ProspectedPlayArea.Position.y, ArcRenderer.target.z);
	}

	private void CheckRotationalPress()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		if (base.Controller.GetPressDown(EVRButtonId.k_EButton_Axis0))
		{
			Vector2 axis = base.Controller.GetAxis();
			_ProspectedPlayArea.Reset();
			if (axis.x < -0.2f)
			{
				_ProspectedPlayArea.Rotation -= 20f;
			}
			else if (axis.x > 0.2f)
			{
				_ProspectedPlayArea.Rotation += 20f;
			}
			_ProspectedPlayArea.Apply();
		}
	}

	protected override void OnUpdate()
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		base.OnUpdate();
		if (State == WarpState.None)
		{
			Vector2 axis = base.Controller.GetAxis();
			if (axis.magnitude < 0.5f)
			{
				if (base.Controller.GetTouchDown(EVRButtonId.k_EButton_Axis0))
				{
					EnterState(WarpState.Rotating);
				}
			}
			else
			{
				CheckRotationalPress();
			}
			if (base.Controller.GetPressDown(EVRButtonId.k_EButton_Grip))
			{
				EnterState(WarpState.Grabbing);
			}
		}
		if (State == WarpState.Grabbing)
		{
			HandleGrabbing();
		}
		if (State == WarpState.Rotating)
		{
			HandleRotation();
		}
		if (State == WarpState.Transforming && base.Controller.GetPressUp(EVRButtonId.k_EButton_Axis0))
		{
			_ProspectedPlayArea.Apply();
			ArcRenderer.Update();
			EnterState(WarpState.Rotating);
		}
		if (State != 0)
		{
			return;
		}
		if (base.Controller.GetHairTriggerDown())
		{
			_TriggerDownTime = Time.unscaledTime;
		}
		if (_TriggerDownTime.HasValue)
		{
			if (base.Controller.GetHairTrigger() && Time.unscaledTime - _TriggerDownTime > 1f)
			{
				VRManager.Instance.Mode.Impersonate(VR.Interpreter.FindNextActorToImpersonate(), ImpersonationMode.Exactly);
				_TriggerDownTime = null;
			}
			if (VRManager.Instance.Interpreter.Actors.Any() && base.Controller.GetHairTriggerUp())
			{
				VRManager.Instance.Mode.Impersonate(VR.Interpreter.FindNextActorToImpersonate(), ImpersonationMode.Approximately);
			}
		}
	}

	private void HandleRotation()
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		if (Showing)
		{
			_Points.Add(base.Controller.GetAxis());
			if (_Points.Count > 2)
			{
				DetectCircle();
			}
		}
		if (base.Controller.GetPressDown(EVRButtonId.k_EButton_Axis0))
		{
			EnterState(WarpState.Transforming);
		}
		if (base.Controller.GetTouchUp(EVRButtonId.k_EButton_Axis0))
		{
			EnterState(WarpState.None);
		}
	}

	private void InitializeScaleIfNeeded()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		if (!_ScaleInitialized)
		{
			_InitialControllerDistance = Vector3.Distance(((Component)base.OtherController).transform.position, ((Component)this).transform.position);
			_InitialIPD = VR.Settings.IPDScale;
			Vector3 val = ((Component)base.OtherController).transform.position - ((Component)this).transform.position;
			_PrevFromTo = val.normalized;
			_ScaleInitialized = true;
		}
	}

	private void InitializeRotationIfNeeded()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		if (!_ScaleInitialized && !_RotationInitialized)
		{
			Vector3 val = ((Component)base.OtherController).transform.position - ((Component)this).transform.position;
			_PrevFromTo = val.normalized;
			_RotationInitialized = true;
		}
	}

	private void HandleGrabbing()
	{
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_0508: Unknown result type (might be due to invalid IL or missing references)
		//IL_050d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0519: Unknown result type (might be due to invalid IL or missing references)
		//IL_051e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_046e: Unknown result type (might be due to invalid IL or missing references)
		//IL_047d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0482: Unknown result type (might be due to invalid IL or missing references)
		//IL_0487: Unknown result type (might be due to invalid IL or missing references)
		//IL_048c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0491: Unknown result type (might be due to invalid IL or missing references)
		//IL_0494: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
		//IL_0306: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0369: Unknown result type (might be due to invalid IL or missing references)
		//IL_036e: Unknown result type (might be due to invalid IL or missing references)
		if (base.OtherController.IsTracking && !HasLock())
		{
			base.OtherController.TryAcquireFocus(out _OtherLock);
		}
		if (HasLock() && base.OtherController.Input.GetPressDown(EVRButtonId.k_EButton_Axis1))
		{
			_ScaleInitialized = false;
		}
		if (HasLock() && base.OtherController.Input.GetPressDown(EVRButtonId.k_EButton_Grip))
		{
			_RotationInitialized = false;
		}
		Vector3 val;
		if (base.Controller.GetPress(EVRButtonId.k_EButton_Grip))
		{
			if (HasLock() && (base.OtherController.Input.GetPress(EVRButtonId.k_EButton_Grip) || base.OtherController.Input.GetPress(EVRButtonId.k_EButton_Axis1)))
			{
				val = ((Component)base.OtherController).transform.position - ((Component)this).transform.position;
				Vector3 normalized = val.normalized;
				if (base.OtherController.Input.GetPress(EVRButtonId.k_EButton_Axis1))
				{
					InitializeScaleIfNeeded();
					float num = Vector3.Distance(((Component)base.OtherController).transform.position, ((Component)this).transform.position) * (_InitialIPD / VR.Settings.IPDScale) / _InitialControllerDistance;
					VR.Settings.IPDScale = num * _InitialIPD;
					_ProspectedPlayArea.Scale = VR.Settings.IPDScale;
				}
				if (base.OtherController.Input.GetPress(EVRButtonId.k_EButton_Grip))
				{
					InitializeRotationIfNeeded();
					float num2 = Calculator.Angle(_PrevFromTo, normalized) * VR.Settings.RotationMultiplier;
					((Component)VR.Camera.SteamCam.origin).transform.RotateAround(VR.Camera.Head.position, Vector3.up, num2);
					_ProspectedPlayArea.Rotation += num2;
				}
				val = ((Component)base.OtherController).transform.position - ((Component)this).transform.position;
				_PrevFromTo = val.normalized;
			}
			else
			{
				Vector3 val2 = ((Component)this).transform.position - _PrevControllerPos;
				Quaternion val3 = Quaternion.Inverse(_PrevControllerRot * Quaternion.Inverse(((Component)this).transform.rotation)) * (((Component)this).transform.rotation * Quaternion.Inverse(((Component)this).transform.rotation));
				if (Time.unscaledTime - _GripStartTime > 0.1f || Calculator.Distance(val2.magnitude) > 0.01f)
				{
					Vector3 forward = Vector3.forward;
					Vector3 v = val3 * Vector3.forward;
					float num3 = Calculator.Angle(forward, v) * VR.Settings.RotationMultiplier;
					Transform transform = ((Component)VR.Camera.SteamCam.origin).transform;
					transform.position -= val2;
					_ProspectedPlayArea.Height -= val2.y;
					if (!VR.Settings.GrabRotationImmediateMode && base.Controller.GetPress(12884901888uL))
					{
						((Component)VR.Camera.SteamCam.origin).transform.RotateAround(VR.Camera.Head.position, Vector3.up, 0f - num3);
						_ProspectedPlayArea.Rotation -= num3;
					}
					_GripStartTime = 0f;
				}
			}
		}
		if (base.Controller.GetPressUp(EVRButtonId.k_EButton_Grip))
		{
			EnterState(WarpState.None);
			if (Time.unscaledTime - _GripStartTime < 0.1f)
			{
				Owner.StartRumble(new RumbleImpulse(800));
				_ProspectedPlayArea.Height = 0f;
				_ProspectedPlayArea.Scale = _IPDOnStart;
			}
		}
		if (VR.Settings.GrabRotationImmediateMode && base.Controller.GetPressUp(12884901888uL))
		{
			val = Vector3.ProjectOnPlane(((Component)this).transform.position - VR.Camera.Head.position, Vector3.up);
			Vector3 normalized2 = val.normalized;
			val = Vector3.ProjectOnPlane(VR.Camera.Head.forward, Vector3.up);
			Vector3 normalized3 = val.normalized;
			float num4 = Calculator.Angle(normalized2, normalized3);
			((Component)VR.Camera.SteamCam.origin).transform.RotateAround(VR.Camera.Head.position, Vector3.up, num4);
			_ProspectedPlayArea.Rotation = num4;
		}
		_PrevControllerPos = ((Component)this).transform.position;
		_PrevControllerRot = ((Component)this).transform.rotation;
		CheckRotationalPress();
	}

	private float NormalizeAngle(float angle)
	{
		return angle % 360f;
	}

	private void DetectCircle()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		float? num = null;
		float? num2 = null;
		float num3 = 0f;
		foreach (Vector2 point in _Points)
		{
			Vector2 current = point;
			float magnitude = current.magnitude;
			num = Math.Max(num ?? magnitude, magnitude);
			num2 = Math.Max(num2 ?? magnitude, magnitude);
			num3 += magnitude;
		}
		num3 /= (float)_Points.Count;
		float? num4 = num2 - num;
		float num5 = 0.2f;
		if (num4.GetValueOrDefault() < num5 && num4.HasValue && num > 0.2f)
		{
			float num6 = Mathf.Atan2(_Points.First().y, _Points.First().x) * 57.29578f;
			float num7 = Mathf.Atan2(_Points.Last().y, _Points.Last().x) * 57.29578f - num6;
			if (Mathf.Abs(num7) < 60f)
			{
				_ProspectedPlayArea.Rotation -= num7;
			}
			else
			{
				VRLog.Info("Discarding too large rotation: {0}", num7);
			}
		}
		_Points.Clear();
	}

	private void EnterState(WarpState state)
	{
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		switch (State)
		{
		case WarpState.Grabbing:
			Owner.StopRumble(_TravelRumble);
			_ScaleInitialized = (_RotationInitialized = false);
			if (HasLock())
			{
				VRLog.Info("Releasing lock on other controller!");
				_OtherLock.SafeRelease();
			}
			break;
		}
		switch (state)
		{
		case WarpState.None:
			SetVisibility(visible: false);
			break;
		case WarpState.Rotating:
			SetVisibility(visible: true);
			Reset();
			break;
		case WarpState.Grabbing:
			_PrevControllerPos = ((Component)this).transform.position;
			_GripStartTime = Time.unscaledTime;
			_TravelRumble.Reset();
			_PrevControllerPos = ((Component)this).transform.position;
			_PrevControllerRot = ((Component)this).transform.rotation;
			Owner.StartRumble(_TravelRumble);
			break;
		}
		State = state;
	}

	private bool HasLock()
	{
		if (_OtherLock != null)
		{
			return _OtherLock.IsValid;
		}
		return false;
	}

	private void Reset()
	{
		_Points.Clear();
	}

	public override List<HelpText> GetHelpTexts()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		return new List<HelpText>(new HelpText[5]
		{
			HelpText.Create("Press to teleport", FindAttachPosition("trackpad"), new Vector3(0f, 0.02f, 0.05f)),
			HelpText.Create("Circle to rotate", FindAttachPosition("trackpad"), new Vector3(0.05f, 0.02f, 0f), (Vector3?)new Vector3(0.015f, 0f, 0f)),
			HelpText.Create("press & move controller", FindAttachPosition("trackpad"), new Vector3(-0.05f, 0.02f, 0f), (Vector3?)new Vector3(-0.015f, 0f, 0f)),
			HelpText.Create("Warp into main char", FindAttachPosition("trigger"), new Vector3(0.06f, 0.04f, -0.05f)),
			HelpText.Create("reset area", FindAttachPosition("lgrip"), new Vector3(-0.06f, 0f, -0.05f))
		});
	}
}

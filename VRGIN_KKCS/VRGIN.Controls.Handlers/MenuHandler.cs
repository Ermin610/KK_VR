using System;
using System.Linq;
using UnityEngine;
using VRGIN.Controls.Tools;
using VRGIN.Core;
using VRGIN.Native;
using VRGIN.Visuals;
using Valve.VR;

namespace VRGIN.Controls.Handlers;

public class MenuHandler : ProtectedBehaviour
{
	private class ResizeHandler : ProtectedBehaviour
	{
		private GUIQuad _Gui;

		private Vector3? _StartLeft;

		private Vector3? _StartRight;

		private Vector3? _StartScale;

		private Quaternion? _StartRotation;

		private Vector3? _StartPosition;

		private Quaternion _StartRotationController;

		private Vector3? _OffsetFromCenter;

		public bool IsDragging { get; private set; }

		protected override void OnStart()
		{
			base.OnStart();
			_Gui = ((Component)this).GetComponent<GUIQuad>();
		}

		protected override void OnUpdate()
		{
			//IL_006c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0071: Unknown result type (might be due to invalid IL or missing references)
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_0086: Unknown result type (might be due to invalid IL or missing references)
			//IL_0087: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			//IL_0095: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0101: Unknown result type (might be due to invalid IL or missing references)
			//IL_0106: Unknown result type (might be due to invalid IL or missing references)
			//IL_010b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0121: Unknown result type (might be due to invalid IL or missing references)
			//IL_0126: Unknown result type (might be due to invalid IL or missing references)
			//IL_013b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0143: Unknown result type (might be due to invalid IL or missing references)
			//IL_0148: Unknown result type (might be due to invalid IL or missing references)
			//IL_015d: Unknown result type (might be due to invalid IL or missing references)
			//IL_015f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0162: Unknown result type (might be due to invalid IL or missing references)
			//IL_0167: Unknown result type (might be due to invalid IL or missing references)
			//IL_016c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0177: Unknown result type (might be due to invalid IL or missing references)
			//IL_017c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0181: Unknown result type (might be due to invalid IL or missing references)
			base.OnUpdate();
			IsDragging = GetDevice(VR.Mode.Left).GetPress(EVRButtonId.k_EButton_Axis1) && GetDevice(VR.Mode.Right).GetPress(EVRButtonId.k_EButton_Axis1);
			if (IsDragging)
			{
				if (!_StartScale.HasValue)
				{
					Initialize();
				}
				Vector3 position = ((Component)VR.Mode.Left).transform.position;
				Vector3 position2 = ((Component)VR.Mode.Right).transform.position;
				float num = Vector3.Distance(position, position2);
				float num2 = Vector3.Distance(_StartLeft.Value, _StartRight.Value);
				Vector3 val = position2 - position;
				Vector3 val2 = position + val * 0.5f;
				Quaternion val3 = Quaternion.Inverse(VR.Camera.SteamCam.origin.rotation);
				Quaternion averageRotation = GetAverageRotation();
				Quaternion val4 = val3 * averageRotation * Quaternion.Inverse(val3 * _StartRotationController);
				((Component)_Gui).transform.localScale = num / num2 * _StartScale.Value;
				((Component)_Gui).transform.localRotation = val4 * _StartRotation.Value;
				((Component)_Gui).transform.position = val2 + averageRotation * Quaternion.Inverse(_StartRotationController) * _OffsetFromCenter.Value;
			}
			else
			{
				_StartScale = null;
			}
		}

		private Quaternion GetAverageRotation()
		{
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_005a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			Vector3 position = ((Component)VR.Mode.Left).transform.position;
			Vector3 val = ((Component)VR.Mode.Right).transform.position - position;
			Vector3 normalized = ((Vector3)(ref val)).normalized;
			Vector3 val2 = Vector3.Lerp(((Component)VR.Mode.Left).transform.forward, ((Component)VR.Mode.Right).transform.forward, 0.5f);
			val = Vector3.Cross(normalized, val2);
			return Quaternion.LookRotation(((Vector3)(ref val)).normalized, val2);
		}

		private void Initialize()
		{
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			//IL_0080: Unknown result type (might be due to invalid IL or missing references)
			//IL_0091: Unknown result type (might be due to invalid IL or missing references)
			//IL_0096: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00de: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
			_StartLeft = ((Component)VR.Mode.Left).transform.position;
			_StartRight = ((Component)VR.Mode.Right).transform.position;
			_StartScale = ((Component)_Gui).transform.localScale;
			_StartRotation = ((Component)_Gui).transform.localRotation;
			_StartPosition = ((Component)_Gui).transform.position;
			_StartRotationController = GetAverageRotation();
			Vector3.Distance(_StartLeft.Value, _StartRight.Value);
			Vector3 val = _StartRight.Value - _StartLeft.Value;
			Vector3 val2 = _StartLeft.Value + val * 0.5f;
			_OffsetFromCenter = ((Component)this).transform.position - val2;
		}

		private SteamVR_Controller.Device GetDevice(Controller controller)
		{
			return SteamVR_Controller.Input((int)controller.Tracking.index);
		}
	}

	private Controller _Controller;

	private const float RANGE = 0.25f;

	private const int MOUSE_STABILIZER_THRESHOLD = 30;

	private Controller.Lock _LaserLock = Controller.Lock.Invalid;

	private LineRenderer Laser;

	private Vector2? mouseDownPosition;

	private GUIQuad _Target;

	private MenuHandler _Other;

	private ResizeHandler _ResizeHandler;

	private Vector3 _ScaleVector;

	protected SteamVR_Controller.Device Device => SteamVR_Controller.Input((int)_Controller.Tracking.index);

	private bool IsResizing
	{
		get
		{
			if (Object.op_Implicit((Object)(object)_ResizeHandler))
			{
				return _ResizeHandler.IsDragging;
			}
			return false;
		}
	}

	public bool LaserVisible
	{
		get
		{
			if (Object.op_Implicit((Object)(object)Laser))
			{
				return ((Component)Laser).gameObject.activeSelf;
			}
			return false;
		}
		set
		{
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
			if (!Object.op_Implicit((Object)(object)Laser))
			{
				return;
			}
			if (value && !_LaserLock.IsValid)
			{
				_LaserLock = _Controller.AcquireFocus();
				if (!_LaserLock.IsValid)
				{
					return;
				}
			}
			else if (!value && _LaserLock.IsValid)
			{
				_LaserLock.Release();
			}
			((Component)Laser).gameObject.SetActive(value);
			if (value)
			{
				Laser.SetPosition(0, ((Component)Laser).transform.position);
				Laser.SetPosition(1, ((Component)Laser).transform.position);
			}
			else
			{
				mouseDownPosition = null;
			}
		}
	}

	public bool IsPressing { get; private set; }

	protected override void OnStart()
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		base.OnStart();
		VRLog.Info("Menu Handler started");
		_Controller = ((Component)this).GetComponent<Controller>();
		_ScaleVector = Vector2.op_Implicit(new Vector2(1f, 1f));
		_Other = ((Component)_Controller.Other).GetComponent<MenuHandler>();
	}

	private void OnRenderModelLoaded()
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (!Object.op_Implicit((Object)(object)_Controller))
			{
				_Controller = ((Component)this).GetComponent<Controller>();
			}
			Transform val = _Controller.FindAttachPosition("tip");
			if (!Object.op_Implicit((Object)(object)val))
			{
				VRLog.Error("Attach position not found for laser!");
				val = ((Component)this).transform;
			}
			Laser = new GameObject().AddComponent<LineRenderer>();
			((Component)Laser).transform.SetParent(val, false);
			((Renderer)Laser).material = Resources.GetBuiltinResource<Material>("Sprites-Default.mat");
			Material material = ((Renderer)Laser).material;
			material.renderQueue += 5000;
			Laser.SetColors(Color.cyan, Color.cyan);
			if (SteamVR.instance.hmd_TrackingSystemName == "lighthouse")
			{
				((Component)Laser).transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
				Transform transform = ((Component)Laser).transform;
				transform.position += ((Component)Laser).transform.forward * 0.06f;
			}
			Laser.SetVertexCount(2);
			Laser.useWorldSpace = true;
			Laser.SetWidth(0.002f, 0.002f);
		}
		catch (Exception obj)
		{
			VRLog.Error(obj);
		}
	}

	protected override void OnUpdate()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		base.OnUpdate();
		if (LaserVisible)
		{
			if (IsResizing)
			{
				Laser.SetPosition(0, ((Component)Laser).transform.position);
				Laser.SetPosition(1, ((Component)Laser).transform.position);
			}
			else
			{
				UpdateLaser();
			}
		}
		else if (_Controller.CanAcquireFocus())
		{
			CheckForNearMenu();
		}
		CheckInput();
	}

	private void OnDisable()
	{
		if (_LaserLock.IsValid)
		{
			_LaserLock.Release();
		}
	}

	private void EnsureResizeHandler()
	{
		if (!Object.op_Implicit((Object)(object)_ResizeHandler))
		{
			_ResizeHandler = ((Component)_Target).GetComponent<ResizeHandler>();
			if (!Object.op_Implicit((Object)(object)_ResizeHandler))
			{
				_ResizeHandler = ((Component)_Target).gameObject.AddComponent<ResizeHandler>();
			}
		}
	}

	private void EnsureNoResizeHandler()
	{
		if (Object.op_Implicit((Object)(object)_ResizeHandler))
		{
			Object.DestroyImmediate((Object)(object)_ResizeHandler);
		}
		_ResizeHandler = null;
	}

	protected void CheckInput()
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		IsPressing = false;
		if (!LaserVisible || !Object.op_Implicit((Object)(object)_Target))
		{
			return;
		}
		if (_Other.LaserVisible && (Object)(object)_Other._Target == (Object)(object)_Target)
		{
			EnsureResizeHandler();
		}
		else
		{
			EnsureNoResizeHandler();
		}
		if (IsResizing)
		{
			return;
		}
		if (Device.GetPressDown(EVRButtonId.k_EButton_Axis1))
		{
			IsPressing = true;
			VR.Input.Mouse.LeftButtonDown();
			mouseDownPosition = new Vector2(Input.mousePosition.x, (float)VRGUI.Height - Input.mousePosition.y);
		}
		if (Device.GetPress(EVRButtonId.k_EButton_Axis1))
		{
			IsPressing = true;
		}
		if (Device.GetPressUp(EVRButtonId.k_EButton_Axis1))
		{
			IsPressing = true;
			VR.Input.Mouse.LeftButtonUp();
			mouseDownPosition = null;
		}
		if (Device.GetPressUp(EVRButtonId.k_EButton_Grip))
		{
			MenuTool component = ((Component)_Controller).GetComponent<MenuTool>();
			if (Object.op_Implicit((Object)(object)component) && !Object.op_Implicit((Object)(object)component.Gui))
			{
				component.TakeGUI(_Target);
				_Controller.ToolIndex = _Controller.Tools.IndexOf(component);
			}
		}
	}

	private void CheckForNearMenu()
	{
		_Target = GUIQuadRegistry.Quads.FirstOrDefault(IsLaserable);
		if (Object.op_Implicit((Object)(object)_Target))
		{
			LaserVisible = true;
		}
	}

	private bool IsLaserable(GUIQuad quad)
	{
		RaycastHit hit;
		if (IsWithinRange(quad))
		{
			return Raycast(quad, out hit);
		}
		return false;
	}

	private float GetRange(GUIQuad quad)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Vector3 localScale = ((Component)quad).transform.localScale;
		return Mathf.Clamp(((Vector3)(ref localScale)).magnitude * 0.25f, 0.25f, 1.25f) * VR.Settings.IPDScale;
	}

	private bool IsWithinRange(GUIQuad quad)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)Laser))
		{
			return false;
		}
		if ((Object)(object)((Component)quad).transform.parent == (Object)(object)((Component)this).transform)
		{
			return false;
		}
		Vector3 val = -((Component)quad).transform.forward;
		_ = ((Component)quad).transform.position;
		Vector3 position = ((Component)Laser).transform.position;
		Vector3 forward = ((Component)Laser).transform.forward;
		float num = 0f - ((Component)quad).transform.InverseTransformPoint(position).z;
		Vector3 localScale = ((Component)quad).transform.localScale;
		float num2 = num * ((Vector3)(ref localScale)).magnitude;
		if (num2 > 0f && num2 < GetRange(quad))
		{
			return Vector3.Dot(val, forward) < 0f;
		}
		return false;
	}

	private bool Raycast(GUIQuad quad, out RaycastHit hit)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		Vector3 position = ((Component)Laser).transform.position;
		Vector3 forward = ((Component)Laser).transform.forward;
		Collider component = ((Component)quad).GetComponent<Collider>();
		if (Object.op_Implicit((Object)(object)component))
		{
			Ray val = default(Ray);
			((Ray)(ref val))._002Ector(position, forward);
			return component.Raycast(val, ref hit, GetRange(quad));
		}
		hit = default(RaycastHit);
		return false;
	}

	private void UpdateLaser()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		Laser.SetPosition(0, ((Component)Laser).transform.position);
		Laser.SetPosition(1, ((Component)Laser).transform.position + ((Component)Laser).transform.forward);
		if (Object.op_Implicit((Object)(object)_Target) && ((Component)_Target).gameObject.activeInHierarchy)
		{
			if (IsWithinRange(_Target) && Raycast(_Target, out var hit))
			{
				Laser.SetPosition(1, ((RaycastHit)(ref hit)).point);
				if (!IsOtherWorkingOn(_Target))
				{
					Vector2 val = default(Vector2);
					((Vector2)(ref val))._002Ector(((RaycastHit)(ref hit)).textureCoord.x * (float)VRGUI.Width, (1f - ((RaycastHit)(ref hit)).textureCoord.y) * (float)VRGUI.Height);
					if (!mouseDownPosition.HasValue || Vector2.Distance(mouseDownPosition.Value, val) > 30f)
					{
						MouseOperations.SetClientCursorPosition((int)val.x, (int)val.y);
						mouseDownPosition = null;
					}
				}
			}
			else
			{
				LaserVisible = false;
			}
		}
		else
		{
			LaserVisible = false;
		}
	}

	private bool IsOtherWorkingOn(GUIQuad target)
	{
		if (Object.op_Implicit((Object)(object)_Other) && _Other.LaserVisible && (Object)(object)_Other._Target == (Object)(object)target)
		{
			return _Other.IsPressing;
		}
		return false;
	}
}

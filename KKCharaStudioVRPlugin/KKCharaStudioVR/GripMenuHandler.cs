using System.Linq;
using UnityEngine;
using VRGIN.Controls;
using VRGIN.Core;
using VRGIN.Native;
using VRGIN.Visuals;
using Valve.VR;

namespace KKCharaStudioVR;

public class GripMenuHandler : ProtectedBehaviour
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

		protected override void OnFixedUpdate()
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
			base.OnFixedUpdate();
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

	private LineRenderer Laser;

	private GameObject dotCursor;

	private Vector2? mouseDownPosition;

	private GUIQuad _Target;

	private ResizeHandler _ResizeHandler;

	private Vector3 _ScaleVector;

	private Vector3 _cachedLaserPos;

	private Vector3 _cachedLaserFwd;

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
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			((Component)Laser).gameObject.SetActive(value);
			if (dotCursor != null) dotCursor.SetActive(value);
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
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		base.OnStart();
		_Controller = ((Component)this).GetComponent<Controller>();
		_ScaleVector = Vector2.op_Implicit(new Vector2((float)VRGUI.Width / (float)Screen.width, (float)VRGUI.Height / (float)Screen.height));
		InitLaser();
	}

	private void InitLaser()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		Laser = new GameObject().AddComponent<LineRenderer>();
		((Component)Laser).transform.SetParent(((Component)this).transform, false);
		((Renderer)Laser).material = Resources.GetBuiltinResource<Material>("Sprites-Default.mat");
		Material material = ((Renderer)Laser).material;
		material.renderQueue += 5000;
		Laser.SetColors(Color.cyan, Color.cyan);
		((Component)Laser).transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
		Transform transform = ((Component)Laser).transform;
		transform.position += ((Component)Laser).transform.forward * 0.07f;
		Laser.SetVertexCount(2);
		Laser.useWorldSpace = true;
		Laser.SetWidth(0.002f, 0.002f);

		dotCursor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		dotCursor.transform.SetParent(((Component)this).transform, false);
		dotCursor.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
		Object.DestroyImmediate(dotCursor.GetComponent<Collider>());
		Renderer dotRenderer = dotCursor.GetComponent<Renderer>();
		dotRenderer.material = Resources.GetBuiltinResource<Material>("Sprites-Default.mat");
		dotRenderer.material.renderQueue += 5000;
		dotRenderer.material.color = Color.cyan;
		dotCursor.SetActive(false);
	}

	protected override void OnUpdate()
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		base.OnUpdate();
		if (!((Component)VR.Camera).gameObject.activeInHierarchy)
		{
			return;
		}

		GetLaserOrigin(out _cachedLaserPos, out _cachedLaserFwd);

		if (LaserVisible)
		{
			if (IsResizing)
			{
				Laser.SetPosition(0, _cachedLaserPos);
				Laser.SetPosition(1, _cachedLaserPos);
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

	private void GetLaserOrigin(out Vector3 originPos, out Vector3 originFwd)
	{
		originPos = ((Component)Laser).transform.position;
		originFwd = ((Component)Laser).transform.forward;

		if (VRHandModelManager.Instance != null && VR.Manager != null && VR.Manager.Context != null)
		{
			var settings = VR.Manager.Context.Settings as KKCharaStudioVRSettings;
			if (settings != null && settings.HandModelEnabled)
			{
				bool isLeft = ((Component)this).GetComponent<LeftController>() != null;
				Transform fingerTip = VRHandModelManager.Instance.GetFingerTipTransform(isLeft, 1);
				if (fingerTip != null)
				{
					originPos = fingerTip.position;
					originFwd = fingerTip.forward;
				}
			}
		}
	}

	private void OnDisable()
	{
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
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		IsPressing = false;
		if (LaserVisible && Object.op_Implicit((Object)(object)_Target) && !IsResizing)
		{
			if (Device.GetPressDown(EVRButtonId.k_EButton_Axis1))
			{
				IsPressing = true;
				VR.Input.Mouse.LeftButtonDown();
				mouseDownPosition = Vector2.Scale(new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y), Vector2.op_Implicit(_ScaleVector));
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
		return Mathf.Clamp(((Vector3)(ref localScale)).magnitude * 0.25f, 0.25f, 1.25f);
	}

	private bool IsWithinRange(GUIQuad quad)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)((Component)quad).transform.parent == (Object)(object)((Component)this).transform)
		{
			return false;
		}
		Vector3 val = -((Component)quad).transform.forward;
		_ = ((Component)quad).transform.position;
		Vector3 position = _cachedLaserPos;
		Vector3 forward = _cachedLaserFwd;
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
		Vector3 position = _cachedLaserPos;
		Vector3 forward = _cachedLaserFwd;
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
		Laser.SetPosition(0, _cachedLaserPos);
		Vector3 endPos = _cachedLaserPos + _cachedLaserFwd;
		Laser.SetPosition(1, endPos);
		bool hitUI = false;

		if (Object.op_Implicit((Object)(object)_Target) && ((Component)_Target).gameObject.activeInHierarchy)
		{
			if (IsWithinRange(_Target) && Raycast(_Target, out var hit))
			{
				hitUI = true;
				endPos = ((RaycastHit)(ref hit)).point;
				Laser.SetPosition(1, endPos);
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

		if (LaserVisible && dotCursor != null)
		{
			if (hitUI)
			{
				dotCursor.SetActive(true);
				dotCursor.transform.position = endPos;
				if (IsPressing)
				{
					Laser.SetColors(Color.red, Color.red);
					dotCursor.GetComponent<Renderer>().material.color = Color.red;
					Vector3 dir = endPos - _cachedLaserPos;
					if (dir.magnitude > 0.02f)
					{
						Laser.SetPosition(1, endPos - dir.normalized * 0.02f);
					}
				}
				else
				{
					Laser.SetColors(Color.green, Color.green);
					dotCursor.GetComponent<Renderer>().material.color = Color.green;
				}
			}
			else
			{
				dotCursor.SetActive(false);
				Laser.SetColors(Color.cyan, Color.cyan);
			}
		}
		else if (dotCursor != null)
		{
			dotCursor.SetActive(false);
		}
	}

	private bool IsOtherWorkingOn(GUIQuad target)
	{
		return false;
	}
}

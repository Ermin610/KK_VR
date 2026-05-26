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
			IsDragging = false; // Disabled two-handed UI moving/scaling to prevent accidental triggering
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
			Vector3 normalized = val.normalized;
			Vector3 val2 = Vector3.Lerp(((Component)VR.Mode.Left).transform.forward, ((Component)VR.Mode.Right).transform.forward, 0.5f);
			val = Vector3.Cross(normalized, val2);
			return Quaternion.LookRotation(val.normalized, val2);
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

	private const float RESIZE_RATE = 0.01f;

	private const int MOUSE_STABILIZER_THRESHOLD = 30;

	public static GripMenuHandler ActiveMouseHandler;

	private Controller _Controller;

	private const float RANGE = 0.25f;

	private LineRenderer Laser;

	private GameObject dotCursor;

	private Vector2? mouseDownPosition;

	private GUIQuad _Target;

	private ResizeHandler _ResizeHandler;

	private Vector3 _ScaleVector;

	private float _lastScrollTime;

	private Renderer _dotRenderer;

	private Color _currentLaserColor = Color.cyan;

	private Color _currentDotColor = Color.cyan;

	protected SteamVR_Controller.Device Device => SteamVR_Controller.Input((int)_Controller.Tracking.index);

	private bool IsResizing
	{
		get
		{
			if ((_ResizeHandler != null))
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
			if ((Laser != null))
			{
				return ((Component)Laser).gameObject.activeSelf;
			}
			return false;
		}
		set
		{
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
		base.OnStart();
		_Controller = ((Component)this).GetComponent<Controller>();
		_ScaleVector = (Vector2)(new Vector2(1f, 1f));
		InitLaser();
	}

	private void InitLaser()
	{
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
		_dotRenderer = dotCursor.GetComponent<Renderer>();
		_dotRenderer.material = Resources.GetBuiltinResource<Material>("Sprites-Default.mat");
		_dotRenderer.material.renderQueue += 5000;
		_dotRenderer.material.color = Color.cyan;
		dotCursor.SetActive(false);
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (!((Component)VR.Camera).gameObject.activeInHierarchy)
		{
			return;
		}

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
	}

	private void EnsureResizeHandler()
	{
		if ((_ResizeHandler == null))
		{
			_ResizeHandler = ((Component)_Target).GetComponent<ResizeHandler>();
			if ((_ResizeHandler == null))
			{
				_ResizeHandler = ((Component)_Target).gameObject.AddComponent<ResizeHandler>();
			}
		}
	}

	private void EnsureNoResizeHandler()
	{
		if ((_ResizeHandler != null))
		{
			UnityEngine.Object.DestroyImmediate(_ResizeHandler);
		}
		_ResizeHandler = null;
	}

	private void PulseHaptic(ushort durationMicroseconds)
	{
		if (_Controller != null && _Controller.Tracking != null)
		{
			var device = SteamVR_Controller.Input((int)_Controller.Tracking.index);
			device.TriggerHapticPulse(durationMicroseconds, EVRButtonId.k_EButton_Axis0);
		}
	}

	protected void CheckInput()
	{
		IsPressing = false;
		if (LaserVisible && (_Target != null) && !IsResizing)
		{
			if (Device.GetPressDown(EVRButtonId.k_EButton_Axis1))
			{
				IsPressing = true;
				// Use direct Win32 mouse events for reliable IMGUI click handling
				// (dropdowns, popups, sliders, etc.)
				MouseOperations.MouseEvent(WindowsInterop.MouseEventFlags.LeftDown);
				mouseDownPosition = new Vector2(Input.mousePosition.x, (float)VRGUI.Height - Input.mousePosition.y);
				PulseHaptic(800);
			}
			if (Device.GetPress(EVRButtonId.k_EButton_Axis1))
			{
				IsPressing = true;
			}
			if (Device.GetPressUp(EVRButtonId.k_EButton_Axis1))
			{
				IsPressing = true;
				MouseOperations.MouseEvent(WindowsInterop.MouseEventFlags.LeftUp);
				mouseDownPosition = null;
			}

			float thumbstickY = Device.GetAxis(EVRButtonId.k_EButton_Axis0).y;
			if (!IsPressing && Mathf.Abs(thumbstickY) > 0.7f && Time.time - _lastScrollTime > 0.05f)
			{
				WindowsInterop.mouse_event(0x0800, 0, 0, (int)(thumbstickY * 120f), 0);
				_lastScrollTime = Time.time;
				PulseHaptic(200);
			}
		}
	}

	private void CheckForNearMenu()
	{
		_Target = GUIQuadRegistry.Quads.FirstOrDefault(IsLaserable);
		if ((_Target != null))
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
		Vector3 localScale = ((Component)quad).transform.localScale;
		return Mathf.Clamp(localScale.magnitude * 2.0f, 1.5f, 5.0f);
	}

	// Directly use Laser.transform — identical to VRGIN MenuHandler
	private bool IsWithinRange(GUIQuad quad)
	{
		if (((Component)quad).transform.parent == ((Component)this).transform)
		{
			return false;
		}
		Vector3 val = -((Component)quad).transform.forward;
		_ = ((Component)quad).transform.position;
		Vector3 position = ((Component)Laser).transform.position;
		Vector3 forward = ((Component)Laser).transform.forward;
		float num = 0f - ((Component)quad).transform.InverseTransformPoint(position).z;
		Vector3 localScale = ((Component)quad).transform.localScale;
		float num2 = num * localScale.magnitude;
		if (num2 > 0f && num2 < GetRange(quad))
		{
			return Vector3.Dot(val, forward) < 0f;
		}
		return false;
	}

	// Directly use Laser.transform — identical to VRGIN MenuHandler
	private bool Raycast(GUIQuad quad, out RaycastHit hit)
	{
		Vector3 position = ((Component)Laser).transform.position;
		Vector3 forward = ((Component)Laser).transform.forward;
		Collider component = ((Component)quad).GetComponent<Collider>();
		if ((component != null))
		{
			Ray val = new Ray(position, forward);
			return component.Raycast(val, out hit, GetRange(quad));
		}
		hit = default(RaycastHit);
		return false;
	}

	// Directly use Laser.transform — identical to VRGIN MenuHandler
	private void UpdateLaser()
	{
		Vector3 laserPos = ((Component)Laser).transform.position;
		Vector3 laserEnd = laserPos + ((Component)Laser).transform.forward;
		Laser.SetPosition(0, laserPos);
		Laser.SetPosition(1, laserEnd);
		bool hitUI = false;

		if ((_Target != null) && ((Component)_Target).gameObject.activeInHierarchy)
		{
			if (IsWithinRange(_Target) && Raycast(_Target, out var hit))
			{
				hitUI = true;
				laserEnd = hit.point;
				Laser.SetPosition(1, laserEnd);

				bool stealFocus = false;
				if (ActiveMouseHandler == null || ActiveMouseHandler == this || !ActiveMouseHandler.LaserVisible)
				{
					stealFocus = true;
				}
				else if (ActiveMouseHandler.IsPressing == false && Device != null && Device.GetPress(EVRButtonId.k_EButton_Axis1))
				{
					stealFocus = true;
				}

				if (!IsOtherWorkingOn(_Target) && stealFocus)
				{
					ActiveMouseHandler = this;
					Vector2 val = default(Vector2);
					val = new Vector2(hit.textureCoord.x * (float)VRGUI.Width, (1f - hit.textureCoord.y) * (float)VRGUI.Height);
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

		Color targetColor = Color.cyan;

		if (LaserVisible && dotCursor != null)
		{
			if (hitUI)
			{
				dotCursor.SetActive(true);
				dotCursor.transform.position = laserEnd;
				if (IsPressing)
				{
					targetColor = Color.red;
					Vector3 dir = laserEnd - laserPos;
					if (dir.magnitude > 0.02f)
					{
						Laser.SetPosition(1, laserEnd - dir.normalized * 0.02f);
					}
				}
				else
				{
					targetColor = Color.green;
				}
			}
			else
			{
				dotCursor.SetActive(false);
			}
		}
		else if (dotCursor != null)
		{
			dotCursor.SetActive(false);
		}

		_currentLaserColor = Color.Lerp(_currentLaserColor, targetColor, Time.deltaTime * 10f);
		_currentDotColor = _currentLaserColor;
		Laser.SetColors(_currentLaserColor, _currentLaserColor);
		if (_dotRenderer != null)
			_dotRenderer.material.color = _currentDotColor;

		if (hitUI)
		{
			float w = 0.002f + 0.001f * Mathf.Sin(Time.time * 3f);
			Laser.SetWidth(w, w * 0.5f);
		}
		else
		{
			Laser.SetWidth(0.002f, 0.002f);
		}
	}

	private bool IsOtherWorkingOn(GUIQuad target)
	{
		return false;
	}
}

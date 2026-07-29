using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;
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

	private Vector2 _lastHitScreenPos;

	private bool _hasValidHit;

	private GameObject _clickOverrideTarget;

	private PointerEventData _clickPointerData;

	private bool _pointerDownActive;

	private bool _win32MouseDown;

	protected SteamVR_Controller.Device Device
	{
		get
		{
			if (_Controller == null || _Controller.Tracking == null ||
				_Controller.Tracking.index == SteamVR_TrackedObject.EIndex.None)
			{
				return null;
			}
			return SteamVR_Controller.Input((int)_Controller.Tracking.index);
		}
	}

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
		if (VR.Camera == null || !((Component)VR.Camera).gameObject.activeInHierarchy)
		{
			ReleasePointer(false);
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
		ReleasePointer(false);
		if (ActiveMouseHandler == this)
		{
			ActiveMouseHandler = null;
		}
	}

	private void OnDestroy()
	{
		ReleasePointer(false);
		if (ActiveMouseHandler == this)
		{
			ActiveMouseHandler = null;
		}
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

	// Finds the uGUI click target the user actually aimed at, by sorting raycast
	// hits by sortingOrder (then depth) descending — the correct visual top.
	//
	// Why this is needed: Koikatu's SortingAwareGraphicRaycaster returns hits in an
	// order where a lower-sortingOrder element (e.g. the scene-card grid, order 10)
	// can come BEFORE a higher-sortingOrder popup button (e.g. the Load confirm,
	// order 100). Unity's EventSystem always acts on results[0], so the click lands
	// on the grid behind the popup. We detect that case and dispatch the click to
	// the correct top element ourselves.
	//
	// Returns the GameObject that should receive the click, or null if no override
	// is needed (results[0] is already the top — let the normal Win32 click handle it).
	private GameObject FindClickOverrideTarget()
	{
		if (EventSystem.current == null) return null;
		var ped = new PointerEventData(EventSystem.current);
		ped.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
		var results = new List<RaycastResult>();
		EventSystem.current.RaycastAll(ped, results);
		if (results.Count < 2) return null;

		// Pick the hit with the highest sortingOrder, tie-broken by depth.
		int bestIdx = 0;
		for (int i = 1; i < results.Count; i++)
		{
			bool higher = results[i].sortingOrder > results[bestIdx].sortingOrder
				|| (results[i].sortingOrder == results[bestIdx].sortingOrder && results[i].depth > results[bestIdx].depth);
			if (higher) bestIdx = i;
		}

		// Only override when the EventSystem would pick the WRONG element, i.e. the
		// true top (by sortingOrder) is not what RaycastAll put first.
		if (results[bestIdx].sortingOrder <= results[0].sortingOrder) return null;

		// Walk up to the nearest object that actually handles a pointer click.
		return ExecuteEvents.GetEventHandler<IPointerClickHandler>(results[bestIdx].gameObject);
	}

	protected void CheckInput()
	{
		IsPressing = false;
		SteamVR_Controller.Device device = Device;
		if (device == null)
		{
			ReleasePointer(false);
			return;
		}

		bool triggerPressed = device.GetPress(EVRButtonId.k_EButton_Axis1);
		bool triggerReleased = device.GetPressUp(EVRButtonId.k_EButton_Axis1);

		// A press can outlive the laser hit. Always release it even if the UI moved,
		// the controller lost focus, or this handler was disabled between frames.
		if (_pointerDownActive)
		{
			IsPressing = triggerPressed;
			if (triggerReleased || !triggerPressed)
			{
				ReleasePointer(triggerReleased);
			}
			return;
		}

		if (LaserVisible && (_Target != null) && !IsResizing)
		{
			if (device.GetPressDown(EVRButtonId.k_EButton_Axis1))
			{
				IsPressing = true;
				if (_hasValidHit)
				{
					MouseOperations.SetClientCursorPosition((int)_lastHitScreenPos.x, (int)_lastHitScreenPos.y);
				}

				_clickOverrideTarget = FindClickOverrideTarget();
				if (_clickOverrideTarget != null)
				{
					_clickPointerData = new PointerEventData(EventSystem.current);
					_clickPointerData.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
					_clickPointerData.button = PointerEventData.InputButton.Left;
					_pointerDownActive = true;
					_win32MouseDown = false;
					ExecuteEvents.Execute(_clickOverrideTarget, _clickPointerData, ExecuteEvents.pointerDownHandler);
				}
				else
				{
					_pointerDownActive = true;
					_win32MouseDown = true;
					MouseOperations.MouseEvent(WindowsInterop.MouseEventFlags.LeftDown);
				}
				mouseDownPosition = new Vector2(Input.mousePosition.x, (float)VRGUI.Height - Input.mousePosition.y);
				PulseHaptic(800);
			}

			if (triggerPressed)
			{
				IsPressing = true;
			}

			float thumbstickY = device.GetAxis(EVRButtonId.k_EButton_Axis0).y;
			if (!IsPressing && Mathf.Abs(thumbstickY) > 0.7f && Time.time - _lastScrollTime > 0.05f)
			{
				WindowsInterop.mouse_event(0x0800, 0, 0, (int)(thumbstickY * 120f), 0);
				_lastScrollTime = Time.time;
				PulseHaptic(200);
			}
		}
	}

	private void ReleasePointer(bool sendClick)
	{
		if (!_pointerDownActive && !_win32MouseDown && _clickOverrideTarget == null)
		{
			mouseDownPosition = null;
			_clickPointerData = null;
			IsPressing = false;
			return;
		}

		try
		{
			if (_clickOverrideTarget != null && _clickPointerData != null)
			{
				_clickPointerData.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
				ExecuteEvents.Execute(_clickOverrideTarget, _clickPointerData, ExecuteEvents.pointerUpHandler);
				if (sendClick && _clickOverrideTarget.activeInHierarchy)
				{
					ExecuteEvents.Execute(_clickOverrideTarget, _clickPointerData, ExecuteEvents.pointerClickHandler);
				}
			}
			else if (_win32MouseDown)
			{
				MouseOperations.MouseEvent(WindowsInterop.MouseEventFlags.LeftUp);
			}
		}
		catch (Exception ex)
		{
			VRLog.Warn("Failed to release VR UI pointer cleanly: " + ex.Message);
		}
		finally
		{
			_pointerDownActive = false;
			_win32MouseDown = false;
			_clickOverrideTarget = null;
			_clickPointerData = null;
			mouseDownPosition = null;
			IsPressing = false;
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

	// Map a quad UV (0..1) to client-pixel coordinates using the CURRENT window
	// client size, not the size cached at startup. The IMGUI is rendered at the
	// live window resolution, so if the window was resized/maximized or DPI-scaled
	// after VRGIN init, the cached VRGUI.Width/Height would map clicks off-target —
	// landing on the large thumbnail behind a small confirm button. Using the live
	// rect keeps clicks pixel-accurate.
	private Vector2 UVToClient(float u, float v)
	{
		WindowsInterop.RECT r = WindowManager.GetClientRect();
		float w = r.Right - r.Left;
		float h = r.Bottom - r.Top;
		if (w <= 0f) w = VRGUI.Width;
		if (h <= 0f) h = VRGUI.Height;
		return new Vector2(u * w, (1f - v) * h);
	}

	// Directly use Laser.transform — identical to VRGIN MenuHandler
	private void UpdateLaser()
	{
		Vector3 laserPos = ((Component)Laser).transform.position;
		Vector3 laserEnd = laserPos + ((Component)Laser).transform.forward;
		Laser.SetPosition(0, laserPos);
		Laser.SetPosition(1, laserEnd);
		bool hitUI = false;
		_hasValidHit = false;

		if ((_Target != null) && ((Component)_Target).gameObject.activeInHierarchy)
		{
			if (IsWithinRange(_Target) && Raycast(_Target, out var hit))
			{
				hitUI = true;
				laserEnd = hit.point;
				Laser.SetPosition(1, laserEnd);

				// Always remember the exact screen position the laser points at,
				// so a click can re-assert the cursor there right before firing.
				_lastHitScreenPos = UVToClient(hit.textureCoord.x, hit.textureCoord.y);
				_hasValidHit = true;

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
					Vector2 val = UVToClient(hit.textureCoord.x, hit.textureCoord.y);
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Studio;
using UnityEngine;
using VRGIN.Controls;
using VRGIN.Controls.Handlers;
using VRGIN.Controls.Tools;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.Visuals;
using Valve.VR;

namespace KKCharaStudioVR;

internal class GripMoveKKCharaStudioTool : Tool
{
	private static GUIQuad internalGui;
	private float menuDownTime;
	private KKCharaStudioVRSettings _settings;
	private GameObject mirror1;
	private Vector3 _grabLocalPosOffset;
	private Quaternion _grabLocalRotOffset;

	private bool screenGrabbed;
	private GameObject lastGrabbedObject;
	private GameObject grabbingObject;
	private MenuHandler menuHandlder;
	private GripMenuHandler gripMenuHandler;
	private IKTool ikTool;
	private float nearestGrabable = float.MaxValue;
	private string[] FINGER_KEYS = new string[5] { "j_thumb", "j_index", "j_middle", "j_ring", "j_little" };
	private static FieldInfo f_dicGuideObject = typeof(GuideObjectManager).GetField("dicGuideObject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	private GameObject marker;
	public GameObject target;
	private bool lockRotXZ = true;
	private float lastSnapTurnTime;
	private bool _isLeftHand;
	private bool _lastHandModelEnabled;
	private MoveableGUIObject _proximityTarget;
	private GameObject _proximityHighlight;
	private int _proximityCheckCounter;
	private LineRenderer _grabLine;
	private Vector3 _smoothGrabPos;
	private Quaternion _smoothGrabRot;
	private bool _smoothGrabInitialized;

	public override Texture2D Image => UnityHelper.LoadImage("icon_gripmove.png");

	public GUIQuad Gui { get; private set; }

	private SteamVR_Controller.Device controller
	{
		get
		{
			SteamVR_TrackedObject component = ((Component)this).gameObject.GetComponent<SteamVR_TrackedObject>();
			if (component != null)
			{
				return SteamVR_Controller.Input((int)component.index);
			}
			return null;
		}
	}

	private void resetGUIPosition()
	{
		Transform head = VR.Camera.Head;
		((Component)internalGui).transform.parent = ((Component)this).transform;
		((Component)internalGui).transform.localScale = Vector3.one * 0.4f;
		if (head != null)
		{
			float dist = _settings != null ? _settings.UISpawnDistance : 1.2f;
			((Component)internalGui).transform.position = head.TransformPoint(new Vector3(0f, 0f, dist));
			((Component)internalGui).transform.rotation = Quaternion.LookRotation(head.TransformVector(new Vector3(0f, 0f, 1f)));
		}
		else
		{
			((Component)internalGui).transform.localPosition = new Vector3(0f, 0.05f, -0.06f);
			((Component)internalGui).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
		}
		((Component)internalGui).transform.parent = ((Component)this).transform.parent;
		internalGui.UpdateAspect();
	}

	private IEnumerator DelayedResetGUI()
	{
		// Wait for VR camera head to be fully initialized
		yield return new WaitForSeconds(1.0f);
		if (internalGui != null)
		{
			resetGUIPosition();
		}
	}



	protected override void OnDestroy()
	{
		if (_proximityHighlight != null)
			UnityEngine.Object.Destroy(_proximityHighlight);
		if (_grabLine != null)
			UnityEngine.Object.Destroy(((Component)_grabLine).gameObject);
		if (marker != null)
			UnityEngine.Object.Destroy(marker);
		if (mirror1 != null)
			UnityEngine.Object.Destroy(mirror1);
		// Note: We no longer destroy internalGui here because it is a static shared instance across both controllers
	}

	protected override void OnStart()
	{
		base.OnStart();
		try
		{
			VRLog.Info("Loading GripMoveTool");
			_settings = VR.Manager.Context.Settings as KKCharaStudioVRSettings;
			if (internalGui == null)
			{
				internalGui = GUIQuad.Create();
				((Component)internalGui).gameObject.AddComponent<MoveableGUIObject>();
				((Component)internalGui).gameObject.AddComponent<BoxCollider>();
				internalGui.IsOwned = true;
				UnityEngine.Object.DontDestroyOnLoad(((Component)internalGui).gameObject);
				// Delay position reset so VR camera head is fully initialized
				((MonoBehaviour)this).StartCoroutine(DelayedResetGUI());
			}

			gripMenuHandler = ((Component)this).gameObject.AddComponent<GripMenuHandler>();
			((Behaviour)gripMenuHandler).enabled = false;
		}
		catch (Exception obj)
		{
			VRLog.Info(obj);
		}
		if (marker == null)
		{
			marker = new GameObject("__GripMoveMarker__");
			marker.transform.parent = ((Component)this).transform.parent;
			marker.transform.position = ((Component)this).transform.position;
			marker.transform.rotation = ((Component)this).transform.rotation;
		}
		menuHandlder = ((Component)this).GetComponent<MenuHandler>();
		// Permanently disable VRGIN MenuHandler to prevent two-hand trigger resize/move interference
		if (menuHandlder != null)
			((Behaviour)menuHandlder).enabled = false;
		ikTool = IKTool.instance;
		_isLeftHand = ((Component)this).GetComponent<VRGIN.Controls.LeftController>() != null;
		_lastHandModelEnabled = _settings != null && _settings.HandModelEnabled;
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		ClearProximityHighlight();
		if (_grabLine != null) ((Component)_grabLine).gameObject.SetActive(false);
		if (gripMenuHandler != null)
			((Behaviour)gripMenuHandler).enabled = false;
		// Keep VRGIN MenuHandler disabled (was permanently disabled in OnStart)
		if ((internalGui != null))
			((Component)internalGui).gameObject.SetActive(false);

		// Don't hide hand models during tool switching — keep them visible
		// Only hide if settings explicitly disable hand models
		bool handEnabled = _settings != null && _settings.HandModelEnabled;
		if (!handEnabled)
		{
			if (VRHandModelManager.Instance != null)
				VRHandModelManager.Instance.SetHandVisible(_isLeftHand, false);
			if (Owner != null)
				Owner.SetRenderModelVisible(true);
		}
	}

	private void ApplyHandModelState(bool handEnabled)
	{
		if (handEnabled)
		{
			if (VRHandModelManager.Instance != null)
				VRHandModelManager.Instance.SetHandVisible(_isLeftHand, true);
			if (Owner != null)
			{
				Owner.SetRenderModelVisible(false);
				// Hide the VRGIN tool icon circle and alpha concealer sphere
				var canvas = ((Component)Owner).GetComponentInChildren<Canvas>(true);
				if (canvas != null) ((Component)canvas).gameObject.SetActive(false);
				// Hide AlphaConcealer — it's a Sphere primitive parented directly to Controller
				HideAlphaConcealer(true);
			}
		}
		else
		{
			if (VRHandModelManager.Instance != null)
				VRHandModelManager.Instance.SetHandVisible(_isLeftHand, false);
			if (Owner != null)
			{
				Owner.SetRenderModelVisible(true);
				var canvas = ((Component)Owner).GetComponentInChildren<Canvas>(true);
				if (canvas != null) ((Component)canvas).gameObject.SetActive(true);
				HideAlphaConcealer(false);
			}
		}
	}

	private void HideAlphaConcealer(bool hide)
	{
		if (Owner == null) return;
		// The AlphaConcealer is a Sphere primitive child of the Controller transform
		// It has a MeshFilter with "Sphere" mesh and a Renderer but no name set by VRGIN
		Transform controllerT = ((Component)Owner).transform;
		for (int i = 0; i < controllerT.childCount; i++)
		{
			Transform child = controllerT.GetChild(i);
			MeshFilter mf = ((Component)child).GetComponent<MeshFilter>();
			if (mf != null && mf.sharedMesh != null && mf.sharedMesh.name.Contains("Sphere"))
			{
				((Component)child).gameObject.SetActive(!hide);
				break;
			}
		}
	}

	private void UpdateProximityDetection()
	{
		// Only when hand model is active and not currently grabbing
		if (_settings == null || !_settings.HandModelEnabled || !_settings.ProximityGrabEnabled || grabbingObject != null)
		{
			ClearProximityHighlight();
			return;
		}

		// Throttle: check every 3 frames to save performance
		_proximityCheckCounter++;
		if (_proximityCheckCounter < 3) 
		{
			// Still update highlight position even on skip frames
			if (_proximityTarget != null && _proximityHighlight != null && _proximityHighlight.activeSelf)
			{
				_proximityHighlight.transform.position = _proximityTarget.transform.position;
			}
			return;
		}
		_proximityCheckCounter = 0;

		Vector3 handPos = ((Component)this).transform.position;
		// Move detection center slightly forward (toward fingers)
		handPos += ((Component)this).transform.forward * 0.05f;

		float grabRadius = _settings != null ? _settings.ProximityGrabRadius : 0.12f;
		Collider[] nearby = Physics.OverlapSphere(handPos, grabRadius);

		float nearestDist = float.MaxValue;
		MoveableGUIObject nearest = null;

		foreach (var col in nearby)
		{
			if (col == null) continue;
			MoveableGUIObject mgo = ((Component)col).GetComponent<MoveableGUIObject>();
			// Only consider character IK targets, not GUI elements
			if (mgo == null || mgo.guideObject == null) continue;

			float dist = Vector3.Distance(((Component)col).transform.position, handPos);
			if (dist < nearestDist)
			{
				nearestDist = dist;
				nearest = mgo;
			}
		}

		if (nearest != _proximityTarget)
		{
			ClearProximityHighlight();
			_proximityTarget = nearest;
			if (_proximityTarget != null)
			{
				ShowProximityHighlight(_proximityTarget);
			}
		}
		else if (_proximityTarget != null && _proximityHighlight != null && _proximityHighlight.activeSelf)
		{
			// Update position for existing highlight
			_proximityHighlight.transform.position = _proximityTarget.transform.position;
		}

		// 高亮脉冲动画（缓慢呼吸效果）
		if (_proximityHighlight != null && _proximityHighlight.activeSelf)
		{
			float pulse = 0.035f * (1f + 0.2f * Mathf.Sin(Time.time * 5f));
			_proximityHighlight.transform.localScale = Vector3.one * pulse;
		}
	}

	private void ShowProximityHighlight(MoveableGUIObject target)
	{
		if (_proximityHighlight == null)
		{
			_proximityHighlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			((UnityEngine.Object)_proximityHighlight).name = "_VRProximityHighlight";
			UnityEngine.Object.Destroy(_proximityHighlight.GetComponent<Collider>());
			Renderer r = _proximityHighlight.GetComponent<Renderer>();
			r.material = new Material(MaterialHelper.GetColorZOrderShader());
			r.material.color = new Color(0f, 1f, 0.5f, 0.35f);
			r.material.renderQueue = 3500;
		}
		_proximityHighlight.SetActive(true);
		_proximityHighlight.transform.position = ((Component)target).transform.position;
		_proximityHighlight.transform.localScale = Vector3.one * 0.035f;
	}

	private void ClearProximityHighlight()
	{
		_proximityTarget = null;
		if (_proximityHighlight != null)
			_proximityHighlight.SetActive(false);
	}

	private void UpdateGrabLine()
	{
		if (grabbingObject == null)
		{
			if (_grabLine != null)
				((Component)_grabLine).gameObject.SetActive(false);
			return;
		}

		// 只对有 guideObject 的 IK 目标显示连线
		MoveableGUIObject mgo = grabbingObject.GetComponent<MoveableGUIObject>();
		if (mgo == null || mgo.guideObject == null)
		{
			if (_grabLine != null)
				((Component)_grabLine).gameObject.SetActive(false);
			return;
		}

		// 延迟创建 LineRenderer
		if (_grabLine == null)
		{
			GameObject lineObj = new GameObject("_VRGrabLine");
			UnityEngine.Object.DontDestroyOnLoad(lineObj);
			_grabLine = lineObj.AddComponent<LineRenderer>();
			_grabLine.material = new Material(MaterialHelper.GetColorZOrderShader());
			_grabLine.material.renderQueue = 3600;
			_grabLine.SetVertexCount(2);
			_grabLine.useWorldSpace = true;
			_grabLine.SetWidth(0.005f, 0.002f);
		}

		((Component)_grabLine).gameObject.SetActive(true);

		Vector3 handPos = ((Component)this).transform.position;
		Vector3 targetPos = mgo.guideObject.transformTarget.position;
		float dist = Vector3.Distance(handPos, targetPos);

		// 颜色根据距离变化：近=绿色，中=黄色，远=橙色
		Color lineColor;
		if (dist < 0.1f)
			lineColor = new Color(0f, 1f, 0.5f, 0.4f);
		else if (dist < 0.3f)
			lineColor = new Color(1f, 1f, 0f, 0.5f);
		else
			lineColor = new Color(1f, 0.5f, 0f, 0.7f);

		_grabLine.material.color = lineColor;
		_grabLine.SetPosition(0, handPos);
		_grabLine.SetPosition(1, targetPos);
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		if (gripMenuHandler != null)
			((Behaviour)gripMenuHandler).enabled = true;
		// Keep VRGIN MenuHandler disabled
		if ((internalGui != null))
			((Component)internalGui).gameObject.SetActive(true);

		bool handEnabled = _settings != null && _settings.HandModelEnabled;
		_lastHandModelEnabled = handEnabled;
		ApplyHandModelState(handEnabled);
	}

	protected override void OnLevel(int level)
	{
		base.OnLevel(level);
		((MonoBehaviour)this).StopAllCoroutines();
		ClearProximityHighlight();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (controller == null)
		{
			return;
		}

		// 运行时检测设置变化
		bool currentHandEnabled = _settings != null && _settings.HandModelEnabled;
		if (currentHandEnabled != _lastHandModelEnabled)
		{
			_lastHandModelEnabled = currentHandEnabled;
			ApplyHandModelState(currentHandEnabled);
		}

		UpdateProximityDetection();
		HandleThumbstickLocomotion();
		HandleButtonEvents();
		HandleObjectGrab();
		HandleGripWorldMove();
		UpdateGrabLine();

		if (lastGrabbedObject != null && grabbingObject == null)
		{
			float dist = Vector3.Distance(((Component)this).transform.position, lastGrabbedObject.transform.position);
			if (dist > 0.25f)
			{
				lastGrabbedObject = null;
				screenGrabbed = false;
			}
		}
		nearestGrabable = float.MaxValue;
		marker.transform.position = ((Component)this).transform.position;
		marker.transform.rotation = ((Component)this).transform.rotation;
	}

	private void HandleThumbstickLocomotion()
	{
		if (gripMenuHandler != null && gripMenuHandler.LaserVisible) return;
		Vector2 axis = controller.GetAxis(EVRButtonId.k_EButton_SteamVR_Touchpad);
		bool isLeft = _isLeftHand;

		// Right thumbstick click: reset view orientation (fixes MMD animation rotation)
		if (!isLeft && controller.GetPressDown(EVRButtonId.k_EButton_SteamVR_Touchpad))
		{
			Transform origin = VR.Camera.SteamCam.origin;
			if (origin != null)
			{
				Vector3 euler = origin.eulerAngles;
				origin.rotation = Quaternion.Euler(0f, euler.y, 0f);
			}
		}

		if (Mathf.Abs(axis.y) > 0.1f || Mathf.Abs(axis.x) > 0.1f)
		{
			Transform head = VR.Camera.Head;
			Transform origin = VR.Camera.SteamCam.origin;

			if (origin != null && head != null)
			{
				if (isLeft)
				{
					Vector3 forward = head.forward;
					forward.y = 0f;
					forward.Normalize();
					Vector3 right = head.right;
					right.y = 0f;
					right.Normalize();
					
					float speed = _settings != null ? _settings.LocomotionSpeed : 2.0f;
					origin.position += (forward * axis.y + right * axis.x) * speed * Time.deltaTime;
				}
				else
				{
					// Right stick X = turn, Y = height adjustment
					bool smoothTurn = _settings != null && _settings.SmoothTurnEnabled;
					if (smoothTurn)
					{
						float turnSpeed = _settings != null ? _settings.SmoothTurnSpeed : 90f;
						origin.RotateAround(head.position, Vector3.up, axis.x * turnSpeed * Time.deltaTime);
					}
					else if (Mathf.Abs(axis.x) > 0.5f)
					{
						float cooldown = _settings != null ? _settings.SnapTurnCooldown : 0.3f;
						if (Time.time - lastSnapTurnTime > cooldown)
						{
							float angle = _settings != null ? _settings.SnapTurnAngle : 45f;
							origin.RotateAround(head.position, Vector3.up, Mathf.Sign(axis.x) * angle);
							lastSnapTurnTime = Time.time;
						}
					}

					// Y-axis: height (up/down) adjustment
					if (Mathf.Abs(axis.y) > 0.1f)
					{
						float heightSpeed = _settings != null ? _settings.LocomotionSpeed : 2.0f;
						origin.position += Vector3.up * axis.y * heightSpeed * Time.deltaTime;
					}
				}

				// 通知舒适暗角：正在移动
				if (VRComfortVignette.Instance != null)
					VRComfortVignette.Instance.SetMoving(true);
			}
		}
		else
		{
			// 通知舒适暗角：停止移动
			if (VRComfortVignette.Instance != null)
				VRComfortVignette.Instance.SetMoving(false);
		}
	}

	private void HandleButtonEvents()
	{
		if (controller.GetPressDown(EVRButtonId.k_EButton_ApplicationMenu))
		{
			menuDownTime = Time.time;
		}

		if (controller.GetPress(EVRButtonId.k_EButton_ApplicationMenu) && Time.time - menuDownTime > 1.5f)
		{
			resetGUIPosition();
			menuDownTime = Time.time;
		}

		if (controller.GetPress(EVRButtonId.k_EButton_Axis1) && controller.GetPress(EVRButtonId.k_EButton_Grip) && controller.GetPress(EVRButtonId.k_EButton_ApplicationMenu) && Time.time - menuDownTime > 0.5f)
		{
			lockRotXZ = !lockRotXZ;
			if (lockRotXZ)
			{
				ResetRotation();
			}
			menuDownTime = Time.time;
		}

		// When the laser is hitting the UI quad, let GripMenuHandler handle ALL trigger input.
		// Skip trigger handling here to prevent VRToggleObjectSelectOnCursor from interfering
		// with UI clicks (dropdowns, sliders, settings panels, etc).
		if (gripMenuHandler != null && gripMenuHandler.LaserVisible)
			return;

		bool flag = false;

		// 近距离高亮目标：Trigger 点击自动在工作区树中选中
		if (controller.GetPressDown(EVRButtonId.k_EButton_Axis1) && _proximityTarget != null
		    && _proximityTarget.guideObject != null)
		{
			GuideObject pg = _proximityTarget.guideObject;
			if (pg.guideSelect != null
			    && pg.guideSelect.treeNodeObject != null)
			{
				pg.guideSelect.treeNodeObject.OnClickSelect();
			}
			else
			{
				Singleton<GuideObjectManager>.Instance.selectObject = pg;
			}
			flag = true;
		}

		// 直接接触目标：Trigger 点击选中（与上面互斥）
		if (!flag && controller.GetPressDown(EVRButtonId.k_EButton_Axis1) && lastGrabbedObject != null && lastGrabbedObject.GetComponent<MoveableGUIObject>() != null)
		{
			GuideObject guideObject = lastGrabbedObject.GetComponent<MoveableGUIObject>().guideObject;
			if (guideObject != null)
			{
				if (guideObject.guideSelect != null && guideObject.guideSelect.treeNodeObject != null)
				{
					guideObject.guideSelect.treeNodeObject.OnClickSelect();
				}
				else
				{
					Singleton<GuideObjectManager>.Instance.selectObject = guideObject;
				}
				flag = true;
			}
		}
	}

	private void HandleObjectGrab()
	{
		// Proximity grab: when grip pressed near character IK target but not currently grabbing
		if (controller.GetPressDown(EVRButtonId.k_EButton_Grip) && grabbingObject == null && _proximityTarget != null)
		{
			screenGrabbed = true;
			lastGrabbedObject = ((Component)_proximityTarget).gameObject;
			ClearProximityHighlight();
		}

		bool pressDown = controller.GetPressDown(EVRButtonId.k_EButton_Grip);
		bool press = controller.GetPress(EVRButtonId.k_EButton_Grip);
		bool pressUp = controller.GetPressUp(EVRButtonId.k_EButton_Grip);

		if (pressDown && screenGrabbed && lastGrabbedObject != null)
		{
			grabbingObject = lastGrabbedObject;
			_grabLocalPosOffset = ((Component)this).transform.InverseTransformPoint(grabbingObject.transform.position);
			_grabLocalRotOffset = Quaternion.Inverse(((Component)this).transform.rotation) * grabbingObject.transform.rotation;

			if (lastGrabbedObject.GetComponent<MoveableGUIObject>() != null)
			{
				MoveableGUIObject component = lastGrabbedObject.GetComponent<MoveableGUIObject>();
				if (component.guideObject != null)
				{
					ApplyFingerFKIfNeeded(component.guideObject);
					_grabLocalRotOffset = Quaternion.Inverse(((Component)this).transform.rotation) * component.guideObject.transformTarget.rotation;
					component.OnMoveStart();
				}
			}

			// 抓取开始触觉反馈
			if (controller != null)
				controller.TriggerHapticPulse(1500, EVRButtonId.k_EButton_Axis0);
			_smoothGrabInitialized = false;
		}

		if (press && grabbingObject != null)
		{
			Vector3 targetPos = ((Component)this).transform.TransformPoint(_grabLocalPosOffset);
			Quaternion targetRot = ((Component)this).transform.rotation * _grabLocalRotOffset;

			// 检测是否为 IK 目标，决定是否使用平滑插值
			MoveableGUIObject mgo = grabbingObject.GetComponent<MoveableGUIObject>();
			bool isIKTarget = mgo != null && mgo.guideObject != null;

			if (isIKTarget)
			{
				// IK 目标使用弹簧阻尼跟随，让操控有物理感
				if (!_smoothGrabInitialized)
				{
					_smoothGrabPos = targetPos;
					_smoothGrabRot = targetRot;
					_smoothGrabInitialized = true;
				}

				float smoothness = 15f; // 约 0.07 秒达到 63%，轻微弹簧感
				_smoothGrabPos = Vector3.Lerp(_smoothGrabPos, targetPos, Time.deltaTime * smoothness);
				_smoothGrabRot = Quaternion.Slerp(_smoothGrabRot, targetRot, Time.deltaTime * smoothness);

				grabbingObject.transform.position = _smoothGrabPos;
				grabbingObject.transform.rotation = _smoothGrabRot;
			}
			else
			{
				// 非 IK 目标（GUI 面板等）直接跟随
				grabbingObject.transform.position = targetPos;
				grabbingObject.transform.rotation = targetRot;
			}

			if (mgo != null)
			{
				mgo.OnMoved();
			}
		}

		if (pressUp)
		{
			if (grabbingObject != null)
			{
				if (grabbingObject.GetComponent<MoveableGUIObject>() != null)
				{
					grabbingObject.GetComponent<MoveableGUIObject>().OnReleased();
				}
				// 释放触觉反馈
				if (controller != null)
					controller.TriggerHapticPulse(800, EVRButtonId.k_EButton_Axis0);
				_smoothGrabInitialized = false;
				grabbingObject = null;
			}
			screenGrabbed = false;
			lastGrabbedObject = null;
		}
	}

	private void HandleGripWorldMove()
	{
		// 双手缩放时跳过世界移动，避免冲突
		if (VRTwoHandScale.Instance != null && VRTwoHandScale.Instance.IsScaling)
			return;

		if (controller.GetPress(EVRButtonId.k_EButton_Grip) && grabbingObject == null)
		{
			target = ((Component)VR.Camera.SteamCam.origin).gameObject;
			if (target != null)
			{
				if (mirror1 == null)
				{
					mirror1 = new GameObject("__GripMoveMirror1__");
					mirror1.transform.position = ((Component)this).transform.position;
					mirror1.transform.rotation = ((Component)this).transform.rotation;
				}
				Vector3 val = marker.transform.position - ((Component)this).transform.position;
				Quaternion q = marker.transform.rotation * Quaternion.Inverse(((Component)this).transform.rotation);
				Quaternion val2 = RemoveLockedAxisRot(q);
				Transform parent = target.transform.parent;
				mirror1.transform.position = ((Component)this).transform.position;
				mirror1.transform.rotation = ((Component)this).transform.rotation;
				target.transform.parent = mirror1.transform;
				mirror1.transform.rotation = val2 * mirror1.transform.rotation;
				mirror1.transform.position = mirror1.transform.position + val;
				target.transform.parent = parent;
			}
		}
	}

	private void ApplyFingerFKIfNeeded(GuideObject guideObject)
	{
		List<GuideObject> list = new List<GuideObject>();
		if (IsFinger(guideObject.transformTarget))
		{
			list.Add(guideObject);
		}
		foreach (GuideObject item in list)
		{
			item.transformTarget.localEulerAngles = item.changeAmount.rot;
		}
	}

	private bool IsFinger(Transform t)
	{
		string[] fINGER_KEYS = FINGER_KEYS;
		foreach (string value in fINGER_KEYS)
		{
			if (((UnityEngine.Object)t).name.Contains(value))
			{
				return true;
			}
		}
		return false;
	}

	public override List<HelpText> GetHelpTexts()
	{
		return new List<HelpText>(new HelpText[3]
		{
			HelpText.Create("Thumbstick to Move/Turn", FindAttachPosition("touchpad"), new Vector3(0.06f, 0.04f, 0f)),
			HelpText.Create("Grip to grab world or UI panels", FindAttachPosition("rgrip"), new Vector3(0.06f, 0.04f, 0f)),
			HelpText.Create("Trigger to click UI or select objects", FindAttachPosition("trigger"), new Vector3(-0.06f, -0.04f, 0f))
		});
	}

	private void ResetRotation()
	{
		if (target != null)
		{
			Quaternion rotation = target.transform.rotation;
			Vector3 eulerAngles = rotation.eulerAngles;
			eulerAngles.x = 0f;
			eulerAngles.z = 0f;
			target.transform.rotation = Quaternion.Euler(eulerAngles);
		}
	}

	private IEnumerator UpdateMarkerPos()
	{
		yield return (object)new WaitForEndOfFrame();
		marker.transform.position = ((Component)this).transform.position;
		marker.transform.rotation = ((Component)this).transform.rotation;
	}

	private Quaternion RemoveLockedAxisRot(Quaternion q)
	{
		if (lockRotXZ)
		{
			return RemoveXZRot(q);
		}
		return q;
	}

	public static Quaternion RemoveXZRot(Quaternion q)
	{
		Vector3 eulerAngles = q.eulerAngles;
		eulerAngles.x = 0f;
		eulerAngles.z = 0f;
		return Quaternion.Euler(eulerAngles);
	}

	private void OnTriggerStay(Collider collider)
	{
		if (((Component)collider).GetComponent<GUIQuad>() != null)
		{
			screenGrabbed = true;
			lastGrabbedObject = ((Component)collider).gameObject;
		}
		else if (((Component)collider).GetComponent<MoveableGUIObject>() != null)
		{
			screenGrabbed = true;
			if (lastGrabbedObject != null)
			{
				Vector3 val = ((Component)collider).gameObject.transform.position - ((Component)this).transform.position;
				float sqrMagnitude = val.sqrMagnitude;
				if (sqrMagnitude < nearestGrabable)
				{
					lastGrabbedObject = ((Component)collider).gameObject;
					nearestGrabable = sqrMagnitude;
				}
			}
			else
			{
				lastGrabbedObject = ((Component)collider).gameObject;
			}
		}

	}

	private void OnTriggerEnter(Collider collider)
	{
	}

	private void OnTriggerExit(Collider collider)
	{
		GameObject gameObject = ((Component)collider).gameObject;
		if (screenGrabbed && ((Component)collider).GetComponent<MoveableGUIObject>() != null && gameObject == lastGrabbedObject)
		{

			screenGrabbed = false;
			lastGrabbedObject = null;
		}
	}
}

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
	private GUIQuad internalGui;
	private float menuDownTime;
	private KKCharaStudioVRSettings _settings;
	private GameObject mirror1;
	private GameObject grabHandle;
	private GameObject pointer;
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

	public override Texture2D Image => UnityHelper.LoadImage("icon_gripmove.png");

	public GUIQuad Gui { get; private set; }

	private SteamVR_Controller.Device controller
	{
		get
		{
			SteamVR_TrackedObject component = ((Component)this).gameObject.GetComponent<SteamVR_TrackedObject>();
			if ((Object)(object)component != (Object)null)
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
		if ((Object)(object)head != (Object)null)
		{
			((Component)internalGui).transform.position = head.TransformPoint(new Vector3(0f, 0f, 0.3f));
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

	private void CreatePointer()
	{
		if ((Object)(object)pointer == (Object)null)
		{
			pointer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			((Object)pointer).name = "pointer";
			pointer.GetComponent<SphereCollider>();
			pointer.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
			pointer.transform.parent = ((Component)this).transform;
			pointer.transform.localPosition = new Vector3(0f, -0.03f, 0.03f);
			Renderer component = pointer.GetComponent<Renderer>();
			component.enabled = true;
			Material material = new Material(MaterialHelper.GetColorZOrderShader());
			component.material = material;
		}
	}

	protected override void OnDestroy()
	{
		if ((Object)(object)marker != (Object)null)
			Object.Destroy((Object)(object)marker);
		if ((Object)(object)mirror1 != (Object)null)
			Object.Destroy((Object)(object)mirror1);
		if ((Object)(object)grabHandle != (Object)null)
			Object.Destroy((Object)(object)grabHandle);
		if ((Object)(object)internalGui != (Object)null)
			Object.DestroyImmediate((Object)(object)((Component)internalGui).gameObject);
	}

	protected override void OnStart()
	{
		base.OnStart();
		try
		{
			VRLog.Info("Loading GripMoveTool");
			_settings = VR.Manager.Context.Settings as KKCharaStudioVRSettings;
			internalGui = GUIQuad.Create();
			resetGUIPosition();
			((Component)internalGui).gameObject.AddComponent<MoveableGUIObject>();
			((Component)internalGui).gameObject.AddComponent<BoxCollider>();
			internalGui.IsOwned = true;
			Object.DontDestroyOnLoad((Object)(object)((Component)internalGui).gameObject);
			CreatePointer();
			gripMenuHandler = ((Component)this).gameObject.AddComponent<GripMenuHandler>();
			((Behaviour)gripMenuHandler).enabled = false;
		}
		catch (Exception obj)
		{
			VRLog.Info(obj);
		}
		if ((Object)(object)marker == (Object)null)
		{
			marker = new GameObject("__GripMoveMarker__");
			marker.transform.parent = ((Component)this).transform.parent;
			marker.transform.position = ((Component)this).transform.position;
			marker.transform.rotation = ((Component)this).transform.rotation;
		}
		menuHandlder = ((Component)this).GetComponent<MenuHandler>();
		ikTool = IKTool.instance;
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		if ((Object)(object)gripMenuHandler != (Object)null)
			((Behaviour)gripMenuHandler).enabled = false;
		if ((Object)(object)menuHandlder != (Object)null)
			((Behaviour)menuHandlder).enabled = true;
		if (Object.op_Implicit((Object)(object)internalGui))
			((Component)internalGui).gameObject.SetActive(false);
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		if ((Object)(object)gripMenuHandler != (Object)null)
			((Behaviour)gripMenuHandler).enabled = true;
		if ((Object)(object)menuHandlder != (Object)null)
			((Behaviour)menuHandlder).enabled = false;
		if (Object.op_Implicit((Object)(object)internalGui))
			((Component)internalGui).gameObject.SetActive(true);
	}

	protected override void OnLevel(int level)
	{
		base.OnLevel(level);
		((MonoBehaviour)this).StopAllCoroutines();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (controller == null)
		{
			return;
		}

		HandleThumbstickLocomotion();
		HandleButtonEvents();
		HandleObjectGrab();
		HandleGripWorldMove();

		lastGrabbedObject = null;
		nearestGrabable = float.MaxValue;
		marker.transform.position = ((Component)this).transform.position;
		marker.transform.rotation = ((Component)this).transform.rotation;
	}

	private void HandleThumbstickLocomotion()
	{
		Vector2 axis = controller.GetAxis(EVRButtonId.k_EButton_SteamVR_Touchpad);
		bool isLeft = ((Component)this).GetComponent<VRGIN.Controls.LeftController>() != null;

		if (Mathf.Abs(axis.y) > 0.1f || Mathf.Abs(axis.x) > 0.1f)
		{
			Transform head = VR.Camera.Head;
			Transform origin = VR.Camera.SteamCam.origin;

			if ((Object)(object)origin != (Object)null && (Object)(object)head != (Object)null)
			{
				if (isLeft)
				{
					Vector3 forward = head.forward;
					forward.y = 0f;
					((Vector3)(ref forward)).Normalize();
					Vector3 right = head.right;
					right.y = 0f;
					((Vector3)(ref right)).Normalize();
					
					float speed = _settings != null ? _settings.LocomotionSpeed : 2.0f;
					origin.position += (forward * axis.y + right * axis.x) * speed * Time.deltaTime;
				}
				else
				{
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
				}
			}
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

		bool flag = false;
		if (controller.GetPressDown(EVRButtonId.k_EButton_Axis1) && (Object)(object)lastGrabbedObject != (Object)null && (Object)(object)lastGrabbedObject.GetComponent<MoveableGUIObject>() != (Object)null)
		{
			GuideObject guideObject = lastGrabbedObject.GetComponent<MoveableGUIObject>().guideObject;
			if ((Object)(object)guideObject != (Object)null)
			{
				if ((Object)(object)guideObject.guideSelect != (Object)null && (Object)(object)guideObject.guideSelect.treeNodeObject != (Object)null)
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

		if (controller.GetPressDown(EVRButtonId.k_EButton_Axis1) && !flag)
		{
			VRLog.Info("Called on Select VRToggle");
			if (Object.op_Implicit((Object)(object)gripMenuHandler) && gripMenuHandler.LaserVisible)
			{
				VRItemObjMoveHelper.Instance.VRToggleObjectSelectOnCursor();
			}
		}
	}

	private void HandleObjectGrab()
	{
		if ((Object)(object)grabHandle == (Object)null)
		{
			grabHandle = new GameObject("__GripMoveGrabHandle__");
			grabHandle.transform.parent = ((Component)this).transform;
			grabHandle.transform.position = ((Component)this).transform.position;
			grabHandle.transform.rotation = ((Component)this).transform.rotation;
		}

		bool pressDown = controller.GetPressDown(EVRButtonId.k_EButton_Grip);
		bool press = controller.GetPress(EVRButtonId.k_EButton_Grip);
		bool pressUp = controller.GetPressUp(EVRButtonId.k_EButton_Grip);

		if (pressDown && screenGrabbed && (Object)(object)lastGrabbedObject != (Object)null && (Object)(object)grabHandle != (Object)null)
		{
			grabbingObject = lastGrabbedObject;
			grabHandle.transform.position = lastGrabbedObject.transform.position;
			grabHandle.transform.rotation = lastGrabbedObject.transform.rotation;
			if ((Object)(object)lastGrabbedObject.GetComponent<MoveableGUIObject>() != (Object)null)
			{
				MoveableGUIObject component = lastGrabbedObject.GetComponent<MoveableGUIObject>();
				if ((Object)(object)component.guideObject != (Object)null)
				{
					ApplyFingerFKIfNeeded(component.guideObject);
					grabHandle.transform.rotation = component.guideObject.transformTarget.rotation;
					grabbingObject.transform.rotation = component.guideObject.transformTarget.rotation;
					component.OnMoveStart();
				}
			}
		}

		if (press && (Object)(object)grabbingObject != (Object)null)
		{
			grabbingObject.transform.position = grabHandle.transform.position;
			grabbingObject.transform.rotation = grabHandle.transform.rotation;
			if ((Object)(object)grabbingObject.GetComponent<MoveableGUIObject>() != (Object)null)
			{
				grabbingObject.GetComponent<MoveableGUIObject>().OnMoved();
			}
		}

		if (screenGrabbed && (Object)(object)grabbingObject != (Object)null && pressUp)
		{
			if ((Object)(object)grabbingObject.GetComponent<MoveableGUIObject>() != (Object)null)
			{
				grabbingObject.GetComponent<MoveableGUIObject>().OnReleased();
			}
			grabbingObject = null;
		}
	}

	private void HandleGripWorldMove()
	{
		if (controller.GetPress(EVRButtonId.k_EButton_Grip) && (Object)(object)grabbingObject == (Object)null)
		{
			target = ((Component)VR.Camera.SteamCam.origin).gameObject;
			if ((Object)(object)target != (Object)null)
			{
				if ((Object)(object)mirror1 == (Object)null)
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
			if (((Object)t).name.Contains(value))
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
		if ((Object)(object)target != (Object)null)
		{
			Quaternion rotation = target.transform.rotation;
			Vector3 eulerAngles = ((Quaternion)(ref rotation)).eulerAngles;
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
		Vector3 eulerAngles = ((Quaternion)(ref q)).eulerAngles;
		eulerAngles.x = 0f;
		eulerAngles.z = 0f;
		return Quaternion.Euler(eulerAngles);
	}

	private void OnTriggerStay(Collider collider)
	{
		if ((Object)(object)((Component)collider).GetComponent<GUIQuad>() != (Object)null)
		{
			screenGrabbed = true;
			lastGrabbedObject = ((Component)collider).gameObject;
		}
		else if ((Object)(object)((Component)collider).GetComponent<MoveableGUIObject>() != (Object)null)
		{
			screenGrabbed = true;
			if ((Object)(object)lastGrabbedObject != (Object)null)
			{
				Vector3 val = ((Component)collider).gameObject.transform.position - pointer.transform.position;
				float sqrMagnitude = ((Vector3)(ref val)).sqrMagnitude;
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
		if (screenGrabbed && (Object)(object)lastGrabbedObject != (Object)null && (Object)(object)pointer != (Object)null)
		{
			((Renderer)pointer.GetComponent<MeshRenderer>()).material.color = Color.red;
		}
	}

	private void OnTriggerEnter(Collider collider)
	{
	}

	private void OnTriggerExit(Collider collider)
	{
		GameObject gameObject = ((Component)collider).gameObject;
		if (screenGrabbed && (Object)(object)((Component)collider).GetComponent<MoveableGUIObject>() != (Object)null && (Object)(object)gameObject == (Object)(object)lastGrabbedObject)
		{
			((Renderer)pointer.GetComponent<MeshRenderer>()).material.color = Color.white;
			screenGrabbed = false;
			lastGrabbedObject = null;
		}
	}
}

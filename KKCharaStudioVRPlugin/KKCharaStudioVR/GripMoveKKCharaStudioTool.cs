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

	private float pressDownTime;

	private Vector2 touchDownPosition;

	private float menuDownTime;

	private float touchpadDownTime;

	private double _DeltaX;

	private double _DeltaY;

	private EVRButtonId moveSelfButton = EVRButtonId.k_EButton_Grip;

	private EVRButtonId grabScreenButton = EVRButtonId.k_EButton_Axis1;

	private string moveSelfButtonName = "rgrip";

	private KKCharaStudioVRSettings _settings;

	private float triggerDownTime;

	private float gripDownTime;

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
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Expected O, but got Unknown
		if ((Object)(object)pointer == (Object)null)
		{
			pointer = GameObject.CreatePrimitive((PrimitiveType)0);
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
		{
			Object.Destroy((Object)(object)marker);
		}
		if ((Object)(object)mirror1 != (Object)null)
		{
			Object.Destroy((Object)(object)mirror1);
		}
		if ((Object)(object)grabHandle != (Object)null)
		{
			Object.Destroy((Object)(object)grabHandle);
		}
		if ((Object)(object)internalGui != (Object)null)
		{
			Object.DestroyImmediate((Object)(object)((Component)internalGui).gameObject);
		}
	}

	protected override void OnStart()
	{
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Expected O, but got Unknown
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
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
		if (_settings != null)
		{
			moveSelfButton = EVRButtonId.k_EButton_Grip;
			moveSelfButtonName = "rgrip";
			grabScreenButton = EVRButtonId.k_EButton_Axis1;
		}
		menuHandlder = ((Component)this).GetComponent<MenuHandler>();
		ikTool = IKTool.instance;
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		if ((Object)(object)gripMenuHandler != (Object)null)
		{
			((Behaviour)gripMenuHandler).enabled = false;
		}
		if ((Object)(object)menuHandlder != (Object)null)
		{
			((Behaviour)menuHandlder).enabled = true;
		}
		if (Object.op_Implicit((Object)(object)internalGui))
		{
			((Component)internalGui).gameObject.SetActive(false);
		}
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		if ((Object)(object)gripMenuHandler != (Object)null)
		{
			((Behaviour)gripMenuHandler).enabled = true;
		}
		if ((Object)(object)menuHandlder != (Object)null)
		{
			((Behaviour)menuHandlder).enabled = false;
		}
		if (Object.op_Implicit((Object)(object)internalGui))
		{
			((Component)internalGui).gameObject.SetActive(true);
		}
	}

	protected override void OnLevel(int level)
	{
		base.OnLevel(level);
		((MonoBehaviour)this).StopAllCoroutines();
	}

	protected override void OnUpdate()
	{
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Expected O, but got Unknown
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0407: Unknown result type (might be due to invalid IL or missing references)
		//IL_0427: Unknown result type (might be due to invalid IL or missing references)
		//IL_0678: Unknown result type (might be due to invalid IL or missing references)
		//IL_0693: Unknown result type (might be due to invalid IL or missing references)
		//IL_054a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0555: Unknown result type (might be due to invalid IL or missing references)
		//IL_055a: Unknown result type (might be due to invalid IL or missing references)
		//IL_055f: Unknown result type (might be due to invalid IL or missing references)
		//IL_056c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0577: Unknown result type (might be due to invalid IL or missing references)
		//IL_057c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0581: Unknown result type (might be due to invalid IL or missing references)
		//IL_0586: Unknown result type (might be due to invalid IL or missing references)
		//IL_0589: Unknown result type (might be due to invalid IL or missing references)
		//IL_058b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0590: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0600: Unknown result type (might be due to invalid IL or missing references)
		//IL_060d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0612: Unknown result type (might be due to invalid IL or missing references)
		//IL_0632: Unknown result type (might be due to invalid IL or missing references)
		//IL_0637: Unknown result type (might be due to invalid IL or missing references)
		//IL_0639: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0509: Expected O, but got Unknown
		//IL_051a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0535: Unknown result type (might be due to invalid IL or missing references)
		base.OnUpdate();
		if (controller == null)
		{
			return;
		}
		if (controller.GetPressDown(EVRButtonId.k_EButton_Axis1))
		{
			triggerDownTime = Time.time;
		}
		if (controller.GetPressDown(EVRButtonId.k_EButton_Grip))
		{
			gripDownTime = Time.time;
		}
		if (controller.GetPressDown(EVRButtonId.k_EButton_ApplicationMenu))
		{
			menuDownTime = Time.time;
		}
		if (controller.GetPressDown(EVRButtonId.k_EButton_Axis0) || controller.GetPressDown(EVRButtonId.k_EButton_A))
		{
			touchpadDownTime = Time.time;
		}
		if (controller.GetPress(EVRButtonId.k_EButton_Axis1) && controller.GetPress(EVRButtonId.k_EButton_Grip) && controller.GetPress(EVRButtonId.k_EButton_ApplicationMenu) && Time.time - menuDownTime > 0.5f)
		{
			lockRotXZ = !lockRotXZ;
			if (lockRotXZ)
			{
				ResetRotation();
			}
		}
		if (controller.GetPress(EVRButtonId.k_EButton_ApplicationMenu) && Time.time - menuDownTime > 1.5f)
		{
			resetGUIPosition();
			menuDownTime = Time.time;
		}
		if (controller.GetPressDown(EVRButtonId.k_EButton_Axis0) || controller.GetPressDown(EVRButtonId.k_EButton_A))
		{
			controller.GetPress(EVRButtonId.k_EButton_Grip);
		}
		bool pressDown = controller.GetPressDown(grabScreenButton);
		bool press = controller.GetPress(grabScreenButton);
		bool pressUp = controller.GetPressUp(grabScreenButton);
		if ((Object)(object)grabHandle == (Object)null)
		{
			grabHandle = new GameObject("__GripMoveGrabHandle__");
			grabHandle.transform.parent = ((Component)this).transform;
			grabHandle.transform.position = ((Component)this).transform.position;
			grabHandle.transform.rotation = ((Component)this).transform.rotation;
		}
		if (pressDown && screenGrabbed && (Object)(object)lastGrabbedObject != (Object)null && (Object)(object)grabHandle != (Object)null)
		{
			grabbingObject = lastGrabbedObject;
			grabHandle.transform.position = lastGrabbedObject.transform.position;
			grabHandle.transform.rotation = lastGrabbedObject.transform.rotation;
			if ((Object)(object)lastGrabbedObject.GetComponent<MoveableGUIObject>() != (Object)null)
			{
				_ = lastGrabbedObject.transform.parent;
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
		bool flag = false;
		if ((controller.GetPressDown(EVRButtonId.k_EButton_Axis0) || controller.GetPressDown(EVRButtonId.k_EButton_A)) && (Object)(object)lastGrabbedObject != (Object)null && (Object)(object)lastGrabbedObject.GetComponent<MoveableGUIObject>() != (Object)null)
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
		if (controller.GetPressDown(EVRButtonId.k_EButton_Axis0) || (controller.GetPressDown(EVRButtonId.k_EButton_A) && !flag))
		{
			VRLog.Info("Called on Select VRToggle");
			if (Object.op_Implicit((Object)(object)gripMenuHandler) && gripMenuHandler.LaserVisible)
			{
				VRItemObjMoveHelper.Instance.VRToggleObjectSelectOnCursor();
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
		if (controller.GetPress(moveSelfButton) && (Object)(object)grabbingObject == (Object)null)
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
		lastGrabbedObject = null;
		nearestGrabable = float.MaxValue;
		marker.transform.position = ((Component)this).transform.position;
		marker.transform.rotation = ((Component)this).transform.rotation;
	}

	private void ApplyFingerFKIfNeeded(GuideObject guideObject)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		new List<Transform>();
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
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		return new List<HelpText>(new HelpText[3]
		{
			HelpText.Create("Swipe as wheel.", FindAttachPosition("touchpad"), new Vector3(0.06f, 0.04f, 0f)),
			HelpText.Create("Grip and move controller to move yourself", FindAttachPosition("rgrip"), new Vector3(0.06f, 0.04f, 0f)),
			HelpText.Create("Trigger to grab objects / IK markers and move them along with controller.", FindAttachPosition("trigger"), new Vector3(-0.06f, -0.04f, 0f))
		});
	}

	private void ResetRotation()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		if (lockRotXZ)
		{
			return RemoveXZRot(q);
		}
		return q;
	}

	public static Quaternion RemoveXZRot(Quaternion q)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		Vector3 eulerAngles = ((Quaternion)(ref q)).eulerAngles;
		eulerAngles.x = 0f;
		eulerAngles.z = 0f;
		return Quaternion.Euler(eulerAngles);
	}

	private void OnTriggerStay(Collider collider)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		GameObject gameObject = ((Component)collider).gameObject;
		if (screenGrabbed && (Object)(object)((Component)collider).GetComponent<MoveableGUIObject>() != (Object)null && (Object)(object)gameObject == (Object)(object)lastGrabbedObject)
		{
			((Renderer)pointer.GetComponent<MeshRenderer>()).material.color = Color.white;
			screenGrabbed = false;
			lastGrabbedObject = null;
		}
	}
}

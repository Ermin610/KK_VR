using System;
using System.Collections.Generic;
using System.Linq;
using Leap;
using Leap.Unity;
using UnityEngine;
using Object = UnityEngine.Object;
using VRGIN.Controls;
using VRGIN.Controls.LeapMotion;
using VRGIN.Controls.Speech;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.U46.Controls.Leap;
using VRGIN.Visuals;
using Valve.VR;

namespace VRGIN.Modes;

public abstract class ControlMode : ProtectedBehaviour
{
	private static bool _ControllerFound;

	protected SteamVR_ControllerManager ControllerManager;

	private static int cnter;

	private VRCapturePanorama _CapturePanorama;

	public abstract ETrackingUniverseOrigin TrackingOrigin { get; }

	public VRGIN.Controls.Controller Left { get; private set; }

	public VRGIN.Controls.Controller Right { get; private set; }

	public HandAttachments LeftHand { get; private set; }

	public HandModel LeftGraphicalHand { get; private set; }

	public HandAttachments RightHand { get; private set; }

	public HandModel RightGraphicalHand { get; private set; }

	public LeapServiceProvider LeapMotion { get; private set; }

	protected IEnumerable<IShortcut> Shortcuts { get; private set; }

	public virtual IEnumerable<Type> Tools => new List<Type>();

	public virtual IEnumerable<Type> LeftTools => new List<Type>();

	public virtual IEnumerable<Type> RightTools => new List<Type>();

	internal event EventHandler<EventArgs> ControllersCreated = delegate
	{
	};

	public virtual void Impersonate(IActor actor)
	{
		Impersonate(actor, ImpersonationMode.Approximately);
	}

	public virtual void Impersonate(IActor actor, ImpersonationMode mode)
	{
		if (actor != null)
		{
			actor.HasHead = false;
		}
	}

	public virtual void MoveToPosition(Vector3 targetPosition, bool ignoreHeight = true)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		MoveToPosition(targetPosition, VR.Camera.SteamCam.head.rotation, ignoreHeight);
	}

	public virtual void MoveToPosition(Vector3 targetPosition, Quaternion rotation = default(Quaternion), bool ignoreHeight = true)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		Vector3 forwardVector = Calculator.GetForwardVector(rotation);
		Vector3 forwardVector2 = Calculator.GetForwardVector(VR.Camera.SteamCam.head.rotation);
		Transform origin = VR.Camera.SteamCam.origin;
		origin.rotation *= Quaternion.FromToRotation(forwardVector2, forwardVector);
		float num = (ignoreHeight ? 0f : targetPosition.y);
		float num2 = (ignoreHeight ? 0f : VR.Camera.SteamCam.head.position.y);
		targetPosition = new Vector3(targetPosition.x, num, targetPosition.z);
		Vector3 val = default(Vector3);
		val = new Vector3(VR.Camera.SteamCam.head.position.x, num2, VR.Camera.SteamCam.head.position.z);
		Transform origin2 = VR.Camera.SteamCam.origin;
		origin2.position += targetPosition - val;
	}

	protected override void OnStart()
	{
		CreateControllers();
		Shortcuts = CreateShortcuts();
		SteamVR_Render.instance.trackingSpace = TrackingOrigin;
		InitializeScreenCapture();
	}

	protected virtual void OnEnable()
	{
		SteamVR_Events.DeviceConnected.Listen(OnDeviceConnected);
		VRLog.Info("Enabled {0}", ((object)this).GetType().Name);
	}

	protected virtual void OnDisable()
	{
		VRLog.Info("Disabled {0}", ((object)this).GetType().Name);
		SteamVR_Events.DeviceConnected.Remove(OnDeviceConnected);
	}

	protected virtual void CreateControllers()
	{
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		SteamVR_Camera steamCam = VR.Camera.SteamCam;
		((Component)steamCam.origin).gameObject.SetActive(false);
		ControllerManager = ((Component)steamCam.origin).gameObject.AddComponent<SteamVR_ControllerManager>();
		if (VR.Settings.Leap)
		{
			LeapMotion = CreateLeapHandController();
			((Object)((Component)LeapMotion).transform).name = "Leap Motion Controller (" + ++cnter + ")";
			((Component)LeapMotion).transform.SetParent(((Component)steamCam.head).transform, false);
			((Component)LeapMotion).transform.localRotation = Quaternion.Euler(-90f, 180f, 0f);
			Transform transform = ((Component)LeapMotion).transform;
			transform.localPosition += Vector3.forward * 0.08f;
		}
		Left = CreateLeftController();
		((Component)Left).transform.SetParent(steamCam.origin, false);
		Right = CreateRightController();
		((Component)Right).transform.SetParent(steamCam.origin, false);
		Left.Other = Right;
		Right.Other = Left;
		ControllerManager.left = ((Component)Left).gameObject;
		ControllerManager.right = ((Component)Right).gameObject;
		((Component)steamCam.origin).gameObject.SetActive(true);
		VRLog.Info("---- Initialize left tools");
		InitializeTools(Left, isLeft: true);
		VRLog.Info("---- Initialize right tools");
		InitializeTools(Right, isLeft: false);
		this.ControllersCreated(this, new EventArgs());
	}

	private LeapServiceProvider CreateLeapHandController()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		LeapServiceProvider leapServiceProvider = new GameObject("LeapHandController").AddComponent<LeapServiceProvider>();
		LeapHandController leapHandController = ((Component)leapServiceProvider).gameObject.AddComponent<LeapHandController>();
		HandPool handPool = ((Component)leapHandController).gameObject.AddComponent<HandPool>();
		((Component)leapHandController).gameObject.AddComponent<PinchController>();
		leapServiceProvider._isHeadMounted = true;
		LeftGraphicalHand = BuildGraphicalHand(Chirality.Left);
		RightGraphicalHand = BuildGraphicalHand(Chirality.Right);
		LeftHand = BuildAttachmentHand(Chirality.Left);
		RightHand = BuildAttachmentHand(Chirality.Right);
		handPool.ModelPool = new List<HandPool.ModelGroup>();
		handPool.ModelPool.Add(new HandPool.ModelGroup
		{
			GroupName = "Graphics_Hands",
			CanDuplicate = false,
			IsEnabled = true,
			LeftModel = LeftGraphicalHand,
			RightModel = RightGraphicalHand,
			modelList = new List<IHandModel>(),
			modelsCheckedOut = new List<IHandModel>()
		});
		handPool.ModelPool.Add(new HandPool.ModelGroup
		{
			GroupName = "Attachments",
			CanDuplicate = false,
			IsEnabled = true,
			LeftModel = LeftHand,
			RightModel = RightHand,
			modelList = new List<IHandModel>(),
			modelsCheckedOut = new List<IHandModel>()
		});
		((Component)LeftHand).transform.SetParent(((Component)handPool).transform, false);
		((Component)RightHand).transform.SetParent(((Component)handPool).transform, false);
		((Component)LeftGraphicalHand).transform.SetParent(((Component)handPool).transform, false);
		((Component)RightGraphicalHand).transform.SetParent(((Component)handPool).transform, false);
		return leapServiceProvider;
	}

	protected virtual HandModel BuildGraphicalHand(Chirality handedness)
	{
		GameObject handObj = UnityHelper.LoadFromAssetBundle<GameObject>(ResourceManager.Hands, "LoPoly_Rigged_Hand_" + handedness);
		RiggedHand riggedHand = SetUpRiggedHand(handObj, handedness);
		((Component)riggedHand).gameObject.AddComponent<HandEnableDisable>();
		return riggedHand;
	}

	private RiggedHand SetUpRiggedHand(GameObject handObj, Chirality handedness)
	{
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_043a: Unknown result type (might be due to invalid IL or missing references)
		//IL_043c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0442: Unknown result type (might be due to invalid IL or missing references)
		//IL_0444: Unknown result type (might be due to invalid IL or missing references)
		//IL_049d: Unknown result type (might be due to invalid IL or missing references)
		RiggedHand component = handObj.GetComponent<RiggedHand>();
		if ((component != null))
		{
			((Component)component).gameObject.AddComponent<LeapMenuHandler>();
			return component;
		}
		component = handObj.AddComponent<RiggedHand>();
		((Component)component).gameObject.AddComponent<LeapMenuHandler>();
		handObj.AddComponent<HandEnableDisable>();
		component.handedness = handedness;
		RiggedFinger riggedFinger = ((Component)component).gameObject.Descendants().First((GameObject d) => ((Object)d).name.EndsWith("thumb_meta", StringComparison.InvariantCultureIgnoreCase)).AddComponent<RiggedFinger>();
		RiggedFinger riggedFinger2 = ((Component)component).gameObject.Descendants().First((GameObject d) => ((Object)d).name.EndsWith("index_meta", StringComparison.InvariantCultureIgnoreCase)).AddComponent<RiggedFinger>();
		RiggedFinger riggedFinger3 = ((Component)component).gameObject.Descendants().First((GameObject d) => ((Object)d).name.EndsWith("ring_meta")).AddComponent<RiggedFinger>();
		RiggedFinger riggedFinger4 = ((Component)component).gameObject.Descendants().First((GameObject d) => ((Object)d).name.EndsWith("middle_meta")).AddComponent<RiggedFinger>();
		RiggedFinger riggedFinger5 = ((Component)component).gameObject.Descendants().First((GameObject d) => ((Object)d).name.EndsWith("pinky_meta")).AddComponent<RiggedFinger>();
		Transform transform = ((Component)component).gameObject.Descendants().First((GameObject d) => ((Object)d).name.EndsWith("Wrist")).transform;
		Transform transform2 = ((Component)component).gameObject.Descendants().First((GameObject d) => ((Object)d).name.EndsWith("Palm")).transform;
		component.fingers = new RiggedFinger[5] { riggedFinger, riggedFinger2, riggedFinger4, riggedFinger3, riggedFinger5 };
		component.wristJoint = transform;
		component.palm = transform2;
		component.ModelPalmAtLeapWrist = true;
		component.handModelPalmWidth = 0.085f;
		component.UseMetaCarpals = true;
		Vector3 modelFingerPointing = Vector3.left * (float)((component.handedness == Chirality.Left) ? 1 : (-1));
		Vector3 modelPalmFacing = Vector3.up * (float)((component.handedness == Chirality.Left) ? 1 : (-1));
		component.modelFingerPointing = modelFingerPointing;
		component.modelPalmFacing = modelPalmFacing;
		riggedFinger.fingerType = Finger.FingerType.TYPE_THUMB;
		riggedFinger2.fingerType = Finger.FingerType.TYPE_INDEX;
		riggedFinger3.fingerType = Finger.FingerType.TYPE_RING;
		riggedFinger4.fingerType = Finger.FingerType.TYPE_MIDDLE;
		riggedFinger5.fingerType = Finger.FingerType.TYPE_PINKY;
		riggedFinger.bones = ((IEnumerable<Transform>)(object)new Transform[2]
		{
			default(Transform),
			((Component)riggedFinger).transform
		}).Concat((from d in ((Component)riggedFinger).gameObject.Descendants()
			select d.transform).Take(2)).ToArray();
		riggedFinger2.bones = ((IEnumerable<Transform>)(object)new Transform[1] { ((Component)riggedFinger2).transform }).Concat((from d in ((Component)riggedFinger2).gameObject.Descendants()
			select d.transform).Take(3)).ToArray();
		riggedFinger3.bones = ((IEnumerable<Transform>)(object)new Transform[1] { ((Component)riggedFinger3).transform }).Concat((from d in ((Component)riggedFinger3).gameObject.Descendants()
			select d.transform).Take(3)).ToArray();
		riggedFinger4.bones = ((IEnumerable<Transform>)(object)new Transform[1] { ((Component)riggedFinger4).transform }).Concat((from d in ((Component)riggedFinger4).gameObject.Descendants()
			select d.transform).Take(3)).ToArray();
		riggedFinger5.bones = ((IEnumerable<Transform>)(object)new Transform[1] { ((Component)riggedFinger5).transform }).Concat((from d in ((Component)riggedFinger5).gameObject.Descendants()
			select d.transform).Take(3)).ToArray();
		RiggedFinger[] array = new RiggedFinger[5] { riggedFinger, riggedFinger2, riggedFinger3, riggedFinger4, riggedFinger5 };
		foreach (RiggedFinger obj in array)
		{
			obj.modelFingerPointing = modelFingerPointing;
			obj.modelPalmFacing = modelPalmFacing;
			obj.joints = (Transform[])(object)new Transform[3];
		}
		foreach (GameObject item in handObj.Descendants())
		{
			VRLog.Info("{0}: {1}", ((Object)item.transform).name, item.transform.localScale);
		}
		return component;
	}

	protected virtual HandAttachments BuildAttachmentHand(Chirality handedness)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		HandAttachments handAttachments = new GameObject("AHand_" + handedness).AddComponent<HandAttachments>();
		handAttachments._handedness = handedness;
		((Component)handAttachments).gameObject.AddComponent<HandEnableDisable>();
		((Component)handAttachments).gameObject.AddComponent<WarpHandler>();
		handAttachments.GrabPoint = UnityHelper.CreateGameObjectAsChild("GrabPoint", ((Component)handAttachments).transform, dontDestroy: true);
		handAttachments.Arm = UnityHelper.CreateGameObjectAsChild("Arm", ((Component)handAttachments).transform, dontDestroy: true);
		handAttachments.Thumb = UnityHelper.CreateGameObjectAsChild("Thumb", ((Component)handAttachments).transform, dontDestroy: true);
		handAttachments.Index = UnityHelper.CreateGameObjectAsChild("Index", ((Component)handAttachments).transform, dontDestroy: true);
		handAttachments.Middle = UnityHelper.CreateGameObjectAsChild("Middle", ((Component)handAttachments).transform, dontDestroy: true);
		handAttachments.Ring = UnityHelper.CreateGameObjectAsChild("Ring", ((Component)handAttachments).transform, dontDestroy: true);
		handAttachments.Pinky = UnityHelper.CreateGameObjectAsChild("Pinky", ((Component)handAttachments).transform, dontDestroy: true);
		handAttachments.PinchPoint = UnityHelper.CreateGameObjectAsChild("PinchPoint", ((Component)handAttachments).transform, dontDestroy: true);
		handAttachments.Palm = UnityHelper.CreateGameObjectAsChild("Palm", ((Component)handAttachments).transform, dontDestroy: true);
		handAttachments.OnBegin += delegate
		{
			if (!_ControllerFound)
			{
				_ControllerFound = true;
				ChangeModeOnControllersDetected();
			}
		};
		return handAttachments;
	}

	public virtual void OnDestroy()
	{
		Object.Destroy((Object)(object)ControllerManager);
		Object.Destroy((Object)(object)Left);
		Object.Destroy((Object)(object)Right);
		if ((_CapturePanorama != null))
		{
			Object.Destroy((Object)(object)_CapturePanorama);
		}
		if ((LeapMotion != null))
		{
			Object.DestroyImmediate((Object)(object)((Component)LeapMotion).gameObject);
		}
		if (Shortcuts == null)
		{
			return;
		}
		foreach (IShortcut shortcut in Shortcuts)
		{
			shortcut.Dispose();
		}
	}

	protected virtual void InitializeTools(VRGIN.Controls.Controller controller, bool isLeft)
	{
		IEnumerable<Type> enumerable = Tools.Concat(isLeft ? LeftTools : RightTools).Distinct();
		foreach (Type item in enumerable)
		{
			controller.AddTool(item);
		}
		VRLog.Info("{0} tools added", enumerable.Count());
	}

	protected virtual VRGIN.Controls.Controller CreateLeftController()
	{
		return LeftController.Create();
	}

	protected virtual VRGIN.Controls.Controller CreateRightController()
	{
		return RightController.Create();
	}

	protected virtual IEnumerable<IShortcut> CreateShortcuts()
	{
		return new List<IShortcut>
		{
			new KeyboardShortcut(VR.Shortcuts.ShrinkWorld, delegate
			{
				VR.Settings.IPDScale += Time.deltaTime;
			}),
			new KeyboardShortcut(VR.Shortcuts.EnlargeWorld, delegate
			{
				VR.Settings.IPDScale -= Time.deltaTime;
			}),
			new VoiceShortcut(VoiceCommand.DecreaseScale, delegate
			{
				VR.Settings.IPDScale *= 1.2f;
			}),
			new VoiceShortcut(VoiceCommand.IncreaseScale, delegate
			{
				VR.Settings.IPDScale *= 0.8f;
			}),
			new MultiKeyboardShortcut(new KeyStroke("Ctrl + C"), new KeyStroke("Ctrl + D"), delegate
			{
				UnityHelper.DumpScene("dump.json");
			}),
			new MultiKeyboardShortcut(new KeyStroke("Ctrl + C"), new KeyStroke("Ctrl + I"), delegate
			{
				UnityHelper.DumpScene("dump.json", onlyActive: true);
			}),
			new MultiKeyboardShortcut(VR.Shortcuts.ToggleUserCamera, ToggleUserCamera),
			new MultiKeyboardShortcut(VR.Shortcuts.SaveSettings, delegate
			{
				VR.Settings.Save();
			}),
			new VoiceShortcut(VoiceCommand.SaveSettings, delegate
			{
				VR.Settings.Save();
			}),
			new KeyboardShortcut(VR.Shortcuts.LoadSettings, delegate
			{
				VR.Settings.Reload();
			}),
			new VoiceShortcut(VoiceCommand.LoadSettings, delegate
			{
				VR.Settings.Reload();
			}),
			new KeyboardShortcut(VR.Shortcuts.ResetSettings, delegate
			{
				VR.Settings.Reset();
			}),
			new VoiceShortcut(VoiceCommand.ResetSettings, delegate
			{
				VR.Settings.Reset();
			}),
			new VoiceShortcut(VoiceCommand.Impersonate, delegate
			{
				Impersonate(VR.Interpreter.Actors.FirstOrDefault());
			}),
			new KeyboardShortcut(VR.Shortcuts.ApplyEffects, delegate
			{
				VR.Manager.ToggleEffects();
			})
		};
	}

	protected virtual void ToggleUserCamera()
	{
		if (!PlayerCamera.Created)
		{
			VRLog.Info("Create user camera");
			PlayerCamera.Create();
		}
		else
		{
			VRLog.Info("Remove user camera");
			PlayerCamera.Remove();
		}
	}

	protected virtual void InitializeScreenCapture()
	{
		_CapturePanorama = ((Component)VR.Camera.SteamCam).gameObject.AddComponent<VRCapturePanorama>();
	}

	protected override void OnUpdate()
	{
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		base.OnUpdate();
		SteamVR_Render.instance.trackingSpace = TrackingOrigin;
		SteamVR_Camera steamCam = VRCamera.Instance.SteamCam;
		int num = 0;
		bool isEveryoneHeaded = VR.Interpreter.IsEveryoneHeaded;
		foreach (IActor actor in VR.Interpreter.Actors)
		{
			if (actor.HasHead)
			{
				if (isEveryoneHeaded)
				{
					Vector3 position = actor.Eyes.position;
					Vector3 forward = actor.Eyes.forward;
					Vector3 position2 = steamCam.head.position;
					Vector3 forward2 = steamCam.head.forward;
					VRLog.Debug("Actor #{0} -- He: {1} -> {2} | Me: {3} -> {4}", num, position, forward, position2, forward2);
					if (Vector3.Distance(position, position2) * VR.Context.UnitToMeter < 0.15f && Vector3.Dot(forward, forward2) > 0.6f)
					{
						actor.HasHead = false;
					}
				}
			}
			else if (Vector3.Distance(actor.Eyes.position, steamCam.head.position) * VR.Context.UnitToMeter > 0.3f)
			{
				actor.HasHead = true;
			}
			num++;
		}
		CheckInput();
	}

	protected void CheckInput()
	{
		foreach (IShortcut shortcut in Shortcuts)
		{
			shortcut.Evaluate();
		}
	}

	private void OnDeviceConnected(int idx, bool connected)
	{
		if (_ControllerFound)
		{
			return;
		}
		VRLog.Info("Device connected: {0}", (uint)idx);
		if (connected && idx != 0)
		{
			CVRSystem system = OpenVR.System;
			if (system != null && system.GetTrackedDeviceClass((uint)idx) == ETrackedDeviceClass.Controller)
			{
				_ControllerFound = true;
				ChangeModeOnControllersDetected();
			}
		}
	}

	protected virtual void ChangeModeOnControllersDetected()
	{
	}
}

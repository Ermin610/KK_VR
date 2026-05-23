using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VRGIN.Controls.Handlers;
using VRGIN.Controls.Tools;
using VRGIN.Core;
using VRGIN.Helpers;
using Valve.VR;

namespace VRGIN.Controls;

public abstract class Controller : ProtectedBehaviour
{
	public class Lock
	{
		private Controller _Controller;

		public static readonly Lock Invalid = new Lock();

		public bool IsInvalidating { get; private set; }

		public bool IsValid { get; private set; }

		private Lock()
		{
			IsValid = false;
		}

		internal Lock(Controller controller)
		{
			IsValid = true;
			_Controller = controller;
			_Controller._Lock = this;
			_Controller.OnLock();
		}

		public void Release()
		{
			if (IsValid)
			{
				_Controller._Lock = null;
				_Controller.OnUnlock();
				IsValid = false;
			}
			else
			{
				VRLog.Warn("Tried to release an invalid lock!");
			}
		}

		public void SafeRelease()
		{
			if (IsValid)
			{
				IsInvalidating = true;
			}
			else
			{
				VRLog.Warn("Tried to release an invalid lock!");
			}
		}
	}

	private bool _Started;

	public SteamVR_TrackedObject Tracking;

	protected BoxCollider Collider;

	private float? appButtonPressTime;

	public List<Tool> Tools = new List<Tool>();

	public Controller Other;

	private const float APP_BUTTON_TIME_THRESHOLD = 0.5f;

	private bool helpShown;

	private List<HelpText> helpTexts;

	private Canvas _Canvas;

	private Lock _Lock = Lock.Invalid;

	private GameObject _AlphaConcealer;

	public SteamVR_RenderModel Model { get; private set; }

	public RumbleManager Rumble { get; private set; }

	public virtual int ToolIndex { get; set; }

	public Tool ActiveTool
	{
		get
		{
			if (ToolIndex >= Tools.Count)
			{
				return null;
			}
			return Tools[ToolIndex];
		}
	}

	public virtual IList<Type> ToolTypes => new List<Type>();

	public bool ToolEnabled
	{
		get
		{
			if ((Object)(object)ActiveTool != (Object)null)
			{
				return ((Behaviour)ActiveTool).enabled;
			}
			return false;
		}
		set
		{
			if ((Object)(object)ActiveTool != (Object)null)
			{
				((Behaviour)ActiveTool).enabled = value;
				if (!value)
				{
					HideHelp();
				}
			}
		}
	}

	public bool IsTracking
	{
		get
		{
			if (Object.op_Implicit((Object)(object)Tracking))
			{
				return Tracking.isValid;
			}
			return false;
		}
	}

	public SteamVR_Controller.Device Input => SteamVR_Controller.Input((int)Tracking.index);

	[Obsolete("Use TryAcquireFocus() or AcquireFocus()")]
	public bool AcquireFocus(out Lock lockObj)
	{
		return TryAcquireFocus(out lockObj);
	}

	public bool TryAcquireFocus(out Lock lockObj)
	{
		lockObj = null;
		if (CanAcquireFocus())
		{
			lockObj = new Lock(this);
			return true;
		}
		return false;
	}

	public Lock AcquireFocus()
	{
		if (TryAcquireFocus(out var lockObj))
		{
			return lockObj;
		}
		return Lock.Invalid;
	}

	public bool CanAcquireFocus()
	{
		if (_Lock != null)
		{
			return !_Lock.IsValid;
		}
		return true;
	}

	protected virtual void OnLock()
	{
		ToolEnabled = false;
		_AlphaConcealer.SetActive(false);
	}

	protected virtual void OnUnlock()
	{
		ToolEnabled = true;
		_AlphaConcealer.SetActive(true);
	}

	protected virtual void OnDestroy()
	{
		Object.Destroy((Object)(object)((Component)this).gameObject);
		SteamVR_Events.RenderModelLoaded.Remove(_OnRenderModelLoaded);
	}

	protected void SetUp()
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		SteamVR_Events.RenderModelLoaded.Listen(_OnRenderModelLoaded);
		Tracking = ((Component)this).gameObject.AddComponent<SteamVR_TrackedObject>();
		Rumble = ((Component)this).gameObject.AddComponent<RumbleManager>();
		((Component)this).gameObject.AddComponent<BodyRumbleHandler>();
		((Component)this).gameObject.AddComponent<MenuHandler>();
		Model = new GameObject("Model").AddComponent<SteamVR_RenderModel>();
		Model.shader = VRManager.Instance.Context.Materials.StandardShader;
		if (!Object.op_Implicit((Object)(object)Model.shader))
		{
			VRLog.Warn("Shader not found");
		}
		((Component)Model).transform.SetParent(((Component)this).transform, false);
		BuildCanvas();
		Collider = new GameObject("Collider").AddComponent<BoxCollider>();
		((Component)Collider).transform.SetParent(((Component)this).transform, false);
		Collider.center = new Vector3(0f, -0.02f, -0.06f);
		Collider.size = new Vector3(-0.05f, 0.05f, 0.2f);
		((Collider)Collider).isTrigger = true;
		((Component)this).gameObject.AddComponent<Rigidbody>().isKinematic = true;
	}

	private void _OnRenderModelLoaded(SteamVR_RenderModel model, bool isLoaded)
	{
		try
		{
			if (Object.op_Implicit((Object)(object)model) && ((Component)model).transform.IsChildOf(((Component)this).transform))
			{
				VRLog.Info("Render model loaded!");
				((Component)this).gameObject.SendMessageToAll("OnRenderModelLoaded");
				OnRenderModelLoaded();
			}
		}
		catch (Exception obj)
		{
			VRLog.Error(obj);
		}
	}

	private void OnRenderModelLoaded()
	{
	}

	protected override void OnAwake()
	{
		base.OnAwake();
		SetUp();
	}

	public void AddTool(Type toolType)
	{
		if (toolType.IsSubclassOf(typeof(Tool)) && !Tools.Any((Tool tool) => toolType.IsAssignableFrom(((object)tool).GetType())))
		{
			Tool tool2 = ((Component)this).gameObject.AddComponent(toolType) as Tool;
			Tools.Add(tool2);
			CreateToolCanvas(tool2);
			((Behaviour)tool2).enabled = false;
		}
	}

	public void AddTool<T>() where T : Tool
	{
		AddTool(typeof(T));
	}

	protected override void OnStart()
	{
		int num = 0;
		foreach (Tool tool in Tools)
		{
			if (num++ != ToolIndex && Object.op_Implicit((Object)(object)tool))
			{
				((Behaviour)tool).enabled = false;
				VRLog.Info("Disable tool #{0} ({1})", num - 1, ToolIndex);
				continue;
			}
			VRLog.Info("Enable Tool #{0}", num - 1);
			if (((Behaviour)tool).enabled)
			{
				((Behaviour)tool).enabled = false;
			}
			((Behaviour)tool).enabled = true;
		}
		_Started = true;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		SteamVR_Controller.Device device = SteamVR_Controller.Input((int)Tracking.index);
		if (_Lock != null && _Lock.IsInvalidating)
		{
			TryReleaseLock();
		}
		if (_Lock != null && _Lock.IsValid)
		{
			return;
		}
		if (device.GetPressDown(EVRButtonId.k_EButton_ApplicationMenu))
		{
			appButtonPressTime = Time.unscaledTime;
		}
		if (device.GetPress(EVRButtonId.k_EButton_ApplicationMenu) && Time.unscaledTime - appButtonPressTime > 0.5f)
		{
			ShowHelp();
			appButtonPressTime = null;
		}
		if (!device.GetPressUp(EVRButtonId.k_EButton_ApplicationMenu))
		{
			return;
		}
		if (helpShown)
		{
			HideHelp();
		}
		else
		{
			if (Object.op_Implicit((Object)(object)ActiveTool))
			{
				((Behaviour)ActiveTool).enabled = false;
			}
			ToolIndex = (ToolIndex + 1) % Tools.Count;
			if (Object.op_Implicit((Object)(object)ActiveTool))
			{
				((Behaviour)ActiveTool).enabled = true;
			}
		}
		appButtonPressTime = null;
	}

	private void TryReleaseLock()
	{
		SteamVR_Controller.Device input = Input;
		foreach (EVRButtonId item in Enum.GetValues(typeof(EVRButtonId)).OfType<EVRButtonId>())
		{
			if (input.GetPress(item))
			{
				return;
			}
		}
		_Lock.Release();
	}

	public void StartRumble(IRumbleSession session)
	{
		Rumble.StartRumble(session);
	}

	public void StopRumble(IRumbleSession session)
	{
		Rumble.StopRumble(session);
	}

	private void HideHelp()
	{
		if (helpShown)
		{
			helpTexts.ForEach(delegate(HelpText h)
			{
				Object.Destroy((Object)(object)((Component)h).gameObject);
			});
			helpShown = false;
		}
	}

	private void ShowHelp()
	{
		if ((Object)(object)ActiveTool != (Object)null)
		{
			helpTexts = ActiveTool.GetHelpTexts();
			helpShown = true;
		}
	}

	private void BuildCanvas()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		Canvas obj = (_Canvas = new GameObject().AddComponent<Canvas>());
		obj.renderMode = (RenderMode)2;
		((Component)obj).transform.SetParent(((Component)this).transform, false);
		((Component)obj).GetComponent<RectTransform>().SetSizeWithCurrentAnchors((Axis)0, 950f);
		((Component)obj).GetComponent<RectTransform>().SetSizeWithCurrentAnchors((Axis)1, 950f);
		((Component)obj).transform.localPosition = new Vector3(0f, -0.02725995f, 0.0279f);
		((Component)obj).transform.localRotation = Quaternion.Euler(30f, 180f, 180f);
		((Component)obj).transform.localScale = new Vector3(4.930151E-05f, 4.930148E-05f, 0f);
		((Component)obj).gameObject.layer = 0;
		_AlphaConcealer = GameObject.CreatePrimitive((PrimitiveType)0);
		_AlphaConcealer.transform.SetParent(((Component)this).transform, false);
		_AlphaConcealer.transform.localScale = new Vector3(0.05f, 0f, 0.05f);
		_AlphaConcealer.transform.localPosition = new Vector3(0f, -0.0303f, 0.0142f);
		_AlphaConcealer.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
		_AlphaConcealer.GetComponent<Collider>().enabled = false;
	}

	private void CreateToolCanvas(Tool tool)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		Image val = new GameObject().AddComponent<Image>();
		((Component)val).transform.SetParent(((Component)_Canvas).transform, false);
		Texture2D image = tool.Image;
		val.sprite = Sprite.Create(image, new Rect(0f, 0f, (float)((Texture)image).width, (float)((Texture)image).height), new Vector2(0.5f, 0.5f));
		((Component)val).GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0f);
		((Component)val).GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1f);
		((Graphic)val).color = Color.cyan;
		tool.Icon = ((Component)val).gameObject;
		tool.Icon.SetActive(false);
		tool.Icon.layer = 0;
	}

	public Transform FindAttachPosition(params string[] names)
	{
		Transform val = (from t in ((Component)((Component)this).transform).GetComponentsInChildren<Transform>()
			where names.Contains(((Object)t).name)
			select t).FirstOrDefault();
		if ((Object)(object)val == (Object)null)
		{
			return null;
		}
		return val.Find("attach");
	}
}

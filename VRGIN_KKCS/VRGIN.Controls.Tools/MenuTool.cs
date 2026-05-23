using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.Native;
using VRGIN.Visuals;

namespace VRGIN.Controls.Tools;

public class MenuTool : Tool
{
	private float pressDownTime;

	private Vector2 touchDownPosition;

	private WindowsInterop.POINT touchDownMousePosition;

	private float timeAbandoned;

	private double _DeltaX;

	private double _DeltaY;

	public GUIQuad Gui { get; private set; }

	public override Texture2D Image => UnityHelper.LoadImage("icon_settings.png");

	public void TakeGUI(GUIQuad quad)
	{
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)quad) && !Object.op_Implicit((Object)(object)Gui) && !quad.IsOwned)
		{
			Gui = quad;
			((Component)Gui).transform.parent = ((Component)this).transform;
			((Component)Gui).transform.SetParent(((Component)this).transform, true);
			((Component)Gui).transform.localPosition = new Vector3(0f, 0.05f, -0.06f);
			((Component)Gui).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
			quad.IsOwned = true;
		}
	}

	public void AbandonGUI()
	{
		if (Object.op_Implicit((Object)(object)Gui))
		{
			timeAbandoned = Time.unscaledTime;
			Gui.IsOwned = false;
			((Component)Gui).transform.SetParent(VR.Camera.SteamCam.origin, true);
			Gui = null;
		}
	}

	protected override void OnAwake()
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		base.OnAwake();
		Gui = GUIQuad.Create();
		((Component)Gui).transform.parent = ((Component)this).transform;
		((Component)Gui).transform.localScale = Vector3.one * 0.3f;
		((Component)Gui).transform.localPosition = new Vector3(0f, 0.05f, -0.06f);
		((Component)Gui).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
		Gui.IsOwned = true;
		Object.DontDestroyOnLoad((Object)(object)((Component)Gui).gameObject);
	}

	protected override void OnStart()
	{
		base.OnStart();
	}

	protected override void OnDestroy()
	{
		Object.DestroyImmediate((Object)(object)((Component)Gui).gameObject);
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		if (Object.op_Implicit((Object)(object)Gui))
		{
			((Component)Gui).gameObject.SetActive(false);
		}
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		if (Object.op_Implicit((Object)(object)Gui))
		{
			((Component)Gui).gameObject.SetActive(true);
		}
	}

	protected override void OnUpdate()
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		base.OnUpdate();
		SteamVR_Controller.Device controller = base.Controller;
		if (controller.GetPressDown(12884901888uL))
		{
			VR.Input.Mouse.LeftButtonDown();
			pressDownTime = Time.unscaledTime;
		}
		if (controller.GetPressUp(4uL))
		{
			if (Object.op_Implicit((Object)(object)Gui))
			{
				AbandonGUI();
			}
			else
			{
				TakeGUI(GUIQuadRegistry.Quads.FirstOrDefault((GUIQuad q) => !q.IsOwned));
			}
		}
		if (controller.GetTouchDown(4294967296uL))
		{
			touchDownPosition = controller.GetAxis();
			touchDownMousePosition = MouseOperations.GetClientCursorPosition();
		}
		if (controller.GetTouch(4294967296uL) && Time.unscaledTime - pressDownTime > 0.3f)
		{
			Vector2 axis = controller.GetAxis();
			Vector2 val = axis - ((VR.HMD == HMDType.Oculus) ? Vector2.zero : touchDownPosition);
			float num = ((VR.HMD == HMDType.Oculus) ? (Time.unscaledDeltaTime * 5f) : 1f);
			_DeltaX += (double)(val.x * (float)VRGUI.Width) * 0.1 * (double)num;
			_DeltaY += (double)((0f - val.y) * (float)VRGUI.Height) * 0.2 * (double)num;
			int num2 = (int)((_DeltaX > 0.0) ? Math.Floor(_DeltaX) : Math.Ceiling(_DeltaX));
			int num3 = (int)((_DeltaY > 0.0) ? Math.Floor(_DeltaY) : Math.Ceiling(_DeltaY));
			_DeltaX -= num2;
			_DeltaY -= num3;
			VR.Input.Mouse.MoveMouseBy(num2, num3);
			touchDownPosition = axis;
		}
		if (controller.GetPressUp(12884901888uL))
		{
			VR.Input.Mouse.LeftButtonUp();
			pressDownTime = 0f;
		}
	}

	public override List<HelpText> GetHelpTexts()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		return new List<HelpText>(new HelpText[3]
		{
			HelpText.Create("Tap to click", FindAttachPosition("trackpad"), new Vector3(0f, 0.02f, 0.05f)),
			HelpText.Create("Slide to move cursor", FindAttachPosition("trackpad"), new Vector3(0.05f, 0.02f, 0f), (Vector3?)new Vector3(0.015f, 0f, 0f)),
			HelpText.Create("Attach/Remove menu", FindAttachPosition("lgrip"), new Vector3(-0.06f, 0f, -0.05f))
		});
	}
}

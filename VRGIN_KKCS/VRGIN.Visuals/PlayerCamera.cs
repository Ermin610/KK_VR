using UnityEngine;
using VRGIN.Controls;
using VRGIN.Core;
using Valve.VR;

namespace VRGIN.Visuals;

public class PlayerCamera : ProtectedBehaviour
{
	private SteamVR_RenderModel model;

	private Controller controller;

	private bool tracking;

	private static Vector3 S_Position;

	private static Quaternion S_Rotation;

	private Vector3 posOffset;

	private Quaternion rotOffset;

	public static bool Created { get; private set; }

	public static PlayerCamera Create()
	{
		Created = true;
		return GameObject.CreatePrimitive((PrimitiveType)3).AddComponent<PlayerCamera>();
	}

	internal static void Remove()
	{
		if (Created)
		{
			Object.Destroy((Object)(object)((Component)Object.FindObjectOfType<PlayerCamera>()).gameObject);
			Created = false;
		}
	}

	protected void OnEnable()
	{
		VRGUI.Instance.Listen();
	}

	protected void OnDisable()
	{
		VRGUI.Instance.Unlisten();
	}

	protected override void OnAwake()
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		GameObject obj = GameObject.CreatePrimitive((PrimitiveType)0);
		obj.transform.SetParent(((Component)this).transform, false);
		GameObject val = GameObject.CreatePrimitive((PrimitiveType)0);
		val.transform.SetParent(((Component)this).transform, false);
		((Component)this).transform.localScale = 0.3f * Vector3.one;
		((Component)this).transform.localScale = new Vector3(0.2f, 0.2f, 0.4f);
		obj.transform.localScale = Vector3.one * 0.3f;
		obj.transform.localPosition = Vector3.forward * 0.5f;
		val.transform.localScale = Vector3.one * 0.3f;
		val.transform.localPosition = Vector3.up * 0.5f;
		((Component)this).GetComponent<Collider>().isTrigger = true;
		model = new GameObject("Model").AddComponent<SteamVR_RenderModel>();
		((Component)model).transform.SetParent(VR.Camera.SteamCam.head, false);
		model.shader = VR.Context.Materials.StandardShader;
		model.SetDeviceIndex(0);
		((Component)model).gameObject.layer = LayerMask.NameToLayer(VR.Context.InvisibleLayer);
		Camera obj2 = ((Component)this).gameObject.AddComponent<Camera>();
		obj2.depth = 1f;
		obj2.nearClipPlane = 0.3f;
		obj2.cullingMask = 0x7FFFFFFF & ~VR.Context.UILayerMask;
		((Component)this).transform.position = S_Position;
		((Component)this).transform.rotation = S_Rotation;
	}

	protected override void OnUpdate()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		S_Position = ((Component)this).transform.position;
		S_Rotation = ((Component)this).transform.rotation;
		CheckInput();
	}

	protected void CheckInput()
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		if (!(controller != null))
		{
			return;
		}
		if (!tracking && SteamVR_Controller.Input((int)controller.Tracking.index).GetPressDown(EVRButtonId.k_EButton_Axis1))
		{
			tracking = true;
			posOffset = ((Component)this).transform.position - ((Component)controller).transform.position;
			rotOffset = Quaternion.Inverse(((Component)controller).transform.rotation) * ((Component)this).transform.rotation;
		}
		else if (tracking)
		{
			if (SteamVR_Controller.Input((int)controller.Tracking.index).GetPressUp(EVRButtonId.k_EButton_Axis1))
			{
				tracking = false;
				return;
			}
			((Component)this).transform.position = ((Component)controller).transform.position + posOffset;
			((Component)this).transform.rotation = ((Component)controller).transform.rotation * rotOffset;
		}
	}

	public void OnTriggerEnter(Collider other)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		((Component)this).GetComponent<Renderer>().material.color = Color.red;
		controller = ((Component)other).GetComponentInParent<Controller>();
		controller.ToolEnabled = false;
	}

	public void OnTriggerExit()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		((Component)this).GetComponent<Renderer>().material.color = Color.white;
		controller.ToolEnabled = true;
		if (!tracking)
		{
			controller = null;
		}
	}
}

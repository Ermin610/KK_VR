using System;
using CapturePanorama;
using UnityEngine;
using VRGIN.Controls;
using VRGIN.Core;
using Valve.VR;

namespace VRGIN.Helpers;

public class VRCapturePanorama : global::CapturePanorama.CapturePanorama
{
	private Camera _Camera;

	private IShortcut _Shortcut;

	protected override void OnStart()
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		fadeMaterial = UnityHelper.LoadFromAssetBundle<Material>(ResourceManager.Capture, "Fade material");
		convertPanoramaShader = UnityHelper.LoadFromAssetBundle<ComputeShader>(ResourceManager.Capture, "ConvertPanoramaShader");
		convertPanoramaStereoShader = UnityHelper.LoadFromAssetBundle<ComputeShader>(ResourceManager.Capture, "ConvertPanoramaStereoShader");
		textureToBufferShader = UnityHelper.LoadFromAssetBundle<ComputeShader>(ResourceManager.Capture, "TextureToBufferShader");
		captureStereoscopic = VR.Settings.Capture.Stereoscopic;
		interpupillaryDistance = SteamVR.instance.GetFloatProperty(ETrackedDeviceProperty.Prop_UserIpdMeters_Float) * VR.Settings.IPDScale;
		captureKey = (KeyCode)0;
		_Shortcut = new MultiKeyboardShortcut(VR.Settings.Capture.Shortcut, delegate
		{
			if (!Capturing)
			{
				string text = $"{Application.productName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}";
				VRLog.Info("Panorama capture key pressed, capturing " + text);
				CaptureScreenshotAsync(text);
			}
		});
		base.OnStart();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		_Shortcut.Evaluate();
	}

	public override Camera[] GetCaptureCameras()
	{
		return (Camera[])(object)new Camera[1] { _Camera };
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (Object.op_Implicit((Object)(object)_Camera))
		{
			Object.Destroy((Object)(object)((Component)_Camera).gameObject);
		}
	}

	public override bool OnCaptureStart()
	{
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)_Camera))
		{
			_Camera = VR.Camera.Clone(VR.Settings.Capture.WithEffects);
			((Component)_Camera).gameObject.SetActive(false);
			if (VR.Settings.Capture.HideGUI)
			{
				Camera camera = _Camera;
				camera.cullingMask &= ~LayerMask.GetMask(new string[1] { VR.Context.GuiLayer });
			}
		}
		((Component)_Camera).transform.position = VR.Camera.Head.position;
		if (VR.Settings.Capture.SetCameraUpright)
		{
			Vector3 val = Vector3.ProjectOnPlane(VR.Camera.Head.forward, Vector3.up);
			Vector3 val2 = ((Vector3)(ref val)).normalized;
			if ((double)((Vector3)(ref val2)).magnitude < 0.1)
			{
				val2 = Vector3.forward;
			}
			((Component)_Camera).transform.rotation = Quaternion.LookRotation(val2);
		}
		else
		{
			((Component)_Camera).transform.rotation = VR.Camera.Head.rotation;
		}
		return true;
	}

	public override void AfterRenderPanorama()
	{
		base.AfterRenderPanorama();
	}
}

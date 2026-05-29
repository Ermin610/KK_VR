using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VRGIN.Core;

public class CameraSlave : ProtectedBehaviour
{
	public Camera Camera => ((Component)this).GetComponent<Camera>();

	public float nearClipPlane { get; private set; }

	public float farClipPlane { get; private set; }

	public CameraClearFlags clearFlags { get; private set; }

	public RenderingPath renderingPath { get; private set; }

	public bool clearStencilAfterLightingPass { get; private set; }

	public DepthTextureMode depthTextureMode { get; private set; }

	public float[] layerCullDistances { get; private set; }

	public bool layerCullSpherical { get; private set; }

	public bool useOcclusionCulling { get; private set; }

	public Color backgroundColor { get; private set; }

	public int cullingMask { get; private set; }

	protected override void OnAwake()
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		base.OnAwake();
		Camera camera = Camera;
		if (!(camera != null))
		{
			VRLog.Error("No camera found! {0}", ((Object)this).name);
			Object.Destroy((Object)(object)this);
			return;
		}
		nearClipPlane = camera.nearClipPlane;
		farClipPlane = camera.farClipPlane;
		clearFlags = camera.clearFlags;
		renderingPath = camera.renderingPath;
		clearStencilAfterLightingPass = camera.clearStencilAfterLightingPass;
		depthTextureMode = camera.depthTextureMode;
		layerCullDistances = camera.layerCullDistances;
		layerCullSpherical = camera.layerCullSpherical;
		useOcclusionCulling = camera.useOcclusionCulling;
		backgroundColor = camera.backgroundColor;
		cullingMask = camera.cullingMask;
	}

	public void OnEnable()
	{
		try
		{
			VR.Camera.RegisterSlave(this);
		}
		catch (Exception obj)
		{
			VRLog.Error(obj);
		}
	}

	public void OnDisable()
	{
		try
		{
			VR.Camera.UnregisterSlave(this);
		}
		catch (Exception obj)
		{
			VRLog.Error(obj);
		}
	}
}

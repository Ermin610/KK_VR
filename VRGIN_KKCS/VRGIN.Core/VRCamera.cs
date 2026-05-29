using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using VRGIN.Helpers;

namespace VRGIN.Core;

public class VRCamera : ProtectedBehaviour
{
	private delegate void CameraOperation(Camera camera);

	private static VRCamera _Instance;

	private IList<CameraSlave> Slaves = new List<CameraSlave>();

	private const float MIN_FAR_CLIP_PLANE = 10f;

	private Camera _Camera;

	public SteamVR_Camera SteamCam { get; private set; }

	public Camera Blueprint
	{
		get
		{
			if (!(_Blueprint != null) || !((Behaviour)_Blueprint).isActiveAndEnabled)
			{
				return Slaves.Select((CameraSlave s) => s.Camera).FirstOrDefault((Camera c) => !VR.GUI.Owns(c));
			}
			return _Blueprint;
		}
	}

	private Camera _Blueprint { get; set; }

	public bool HasValidBlueprint => Slaves.Count > 0;

	public Transform Origin => SteamCam.origin;

	public Transform Head => SteamCam.head;

	public static VRCamera Instance
	{
		get
		{
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			if (_Instance == null)
			{
				_Instance = ((Component)new GameObject("VRGIN_Camera").AddComponent<AudioListener>()).gameObject.AddComponent<VRCamera>();
			}
			return _Instance;
		}
	}

	public event EventHandler<InitializeCameraEventArgs> InitializeCamera = delegate
	{
	};

	public event EventHandler<CopiedCameraEventArgs> CopiedCamera = delegate
	{
	};

	protected override void OnAwake()
	{
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		VRLog.Info("Creating VR Camera");
		_Camera = ((Component)this).gameObject.AddComponent<Camera>();
		((Component)this).gameObject.AddComponent<SteamVR_Camera>();
		SteamCam = ((Component)this).GetComponent<SteamVR_Camera>();
		SteamCam.Expand();
		if (!VR.Settings.MirrorScreen)
		{
			Object.Destroy((Object)(object)((Component)SteamCam.head).GetComponent<SteamVR_GameView>());
			Object.Destroy((Object)(object)((Component)SteamCam.head).GetComponent<Camera>());
		}
		SteamVR_Camera.sceneResolutionScale = VR.Settings.RenderScale;
		new GameObject("CenterEyeAnchor").transform.SetParent(SteamCam.head);
		Object.DontDestroyOnLoad((Object)(object)((Component)SteamCam.origin).gameObject);
	}

	public void Copy(Camera blueprint, bool master = false, bool hasOtherConsumers = false)
	{
		VRLog.Info("Copying camera: {0}", (blueprint != null) ? ((Object)blueprint).name : "NULL");
		if ((blueprint != null) && (((Component)blueprint).GetComponent<CameraSlave>() != null))
		{
			VRLog.Warn("Is already slave -- NOOP");
			return;
		}
		if (master && UseNewCamera(blueprint))
		{
			_Blueprint = blueprint ?? _Camera;
			ApplyToCameras(delegate(Camera targetCamera)
			{
				//IL_0032: Unknown result type (might be due to invalid IL or missing references)
				//IL_0038: Invalid comparison between Unknown and I4
				//IL_004a: Unknown result type (might be due to invalid IL or missing references)
				//IL_006c: Unknown result type (might be due to invalid IL or missing references)
				//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
				targetCamera.nearClipPlane = VR.Context.NearClipPlane;
				targetCamera.farClipPlane = Mathf.Max(Blueprint.farClipPlane, 10f);
				targetCamera.clearFlags = (CameraClearFlags)(((int)Blueprint.clearFlags == 1) ? 1 : 2);
				targetCamera.renderingPath = Blueprint.renderingPath;
				targetCamera.clearStencilAfterLightingPass = Blueprint.clearStencilAfterLightingPass;
				targetCamera.depthTextureMode = Blueprint.depthTextureMode;
				targetCamera.layerCullDistances = Blueprint.layerCullDistances;
				targetCamera.layerCullSpherical = Blueprint.layerCullSpherical;
				targetCamera.useOcclusionCulling = Blueprint.useOcclusionCulling;
				targetCamera.allowHDR = Blueprint.allowHDR;
				targetCamera.backgroundColor = Blueprint.backgroundColor;
				Skybox component2 = ((Component)Blueprint).GetComponent<Skybox>();
				if (component2 != null)
				{
					Skybox val = ((Component)targetCamera).gameObject.GetComponent<Skybox>();
					if (val == null)
					{
						val = ((Component)val).gameObject.AddComponent<Skybox>();
					}
					val.material = component2.material;
				}
			});
		}
		if ((blueprint != null))
		{
			((Component)blueprint).gameObject.AddComponent<CameraSlave>();
			AudioListener component = ((Component)blueprint).GetComponent<AudioListener>();
			if ((component != null))
			{
				Object.Destroy((Object)(object)component);
			}
			if (!hasOtherConsumers && blueprint.targetTexture == null && VR.Interpreter.IsIrrelevantCamera(blueprint))
			{
				((Component)blueprint).gameObject.AddComponent<CameraKiller>();
			}
		}
		if (master)
		{
			this.InitializeCamera(this, new InitializeCameraEventArgs(((Component)this).GetComponent<Camera>(), Blueprint));
		}
		this.CopiedCamera(this, new CopiedCameraEventArgs(blueprint));
	}

	private bool UseNewCamera(Camera blueprint)
	{
		if ((_Blueprint != null) && _Blueprint != _Camera && _Blueprint != blueprint && ((Object)_Blueprint).name == "Main Camera")
		{
			VRLog.Info("Using {0} over {1} as main camera", ((Object)_Blueprint).name, ((Object)blueprint).name);
			return false;
		}
		return true;
	}

	private void UpdateCameraConfig()
	{
		int num = Slaves.Aggregate(0, (int cull, CameraSlave cam) => cull | cam.cullingMask);
		num |= VR.Interpreter.DefaultCullingMask;
		num &= ~LayerMask.GetMask(new string[2]
		{
			VR.Context.UILayer,
			VR.Context.InvisibleLayer
		});
		num &= ~VR.Context.IgnoreMask;
		VRLog.Info("The camera sees {0} ({1})", string.Join(", ", UnityHelper.GetLayerNames(num)), string.Join(", ", Slaves.Select((CameraSlave s) => ((Object)s).name).ToArray()));
		((Component)this).GetComponent<Camera>().cullingMask = num;
	}

	public void CopyFX(Camera blueprint)
	{
		CopyFX(((Component)blueprint).gameObject, ((Component)this).gameObject, disabledSourceFx: true);
		FixEffectOrder();
	}

	public void FixEffectOrder()
	{
		if (!(SteamCam != null))
		{
			SteamCam = ((Component)this).GetComponent<SteamVR_Camera>();
		}
		SteamCam.ForceLast();
		SteamCam = ((Component)this).GetComponent<SteamVR_Camera>();
	}

	private void CopyFX(GameObject source, GameObject target, bool disabledSourceFx = false)
	{
		foreach (MonoBehaviour cameraEffect in target.GetCameraEffects())
		{
			Object.DestroyImmediate((Object)(object)cameraEffect);
		}
		int num = target.GetComponents<Component>().Length;
		VRLog.Info("Copying FX to {0}...", ((Object)target).name);
		foreach (MonoBehaviour cameraEffect2 in source.GetCameraEffects())
		{
			if (VR.Interpreter.IsAllowedEffect(cameraEffect2))
			{
				VRLog.Info("Copy FX: {0} (enabled={1})", ((object)cameraEffect2).GetType().Name, ((Behaviour)cameraEffect2).enabled);
				MonoBehaviour val = target.CopyComponentFrom<MonoBehaviour>(cameraEffect2);
				if ((val != null))
				{
					VRLog.Info("Attached!");
				}
				((Behaviour)val).enabled = ((Behaviour)cameraEffect2).enabled;
			}
			else
			{
				VRLog.Info("Skipping image effect {0}", ((object)cameraEffect2).GetType().Name);
			}
			if (disabledSourceFx)
			{
				((Behaviour)cameraEffect2).enabled = false;
			}
		}
		VRLog.Info("{0} components before the additions, {1} after", num, target.GetComponents<Component>().Length);
	}

	private void ApplyToCameras(CameraOperation operation)
	{
		operation(((Component)SteamCam).GetComponent<Camera>());
	}

	protected override void OnUpdate()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		base.OnUpdate();
		if ((SteamCam.origin != null))
		{
			SteamCam.origin.localScale = Vector3.one * VR.Settings.IPDScale;
		}
	}

	public void Refresh()
	{
		CopyFX(Blueprint);
	}

	internal Camera Clone(bool copyEffects = true)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		Camera val = GameObjectExtensions.CopyComponentFrom<Camera>(new GameObject("VRGIN_Camera_Clone"), ((Component)SteamCam).GetComponent<Camera>());
		if (copyEffects)
		{
			CopyFX(((Component)SteamCam).gameObject, ((Component)val).gameObject);
		}
		((Component)val).transform.position = ((Component)this).transform.position;
		((Component)val).transform.rotation = ((Component)this).transform.rotation;
		val.nearClipPlane = 0.01f;
		return val;
	}

	internal void RegisterSlave(CameraSlave slave)
	{
		VRLog.Info("Camera went online: {0}", ((Object)slave).name);
		Slaves.Add(slave);
		UpdateCameraConfig();
	}

	internal void UnregisterSlave(CameraSlave slave)
	{
		VRLog.Info("Camera went offline: {0}", ((Object)slave).name);
		Slaves.Remove(slave);
		UpdateCameraConfig();
	}
}

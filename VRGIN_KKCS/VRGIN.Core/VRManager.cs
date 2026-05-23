using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRGIN.Controls.Speech;
using VRGIN.Modes;
using WindowsInput;

namespace VRGIN.Core;

public class VRManager : ProtectedBehaviour
{
	private VRGUI _Gui;

	private bool _CameraLoaded;

	private bool _IsEnabledEffects;

	private static VRManager _Instance;

	private HashSet<Camera> _CheckedCameras = new HashSet<Camera>();

	private static Type ModeType;

	public static VRManager Instance
	{
		get
		{
			if ((Object)(object)_Instance == (Object)null)
			{
				throw new InvalidOperationException("VR Manager has not been created yet!");
			}
			return _Instance;
		}
	}

	public IVRManagerContext Context { get; private set; }

	public GameInterpreter Interpreter { get; private set; }

	public SpeechManager Speech { get; private set; }

	public HMDType HMD { get; private set; }

	public ControlMode Mode { get; private set; }

	public InputSimulator Input { get; internal set; }

	public event EventHandler<ModeInitializedEventArgs> ModeInitialized = delegate
	{
	};

	public static VRManager Create<T>(IVRManagerContext context) where T : GameInterpreter
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)_Instance == (Object)null)
		{
			VR.Active = true;
			_Instance = new GameObject("VRGIN_Manager").AddComponent<VRManager>();
			_Instance.Context = context;
			_Instance.Interpreter = ((Component)_Instance).gameObject.AddComponent<T>();
			_Instance._Gui = VRGUI.Instance;
			_Instance.Input = new InputSimulator();
			if (VR.Settings.SpeechRecognition)
			{
				_Instance.Speech = ((Component)_Instance).gameObject.AddComponent<SpeechManager>();
			}
			if (VR.Settings.ApplyEffects)
			{
				_Instance.EnableEffects();
			}
			VR.Settings.Save();
		}
		return _Instance;
	}

	public void SetMode<T>() where T : ControlMode
	{
		if ((Object)(object)Mode == (Object)null || !(Mode is T))
		{
			ModeType = typeof(T);
			if ((Object)(object)Mode != (Object)null)
			{
				Mode.ControllersCreated -= OnControllersCreated;
				Object.DestroyImmediate((Object)(object)Mode);
			}
			Mode = ((Component)VRCamera.Instance).gameObject.AddComponent<T>();
			Mode.ControllersCreated += OnControllersCreated;
		}
	}

	protected override void OnAwake()
	{
		string hmd_TrackingSystemName = SteamVR.instance.hmd_TrackingSystemName;
		VRLog.Info("------------------------------------");
		VRLog.Info(" Booting VR [{0}]", hmd_TrackingSystemName);
		VRLog.Info("------------------------------------");
		HMD = ((!(hmd_TrackingSystemName == "oculus")) ? ((hmd_TrackingSystemName == "lighthouse") ? HMDType.Vive : HMDType.Other) : HMDType.Oculus);
		Application.targetFrameRate = 90;
		Time.fixedDeltaTime = 1f / 90f;
		Application.runInBackground = true;
		Object.DontDestroyOnLoad((Object)(object)((Component)SteamVR_Render.instance).gameObject);
		Object.DontDestroyOnLoad((Object)(object)((Component)this).gameObject);
	}

	protected override void OnStart()
	{
	}

	protected override void OnLevel(int level)
	{
		_CheckedCameras.Clear();
	}

	protected override void OnUpdate()
	{
		foreach (Camera item in Camera.allCameras.Except(_CheckedCameras).ToList())
		{
			_CheckedCameras.Add(item);
			CameraJudgement cameraJudgement = VR.Interpreter.JudgeCamera(item);
			VRLog.Info("Detected new camera {0} Action: {1}", ((Object)item).name, cameraJudgement);
			switch (cameraJudgement)
			{
			case CameraJudgement.MainCamera:
				VR.Camera.Copy(item, master: true);
				if (_IsEnabledEffects)
				{
					ApplyEffects();
				}
				break;
			case CameraJudgement.SubCamera:
				VR.Camera.Copy(item);
				break;
			case CameraJudgement.GUI:
				VR.GUI.AddCamera(item);
				break;
			case CameraJudgement.GUIAndCamera:
				VR.Camera.Copy(item, master: false, hasOtherConsumers: true);
				VR.GUI.AddCamera(item);
				break;
			}
		}
	}

	private void OnControllersCreated(object sender, EventArgs e)
	{
		this.ModeInitialized(this, new ModeInitializedEventArgs(Mode));
	}

	public void EnableEffects()
	{
		_IsEnabledEffects = true;
		if (Object.op_Implicit((Object)(object)VR.Camera.Blueprint))
		{
			ApplyEffects();
		}
	}

	public void DisableEffects()
	{
		_IsEnabledEffects = false;
	}

	public void ToggleEffects()
	{
		if (_IsEnabledEffects)
		{
			DisableEffects();
		}
		else
		{
			EnableEffects();
		}
	}

	private void ApplyEffects()
	{
		VR.Camera.CopyFX(VR.Camera.Blueprint);
	}
}

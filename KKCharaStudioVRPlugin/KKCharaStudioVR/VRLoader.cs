using System;
using System.Collections;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.VR;
using VRGIN.Core;
using VRSettings = UnityEngine.VR.VRSettings;

namespace KKCharaStudioVR;

internal class VRLoader : ProtectedBehaviour
{
	private static string DeviceOpenVR = "OpenVR";

	// Unity 5.6 disables native VR by loading an empty device name. "None" is
	// the serialized fallback label in globalgamemanagers, not a valid runtime
	// device name for VRSettings.LoadDeviceByName.
	private static string DeviceNone = string.Empty;

	private const float DeviceLoadTimeoutSeconds = 10f;

	private static bool _isVREnable = false;

	private static VRLoader _Instance;

	public static VRLoader Instance
	{
		get
		{
			if (_Instance == null)
			{
				throw new InvalidOperationException("VR Loader has not been created yet!");
			}
			return _Instance;
		}
	}

	public static VRLoader Create(bool isEnable)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		_isVREnable = isEnable;
		_Instance = new GameObject("VRLoader").AddComponent<VRLoader>();
		return _Instance;
	}

	protected override void OnAwake()
	{
		if (_isVREnable)
		{
			((MonoBehaviour)this).StartCoroutine(LoadDevice(DeviceOpenVR));
		}
		else
		{
			((MonoBehaviour)this).StartCoroutine(LoadDevice(DeviceNone));
		}
	}

	private IVRManagerContext CreateContext(string path)
	{
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(ConfigurableContext));
		if (File.Exists(path))
		{
			using FileStream stream = File.OpenRead(path);
			try
			{
				return xmlSerializer.Deserialize(stream) as ConfigurableContext;
			}
			catch (Exception)
			{
				VRLog.Error("Failed to deserialize {0} -- using default", path);
			}
		}
		ConfigurableContext configurableContext = new ConfigurableContext();
		try
		{
			using StreamWriter streamWriter = new StreamWriter(path);
			streamWriter.BaseStream.SetLength(0L);
			xmlSerializer.Serialize(streamWriter, configurableContext);
		}
		catch (Exception)
		{
			VRLog.Error("Failed to write {0}", path);
		}
		return configurableContext;
	}

	private IEnumerator LoadDevice(string newDevice)
	{
		bool vrMode = !string.IsNullOrEmpty(newDevice);
		if (vrMode && VRSettings.enabled && VRSettings.loadedDeviceName == newDevice)
		{
			VRLog.Info("VR device '{0}' already active, skipping LoadDeviceByName", newDevice);
		}
		else
		{
			VRLog.Info("VR not active at engine startup (enabled={0}, device='{1}'), loading via plugin",
				VRSettings.enabled, VRSettings.loadedDeviceName);
			VRSettings.LoadDeviceByName(newDevice);
			yield return null;
			VRSettings.enabled = vrMode;
			yield return null;
			float deadline = Time.realtimeSinceStartup + DeviceLoadTimeoutSeconds;
			while (VRSettings.enabled != vrMode
				|| (vrMode && !string.Equals(
					VRSettings.loadedDeviceName,
					newDevice,
					StringComparison.OrdinalIgnoreCase)))
			{
				if (Time.realtimeSinceStartup >= deadline)
				{
					VRLog.Error(
						"Timed out while switching VR device to '{0}' (enabled={1}, loaded='{2}')",
						vrMode ? newDevice : "<none>",
						VRSettings.enabled,
						VRSettings.loadedDeviceName);
					if (vrMode)
						yield break;

					// A failed native unload cannot remove a DLL that Unity already
					// mapped, but keeping VR disabled still restores desktop control.
					VRSettings.enabled = false;
					break;
				}
				yield return null;
			}
		}
		if (!vrMode)
		{
			VRLog.Info("Native VR runtime disabled; managed VR objects were not created.");
			yield break;
		}
		if (vrMode)
		{
			VRManager.Create<KKCharaStudioInterpreter>(CreateContext("KKCSVRContext.xml"));
			VR.Manager.SetMode<GenericStandingMode>();
			GameObject val = new GameObject("KKCharaStudioVR");
			UnityEngine.Object.DontDestroyOnLoad(val);
			IKTool.Create(val);
			VRControllerMgr.Install(val);
			VRCameraMoveHelper.Install(val);
			VRItemObjMoveHelper.Install(val);
			val.AddComponent<DynamicBoneColliderManager>();
			val.AddComponent<KKCharaStudioVRGUI>();
			val.AddComponent<VRHandModelManager>();
			val.AddComponent<VRQuickActions>();
			val.AddComponent<VRMmdPlaybackController>();
			val.AddComponent<VRMmdCameraAnchorController>();
			val.AddComponent<VRWristMenuController>();
			val.AddComponent<VRTimelineCameraFollowController>();
			val.AddComponent<VRComfortVignette>();
			val.AddComponent<VRTwoHandScale>(); // Controlled by TwoHandScaleEnabled setting
			UnityEngine.Object.DontDestroyOnLoad(((Component)VRCamera.Instance).gameObject);
		}
	}
}

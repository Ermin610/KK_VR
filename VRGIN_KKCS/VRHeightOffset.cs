using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.VR;

public class VRHeightOffset : MonoBehaviour
{
	[Serializable]
	public class DeviceHeightPair
	{
		public string DeviceName;

		public float HeightOffset;

		public DeviceHeightPair(string deviceName, float heightOffset)
		{
			DeviceName = deviceName;
			HeightOffset = heightOffset;
		}
	}

	public DeviceHeightPair[] _deviceOffsets;

	private void Reset()
	{
		_deviceOffsets = new DeviceHeightPair[1];
		_deviceOffsets[0] = new DeviceHeightPair("oculus", 1f);
	}

	private void Start()
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		if (VRDevice.isPresent && VRSettings.enabled && _deviceOffsets != null)
		{
			string deviceName = VRDevice.family;
			DeviceHeightPair deviceHeightPair = _deviceOffsets.FirstOrDefault((DeviceHeightPair d) => deviceName.ToLower().Contains(d.DeviceName.ToLower()));
			if (deviceHeightPair != null)
			{
				((Component)this).transform.Translate(Vector3.up * deviceHeightPair.HeightOffset);
			}
		}
	}
}

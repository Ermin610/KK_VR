using System.Runtime.InteropServices;

namespace Valve.VR;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct VREvent_t_Packed
{
	public uint eventType;

	public uint trackedDeviceIndex;

	public float eventAgeSeconds;

	public VREvent_Data_t data;

	public VREvent_t_Packed(VREvent_t unpacked)
	{
		eventType = unpacked.eventType;
		trackedDeviceIndex = unpacked.trackedDeviceIndex;
		eventAgeSeconds = unpacked.eventAgeSeconds;
		data = unpacked.data;
	}

	public void Unpack(ref VREvent_t unpacked)
	{
		unpacked.eventType = eventType;
		unpacked.trackedDeviceIndex = trackedDeviceIndex;
		unpacked.eventAgeSeconds = eventAgeSeconds;
		unpacked.data = data;
	}
}

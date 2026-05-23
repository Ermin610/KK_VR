using System.Runtime.InteropServices;

namespace Valve.VR;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct VRControllerState_t_Packed
{
	public uint unPacketNum;

	public ulong ulButtonPressed;

	public ulong ulButtonTouched;

	public VRControllerAxis_t rAxis0;

	public VRControllerAxis_t rAxis1;

	public VRControllerAxis_t rAxis2;

	public VRControllerAxis_t rAxis3;

	public VRControllerAxis_t rAxis4;

	public VRControllerState_t_Packed(VRControllerState_t unpacked)
	{
		unPacketNum = unpacked.unPacketNum;
		ulButtonPressed = unpacked.ulButtonPressed;
		ulButtonTouched = unpacked.ulButtonTouched;
		rAxis0 = unpacked.rAxis0;
		rAxis1 = unpacked.rAxis1;
		rAxis2 = unpacked.rAxis2;
		rAxis3 = unpacked.rAxis3;
		rAxis4 = unpacked.rAxis4;
	}

	public void Unpack(ref VRControllerState_t unpacked)
	{
		unpacked.unPacketNum = unPacketNum;
		unpacked.ulButtonPressed = ulButtonPressed;
		unpacked.ulButtonTouched = ulButtonTouched;
		unpacked.rAxis0 = rAxis0;
		unpacked.rAxis1 = rAxis1;
		unpacked.rAxis2 = rAxis2;
		unpacked.rAxis3 = rAxis3;
		unpacked.rAxis4 = rAxis4;
	}
}

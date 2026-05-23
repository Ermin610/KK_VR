namespace Valve.VR;

public struct VREvent_HapticVibration_t
{
	public ulong containerHandle;

	public ulong componentHandle;

	public float fDurationSeconds;

	public float fFrequency;

	public float fAmplitude;
}

using System;

namespace Valve.VR;

public struct VRTextureDepthInfo_t
{
	public IntPtr handle;

	public HmdMatrix44_t mProjection;

	public HmdVector2_t vRange;
}

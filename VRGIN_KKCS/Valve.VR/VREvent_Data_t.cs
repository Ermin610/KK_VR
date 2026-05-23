using System.Runtime.InteropServices;

namespace Valve.VR;

[StructLayout(LayoutKind.Explicit)]
public struct VREvent_Data_t
{
	[FieldOffset(0)]
	public VREvent_Reserved_t reserved;

	[FieldOffset(0)]
	public VREvent_Controller_t controller;

	[FieldOffset(0)]
	public VREvent_Mouse_t mouse;

	[FieldOffset(0)]
	public VREvent_Scroll_t scroll;

	[FieldOffset(0)]
	public VREvent_Process_t process;

	[FieldOffset(0)]
	public VREvent_Notification_t notification;

	[FieldOffset(0)]
	public VREvent_Overlay_t overlay;

	[FieldOffset(0)]
	public VREvent_Status_t status;

	[FieldOffset(0)]
	public VREvent_Ipd_t ipd;

	[FieldOffset(0)]
	public VREvent_Chaperone_t chaperone;

	[FieldOffset(0)]
	public VREvent_PerformanceTest_t performanceTest;

	[FieldOffset(0)]
	public VREvent_TouchPadMove_t touchPadMove;

	[FieldOffset(0)]
	public VREvent_SeatedZeroPoseReset_t seatedZeroPoseReset;

	[FieldOffset(0)]
	public VREvent_Screenshot_t screenshot;

	[FieldOffset(0)]
	public VREvent_ScreenshotProgress_t screenshotProgress;

	[FieldOffset(0)]
	public VREvent_ApplicationLaunch_t applicationLaunch;

	[FieldOffset(0)]
	public VREvent_EditingCameraSurface_t cameraSurface;

	[FieldOffset(0)]
	public VREvent_MessageOverlay_t messageOverlay;

	[FieldOffset(0)]
	public VREvent_Property_t property;

	[FieldOffset(0)]
	public VREvent_DualAnalog_t dualAnalog;

	[FieldOffset(0)]
	public VREvent_HapticVibration_t hapticVibration;

	[FieldOffset(0)]
	public VREvent_Keyboard_t keyboard;
}

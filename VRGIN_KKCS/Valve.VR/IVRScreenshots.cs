using System.Runtime.InteropServices;
using System.Text;

namespace Valve.VR;

public struct IVRScreenshots
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate EVRScreenshotError _RequestScreenshot(ref uint pOutScreenshotHandle, EVRScreenshotType type, string pchPreviewFilename, string pchVRFilename);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate EVRScreenshotError _HookScreenshot([In][Out] EVRScreenshotType[] pSupportedTypes, int numTypes);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate EVRScreenshotType _GetScreenshotPropertyType(uint screenshotHandle, ref EVRScreenshotError pError);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate uint _GetScreenshotPropertyFilename(uint screenshotHandle, EVRScreenshotPropertyFilenames filenameType, StringBuilder pchFilename, uint cchFilename, ref EVRScreenshotError pError);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate EVRScreenshotError _UpdateScreenshotProgress(uint screenshotHandle, float flProgress);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate EVRScreenshotError _TakeStereoScreenshot(ref uint pOutScreenshotHandle, string pchPreviewFilename, string pchVRFilename);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate EVRScreenshotError _SubmitScreenshot(uint screenshotHandle, EVRScreenshotType type, string pchSourcePreviewFilename, string pchSourceVRFilename);

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _RequestScreenshot RequestScreenshot;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _HookScreenshot HookScreenshot;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetScreenshotPropertyType GetScreenshotPropertyType;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetScreenshotPropertyFilename GetScreenshotPropertyFilename;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _UpdateScreenshotProgress UpdateScreenshotProgress;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _TakeStereoScreenshot TakeStereoScreenshot;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _SubmitScreenshot SubmitScreenshot;
}

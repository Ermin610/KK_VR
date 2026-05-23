using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Valve.VR;

public struct IVRCompositor
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _SetTrackingSpace(ETrackingUniverseOrigin eOrigin);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate ETrackingUniverseOrigin _GetTrackingSpace();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate EVRCompositorError _WaitGetPoses([In][Out] TrackedDevicePose_t[] pRenderPoseArray, uint unRenderPoseArrayCount, [In][Out] TrackedDevicePose_t[] pGamePoseArray, uint unGamePoseArrayCount);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate EVRCompositorError _GetLastPoses([In][Out] TrackedDevicePose_t[] pRenderPoseArray, uint unRenderPoseArrayCount, [In][Out] TrackedDevicePose_t[] pGamePoseArray, uint unGamePoseArrayCount);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate EVRCompositorError _GetLastPoseForTrackedDeviceIndex(uint unDeviceIndex, ref TrackedDevicePose_t pOutputPose, ref TrackedDevicePose_t pOutputGamePose);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate EVRCompositorError _Submit(EVREye eEye, ref Texture_t pTexture, ref VRTextureBounds_t pBounds, EVRSubmitFlags nSubmitFlags);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _ClearLastSubmittedFrame();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _PostPresentHandoff();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate bool _GetFrameTiming(ref Compositor_FrameTiming pTiming, uint unFramesAgo);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate uint _GetFrameTimings(ref Compositor_FrameTiming pTiming, uint nFrames);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate float _GetFrameTimeRemaining();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _GetCumulativeStats(ref Compositor_CumulativeStats pStats, uint nStatsSizeInBytes);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _FadeToColor(float fSeconds, float fRed, float fGreen, float fBlue, float fAlpha, bool bBackground);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate HmdColor_t _GetCurrentFadeColor(bool bBackground);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _FadeGrid(float fSeconds, bool bFadeIn);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate float _GetCurrentGridAlpha();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate EVRCompositorError _SetSkyboxOverride([In][Out] Texture_t[] pTextures, uint unTextureCount);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _ClearSkyboxOverride();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _CompositorBringToFront();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _CompositorGoToBack();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _CompositorQuit();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate bool _IsFullscreen();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate uint _GetCurrentSceneFocusProcess();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate uint _GetLastFrameRenderer();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate bool _CanRenderScene();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _ShowMirrorWindow();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _HideMirrorWindow();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate bool _IsMirrorWindowVisible();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _CompositorDumpImages();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate bool _ShouldAppRenderWithLowResources();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _ForceInterleavedReprojectionOn(bool bOverride);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _ForceReconnectProcess();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _SuspendRendering(bool bSuspend);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate EVRCompositorError _GetMirrorTextureD3D11(EVREye eEye, IntPtr pD3D11DeviceOrResource, ref IntPtr ppD3D11ShaderResourceView);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _ReleaseMirrorTextureD3D11(IntPtr pD3D11ShaderResourceView);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate EVRCompositorError _GetMirrorTextureGL(EVREye eEye, ref uint pglTextureId, IntPtr pglSharedTextureHandle);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate bool _ReleaseSharedGLTexture(uint glTextureId, IntPtr glSharedTextureHandle);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _LockGLSharedTextureForAccess(IntPtr glSharedTextureHandle);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _UnlockGLSharedTextureForAccess(IntPtr glSharedTextureHandle);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate uint _GetVulkanInstanceExtensionsRequired(StringBuilder pchValue, uint unBufferSize);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate uint _GetVulkanDeviceExtensionsRequired(IntPtr pPhysicalDevice, StringBuilder pchValue, uint unBufferSize);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _SetExplicitTimingMode(EVRCompositorTimingMode eTimingMode);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate EVRCompositorError _SubmitExplicitTimingData();

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _SetTrackingSpace SetTrackingSpace;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetTrackingSpace GetTrackingSpace;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _WaitGetPoses WaitGetPoses;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetLastPoses GetLastPoses;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetLastPoseForTrackedDeviceIndex GetLastPoseForTrackedDeviceIndex;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _Submit Submit;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _ClearLastSubmittedFrame ClearLastSubmittedFrame;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _PostPresentHandoff PostPresentHandoff;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetFrameTiming GetFrameTiming;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetFrameTimings GetFrameTimings;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetFrameTimeRemaining GetFrameTimeRemaining;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetCumulativeStats GetCumulativeStats;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _FadeToColor FadeToColor;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetCurrentFadeColor GetCurrentFadeColor;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _FadeGrid FadeGrid;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetCurrentGridAlpha GetCurrentGridAlpha;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _SetSkyboxOverride SetSkyboxOverride;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _ClearSkyboxOverride ClearSkyboxOverride;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _CompositorBringToFront CompositorBringToFront;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _CompositorGoToBack CompositorGoToBack;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _CompositorQuit CompositorQuit;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _IsFullscreen IsFullscreen;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetCurrentSceneFocusProcess GetCurrentSceneFocusProcess;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetLastFrameRenderer GetLastFrameRenderer;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _CanRenderScene CanRenderScene;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _ShowMirrorWindow ShowMirrorWindow;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _HideMirrorWindow HideMirrorWindow;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _IsMirrorWindowVisible IsMirrorWindowVisible;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _CompositorDumpImages CompositorDumpImages;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _ShouldAppRenderWithLowResources ShouldAppRenderWithLowResources;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _ForceInterleavedReprojectionOn ForceInterleavedReprojectionOn;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _ForceReconnectProcess ForceReconnectProcess;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _SuspendRendering SuspendRendering;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetMirrorTextureD3D11 GetMirrorTextureD3D11;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _ReleaseMirrorTextureD3D11 ReleaseMirrorTextureD3D11;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetMirrorTextureGL GetMirrorTextureGL;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _ReleaseSharedGLTexture ReleaseSharedGLTexture;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _LockGLSharedTextureForAccess LockGLSharedTextureForAccess;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _UnlockGLSharedTextureForAccess UnlockGLSharedTextureForAccess;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetVulkanInstanceExtensionsRequired GetVulkanInstanceExtensionsRequired;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetVulkanDeviceExtensionsRequired GetVulkanDeviceExtensionsRequired;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _SetExplicitTimingMode SetExplicitTimingMode;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _SubmitExplicitTimingData SubmitExplicitTimingData;
}

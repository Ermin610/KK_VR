using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Valve.VR;

public class CVRCompositor
{
	private IVRCompositor FnTable;

	internal CVRCompositor(IntPtr pInterface)
	{
		FnTable = (IVRCompositor)Marshal.PtrToStructure(pInterface, typeof(IVRCompositor));
	}

	public void SetTrackingSpace(ETrackingUniverseOrigin eOrigin)
	{
		FnTable.SetTrackingSpace(eOrigin);
	}

	public ETrackingUniverseOrigin GetTrackingSpace()
	{
		return FnTable.GetTrackingSpace();
	}

	public EVRCompositorError WaitGetPoses(TrackedDevicePose_t[] pRenderPoseArray, TrackedDevicePose_t[] pGamePoseArray)
	{
		return FnTable.WaitGetPoses(pRenderPoseArray, (uint)pRenderPoseArray.Length, pGamePoseArray, (uint)pGamePoseArray.Length);
	}

	public EVRCompositorError GetLastPoses(TrackedDevicePose_t[] pRenderPoseArray, TrackedDevicePose_t[] pGamePoseArray)
	{
		return FnTable.GetLastPoses(pRenderPoseArray, (uint)pRenderPoseArray.Length, pGamePoseArray, (uint)pGamePoseArray.Length);
	}

	public EVRCompositorError GetLastPoseForTrackedDeviceIndex(uint unDeviceIndex, ref TrackedDevicePose_t pOutputPose, ref TrackedDevicePose_t pOutputGamePose)
	{
		return FnTable.GetLastPoseForTrackedDeviceIndex(unDeviceIndex, ref pOutputPose, ref pOutputGamePose);
	}

	public EVRCompositorError Submit(EVREye eEye, ref Texture_t pTexture, ref VRTextureBounds_t pBounds, EVRSubmitFlags nSubmitFlags)
	{
		return FnTable.Submit(eEye, ref pTexture, ref pBounds, nSubmitFlags);
	}

	public void ClearLastSubmittedFrame()
	{
		FnTable.ClearLastSubmittedFrame();
	}

	public void PostPresentHandoff()
	{
		FnTable.PostPresentHandoff();
	}

	public bool GetFrameTiming(ref Compositor_FrameTiming pTiming, uint unFramesAgo)
	{
		return FnTable.GetFrameTiming(ref pTiming, unFramesAgo);
	}

	public uint GetFrameTimings(ref Compositor_FrameTiming pTiming, uint nFrames)
	{
		return FnTable.GetFrameTimings(ref pTiming, nFrames);
	}

	public float GetFrameTimeRemaining()
	{
		return FnTable.GetFrameTimeRemaining();
	}

	public void GetCumulativeStats(ref Compositor_CumulativeStats pStats, uint nStatsSizeInBytes)
	{
		FnTable.GetCumulativeStats(ref pStats, nStatsSizeInBytes);
	}

	public void FadeToColor(float fSeconds, float fRed, float fGreen, float fBlue, float fAlpha, bool bBackground)
	{
		FnTable.FadeToColor(fSeconds, fRed, fGreen, fBlue, fAlpha, bBackground);
	}

	public HmdColor_t GetCurrentFadeColor(bool bBackground)
	{
		return FnTable.GetCurrentFadeColor(bBackground);
	}

	public void FadeGrid(float fSeconds, bool bFadeIn)
	{
		FnTable.FadeGrid(fSeconds, bFadeIn);
	}

	public float GetCurrentGridAlpha()
	{
		return FnTable.GetCurrentGridAlpha();
	}

	public EVRCompositorError SetSkyboxOverride(Texture_t[] pTextures)
	{
		return FnTable.SetSkyboxOverride(pTextures, (uint)pTextures.Length);
	}

	public void ClearSkyboxOverride()
	{
		FnTable.ClearSkyboxOverride();
	}

	public void CompositorBringToFront()
	{
		FnTable.CompositorBringToFront();
	}

	public void CompositorGoToBack()
	{
		FnTable.CompositorGoToBack();
	}

	public void CompositorQuit()
	{
		FnTable.CompositorQuit();
	}

	public bool IsFullscreen()
	{
		return FnTable.IsFullscreen();
	}

	public uint GetCurrentSceneFocusProcess()
	{
		return FnTable.GetCurrentSceneFocusProcess();
	}

	public uint GetLastFrameRenderer()
	{
		return FnTable.GetLastFrameRenderer();
	}

	public bool CanRenderScene()
	{
		return FnTable.CanRenderScene();
	}

	public void ShowMirrorWindow()
	{
		FnTable.ShowMirrorWindow();
	}

	public void HideMirrorWindow()
	{
		FnTable.HideMirrorWindow();
	}

	public bool IsMirrorWindowVisible()
	{
		return FnTable.IsMirrorWindowVisible();
	}

	public void CompositorDumpImages()
	{
		FnTable.CompositorDumpImages();
	}

	public bool ShouldAppRenderWithLowResources()
	{
		return FnTable.ShouldAppRenderWithLowResources();
	}

	public void ForceInterleavedReprojectionOn(bool bOverride)
	{
		FnTable.ForceInterleavedReprojectionOn(bOverride);
	}

	public void ForceReconnectProcess()
	{
		FnTable.ForceReconnectProcess();
	}

	public void SuspendRendering(bool bSuspend)
	{
		FnTable.SuspendRendering(bSuspend);
	}

	public EVRCompositorError GetMirrorTextureD3D11(EVREye eEye, IntPtr pD3D11DeviceOrResource, ref IntPtr ppD3D11ShaderResourceView)
	{
		return FnTable.GetMirrorTextureD3D11(eEye, pD3D11DeviceOrResource, ref ppD3D11ShaderResourceView);
	}

	public void ReleaseMirrorTextureD3D11(IntPtr pD3D11ShaderResourceView)
	{
		FnTable.ReleaseMirrorTextureD3D11(pD3D11ShaderResourceView);
	}

	public EVRCompositorError GetMirrorTextureGL(EVREye eEye, ref uint pglTextureId, IntPtr pglSharedTextureHandle)
	{
		pglTextureId = 0u;
		return FnTable.GetMirrorTextureGL(eEye, ref pglTextureId, pglSharedTextureHandle);
	}

	public bool ReleaseSharedGLTexture(uint glTextureId, IntPtr glSharedTextureHandle)
	{
		return FnTable.ReleaseSharedGLTexture(glTextureId, glSharedTextureHandle);
	}

	public void LockGLSharedTextureForAccess(IntPtr glSharedTextureHandle)
	{
		FnTable.LockGLSharedTextureForAccess(glSharedTextureHandle);
	}

	public void UnlockGLSharedTextureForAccess(IntPtr glSharedTextureHandle)
	{
		FnTable.UnlockGLSharedTextureForAccess(glSharedTextureHandle);
	}

	public uint GetVulkanInstanceExtensionsRequired(StringBuilder pchValue, uint unBufferSize)
	{
		return FnTable.GetVulkanInstanceExtensionsRequired(pchValue, unBufferSize);
	}

	public uint GetVulkanDeviceExtensionsRequired(IntPtr pPhysicalDevice, StringBuilder pchValue, uint unBufferSize)
	{
		return FnTable.GetVulkanDeviceExtensionsRequired(pPhysicalDevice, pchValue, unBufferSize);
	}

	public void SetExplicitTimingMode(EVRCompositorTimingMode eTimingMode)
	{
		FnTable.SetExplicitTimingMode(eTimingMode);
	}

	public EVRCompositorError SubmitExplicitTimingData()
	{
		return FnTable.SubmitExplicitTimingData();
	}
}

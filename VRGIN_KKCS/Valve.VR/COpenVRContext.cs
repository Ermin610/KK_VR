using System;

namespace Valve.VR;

public struct COpenVRContext
{
	public IntPtr m_pVRSystem;

	public IntPtr m_pVRChaperone;

	public IntPtr m_pVRChaperoneSetup;

	public IntPtr m_pVRCompositor;

	public IntPtr m_pVROverlay;

	public IntPtr m_pVRResources;

	public IntPtr m_pVRRenderModels;

	public IntPtr m_pVRExtendedDisplay;

	public IntPtr m_pVRSettings;

	public IntPtr m_pVRApplications;

	public IntPtr m_pVRTrackedCamera;

	public IntPtr m_pVRScreenshots;

	public IntPtr m_pVRDriverManager;
}

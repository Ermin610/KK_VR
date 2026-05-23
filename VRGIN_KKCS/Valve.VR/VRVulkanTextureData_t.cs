using System;

namespace Valve.VR;

public struct VRVulkanTextureData_t
{
	public ulong m_nImage;

	public IntPtr m_pDevice;

	public IntPtr m_pPhysicalDevice;

	public IntPtr m_pInstance;

	public IntPtr m_pQueue;

	public uint m_nQueueFamilyIndex;

	public uint m_nWidth;

	public uint m_nHeight;

	public uint m_nFormat;

	public uint m_nSampleCount;
}

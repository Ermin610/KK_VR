using System;

namespace Valve.VR;

public struct D3D12TextureData_t
{
	public IntPtr m_pResource;

	public IntPtr m_pCommandQueue;

	public uint m_nNodeMask;
}

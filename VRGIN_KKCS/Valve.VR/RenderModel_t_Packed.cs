using System;
using System.Runtime.InteropServices;

namespace Valve.VR;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct RenderModel_t_Packed
{
	public IntPtr rVertexData;

	public uint unVertexCount;

	public IntPtr rIndexData;

	public uint unTriangleCount;

	public int diffuseTextureId;

	public RenderModel_t_Packed(RenderModel_t unpacked)
	{
		rVertexData = unpacked.rVertexData;
		unVertexCount = unpacked.unVertexCount;
		rIndexData = unpacked.rIndexData;
		unTriangleCount = unpacked.unTriangleCount;
		diffuseTextureId = unpacked.diffuseTextureId;
	}

	public void Unpack(ref RenderModel_t unpacked)
	{
		unpacked.rVertexData = rVertexData;
		unpacked.unVertexCount = unVertexCount;
		unpacked.rIndexData = rIndexData;
		unpacked.unTriangleCount = unTriangleCount;
		unpacked.diffuseTextureId = diffuseTextureId;
	}
}

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Valve.VR;

public class CVRDriverManager
{
	private IVRDriverManager FnTable;

	internal CVRDriverManager(IntPtr pInterface)
	{
		FnTable = (IVRDriverManager)Marshal.PtrToStructure(pInterface, typeof(IVRDriverManager));
	}

	public uint GetDriverCount()
	{
		return FnTable.GetDriverCount();
	}

	public uint GetDriverName(uint nDriver, StringBuilder pchValue, uint unBufferSize)
	{
		return FnTable.GetDriverName(nDriver, pchValue, unBufferSize);
	}
}

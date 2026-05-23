using System.Runtime.InteropServices;
using System.Text;

namespace Valve.VR;

public struct IVRDriverManager
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate uint _GetDriverCount();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate uint _GetDriverName(uint nDriver, StringBuilder pchValue, uint unBufferSize);

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetDriverCount GetDriverCount;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetDriverName GetDriverName;
}

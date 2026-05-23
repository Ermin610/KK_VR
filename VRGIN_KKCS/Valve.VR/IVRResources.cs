using System.Runtime.InteropServices;
using System.Text;

namespace Valve.VR;

public struct IVRResources
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate uint _LoadSharedResource(string pchResourceName, string pchBuffer, uint unBufferLen);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate uint _GetResourceFullPath(string pchResourceName, string pchResourceTypeDirectory, StringBuilder pchPathBuffer, uint unBufferLen);

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _LoadSharedResource LoadSharedResource;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetResourceFullPath GetResourceFullPath;
}

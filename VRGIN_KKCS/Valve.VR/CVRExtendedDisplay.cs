using System;
using System.Runtime.InteropServices;

namespace Valve.VR;

public class CVRExtendedDisplay
{
	private IVRExtendedDisplay FnTable;

	internal CVRExtendedDisplay(IntPtr pInterface)
	{
		FnTable = (IVRExtendedDisplay)Marshal.PtrToStructure(pInterface, typeof(IVRExtendedDisplay));
	}

	public void GetWindowBounds(ref int pnX, ref int pnY, ref uint pnWidth, ref uint pnHeight)
	{
		pnX = 0;
		pnY = 0;
		pnWidth = 0u;
		pnHeight = 0u;
		FnTable.GetWindowBounds(ref pnX, ref pnY, ref pnWidth, ref pnHeight);
	}

	public void GetEyeOutputViewport(EVREye eEye, ref uint pnX, ref uint pnY, ref uint pnWidth, ref uint pnHeight)
	{
		pnX = 0u;
		pnY = 0u;
		pnWidth = 0u;
		pnHeight = 0u;
		FnTable.GetEyeOutputViewport(eEye, ref pnX, ref pnY, ref pnWidth, ref pnHeight);
	}

	public void GetDXGIOutputInfo(ref int pnAdapterIndex, ref int pnAdapterOutputIndex)
	{
		pnAdapterIndex = 0;
		pnAdapterOutputIndex = 0;
		FnTable.GetDXGIOutputInfo(ref pnAdapterIndex, ref pnAdapterOutputIndex);
	}
}

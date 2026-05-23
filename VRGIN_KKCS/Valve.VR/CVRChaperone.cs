using System;
using System.Runtime.InteropServices;

namespace Valve.VR;

public class CVRChaperone
{
	private IVRChaperone FnTable;

	internal CVRChaperone(IntPtr pInterface)
	{
		FnTable = (IVRChaperone)Marshal.PtrToStructure(pInterface, typeof(IVRChaperone));
	}

	public ChaperoneCalibrationState GetCalibrationState()
	{
		return FnTable.GetCalibrationState();
	}

	public bool GetPlayAreaSize(ref float pSizeX, ref float pSizeZ)
	{
		pSizeX = 0f;
		pSizeZ = 0f;
		return FnTable.GetPlayAreaSize(ref pSizeX, ref pSizeZ);
	}

	public bool GetPlayAreaRect(ref HmdQuad_t rect)
	{
		return FnTable.GetPlayAreaRect(ref rect);
	}

	public void ReloadInfo()
	{
		FnTable.ReloadInfo();
	}

	public void SetSceneColor(HmdColor_t color)
	{
		FnTable.SetSceneColor(color);
	}

	public void GetBoundsColor(ref HmdColor_t pOutputColorArray, int nNumOutputColors, float flCollisionBoundsFadeDistance, ref HmdColor_t pOutputCameraColor)
	{
		FnTable.GetBoundsColor(ref pOutputColorArray, nNumOutputColors, flCollisionBoundsFadeDistance, ref pOutputCameraColor);
	}

	public bool AreBoundsVisible()
	{
		return FnTable.AreBoundsVisible();
	}

	public void ForceBoundsVisible(bool bForce)
	{
		FnTable.ForceBoundsVisible(bForce);
	}
}

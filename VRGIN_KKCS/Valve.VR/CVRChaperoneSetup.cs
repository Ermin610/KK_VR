using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Valve.VR;

public class CVRChaperoneSetup
{
	private IVRChaperoneSetup FnTable;

	internal CVRChaperoneSetup(IntPtr pInterface)
	{
		FnTable = (IVRChaperoneSetup)Marshal.PtrToStructure(pInterface, typeof(IVRChaperoneSetup));
	}

	public bool CommitWorkingCopy(EChaperoneConfigFile configFile)
	{
		return FnTable.CommitWorkingCopy(configFile);
	}

	public void RevertWorkingCopy()
	{
		FnTable.RevertWorkingCopy();
	}

	public bool GetWorkingPlayAreaSize(ref float pSizeX, ref float pSizeZ)
	{
		pSizeX = 0f;
		pSizeZ = 0f;
		return FnTable.GetWorkingPlayAreaSize(ref pSizeX, ref pSizeZ);
	}

	public bool GetWorkingPlayAreaRect(ref HmdQuad_t rect)
	{
		return FnTable.GetWorkingPlayAreaRect(ref rect);
	}

	public bool GetWorkingCollisionBoundsInfo(out HmdQuad_t[] pQuadsBuffer)
	{
		uint punQuadsCount = 0u;
		FnTable.GetWorkingCollisionBoundsInfo(null, ref punQuadsCount);
		pQuadsBuffer = new HmdQuad_t[punQuadsCount];
		return FnTable.GetWorkingCollisionBoundsInfo(pQuadsBuffer, ref punQuadsCount);
	}

	public bool GetLiveCollisionBoundsInfo(out HmdQuad_t[] pQuadsBuffer)
	{
		uint punQuadsCount = 0u;
		FnTable.GetLiveCollisionBoundsInfo(null, ref punQuadsCount);
		pQuadsBuffer = new HmdQuad_t[punQuadsCount];
		return FnTable.GetLiveCollisionBoundsInfo(pQuadsBuffer, ref punQuadsCount);
	}

	public bool GetWorkingSeatedZeroPoseToRawTrackingPose(ref HmdMatrix34_t pmatSeatedZeroPoseToRawTrackingPose)
	{
		return FnTable.GetWorkingSeatedZeroPoseToRawTrackingPose(ref pmatSeatedZeroPoseToRawTrackingPose);
	}

	public bool GetWorkingStandingZeroPoseToRawTrackingPose(ref HmdMatrix34_t pmatStandingZeroPoseToRawTrackingPose)
	{
		return FnTable.GetWorkingStandingZeroPoseToRawTrackingPose(ref pmatStandingZeroPoseToRawTrackingPose);
	}

	public void SetWorkingPlayAreaSize(float sizeX, float sizeZ)
	{
		FnTable.SetWorkingPlayAreaSize(sizeX, sizeZ);
	}

	public void SetWorkingCollisionBoundsInfo(HmdQuad_t[] pQuadsBuffer)
	{
		FnTable.SetWorkingCollisionBoundsInfo(pQuadsBuffer, (uint)pQuadsBuffer.Length);
	}

	public void SetWorkingSeatedZeroPoseToRawTrackingPose(ref HmdMatrix34_t pMatSeatedZeroPoseToRawTrackingPose)
	{
		FnTable.SetWorkingSeatedZeroPoseToRawTrackingPose(ref pMatSeatedZeroPoseToRawTrackingPose);
	}

	public void SetWorkingStandingZeroPoseToRawTrackingPose(ref HmdMatrix34_t pMatStandingZeroPoseToRawTrackingPose)
	{
		FnTable.SetWorkingStandingZeroPoseToRawTrackingPose(ref pMatStandingZeroPoseToRawTrackingPose);
	}

	public void ReloadFromDisk(EChaperoneConfigFile configFile)
	{
		FnTable.ReloadFromDisk(configFile);
	}

	public bool GetLiveSeatedZeroPoseToRawTrackingPose(ref HmdMatrix34_t pmatSeatedZeroPoseToRawTrackingPose)
	{
		return FnTable.GetLiveSeatedZeroPoseToRawTrackingPose(ref pmatSeatedZeroPoseToRawTrackingPose);
	}

	public void SetWorkingCollisionBoundsTagsInfo(byte[] pTagsBuffer)
	{
		FnTable.SetWorkingCollisionBoundsTagsInfo(pTagsBuffer, (uint)pTagsBuffer.Length);
	}

	public bool GetLiveCollisionBoundsTagsInfo(out byte[] pTagsBuffer)
	{
		uint punTagCount = 0u;
		FnTable.GetLiveCollisionBoundsTagsInfo(null, ref punTagCount);
		pTagsBuffer = new byte[punTagCount];
		return FnTable.GetLiveCollisionBoundsTagsInfo(pTagsBuffer, ref punTagCount);
	}

	public bool SetWorkingPhysicalBoundsInfo(HmdQuad_t[] pQuadsBuffer)
	{
		return FnTable.SetWorkingPhysicalBoundsInfo(pQuadsBuffer, (uint)pQuadsBuffer.Length);
	}

	public bool GetLivePhysicalBoundsInfo(out HmdQuad_t[] pQuadsBuffer)
	{
		uint punQuadsCount = 0u;
		FnTable.GetLivePhysicalBoundsInfo(null, ref punQuadsCount);
		pQuadsBuffer = new HmdQuad_t[punQuadsCount];
		return FnTable.GetLivePhysicalBoundsInfo(pQuadsBuffer, ref punQuadsCount);
	}

	public bool ExportLiveToBuffer(StringBuilder pBuffer, ref uint pnBufferLength)
	{
		pnBufferLength = 0u;
		return FnTable.ExportLiveToBuffer(pBuffer, ref pnBufferLength);
	}

	public bool ImportFromBufferToWorking(string pBuffer, uint nImportFlags)
	{
		return FnTable.ImportFromBufferToWorking(pBuffer, nImportFlags);
	}
}

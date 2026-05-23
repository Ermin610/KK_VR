using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Valve.VR;

public class CVRSystem
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate bool _PollNextEventPacked(ref VREvent_t_Packed pEvent, uint uncbVREvent);

	[StructLayout(LayoutKind.Explicit)]
	private struct PollNextEventUnion
	{
		[FieldOffset(0)]
		public IVRSystem._PollNextEvent pPollNextEvent;

		[FieldOffset(0)]
		public _PollNextEventPacked pPollNextEventPacked;
	}

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate bool _GetControllerStatePacked(uint unControllerDeviceIndex, ref VRControllerState_t_Packed pControllerState, uint unControllerStateSize);

	[StructLayout(LayoutKind.Explicit)]
	private struct GetControllerStateUnion
	{
		[FieldOffset(0)]
		public IVRSystem._GetControllerState pGetControllerState;

		[FieldOffset(0)]
		public _GetControllerStatePacked pGetControllerStatePacked;
	}

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate bool _GetControllerStateWithPosePacked(ETrackingUniverseOrigin eOrigin, uint unControllerDeviceIndex, ref VRControllerState_t_Packed pControllerState, uint unControllerStateSize, ref TrackedDevicePose_t pTrackedDevicePose);

	[StructLayout(LayoutKind.Explicit)]
	private struct GetControllerStateWithPoseUnion
	{
		[FieldOffset(0)]
		public IVRSystem._GetControllerStateWithPose pGetControllerStateWithPose;

		[FieldOffset(0)]
		public _GetControllerStateWithPosePacked pGetControllerStateWithPosePacked;
	}

	private IVRSystem FnTable;

	internal CVRSystem(IntPtr pInterface)
	{
		FnTable = (IVRSystem)Marshal.PtrToStructure(pInterface, typeof(IVRSystem));
	}

	public void GetRecommendedRenderTargetSize(ref uint pnWidth, ref uint pnHeight)
	{
		pnWidth = 0u;
		pnHeight = 0u;
		FnTable.GetRecommendedRenderTargetSize(ref pnWidth, ref pnHeight);
	}

	public HmdMatrix44_t GetProjectionMatrix(EVREye eEye, float fNearZ, float fFarZ)
	{
		return FnTable.GetProjectionMatrix(eEye, fNearZ, fFarZ);
	}

	public void GetProjectionRaw(EVREye eEye, ref float pfLeft, ref float pfRight, ref float pfTop, ref float pfBottom)
	{
		pfLeft = 0f;
		pfRight = 0f;
		pfTop = 0f;
		pfBottom = 0f;
		FnTable.GetProjectionRaw(eEye, ref pfLeft, ref pfRight, ref pfTop, ref pfBottom);
	}

	public bool ComputeDistortion(EVREye eEye, float fU, float fV, ref DistortionCoordinates_t pDistortionCoordinates)
	{
		return FnTable.ComputeDistortion(eEye, fU, fV, ref pDistortionCoordinates);
	}

	public HmdMatrix34_t GetEyeToHeadTransform(EVREye eEye)
	{
		return FnTable.GetEyeToHeadTransform(eEye);
	}

	public bool GetTimeSinceLastVsync(ref float pfSecondsSinceLastVsync, ref ulong pulFrameCounter)
	{
		pfSecondsSinceLastVsync = 0f;
		pulFrameCounter = 0uL;
		return FnTable.GetTimeSinceLastVsync(ref pfSecondsSinceLastVsync, ref pulFrameCounter);
	}

	public int GetD3D9AdapterIndex()
	{
		return FnTable.GetD3D9AdapterIndex();
	}

	public void GetDXGIOutputInfo(ref int pnAdapterIndex)
	{
		pnAdapterIndex = 0;
		FnTable.GetDXGIOutputInfo(ref pnAdapterIndex);
	}

	public void GetOutputDevice(ref ulong pnDevice, ETextureType textureType, IntPtr pInstance)
	{
		pnDevice = 0uL;
		FnTable.GetOutputDevice(ref pnDevice, textureType, pInstance);
	}

	public bool IsDisplayOnDesktop()
	{
		return FnTable.IsDisplayOnDesktop();
	}

	public bool SetDisplayVisibility(bool bIsVisibleOnDesktop)
	{
		return FnTable.SetDisplayVisibility(bIsVisibleOnDesktop);
	}

	public void GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin eOrigin, float fPredictedSecondsToPhotonsFromNow, TrackedDevicePose_t[] pTrackedDevicePoseArray)
	{
		FnTable.GetDeviceToAbsoluteTrackingPose(eOrigin, fPredictedSecondsToPhotonsFromNow, pTrackedDevicePoseArray, (uint)pTrackedDevicePoseArray.Length);
	}

	public void ResetSeatedZeroPose()
	{
		FnTable.ResetSeatedZeroPose();
	}

	public HmdMatrix34_t GetSeatedZeroPoseToStandingAbsoluteTrackingPose()
	{
		return FnTable.GetSeatedZeroPoseToStandingAbsoluteTrackingPose();
	}

	public HmdMatrix34_t GetRawZeroPoseToStandingAbsoluteTrackingPose()
	{
		return FnTable.GetRawZeroPoseToStandingAbsoluteTrackingPose();
	}

	public uint GetSortedTrackedDeviceIndicesOfClass(ETrackedDeviceClass eTrackedDeviceClass, uint[] punTrackedDeviceIndexArray, uint unRelativeToTrackedDeviceIndex)
	{
		return FnTable.GetSortedTrackedDeviceIndicesOfClass(eTrackedDeviceClass, punTrackedDeviceIndexArray, (uint)punTrackedDeviceIndexArray.Length, unRelativeToTrackedDeviceIndex);
	}

	public EDeviceActivityLevel GetTrackedDeviceActivityLevel(uint unDeviceId)
	{
		return FnTable.GetTrackedDeviceActivityLevel(unDeviceId);
	}

	public void ApplyTransform(ref TrackedDevicePose_t pOutputPose, ref TrackedDevicePose_t pTrackedDevicePose, ref HmdMatrix34_t pTransform)
	{
		FnTable.ApplyTransform(ref pOutputPose, ref pTrackedDevicePose, ref pTransform);
	}

	public uint GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole unDeviceType)
	{
		return FnTable.GetTrackedDeviceIndexForControllerRole(unDeviceType);
	}

	public ETrackedControllerRole GetControllerRoleForTrackedDeviceIndex(uint unDeviceIndex)
	{
		return FnTable.GetControllerRoleForTrackedDeviceIndex(unDeviceIndex);
	}

	public ETrackedDeviceClass GetTrackedDeviceClass(uint unDeviceIndex)
	{
		return FnTable.GetTrackedDeviceClass(unDeviceIndex);
	}

	public bool IsTrackedDeviceConnected(uint unDeviceIndex)
	{
		return FnTable.IsTrackedDeviceConnected(unDeviceIndex);
	}

	public bool GetBoolTrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop, ref ETrackedPropertyError pError)
	{
		return FnTable.GetBoolTrackedDeviceProperty(unDeviceIndex, prop, ref pError);
	}

	public float GetFloatTrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop, ref ETrackedPropertyError pError)
	{
		return FnTable.GetFloatTrackedDeviceProperty(unDeviceIndex, prop, ref pError);
	}

	public int GetInt32TrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop, ref ETrackedPropertyError pError)
	{
		return FnTable.GetInt32TrackedDeviceProperty(unDeviceIndex, prop, ref pError);
	}

	public ulong GetUint64TrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop, ref ETrackedPropertyError pError)
	{
		return FnTable.GetUint64TrackedDeviceProperty(unDeviceIndex, prop, ref pError);
	}

	public HmdMatrix34_t GetMatrix34TrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop, ref ETrackedPropertyError pError)
	{
		return FnTable.GetMatrix34TrackedDeviceProperty(unDeviceIndex, prop, ref pError);
	}

	public uint GetArrayTrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop, uint propType, IntPtr pBuffer, uint unBufferSize, ref ETrackedPropertyError pError)
	{
		return FnTable.GetArrayTrackedDeviceProperty(unDeviceIndex, prop, propType, pBuffer, unBufferSize, ref pError);
	}

	public uint GetStringTrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop, StringBuilder pchValue, uint unBufferSize, ref ETrackedPropertyError pError)
	{
		return FnTable.GetStringTrackedDeviceProperty(unDeviceIndex, prop, pchValue, unBufferSize, ref pError);
	}

	public string GetPropErrorNameFromEnum(ETrackedPropertyError error)
	{
		return Marshal.PtrToStringAnsi(FnTable.GetPropErrorNameFromEnum(error));
	}

	public bool PollNextEvent(ref VREvent_t pEvent, uint uncbVREvent)
	{
		if (Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix)
		{
			VREvent_t_Packed pEvent2 = default(VREvent_t_Packed);
			PollNextEventUnion pollNextEventUnion = default(PollNextEventUnion);
			pollNextEventUnion.pPollNextEventPacked = null;
			pollNextEventUnion.pPollNextEvent = FnTable.PollNextEvent;
			bool result = pollNextEventUnion.pPollNextEventPacked(ref pEvent2, (uint)Marshal.SizeOf(typeof(VREvent_t_Packed)));
			pEvent2.Unpack(ref pEvent);
			return result;
		}
		return FnTable.PollNextEvent(ref pEvent, uncbVREvent);
	}

	public bool PollNextEventWithPose(ETrackingUniverseOrigin eOrigin, ref VREvent_t pEvent, uint uncbVREvent, ref TrackedDevicePose_t pTrackedDevicePose)
	{
		return FnTable.PollNextEventWithPose(eOrigin, ref pEvent, uncbVREvent, ref pTrackedDevicePose);
	}

	public string GetEventTypeNameFromEnum(EVREventType eType)
	{
		return Marshal.PtrToStringAnsi(FnTable.GetEventTypeNameFromEnum(eType));
	}

	public HiddenAreaMesh_t GetHiddenAreaMesh(EVREye eEye, EHiddenAreaMeshType type)
	{
		return FnTable.GetHiddenAreaMesh(eEye, type);
	}

	public bool GetControllerState(uint unControllerDeviceIndex, ref VRControllerState_t pControllerState, uint unControllerStateSize)
	{
		if (Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix)
		{
			VRControllerState_t_Packed pControllerState2 = new VRControllerState_t_Packed(pControllerState);
			GetControllerStateUnion getControllerStateUnion = default(GetControllerStateUnion);
			getControllerStateUnion.pGetControllerStatePacked = null;
			getControllerStateUnion.pGetControllerState = FnTable.GetControllerState;
			bool result = getControllerStateUnion.pGetControllerStatePacked(unControllerDeviceIndex, ref pControllerState2, (uint)Marshal.SizeOf(typeof(VRControllerState_t_Packed)));
			pControllerState2.Unpack(ref pControllerState);
			return result;
		}
		return FnTable.GetControllerState(unControllerDeviceIndex, ref pControllerState, unControllerStateSize);
	}

	public bool GetControllerStateWithPose(ETrackingUniverseOrigin eOrigin, uint unControllerDeviceIndex, ref VRControllerState_t pControllerState, uint unControllerStateSize, ref TrackedDevicePose_t pTrackedDevicePose)
	{
		if (Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix)
		{
			VRControllerState_t_Packed pControllerState2 = new VRControllerState_t_Packed(pControllerState);
			GetControllerStateWithPoseUnion getControllerStateWithPoseUnion = default(GetControllerStateWithPoseUnion);
			getControllerStateWithPoseUnion.pGetControllerStateWithPosePacked = null;
			getControllerStateWithPoseUnion.pGetControllerStateWithPose = FnTable.GetControllerStateWithPose;
			bool result = getControllerStateWithPoseUnion.pGetControllerStateWithPosePacked(eOrigin, unControllerDeviceIndex, ref pControllerState2, (uint)Marshal.SizeOf(typeof(VRControllerState_t_Packed)), ref pTrackedDevicePose);
			pControllerState2.Unpack(ref pControllerState);
			return result;
		}
		return FnTable.GetControllerStateWithPose(eOrigin, unControllerDeviceIndex, ref pControllerState, unControllerStateSize, ref pTrackedDevicePose);
	}

	public void TriggerHapticPulse(uint unControllerDeviceIndex, uint unAxisId, char usDurationMicroSec)
	{
		FnTable.TriggerHapticPulse(unControllerDeviceIndex, unAxisId, usDurationMicroSec);
	}

	public string GetButtonIdNameFromEnum(EVRButtonId eButtonId)
	{
		return Marshal.PtrToStringAnsi(FnTable.GetButtonIdNameFromEnum(eButtonId));
	}

	public string GetControllerAxisTypeNameFromEnum(EVRControllerAxisType eAxisType)
	{
		return Marshal.PtrToStringAnsi(FnTable.GetControllerAxisTypeNameFromEnum(eAxisType));
	}

	public bool IsInputAvailable()
	{
		return FnTable.IsInputAvailable();
	}

	public bool IsSteamVRDrawingControllers()
	{
		return FnTable.IsSteamVRDrawingControllers();
	}

	public bool ShouldApplicationPause()
	{
		return FnTable.ShouldApplicationPause();
	}

	public bool ShouldApplicationReduceRenderingWork()
	{
		return FnTable.ShouldApplicationReduceRenderingWork();
	}

	public uint DriverDebugRequest(uint unDeviceIndex, string pchRequest, StringBuilder pchResponseBuffer, uint unResponseBufferSize)
	{
		return FnTable.DriverDebugRequest(unDeviceIndex, pchRequest, pchResponseBuffer, unResponseBufferSize);
	}

	public EVRFirmwareError PerformFirmwareUpdate(uint unDeviceIndex)
	{
		return FnTable.PerformFirmwareUpdate(unDeviceIndex);
	}

	public void AcknowledgeQuit_Exiting()
	{
		FnTable.AcknowledgeQuit_Exiting();
	}

	public void AcknowledgeQuit_UserPrompt()
	{
		FnTable.AcknowledgeQuit_UserPrompt();
	}
}

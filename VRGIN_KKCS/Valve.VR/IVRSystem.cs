using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Valve.VR;

public struct IVRSystem
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _GetRecommendedRenderTargetSize(ref uint pnWidth, ref uint pnHeight);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate HmdMatrix44_t _GetProjectionMatrix(EVREye eEye, float fNearZ, float fFarZ);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _GetProjectionRaw(EVREye eEye, ref float pfLeft, ref float pfRight, ref float pfTop, ref float pfBottom);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate bool _ComputeDistortion(EVREye eEye, float fU, float fV, ref DistortionCoordinates_t pDistortionCoordinates);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate HmdMatrix34_t _GetEyeToHeadTransform(EVREye eEye);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate bool _GetTimeSinceLastVsync(ref float pfSecondsSinceLastVsync, ref ulong pulFrameCounter);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate int _GetD3D9AdapterIndex();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _GetDXGIOutputInfo(ref int pnAdapterIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _GetOutputDevice(ref ulong pnDevice, ETextureType textureType, IntPtr pInstance);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate bool _IsDisplayOnDesktop();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate bool _SetDisplayVisibility(bool bIsVisibleOnDesktop);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin eOrigin, float fPredictedSecondsToPhotonsFromNow, [In][Out] TrackedDevicePose_t[] pTrackedDevicePoseArray, uint unTrackedDevicePoseArrayCount);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _ResetSeatedZeroPose();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate HmdMatrix34_t _GetSeatedZeroPoseToStandingAbsoluteTrackingPose();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate HmdMatrix34_t _GetRawZeroPoseToStandingAbsoluteTrackingPose();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate uint _GetSortedTrackedDeviceIndicesOfClass(ETrackedDeviceClass eTrackedDeviceClass, [In][Out] uint[] punTrackedDeviceIndexArray, uint unTrackedDeviceIndexArrayCount, uint unRelativeToTrackedDeviceIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate EDeviceActivityLevel _GetTrackedDeviceActivityLevel(uint unDeviceId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _ApplyTransform(ref TrackedDevicePose_t pOutputPose, ref TrackedDevicePose_t pTrackedDevicePose, ref HmdMatrix34_t pTransform);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate uint _GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole unDeviceType);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate ETrackedControllerRole _GetControllerRoleForTrackedDeviceIndex(uint unDeviceIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate ETrackedDeviceClass _GetTrackedDeviceClass(uint unDeviceIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate bool _IsTrackedDeviceConnected(uint unDeviceIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate bool _GetBoolTrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop, ref ETrackedPropertyError pError);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate float _GetFloatTrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop, ref ETrackedPropertyError pError);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate int _GetInt32TrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop, ref ETrackedPropertyError pError);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate ulong _GetUint64TrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop, ref ETrackedPropertyError pError);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate HmdMatrix34_t _GetMatrix34TrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop, ref ETrackedPropertyError pError);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate uint _GetArrayTrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop, uint propType, IntPtr pBuffer, uint unBufferSize, ref ETrackedPropertyError pError);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate uint _GetStringTrackedDeviceProperty(uint unDeviceIndex, ETrackedDeviceProperty prop, StringBuilder pchValue, uint unBufferSize, ref ETrackedPropertyError pError);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate IntPtr _GetPropErrorNameFromEnum(ETrackedPropertyError error);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate bool _PollNextEvent(ref VREvent_t pEvent, uint uncbVREvent);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate bool _PollNextEventWithPose(ETrackingUniverseOrigin eOrigin, ref VREvent_t pEvent, uint uncbVREvent, ref TrackedDevicePose_t pTrackedDevicePose);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate IntPtr _GetEventTypeNameFromEnum(EVREventType eType);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate HiddenAreaMesh_t _GetHiddenAreaMesh(EVREye eEye, EHiddenAreaMeshType type);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate bool _GetControllerState(uint unControllerDeviceIndex, ref VRControllerState_t pControllerState, uint unControllerStateSize);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate bool _GetControllerStateWithPose(ETrackingUniverseOrigin eOrigin, uint unControllerDeviceIndex, ref VRControllerState_t pControllerState, uint unControllerStateSize, ref TrackedDevicePose_t pTrackedDevicePose);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _TriggerHapticPulse(uint unControllerDeviceIndex, uint unAxisId, char usDurationMicroSec);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate IntPtr _GetButtonIdNameFromEnum(EVRButtonId eButtonId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate IntPtr _GetControllerAxisTypeNameFromEnum(EVRControllerAxisType eAxisType);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate bool _IsInputAvailable();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate bool _IsSteamVRDrawingControllers();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate bool _ShouldApplicationPause();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate bool _ShouldApplicationReduceRenderingWork();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate uint _DriverDebugRequest(uint unDeviceIndex, string pchRequest, StringBuilder pchResponseBuffer, uint unResponseBufferSize);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate EVRFirmwareError _PerformFirmwareUpdate(uint unDeviceIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _AcknowledgeQuit_Exiting();

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void _AcknowledgeQuit_UserPrompt();

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetRecommendedRenderTargetSize GetRecommendedRenderTargetSize;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetProjectionMatrix GetProjectionMatrix;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetProjectionRaw GetProjectionRaw;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _ComputeDistortion ComputeDistortion;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetEyeToHeadTransform GetEyeToHeadTransform;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetTimeSinceLastVsync GetTimeSinceLastVsync;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetD3D9AdapterIndex GetD3D9AdapterIndex;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetDXGIOutputInfo GetDXGIOutputInfo;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetOutputDevice GetOutputDevice;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _IsDisplayOnDesktop IsDisplayOnDesktop;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _SetDisplayVisibility SetDisplayVisibility;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetDeviceToAbsoluteTrackingPose GetDeviceToAbsoluteTrackingPose;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _ResetSeatedZeroPose ResetSeatedZeroPose;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetSeatedZeroPoseToStandingAbsoluteTrackingPose GetSeatedZeroPoseToStandingAbsoluteTrackingPose;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetRawZeroPoseToStandingAbsoluteTrackingPose GetRawZeroPoseToStandingAbsoluteTrackingPose;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetSortedTrackedDeviceIndicesOfClass GetSortedTrackedDeviceIndicesOfClass;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetTrackedDeviceActivityLevel GetTrackedDeviceActivityLevel;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _ApplyTransform ApplyTransform;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetTrackedDeviceIndexForControllerRole GetTrackedDeviceIndexForControllerRole;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetControllerRoleForTrackedDeviceIndex GetControllerRoleForTrackedDeviceIndex;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetTrackedDeviceClass GetTrackedDeviceClass;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _IsTrackedDeviceConnected IsTrackedDeviceConnected;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetBoolTrackedDeviceProperty GetBoolTrackedDeviceProperty;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetFloatTrackedDeviceProperty GetFloatTrackedDeviceProperty;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetInt32TrackedDeviceProperty GetInt32TrackedDeviceProperty;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetUint64TrackedDeviceProperty GetUint64TrackedDeviceProperty;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetMatrix34TrackedDeviceProperty GetMatrix34TrackedDeviceProperty;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetArrayTrackedDeviceProperty GetArrayTrackedDeviceProperty;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetStringTrackedDeviceProperty GetStringTrackedDeviceProperty;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetPropErrorNameFromEnum GetPropErrorNameFromEnum;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _PollNextEvent PollNextEvent;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _PollNextEventWithPose PollNextEventWithPose;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetEventTypeNameFromEnum GetEventTypeNameFromEnum;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetHiddenAreaMesh GetHiddenAreaMesh;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetControllerState GetControllerState;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetControllerStateWithPose GetControllerStateWithPose;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _TriggerHapticPulse TriggerHapticPulse;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetButtonIdNameFromEnum GetButtonIdNameFromEnum;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _GetControllerAxisTypeNameFromEnum GetControllerAxisTypeNameFromEnum;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _IsInputAvailable IsInputAvailable;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _IsSteamVRDrawingControllers IsSteamVRDrawingControllers;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _ShouldApplicationPause ShouldApplicationPause;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _ShouldApplicationReduceRenderingWork ShouldApplicationReduceRenderingWork;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _DriverDebugRequest DriverDebugRequest;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _PerformFirmwareUpdate PerformFirmwareUpdate;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _AcknowledgeQuit_Exiting AcknowledgeQuit_Exiting;

	[MarshalAs(UnmanagedType.FunctionPtr)]
	internal _AcknowledgeQuit_UserPrompt AcknowledgeQuit_UserPrompt;
}

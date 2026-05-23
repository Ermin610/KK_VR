using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Valve.VR;

public class CVRApplications
{
	private IVRApplications FnTable;

	internal CVRApplications(IntPtr pInterface)
	{
		FnTable = (IVRApplications)Marshal.PtrToStructure(pInterface, typeof(IVRApplications));
	}

	public EVRApplicationError AddApplicationManifest(string pchApplicationManifestFullPath, bool bTemporary)
	{
		return FnTable.AddApplicationManifest(pchApplicationManifestFullPath, bTemporary);
	}

	public EVRApplicationError RemoveApplicationManifest(string pchApplicationManifestFullPath)
	{
		return FnTable.RemoveApplicationManifest(pchApplicationManifestFullPath);
	}

	public bool IsApplicationInstalled(string pchAppKey)
	{
		return FnTable.IsApplicationInstalled(pchAppKey);
	}

	public uint GetApplicationCount()
	{
		return FnTable.GetApplicationCount();
	}

	public EVRApplicationError GetApplicationKeyByIndex(uint unApplicationIndex, StringBuilder pchAppKeyBuffer, uint unAppKeyBufferLen)
	{
		return FnTable.GetApplicationKeyByIndex(unApplicationIndex, pchAppKeyBuffer, unAppKeyBufferLen);
	}

	public EVRApplicationError GetApplicationKeyByProcessId(uint unProcessId, StringBuilder pchAppKeyBuffer, uint unAppKeyBufferLen)
	{
		return FnTable.GetApplicationKeyByProcessId(unProcessId, pchAppKeyBuffer, unAppKeyBufferLen);
	}

	public EVRApplicationError LaunchApplication(string pchAppKey)
	{
		return FnTable.LaunchApplication(pchAppKey);
	}

	public EVRApplicationError LaunchTemplateApplication(string pchTemplateAppKey, string pchNewAppKey, AppOverrideKeys_t[] pKeys)
	{
		return FnTable.LaunchTemplateApplication(pchTemplateAppKey, pchNewAppKey, pKeys, (uint)pKeys.Length);
	}

	public EVRApplicationError LaunchApplicationFromMimeType(string pchMimeType, string pchArgs)
	{
		return FnTable.LaunchApplicationFromMimeType(pchMimeType, pchArgs);
	}

	public EVRApplicationError LaunchDashboardOverlay(string pchAppKey)
	{
		return FnTable.LaunchDashboardOverlay(pchAppKey);
	}

	public bool CancelApplicationLaunch(string pchAppKey)
	{
		return FnTable.CancelApplicationLaunch(pchAppKey);
	}

	public EVRApplicationError IdentifyApplication(uint unProcessId, string pchAppKey)
	{
		return FnTable.IdentifyApplication(unProcessId, pchAppKey);
	}

	public uint GetApplicationProcessId(string pchAppKey)
	{
		return FnTable.GetApplicationProcessId(pchAppKey);
	}

	public string GetApplicationsErrorNameFromEnum(EVRApplicationError error)
	{
		return Marshal.PtrToStringAnsi(FnTable.GetApplicationsErrorNameFromEnum(error));
	}

	public uint GetApplicationPropertyString(string pchAppKey, EVRApplicationProperty eProperty, StringBuilder pchPropertyValueBuffer, uint unPropertyValueBufferLen, ref EVRApplicationError peError)
	{
		return FnTable.GetApplicationPropertyString(pchAppKey, eProperty, pchPropertyValueBuffer, unPropertyValueBufferLen, ref peError);
	}

	public bool GetApplicationPropertyBool(string pchAppKey, EVRApplicationProperty eProperty, ref EVRApplicationError peError)
	{
		return FnTable.GetApplicationPropertyBool(pchAppKey, eProperty, ref peError);
	}

	public ulong GetApplicationPropertyUint64(string pchAppKey, EVRApplicationProperty eProperty, ref EVRApplicationError peError)
	{
		return FnTable.GetApplicationPropertyUint64(pchAppKey, eProperty, ref peError);
	}

	public EVRApplicationError SetApplicationAutoLaunch(string pchAppKey, bool bAutoLaunch)
	{
		return FnTable.SetApplicationAutoLaunch(pchAppKey, bAutoLaunch);
	}

	public bool GetApplicationAutoLaunch(string pchAppKey)
	{
		return FnTable.GetApplicationAutoLaunch(pchAppKey);
	}

	public EVRApplicationError SetDefaultApplicationForMimeType(string pchAppKey, string pchMimeType)
	{
		return FnTable.SetDefaultApplicationForMimeType(pchAppKey, pchMimeType);
	}

	public bool GetDefaultApplicationForMimeType(string pchMimeType, StringBuilder pchAppKeyBuffer, uint unAppKeyBufferLen)
	{
		return FnTable.GetDefaultApplicationForMimeType(pchMimeType, pchAppKeyBuffer, unAppKeyBufferLen);
	}

	public bool GetApplicationSupportedMimeTypes(string pchAppKey, StringBuilder pchMimeTypesBuffer, uint unMimeTypesBuffer)
	{
		return FnTable.GetApplicationSupportedMimeTypes(pchAppKey, pchMimeTypesBuffer, unMimeTypesBuffer);
	}

	public uint GetApplicationsThatSupportMimeType(string pchMimeType, StringBuilder pchAppKeysThatSupportBuffer, uint unAppKeysThatSupportBuffer)
	{
		return FnTable.GetApplicationsThatSupportMimeType(pchMimeType, pchAppKeysThatSupportBuffer, unAppKeysThatSupportBuffer);
	}

	public uint GetApplicationLaunchArguments(uint unHandle, StringBuilder pchArgs, uint unArgs)
	{
		return FnTable.GetApplicationLaunchArguments(unHandle, pchArgs, unArgs);
	}

	public EVRApplicationError GetStartingApplication(StringBuilder pchAppKeyBuffer, uint unAppKeyBufferLen)
	{
		return FnTable.GetStartingApplication(pchAppKeyBuffer, unAppKeyBufferLen);
	}

	public EVRApplicationTransitionState GetTransitionState()
	{
		return FnTable.GetTransitionState();
	}

	public EVRApplicationError PerformApplicationPrelaunchCheck(string pchAppKey)
	{
		return FnTable.PerformApplicationPrelaunchCheck(pchAppKey);
	}

	public string GetApplicationsTransitionStateNameFromEnum(EVRApplicationTransitionState state)
	{
		return Marshal.PtrToStringAnsi(FnTable.GetApplicationsTransitionStateNameFromEnum(state));
	}

	public bool IsQuitUserPromptRequested()
	{
		return FnTable.IsQuitUserPromptRequested();
	}

	public EVRApplicationError LaunchInternalProcess(string pchBinaryPath, string pchArguments, string pchWorkingDirectory)
	{
		return FnTable.LaunchInternalProcess(pchBinaryPath, pchArguments, pchWorkingDirectory);
	}

	public uint GetCurrentSceneProcessId()
	{
		return FnTable.GetCurrentSceneProcessId();
	}
}

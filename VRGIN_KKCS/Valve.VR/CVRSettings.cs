using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Valve.VR;

public class CVRSettings
{
	private IVRSettings FnTable;

	internal CVRSettings(IntPtr pInterface)
	{
		FnTable = (IVRSettings)Marshal.PtrToStructure(pInterface, typeof(IVRSettings));
	}

	public string GetSettingsErrorNameFromEnum(EVRSettingsError eError)
	{
		return Marshal.PtrToStringAnsi(FnTable.GetSettingsErrorNameFromEnum(eError));
	}

	public bool Sync(bool bForce, ref EVRSettingsError peError)
	{
		return FnTable.Sync(bForce, ref peError);
	}

	public void SetBool(string pchSection, string pchSettingsKey, bool bValue, ref EVRSettingsError peError)
	{
		FnTable.SetBool(pchSection, pchSettingsKey, bValue, ref peError);
	}

	public void SetInt32(string pchSection, string pchSettingsKey, int nValue, ref EVRSettingsError peError)
	{
		FnTable.SetInt32(pchSection, pchSettingsKey, nValue, ref peError);
	}

	public void SetFloat(string pchSection, string pchSettingsKey, float flValue, ref EVRSettingsError peError)
	{
		FnTable.SetFloat(pchSection, pchSettingsKey, flValue, ref peError);
	}

	public void SetString(string pchSection, string pchSettingsKey, string pchValue, ref EVRSettingsError peError)
	{
		FnTable.SetString(pchSection, pchSettingsKey, pchValue, ref peError);
	}

	public bool GetBool(string pchSection, string pchSettingsKey, ref EVRSettingsError peError)
	{
		return FnTable.GetBool(pchSection, pchSettingsKey, ref peError);
	}

	public int GetInt32(string pchSection, string pchSettingsKey, ref EVRSettingsError peError)
	{
		return FnTable.GetInt32(pchSection, pchSettingsKey, ref peError);
	}

	public float GetFloat(string pchSection, string pchSettingsKey, ref EVRSettingsError peError)
	{
		return FnTable.GetFloat(pchSection, pchSettingsKey, ref peError);
	}

	public void GetString(string pchSection, string pchSettingsKey, StringBuilder pchValue, uint unValueLen, ref EVRSettingsError peError)
	{
		FnTable.GetString(pchSection, pchSettingsKey, pchValue, unValueLen, ref peError);
	}

	public void RemoveSection(string pchSection, ref EVRSettingsError peError)
	{
		FnTable.RemoveSection(pchSection, ref peError);
	}

	public void RemoveKeyInSection(string pchSection, string pchSettingsKey, ref EVRSettingsError peError)
	{
		FnTable.RemoveKeyInSection(pchSection, pchSettingsKey, ref peError);
	}
}

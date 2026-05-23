using System;
using UnityEngine;
using Valve.VR;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class SteamVR_Frustum : MonoBehaviour
{
	public SteamVR_TrackedObject.EIndex index;

	public float fovLeft = 45f;

	public float fovRight = 45f;

	public float fovTop = 45f;

	public float fovBottom = 45f;

	public float nearZ = 0.5f;

	public float farZ = 2.5f;

	public void UpdateModel()
	{
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_027f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0301: Unknown result type (might be due to invalid IL or missing references)
		//IL_0306: Unknown result type (might be due to invalid IL or missing references)
		//IL_030b: Unknown result type (might be due to invalid IL or missing references)
		//IL_030f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_031e: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_032d: Unknown result type (might be due to invalid IL or missing references)
		//IL_033a: Unknown result type (might be due to invalid IL or missing references)
		//IL_033c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0347: Unknown result type (might be due to invalid IL or missing references)
		//IL_0349: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_0358: Unknown result type (might be due to invalid IL or missing references)
		//IL_0365: Unknown result type (might be due to invalid IL or missing references)
		//IL_0367: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b4: Expected O, but got Unknown
		fovLeft = Mathf.Clamp(fovLeft, 1f, 89f);
		fovRight = Mathf.Clamp(fovRight, 1f, 89f);
		fovTop = Mathf.Clamp(fovTop, 1f, 89f);
		fovBottom = Mathf.Clamp(fovBottom, 1f, 89f);
		farZ = Mathf.Max(farZ, nearZ + 0.01f);
		nearZ = Mathf.Clamp(nearZ, 0.01f, farZ - 0.01f);
		float num = Mathf.Sin((0f - fovLeft) * ((float)Math.PI / 180f));
		float num2 = Mathf.Sin(fovRight * ((float)Math.PI / 180f));
		float num3 = Mathf.Sin(fovTop * ((float)Math.PI / 180f));
		float num4 = Mathf.Sin((0f - fovBottom) * ((float)Math.PI / 180f));
		float num5 = Mathf.Cos((0f - fovLeft) * ((float)Math.PI / 180f));
		float num6 = Mathf.Cos(fovRight * ((float)Math.PI / 180f));
		float num7 = Mathf.Cos(fovTop * ((float)Math.PI / 180f));
		float num8 = Mathf.Cos((0f - fovBottom) * ((float)Math.PI / 180f));
		Vector3[] array = (Vector3[])(object)new Vector3[8]
		{
			new Vector3(num * nearZ / num5, num3 * nearZ / num7, nearZ),
			new Vector3(num2 * nearZ / num6, num3 * nearZ / num7, nearZ),
			new Vector3(num2 * nearZ / num6, num4 * nearZ / num8, nearZ),
			new Vector3(num * nearZ / num5, num4 * nearZ / num8, nearZ),
			new Vector3(num * farZ / num5, num3 * farZ / num7, farZ),
			new Vector3(num2 * farZ / num6, num3 * farZ / num7, farZ),
			new Vector3(num2 * farZ / num6, num4 * farZ / num8, farZ),
			new Vector3(num * farZ / num5, num4 * farZ / num8, farZ)
		};
		int[] array2 = new int[48]
		{
			0, 4, 7, 0, 7, 3, 0, 7, 4, 0,
			3, 7, 1, 5, 6, 1, 6, 2, 1, 6,
			5, 1, 2, 6, 0, 4, 5, 0, 5, 1,
			0, 5, 4, 0, 1, 5, 2, 3, 7, 2,
			7, 6, 2, 7, 3, 2, 6, 7
		};
		int num9 = 0;
		Vector3[] array3 = (Vector3[])(object)new Vector3[array2.Length];
		Vector3[] array4 = (Vector3[])(object)new Vector3[array2.Length];
		for (int i = 0; i < array2.Length / 3; i++)
		{
			Vector3 val = array[array2[i * 3]];
			Vector3 val2 = array[array2[i * 3 + 1]];
			Vector3 val3 = array[array2[i * 3 + 2]];
			Vector3 val4 = Vector3.Cross(val2 - val, val3 - val);
			array4[i * 3 + 2] = (array4[i * 3 + 1] = (array4[i * 3] = ((Vector3)(ref val4)).normalized));
			array3[i * 3] = val;
			array3[i * 3 + 1] = val2;
			array3[i * 3 + 2] = val3;
			array2[i * 3] = num9++;
			array2[i * 3 + 1] = num9++;
			array2[i * 3 + 2] = num9++;
		}
		Mesh val5 = new Mesh();
		val5.vertices = array3;
		val5.normals = array4;
		val5.triangles = array2;
		((Component)this).GetComponent<MeshFilter>().mesh = val5;
	}

	private void OnDeviceConnected(int i, bool connected)
	{
		if (i != (int)index)
		{
			return;
		}
		((Component)this).GetComponent<MeshFilter>().mesh = null;
		if (!connected)
		{
			return;
		}
		CVRSystem system = OpenVR.System;
		if (system != null && system.GetTrackedDeviceClass((uint)i) == ETrackedDeviceClass.TrackingReference)
		{
			ETrackedPropertyError pError = ETrackedPropertyError.TrackedProp_Success;
			float floatTrackedDeviceProperty = system.GetFloatTrackedDeviceProperty((uint)i, ETrackedDeviceProperty.Prop_FieldOfViewLeftDegrees_Float, ref pError);
			if (pError == ETrackedPropertyError.TrackedProp_Success)
			{
				fovLeft = floatTrackedDeviceProperty;
			}
			floatTrackedDeviceProperty = system.GetFloatTrackedDeviceProperty((uint)i, ETrackedDeviceProperty.Prop_FieldOfViewRightDegrees_Float, ref pError);
			if (pError == ETrackedPropertyError.TrackedProp_Success)
			{
				fovRight = floatTrackedDeviceProperty;
			}
			floatTrackedDeviceProperty = system.GetFloatTrackedDeviceProperty((uint)i, ETrackedDeviceProperty.Prop_FieldOfViewTopDegrees_Float, ref pError);
			if (pError == ETrackedPropertyError.TrackedProp_Success)
			{
				fovTop = floatTrackedDeviceProperty;
			}
			floatTrackedDeviceProperty = system.GetFloatTrackedDeviceProperty((uint)i, ETrackedDeviceProperty.Prop_FieldOfViewBottomDegrees_Float, ref pError);
			if (pError == ETrackedPropertyError.TrackedProp_Success)
			{
				fovBottom = floatTrackedDeviceProperty;
			}
			floatTrackedDeviceProperty = system.GetFloatTrackedDeviceProperty((uint)i, ETrackedDeviceProperty.Prop_TrackingRangeMinimumMeters_Float, ref pError);
			if (pError == ETrackedPropertyError.TrackedProp_Success)
			{
				nearZ = floatTrackedDeviceProperty;
			}
			floatTrackedDeviceProperty = system.GetFloatTrackedDeviceProperty((uint)i, ETrackedDeviceProperty.Prop_TrackingRangeMaximumMeters_Float, ref pError);
			if (pError == ETrackedPropertyError.TrackedProp_Success)
			{
				farZ = floatTrackedDeviceProperty;
			}
			UpdateModel();
		}
	}

	private void OnEnable()
	{
		((Component)this).GetComponent<MeshFilter>().mesh = null;
		SteamVR_Events.DeviceConnected.Listen(OnDeviceConnected);
	}

	private void OnDisable()
	{
		SteamVR_Events.DeviceConnected.Remove(OnDeviceConnected);
		((Component)this).GetComponent<MeshFilter>().mesh = null;
	}
}

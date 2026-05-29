using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class SteamVR_TestController : MonoBehaviour
{
	private List<int> controllerIndices = new List<int>();

	private EVRButtonId[] buttonIds = new EVRButtonId[4]
	{
		EVRButtonId.k_EButton_ApplicationMenu,
		EVRButtonId.k_EButton_Grip,
		EVRButtonId.k_EButton_Axis0,
		EVRButtonId.k_EButton_Axis1
	};

	private EVRButtonId[] axisIds = new EVRButtonId[2]
	{
		EVRButtonId.k_EButton_Axis0,
		EVRButtonId.k_EButton_Axis1
	};

	public Transform point;

	public Transform pointer;

	private void OnDeviceConnected(int index, bool connected)
	{
		CVRSystem system = OpenVR.System;
		if (system != null && system.GetTrackedDeviceClass((uint)index) == ETrackedDeviceClass.Controller)
		{
			if (connected)
			{
				Debug.Log((object)$"Controller {index} connected.");
				PrintControllerStatus(index);
				controllerIndices.Add(index);
			}
			else
			{
				Debug.Log((object)$"Controller {index} disconnected.");
				PrintControllerStatus(index);
				controllerIndices.Remove(index);
			}
		}
	}

	private void OnEnable()
	{
		SteamVR_Events.DeviceConnected.Listen(OnDeviceConnected);
	}

	private void OnDisable()
	{
		SteamVR_Events.DeviceConnected.Remove(OnDeviceConnected);
	}

	private void PrintControllerStatus(int index)
	{
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		SteamVR_Controller.Device device = SteamVR_Controller.Input(index);
		Debug.Log((object)("index: " + device.index));
		Debug.Log((object)("connected: " + device.connected));
		Debug.Log((object)("hasTracking: " + device.hasTracking));
		Debug.Log((object)("outOfRange: " + device.outOfRange));
		Debug.Log((object)("calibrating: " + device.calibrating));
		Debug.Log((object)("uninitialized: " + device.uninitialized));
		Debug.Log((object)("pos: " + device.transform.pos));
		SteamVR_Utils.RigidTransform transform = device.transform;
		Debug.Log((object)("rot: " + transform.rot.eulerAngles));
		Debug.Log((object)("velocity: " + device.velocity));
		Debug.Log((object)("angularVelocity: " + device.angularVelocity));
		int deviceIndex = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Leftmost);
		int deviceIndex2 = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Rightmost);
		Debug.Log((object)((deviceIndex == deviceIndex2) ? "first" : ((deviceIndex == index) ? "left" : "right")));
	}

	private void Update()
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		foreach (int controllerIndex in controllerIndices)
		{
			SteamVR_Overlay instance = SteamVR_Overlay.instance;
			if ((instance != null) && (point != null) && (pointer != null))
			{
				SteamVR_Utils.RigidTransform transform = SteamVR_Controller.Input(controllerIndex).transform;
				((Component)pointer).transform.localPosition = transform.pos;
				((Component)pointer).transform.localRotation = transform.rot;
				SteamVR_Overlay.IntersectionResults results = default(SteamVR_Overlay.IntersectionResults);
				if (instance.ComputeIntersection(transform.pos, transform.rot * Vector3.forward, ref results))
				{
					((Component)point).transform.localPosition = results.point;
					((Component)point).transform.localRotation = Quaternion.LookRotation(results.normal);
				}
				continue;
			}
			EVRButtonId[] array = buttonIds;
			foreach (EVRButtonId eVRButtonId in array)
			{
				if (SteamVR_Controller.Input(controllerIndex).GetPressDown(eVRButtonId))
				{
					Debug.Log((object)string.Concat(eVRButtonId, " press down"));
				}
				if (SteamVR_Controller.Input(controllerIndex).GetPressUp(eVRButtonId))
				{
					Debug.Log((object)string.Concat(eVRButtonId, " press up"));
					if (eVRButtonId == EVRButtonId.k_EButton_Axis1)
					{
						SteamVR_Controller.Input(controllerIndex).TriggerHapticPulse(500);
						PrintControllerStatus(controllerIndex);
					}
				}
				if (SteamVR_Controller.Input(controllerIndex).GetPress(eVRButtonId))
				{
					Debug.Log((object)eVRButtonId);
				}
			}
			array = axisIds;
			foreach (EVRButtonId eVRButtonId2 in array)
			{
				if (SteamVR_Controller.Input(controllerIndex).GetTouchDown(eVRButtonId2))
				{
					Debug.Log((object)string.Concat(eVRButtonId2, " touch down"));
				}
				if (SteamVR_Controller.Input(controllerIndex).GetTouchUp(eVRButtonId2))
				{
					Debug.Log((object)string.Concat(eVRButtonId2, " touch up"));
				}
				if (SteamVR_Controller.Input(controllerIndex).GetTouch(eVRButtonId2))
				{
					Vector2 axis = SteamVR_Controller.Input(controllerIndex).GetAxis(eVRButtonId2);
					Debug.Log((object)("axis: " + axis));
				}
			}
		}
	}
}

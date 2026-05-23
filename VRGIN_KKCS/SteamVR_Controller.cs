using System.Runtime.InteropServices;
using UnityEngine;
using Valve.VR;

public class SteamVR_Controller
{
	public class ButtonMask
	{
		public const ulong System = 1uL;

		public const ulong ApplicationMenu = 2uL;

		public const ulong Grip = 4uL;

		public const ulong Axis0 = 4294967296uL;

		public const ulong Axis1 = 8589934592uL;

		public const ulong Axis2 = 17179869184uL;

		public const ulong Axis3 = 34359738368uL;

		public const ulong Axis4 = 68719476736uL;

		public const ulong Touchpad = 4294967296uL;

		public const ulong Trigger = 8589934592uL;
	}

	public class Device
	{
		private VRControllerState_t state;

		private VRControllerState_t prevState;

		private TrackedDevicePose_t pose;

		private int prevFrameCount = -1;

		public float hairTriggerDelta = 0.1f;

		private float hairTriggerLimit;

		private bool hairTriggerState;

		private bool hairTriggerPrevState;

		public uint index { get; private set; }

		public bool valid { get; private set; }

		public bool connected
		{
			get
			{
				Update();
				return pose.bDeviceIsConnected;
			}
		}

		public bool hasTracking
		{
			get
			{
				Update();
				return pose.bPoseIsValid;
			}
		}

		public bool outOfRange
		{
			get
			{
				Update();
				if (pose.eTrackingResult != ETrackingResult.Running_OutOfRange)
				{
					return pose.eTrackingResult == ETrackingResult.Calibrating_OutOfRange;
				}
				return true;
			}
		}

		public bool calibrating
		{
			get
			{
				Update();
				if (pose.eTrackingResult != ETrackingResult.Calibrating_InProgress)
				{
					return pose.eTrackingResult == ETrackingResult.Calibrating_OutOfRange;
				}
				return true;
			}
		}

		public bool uninitialized
		{
			get
			{
				Update();
				return pose.eTrackingResult == ETrackingResult.Uninitialized;
			}
		}

		public SteamVR_Utils.RigidTransform transform
		{
			get
			{
				Update();
				return new SteamVR_Utils.RigidTransform(pose.mDeviceToAbsoluteTracking);
			}
		}

		public Vector3 velocity
		{
			get
			{
				//IL_0037: Unknown result type (might be due to invalid IL or missing references)
				Update();
				return new Vector3(pose.vVelocity.v0, pose.vVelocity.v1, 0f - pose.vVelocity.v2);
			}
		}

		public Vector3 angularVelocity
		{
			get
			{
				//IL_0038: Unknown result type (might be due to invalid IL or missing references)
				Update();
				return new Vector3(0f - pose.vAngularVelocity.v0, 0f - pose.vAngularVelocity.v1, pose.vAngularVelocity.v2);
			}
		}

		public Device(uint i)
		{
			index = i;
		}

		public VRControllerState_t GetState()
		{
			Update();
			return state;
		}

		public VRControllerState_t GetPrevState()
		{
			Update();
			return prevState;
		}

		public TrackedDevicePose_t GetPose()
		{
			Update();
			return pose;
		}

		public void Update()
		{
			if (Time.frameCount != prevFrameCount)
			{
				prevFrameCount = Time.frameCount;
				prevState = state;
				CVRSystem system = OpenVR.System;
				if (system != null)
				{
					valid = system.GetControllerStateWithPose(SteamVR_Render.instance.trackingSpace, index, ref state, (uint)Marshal.SizeOf(typeof(VRControllerState_t)), ref pose);
					UpdateHairTrigger();
				}
			}
		}

		public bool GetPress(ulong buttonMask)
		{
			Update();
			return (state.ulButtonPressed & buttonMask) != 0;
		}

		public bool GetPressDown(ulong buttonMask)
		{
			Update();
			if ((state.ulButtonPressed & buttonMask) != 0L)
			{
				return (prevState.ulButtonPressed & buttonMask) == 0;
			}
			return false;
		}

		public bool GetPressUp(ulong buttonMask)
		{
			Update();
			if ((state.ulButtonPressed & buttonMask) == 0L)
			{
				return (prevState.ulButtonPressed & buttonMask) != 0;
			}
			return false;
		}

		public bool GetPress(EVRButtonId buttonId)
		{
			return GetPress((ulong)(1L << (int)buttonId));
		}

		public bool GetPressDown(EVRButtonId buttonId)
		{
			return GetPressDown((ulong)(1L << (int)buttonId));
		}

		public bool GetPressUp(EVRButtonId buttonId)
		{
			return GetPressUp((ulong)(1L << (int)buttonId));
		}

		public bool GetTouch(ulong buttonMask)
		{
			Update();
			return (state.ulButtonTouched & buttonMask) != 0;
		}

		public bool GetTouchDown(ulong buttonMask)
		{
			Update();
			if ((state.ulButtonTouched & buttonMask) != 0L)
			{
				return (prevState.ulButtonTouched & buttonMask) == 0;
			}
			return false;
		}

		public bool GetTouchUp(ulong buttonMask)
		{
			Update();
			if ((state.ulButtonTouched & buttonMask) == 0L)
			{
				return (prevState.ulButtonTouched & buttonMask) != 0;
			}
			return false;
		}

		public bool GetTouch(EVRButtonId buttonId)
		{
			return GetTouch((ulong)(1L << (int)buttonId));
		}

		public bool GetTouchDown(EVRButtonId buttonId)
		{
			return GetTouchDown((ulong)(1L << (int)buttonId));
		}

		public bool GetTouchUp(EVRButtonId buttonId)
		{
			return GetTouchUp((ulong)(1L << (int)buttonId));
		}

		public Vector2 GetAxis(EVRButtonId buttonId = EVRButtonId.k_EButton_Axis0)
		{
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Unknown result type (might be due to invalid IL or missing references)
			//IL_0096: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
			Update();
			return (Vector2)((uint)buttonId switch
			{
				32u => new Vector2(state.rAxis0.x, state.rAxis0.y), 
				33u => new Vector2(state.rAxis1.x, state.rAxis1.y), 
				34u => new Vector2(state.rAxis2.x, state.rAxis2.y), 
				35u => new Vector2(state.rAxis3.x, state.rAxis3.y), 
				36u => new Vector2(state.rAxis4.x, state.rAxis4.y), 
				_ => Vector2.zero, 
			});
		}

		public void TriggerHapticPulse(ushort durationMicroSec = 500, EVRButtonId buttonId = EVRButtonId.k_EButton_Axis0)
		{
			CVRSystem system = OpenVR.System;
			if (system != null)
			{
				uint unAxisId = (uint)(buttonId - 32);
				system.TriggerHapticPulse(index, unAxisId, (char)durationMicroSec);
			}
		}

		private void UpdateHairTrigger()
		{
			hairTriggerPrevState = hairTriggerState;
			float x = state.rAxis1.x;
			if (hairTriggerState)
			{
				if (x < hairTriggerLimit - hairTriggerDelta || x <= 0f)
				{
					hairTriggerState = false;
				}
			}
			else if (x > hairTriggerLimit + hairTriggerDelta || x >= 1f)
			{
				hairTriggerState = true;
			}
			hairTriggerLimit = (hairTriggerState ? Mathf.Max(hairTriggerLimit, x) : Mathf.Min(hairTriggerLimit, x));
		}

		public bool GetHairTrigger()
		{
			Update();
			return hairTriggerState;
		}

		public bool GetHairTriggerDown()
		{
			Update();
			if (hairTriggerState)
			{
				return !hairTriggerPrevState;
			}
			return false;
		}

		public bool GetHairTriggerUp()
		{
			Update();
			if (!hairTriggerState)
			{
				return hairTriggerPrevState;
			}
			return false;
		}
	}

	public enum DeviceRelation
	{
		First,
		Leftmost,
		Rightmost,
		FarthestLeft,
		FarthestRight
	}

	private static Device[] devices;

	public static Device Input(int deviceIndex)
	{
		if (devices == null)
		{
			devices = new Device[64];
			for (uint num = 0u; num < devices.Length; num++)
			{
				devices[num] = new Device(num);
			}
		}
		return devices[deviceIndex];
	}

	public static void Update()
	{
		for (int i = 0; (long)i < 64L; i++)
		{
			Input(i).Update();
		}
	}

	public static int GetDeviceIndex(DeviceRelation relation, ETrackedDeviceClass deviceClass = ETrackedDeviceClass.Controller, int relativeTo = 0)
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		int result = -1;
		SteamVR_Utils.RigidTransform rigidTransform = (((uint)relativeTo < 64u) ? Input(relativeTo).transform.GetInverse() : SteamVR_Utils.RigidTransform.identity);
		CVRSystem system = OpenVR.System;
		if (system == null)
		{
			return result;
		}
		float num = float.MinValue;
		for (int i = 0; (long)i < 64L; i++)
		{
			if (i == relativeTo || system.GetTrackedDeviceClass((uint)i) != deviceClass)
			{
				continue;
			}
			Device device = Input(i);
			if (device.connected)
			{
				if (relation == DeviceRelation.First)
				{
					return i;
				}
				Vector3 val = rigidTransform * device.transform.pos;
				float num3;
				switch (relation)
				{
				case DeviceRelation.FarthestRight:
					num3 = val.x;
					break;
				case DeviceRelation.FarthestLeft:
					num3 = 0f - val.x;
					break;
				default:
				{
					Vector3 val2 = new Vector3(val.x, 0f, val.z);
					Vector3 normalized = ((Vector3)(ref val2)).normalized;
					float num2 = Vector3.Dot(normalized, Vector3.forward);
					Vector3 val3 = Vector3.Cross(normalized, Vector3.forward);
					num3 = ((relation != DeviceRelation.Leftmost) ? ((val3.y < 0f) ? (2f - num2) : num2) : ((val3.y > 0f) ? (2f - num2) : num2));
					break;
				}
				}
				if (num3 > num)
				{
					result = i;
					num = num3;
				}
			}
		}
		return result;
	}
}

using System;
using UnityEngine;
using Valve.VR;

public class SteamVR_TrackedObject : MonoBehaviour
{
	public enum EIndex
	{
		None = -1,
		Hmd,
		Device1,
		Device2,
		Device3,
		Device4,
		Device5,
		Device6,
		Device7,
		Device8,
		Device9,
		Device10,
		Device11,
		Device12,
		Device13,
		Device14,
		Device15
	}

	public EIndex index;

	[Tooltip("If not set, relative to parent")]
	public Transform origin;

	private SteamVR_Events.Action newPosesAction;

	public bool isValid { get; private set; }

	private void OnNewPoses(TrackedDevicePose_t[] poses)
	{
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		if (index == EIndex.None)
		{
			return;
		}
		int num = (int)index;
		isValid = false;
		if (poses.Length > num && poses[num].bDeviceIsConnected && poses[num].bPoseIsValid)
		{
			isValid = true;
			SteamVR_Utils.RigidTransform rigidTransform = new SteamVR_Utils.RigidTransform(poses[num].mDeviceToAbsoluteTracking);
			if ((Object)(object)origin != (Object)null)
			{
				((Component)this).transform.position = ((Component)origin).transform.TransformPoint(rigidTransform.pos);
				((Component)this).transform.rotation = origin.rotation * rigidTransform.rot;
			}
			else
			{
				((Component)this).transform.localPosition = rigidTransform.pos;
				((Component)this).transform.localRotation = rigidTransform.rot;
			}
		}
	}

	private SteamVR_TrackedObject()
	{
		newPosesAction = SteamVR_Events.NewPosesAction(OnNewPoses);
	}

	private void OnEnable()
	{
		if ((Object)(object)SteamVR_Render.instance == (Object)null)
		{
			((Behaviour)this).enabled = false;
		}
		else
		{
			newPosesAction.enabled = true;
		}
	}

	private void OnDisable()
	{
		newPosesAction.enabled = false;
		isValid = false;
	}

	public void SetDeviceIndex(int index)
	{
		if (Enum.IsDefined(typeof(EIndex), index))
		{
			this.index = (EIndex)index;
		}
	}
}

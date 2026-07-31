using UnityEngine;
using Valve.VR;

public class SteamVR_ControllerManager : MonoBehaviour
{
	public GameObject left;

	public GameObject right;

	[Tooltip("Populate with objects you want to assign to additional controllers")]
	public GameObject[] objects;

	[Tooltip("Set to true if you want objects arbitrarily assigned to controllers before their role (left vs right) is identified")]
	public bool assignAllBeforeIdentified;

	private uint[] indices;

	private bool[] connected = new bool[64];

	private uint leftIndex = uint.MaxValue;

	private uint rightIndex = uint.MaxValue;

	private SteamVR_Events.Action inputFocusAction;

	private SteamVR_Events.Action deviceConnectedAction;

	private SteamVR_Events.Action trackedDeviceRoleChangedAction;

	private static string hiddenPrefix = "hidden (";

	private static string hiddenPostfix = ")";

	private static string[] labels = new string[2] { "left", "right" };

	private void SetUniqueObject(GameObject o, int index)
	{
		for (int i = 0; i < index; i++)
		{
			if (objects[i] == o)
			{
				return;
			}
		}
		objects[index] = o;
	}

	public void UpdateTargets()
	{
		GameObject[] array = objects;
		int num = ((array != null) ? array.Length : 0);
		objects = (GameObject[])(object)new GameObject[2 + num];
		SetUniqueObject(right, 0);
		SetUniqueObject(left, 1);
		for (int i = 0; i < num; i++)
		{
			SetUniqueObject(array[i], 2 + i);
		}
		indices = new uint[2 + num];
		for (int j = 0; j < indices.Length; j++)
		{
			indices[j] = uint.MaxValue;
		}
	}

	private void Awake()
	{
		UpdateTargets();
	}

	private SteamVR_ControllerManager()
	{
		inputFocusAction = SteamVR_Events.InputFocusAction(OnInputFocus);
		deviceConnectedAction = SteamVR_Events.DeviceConnectedAction(OnDeviceConnected);
		trackedDeviceRoleChangedAction = SteamVR_Events.SystemAction(EVREventType.VREvent_TrackedDeviceRoleChanged, OnTrackedDeviceRoleChanged);
	}

	private void OnEnable()
	{
		for (int i = 0; i < objects.Length; i++)
		{
			GameObject val = objects[i];
			if (val != null)
			{
				val.SetActive(false);
			}
			indices[i] = uint.MaxValue;
		}
		Refresh();
		for (int j = 0; j < SteamVR.connected.Length; j++)
		{
			if (SteamVR.connected[j])
			{
				OnDeviceConnected(j, connected: true);
			}
		}
		inputFocusAction.enabled = true;
		deviceConnectedAction.enabled = true;
		trackedDeviceRoleChangedAction.enabled = true;
	}

	private void OnDisable()
	{
		inputFocusAction.enabled = false;
		deviceConnectedAction.enabled = false;
		trackedDeviceRoleChangedAction.enabled = false;
	}

	private void OnInputFocus(bool hasFocus)
	{
		if (hasFocus)
		{
			for (int i = 0; i < objects.Length; i++)
			{
				GameObject val = objects[i];
				if (val != null)
				{
					string text = ((i < 2) ? labels[i] : (i - 1).ToString());
					ShowObject(val.transform, hiddenPrefix + text + hiddenPostfix);
				}
			}
			return;
		}
		for (int j = 0; j < objects.Length; j++)
		{
			GameObject val2 = objects[j];
			if (val2 != null)
			{
				string text2 = ((j < 2) ? labels[j] : (j - 1).ToString());
				HideObject(val2.transform, hiddenPrefix + text2 + hiddenPostfix);
			}
		}
	}

	private void HideObject(Transform t, string name)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		if (((Object)((Component)t).gameObject).name.StartsWith(hiddenPrefix))
		{
			Debug.Log((object)"Ignoring double-hide.");
			return;
		}
		Transform transform = new GameObject(name).transform;
		transform.parent = t.parent;
		t.parent = transform;
		((Component)transform).gameObject.SetActive(false);
	}

	private void ShowObject(Transform t, string name)
	{
		Transform parent = t.parent;
		if (!(((Object)((Component)parent).gameObject).name != name))
		{
			t.parent = parent.parent;
			Object.Destroy((Object)(object)((Component)parent).gameObject);
		}
	}

	private void SetTrackedDeviceIndex(int objectIndex, uint trackedDeviceIndex)
	{
		if (trackedDeviceIndex != uint.MaxValue)
		{
			for (int i = 0; i < objects.Length; i++)
			{
				if (i != objectIndex && indices[i] == trackedDeviceIndex)
				{
					GameObject val = objects[i];
					if (val != null)
					{
						val.SetActive(false);
					}
					indices[i] = uint.MaxValue;
				}
			}
		}
		if (trackedDeviceIndex == indices[objectIndex])
		{
			return;
		}
		indices[objectIndex] = trackedDeviceIndex;
		GameObject val2 = objects[objectIndex];
		if (val2 != null)
		{
			if (trackedDeviceIndex == uint.MaxValue)
			{
				val2.SetActive(false);
				return;
			}
			val2.SetActive(true);
			val2.BroadcastMessage("SetDeviceIndex", (object)(int)trackedDeviceIndex, (SendMessageOptions)1);
		}
	}

	private void OnTrackedDeviceRoleChanged(VREvent_t vrEvent)
	{
		Refresh();
	}

	private void OnDeviceConnected(int index, bool connected)
	{
		bool flag = this.connected[index];
		this.connected[index] = false;
		if (connected)
		{
			CVRSystem system = OpenVR.System;
			if (system != null)
			{
				ETrackedDeviceClass trackedDeviceClass = system.GetTrackedDeviceClass((uint)index);
				// Hand objects must never bind to a body tracker. A tracker has a valid pose
				// but no controller buttons, which leaves the cyan tool icon in the world
				// while every hand action appears dead.
				if (trackedDeviceClass == ETrackedDeviceClass.Controller)
				{
					this.connected[index] = true;
					flag = !flag;
				}
			}
		}
		if (flag)
		{
			Refresh();
		}
	}

	public void Refresh()
	{
		int num = 0;
		CVRSystem system = OpenVR.System;
		if (system != null)
		{
			leftIndex = system.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
			rightIndex = system.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);
		}
		if (leftIndex == uint.MaxValue && rightIndex == uint.MaxValue)
		{
			for (uint num2 = 0u; num2 < connected.Length; num2++)
			{
				if (num >= objects.Length)
				{
					break;
				}
				if (connected[num2])
				{
					SetTrackedDeviceIndex(num++, num2);
					if (!assignAllBeforeIdentified)
					{
						break;
					}
				}
			}
		}
		else
		{
			SetTrackedDeviceIndex(num++, (rightIndex < connected.Length && connected[rightIndex]) ? rightIndex : uint.MaxValue);
			SetTrackedDeviceIndex(num++, (leftIndex < connected.Length && connected[leftIndex]) ? leftIndex : uint.MaxValue);
			if (leftIndex != uint.MaxValue && rightIndex != uint.MaxValue)
			{
				for (uint num3 = 0u; num3 < connected.Length; num3++)
				{
					if (num >= objects.Length)
					{
						break;
					}
					if (connected[num3] && num3 != leftIndex && num3 != rightIndex)
					{
						SetTrackedDeviceIndex(num++, num3);
					}
				}
			}
		}
		while (num < objects.Length)
		{
			SetTrackedDeviceIndex(num++, uint.MaxValue);
		}
	}
}

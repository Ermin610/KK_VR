using System.Collections;
using IllusionUtility.GetUtility;
using UnityEngine;
using VRGIN.Core;
using VRGIN.Modes;

namespace KKCharaStudioVR;

public class VRControllerMgr : MonoBehaviour
{
	private static VRControllerMgr _instance;

	private bool isOculusTouchMode;

	private bool touchModeCheckCompleted;

	public static bool IsOculusTouchMode => _instance.isOculusTouchMode;

	public static VRControllerMgr Install(GameObject container)
	{
		if (_instance == null)
		{
			_instance = container.AddComponent<VRControllerMgr>();
			_instance.OnLevelWasLoaded(Application.loadedLevel);
		}
		return _instance;
	}

	private void Start()
	{
	}

	private void OnLevelWasLoaded(int level)
	{
		((MonoBehaviour)this).StopAllCoroutines();
		touchModeCheckCompleted = false;
		((MonoBehaviour)this).StartCoroutine(CheckTouchMode());
	}

	private IEnumerator CheckTouchMode()
	{
		while (!touchModeCheckCompleted)
		{
			CheckControllerType();
			yield return (object)new WaitForSeconds(0.5f);
		}
	}

	private void CheckControllerType()
	{
		if (isOculusTouchMode)
		{
			touchModeCheckCompleted = true;
		}
		else
		{
			if (!(VR.Mode is StandingMode))
			{
				return;
			}
			if (VR.Mode.Left != null && VR.Mode.Left.IsTracking)
			{
				if (TransformFindEx.FindLoop(((Component)VR.Mode.Left).transform, "touchpad") != null)
				{
					isOculusTouchMode = false;
					touchModeCheckCompleted = true;
					return;
				}
				if (TransformFindEx.FindLoop(((Component)VR.Mode.Left).transform, "thumbstick") != null)
				{
					isOculusTouchMode = true;
					touchModeCheckCompleted = true;
				}
			}
			if (VR.Mode.Right != null && VR.Mode.Right.IsTracking)
			{
				if (TransformFindEx.FindLoop(((Component)VR.Mode.Right).transform, "touchpad") != null)
				{
					isOculusTouchMode = false;
					touchModeCheckCompleted = true;
				}
				else if (TransformFindEx.FindLoop(((Component)VR.Mode.Right).transform, "thumbstick") != null)
				{
					isOculusTouchMode = true;
					touchModeCheckCompleted = true;
				}
			}
		}
	}
}

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
		if ((Object)(object)_instance == (Object)null)
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
			if ((Object)(object)VR.Mode.Left != (Object)null && VR.Mode.Left.IsTracking)
			{
				if ((Object)(object)TransformFindEx.FindLoop(((Component)VR.Mode.Left).transform, "touchpad") != (Object)null)
				{
					isOculusTouchMode = false;
					touchModeCheckCompleted = true;
					return;
				}
				if ((Object)(object)TransformFindEx.FindLoop(((Component)VR.Mode.Left).transform, "thumbstick") != (Object)null)
				{
					isOculusTouchMode = true;
					touchModeCheckCompleted = true;
				}
			}
			if ((Object)(object)VR.Mode.Right != (Object)null && VR.Mode.Right.IsTracking)
			{
				if ((Object)(object)TransformFindEx.FindLoop(((Component)VR.Mode.Right).transform, "touchpad") != (Object)null)
				{
					isOculusTouchMode = false;
					touchModeCheckCompleted = true;
				}
				else if ((Object)(object)TransformFindEx.FindLoop(((Component)VR.Mode.Right).transform, "thumbstick") != (Object)null)
				{
					isOculusTouchMode = true;
					touchModeCheckCompleted = true;
				}
			}
		}
	}
}

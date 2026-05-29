using System.Collections;
using UnityEngine;

namespace Leap.Unity;

public class ExtendedFingerDetector : Detector
{
	[Tooltip("The interval in seconds at which to check this detector's conditions.")]
	public float Period = 0.1f;

	[Tooltip("The hand model to watch. Set automatically if detector is on a hand.")]
	public IHandModel HandModel;

	public PointingState Thumb = PointingState.Either;

	public PointingState Index = PointingState.Either;

	public PointingState Middle = PointingState.Either;

	public PointingState Ring = PointingState.Either;

	public PointingState Pinky = PointingState.Either;

	private IEnumerator watcherCoroutine;

	private void Awake()
	{
		watcherCoroutine = extendedFingerWatcher();
		if (HandModel == null)
		{
			HandModel = ((Component)this).gameObject.GetComponentInParent<IHandModel>();
		}
	}

	private void OnEnable()
	{
		((MonoBehaviour)this).StartCoroutine(watcherCoroutine);
	}

	private void OnDisable()
	{
		((MonoBehaviour)this).StopCoroutine(watcherCoroutine);
		Deactivate();
	}

	private IEnumerator extendedFingerWatcher()
	{
		while (true)
		{
			if (HandModel != null && HandModel.IsTracked)
			{
				Hand leapHand = HandModel.GetLeapHand();
				if (leapHand != null)
				{
					bool flag = matchFingerState(leapHand.Fingers[0], 0) && matchFingerState(leapHand.Fingers[1], 1) && matchFingerState(leapHand.Fingers[2], 2) && matchFingerState(leapHand.Fingers[3], 3) && matchFingerState(leapHand.Fingers[4], 4);
					if (HandModel.IsTracked && flag)
					{
						Activate();
					}
					else if (!HandModel.IsTracked || !flag)
					{
						Deactivate();
					}
				}
			}
			else if (IsActive)
			{
				Deactivate();
			}
			yield return (object)new WaitForSeconds(Period);
		}
	}

	private bool matchFingerState(Finger finger, int ordinal)
	{
		PointingState pointingState;
		switch (ordinal)
		{
		case 0:
			pointingState = Thumb;
			break;
		case 1:
			pointingState = Index;
			break;
		case 2:
			pointingState = Middle;
			break;
		case 3:
			pointingState = Ring;
			break;
		case 4:
			pointingState = Pinky;
			break;
		default:
			return false;
		}
		if (pointingState != PointingState.Either && (pointingState != 0 || !finger.IsExtended))
		{
			if (pointingState == PointingState.NotExtended)
			{
				return !finger.IsExtended;
			}
			return false;
		}
		return true;
	}
}

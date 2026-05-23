using System.Collections;
using UnityEngine;

namespace Leap.Unity;

public class HandDrop : HandTransitionBehavior
{
	private Vector3 startingPalmPosition;

	private Quaternion startingOrientation;

	private Transform palm;

	protected override void Awake()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		base.Awake();
		palm = ((Component)this).GetComponent<HandModel>().palm;
		startingPalmPosition = palm.localPosition;
		startingOrientation = palm.localRotation;
	}

	protected override void HandFinish()
	{
		((MonoBehaviour)this).StartCoroutine(LerpToStart());
	}

	protected override void HandReset()
	{
		((MonoBehaviour)this).StopAllCoroutines();
	}

	private IEnumerator LerpToStart()
	{
		Vector3 droppedPosition = palm.localPosition;
		Quaternion droppedOrientation = palm.localRotation;
		float duration = 1f;
		float startTime = Time.time;
		float endTime = startTime + duration;
		while (Time.time <= endTime)
		{
			float num = (Time.time - startTime) / duration;
			palm.localPosition = Vector3.Lerp(droppedPosition, startingPalmPosition, num);
			palm.localRotation = Quaternion.Lerp(droppedOrientation, startingOrientation, num);
			yield return null;
		}
	}
}

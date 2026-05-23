using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity;

[ExecuteInEditMode]
public class Transition : MonoBehaviour
{
	public bool AnimatePosition;

	public Vector3 RelativeOnPosition = Vector3.zero;

	public AnimationCurve XPosition;

	public AnimationCurve YPosition;

	public AnimationCurve ZPosition;

	public bool AnimateRotation;

	public Vector3 RelativeOnRotation = Vector3.zero;

	public AnimationCurve XRotation;

	public AnimationCurve YRotation;

	public AnimationCurve ZRotation;

	public bool AnimateScale;

	public Vector3 RelativeOnScale = Vector3.one;

	public AnimationCurve XScale;

	public AnimationCurve YScale;

	public AnimationCurve ZScale;

	[Range(0.001f, 2f)]
	public float Duration = 0.5f;

	[Range(-1f, 1f)]
	public float Simulate;

	[Range(0f, 1f)]
	public float Progress = 1f;

	public UnityEvent OnComplete;

	private void Awake()
	{
		updateTransition(1f);
	}

	public void TransitionIn()
	{
		if (((Behaviour)this).enabled)
		{
			((MonoBehaviour)this).StopAllCoroutines();
			((MonoBehaviour)this).StartCoroutine(transitionIn());
		}
	}

	public void TransitionOut()
	{
		if (((Behaviour)this).enabled)
		{
			((MonoBehaviour)this).StopAllCoroutines();
			((MonoBehaviour)this).StartCoroutine(transitionOut());
		}
	}

	private IEnumerator transitionIn()
	{
		float start = Time.time;
		do
		{
			Progress -= (Time.time - start) / Duration;
			updateTransition(Progress);
			yield return null;
		}
		while (Progress >= 0f);
		Progress = 0f;
		OnComplete.Invoke();
	}

	private IEnumerator transitionOut()
	{
		float start = Time.time;
		do
		{
			Progress = (Time.time - start) / Duration;
			updateTransition(Progress);
			yield return null;
		}
		while (Progress <= 1f);
		Progress = 1f;
		OnComplete.Invoke();
	}

	private void updateTransition(float interpolationPoint)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		if (AnimatePosition)
		{
			Vector3 localPosition = ((Component)this).transform.localPosition;
			localPosition.x = XPosition.Evaluate(interpolationPoint) * RelativeOnPosition.x;
			localPosition.y = YPosition.Evaluate(interpolationPoint) * RelativeOnPosition.y;
			localPosition.z = ZPosition.Evaluate(interpolationPoint) * RelativeOnPosition.z;
			((Component)this).transform.localPosition = localPosition;
		}
		if (AnimateRotation)
		{
			Quaternion localRotation = Quaternion.Euler(((Component)this).transform.localRotation.x + XRotation.Evaluate(interpolationPoint) * RelativeOnRotation.x, ((Component)this).transform.localRotation.y + YRotation.Evaluate(interpolationPoint) * RelativeOnRotation.y, ((Component)this).transform.localRotation.z + ZRotation.Evaluate(interpolationPoint) * RelativeOnRotation.z);
			((Component)this).transform.localRotation = localRotation;
		}
		if (AnimateScale)
		{
			Vector3 localScale = ((Component)this).transform.localScale;
			localScale.x = XScale.Evaluate(1f - interpolationPoint) * RelativeOnScale.x;
			localScale.y = YScale.Evaluate(1f - interpolationPoint) * RelativeOnScale.y;
			localScale.z = ZScale.Evaluate(1f - interpolationPoint) * RelativeOnScale.z;
			((Component)this).transform.localScale = localScale;
		}
	}
}

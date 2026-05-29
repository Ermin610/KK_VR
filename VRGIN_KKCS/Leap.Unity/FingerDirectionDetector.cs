using System.Collections;
using UnityEngine;

namespace Leap.Unity;

public class FingerDirectionDetector : Detector
{
	[Tooltip("The interval in seconds at which to check this detector's conditions.")]
	public float Period = 0.1f;

	[Tooltip("The hand model to watch. Set automatically if detector is on a hand.")]
	public IHandModel HandModel;

	[Tooltip("The finger to observe.")]
	public Finger.FingerType FingerName = Finger.FingerType.TYPE_INDEX;

	[Tooltip("The target direction.")]
	public Vector3 PointingDirection = Vector3.forward;

	[Tooltip("How to treat the target direction.")]
	public PointingType PointingType = PointingType.RelativeToHorizon;

	[Tooltip("A target object(optional). Use PointingType.AtTarget")]
	public Transform TargetObject;

	[Tooltip("The angle in degrees from the target direction at which to turn on.")]
	[Range(0f, 360f)]
	public float OnAngle = 15f;

	[Tooltip("The angle in degrees from the target direction at which to turn off.")]
	[Range(0f, 360f)]
	public float OffAngle = 25f;

	private IEnumerator watcherCoroutine;

	private void OnValidate()
	{
		if (OffAngle < OnAngle)
		{
			OffAngle = OnAngle;
		}
	}

	private void Awake()
	{
		watcherCoroutine = fingerPointingWatcher();
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

	private IEnumerator fingerPointingWatcher()
	{
		int selectedFinger = selectedFingerOrdinal();
		while (true)
		{
			if (HandModel != null)
			{
				Hand leapHand = HandModel.GetLeapHand();
				if (leapHand != null)
				{
					Vector3 val = selectedDirection(leapHand.Fingers[selectedFinger].TipPosition.ToVector3());
					float num = Vector3.Angle(leapHand.Fingers[selectedFinger].Direction.ToVector3(), val);
					if (HandModel.IsTracked && num <= OnAngle)
					{
						Activate();
					}
					else if (!HandModel.IsTracked || num >= OffAngle)
					{
						Deactivate();
					}
				}
			}
			yield return (object)new WaitForSeconds(Period);
		}
	}

	private Vector3 selectedDirection(Vector3 tipPosition)
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		switch (PointingType)
		{
		case PointingType.RelativeToHorizon:
		{
			Quaternion rotation = ((Component)Camera.main).transform.rotation;
			return Quaternion.AngleAxis(rotation.eulerAngles.y, Vector3.up) * PointingDirection;
		}
		case PointingType.RelativeToCamera:
			return ((Component)Camera.main).transform.TransformDirection(PointingDirection);
		case PointingType.RelativeToWorld:
			return PointingDirection;
		case PointingType.AtTarget:
			return TargetObject.position - tipPosition;
		default:
			return PointingDirection;
		}
	}

	private int selectedFingerOrdinal()
	{
		return FingerName switch
		{
			Finger.FingerType.TYPE_INDEX => 1, 
			Finger.FingerType.TYPE_MIDDLE => 2, 
			Finger.FingerType.TYPE_PINKY => 4, 
			Finger.FingerType.TYPE_RING => 3, 
			Finger.FingerType.TYPE_THUMB => 0, 
			_ => 1, 
		};
	}
}

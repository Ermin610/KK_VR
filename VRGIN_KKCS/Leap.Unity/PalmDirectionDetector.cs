using System.Collections;
using UnityEngine;
using VRGIN.Core;

namespace Leap.Unity;

public class PalmDirectionDetector : Detector
{
	[Tooltip("The interval in seconds at which to check this detector's conditions.")]
	public float Period = 0.1f;

	[Tooltip("The hand model to watch. Set automatically if detector is on a hand.")]
	public IHandModel HandModel;

	[Tooltip("The target direction.")]
	public Vector3 PointingDirection = Vector3.forward;

	[Tooltip("How to treat the target direction.")]
	public PointingType PointingType = PointingType.RelativeToHorizon;

	[Tooltip("A target object(optional). Use PointingType.AtTarget")]
	public Transform TargetObject;

	[Tooltip("The angle in degrees from the target direction at which to turn on.")]
	[Range(0f, 360f)]
	public float OnAngle = 45f;

	[Tooltip("The angle in degrees from the target direction at which to turn off.")]
	[Range(0f, 360f)]
	public float OffAngle = 65f;

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
		watcherCoroutine = palmWatcher();
		if ((Object)(object)HandModel == (Object)null)
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
	}

	private IEnumerator palmWatcher()
	{
		while (true)
		{
			if ((Object)(object)HandModel != (Object)null)
			{
				Hand leapHand = HandModel.GetLeapHand();
				if (leapHand != null)
				{
					float num = Vector3.Angle(leapHand.PalmNormal.ToVector3(), selectedDirection(leapHand.PalmPosition.ToVector3()));
					if (num <= OnAngle)
					{
						Activate();
					}
					else if (num > OffAngle)
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
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		switch (PointingType)
		{
		case PointingType.RelativeToHorizon:
		{
			Quaternion rotation = ((Component)VR.Camera.Head).transform.rotation;
			return Quaternion.AngleAxis(((Quaternion)(ref rotation)).eulerAngles.y, Vector3.up) * PointingDirection;
		}
		case PointingType.RelativeToCamera:
			return VR.Camera.Head.TransformDirection(PointingDirection);
		case PointingType.RelativeToWorld:
			return PointingDirection;
		case PointingType.AtTarget:
			return TargetObject.position - tipPosition;
		default:
			return PointingDirection;
		}
	}
}

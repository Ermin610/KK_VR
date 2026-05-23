using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity;

public class ProximityDetector : Detector
{
	[Tooltip("The interval in seconds at which to check this detector's conditions.")]
	public float Period = 0.1f;

	[Tooltip("Dispatched when close enough to a target.")]
	public ProximityEvent OnProximity = new ProximityEvent();

	[Tooltip("The list of target objects.")]
	public GameObject[] TargetObjects;

	[Tooltip("The target distance in meters to activate the detector.")]
	public float OnDistance = 0.01f;

	[Tooltip("The distance in meters at which to deactivate the detector.")]
	public float OffDistance = 0.015f;

	private IEnumerator proximityWatcherCoroutine;

	private GameObject _currentObj;

	public GameObject CurrentObject => _currentObj;

	private void Awake()
	{
		proximityWatcherCoroutine = proximityWatcher();
	}

	private void OnEnable()
	{
		((MonoBehaviour)this).StopCoroutine(proximityWatcherCoroutine);
		((MonoBehaviour)this).StartCoroutine(proximityWatcherCoroutine);
	}

	private void OnDisable()
	{
		((MonoBehaviour)this).StopCoroutine(proximityWatcherCoroutine);
	}

	private IEnumerator proximityWatcher()
	{
		bool proximityState = false;
		while (true)
		{
			float num = OnDistance * OnDistance;
			float num2 = OffDistance * OffDistance;
			if ((Object)(object)_currentObj != (Object)null)
			{
				if (distanceSquared(_currentObj) > num2)
				{
					_currentObj = null;
					proximityState = false;
				}
			}
			else
			{
				for (int i = 0; i < TargetObjects.Length; i++)
				{
					GameObject val = TargetObjects[i];
					if (distanceSquared(val) < num)
					{
						_currentObj = val;
						proximityState = true;
						((UnityEvent<GameObject>)OnProximity).Invoke(_currentObj);
						break;
					}
				}
			}
			if (proximityState)
			{
				Activate();
			}
			else
			{
				Deactivate();
			}
			yield return (object)new WaitForSeconds(Period);
		}
	}

	private float distanceSquared(GameObject target)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		Collider component = target.GetComponent<Collider>();
		Vector3 val = ((!((Object)(object)component != (Object)null)) ? target.transform.position : component.ClosestPointOnBounds(((Component)this).transform.position));
		Vector3 val2 = val - ((Component)this).transform.position;
		return ((Vector3)(ref val2)).sqrMagnitude;
	}
}

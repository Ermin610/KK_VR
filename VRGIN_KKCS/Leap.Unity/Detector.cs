using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity;

public class Detector : MonoBehaviour
{
	private bool _isActive;

	[Tooltip("Draw this detector's Gizmos, if any. (Gizmos must be on in Unity edtor, too.)")]
	public bool ShowGizmos = true;

	[Tooltip("Dispatched when condition is detected.")]
	public UnityEvent OnActivate = new UnityEvent();

	[Tooltip("Dispatched when condition is no longer detected.")]
	public UnityEvent OnDeactivate = new UnityEvent();

	public bool IsActive => _isActive;

	public virtual void Activate()
	{
		if (!IsActive)
		{
			_isActive = true;
			OnActivate.Invoke();
		}
	}

	public virtual void Deactivate()
	{
		if (IsActive)
		{
			_isActive = false;
			OnDeactivate.Invoke();
		}
	}
}

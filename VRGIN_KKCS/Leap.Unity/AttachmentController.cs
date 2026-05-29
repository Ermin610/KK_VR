using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity;

public class AttachmentController : MonoBehaviour
{
	public bool IsActive;

	public Transition InTransition;

	public Transition OutTransition;

	public virtual void Activate(bool doTransition = true)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		IsActive = true;
		ChangeChildState();
		if (InTransition != null && doTransition)
		{
			InTransition.OnComplete.AddListener(new UnityAction(ChangeChildState));
			InTransition.TransitionIn();
		}
	}

	public virtual void Deactivate(bool doTransition = true)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		IsActive = false;
		if (OutTransition != null && doTransition)
		{
			OutTransition.OnComplete.AddListener(new UnityAction(ChangeChildState));
			OutTransition.TransitionOut();
		}
		else
		{
			ChangeChildState();
		}
	}

	protected virtual void ChangeChildState()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Expected O, but got Unknown
		if (InTransition != null)
		{
			InTransition.OnComplete.RemoveListener(new UnityAction(ChangeChildState));
		}
		if (OutTransition != null)
		{
			OutTransition.OnComplete.RemoveListener(new UnityAction(ChangeChildState));
		}
		Transform[] componentsInChildren = ((Component)this).GetComponentsInChildren<Transform>(true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (((Object)((Component)componentsInChildren[i]).gameObject).GetInstanceID() != ((Object)((Component)this).gameObject).GetInstanceID())
			{
				((Component)componentsInChildren[i]).gameObject.SetActive(IsActive);
			}
		}
	}

	private void OnDisable()
	{
		Deactivate(doTransition: false);
	}
}

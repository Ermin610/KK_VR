using UnityEngine;

namespace Leap.Unity;

public abstract class HandTransitionBehavior : MonoBehaviour
{
	protected IHandModel iHandModel;

	protected abstract void HandReset();

	protected abstract void HandFinish();

	protected virtual void Awake()
	{
		iHandModel = ((Component)this).GetComponent<IHandModel>();
		if ((Object)(object)iHandModel == (Object)null)
		{
			Debug.LogWarning((object)"HandTransitionBehavior components require an IHandModel component attached to the same GameObject");
			return;
		}
		iHandModel.OnBegin += HandReset;
		iHandModel.OnFinish += HandFinish;
	}

	protected virtual void OnDestroy()
	{
		IHandModel component = ((Component)this).GetComponent<IHandModel>();
		if ((Object)(object)component == (Object)null)
		{
			Debug.LogWarning((object)"HandTransitionBehavior components require an IHandModel component attached to the same GameObject");
			return;
		}
		component.OnBegin -= HandReset;
		component.OnFinish -= HandFinish;
	}
}

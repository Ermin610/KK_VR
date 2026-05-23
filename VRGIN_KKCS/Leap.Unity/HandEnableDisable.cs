using System.Runtime.InteropServices;
using UnityEngine;

namespace Leap.Unity;

[Guid("8bcd03e0-0992-e084-c8be-61565d44b8bd")]
public class HandEnableDisable : HandTransitionBehavior
{
	protected override void Awake()
	{
		base.Awake();
		((Component)this).gameObject.SetActive(false);
	}

	protected override void HandReset()
	{
		((Component)this).gameObject.SetActive(true);
	}

	protected override void HandFinish()
	{
		((Component)this).gameObject.SetActive(false);
	}
}

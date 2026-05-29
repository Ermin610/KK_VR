using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VRGIN.Core;

internal class BlacklistThrottler : ProtectedBehaviour
{
	public HashSet<Type> Targets = new HashSet<Type>();

	protected override void OnStart()
	{
		Targets.Add(typeof(Camera));
		base.OnStart();
	}

	protected override void OnUpdate()
	{
		foreach (Behaviour item in from c in ((Component)this).GetComponents<Behaviour>()
			where Targets.Contains(((object)c).GetType())
			select c)
		{
			item.enabled = false;
		}
		base.OnUpdate();
	}
}

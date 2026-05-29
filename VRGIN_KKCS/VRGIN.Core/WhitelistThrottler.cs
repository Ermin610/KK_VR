using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VRGIN.Core;

internal class WhitelistThrottler : ProtectedBehaviour
{
	public HashSet<Type> Exceptions = new HashSet<Type>();

	protected override void OnStart()
	{
		Exceptions.Add(typeof(Transform));
		Exceptions.Add(typeof(ProtectedBehaviour));
		base.OnStart();
	}

	protected override void OnUpdate()
	{
		foreach (Behaviour item in from c in ((Component)this).GetComponents<Behaviour>()
			where !Exceptions.Contains(((object)c).GetType())
			select c)
		{
			item.enabled = false;
		}
		base.OnUpdate();
	}
}

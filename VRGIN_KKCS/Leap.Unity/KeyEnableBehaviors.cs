using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity;

public class KeyEnableBehaviors : MonoBehaviour
{
	public List<Behaviour> targets;

	[Header("Controls")]
	public KeyCode unlockHold;

	public KeyCode toggle = (KeyCode)32;

	private void Update()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		if (((int)unlockHold != 0 && !Input.GetKey(unlockHold)) || !Input.GetKeyDown(toggle))
		{
			return;
		}
		foreach (MonoBehaviour target in targets)
		{
			((Behaviour)target).enabled = !((Behaviour)target).enabled;
		}
	}
}

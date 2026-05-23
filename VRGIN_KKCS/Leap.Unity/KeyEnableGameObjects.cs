using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity;

public class KeyEnableGameObjects : MonoBehaviour
{
	public List<GameObject> targets;

	[Header("Controls")]
	public KeyCode unlockHold = (KeyCode)303;

	public KeyCode toggle = (KeyCode)116;

	private void Update()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		if (((int)unlockHold != 0 && !Input.GetKey(unlockHold)) || !Input.GetKeyDown(toggle))
		{
			return;
		}
		foreach (GameObject target in targets)
		{
			target.SetActive(!target.activeSelf);
		}
	}
}

using UnityEngine;

namespace VRGIN.Controls;

public class LeftController : Controller
{
	public static LeftController Create()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		return new GameObject("Left Controller").AddComponent<LeftController>();
	}
}

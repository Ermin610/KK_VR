using UnityEngine;

namespace VRGIN.Controls;

public class RightController : Controller
{
	public static RightController Create()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		return new GameObject("Right Controller").AddComponent<RightController>();
	}
}

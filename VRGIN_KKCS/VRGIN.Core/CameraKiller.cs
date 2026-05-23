using System.Linq;
using UnityEngine;
using VRGIN.Helpers;

namespace VRGIN.Core;

public class CameraKiller : ProtectedBehaviour
{
	private MonoBehaviour[] _CameraEffects = (MonoBehaviour[])(object)new MonoBehaviour[0];

	private Camera _Camera;

	protected override void OnStart()
	{
		base.OnStart();
		_CameraEffects = ((Component)this).gameObject.GetCameraEffects().ToArray();
		_Camera = ((Component)this).GetComponent<Camera>();
		_Camera.cullingMask = 0;
		_Camera.depth = -9999f;
		_Camera.useOcclusionCulling = false;
		_Camera.clearFlags = (CameraClearFlags)4;
	}

	public void OnPreCull()
	{
		((Behaviour)_Camera).enabled = false;
	}

	public void OnGUI()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		if ((int)Event.current.type == 7)
		{
			((Behaviour)_Camera).enabled = true;
		}
	}
}

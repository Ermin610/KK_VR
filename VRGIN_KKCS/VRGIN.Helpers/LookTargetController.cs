using UnityEngine;
using VRGIN.Core;

namespace VRGIN.Helpers;

public class LookTargetController : ProtectedBehaviour
{
	private Transform _RootNode;

	public float Offset = 0.5f;

	public Transform Target { get; private set; }

	public static LookTargetController AttachTo(IActor actor, GameObject gameObject)
	{
		LookTargetController lookTargetController = gameObject.AddComponent<LookTargetController>();
		lookTargetController._RootNode = actor.Eyes;
		return lookTargetController;
	}

	protected override void OnAwake()
	{
		base.OnAwake();
		CreateTarget();
	}

	private void CreateTarget()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		Target = new GameObject("VRGIN_LookTarget").transform;
		Object.DontDestroyOnLoad((Object)(object)((Component)Target).gameObject);
	}

	protected override void OnUpdate()
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		base.OnUpdate();
		if ((_RootNode != null) && (((Component)VR.Camera.SteamCam.head).transform != null))
		{
			Transform transform = ((Component)VR.Camera.SteamCam.head).transform;
			Vector3 val = transform.position - _RootNode.position;
			Vector3 normalized = val.normalized;
			((Component)Target).transform.position = transform.position + normalized * Offset;
		}
	}

	private void OnDestroy()
	{
		Object.Destroy((Object)(object)((Component)Target).gameObject);
	}
}

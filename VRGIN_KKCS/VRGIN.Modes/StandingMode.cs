using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using VRGIN.Controls.Tools;
using VRGIN.Core;
using Valve.VR;

namespace VRGIN.Modes;

public class StandingMode : ControlMode
{
	public override IEnumerable<Type> Tools => base.Tools.Concat(new Type[2]
	{
		typeof(MenuTool),
		typeof(WarpTool)
	});

	public override ETrackingUniverseOrigin TrackingOrigin => ETrackingUniverseOrigin.TrackingUniverseStanding;

	public override void Impersonate(IActor actor, ImpersonationMode mode)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		base.Impersonate(actor, mode);
		MoveToPosition(actor.Eyes.position, actor.Eyes.rotation, mode == ImpersonationMode.Approximately);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
	}

	protected override void OnStart()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		base.OnStart();
		VR.Camera.SteamCam.origin.position = Vector3.zero;
		VR.Camera.SteamCam.origin.rotation = Quaternion.identity;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (VRCamera.Instance.HasValidBlueprint)
		{
			SyncCameras();
		}
	}

	protected virtual void SyncCameras()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		((Component)VRCamera.Instance.Blueprint).transform.position = VR.Camera.SteamCam.head.position;
		((Component)VRCamera.Instance.Blueprint).transform.rotation = VR.Camera.SteamCam.head.rotation;
	}
}

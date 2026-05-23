using System.Collections.Generic;
using UnityEngine;
using VRGIN.Core;

namespace VRGIN.Controls.Tools;

public abstract class Tool : ProtectedBehaviour
{
	protected SteamVR_TrackedObject Tracking;

	protected Controller Owner;

	protected Controller Neighbor;

	public abstract Texture2D Image { get; }

	public GameObject Icon { get; set; }

	protected bool IsTracking
	{
		get
		{
			if (Object.op_Implicit((Object)(object)Tracking))
			{
				return Tracking.isValid;
			}
			return false;
		}
	}

	protected SteamVR_Controller.Device Controller => SteamVR_Controller.Input((int)Tracking.index);

	protected Controller OtherController => Neighbor;

	protected override void OnStart()
	{
		base.OnStart();
		Tracking = ((Component)this).GetComponent<SteamVR_TrackedObject>();
		Owner = ((Component)this).GetComponent<Controller>();
		Neighbor = (((Object)(object)VR.Mode.Left == (Object)(object)Owner) ? VR.Mode.Right : VR.Mode.Left);
		VRLog.Info(Object.op_Implicit((Object)(object)Neighbor) ? "Got my neighbor!" : "No neighbor");
	}

	protected abstract void OnDestroy();

	protected virtual void OnEnable()
	{
		VRLog.Info("On Enable: {0}", ((object)this).GetType().Name);
		if (Object.op_Implicit((Object)(object)Icon))
		{
			Icon.SetActive(true);
		}
		else
		{
			VRLog.Info("But no icon...");
		}
	}

	protected virtual void OnDisable()
	{
		VRLog.Info("On Disable: {0}", ((object)this).GetType().Name);
		if (Object.op_Implicit((Object)(object)Icon))
		{
			Icon.SetActive(false);
		}
		else
		{
			VRLog.Info("But no icon...");
		}
	}

	public virtual List<HelpText> GetHelpTexts()
	{
		return new List<HelpText>();
	}

	protected Transform FindAttachPosition(params string[] names)
	{
		return Owner.FindAttachPosition(names);
	}
}

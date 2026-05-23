using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VRGIN.Core;

public class GameInterpreter : ProtectedBehaviour
{
	public virtual IEnumerable<IActor> Actors
	{
		get
		{
			yield break;
		}
	}

	public virtual bool IsEveryoneHeaded => Actors.All((IActor a) => a.HasHead);

	public virtual int DefaultCullingMask => LayerMask.GetMask(new string[1] { "Default" });

	protected override void OnLevel(int level)
	{
		base.OnLevel(level);
		VRLog.Info("Loaded level {0}", level);
	}

	public virtual IActor FindImpersonatedActor()
	{
		return Actors.FirstOrDefault((IActor a) => !a.HasHead);
	}

	public virtual IActor FindNextActorToImpersonate()
	{
		List<IActor> list = Actors.ToList();
		IActor actor2 = FindImpersonatedActor();
		if (actor2 != null)
		{
			list.Remove(actor2);
		}
		return list.OrderByDescending(delegate(IActor actor)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			Vector3 val = actor.Eyes.position - ((Component)VR.Camera).transform.position;
			return Vector3.Dot(((Vector3)(ref val)).normalized, VR.Camera.SteamCam.head.forward);
		}).FirstOrDefault();
	}

	public virtual Camera FindCamera()
	{
		return Camera.main;
	}

	public virtual IEnumerable<Camera> FindSubCameras()
	{
		return Camera.allCameras.Where((Camera c) => (Object)(object)c.targetTexture == (Object)null).Except((IEnumerable<Camera>)(object)new Camera[1] { Camera.main });
	}

	public CameraJudgement JudgeCamera(Camera camera)
	{
		if (((Object)camera).name.Contains("VRGIN") || ((Object)camera).name == "poseUpdater")
		{
			return CameraJudgement.Ignore;
		}
		return JudgeCameraInternal(camera);
	}

	protected virtual CameraJudgement JudgeCameraInternal(Camera camera)
	{
		bool flag = VR.GUI.IsInterested(camera);
		if ((Object)(object)camera.targetTexture == (Object)null)
		{
			if (flag)
			{
				return CameraJudgement.GUIAndCamera;
			}
			if (((Component)camera).CompareTag("MainCamera"))
			{
				return CameraJudgement.MainCamera;
			}
			return CameraJudgement.SubCamera;
		}
		if (!flag)
		{
			return CameraJudgement.Ignore;
		}
		return CameraJudgement.GUI;
	}

	public virtual bool IsBody(Collider collider)
	{
		return false;
	}

	public virtual bool IsIgnoredCanvas(Canvas canvas)
	{
		return false;
	}

	public virtual bool IsAllowedEffect(MonoBehaviour effect)
	{
		return !VR.Settings.EffectBlacklist.Contains(((object)effect).GetType().Name);
	}

	public virtual bool IsIrrelevantCamera(Camera blueprint)
	{
		return true;
	}

	public virtual bool IsUICamera(Camera camera)
	{
		return Object.op_Implicit((Object)(object)((Component)camera).GetComponent("UICamera"));
	}
}

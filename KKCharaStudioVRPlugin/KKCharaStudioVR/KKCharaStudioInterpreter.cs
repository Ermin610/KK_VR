using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using BepInEx4;
using Manager;
using Studio;
using UnityEngine;
using VRGIN.Core;
using Logger = BepInEx4.Logger;

namespace KKCharaStudioVR;

internal class KKCharaStudioInterpreter : GameInterpreter
{
	private List<KKCharaStudioActor> _Actors = new List<KKCharaStudioActor>();

	private Camera _SubCamera;

	private StudioScene studioScene;

	private int additionalCullingMask;

	public override IEnumerable<IActor> Actors => _Actors.Cast<IActor>();

	protected override void OnStart()
	{
		base.OnStart();
		studioScene = UnityEngine.Object.FindObjectOfType<StudioScene>();
		additionalCullingMask = LayerMask.GetMask(new string[1] { "Studio/Select" });
	}

	protected override void OnLevel(int level)
	{
		base.OnLevel(level);
	}

	public override Camera FindCamera()
	{
		return null;
	}

	public override IActor FindNextActorToImpersonate()
	{
		List<IActor> list = Actors.ToList();
		IActor actor = FindImpersonatedActor();
		if (actor == null)
		{
			return list.FirstOrDefault();
		}
		return list[(list.IndexOf(actor) + 1) % list.Count];
	}

	protected override void OnUpdate()
	{
		try
		{
			if ((VR.Manager != null))
			{
				RefreshActors();
				UpdateMainCameraCullingMask();
			}
		}
		catch (Exception)
		{
		}
	}

	private void UpdateMainCameraCullingMask()
	{
		Camera component = ((Component)VR.Camera.SteamCam).GetComponent<Camera>();
		if (Singleton<Studio.Studio>.Instance.workInfo.visibleAxis)
		{
			component.cullingMask |= additionalCullingMask;
		}
		else
		{
			component.cullingMask &= ~additionalCullingMask;
		}
	}

	private void RefreshActors()
	{
		_Actors.Clear();
		foreach (ChaControl value in Singleton<Character>.Instance.dictEntryChara.Values)
		{
			if (((ChaInfo)value).objBodyBone != null)
			{
				AddActor(DefaultActorBehaviour<ChaControl>.Create<KKCharaStudioActor>(value));
			}
		}
	}

	private void AddActor(KKCharaStudioActor actor)
	{
		if ((actor.Eyes == null))
		{
			actor.Head.Reinitialize();
		}
		else
		{
			_Actors.Add(actor);
		}
	}

	public void ForceResetVRMode()
	{
		((MonoBehaviour)this).StartCoroutine(ForceResetVRModeCo());
	}

	private IEnumerator ForceResetVRModeCo()
	{
		Logger.Log((LogLevel)32, (object)"Check and reset to StandingMode if not.");
		yield return null;
		yield return null;
		yield return null;
		yield return null;
		yield return null;
		if ((VRManager.Instance.Mode == null) || !(VRManager.Instance.Mode is GenericStandingMode))
		{
			Logger.Log((LogLevel)32, (object)"Mode is not StandingMode. Force reset as Standing Mode.");
			ForceResetAsStandingMode();
		}
		else
		{
			Logger.Log((LogLevel)32, (object)"Is Standing Mode. Skip to setting force.");
		}
	}

	public static void ForceResetAsStandingMode()
	{
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Invalid comparison between Unknown and I4
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			VR.Manager.SetMode<GenericStandingMode>();
			if ((VR.Camera != null))
			{
				_ = VR.Camera.Blueprint;
				Camera mainCmaera = Singleton<Studio.Studio>.Instance.cameraCtrl.mainCmaera;
				Logger.Log((LogLevel)32, (object)$"Force replace blueprint camera with {mainCmaera}");
				Camera camera = VR.Camera.SteamCam.camera;
				Camera val = mainCmaera;
				camera.nearClipPlane = VR.Context.NearClipPlane;
				camera.farClipPlane = Mathf.Max(val.farClipPlane, 10f);
				camera.clearFlags = (CameraClearFlags)(((int)val.clearFlags == 1) ? 1 : 2);
				camera.renderingPath = val.renderingPath;
				camera.clearStencilAfterLightingPass = val.clearStencilAfterLightingPass;
				camera.depthTextureMode = val.depthTextureMode;
				camera.layerCullDistances = val.layerCullDistances;
				camera.layerCullSpherical = val.layerCullSpherical;
				camera.useOcclusionCulling = val.useOcclusionCulling;
				camera.allowHDR = val.allowHDR;
				camera.backgroundColor = val.backgroundColor;
				Skybox component = ((Component)val).GetComponent<Skybox>();
				if (component != null)
				{
					Skybox val2 = ((Component)camera).gameObject.GetComponent<Skybox>();
					if (val2 == null)
					{
						val2 = ((Component)val2).gameObject.AddComponent<Skybox>();
					}
					val2.material = component.material;
				}
				VR.Camera.CopyFX(val);
			}
			else
			{
				Logger.Log((LogLevel)32, (object)"VR.Camera is null");
			}
		}
		catch (Exception value)
		{
			Console.WriteLine(value);
		}
	}
}

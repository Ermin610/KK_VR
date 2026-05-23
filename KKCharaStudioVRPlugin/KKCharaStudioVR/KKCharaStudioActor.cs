using UnityEngine;
using VRGIN.Core;
using VRGIN.Helpers;

namespace KKCharaStudioVR;

public class KKCharaStudioActor : DefaultActorBehaviour<ChaControl>
{
	private LookTargetController _TargetController;

	public TransientHead Head { get; private set; }

	public override Transform Eyes => Head.Eyes;

	public override bool HasHead
	{
		get
		{
			return Head.Visible;
		}
		set
		{
			Head.Visible = value;
		}
	}

	public bool IsFemale => ((ChaInfo)base.Actor).sex == 1;

	protected override void Initialize(ChaControl actor)
	{
		base.Initialize(actor);
		Head = ((Component)actor).gameObject.AddComponent<TransientHead>();
	}

	protected override void OnStart()
	{
		base.OnStart();
		_TargetController = LookTargetController.AttachTo(this, ((Component)this).gameObject);
	}

	protected override void OnLevel(int level)
	{
		base.OnLevel(level);
	}

	private void InitializeDynamicBoneColliders()
	{
		DynamicBone[] array = Object.FindObjectsOfType<DynamicBone>();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].m_UpdateRate = 90f;
		}
		DynamicBone_Ver01[] array2 = Object.FindObjectsOfType<DynamicBone_Ver01>();
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].m_UpdateRate = 90f;
		}
		DynamicBone_Ver02[] array3 = Object.FindObjectsOfType<DynamicBone_Ver02>();
		for (int i = 0; i < array3.Length; i++)
		{
			array3[i].UpdateRate = 90f;
		}
	}

	protected override void OnLateUpdate()
	{
		base.OnLateUpdate();
		EyeLookController eyeLookCtrl = ((ChaInfo)base.Actor).eyeLookCtrl;
		NeckLookControllerVer2 neckLookCtrl = ((ChaInfo)base.Actor).neckLookCtrl;
		Transform transform = ((Component)Camera.main).transform;
		if (Object.op_Implicit((Object)(object)transform))
		{
			if (Object.op_Implicit((Object)(object)eyeLookCtrl) && (Object)(object)eyeLookCtrl.target == (Object)(object)transform)
			{
				eyeLookCtrl.target = _TargetController.Target;
			}
			if (Object.op_Implicit((Object)(object)neckLookCtrl) && (Object)(object)neckLookCtrl.target == (Object)(object)transform)
			{
				neckLookCtrl.target = _TargetController.Target;
			}
		}
	}

	internal void OnVRModeChanged(bool newMode)
	{
		if (!((Object)(object)_TargetController != (Object)null) || newMode)
		{
			return;
		}
		EyeLookController eyeLookCtrl = ((ChaInfo)base.Actor).eyeLookCtrl;
		NeckLookControllerVer2 neckLookCtrl = ((ChaInfo)base.Actor).neckLookCtrl;
		Transform transform = ((Component)Camera.main).transform;
		if (Object.op_Implicit((Object)(object)transform))
		{
			if (Object.op_Implicit((Object)(object)eyeLookCtrl) && (Object)(object)eyeLookCtrl.target == (Object)(object)_TargetController.Target)
			{
				eyeLookCtrl.target = transform;
			}
			if (Object.op_Implicit((Object)(object)neckLookCtrl) && (Object)(object)neckLookCtrl.target == (Object)(object)_TargetController.Target)
			{
				neckLookCtrl.target = transform;
			}
		}
	}
}

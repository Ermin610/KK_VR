using UnityEngine;
using UnityEngine.Rendering;
using VRGIN.Core;

namespace VRGIN.U46.Visuals;

public class PlayAreaVisualization : ProtectedBehaviour
{
	private class HMDLoader : ProtectedBehaviour
	{
		public Transform NewParent;

		private SteamVR_RenderModel _Model;

		protected override void OnStart()
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			Object.DontDestroyOnLoad((Object)(object)this);
			((Component)this).transform.localScale = Vector3.zero;
			_Model = ((Component)this).gameObject.AddComponent<SteamVR_RenderModel>();
			_Model.shader = VR.Context.Materials.StandardShader;
			((Component)this).gameObject.AddComponent<SteamVR_TrackedObject>();
			_Model.SetDeviceIndex(0);
		}

		protected override void OnUpdate()
		{
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_0072: Unknown result type (might be due to invalid IL or missing references)
			base.OnUpdate();
			if (!(NewParent != null) && !((Behaviour)this).enabled)
			{
				Object.DestroyImmediate((Object)(object)((Component)this).gameObject);
			}
			if ((((Component)this).GetComponent<Renderer>() != null))
			{
				if ((NewParent != null))
				{
					((Component)this).transform.SetParent(NewParent, false);
					((Component)this).transform.localScale = Vector3.one;
					((Component)this).GetComponent<Renderer>().material.color = VR.Context.PrimaryColor;
					((Behaviour)this).enabled = false;
				}
				else
				{
					VRLog.Info("We're too late!");
					Object.Destroy((Object)(object)((Component)this).gameObject);
				}
			}
		}
	}

	public PlayArea Area = new PlayArea();

	private SteamVR_PlayArea PlayArea;

	private Transform Indicator;

	private Transform DirectionIndicator;

	private Transform HeightIndicator;

	private Material[] _IndicatorMaterials;

	protected override void OnAwake()
	{
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		base.OnAwake();
		CreateArea();
		Indicator = GameObject.CreatePrimitive((PrimitiveType)0).transform;
		Indicator.SetParent(((Component)this).transform, false);
		HeightIndicator = GameObject.CreatePrimitive((PrimitiveType)2).transform;
		HeightIndicator.SetParent(((Component)this).transform, false);
		Transform[] array = (Transform[])(object)new Transform[2] { Indicator, HeightIndicator };
		_IndicatorMaterials = new Material[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			Renderer component = ((Component)array[i]).GetComponent<Renderer>();
			Material source = VR.Context.Materials.Sprite;
			if (source != null)
			{
				_IndicatorMaterials[i] = new Material(source);
				_IndicatorMaterials[i].name = "VRGIN Play Area Indicator";
				_IndicatorMaterials[i].hideFlags = HideFlags.HideAndDontSave;
				component.sharedMaterial = _IndicatorMaterials[i];
			}
			else
			{
				component.enabled = false;
			}
			component.reflectionProbeUsage = (ReflectionProbeUsage)0;
			component.shadowCastingMode = (ShadowCastingMode)0;
			component.receiveShadows = false;
			component.useLightProbes = false;
			component.material.color = VR.Context.PrimaryColor;
		}
	}

	protected virtual void CreateArea()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		PlayArea = new GameObject("PlayArea").AddComponent<SteamVR_PlayArea>();
		PlayArea.drawInGame = true;
		PlayArea.size = SteamVR_PlayArea.Size.Calibrated;
		((Component)PlayArea).transform.SetParent(((Component)this).transform, false);
		DirectionIndicator = CreateClone();
	}

	protected virtual Transform CreateClone()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		HMDLoader hMDLoader = new GameObject("Model").AddComponent<HMDLoader>();
		hMDLoader.NewParent = ((Component)PlayArea).transform;
		return ((Component)hMDLoader).transform;
	}

	internal static PlayAreaVisualization Create(PlayArea playArea = null)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		PlayAreaVisualization playAreaVisualization = new GameObject("Play Area Viszalization").AddComponent<PlayAreaVisualization>();
		if (playArea != null)
		{
			playAreaVisualization.Area = playArea;
		}
		return playAreaVisualization;
	}

	protected override void OnStart()
	{
		base.OnStart();
	}

	protected virtual void OnEnable()
	{
		PlayArea.BuildMesh();
	}

	protected virtual void OnDisable()
	{
	}

	protected virtual void OnDestroy()
	{
		if (_IndicatorMaterials == null)
			return;
		foreach (Material material in _IndicatorMaterials)
		{
			if (material != null)
				Object.Destroy(material);
		}
		_IndicatorMaterials = null;
	}

	public void Enable()
	{
		((Component)this).gameObject.SetActive(true);
	}

	public void Disable()
	{
		((Component)this).gameObject.SetActive(false);
	}

	public void UpdatePosition()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		SteamVR_Camera steamCam = VRCamera.Instance.SteamCam;
		float num = 2f;
		float y = steamCam.head.localPosition.y;
		float num2 = 1f;
		((Component)this).transform.position = Area.Position;
		((Component)this).transform.localScale = Vector3.one * Area.Scale;
		((Component)PlayArea).transform.localPosition = -new Vector3(((Component)steamCam.head).transform.localPosition.x, 0f, ((Component)steamCam.head).transform.localPosition.z);
		((Component)this).transform.rotation = Quaternion.Euler(0f, Area.Rotation, 0f);
		Indicator.localScale = Vector3.one * 0.1f + Vector3.one * Mathf.Sin(Time.time * 5f) * 0.05f;
		HeightIndicator.localScale = new Vector3(0.01f, y / num, 0.01f);
		HeightIndicator.localPosition = new Vector3(0f, y - num2 * (y / num), 0f);
	}

	protected override void OnLateUpdate()
	{
		UpdatePosition();
	}
}

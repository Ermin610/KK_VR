using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using VRGIN.Core;

namespace VRGIN.Visuals;

public class GUIQuad : ProtectedBehaviour
{
	private Renderer renderer;

	public bool IsOwned;

	private IScreenGrabber _Source;

	public static GUIQuad Create(IScreenGrabber source = null)
	{
		source = source ?? VR.GUI;
		VRLog.Info("Create GUI");
		GUIQuad gUIQuad = GameObject.CreatePrimitive((PrimitiveType)5).AddComponent<GUIQuad>();
		((Object)gUIQuad).name = "GUIQuad";
		if (source != VR.GUI)
		{
			((Component)gUIQuad).gameObject.SetActive(false);
			gUIQuad._Source = source;
			((Component)gUIQuad).gameObject.SetActive(true);
		}
		gUIQuad.UpdateGUI();
		return gUIQuad;
	}

	protected override void OnAwake()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		renderer = ((Component)this).GetComponent<Renderer>();
		_Source = VR.GUI;
		((Component)this).transform.localPosition = Vector3.zero;
		((Component)this).transform.localRotation = Quaternion.identity;
		((Component)this).gameObject.layer = LayerMask.NameToLayer(VRManager.Instance.Context.GuiLayer);
	}

	protected override void OnStart()
	{
		base.OnStart();
		UpdateAspect();
	}

	protected virtual void OnEnable()
	{
		if (IsGUISource())
		{
			VRLog.Info("Start listening to GUI ({0})", ((Object)this).name);
			GUIQuadRegistry.Register(this);
			VR.GUI.Listen();
		}
	}

	protected virtual void OnDisable()
	{
		if (IsGUISource())
		{
			VRLog.Info("Stop listening to GUI ({0})", ((Object)this).name);
			GUIQuadRegistry.Unregister(this);
			VR.GUI.Unlisten();
		}
	}

	private bool IsGUISource()
	{
		return _Source == VR.GUI;
	}

	public virtual void UpdateAspect()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		float y = ((Component)this).transform.localScale.y;
		float num = y / (float)VRGUI.Height * (float)VRGUI.Width;
		((Component)this).transform.localScale = new Vector3(num, y, 1f);
	}

	public virtual void UpdateGUI()
	{
		UpdateAspect();
		if (!Object.op_Implicit((Object)(object)renderer))
		{
			VRLog.Warn("No renderer!");
		}
		try
		{
			renderer.receiveShadows = false;
			renderer.shadowCastingMode = (ShadowCastingMode)0;
			IEnumerable<RenderTexture> textures = _Source.GetTextures();
			VRLog.Info("Updating GUI {0} with {1} textures", ((Object)this).name, textures.Count());
			if (textures.Count() >= 2)
			{
				renderer.material = VR.Context.Materials.UnlitTransparentCombined;
				renderer.material.SetTexture("_MainTex", (Texture)(object)textures.FirstOrDefault());
				renderer.material.SetTexture("_SubTex", (Texture)(object)textures.Last());
			}
			else
			{
				renderer.material = VR.Context.Materials.UnlitTransparent;
				renderer.material.SetTexture("_MainTex", (Texture)(object)textures.FirstOrDefault());
			}
		}
		catch (Exception obj)
		{
			VRLog.Info(obj);
		}
	}
}

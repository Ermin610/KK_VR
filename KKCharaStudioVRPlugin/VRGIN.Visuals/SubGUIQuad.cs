using System;
using UnityEngine;
using UnityEngine.Rendering;
using VRGIN.Core;

namespace VRGIN.Visuals;

public class SubGUIQuad : ProtectedBehaviour
{
	private Renderer renderer;

	public bool IsOwned;

	private int left;

	private int bottom;

	private int width;

	private int height;

	private int virtualScreenWidth;

	private int virtualScreenHeight;

	public static SubGUIQuad Create(int left, int bottom, int width, int height, int virtualScreenWidth, int virtualScreenHeight)
	{
		VRLog.Info("Create SubGUI");
		SubGUIQuad subGUIQuad = GameObject.CreatePrimitive((PrimitiveType)5).AddComponent<SubGUIQuad>();
		((Object)subGUIQuad).name = "SubGUIQuad";
		subGUIQuad.left = left;
		subGUIQuad.bottom = bottom;
		subGUIQuad.width = width;
		subGUIQuad.height = height;
		subGUIQuad.virtualScreenWidth = virtualScreenWidth;
		subGUIQuad.virtualScreenHeight = virtualScreenHeight;
		subGUIQuad.UpdateMesh();
		subGUIQuad.UpdateGUI();
		if ((Object)(object)VR.GUI.SoftCursor != (Object)null)
		{
			((Behaviour)VR.GUI.SoftCursor).enabled = false;
		}
		return subGUIQuad;
	}

	protected override void OnAwake()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		renderer = ((Component)this).GetComponent<Renderer>();
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
		VRLog.Info("Listen!");
		VRGUI.Instance.Listen();
	}

	public void UpdateMesh()
	{
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		MeshFilter component = ((Component)this).GetComponent<MeshFilter>();
		Mesh mesh = component.mesh;
		float num = (float)left / (float)virtualScreenWidth;
		float num2 = (float)bottom / (float)virtualScreenHeight;
		float num3 = (float)(left + width) / (float)virtualScreenWidth;
		float num4 = (float)(bottom + height) / (float)virtualScreenHeight;
		VRLog.Info("Mesh UV for SubGUI");
		for (int i = 0; i < mesh.uv.Length; i++)
		{
			VRLog.Info(mesh.uv[i]);
		}
		VRLog.Info("Mesh UV for SubGUI (Updated)");
		mesh.uv = (Vector2[])(object)new Vector2[4]
		{
			new Vector2(num, num2),
			new Vector2(num3, num4),
			new Vector2(num3, num2),
			new Vector2(num, num4)
		};
		for (int j = 0; j < mesh.uv.Length; j++)
		{
			VRLog.Info(mesh.uv[j]);
		}
		component.mesh = mesh;
	}

	protected virtual void OnDisable()
	{
		VRLog.Info("Unlisten!");
		VRGUI.Instance.Unlisten();
	}

	public virtual void UpdateAspect()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		float y = ((Component)this).transform.localScale.y;
		float num = y / (float)height * (float)width;
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
			renderer.material = VR.Context.Materials.UnlitTransparentCombined;
			renderer.material.SetTexture("_MainTex", (Texture)(object)VRGUI.Instance.uGuiTexture);
			renderer.material.SetTexture("_SubTex", (Texture)(object)VRGUI.Instance.IMGuiTexture);
		}
		catch (Exception obj)
		{
			VRLog.Info(obj);
		}
	}
}

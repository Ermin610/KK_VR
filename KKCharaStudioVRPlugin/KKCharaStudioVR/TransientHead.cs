using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using VRGIN.Core;
using VRGIN.Helpers;

namespace KKCharaStudioVR;

public class TransientHead : ProtectedBehaviour
{
	private List<Renderer> rendererList = new List<Renderer>();

	private bool hidden;

	private Transform root;

	private Renderer[] m_tongues;

	private ChaControl avatar;

	private Transform headTransform;

	private Transform eyesTransform;

	public Transform Eyes => eyesTransform;

	public bool Visible
	{
		get
		{
			return !hidden;
		}
		set
		{
			if (value)
			{
				Console.WriteLine("SHOW");
			}
			else
			{
				Console.WriteLine("HIDE");
			}
			SetVisibility(value);
		}
	}

	protected override void OnAwake()
	{
		base.OnAwake();
		avatar = ((Component)this).GetComponent<ChaControl>();
		Reinitialize();
	}

	public void Reinitialize()
	{
		headTransform = GetHead(avatar);
		eyesTransform = GetEyes(avatar);
		root = ((ChaInfo)avatar).objRoot.transform;
		m_tongues = (Renderer[])(object)(from renderer in ((Component)root).GetComponentsInChildren<SkinnedMeshRenderer>()
			where ((UnityEngine.Object)renderer).name.ToLower().StartsWith("cm_o_tang") || ((UnityEngine.Object)renderer).name == "cf_o_tang"
			select renderer into tongue
			where ((Renderer)tongue).enabled
			select tongue).ToArray();
	}

	public static Transform GetHead(ChaControl human)
	{
		return ((ChaInfo)human).objHead.GetComponentsInParent<Transform>().First((Transform t) => ((UnityEngine.Object)t).name.StartsWith("c") && ((UnityEngine.Object)t).name.ToLower().Contains("j_head"));
	}

	public static Transform GetEyes(ChaControl human)
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		Transform val = ((ChaInfo)human).objHeadBone.transform.Descendants().FirstOrDefault((Transform t) => ((UnityEngine.Object)t).name.StartsWith("c") && ((UnityEngine.Object)t).name.ToLower().EndsWith("j_faceup_tz"));
		if ((val == null))
		{
			VRLog.Info("Creating eyes");
			val = new GameObject("cf_j_faceup_tz").transform;
			val.SetParent(GetHead(human), false);
			((Component)val).transform.localPosition = new Vector3(0f, 0.07f, 0.05f);
		}
		else
		{
			VRLog.Info("found eyes");
		}
		return val;
	}

	private void SetVisibility(bool visible)
	{
		if (visible)
		{
			if (hidden)
			{
				foreach (Renderer renderer in rendererList)
				{
					if ((renderer != null))
					{
						renderer.enabled = true;
					}
				}
				Renderer[] tongues = m_tongues;
				foreach (Renderer val in tongues)
				{
					if ((val != null))
					{
						val.enabled = true;
					}
				}
			}
		}
		else if (!hidden)
		{
			m_tongues = (Renderer[])(object)(from renderer in ((Component)root).GetComponentsInChildren<SkinnedMeshRenderer>()
				where ((UnityEngine.Object)renderer).name.StartsWith("cm_o_tang") || ((UnityEngine.Object)renderer).name == "cf_o_tang"
				select renderer into tongue
				where ((Renderer)tongue).enabled
				select tongue).ToArray();
			rendererList.Clear();
			foreach (Renderer item in from renderer in ((Component)headTransform).GetComponentsInChildren<Renderer>()
				where renderer.enabled
				select renderer)
			{
				rendererList.Add(item);
				item.enabled = false;
			}
			Renderer[] tongues = m_tongues;
			for (int i = 0; i < tongues.Length; i++)
			{
				tongues[i].enabled = false;
			}
		}
		hidden = !visible;
	}
}

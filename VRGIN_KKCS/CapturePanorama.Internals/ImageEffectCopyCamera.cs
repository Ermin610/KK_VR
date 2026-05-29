using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CapturePanorama.Internals;

internal class ImageEffectCopyCamera : MonoBehaviour
{
	public struct InstanceMethodPair
	{
		public object Instance;

		public MethodInfo Method;
	}

	public List<InstanceMethodPair> onRenderImageMethods = new List<InstanceMethodPair>();

	private RenderTexture[] temp = (RenderTexture[])(object)new RenderTexture[2];

	public static List<InstanceMethodPair> GenerateMethodList(Camera camToCopy)
	{
		List<InstanceMethodPair> list = new List<InstanceMethodPair>();
		MonoBehaviour[] components = ((Component)camToCopy).gameObject.GetComponents<MonoBehaviour>();
		foreach (MonoBehaviour val in components)
		{
			if (((Behaviour)val).enabled)
			{
				MethodInfo method = ((object)val).GetType().GetMethod("OnRenderImage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[2]
				{
					typeof(RenderTexture),
					typeof(RenderTexture)
				}, null);
				if (method != null)
				{
					InstanceMethodPair item = default(InstanceMethodPair);
					item.Instance = val;
					item.Method = method;
					list.Add(item);
				}
			}
		}
		return list;
	}

	private void OnDestroy()
	{
		for (int i = 0; i < temp.Length; i++)
		{
			if (temp[i] != null)
			{
				Object.Destroy((Object)(object)temp[i]);
			}
			temp[i] = null;
		}
	}

	private void OnRenderImage(RenderTexture src, RenderTexture dest)
	{
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Expected O, but got Unknown
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		int num = Math.Max(src.depth, dest.depth);
		for (int i = 0; i < temp.Length; i++)
		{
			if (onRenderImageMethods.Count > i + 1)
			{
				if (temp[i] != null && (((Texture)temp[i]).width != ((Texture)dest).width || ((Texture)temp[i]).height != ((Texture)dest).height || temp[i].depth != num || temp[i].format != dest.format))
				{
					Object.Destroy((Object)(object)temp[i]);
					temp[i] = null;
				}
				if (temp[i] == null)
				{
					temp[i] = new RenderTexture(((Texture)dest).width, ((Texture)dest).height, num, dest.format);
				}
			}
		}
		List<RenderTexture> list = new List<RenderTexture>();
		list.Add(src);
		for (int j = 0; j < onRenderImageMethods.Count - 1; j++)
		{
			list.Add((j % 2 == 0) ? temp[0] : temp[1]);
		}
		list.Add(dest);
		for (int k = 0; k < onRenderImageMethods.Count; k++)
		{
			onRenderImageMethods[k].Method.Invoke(onRenderImageMethods[k].Instance, new object[2]
			{
				list[k],
				list[k + 1]
			});
		}
	}
}

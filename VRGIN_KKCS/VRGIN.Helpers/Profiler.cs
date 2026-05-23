using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using VRGIN.Core;

namespace VRGIN.Helpers;

public class Profiler : ProtectedBehaviour
{
	public delegate void Callback();

	private const int DEFAULT_SAMPLE_COUNT = 30;

	private const float INTERVAL_TIME = 0.01f;

	private Callback _Callback;

	private double _CurrentInterval;

	public static void FindHotPaths(Callback callback)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)GameObject.Find("Profiler")))
		{
			new GameObject("Profiler").AddComponent<Profiler>()._Callback = callback;
		}
	}

	protected override void OnStart()
	{
		base.OnStart();
		((MonoBehaviour)this).StartCoroutine(Measure());
	}

	private IEnumerator Measure()
	{
		List<GameObject> queue = (from n in UnityHelper.GetRootNodes().Except((IEnumerable<GameObject>)(object)new GameObject[1] { ((Component)this).gameObject })
			where !((Object)n).name.StartsWith("VRGIN") && !((Object)n).name.StartsWith("[")
			select n).ToList();
		yield return ((MonoBehaviour)this).StartCoroutine(MeasureFramerate(30));
		double startInterval = _CurrentInterval;
		VRLog.Info("Starting to profile! This might take a while...");
		while (queue.Count > 0)
		{
			GameObject obj = queue.First();
			queue.RemoveAt(0);
			if (!obj.activeInHierarchy)
			{
				continue;
			}
			obj.SetActive(false);
			yield return ((MonoBehaviour)this).StartCoroutine(MeasureFramerate(30));
			obj.SetActive(true);
			double num = startInterval / _CurrentInterval;
			VRLog.Info("{0}{1}: {2:0.00}", string.Join("", Enumerable.Repeat(" ", obj.transform.Depth()).ToArray()), ((Object)obj).name, num);
			if (num > 1.149999976158142)
			{
				queue.InsertRange(0, obj.Children());
				foreach (Behaviour component in from c in obj.GetComponents<Behaviour>()
					where c.enabled
					select c)
				{
					component.enabled = false;
					yield return ((MonoBehaviour)this).StartCoroutine(MeasureFramerate(30));
					component.enabled = true;
					num = startInterval / _CurrentInterval;
					VRLog.Info("{0}{1} [{2}]: {3:0.000}", string.Join("", Enumerable.Repeat(" ", obj.transform.Depth()).ToArray()), ((Object)obj).name, ((object)component).GetType().Name, num);
				}
			}
			yield return null;
		}
		VRLog.Info("Done!");
		_Callback();
		Object.Destroy((Object)(object)((Component)this).gameObject);
	}

	private IEnumerator MeasureFramerate(int sampleCount)
	{
		yield return (object)new WaitForSeconds(0.01f);
		long[] samples = new long[sampleCount];
		yield return null;
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		for (int i = 0; i < sampleCount; i++)
		{
			stopwatch.Reset();
			stopwatch.Start();
			yield return null;
			samples[i] = stopwatch.ElapsedMilliseconds;
		}
		_CurrentInterval = samples.Average();
		yield return (object)new WaitForSeconds(0.01f);
	}
}

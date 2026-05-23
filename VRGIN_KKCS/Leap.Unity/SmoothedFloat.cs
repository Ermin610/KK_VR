using System;
using UnityEngine;

namespace Leap.Unity;

[Serializable]
public class SmoothedFloat
{
	public float value;

	public float delay;

	public bool reset = true;

	public float average_dt;

	public void SetBlend(float blend, float deltaTime = 1f)
	{
		delay = deltaTime * blend / (1f - blend);
	}

	public float Update(float input, float deltaTime = 1f)
	{
		if (deltaTime > 0f && !reset)
		{
			float num = delay / deltaTime;
			float num2 = num / (1f + num);
			float num3 = Mathf.Lerp(value, input, 1f - num2);
			float num4 = Mathf.Abs(num3 - value);
			if (average_dt * 10f < num3 - value)
			{
				Debug.Log((object)"Average dt was excessive!");
			}
			average_dt = Mathf.Lerp(average_dt, num4, 1f - num2);
			value = num3;
		}
		else
		{
			value = input;
			reset = false;
		}
		return value;
	}
}

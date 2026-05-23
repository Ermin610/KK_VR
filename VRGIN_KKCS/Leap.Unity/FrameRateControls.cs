using UnityEngine;

namespace Leap.Unity;

public class FrameRateControls : MonoBehaviour
{
	public int targetRenderRate = 60;

	public int targetRenderRateStep = 1;

	public int fixedPhysicsRate = 50;

	public int fixedPhysicsRateStep = 1;

	public KeyCode unlockRender = (KeyCode)303;

	public KeyCode unlockPhysics = (KeyCode)304;

	public KeyCode decrease = (KeyCode)274;

	public KeyCode increase = (KeyCode)273;

	public KeyCode resetRate = (KeyCode)8;

	private void Awake()
	{
		if (QualitySettings.vSyncCount != 0)
		{
			Debug.LogWarning((object)("vSync will override target frame rate. vSyncCount = " + QualitySettings.vSyncCount));
		}
		Application.targetFrameRate = targetRenderRate;
		Time.fixedDeltaTime = 1f / (float)fixedPhysicsRate;
	}

	private void Update()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		if (Input.GetKey(unlockRender))
		{
			if (Input.GetKeyDown(decrease) && targetRenderRate > targetRenderRateStep)
			{
				targetRenderRate -= targetRenderRateStep;
				Application.targetFrameRate = targetRenderRate;
			}
			if (Input.GetKeyDown(increase))
			{
				targetRenderRate += targetRenderRateStep;
				Application.targetFrameRate = targetRenderRate;
			}
			if (Input.GetKeyDown(resetRate))
			{
				ResetRender();
			}
		}
		if (Input.GetKey(unlockPhysics))
		{
			if (Input.GetKeyDown(decrease) && fixedPhysicsRate > fixedPhysicsRateStep)
			{
				fixedPhysicsRate -= fixedPhysicsRateStep;
				Time.fixedDeltaTime = 1f / (float)fixedPhysicsRate;
			}
			if (Input.GetKeyDown(increase))
			{
				fixedPhysicsRate += fixedPhysicsRateStep;
				Time.fixedDeltaTime = 1f / (float)fixedPhysicsRate;
			}
			if (Input.GetKeyDown(resetRate))
			{
				ResetPhysics();
			}
		}
	}

	public void ResetRender()
	{
		targetRenderRate = 60;
		Application.targetFrameRate = -1;
	}

	public void ResetPhysics()
	{
		fixedPhysicsRate = 50;
		Time.fixedDeltaTime = 0.02f;
	}

	public void ResetAll()
	{
		ResetRender();
		ResetPhysics();
	}
}

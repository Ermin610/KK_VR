using UnityEngine;

namespace Leap.Unity;

public class PolyHand : HandModel
{
	public override ModelType HandModelType => ModelType.Graphics;

	public override bool SupportsEditorPersistence()
	{
		return true;
	}

	public override void InitHand()
	{
		SetPalmOrientation();
		for (int i = 0; i < fingers.Length; i++)
		{
			if (fingers[i] != null)
			{
				fingers[i].fingerType = (Finger.FingerType)i;
				fingers[i].InitFinger();
			}
		}
	}

	public override void UpdateHand()
	{
		SetPalmOrientation();
		for (int i = 0; i < fingers.Length; i++)
		{
			if (fingers[i] != null)
			{
				fingers[i].UpdateFinger();
			}
		}
	}

	protected void SetPalmOrientation()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		if (palm != null)
		{
			palm.position = GetPalmPosition();
			palm.rotation = GetPalmRotation();
		}
	}
}

using UnityEngine;

namespace VRGIN.Core;

public class PlayArea
{
	public float Scale { get; set; }

	public Vector3 Position { get; set; }

	public float Rotation { get; set; }

	public float Height
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return Position.y;
		}
		set
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			Position = new Vector3(Position.x, value, Position.z);
		}
	}

	public PlayArea()
	{
		Scale = 1f;
	}

	public void Apply()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		Quaternion val = Quaternion.Euler(0f, Rotation, 0f);
		SteamVR_Camera steamCam = VR.Camera.SteamCam;
		steamCam.origin.position = Position - val * new Vector3(((Component)steamCam.head).transform.localPosition.x, 0f, ((Component)steamCam.head).transform.localPosition.z) * Scale;
		steamCam.origin.rotation = val;
		VR.Settings.IPDScale = Scale;
	}

	public void Reset()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		Position = new Vector3(VR.Camera.Head.position.x, VR.Camera.Origin.position.y, VR.Camera.Head.position.z);
		Scale = VR.Settings.IPDScale;
		Quaternion rotation = VR.Camera.Origin.rotation;
		Rotation = rotation.eulerAngles.y;
	}
}

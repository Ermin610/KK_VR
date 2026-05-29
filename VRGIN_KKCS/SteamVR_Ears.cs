using UnityEngine;
using UnityEngine.Events;
using Valve.VR;

[RequireComponent(typeof(AudioListener))]
public class SteamVR_Ears : MonoBehaviour
{
	public SteamVR_Camera vrcam;

	private bool usingSpeakers;

	private Quaternion offset;

	private void OnNewPosesApplied()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		Transform origin = vrcam.origin;
		Quaternion val = ((origin != null) ? origin.rotation : Quaternion.identity);
		((Component)this).transform.rotation = val * offset;
	}

	private void OnEnable()
	{
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		usingSpeakers = false;
		CVRSettings settings = OpenVR.Settings;
		if (settings != null)
		{
			EVRSettingsError peError = EVRSettingsError.None;
			if (settings.GetBool("steamvr", "usingSpeakers", ref peError))
			{
				usingSpeakers = true;
				float @float = settings.GetFloat("steamvr", "speakersForwardYawOffsetDegrees", ref peError);
				offset = Quaternion.Euler(0f, @float, 0f);
			}
		}
		if (usingSpeakers)
		{
			SteamVR_Events.NewPosesApplied.Listen(new UnityAction(OnNewPosesApplied));
		}
	}

	private void OnDisable()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		if (usingSpeakers)
		{
			SteamVR_Events.NewPosesApplied.Remove(new UnityAction(OnNewPosesApplied));
		}
	}
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRGIN.Core;
using VRGIN.Helpers;

namespace VRGIN.Controls;

public class RumbleManager : ProtectedBehaviour
{
	private const float MILLI_TO_SECONDS = 0.001f;

	public const float MIN_INTERVAL = 0.0050000004f;

	private HashSet<IRumbleSession> _RumbleSessions = new HashSet<IRumbleSession>();

	private float _LastImpulse;

	private Controller _Controller;

	protected override void OnStart()
	{
		base.OnStart();
		_Controller = ((Component)this).GetComponent<Controller>();
	}

	protected virtual void OnDisable()
	{
		_RumbleSessions.Clear();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (_RumbleSessions.Count <= 0)
		{
			return;
		}
		IRumbleSession rumbleSession = _RumbleSessions.Max();
		float num = Time.unscaledTime - _LastImpulse;
		if (!_Controller.Tracking.isValid || !(num >= rumbleSession.MilliInterval * 0.001f) || !(num > 0.0050000004f))
		{
			return;
		}
		if (rumbleSession.IsOver)
		{
			_RumbleSessions.Remove(rumbleSession);
			return;
		}
		if (VR.Settings.Rumble)
		{
			SteamVR_Controller.Input((int)_Controller.Tracking.index).TriggerHapticPulse(rumbleSession.MicroDuration);
		}
		_LastImpulse = Time.unscaledTime;
		rumbleSession.Consume();
	}

	public void StartRumble(IRumbleSession session)
	{
		_RumbleSessions.Add(session);
	}

	internal void StopRumble(IRumbleSession session)
	{
		_RumbleSessions.Remove(session);
	}
}

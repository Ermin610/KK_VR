using System;
using System.Collections.Generic;
using System.Linq;
using Leap;
using UnityEngine;
using VRGIN.Controls;
using VRGIN.Controls.Tools;
using VRGIN.Core;
using VRGIN.Visuals;
using Valve.VR;

namespace VRGIN.Modes;

public class SeatedMode : ControlMode
{
	private static bool _IsFirstStart = true;

	protected GUIMonitor Monitor;

	protected IActor LockTarget;

	protected ImpersonationMode LockMode;

	public override IEnumerable<Type> Tools => base.Tools.Concat(new Type[1] { typeof(MenuTool) });

	public override ETrackingUniverseOrigin TrackingOrigin => ETrackingUniverseOrigin.TrackingUniverseSeated;

	protected override void OnStart()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		base.OnStart();
		if (_IsFirstStart)
		{
			((Component)VR.Camera.SteamCam.origin).transform.position = new Vector3(0f, 0f, 0f);
			Recenter();
			_IsFirstStart = false;
		}
		Monitor = GUIMonitor.Create();
		((Component)Monitor).transform.SetParent(VR.Camera.SteamCam.origin, false);
		OpenVR.ChaperoneSetup.SetWorkingPlayAreaSize(1000f, 1000f);
	}

	protected override void OnEnable()
	{
		base.OnEnable();
	}

	protected override void OnDisable()
	{
		base.OnDisable();
	}

	private void OnLeapConnect(object sender, ConnectionEventArgs e)
	{
		ChangeModeOnControllersDetected();
	}

	protected override void OnUpdate()
	{
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		base.OnUpdate();
		if (!VR.Camera.HasValidBlueprint || !Object.op_Implicit((Object)(object)VR.Camera.Blueprint))
		{
			return;
		}
		if (LockTarget != null && LockTarget.IsValid)
		{
			((Component)VR.Camera.Blueprint).transform.position = LockTarget.Eyes.position;
			if (LockMode == ImpersonationMode.Approximately)
			{
				((Component)VR.Camera.Blueprint).transform.eulerAngles = new Vector3(0f, LockTarget.Eyes.eulerAngles.y, 0f);
			}
			else
			{
				((Component)VR.Camera.Blueprint).transform.rotation = LockTarget.Eyes.rotation;
			}
		}
		((Component)VR.Camera.SteamCam.origin).transform.position = ((Component)VR.Camera.Blueprint).transform.position;
		if (VR.Settings.PitchLock && LockTarget == null)
		{
			((Component)VR.Camera.SteamCam.origin).transform.eulerAngles = new Vector3(0f, ((Component)VR.Camera.Blueprint).transform.eulerAngles.y, 0f);
			CorrectRotationLock();
		}
		else
		{
			((Component)VR.Camera.SteamCam.origin).transform.rotation = ((Component)VR.Camera.Blueprint).transform.rotation;
		}
	}

	protected virtual void SyncCameras()
	{
	}

	protected virtual void CorrectRotationLock()
	{
	}

	public override void Impersonate(IActor actor, ImpersonationMode mode)
	{
		base.Impersonate(actor, mode);
		SyncCameras();
		LockTarget = actor;
		LockMode = mode;
		Recenter();
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		Object.Destroy((Object)(object)((Component)Monitor).gameObject);
	}

	protected override IEnumerable<IShortcut> CreateShortcuts()
	{
		return new List<IShortcut>
		{
			new KeyboardShortcut(VR.Shortcuts.GUIRaise, MoveGUI(0.1f)),
			new KeyboardShortcut(VR.Shortcuts.GUILower, MoveGUI(-0.1f)),
			new KeyboardShortcut(VR.Shortcuts.GUIIncreaseAngle, delegate
			{
				VR.Settings.Angle += Time.deltaTime * 50f;
			}),
			new KeyboardShortcut(VR.Shortcuts.GUIDecreaseAngle, delegate
			{
				VR.Settings.Angle -= Time.deltaTime * 50f;
			}),
			new KeyboardShortcut(VR.Shortcuts.GUIIncreaseDistance, delegate
			{
				VR.Settings.Distance += Time.deltaTime * 0.1f;
			}),
			new KeyboardShortcut(VR.Shortcuts.GUIDecreaseDistance, delegate
			{
				VR.Settings.Distance -= Time.deltaTime * 0.1f;
			}),
			new KeyboardShortcut(VR.Shortcuts.GUIRotateLeft, delegate
			{
				VR.Settings.Rotation += Time.deltaTime * 50f;
			}),
			new KeyboardShortcut(VR.Shortcuts.GUIRotateRight, delegate
			{
				VR.Settings.Rotation -= Time.deltaTime * 50f;
			}),
			new KeyboardShortcut(VR.Shortcuts.GUIChangeProjection, ChangeProjection),
			new MultiKeyboardShortcut(VR.Shortcuts.ToggleRotationLock, ToggleRotationLock),
			new MultiKeyboardShortcut(VR.Shortcuts.ImpersonateApproximately, delegate
			{
				if (LockTarget == null || !LockTarget.IsValid)
				{
					Impersonate(VR.Interpreter.FindNextActorToImpersonate(), ImpersonationMode.Approximately);
				}
				else
				{
					Impersonate(null);
				}
			}),
			new MultiKeyboardShortcut(VR.Shortcuts.ImpersonateExactly, delegate
			{
				if (LockTarget == null || !LockTarget.IsValid)
				{
					Impersonate(VR.Interpreter.FindNextActorToImpersonate(), ImpersonationMode.Exactly);
				}
				else
				{
					Impersonate(null);
				}
			}),
			new MultiKeyboardShortcut(VR.Shortcuts.ResetView, Recenter)
		}.Concat(base.CreateShortcuts());
	}

	private void ToggleRotationLock()
	{
		SyncCameras();
		VR.Settings.PitchLock = !VR.Settings.PitchLock;
	}

	private void ChangeProjection()
	{
		VR.Settings.Projection = (GUIMonitor.CurvinessState)((int)(VR.Settings.Projection + 1) % Enum.GetValues(typeof(GUIMonitor.CurvinessState)).Length);
	}

	public void Recenter()
	{
		VRLog.Info("Recenter");
		OpenVR.System.ResetSeatedZeroPose();
	}

	protected Action MoveGUI(float speed)
	{
		return delegate
		{
			VR.Settings.OffsetY += speed * Time.deltaTime;
		};
	}
}

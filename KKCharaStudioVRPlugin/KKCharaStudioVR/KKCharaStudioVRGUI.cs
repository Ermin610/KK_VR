using System;
using System.Collections.Generic;
using UnityEngine;
using VRUtil;
using VRGIN.Core;

namespace KKCharaStudioVR;

public class KKCharaStudioVRGUI : MonoBehaviour
{
	private int windowID = 8731;

	private Rect windowRect = new Rect((float)(Screen.width - 250), (float)(Screen.height - 300), 250f, 300f);

	private string windowTitle = "KKCharaStudioVR Settings";

	private Dictionary<string, GUIStyle> styleBackup = new Dictionary<string, GUIStyle>();

	private void OnGUI()
	{
		if (VRIMGUIUtil.VRGUISkin != null)
		{
			GUI.skin = VRIMGUIUtil.VRGUISkin;
		}
		windowRect = GUI.Window(windowID, windowRect, FuncWindowGUI, windowTitle);
	}

	private void FuncWindowGUI(int winID)
	{
		styleBackup = new Dictionary<string, GUIStyle>();
		BackupGUIStyle("Button");
		BackupGUIStyle("Label");
		BackupGUIStyle("Toggle");
		try
		{
			if ((int)Event.current.type == 0)
			{
				GUI.FocusControl("");
				GUI.FocusWindow(winID);
			}
			GUI.enabled = true;
			GUIStyle style = GUI.skin.GetStyle("Button");
			style.normal.textColor = Color.white;
			style.alignment = (TextAnchor)4;
			GUIStyle style2 = GUI.skin.GetStyle("Label");
			style2.normal.textColor = Color.white;
			style2.alignment = (TextAnchor)3;
			style2.wordWrap = false;
			GUIStyle style3 = GUI.skin.GetStyle("Toggle");
			style3.normal.textColor = Color.white;
			style3.onNormal.textColor = Color.white;

			GUILayout.BeginVertical(new GUILayoutOption[0]);

			KKCharaStudioVRSettings settings = VR.Manager.Context.Settings as KKCharaStudioVRSettings;
			if (settings != null)
			{
				GUILayout.Label($"Locomotion Speed: {settings.LocomotionSpeed:F1}");
				settings.LocomotionSpeed = GUILayout.HorizontalSlider(settings.LocomotionSpeed, 0.5f, 10f);

				GUILayout.Label($"Snap Turn Angle: {settings.SnapTurnAngle:F0}");
				settings.SnapTurnAngle = GUILayout.HorizontalSlider(settings.SnapTurnAngle, 15f, 180f);

				settings.SmoothTurnEnabled = GUILayout.Toggle(settings.SmoothTurnEnabled, "Smooth Turn Enabled");
				
				if (settings.SmoothTurnEnabled)
				{
					GUILayout.Label($"Smooth Turn Speed: {settings.SmoothTurnSpeed:F0}");
					settings.SmoothTurnSpeed = GUILayout.HorizontalSlider(settings.SmoothTurnSpeed, 30f, 180f);
				}
			}

			GUILayout.Space(10);

			if (GUILayout.Button("Reset Camera Position"))
			{
				if (VRCameraMoveHelper.Instance != null)
				{
					VRCameraMoveHelper.Instance.MoveToCurrent();
				}
			}

			if (GUILayout.Button("Hide/Show All UI"))
			{
				VRQuickActions actions = ((Component)this).gameObject.GetComponent<VRQuickActions>();
				if (actions != null)
				{
					actions.ForceHideUI();
				}
			}

			GUILayout.EndVertical();
			GUI.DragWindow();
		}
		catch (Exception value)
		{
			Console.WriteLine(value);
		}
		finally
		{
			RestoreGUIStyle("Button");
			RestoreGUIStyle("Label");
			RestoreGUIStyle("Toggle");
		}
	}

	private void BackupGUIStyle(string name)
	{
		if (GUI.skin == null) return;
		GUIStyle style = GUI.skin.GetStyle(name);
		if (style == null) return;
		GUIStyle value = new GUIStyle(style);
		styleBackup.Add(name, value);
	}

	private void RestoreGUIStyle(string name)
	{
		if (styleBackup.ContainsKey(name) && GUI.skin != null)
		{
			GUIStyle val = styleBackup[name];
			GUIStyle style = GUI.skin.GetStyle(name);
			if (style != null)
			{
				style.normal.textColor = val.normal.textColor;
				style.alignment = val.alignment;
				style.wordWrap = val.wordWrap;
			}
		}
	}
}

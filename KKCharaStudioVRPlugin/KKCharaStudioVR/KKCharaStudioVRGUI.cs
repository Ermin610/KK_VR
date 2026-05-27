using System;
using System.Collections.Generic;
using UnityEngine;
using VRUtil;
using VRGIN.Core;

namespace KKCharaStudioVR;

public class KKCharaStudioVRGUI : MonoBehaviour
{
	private int windowID = 8731;
	private Rect windowRect = new Rect((float)(Screen.width - 250), (float)(Screen.height - 400), 250f, 10f);
	private string windowTitle = "KKCharaStudioVR Settings";
	private Dictionary<string, GUIStyle> styleBackup = new Dictionary<string, GUIStyle>();

	private bool _desktopCoverEnabled = false;
	private Camera _coverCamera;

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			SetDesktopCover(!_desktopCoverEnabled);
		}
	}

	private void OnDestroy()
	{
		if (_coverCamera != null && _coverCamera.gameObject != null)
		{
			Destroy(_coverCamera.gameObject);
		}
	}

	private void SetDesktopCover(bool enabled)
	{
		_desktopCoverEnabled = enabled;
		if (_coverCamera == null && enabled)
		{
			CreateCoverCamera();
		}
		if (_coverCamera != null)
		{
			_coverCamera.enabled = enabled;
		}

		try
		{
			UnityEngine.VR.VRSettings.showDeviceView = !enabled;
		}
		catch (Exception e)
		{
			VRLog.Warn($"Failed to set showDeviceView: {e.Message}");
		}
	}

	private void CreateCoverCamera()
	{
		GameObject camObj = new GameObject("VRDesktopCoverCamera");
		GameObject.DontDestroyOnLoad(camObj);

		_coverCamera = camObj.AddComponent<Camera>();
		_coverCamera.depth = 99999f;
		_coverCamera.clearFlags = CameraClearFlags.Color;
		_coverCamera.backgroundColor = Color.black;
		_coverCamera.cullingMask = 0; // Render nothing
		_coverCamera.allowHDR = false;
		_coverCamera.allowMSAA = false;
		_coverCamera.useOcclusionCulling = false;
		_coverCamera.enabled = false;
	}

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

			GUIStyle headerStyle = new GUIStyle(style2);
			headerStyle.fontStyle = FontStyle.Bold;
			headerStyle.alignment = TextAnchor.MiddleCenter;

			KKCharaStudioVRSettings settings = VR.Manager.Context.Settings as KKCharaStudioVRSettings;
			if (settings != null)
			{
				GUILayout.Label("--- 移动设置 ---", headerStyle);
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

				GUILayout.Space(5);
				GUILayout.Label("--- UI 设置 ---", headerStyle);
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

				GUILayout.Space(5);
				GUILayout.Label("--- 手部设置 ---", headerStyle);
				settings.HandModelEnabled = GUILayout.Toggle(settings.HandModelEnabled, "Hand Model Enabled");
				if (settings.HandModelEnabled)
				{
					GUILayout.Label($"Hand Alpha: {settings.HandModelAlpha:F2}");
					settings.HandModelAlpha = GUILayout.HorizontalSlider(settings.HandModelAlpha, 0.05f, 1f);
					GUILayout.Label($"Hand Scale: {settings.HandModelScale:F2}");
					settings.HandModelScale = GUILayout.HorizontalSlider(settings.HandModelScale, 0.5f, 2f);
					
					GUILayout.Label($"Hand Offset X (L/R): {settings.HandOffsetX:F3}");
					settings.HandOffsetX = GUILayout.HorizontalSlider(settings.HandOffsetX, -0.2f, 0.2f);
					GUILayout.Label($"Hand Offset Y (U/D): {settings.HandOffsetY:F3}");
					settings.HandOffsetY = GUILayout.HorizontalSlider(settings.HandOffsetY, -0.2f, 0.2f);
					GUILayout.Label($"Hand Offset Z (F/B): {settings.HandOffsetZ:F3}");
					settings.HandOffsetZ = GUILayout.HorizontalSlider(settings.HandOffsetZ, -0.2f, 0.2f);
					
					GUILayout.Label($"Hand Rot Pitch (X): {settings.HandRotPitch:F0}");
					settings.HandRotPitch = GUILayout.HorizontalSlider(settings.HandRotPitch, -90f, 90f);
					GUILayout.Label($"Hand Rot Yaw (Y): {settings.HandRotYaw:F0}");
					settings.HandRotYaw = GUILayout.HorizontalSlider(settings.HandRotYaw, -90f, 90f);
					GUILayout.Label($"Hand Rot Roll (Z): {settings.HandRotRoll:F0}");
					settings.HandRotRoll = GUILayout.HorizontalSlider(settings.HandRotRoll, -90f, 90f);
				}
				
				GUILayout.Space(5);
				GUILayout.Label("--- 物理设置 ---", headerStyle);
				settings.DynamicBoneCollisionEnabled = GUILayout.Toggle(settings.DynamicBoneCollisionEnabled, "DynamicBone Collision");
				if (settings.DynamicBoneCollisionEnabled)
				{
					GUILayout.Label($"Collider Radius: {settings.ColliderRadius:F3}");
					settings.ColliderRadius = GUILayout.HorizontalSlider(settings.ColliderRadius, 0.005f, 0.1f);
				}

				settings.HapticFeedbackEnabled = GUILayout.Toggle(settings.HapticFeedbackEnabled, "Haptic Feedback");
				if (settings.HapticFeedbackEnabled)
				{
					GUILayout.Label($"Haptic Intensity: {settings.HapticFeedbackIntensity:F2}");
					settings.HapticFeedbackIntensity = GUILayout.HorizontalSlider(settings.HapticFeedbackIntensity, 0.1f, 1f);
				}

				settings.ProximityGrabEnabled = GUILayout.Toggle(settings.ProximityGrabEnabled, "Proximity Grab");
				if (settings.ProximityGrabEnabled)
				{
					GUILayout.Label($"Grab Radius: {settings.ProximityGrabRadius:F2}m");
					settings.ProximityGrabRadius = GUILayout.HorizontalSlider(settings.ProximityGrabRadius, 0.05f, 0.25f);
				}

				GUILayout.Space(5);
				GUILayout.Label("--- 舒适设置 ---", headerStyle);
				settings.ComfortVignetteEnabled = GUILayout.Toggle(settings.ComfortVignetteEnabled, "Movement Vignette");
				if (settings.ComfortVignetteEnabled)
				{
					GUILayout.Label($"Vignette Radius: {settings.ComfortVignetteRadius:F2}");
					settings.ComfortVignetteRadius = GUILayout.HorizontalSlider(settings.ComfortVignetteRadius, 0.3f, 0.8f);
				}

				GUILayout.Space(5);
				GUILayout.Label("--- 高级设置 ---", headerStyle);
				settings.TwoHandScaleEnabled = GUILayout.Toggle(settings.TwoHandScaleEnabled, "Two-Hand World Scale");

				if (GUILayout.Button(_desktopCoverEnabled ? "Restore Desktop View (Space)" : "Cover Desktop View (Space)"))
				{
					SetDesktopCover(!_desktopCoverEnabled);
				}

				GUILayout.Space(10);
				GUILayout.BeginHorizontal();
				if (GUILayout.Button("Save Settings"))
				{
					settings.Save();
				}
				if (GUILayout.Button("Reset to Default"))
				{
					settings.LocomotionSpeed = 2.0f;
					settings.SnapTurnAngle = 45f;
					settings.SnapTurnCooldown = 0.3f;
					settings.SmoothTurnEnabled = false;
					settings.SmoothTurnSpeed = 90f;
					settings.HandModelEnabled = true;
					settings.HandModelAlpha = 0.3f;
					settings.HandModelScale = 1.0f;
					settings.HandOffsetX = 0f;
					settings.HandOffsetY = -0.01f;
					settings.HandOffsetZ = -0.04f;
					settings.HandRotPitch = 40f;
					settings.HandRotYaw = 0f;
					settings.HandRotRoll = 0f;
					settings.DynamicBoneCollisionEnabled = true;
					settings.ColliderRadius = 0.02f;
					settings.HapticFeedbackEnabled = true;
					settings.HapticFeedbackIntensity = 0.5f;
					settings.ProximityGrabEnabled = true;
					settings.ProximityGrabRadius = 0.12f;
					settings.ComfortVignetteEnabled = true;
					settings.ComfortVignetteRadius = 0.5f;
					settings.TwoHandScaleEnabled = true;
					settings.Save();
				}
				GUILayout.EndHorizontal();
			}

			if (GUILayout.Button("Close"))
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

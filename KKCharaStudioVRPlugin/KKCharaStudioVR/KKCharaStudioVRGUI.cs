using System;
using System.Collections.Generic;
using UnityEngine;

namespace KKCharaStudioVR;

public class KKCharaStudioVRGUI : MonoBehaviour
{
	private int windowID = 8731;

	private Rect windowRect = new Rect((float)(Screen.width - 150), (float)(Screen.height - 100), 150f, 100f);

	private string windowTitle = "KKCharaStudioVR";

	private Texture2D windowBG = new Texture2D(1, 1, (TextureFormat)5, false);

	private Dictionary<string, GUIStyle> styleBackup = new Dictionary<string, GUIStyle>();

	private void OnGUI()
	{
	}

	private void FuncWindowGUI(int winID)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
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
			GUILayout.BeginVertical((GUILayoutOption[])(object)new GUILayoutOption[0]);
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
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		GUIStyle value = new GUIStyle(GUI.skin.GetStyle(name));
		styleBackup.Add(name, value);
	}

	private void RestoreGUIStyle(string name)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		if (styleBackup.ContainsKey(name))
		{
			GUIStyle val = styleBackup[name];
			GUIStyle style = GUI.skin.GetStyle(name);
			style.normal.textColor = val.normal.textColor;
			style.alignment = val.alignment;
			style.wordWrap = val.wordWrap;
		}
	}
}

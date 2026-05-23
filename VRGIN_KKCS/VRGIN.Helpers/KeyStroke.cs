using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VRGIN.Helpers;

public class KeyStroke
{
	private List<KeyCode> modifiers = new List<KeyCode>();

	private List<KeyCode> keys = new List<KeyCode>();

	private KeyCode[] MODIFIER_LIST = (KeyCode[])(object)new KeyCode[6]
	{
		(KeyCode)308,
		(KeyCode)307,
		(KeyCode)306,
		(KeyCode)305,
		(KeyCode)304,
		(KeyCode)303
	};

	public KeyStroke(string strokeString)
	{
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		string[] array = (from key in strokeString.ToUpper().Split('+', '-')
			select key.Trim()).ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i];
			switch (text)
			{
			case "CTRL":
				AddStroke((KeyCode)306);
				continue;
			case "ALT":
				AddStroke((KeyCode)308);
				continue;
			case "SHIFT":
				AddStroke((KeyCode)304);
				continue;
			}
			try
			{
				if (Regex.IsMatch(text, "^\\d$"))
				{
					text = "Alpha" + text;
				}
				if (Regex.IsMatch(text, "^(LEFT|RIGHT|UP|DOWN)$"))
				{
					text += "ARROW";
				}
				AddStroke((KeyCode)Enum.Parse(typeof(KeyCode), text, ignoreCase: true));
			}
			catch (Exception)
			{
				Console.WriteLine("FAILED TO PARSE KEY \"{0}\"", text);
			}
		}
		Init();
	}

	public KeyStroke(IEnumerable<KeyCode> strokes)
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		foreach (KeyCode stroke in strokes)
		{
			AddStroke(stroke);
		}
		Init();
	}

	private void Init()
	{
		if (modifiers.Count > 0 && keys.Count == 0)
		{
			keys.AddRange(modifiers);
			modifiers.Clear();
		}
	}

	private void AddStroke(KeyCode stroke)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		if (MODIFIER_LIST.Contains(stroke))
		{
			modifiers.Add(stroke);
		}
		else
		{
			keys.Add(stroke);
		}
	}

	public bool Check(KeyMode mode = KeyMode.PressDown)
	{
		if (modifiers.Count == 0 && keys.Count == 0)
		{
			return false;
		}
		if (modifiers.All((KeyCode key) => Input.GetKey(key)) && keys.All((KeyCode key) => (mode != KeyMode.Press) ? ((mode != 0) ? Input.GetKeyUp(key) : Input.GetKeyDown(key)) : Input.GetKey(key)))
		{
			return MODIFIER_LIST.Except(modifiers).All((KeyCode invalidModifier) => !Input.GetKey(invalidModifier));
		}
		return false;
	}

	public override string ToString()
	{
		return string.Join("+", modifiers.Select((KeyCode m) => ((object)(KeyCode)(ref m)).ToString()).Union(keys.Select((KeyCode k) => ((object)(KeyCode)(ref k)).ToString())).ToArray());
	}
}

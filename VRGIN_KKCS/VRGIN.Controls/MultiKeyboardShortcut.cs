using System;
using UnityEngine;
using VRGIN.Core;
using VRGIN.Helpers;

namespace VRGIN.Controls;

public class MultiKeyboardShortcut : IShortcut, IDisposable
{
	private const float WAIT_TIME = 0.5f;

	private int _Index;

	private float _Time;

	public KeyStroke[] KeyStrokes { get; private set; }

	public Action Action { get; private set; }

	public KeyMode CheckMode { get; private set; }

	public MultiKeyboardShortcut(KeyStroke[] keyStrokes, Action action, KeyMode checkMode = KeyMode.PressUp)
	{
		KeyStrokes = keyStrokes;
		Action = action;
		CheckMode = checkMode;
	}

	public MultiKeyboardShortcut(KeyStroke keyStroke1, KeyStroke keyStroke2, Action action, KeyMode checkMode = KeyMode.PressUp)
	{
		KeyStrokes = new KeyStroke[2] { keyStroke1, keyStroke2 };
		Action = action;
		CheckMode = checkMode;
	}

	public MultiKeyboardShortcut(XmlKeyStroke stroke, Action action)
	{
		KeyStrokes = stroke.GetKeyStrokes();
		Action = action;
		CheckMode = stroke.CheckMode;
	}

	public void Evaluate()
	{
		if (Time.time - _Time > 0.5f)
		{
			_Index = 0;
		}
		bool flag = _Index == KeyStrokes.Length - 1;
		KeyMode mode = ((!flag) ? KeyMode.PressUp : CheckMode);
		if (KeyStrokes[_Index].Check(mode))
		{
			if (flag)
			{
				Action();
				return;
			}
			_Index++;
			_Time = Time.unscaledTime;
		}
	}

	public void Dispose()
	{
	}
}

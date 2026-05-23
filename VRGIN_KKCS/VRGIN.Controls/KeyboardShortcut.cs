using System;
using System.Linq;
using VRGIN.Core;
using VRGIN.Helpers;

namespace VRGIN.Controls;

public class KeyboardShortcut : IShortcut, IDisposable
{
	public KeyStroke KeyStroke { get; private set; }

	public Action Action { get; private set; }

	public KeyMode CheckMode { get; private set; }

	public KeyboardShortcut(KeyStroke keyStroke, Action action, KeyMode checkMode = KeyMode.PressUp)
	{
		KeyStroke = keyStroke;
		Action = action;
		CheckMode = checkMode;
	}

	public KeyboardShortcut(XmlKeyStroke keyStroke, Action action)
	{
		KeyStroke = keyStroke.GetKeyStrokes().FirstOrDefault();
		Action = action;
		CheckMode = keyStroke.CheckMode;
	}

	public void Evaluate()
	{
		if (KeyStroke.Check(CheckMode))
		{
			Action();
		}
	}

	public void Dispose()
	{
	}
}

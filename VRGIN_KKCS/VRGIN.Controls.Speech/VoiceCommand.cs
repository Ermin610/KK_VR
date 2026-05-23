using System.Collections.Generic;
using System.Linq;

namespace VRGIN.Controls.Speech;

public class VoiceCommand
{
	public static readonly VoiceCommand ToggleMenu = new VoiceCommand("toggle menu");

	public static readonly VoiceCommand Impersonate = new VoiceCommand("impersonate");

	public static readonly VoiceCommand SaveSettings = new VoiceCommand("save settings");

	public static readonly VoiceCommand LoadSettings = new VoiceCommand("load settings");

	public static readonly VoiceCommand ResetSettings = new VoiceCommand("reset settings");

	public static readonly VoiceCommand IncreaseScale = new VoiceCommand("increase scale", "larger", "bigger");

	public static readonly VoiceCommand DecreaseScale = new VoiceCommand("decrease scale", "smaller");

	public List<string> Texts;

	protected VoiceCommand(params string[] texts)
	{
		Texts = texts.ToList();
	}

	public bool Matches(string text)
	{
		return Texts.Contains(text);
	}
}

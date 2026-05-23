using System.Linq;
using System.Xml.Serialization;
using VRGIN.Helpers;

namespace VRGIN.Core;

public class XmlKeyStroke
{
	[XmlAttribute("on")]
	public KeyMode CheckMode { get; private set; }

	[XmlText]
	public string Keys { get; private set; }

	public XmlKeyStroke()
	{
	}

	public XmlKeyStroke(string strokeString, KeyMode mode = KeyMode.PressUp)
	{
		CheckMode = mode;
		Keys = strokeString;
	}

	public KeyStroke[] GetKeyStrokes()
	{
		return (from part in Keys.Split(',', '|')
			select new KeyStroke(part.Trim())).ToArray();
	}
}

using System.Xml.Serialization;
using VRGIN.Helpers;

namespace VRGIN.Core;

public class Shortcuts
{
	public XmlKeyStroke ResetView = new XmlKeyStroke("F12");

	public XmlKeyStroke ChangeMode = new XmlKeyStroke("Ctrl+C, Ctrl+C");

	public XmlKeyStroke ShrinkWorld = new XmlKeyStroke("Alt + KeypadMinus", KeyMode.Press);

	public XmlKeyStroke EnlargeWorld = new XmlKeyStroke("Alt + KeypadPlus", KeyMode.Press);

	public XmlKeyStroke ToggleUserCamera = new XmlKeyStroke("Ctrl+C, Ctrl+V");

	public XmlKeyStroke SaveSettings = new XmlKeyStroke("Alt + S");

	public XmlKeyStroke LoadSettings = new XmlKeyStroke("Alt + L");

	public XmlKeyStroke ResetSettings = new XmlKeyStroke("Ctrl + Alt + L");

	public XmlKeyStroke ApplyEffects = new XmlKeyStroke("Ctrl + F5");

	[XmlElement("GUI.Raise")]
	public XmlKeyStroke GUIRaise = new XmlKeyStroke("KeypadMinus", KeyMode.Press);

	[XmlElement("GUI.Lower")]
	public XmlKeyStroke GUILower = new XmlKeyStroke("KeypadPlus", KeyMode.Press);

	[XmlElement("GUI.IncreaseAngle")]
	public XmlKeyStroke GUIIncreaseAngle = new XmlKeyStroke("Ctrl + KeypadMinus", KeyMode.Press);

	[XmlElement("GUI.DecreaseAngle")]
	public XmlKeyStroke GUIDecreaseAngle = new XmlKeyStroke("Ctrl + KeypadPlus", KeyMode.Press);

	[XmlElement("GUI.IncreaseDistance")]
	public XmlKeyStroke GUIIncreaseDistance = new XmlKeyStroke("Shift + KeypadMinus", KeyMode.Press);

	[XmlElement("GUI.DecreaseDistance")]
	public XmlKeyStroke GUIDecreaseDistance = new XmlKeyStroke("Shift + KeypadPlus", KeyMode.Press);

	[XmlElement("GUI.RotateRight")]
	public XmlKeyStroke GUIRotateRight = new XmlKeyStroke("Ctrl + Shift + KeypadMinus", KeyMode.Press);

	[XmlElement("GUI.RotateLeft")]
	public XmlKeyStroke GUIRotateLeft = new XmlKeyStroke("Ctrl + Shift + KeypadPlus", KeyMode.Press);

	[XmlElement("GUI.ChangeProjection")]
	public XmlKeyStroke GUIChangeProjection = new XmlKeyStroke("F4");

	public XmlKeyStroke ToggleRotationLock = new XmlKeyStroke("F5");

	public XmlKeyStroke ImpersonateApproximately = new XmlKeyStroke("Ctrl + X");

	public XmlKeyStroke ImpersonateExactly = new XmlKeyStroke("Ctrl + Shift + X");
}

using System;
using System.Xml.Serialization;
using UnityEngine;
using VRGIN.Controls.Speech;
using VRGIN.Core;
using VRGIN.Visuals;

namespace KKCharaStudioVR;

[XmlRoot("Context")]
public class ConfigurableContext : IVRManagerContext
{
	private DefaultMaterialPalette _Materials;

	private VRSettings _Settings;

	[XmlIgnore]
	public IMaterialPalette Materials => _Materials;

	[XmlIgnore]
	public VRSettings Settings => _Settings;

	[XmlIgnore]
	public Type VoiceCommandType => typeof(VoiceCommand);

	public bool ConfineMouse { get; set; }

	public bool EnforceDefaultGUIMaterials { get; set; }

	public bool GUIAlternativeSortingMode { get; set; }

	public float GuiFarClipPlane { get; set; }

	public string GuiLayer { get; set; }

	public float GuiNearClipPlane { get; set; }

	public int IgnoreMask { get; set; }

	public string InvisibleLayer { get; set; }

	public Color PrimaryColor { get; set; }

	public bool SimulateCursor { get; set; }

	public string UILayer { get; set; }

	public int UILayerMask { get; set; }

	public float UnitToMeter { get; set; }

	public float NearClipPlane { get; set; }

	public GUIType PreferredGUI { get; set; }

	public ConfigurableContext()
	{
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		_Materials = new DefaultMaterialPalette();
		_Settings = KKCharaStudioVRSettings.Load("KKCSVRSettings.xml");
		ConfineMouse = true;
		EnforceDefaultGUIMaterials = false;
		GUIAlternativeSortingMode = false;
		GuiLayer = "Default";
		GuiFarClipPlane = 1000f;
		GuiNearClipPlane = -1000f;
		IgnoreMask = 0;
		InvisibleLayer = "Ignore Raycast";
		PrimaryColor = Color.cyan;
		SimulateCursor = true;
		UILayer = "UI";
		UILayerMask = LayerMask.GetMask(new string[1] { UILayer });
		UnitToMeter = 1f;
		NearClipPlane = 0.001f;
		PreferredGUI = GUIType.IMGUI;
	}
}

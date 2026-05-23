using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.Serialization;
using UnityEngine;
using VRGIN.Visuals;

namespace VRGIN.Core;

[XmlRoot("Settings")]
public class VRSettings
{
	private VRSettings _OldSettings;

	private IDictionary<string, IList<EventHandler<PropertyChangedEventArgs>>> _Listeners = new Dictionary<string, IList<EventHandler<PropertyChangedEventArgs>>>();

	private float _Distance = 0.3f;

	private float _Angle = 170f;

	private float _IPDScale = 1f;

	private float _OffsetY;

	private float _Rotation;

	private bool _Rumble = true;

	private float _RenderScale = 1f;

	private bool _MirrorScreen;

	private bool _PitchLock = true;

	private GUIMonitor.CurvinessState _Projection = GUIMonitor.CurvinessState.Curved;

	private bool _SpeechRecognition;

	private string _Locale = "en-US";

	private bool _Leap;

	private bool _GrabRotationImmediateMode = true;

	private float _RotationMultiplier = 1f;

	private Shortcuts _Shortcuts = new Shortcuts();

	private CaptureConfig _CaptureConfig = new CaptureConfig();

	private bool _ApplyEffects = true;

	private string[] _EffectBlacklist = new string[2] { "BloomAndFlares", "DepthOfField" };

	private float _ArcWarpVelocity = 6f;

	private bool _UseStraightWarp;

	[XmlIgnore]
	public string Path { get; set; }

	[XmlComment("The distance between the camera and the GUI at [0,0,0] [seated]")]
	public float Distance
	{
		get
		{
			return _Distance;
		}
		set
		{
			_Distance = Mathf.Clamp(value, 0.1f, 10f);
			TriggerPropertyChanged("Distance");
		}
	}

	[XmlComment("The width of the arc the GUI takes up. [seated]")]
	public float Angle
	{
		get
		{
			return _Angle;
		}
		set
		{
			_Angle = Mathf.Clamp(value, 50f, 360f);
			TriggerPropertyChanged("Angle");
		}
	}

	[XmlComment("Scale of the camera. The higher, the more gigantic the player is.")]
	public float IPDScale
	{
		get
		{
			return _IPDScale;
		}
		set
		{
			_IPDScale = Mathf.Clamp(value, 0.01f, 50f);
			TriggerPropertyChanged("IPDScale");
		}
	}

	[XmlComment("The vertical offset of the GUI in meters. [seated]")]
	public float OffsetY
	{
		get
		{
			return _OffsetY;
		}
		set
		{
			_OffsetY = value;
			TriggerPropertyChanged("OffsetY");
		}
	}

	[XmlComment("Degrees the GUI is rotated around the y axis [seated]")]
	public float Rotation
	{
		get
		{
			return _Rotation;
		}
		set
		{
			_Rotation = value;
			TriggerPropertyChanged("Rotation");
		}
	}

	[XmlComment("Whether or not rumble is activated.")]
	public bool Rumble
	{
		get
		{
			return _Rumble;
		}
		set
		{
			_Rumble = value;
			TriggerPropertyChanged("Rumble");
		}
	}

	[XmlComment("The render scale of the renderer. Increase for better quality but less performance, decrease for more performance but poor quality. ]0..2]")]
	public float RenderScale
	{
		get
		{
			return _RenderScale;
		}
		set
		{
			_RenderScale = Mathf.Clamp(value, 0.1f, 4f);
			TriggerPropertyChanged("RenderScale");
		}
	}

	[XmlComment("Whether or not to display anything on the mirror screen. (Broken)")]
	public bool MirrorScreen
	{
		get
		{
			return _MirrorScreen;
		}
		set
		{
			_MirrorScreen = value;
			TriggerPropertyChanged("MirrorScreen");
		}
	}

	[XmlComment("Whether or not rotating around the horizontal axis is allowed.")]
	public bool PitchLock
	{
		get
		{
			return _PitchLock;
		}
		set
		{
			_PitchLock = value;
			TriggerPropertyChanged("PitchLock");
		}
	}

	[XmlComment("The curviness of the monitor in seated mode.")]
	public GUIMonitor.CurvinessState Projection
	{
		get
		{
			return _Projection;
		}
		set
		{
			_Projection = value;
			TriggerPropertyChanged("Projection");
		}
	}

	[XmlComment("Whether or not speech recognition is enabled. Refer to the manual for details.")]
	public bool SpeechRecognition
	{
		get
		{
			return _SpeechRecognition;
		}
		set
		{
			_SpeechRecognition = value;
			TriggerPropertyChanged("SpeechRecognition");
		}
	}

	[XmlComment("Locale to use for speech recognition. Make sure that you have installed the corresponding language pack. A dictionary file will automatically be generated at `UserData/dictionaries`.")]
	public string Locale
	{
		get
		{
			return _Locale;
		}
		set
		{
			_Locale = value;
			TriggerPropertyChanged("Locale");
		}
	}

	[XmlComment("Whether or not Leap Motion support is activated.")]
	public bool Leap
	{
		get
		{
			return _Leap;
		}
		set
		{
			_Leap = value;
			TriggerPropertyChanged("Leap");
		}
	}

	[XmlComment("Determines the rotation mode. If enabled, pulling the trigger while grabbing will immediately rotate you. When disabled, doing the same thing will let you 'drag' the view.")]
	public bool GrabRotationImmediateMode
	{
		get
		{
			return _GrabRotationImmediateMode;
		}
		set
		{
			_GrabRotationImmediateMode = value;
			TriggerPropertyChanged("GrabRotationImmediateMode");
		}
	}

	[XmlComment("How quickly the the view should rotate when doing so with the controllers.")]
	public float RotationMultiplier
	{
		get
		{
			return _RotationMultiplier;
		}
		set
		{
			_RotationMultiplier = value;
			TriggerPropertyChanged("RotationMultiplier");
		}
	}

	[XmlComment("Shortcuts used by VRGIN. Refer to https://docs.unity3d.com/ScriptReference/KeyCode.html for a list of available keys.")]
	public virtual Shortcuts Shortcuts
	{
		get
		{
			return _Shortcuts;
		}
		protected set
		{
			_Shortcuts = value;
		}
	}

	public CaptureConfig Capture
	{
		get
		{
			return _CaptureConfig;
		}
		protected set
		{
			_CaptureConfig = value;
		}
	}

	[XmlComment("Whether or not to copy image effects from the main camera.")]
	public bool ApplyEffects
	{
		get
		{
			return _ApplyEffects;
		}
		set
		{
			_ApplyEffects = value;
		}
	}

	[XmlComment("Blacklist of effects that are to be disable (because they don't look good in VR).")]
	public string[] EffectBlacklist
	{
		get
		{
			return _EffectBlacklist;
		}
		set
		{
			_EffectBlacklist = value;
		}
	}

	[XmlComment("Use WarpTool velocity.")]
	public float ArcWarpVelocity
	{
		get
		{
			return _ArcWarpVelocity;
		}
		set
		{
			_ArcWarpVelocity = Mathf.Clamp(3f, value, float.MaxValue);
			TriggerPropertyChanged("ArcWarpVelocity");
		}
	}

	[XmlComment("Use WarpTool Straight Mode.")]
	public bool UseStraightWarp
	{
		get
		{
			return _UseStraightWarp;
		}
		set
		{
			_UseStraightWarp = value;
			TriggerPropertyChanged("UseStraightWarp");
		}
	}

	public event EventHandler<PropertyChangedEventArgs> PropertyChanged = delegate
	{
	};

	public VRSettings()
	{
		PropertyChanged += Distribute;
		_OldSettings = MemberwiseClone() as VRSettings;
	}

	protected void TriggerPropertyChanged(string name)
	{
		this.PropertyChanged(this, new PropertyChangedEventArgs(name));
	}

	public virtual void Save()
	{
		Save(Path);
	}

	public virtual void Save(string path)
	{
		if (path != null)
		{
			XmlSerializer xmlSerializer = new XmlSerializer(GetType());
			using (FileStream fileStream = File.OpenWrite(path))
			{
				fileStream.SetLength(0L);
				xmlSerializer.Serialize(fileStream, this);
			}
			PostProcess(path);
			Path = path;
		}
		_OldSettings = MemberwiseClone() as VRSettings;
	}

	protected virtual void PostProcess(string path)
	{
		XDocument xDocument = XDocument.Load(path);
		foreach (XElement item in xDocument.Root.Elements())
		{
			PropertyInfo propertyInfo = FindProperty(item.Name.LocalName);
			if (propertyInfo != null && propertyInfo.GetCustomAttributes(typeof(XmlCommentAttribute), inherit: true).FirstOrDefault() is XmlCommentAttribute xmlCommentAttribute)
			{
				item.AddBeforeSelf(new XComment(" " + xmlCommentAttribute.Value + " "));
			}
		}
		xDocument.Save(path);
	}

	private PropertyInfo FindProperty(string name)
	{
		return GetType().FindMembers(MemberTypes.Property, BindingFlags.Instance | BindingFlags.Public, Type.FilterName, name).FirstOrDefault() as PropertyInfo;
	}

	public static T Load<T>(string path) where T : VRSettings
	{
		try
		{
			if (!File.Exists(path))
			{
				T val = Activator.CreateInstance<T>();
				val.Save(path);
				return val;
			}
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
			using FileStream stream = new FileStream(path, FileMode.Open);
			T obj = xmlSerializer.Deserialize(stream) as T;
			obj.Path = path;
			return obj;
		}
		catch (Exception ex)
		{
			VRLog.Error("Fatal exception occured while loading XML! (Make sure System.Xml exists!) {0}", ex);
			throw ex;
		}
	}

	public void AddListener(string property, EventHandler<PropertyChangedEventArgs> handler)
	{
		if (!_Listeners.ContainsKey(property))
		{
			_Listeners[property] = new List<EventHandler<PropertyChangedEventArgs>>();
		}
		_Listeners[property].Add(handler);
	}

	public void RemoveListener(string property, EventHandler<PropertyChangedEventArgs> handler)
	{
		if (_Listeners.ContainsKey(property))
		{
			_Listeners[property].Remove(handler);
		}
	}

	private void Distribute(object sender, PropertyChangedEventArgs e)
	{
		if (!_Listeners.ContainsKey(e.PropertyName))
		{
			_Listeners[e.PropertyName] = new List<EventHandler<PropertyChangedEventArgs>>();
		}
		foreach (EventHandler<PropertyChangedEventArgs> item in _Listeners[e.PropertyName])
		{
			item(sender, e);
		}
	}

	public void Reset()
	{
		VRSettings settings = Activator.CreateInstance(GetType()) as VRSettings;
		CopyFrom(settings);
	}

	public void Reload()
	{
		CopyFrom(_OldSettings);
	}

	public void CopyFrom(VRSettings settings)
	{
		foreach (string key in _Listeners.Keys)
		{
			PropertyInfo property = settings.GetType().GetProperty(key, BindingFlags.Instance | BindingFlags.Public);
			if (property != null)
			{
				try
				{
					property.SetValue(this, property.GetValue(settings, null), null);
				}
				catch (Exception obj)
				{
					VRLog.Warn(obj);
				}
			}
		}
	}
}

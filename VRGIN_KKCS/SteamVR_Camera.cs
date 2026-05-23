using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.VR;
using VRGIN.Core;

[RequireComponent(typeof(Camera))]
public class SteamVR_Camera : MonoBehaviour
{
	[SerializeField]
	private Transform _head;

	[SerializeField]
	private Transform _ears;

	public bool wireframe;

	private static Hashtable values;

	private const string eyeSuffix = " (eye)";

	private const string earsSuffix = " (ears)";

	private const string headSuffix = " (head)";

	private const string originSuffix = " (origin)";

	public Transform head => _head;

	public Transform offset => _head;

	public Transform origin => _head.parent;

	public Camera camera { get; private set; }

	public Transform ears => _ears;

	public static float sceneResolutionScale
	{
		get
		{
			return VRSettings.renderScale;
		}
		set
		{
			VRSettings.renderScale = value;
		}
	}

	public string baseName
	{
		get
		{
			if (!((Object)this).name.EndsWith(" (eye)"))
			{
				return ((Object)this).name;
			}
			return ((Object)this).name.Substring(0, ((Object)this).name.Length - " (eye)".Length);
		}
	}

	public Ray GetRay()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		return new Ray(_head.position, _head.forward);
	}

	private void OnDisable()
	{
		SteamVR_Render.Remove(this);
	}

	private void OnEnable()
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		if (SteamVR.instance == null)
		{
			if ((Object)(object)head != (Object)null)
			{
				((Behaviour)((Component)head).GetComponent<SteamVR_TrackedObject>()).enabled = false;
			}
			((Behaviour)this).enabled = false;
			return;
		}
		Transform transform = ((Component)this).transform;
		if ((Object)(object)head != (Object)(object)transform)
		{
			Expand();
			transform.parent = origin;
			while (head.childCount > 0)
			{
				head.GetChild(0).parent = transform;
			}
			head.parent = transform;
			head.localPosition = Vector3.zero;
			head.localRotation = Quaternion.identity;
			head.localScale = Vector3.one;
			((Component)head).gameObject.SetActive(false);
			_head = transform;
		}
		if ((Object)(object)ears == (Object)null)
		{
			SteamVR_Ears componentInChildren = ((Component)((Component)this).transform).GetComponentInChildren<SteamVR_Ears>();
			if ((Object)(object)componentInChildren != (Object)null)
			{
				_ears = ((Component)componentInChildren).transform;
			}
		}
		if ((Object)(object)ears != (Object)null)
		{
			((Component)ears).GetComponent<SteamVR_Ears>().vrcam = this;
		}
		SteamVR_Render.Add(this);
	}

	private void Awake()
	{
		camera = ((Component)this).GetComponent<Camera>();
		ForceLast();
	}

	public void ForceLast()
	{
		if (values != null)
		{
			foreach (DictionaryEntry value in values)
			{
				(value.Key as FieldInfo).SetValue(this, value.Value);
			}
			values = null;
			return;
		}
		Component[] components = ((Component)this).GetComponents<Component>();
		for (int i = 0; i < components.Length; i++)
		{
			SteamVR_Camera steamVR_Camera = components[i] as SteamVR_Camera;
			if ((Object)(object)steamVR_Camera != (Object)null && (Object)(object)steamVR_Camera != (Object)(object)this)
			{
				Object.DestroyImmediate((Object)(object)steamVR_Camera);
			}
		}
		components = ((Component)this).GetComponents<Component>();
		if (!((Object)(object)this != (Object)(object)components[components.Length - 1]))
		{
			return;
		}
		values = new Hashtable();
		FieldInfo[] fields = ((object)this).GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (FieldInfo fieldInfo in fields)
		{
			if (fieldInfo.IsPublic || fieldInfo.IsDefined(typeof(SerializeField), inherit: true))
			{
				values[fieldInfo] = fieldInfo.GetValue(this);
			}
		}
		GameObject gameObject = ((Component)this).gameObject;
		Object.DestroyImmediate((Object)(object)this);
		gameObject.AddComponent<SteamVR_Camera>().ForceLast();
	}

	public void Expand()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
		Transform val = ((Component)this).transform.parent;
		if ((Object)(object)val == (Object)null)
		{
			val = new GameObject(((Object)this).name + " (origin)").transform;
			val.localPosition = ((Component)this).transform.localPosition;
			val.localRotation = ((Component)this).transform.localRotation;
			val.localScale = ((Component)this).transform.localScale;
		}
		if ((Object)(object)head == (Object)null)
		{
			_head = new GameObject(((Object)this).name + " (head)", new Type[1] { typeof(SteamVR_TrackedObject) }).transform;
			head.parent = val;
			head.position = ((Component)this).transform.position;
			head.rotation = ((Component)this).transform.rotation;
			head.localScale = Vector3.one;
			((Component)head).tag = ((Component)this).tag;
		}
		if ((Object)(object)((Component)this).transform.parent != (Object)(object)head)
		{
			((Component)this).transform.parent = head;
			((Component)this).transform.localPosition = Vector3.zero;
			((Component)this).transform.localRotation = Quaternion.identity;
			((Component)this).transform.localScale = Vector3.one;
			while (((Component)this).transform.childCount > 0)
			{
				VRLog.Info("Timing change because not end of expnad");
			}
			((Component)this).transform.GetChild(0).parent = head;
			GUILayer component = ((Component)this).GetComponent<GUILayer>();
			if ((Object)(object)component != (Object)null)
			{
				Object.DestroyImmediate((Object)(object)component);
				((Component)head).gameObject.AddComponent<GUILayer>();
			}
			AudioListener component2 = ((Component)this).GetComponent<AudioListener>();
			if ((Object)(object)component2 != (Object)null)
			{
				Object.DestroyImmediate((Object)(object)component2);
				_ears = new GameObject(((Object)this).name + " (ears)", new Type[1] { typeof(SteamVR_Ears) }).transform;
				ears.parent = _head;
				ears.localPosition = Vector3.zero;
				ears.localRotation = Quaternion.identity;
				ears.localScale = Vector3.one;
			}
		}
		if (!((Object)this).name.EndsWith(" (eye)"))
		{
			((Object)this).name = ((Object)this).name + " (eye)";
		}
	}

	public void Collapse()
	{
		((Component)this).transform.parent = null;
		while (head.childCount > 0)
		{
			head.GetChild(0).parent = ((Component)this).transform;
		}
		GUILayer component = ((Component)head).GetComponent<GUILayer>();
		if ((Object)(object)component != (Object)null)
		{
			Object.DestroyImmediate((Object)(object)component);
			((Component)this).gameObject.AddComponent<GUILayer>();
		}
		if ((Object)(object)ears != (Object)null)
		{
			while (ears.childCount > 0)
			{
				ears.GetChild(0).parent = ((Component)this).transform;
			}
			Object.DestroyImmediate((Object)(object)((Component)ears).gameObject);
			_ears = null;
			((Component)this).gameObject.AddComponent(typeof(AudioListener));
		}
		if ((Object)(object)origin != (Object)null)
		{
			if (((Object)origin).name.EndsWith(" (origin)"))
			{
				Transform val = origin;
				while (val.childCount > 0)
				{
					val.GetChild(0).parent = val.parent;
				}
				Object.DestroyImmediate((Object)(object)((Component)val).gameObject);
			}
			else
			{
				((Component)this).transform.parent = origin;
			}
		}
		Object.DestroyImmediate((Object)(object)((Component)head).gameObject);
		_head = null;
		if (((Object)this).name.EndsWith(" (eye)"))
		{
			((Object)this).name = ((Object)this).name.Substring(0, ((Object)this).name.Length - " (eye)".Length);
		}
	}
}

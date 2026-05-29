using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
using VRGIN.Core;

namespace VRGIN.Helpers;

public static class UnityHelper
{
	private class RayDrawer : ProtectedBehaviour
	{
		private Ray _Ray;

		private Color _Color;

		private float _LastTouch;

		private LineRenderer Renderer;

		public static RayDrawer Create(Color color, Ray ray)
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			RayDrawer rayDrawer = new GameObject(string.Concat("Ray Drawer (", color, ")")).AddComponent<RayDrawer>();
			((Component)rayDrawer).gameObject.AddComponent<LineRenderer>();
			rayDrawer._Ray = ray;
			rayDrawer._Color = color;
			return rayDrawer;
		}

		public void Touch(Ray ray)
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			_LastTouch = Time.time;
			_Ray = ray;
			((Component)this).gameObject.SetActive(true);
		}

		protected override void OnStart()
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			base.OnStart();
			Renderer = ((Component)this).GetComponent<LineRenderer>();
			Renderer.SetColors(_Color, _Color);
			Renderer.SetVertexCount(2);
			Renderer.useWorldSpace = true;
			((Renderer)Renderer).material = VR.Context.Materials.Unlit;
			Renderer.SetWidth(0.01f, 0.01f);
		}

		protected override void OnUpdate()
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			//IL_0082: Unknown result type (might be due to invalid IL or missing references)
			//IL_008c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0091: Unknown result type (might be due to invalid IL or missing references)
			base.OnUpdate();
			Renderer.SetPosition(0, (Vector3.Distance(_Ray.origin, ((Component)VR.Camera).transform.position) < 0.3f) ? (_Ray.origin + _Ray.direction * 0.3f) : _Ray.origin);
			Renderer.SetPosition(1, _Ray.origin + _Ray.direction * 100f);
			CheckAge();
		}

		private void CheckAge()
		{
			if (Time.time - _LastTouch > 1f)
			{
				((Component)this).gameObject.SetActive(false);
			}
		}
	}

	private static AssetBundle _SteamVR;

	private static IDictionary<string, AssetBundle> _AssetBundles = new Dictionary<string, AssetBundle>();

	private static readonly MethodInfo _LoadFromMemory = typeof(AssetBundle).GetMethod("LoadFromMemory", new Type[1] { typeof(byte[]) });

	private static readonly MethodInfo _CreateFromMemory = typeof(AssetBundle).GetMethod("CreateFromMemoryImmediate", new Type[1] { typeof(byte[]) });

	private static Dictionary<Color, RayDrawer> _Rays = new Dictionary<Color, RayDrawer>();

	private static Dictionary<string, Transform> _DebugBalls = new Dictionary<string, Transform>();

	public static Shader GetShader(string name)
	{
		return UnityHelper.LoadFromAssetBundle<Shader>(ResourceManager.SteamVR, name);
	}

	public static T LoadFromAssetBundle<T>(byte[] assetBundleBytes, string name) where T : Object
	{
		string key = GetKey(assetBundleBytes);
		if (!_AssetBundles.ContainsKey(key))
		{
			_AssetBundles[key] = LoadAssetBundle(assetBundleBytes);
			if (_AssetBundles[key] == null)
			{
				VRLog.Error("Looks like the asset bundle failed to load?");
			}
		}
		try
		{
			VRLog.Info("Loading: {0} ({1})", name, key);
			name = name.Replace("Custom/", "");
			T val = _AssetBundles[key].LoadAsset<T>(name);
			if (!(val != null))
			{
				VRLog.Error("Failed to load {0}", name);
			}
			return (!typeof(Shader).IsAssignableFrom(typeof(T)) && !typeof(ComputeShader).IsAssignableFrom(typeof(T))) ? Object.Instantiate<T>(val) : val;
		}
		catch (Exception obj)
		{
			VRLog.Error(obj);
			return default(T);
		}
	}

	private static AssetBundle LoadAssetBundle(byte[] bytes)
	{
		if (_LoadFromMemory != null)
		{
			object obj = _LoadFromMemory.Invoke(null, new object[1] { bytes });
			return (AssetBundle)((obj is AssetBundle) ? obj : null);
		}
		if (_CreateFromMemory != null)
		{
			object obj2 = _CreateFromMemory.Invoke(null, new object[1] { bytes });
			return (AssetBundle)((obj2 is AssetBundle) ? obj2 : null);
		}
		VRLog.Error("Could not find a way to load AssetBundles!");
		return null;
	}

	private static string CalculateChecksum(byte[] byteToCalculate)
	{
		int num = 0;
		foreach (byte b in byteToCalculate)
		{
			num += b;
		}
		return (num & 0xFF).ToString("X2");
	}

	private static string GetKey(byte[] assetBundleBytes)
	{
		return CalculateChecksum(assetBundleBytes);
	}

	public static Transform GetDebugBall(string name)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (!_DebugBalls.TryGetValue(name, out var value) || !(value != null))
		{
			value = GameObject.CreatePrimitive((PrimitiveType)0).transform;
			Transform transform = ((Component)value).transform;
			transform.localScale *= 0.03f;
			_DebugBalls[name] = value;
		}
		return value;
	}

	public static void DrawDebugBall(Transform transform)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		GetDebugBall(((Object)transform).GetInstanceID().ToString()).position = transform.position;
	}

	public static void DrawRay(Color color, Vector3 origin, Vector3 direction)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		DrawRay(color, new Ray(origin, direction.normalized));
	}

	public static void DrawRay(Color color, Ray ray)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		if (!_Rays.TryGetValue(color, out var value) || !(value != null))
		{
			value = RayDrawer.Create(color, ray);
			_Rays[color] = value;
		}
		value.Touch(ray);
	}

	public static Transform CreateGameObjectAsChild(string name, Transform parent, bool dontDestroy = false)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		GameObject val = new GameObject(name);
		val.transform.SetParent(parent, false);
		if (dontDestroy)
		{
			Object.DontDestroyOnLoad((Object)(object)val);
		}
		return val.transform;
	}

	public static Texture2D LoadImage(string filePath)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		filePath = Path.Combine(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Images"), filePath);
		Texture2D val = null;
		if (File.Exists(filePath))
		{
			byte[] array = File.ReadAllBytes(filePath);
			val = new Texture2D(2, 2);
			val.LoadImage(array);
		}
		else
		{
			VRLog.Warn("File " + filePath + " does not exist");
		}
		return val;
	}

	public static string[] GetLayerNames(int mask)
	{
		List<string> list = new List<string>();
		for (int i = 0; i <= 31; i++)
		{
			if ((mask & (1 << i)) != 0)
			{
				list.Add(LayerMask.LayerToName(i));
			}
		}
		return (from m in list
			select m.Trim() into m
			where m.Length > 0
			select m).ToArray();
	}

	public static T CopyComponent<T>(T original, GameObject destination) where T : Component
	{
		Type type = ((object)original).GetType();
		Component val = destination.AddComponent(type);
		FieldInfo[] fields = type.GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			fieldInfo.SetValue(val, fieldInfo.GetValue(original));
		}
		return (T)(object)((val is T) ? val : null);
	}

	public static void DumpScene(string path, bool onlyActive = false)
	{
		VRLog.Info("Dumping scene...");
		JSONArray jSONArray = new JSONArray();
		foreach (GameObject item in from go in Object.FindObjectsOfType<GameObject>()
			where go.transform.parent == null
			select go)
		{
			jSONArray.Add(AnalyzeNode(item, onlyActive));
		}
		File.WriteAllText(path, jSONArray.ToJSON(0));
		VRLog.Info("Done!");
	}

	public static void DumpObject(GameObject obj, string path)
	{
		VRLog.Info("Dumping object...");
		File.WriteAllText(path, AnalyzeNode(obj).ToJSON(0));
		VRLog.Info("Done!");
	}

	public static IEnumerable<GameObject> GetRootNodes()
	{
		return from go in Object.FindObjectsOfType<GameObject>()
			where go.transform.parent == null
			select go;
	}

	public static JSONClass AnalyzeComponent(Component c)
	{
		JSONClass jSONClass = new JSONClass();
		FieldInfo[] fields = ((object)c).GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
		foreach (FieldInfo fieldInfo in fields)
		{
			try
			{
				string text = FieldToString(fieldInfo.Name, fieldInfo.GetValue(c));
				if (text != null)
				{
					jSONClass[fieldInfo.Name] = text;
				}
			}
			catch (Exception)
			{
				VRLog.Warn("Failed to get field {0}", fieldInfo.Name);
			}
		}
		PropertyInfo[] properties = ((object)c).GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
		foreach (PropertyInfo propertyInfo in properties)
		{
			try
			{
				if (propertyInfo.GetIndexParameters().Length == 0)
				{
					string text2 = FieldToString(propertyInfo.Name, propertyInfo.GetValue(c, null));
					if (text2 != null)
					{
						jSONClass[propertyInfo.Name] = text2;
					}
				}
			}
			catch (Exception)
			{
				VRLog.Warn("Failed to get prop {0}", propertyInfo.Name);
			}
		}
		return jSONClass;
	}

	public static JSONClass AnalyzeNode(GameObject go, bool onlyActive = false)
	{
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		JSONClass jSONClass = new JSONClass();
		jSONClass["name"] = ((Object)go).name;
		jSONClass["active"] = go.activeSelf.ToString();
		jSONClass["tag"] = go.tag;
		jSONClass["layer"] = LayerMask.LayerToName(go.gameObject.layer);
		Vector3 val = go.transform.localPosition;
		jSONClass["pos"] = ((object)val).ToString();
		val = go.transform.localEulerAngles;
		jSONClass["rot"] = ((object)val).ToString();
		val = go.transform.localScale;
		jSONClass["scale"] = ((object)val).ToString();
		JSONClass jSONClass2 = new JSONClass();
		Component[] components = go.GetComponents<Component>();
		foreach (Component val2 in components)
		{
			if (val2 == null)
			{
				VRLog.Warn("NULL component: " + val2);
			}
			else
			{
				jSONClass2[((object)val2).GetType().Name] = AnalyzeComponent(val2);
			}
		}
		JSONArray jSONArray = new JSONArray();
		foreach (GameObject item in go.Children())
		{
			if (!onlyActive || item.activeInHierarchy)
			{
				jSONArray.Add(AnalyzeNode(item, onlyActive));
			}
		}
		jSONClass["Components"] = jSONClass2;
		jSONClass["Children"] = jSONArray;
		return jSONClass;
	}

	private static string FieldToString(string memberName, object value)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		if (value == null)
		{
			return null;
		}
		if (!(memberName == "cullingMask"))
		{
			if (memberName == "renderer")
			{
				return ((Object)((Renderer)value).material.shader).name;
			}
			if (value is Vector3 val)
			{
				return $"({val.x:0.000}, {val.y:0.000}, {val.z:0.000})";
			}
			if (value is Vector2 val2)
			{
				return $"({val2.x:0.000}, {val2.y:0.000})";
			}
			return value.ToString();
		}
		return string.Join(", ", GetLayerNames((int)value));
	}

	public static void SetPropertyOrField<T>(T obj, string name, object value)
	{
		PropertyInfo property = typeof(T).GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		FieldInfo field = typeof(T).GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (property != null)
		{
			property.SetValue(obj, value, null);
		}
		else if (field != null)
		{
			field.SetValue(obj, value);
		}
		else
		{
			VRLog.Warn("Prop/Field not found!");
		}
	}

	public static object GetPropertyOrField<T>(T obj, string name)
	{
		PropertyInfo property = typeof(T).GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		FieldInfo field = typeof(T).GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (property != null)
		{
			return property.GetValue(obj, null);
		}
		if (field != null)
		{
			return field.GetValue(obj);
		}
		VRLog.Warn("Prop/Field not found!");
		return null;
	}

	public static void SaveTexture(RenderTexture rt, string pngOutPath)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		RenderTexture active = RenderTexture.active;
		try
		{
			Texture2D val = new Texture2D(((Texture)rt).width, ((Texture)rt).height, (TextureFormat)5, false);
			RenderTexture.active = rt;
			val.ReadPixels(new Rect(0f, 0f, (float)((Texture)rt).width, (float)((Texture)rt).height), 0, 0);
			val.Apply();
			File.WriteAllBytes(pngOutPath, val.EncodeToPNG());
			Object.Destroy((Object)(object)val);
		}
		finally
		{
			RenderTexture.active = active;
		}
	}
}

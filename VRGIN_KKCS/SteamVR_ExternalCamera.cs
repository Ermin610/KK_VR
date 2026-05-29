using System;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Rendering;
using Valve.VR;

public class SteamVR_ExternalCamera : MonoBehaviour
{
	[Serializable]
	public struct Config
	{
		public float x;

		public float y;

		public float z;

		public float rx;

		public float ry;

		public float rz;

		public float fov;

		public float near;

		public float far;

		public float sceneResolutionScale;

		public float frameSkip;

		public float nearOffset;

		public float farOffset;

		public float hmdOffset;

		public float r;

		public float g;

		public float b;

		public float a;

		public bool disableStandardAssets;
	}

	public Config config;

	public string configPath;

	private FileSystemWatcher watcher;

	private Camera cam;

	private Transform target;

	private GameObject clipQuad;

	private Material clipMaterial;

	private Material colorMat;

	private Material alphaMat;

	private Camera[] cameras;

	private Rect[] cameraRects;

	private float sceneResolutionScale;

	public void ReadConfig()
	{
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			HmdMatrix34_t pose = default(HmdMatrix34_t);
			bool flag = false;
			object obj = config;
			string[] array = File.ReadAllLines(configPath);
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split('=');
				if (array2.Length != 2)
				{
					continue;
				}
				string text = array2[0];
				if (text == "m")
				{
					string[] array3 = array2[1].Split(',');
					if (array3.Length == 12)
					{
						pose.m0 = float.Parse(array3[0]);
						pose.m1 = float.Parse(array3[1]);
						pose.m2 = float.Parse(array3[2]);
						pose.m3 = float.Parse(array3[3]);
						pose.m4 = float.Parse(array3[4]);
						pose.m5 = float.Parse(array3[5]);
						pose.m6 = float.Parse(array3[6]);
						pose.m7 = float.Parse(array3[7]);
						pose.m8 = float.Parse(array3[8]);
						pose.m9 = float.Parse(array3[9]);
						pose.m10 = float.Parse(array3[10]);
						pose.m11 = float.Parse(array3[11]);
						flag = true;
					}
				}
				else if (text == "disableStandardAssets")
				{
					obj.GetType().GetField(text)?.SetValue(obj, bool.Parse(array2[1]));
				}
				else
				{
					obj.GetType().GetField(text)?.SetValue(obj, float.Parse(array2[1]));
				}
			}
			config = (Config)obj;
			if (flag)
			{
				SteamVR_Utils.RigidTransform rigidTransform = new SteamVR_Utils.RigidTransform(pose);
				config.x = rigidTransform.pos.x;
				config.y = rigidTransform.pos.y;
				config.z = rigidTransform.pos.z;
				Vector3 eulerAngles = rigidTransform.rot.eulerAngles;
				config.rx = eulerAngles.x;
				config.ry = eulerAngles.y;
				config.rz = eulerAngles.z;
			}
		}
		catch
		{
		}
		target = null;
		if (watcher == null)
		{
			FileInfo fileInfo = new FileInfo(configPath);
			watcher = new FileSystemWatcher(fileInfo.DirectoryName, fileInfo.Name);
			watcher.NotifyFilter = NotifyFilters.LastWrite;
			watcher.Changed += OnChanged;
			watcher.EnableRaisingEvents = true;
		}
	}

	private void OnChanged(object source, FileSystemEventArgs e)
	{
		ReadConfig();
	}

	public void AttachToCamera(SteamVR_Camera vrcam)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Expected O, but got Unknown
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Expected O, but got Unknown
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Expected O, but got Unknown
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
		if (!(target == vrcam.head))
		{
			target = vrcam.head;
			Transform parent = ((Component)this).transform.parent;
			Transform parent2 = vrcam.head.parent;
			parent.parent = parent2;
			parent.localPosition = Vector3.zero;
			parent.localRotation = Quaternion.identity;
			parent.localScale = Vector3.one;
			((Behaviour)vrcam).enabled = false;
			GameObject val = Object.Instantiate<GameObject>(((Component)vrcam).gameObject);
			((Behaviour)vrcam).enabled = true;
			((Object)val).name = "camera";
			Object.DestroyImmediate((Object)(object)val.GetComponent<SteamVR_Camera>());
			Object.DestroyImmediate((Object)(object)val.GetComponent<SteamVR_Fade>());
			cam = val.GetComponent<Camera>();
			cam.stereoTargetEye = (StereoTargetEyeMask)0;
			cam.fieldOfView = config.fov;
			cam.useOcclusionCulling = false;
			((Behaviour)cam).enabled = false;
			colorMat = new Material(Shader.Find("Custom/SteamVR_ColorOut"));
			alphaMat = new Material(Shader.Find("Custom/SteamVR_AlphaOut"));
			clipMaterial = new Material(Shader.Find("Custom/SteamVR_ClearAll"));
			Transform transform = val.transform;
			transform.parent = ((Component)this).transform;
			transform.localPosition = new Vector3(config.x, config.y, config.z);
			transform.localRotation = Quaternion.Euler(config.rx, config.ry, config.rz);
			transform.localScale = Vector3.one;
			while (transform.childCount > 0)
			{
				Object.DestroyImmediate((Object)(object)((Component)transform.GetChild(0)).gameObject);
			}
			clipQuad = GameObject.CreatePrimitive((PrimitiveType)5);
			((Object)clipQuad).name = "ClipQuad";
			Object.DestroyImmediate((Object)(object)clipQuad.GetComponent<MeshCollider>());
			MeshRenderer component = clipQuad.GetComponent<MeshRenderer>();
			((Renderer)component).material = clipMaterial;
			((Renderer)component).shadowCastingMode = (ShadowCastingMode)0;
			((Renderer)component).receiveShadows = false;
			((Renderer)component).lightProbeUsage = (LightProbeUsage)0;
			((Renderer)component).reflectionProbeUsage = (ReflectionProbeUsage)0;
			Transform transform2 = clipQuad.transform;
			transform2.parent = transform;
			transform2.localScale = new Vector3(1000f, 1000f, 1f);
			transform2.localRotation = Quaternion.identity;
			clipQuad.SetActive(false);
		}
	}

	public float GetTargetDistance()
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		if (target == null)
		{
			return config.near + 0.01f;
		}
		Transform transform = ((Component)cam).transform;
		Vector3 val = new Vector3(transform.forward.x, 0f, transform.forward.z);
		Vector3 normalized = val.normalized;
		Vector3 position = target.position;
		val = new Vector3(target.forward.x, 0f, target.forward.z);
		Vector3 val2 = position + val.normalized * config.hmdOffset;
		Plane val3 = new Plane(normalized, val2);
		return Mathf.Clamp(0f - val3.GetDistanceToPoint(transform.position), config.near + 0.01f, config.far - 0.01f);
	}

	public void RenderNear()
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Expected O, but got Unknown
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		int num = Screen.width / 2;
		int num2 = Screen.height / 2;
		if (cam.targetTexture == null || ((Texture)cam.targetTexture).width != num || ((Texture)cam.targetTexture).height != num2)
		{
			RenderTexture val = new RenderTexture(num, num2, 24, (RenderTextureFormat)0);
			val.antiAliasing = ((QualitySettings.antiAliasing == 0) ? 1 : QualitySettings.antiAliasing);
			cam.targetTexture = val;
		}
		cam.nearClipPlane = config.near;
		cam.farClipPlane = config.far;
		CameraClearFlags clearFlags = cam.clearFlags;
		Color backgroundColor = cam.backgroundColor;
		cam.clearFlags = (CameraClearFlags)2;
		cam.backgroundColor = Color.clear;
		clipMaterial.color = new Color(config.r, config.g, config.b, config.a);
		float num3 = Mathf.Clamp(GetTargetDistance() + config.nearOffset, config.near, config.far);
		Transform parent = clipQuad.transform.parent;
		clipQuad.transform.position = parent.position + parent.forward * num3;
		MonoBehaviour[] array = null;
		bool[] array2 = null;
		if (config.disableStandardAssets)
		{
			array = ((Component)cam).gameObject.GetComponents<MonoBehaviour>();
			array2 = new bool[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				MonoBehaviour val2 = array[i];
				if (((Behaviour)val2).enabled && ((object)val2).GetType().ToString().StartsWith("UnityStandardAssets."))
				{
					((Behaviour)val2).enabled = false;
					array2[i] = true;
				}
			}
		}
		clipQuad.SetActive(true);
		cam.Render();
		Graphics.DrawTexture(new Rect(0f, 0f, (float)num, (float)num2), (Texture)(object)cam.targetTexture, colorMat);
		Component component = ((Component)cam).gameObject.GetComponent("PostProcessingBehaviour");
		MonoBehaviour val3 = (MonoBehaviour)(object)((component is MonoBehaviour) ? component : null);
		if (val3 != null && ((Behaviour)val3).enabled)
		{
			((Behaviour)val3).enabled = false;
			cam.Render();
			((Behaviour)val3).enabled = true;
		}
		Graphics.DrawTexture(new Rect((float)num, 0f, (float)num, (float)num2), (Texture)(object)cam.targetTexture, alphaMat);
		clipQuad.SetActive(false);
		if (array != null)
		{
			for (int j = 0; j < array.Length; j++)
			{
				if (array2[j])
				{
					((Behaviour)array[j]).enabled = true;
				}
			}
		}
		cam.clearFlags = clearFlags;
		cam.backgroundColor = backgroundColor;
	}

	public void RenderFar()
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		cam.nearClipPlane = config.near;
		cam.farClipPlane = config.far;
		cam.Render();
		int num = Screen.width / 2;
		int num2 = Screen.height / 2;
		Graphics.DrawTexture(new Rect(0f, (float)num2, (float)num, (float)num2), (Texture)(object)cam.targetTexture, colorMat);
	}

	private void OnGUI()
	{
	}

	private void OnEnable()
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		cameras = Object.FindObjectsOfType<Camera>();
		if (cameras != null)
		{
			int num = cameras.Length;
			cameraRects = (Rect[])(object)new Rect[num];
			for (int i = 0; i < num; i++)
			{
				Camera val = cameras[i];
				cameraRects[i] = val.rect;
				if (!(val == cam) && !(val.targetTexture != null) && !((Object)(object)((Component)val).GetComponent<SteamVR_Camera>() != (Object)null))
				{
					val.rect = new Rect(0.5f, 0f, 0.5f, 0.5f);
				}
			}
		}
		if (config.sceneResolutionScale > 0f)
		{
			sceneResolutionScale = SteamVR_Camera.sceneResolutionScale;
			SteamVR_Camera.sceneResolutionScale = config.sceneResolutionScale;
		}
	}

	private void OnDisable()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		if (cameras != null)
		{
			int num = cameras.Length;
			for (int i = 0; i < num; i++)
			{
				Camera val = cameras[i];
				if (val != null)
				{
					val.rect = cameraRects[i];
				}
			}
			cameras = null;
			cameraRects = null;
		}
		if (config.sceneResolutionScale > 0f)
		{
			SteamVR_Camera.sceneResolutionScale = sceneResolutionScale;
		}
	}
}

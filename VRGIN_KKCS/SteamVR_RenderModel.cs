using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;
using Valve.VR;

[ExecuteInEditMode]
public class SteamVR_RenderModel : MonoBehaviour
{
	public class RenderModel
	{
		public Mesh mesh { get; private set; }

		public Material material { get; private set; }

		public RenderModel(Mesh mesh, Material material)
		{
			this.mesh = mesh;
			this.material = material;
		}
	}

	public sealed class RenderModelInterfaceHolder : IDisposable
	{
		private bool needsShutdown;

		private bool failedLoadInterface;

		private CVRRenderModels _instance;

		public CVRRenderModels instance
		{
			get
			{
				if (_instance == null && !failedLoadInterface)
				{
					if (!SteamVR.active && !SteamVR.usingNativeSupport)
					{
						EVRInitError peError = EVRInitError.None;
						OpenVR.Init(ref peError, EVRApplicationType.VRApplication_Utility);
						needsShutdown = true;
					}
					_instance = OpenVR.RenderModels;
					if (_instance == null)
					{
						Debug.LogError((object)"Failed to load IVRRenderModels interface version IVRRenderModels_005");
						failedLoadInterface = true;
					}
				}
				return _instance;
			}
		}

		public void Dispose()
		{
			if (needsShutdown)
			{
				OpenVR.Shutdown();
			}
		}
	}

	public SteamVR_TrackedObject.EIndex index = SteamVR_TrackedObject.EIndex.None;

	public const string modelOverrideWarning = "Model override is really only meant to be used in the scene view for lining things up; using it at runtime is discouraged.  Use tracked device index instead to ensure the correct model is displayed for all users.";

	[Tooltip("Model override is really only meant to be used in the scene view for lining things up; using it at runtime is discouraged.  Use tracked device index instead to ensure the correct model is displayed for all users.")]
	public string modelOverride;

	[Tooltip("Shader to apply to model.")]
	public Shader shader;

	[Tooltip("Enable to print out when render models are loaded.")]
	public bool verbose;

	[Tooltip("If available, break down into separate components instead of loading as a single mesh.")]
	public bool createComponents = true;

	[Tooltip("Update transforms of components at runtime to reflect user action.")]
	public bool updateDynamically = true;

	public RenderModel_ControllerMode_State_t controllerModeState;

	public const string k_localTransformName = "attach";

	public static Hashtable models = new Hashtable();

	public static Hashtable materials = new Hashtable();

	private SteamVR_Events.Action deviceConnectedAction;

	private SteamVR_Events.Action hideRenderModelsAction;

	private SteamVR_Events.Action modelSkinSettingsHaveChangedAction;

	private Dictionary<int, string> nameCache;

	public string renderModelName { get; private set; }

	private void OnModelSkinSettingsHaveChanged(VREvent_t vrEvent)
	{
		if (!string.IsNullOrEmpty(renderModelName))
		{
			renderModelName = "";
			UpdateModel();
		}
	}

	private void OnHideRenderModels(bool hidden)
	{
		MeshRenderer component = ((Component)this).GetComponent<MeshRenderer>();
		if (component != null)
		{
			((Renderer)component).enabled = !hidden;
		}
		MeshRenderer[] componentsInChildren = ((Component)((Component)this).transform).GetComponentsInChildren<MeshRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			((Renderer)componentsInChildren[i]).enabled = !hidden;
		}
	}

	private void OnDeviceConnected(int i, bool connected)
	{
		if (i == (int)index && connected)
		{
			UpdateModel();
		}
	}

	public void UpdateModel()
	{
		CVRSystem system = OpenVR.System;
		if (system == null)
		{
			return;
		}
		ETrackedPropertyError pError = ETrackedPropertyError.TrackedProp_Success;
		uint stringTrackedDeviceProperty = system.GetStringTrackedDeviceProperty((uint)index, ETrackedDeviceProperty.Prop_RenderModelName_String, null, 0u, ref pError);
		if (stringTrackedDeviceProperty <= 1)
		{
			Debug.LogError((object)("Failed to get render model name for tracked object " + index));
			return;
		}
		StringBuilder stringBuilder = new StringBuilder((int)stringTrackedDeviceProperty);
		system.GetStringTrackedDeviceProperty((uint)index, ETrackedDeviceProperty.Prop_RenderModelName_String, stringBuilder, stringTrackedDeviceProperty, ref pError);
		string text = stringBuilder.ToString();
		if (renderModelName != text)
		{
			renderModelName = text;
			((MonoBehaviour)this).StartCoroutine(SetModelAsync(text));
		}
	}

	private IEnumerator SetModelAsync(string renderModelName)
	{
		if (string.IsNullOrEmpty(renderModelName))
		{
			yield break;
		}
		using (RenderModelInterfaceHolder holder = new RenderModelInterfaceHolder())
		{
			CVRRenderModels renderModels = holder.instance;
			if (renderModels == null)
			{
				yield break;
			}
			uint componentCount = renderModels.GetComponentCount(renderModelName);
			string[] renderModelNames;
			if (componentCount == 0)
			{
				renderModelNames = ((models[renderModelName] is RenderModel renderModel && !(renderModel.mesh == null)) ? new string[0] : new string[1] { renderModelName });
			}
			else
			{
				renderModelNames = new string[componentCount];
				for (int i = 0; i < componentCount; i++)
				{
					uint componentName = renderModels.GetComponentName(renderModelName, (uint)i, null, 0u);
					if (componentName == 0)
					{
						continue;
					}
					StringBuilder stringBuilder = new StringBuilder((int)componentName);
					if (renderModels.GetComponentName(renderModelName, (uint)i, stringBuilder, componentName) == 0)
					{
						continue;
					}
					componentName = renderModels.GetComponentRenderModelName(renderModelName, stringBuilder.ToString(), null, 0u);
					if (componentName == 0)
					{
						continue;
					}
					StringBuilder stringBuilder2 = new StringBuilder((int)componentName);
					if (renderModels.GetComponentRenderModelName(renderModelName, stringBuilder.ToString(), stringBuilder2, componentName) != 0)
					{
						string text = stringBuilder2.ToString();
						if (!(models[text] is RenderModel renderModel2) || renderModel2.mesh == null)
						{
							renderModelNames[i] = text;
						}
					}
				}
			}
			while (true)
			{
				bool flag = false;
				string[] array = renderModelNames;
				foreach (string text2 in array)
				{
					if (string.IsNullOrEmpty(text2))
					{
						continue;
					}
					IntPtr ppRenderModel = IntPtr.Zero;
					switch (renderModels.LoadRenderModel_Async(text2, ref ppRenderModel))
					{
					case EVRRenderModelError.Loading:
						flag = true;
						break;
					case EVRRenderModelError.None:
					{
						RenderModel_t renderModel_t = MarshalRenderModel(ppRenderModel);
						object obj = materials[renderModel_t.diffuseTextureId];
						Material val = (Material)((obj is Material) ? obj : null);
						if (val == null || val.mainTexture == null)
						{
							IntPtr ppTexture = IntPtr.Zero;
							EVRRenderModelError eVRRenderModelError = renderModels.LoadTexture_Async(renderModel_t.diffuseTextureId, ref ppTexture);
							if (eVRRenderModelError == EVRRenderModelError.Loading)
							{
								flag = true;
							}
						}
						break;
					}
					}
				}
				if (!flag)
				{
					break;
				}
				yield return (object)new WaitForSecondsRealtime(0.1f);
			}
		}
		bool arg = SetModel(renderModelName);
		SteamVR_Events.RenderModelLoaded.Send(this, arg);
	}

	private bool SetModel(string renderModelName)
	{
		StripMesh(((Component)this).gameObject);
		using (RenderModelInterfaceHolder renderModelInterfaceHolder = new RenderModelInterfaceHolder())
		{
			if (createComponents)
			{
				if (LoadComponents(renderModelInterfaceHolder, renderModelName))
				{
					UpdateComponents(renderModelInterfaceHolder.instance);
					return true;
				}
				Debug.Log((object)("[" + ((Object)((Component)this).gameObject).name + "] Render model does not support components, falling back to single mesh."));
			}
			if (!string.IsNullOrEmpty(renderModelName))
			{
				RenderModel renderModel = models[renderModelName] as RenderModel;
				if (renderModel == null || renderModel.mesh == null)
				{
					CVRRenderModels instance = renderModelInterfaceHolder.instance;
					if (instance == null)
					{
						return false;
					}
					if (verbose)
					{
						Debug.Log((object)("Loading render model " + renderModelName));
					}
					renderModel = LoadRenderModel(instance, renderModelName, renderModelName);
					if (renderModel == null)
					{
						return false;
					}
					models[renderModelName] = renderModel;
				}
				((Component)this).gameObject.AddComponent<MeshFilter>().mesh = renderModel.mesh;
				((Renderer)((Component)this).gameObject.AddComponent<MeshRenderer>()).sharedMaterial = renderModel.material;
				return true;
			}
		}
		return false;
	}

	private RenderModel LoadRenderModel(CVRRenderModels renderModels, string renderModelName, string baseName)
	{
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Expected O, but got Unknown
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Expected O, but got Unknown
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Invalid comparison between Unknown and I4
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		//IL_032a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0381: Unknown result type (might be due to invalid IL or missing references)
		//IL_0388: Expected O, but got Unknown
		IntPtr ppRenderModel = IntPtr.Zero;
		while (true)
		{
			EVRRenderModelError eVRRenderModelError = renderModels.LoadRenderModel_Async(renderModelName, ref ppRenderModel);
			switch (eVRRenderModelError)
			{
			case EVRRenderModelError.Loading:
				break;
			default:
				Debug.LogError((object)$"Failed to load render model {renderModelName} - {eVRRenderModelError.ToString()}");
				return null;
			case EVRRenderModelError.None:
			{
				RenderModel_t renderModel_t = MarshalRenderModel(ppRenderModel);
				Vector3[] array = (Vector3[])(object)new Vector3[renderModel_t.unVertexCount];
				Vector3[] array2 = (Vector3[])(object)new Vector3[renderModel_t.unVertexCount];
				Vector2[] array3 = (Vector2[])(object)new Vector2[renderModel_t.unVertexCount];
				Type typeFromHandle = typeof(RenderModel_Vertex_t);
				for (int i = 0; i < renderModel_t.unVertexCount; i++)
				{
					RenderModel_Vertex_t renderModel_Vertex_t = (RenderModel_Vertex_t)Marshal.PtrToStructure(new IntPtr(renderModel_t.rVertexData.ToInt64() + i * Marshal.SizeOf(typeFromHandle)), typeFromHandle);
					array[i] = new Vector3(renderModel_Vertex_t.vPosition.v0, renderModel_Vertex_t.vPosition.v1, 0f - renderModel_Vertex_t.vPosition.v2);
					array2[i] = new Vector3(renderModel_Vertex_t.vNormal.v0, renderModel_Vertex_t.vNormal.v1, 0f - renderModel_Vertex_t.vNormal.v2);
					array3[i] = new Vector2(renderModel_Vertex_t.rfTextureCoord0, renderModel_Vertex_t.rfTextureCoord1);
				}
				uint num = renderModel_t.unTriangleCount * 3;
				short[] array4 = new short[num];
				Marshal.Copy(renderModel_t.rIndexData, array4, 0, array4.Length);
				int[] array5 = new int[num];
				for (int j = 0; j < renderModel_t.unTriangleCount; j++)
				{
					array5[j * 3] = array4[j * 3 + 2];
					array5[j * 3 + 1] = array4[j * 3 + 1];
					array5[j * 3 + 2] = array4[j * 3];
				}
				Mesh val = new Mesh();
				val.vertices = array;
				val.normals = array2;
				val.uv = array3;
				val.triangles = array5;
				object obj = materials[renderModel_t.diffuseTextureId];
				Material val2 = (Material)((obj is Material) ? obj : null);
				if (val2 == null || val2.mainTexture == null)
				{
					IntPtr ppTexture = IntPtr.Zero;
					while (true)
					{
						switch (renderModels.LoadTexture_Async(renderModel_t.diffuseTextureId, ref ppTexture))
						{
						case EVRRenderModelError.Loading:
							goto IL_0230;
						case EVRRenderModelError.None:
						{
							RenderModel_TextureMap_t renderModel_TextureMap_t = MarshalRenderModel_TextureMap(ppTexture);
							Texture2D val3 = new Texture2D((int)renderModel_TextureMap_t.unWidth, (int)renderModel_TextureMap_t.unHeight, (TextureFormat)4, false);
							if ((int)SystemInfo.graphicsDeviceType == 2)
							{
								val3.Apply();
								while (true)
								{
									eVRRenderModelError = renderModels.LoadIntoTextureD3D11_Async(renderModel_t.diffuseTextureId, ((Texture)val3).GetNativeTexturePtr());
									if (eVRRenderModelError != EVRRenderModelError.Loading)
									{
										break;
									}
									Sleep();
								}
							}
							else
							{
								byte[] array6 = new byte[renderModel_TextureMap_t.unWidth * renderModel_TextureMap_t.unHeight * 4];
								Marshal.Copy(renderModel_TextureMap_t.rubTextureMapData, array6, 0, array6.Length);
								Color32[] array7 = (Color32[])(object)new Color32[renderModel_TextureMap_t.unWidth * renderModel_TextureMap_t.unHeight];
								int num2 = 0;
								for (int k = 0; k < renderModel_TextureMap_t.unHeight; k++)
								{
									for (int l = 0; l < renderModel_TextureMap_t.unWidth; l++)
									{
										byte b = array6[num2++];
										byte b2 = array6[num2++];
										byte b3 = array6[num2++];
										byte b4 = array6[num2++];
										array7[k * renderModel_TextureMap_t.unWidth + l] = new Color32(b, b2, b3, b4);
									}
								}
								val3.SetPixels32(array7);
								val3.Apply();
							}
							val2 = new Material((shader != null) ? shader : Shader.Find("Standard"));
							val2.mainTexture = (Texture)(object)val3;
							materials[renderModel_t.diffuseTextureId] = val2;
							renderModels.FreeTexture(ppTexture);
							break;
						}
						default:
							Debug.Log((object)("Failed to load render model texture for render model " + renderModelName));
							break;
						}
						break;
						IL_0230:
						Sleep();
					}
				}
				((MonoBehaviour)this).StartCoroutine(FreeRenderModel(ppRenderModel));
				return new RenderModel(val, val2);
			}
			}
			Sleep();
		}
	}

	private IEnumerator FreeRenderModel(IntPtr pRenderModel)
	{
		yield return (object)new WaitForSeconds(1f);
		using RenderModelInterfaceHolder renderModelInterfaceHolder = new RenderModelInterfaceHolder();
		renderModelInterfaceHolder.instance.FreeRenderModel(pRenderModel);
	}

	public Transform FindComponent(string componentName)
	{
		Transform transform = ((Component)this).transform;
		for (int i = 0; i < transform.childCount; i++)
		{
			Transform child = transform.GetChild(i);
			if (((Object)child).name == componentName)
			{
				return child;
			}
		}
		return null;
	}

	private void StripMesh(GameObject go)
	{
		MeshRenderer component = go.GetComponent<MeshRenderer>();
		if (component != null)
		{
			Object.DestroyImmediate((Object)(object)component);
		}
		MeshFilter component2 = go.GetComponent<MeshFilter>();
		if (component2 != null)
		{
			Object.DestroyImmediate((Object)(object)component2);
		}
	}

	private bool LoadComponents(RenderModelInterfaceHolder holder, string renderModelName)
	{
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		Transform transform = ((Component)this).transform;
		for (int i = 0; i < transform.childCount; i++)
		{
			Transform child = transform.GetChild(i);
			((Component)child).gameObject.SetActive(false);
			StripMesh(((Component)child).gameObject);
		}
		if (string.IsNullOrEmpty(renderModelName))
		{
			return true;
		}
		CVRRenderModels instance = holder.instance;
		if (instance == null)
		{
			return false;
		}
		uint componentCount = instance.GetComponentCount(renderModelName);
		if (componentCount == 0)
		{
			return false;
		}
		for (int j = 0; j < componentCount; j++)
		{
			uint componentName = instance.GetComponentName(renderModelName, (uint)j, null, 0u);
			if (componentName == 0)
			{
				continue;
			}
			StringBuilder stringBuilder = new StringBuilder((int)componentName);
			if (instance.GetComponentName(renderModelName, (uint)j, stringBuilder, componentName) == 0)
			{
				continue;
			}
			transform = FindComponent(stringBuilder.ToString());
			if (transform != null)
			{
				((Component)transform).gameObject.SetActive(true);
			}
			else
			{
				transform = new GameObject(stringBuilder.ToString()).transform;
				transform.parent = ((Component)this).transform;
				((Component)transform).gameObject.layer = ((Component)this).gameObject.layer;
				Transform transform2 = new GameObject("attach").transform;
				transform2.parent = transform;
				transform2.localPosition = Vector3.zero;
				transform2.localRotation = Quaternion.identity;
				transform2.localScale = Vector3.one;
				((Component)transform2).gameObject.layer = ((Component)this).gameObject.layer;
			}
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			transform.localScale = Vector3.one;
			componentName = instance.GetComponentRenderModelName(renderModelName, stringBuilder.ToString(), null, 0u);
			if (componentName == 0)
			{
				continue;
			}
			StringBuilder stringBuilder2 = new StringBuilder((int)componentName);
			if (instance.GetComponentRenderModelName(renderModelName, stringBuilder.ToString(), stringBuilder2, componentName) == 0)
			{
				continue;
			}
			RenderModel renderModel = models[stringBuilder2] as RenderModel;
			if (renderModel == null || renderModel.mesh == null)
			{
				if (verbose)
				{
					Debug.Log((object)("Loading render model " + stringBuilder2));
				}
				renderModel = LoadRenderModel(instance, stringBuilder2.ToString(), renderModelName);
				if (renderModel == null)
				{
					continue;
				}
				models[stringBuilder2] = renderModel;
			}
			((Component)transform).gameObject.AddComponent<MeshFilter>().mesh = renderModel.mesh;
			((Renderer)((Component)transform).gameObject.AddComponent<MeshRenderer>()).sharedMaterial = renderModel.material;
		}
		return true;
	}

	private SteamVR_RenderModel()
	{
		deviceConnectedAction = SteamVR_Events.DeviceConnectedAction(OnDeviceConnected);
		hideRenderModelsAction = SteamVR_Events.HideRenderModelsAction(OnHideRenderModels);
		modelSkinSettingsHaveChangedAction = SteamVR_Events.SystemAction(EVREventType.VREvent_ModelSkinSettingsHaveChanged, OnModelSkinSettingsHaveChanged);
	}

	private void OnEnable()
	{
		if (!string.IsNullOrEmpty(modelOverride))
		{
			Debug.Log((object)"Model override is really only meant to be used in the scene view for lining things up; using it at runtime is discouraged.  Use tracked device index instead to ensure the correct model is displayed for all users.");
			((Behaviour)this).enabled = false;
			return;
		}
		CVRSystem system = OpenVR.System;
		if (system != null && system.IsTrackedDeviceConnected((uint)index))
		{
			UpdateModel();
		}
		deviceConnectedAction.enabled = true;
		hideRenderModelsAction.enabled = true;
		modelSkinSettingsHaveChangedAction.enabled = true;
	}

	private void OnDisable()
	{
		deviceConnectedAction.enabled = false;
		hideRenderModelsAction.enabled = false;
		modelSkinSettingsHaveChangedAction.enabled = false;
	}

	private void Update()
	{
		if (updateDynamically)
		{
			UpdateComponents(OpenVR.RenderModels);
		}
	}

	public void UpdateComponents(CVRRenderModels renderModels)
	{
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		if (renderModels == null)
		{
			return;
		}
		Transform transform = ((Component)this).transform;
		if (transform.childCount == 0)
		{
			return;
		}
		VRControllerState_t pControllerState = ((index != SteamVR_TrackedObject.EIndex.None) ? SteamVR_Controller.Input((int)index).GetState() : default(VRControllerState_t));
		if (nameCache == null)
		{
			nameCache = new Dictionary<int, string>();
		}
		for (int i = 0; i < transform.childCount; i++)
		{
			Transform child = transform.GetChild(i);
			if (!nameCache.TryGetValue(((Object)child).GetInstanceID(), out var value))
			{
				value = ((Object)child).name;
				nameCache.Add(((Object)child).GetInstanceID(), value);
			}
			RenderModel_ComponentState_t pComponentState = default(RenderModel_ComponentState_t);
			if (renderModels.GetComponentState(renderModelName, value, ref pControllerState, ref controllerModeState, ref pComponentState))
			{
				SteamVR_Utils.RigidTransform rigidTransform = new SteamVR_Utils.RigidTransform(pComponentState.mTrackingToComponentRenderModel);
				child.localPosition = rigidTransform.pos;
				child.localRotation = rigidTransform.rot;
				Transform val = child.Find("attach");
				if (val != null)
				{
					SteamVR_Utils.RigidTransform rigidTransform2 = new SteamVR_Utils.RigidTransform(pComponentState.mTrackingToComponentLocal);
					val.position = transform.TransformPoint(rigidTransform2.pos);
					val.rotation = transform.rotation * rigidTransform2.rot;
				}
				bool flag = (pComponentState.uProperties & 2) != 0;
				if (flag != ((Component)child).gameObject.activeSelf)
				{
					((Component)child).gameObject.SetActive(flag);
				}
			}
		}
	}

	public void SetDeviceIndex(int index)
	{
		this.index = (SteamVR_TrackedObject.EIndex)index;
		modelOverride = "";
		if (((Behaviour)this).enabled)
		{
			UpdateModel();
		}
	}

	private static void Sleep()
	{
		Thread.Sleep(1);
	}

	private RenderModel_t MarshalRenderModel(IntPtr pRenderModel)
	{
		if (Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix)
		{
			RenderModel_t_Packed renderModel_t_Packed = (RenderModel_t_Packed)Marshal.PtrToStructure(pRenderModel, typeof(RenderModel_t_Packed));
			RenderModel_t unpacked = default(RenderModel_t);
			renderModel_t_Packed.Unpack(ref unpacked);
			return unpacked;
		}
		return (RenderModel_t)Marshal.PtrToStructure(pRenderModel, typeof(RenderModel_t));
	}

	private RenderModel_TextureMap_t MarshalRenderModel_TextureMap(IntPtr pRenderModel)
	{
		if (Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix)
		{
			RenderModel_TextureMap_t_Packed renderModel_TextureMap_t_Packed = (RenderModel_TextureMap_t_Packed)Marshal.PtrToStructure(pRenderModel, typeof(RenderModel_TextureMap_t_Packed));
			RenderModel_TextureMap_t unpacked = default(RenderModel_TextureMap_t);
			renderModel_TextureMap_t_Packed.Unpack(ref unpacked);
			return unpacked;
		}
		return (RenderModel_TextureMap_t)Marshal.PtrToStructure(pRenderModel, typeof(RenderModel_TextureMap_t));
	}
}

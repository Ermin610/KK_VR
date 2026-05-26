using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using VRGIN.Helpers;
using VRGIN.Native;
using VRGIN.Visuals;

namespace VRGIN.Core;

public class VRGUI : ProtectedBehaviour, IScreenGrabber
{
	private class FastGUI : MonoBehaviour
	{
		private void OnGUI()
		{
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Invalid comparison between Unknown and I4
			GUI.depth = int.MaxValue;
			if ((int)Event.current.type == 7)
			{
				((Component)this).SendMessage("OnBeforeGUI");
			}
		}
	}

	private class SlowGUI : MonoBehaviour
	{
		private void OnGUI()
		{
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Invalid comparison between Unknown and I4
			GUI.depth = int.MinValue;
			if ((int)Event.current.type == 7)
			{
				((Component)this).SendMessage("OnAfterGUI");
			}
		}
	}

	private class SortingAwareGraphicRaycaster : GraphicRaycaster
	{
		private Canvas _Canvas;

		private Canvas Canvas
		{
			get
			{
				if ((Object)(object)_Canvas != (Object)null)
				{
					return _Canvas;
				}
				_Canvas = ((Component)this).GetComponent<Canvas>();
				return _Canvas;
			}
		}

		public override int priority => GetOrder();

		public override int sortOrderPriority => GetOrder();

		public override int renderOrderPriority => GetOrder();

		private int GetOrder()
		{
			return -Canvas.sortingOrder;
		}
	}

	private static VRGUI _Instance;

	private IDictionary _Registry;

	private List<IScreenGrabber> _ScreenGrabbers = new List<IScreenGrabber>();

	private FieldInfo _Graphics;

	private RenderTexture _PrevRT;

	private Camera _VRGUICamera;

	private int _Listeners;

	private IDictionary<Camera, IScreenGrabber> _CameraMappings = new Dictionary<Camera, IScreenGrabber>();

	private HashSet<Camera> _CheckedCameras = new HashSet<Camera>();

	public static int Width { get; private set; }

	public static int Height { get; private set; }

	public SimulatedCursor SoftCursor { get; private set; }

	public static VRGUI Instance
	{
		get
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			if (!Object.op_Implicit((Object)(object)_Instance))
			{
				_Instance = new GameObject("VRGIN_GUI").AddComponent<VRGUI>();
				if (VR.Context.SimulateCursor)
				{
					_Instance.SoftCursor = SimulatedCursor.Create();
					((Component)_Instance.SoftCursor).transform.SetParent(((Component)_Instance).transform, false);
					VRLog.Info("Cursor is simulated");
				}
			}
			return _Instance;
		}
	}

	public RenderTexture uGuiTexture { get; private set; }

	public RenderTexture IMGuiTexture { get; private set; }

	public IEnumerable<IScreenGrabber> ScreenGrabbers => _ScreenGrabbers;

	public bool IsInterested(Camera camera)
	{
		return FindCameraMapping(camera) != null;
	}

	internal bool Owns(Camera cam)
	{
		return _CameraMappings.ContainsKey(cam);
	}

	public void Listen()
	{
		_Listeners++;
	}

	public void Unlisten()
	{
		_Listeners--;
	}

	protected override void OnAwake()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected O, but got Unknown
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Expected O, but got Unknown
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		WindowsInterop.RECT clientRect = WindowManager.GetClientRect();
		Width = clientRect.Right - clientRect.Left;
		Height = clientRect.Bottom - clientRect.Top;
		uGuiTexture = new RenderTexture(Width, Height, 24, (RenderTextureFormat)7);
		uGuiTexture.Create();
		IMGuiTexture = new RenderTexture(Width, Height, 0, (RenderTextureFormat)7);
		IMGuiTexture.Create();
		((Component)this).transform.localPosition = Vector3.zero;
		((Component)this).transform.localRotation = Quaternion.identity;
		((Component)((Component)this).transform).gameObject.AddComponent<FastGUI>();
		((Component)((Component)this).transform).gameObject.AddComponent<SlowGUI>();
		float num = (float)Height * 0.5f;
		float num2 = (float)Width * 0.5f;
		_VRGUICamera = new GameObject("VRGIN_GUICamera").AddComponent<Camera>();
		((Component)_VRGUICamera).transform.SetParent(((Component)this).transform, false);
		if (VR.Context.PreferredGUI == GUIType.IMGUI)
		{
			((Component)_VRGUICamera).transform.position = new Vector3(num2, num, -1f);
			((Component)_VRGUICamera).transform.rotation = Quaternion.identity;
			_VRGUICamera.orthographicSize = num;
		}
		_VRGUICamera.cullingMask = VR.Context.UILayerMask;
		_VRGUICamera.depth = 1f;
		_VRGUICamera.nearClipPlane = VR.Context.GuiNearClipPlane;
		_VRGUICamera.farClipPlane = VR.Context.GuiFarClipPlane;
		_VRGUICamera.targetTexture = uGuiTexture;
		_VRGUICamera.backgroundColor = Color.clear;
		_VRGUICamera.clearFlags = (CameraClearFlags)2;
		_VRGUICamera.orthographic = true;
		_VRGUICamera.useOcclusionCulling = false;
		_Graphics = typeof(GraphicRegistry).GetField("m_Graphics", BindingFlags.Instance | BindingFlags.NonPublic);
		_Registry = _Graphics.GetValue(GraphicRegistry.instance) as IDictionary;
		Object.DontDestroyOnLoad((Object)(object)((Component)this).gameObject);
	}

	private bool IsUnprocessed(Canvas c)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Invalid comparison between Unknown and I4
		if ((int)c.renderMode != 0)
		{
			if ((int)c.renderMode == 1 && (Object)(object)c.worldCamera != (Object)(object)_VRGUICamera)
			{
				return (Object)(object)c.worldCamera.targetTexture == (Object)null;
			}
			return false;
		}
		return true;
	}

	protected void CatchCanvas()
	{
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		_VRGUICamera.targetTexture = uGuiTexture;
		foreach (Canvas item in (_Registry.Keys as ICollection<Canvas>).Where((Canvas c) => Object.op_Implicit((Object)(object)c)).ToList().Where(IsUnprocessed))
		{
			if (VR.Interpreter.IsIgnoredCanvas(item))
			{
				continue;
			}
			VRLog.Info("Add {0} [Layer: {1}, SortingLayer: {2}, SortingOrder: {3}, RenderMode: {4}, Camera: {5}, Position: {6} ]", ((Object)item).name, LayerMask.LayerToName(((Component)item).gameObject.layer), item.sortingLayerName, item.sortingOrder, item.renderMode, item.worldCamera, ((Component)item).transform.position);
			item.renderMode = (RenderMode)1;
			item.worldCamera = _VRGUICamera;
			if (((1 << ((Component)item).gameObject.layer) & VR.Context.UILayerMask) == 0)
			{
				int layer = LayerMask.NameToLayer(VR.Context.UILayer);
				((Component)item).gameObject.layer = layer;
				Transform[] componentsInChildren = ((Component)item).gameObject.GetComponentsInChildren<Transform>(true);
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					((Component)componentsInChildren[i]).gameObject.layer = layer;
				}
			}
			if (VR.Context.EnforceDefaultGUIMaterials)
			{
				Graphic[] componentsInChildren2 = ((Component)item).gameObject.GetComponentsInChildren<Graphic>(true);
				foreach (Graphic obj in componentsInChildren2)
				{
					obj.material = obj.defaultMaterial;
				}
			}
			if (VR.Context.GUIAlternativeSortingMode)
			{
				GraphicRaycaster component = ((Component)item).GetComponent<GraphicRaycaster>();
				if (Object.op_Implicit((Object)(object)component))
				{
					Object.DestroyImmediate((Object)(object)component);
					SortingAwareGraphicRaycaster obj2 = ((Component)item).gameObject.AddComponent<SortingAwareGraphicRaycaster>();
					UnityHelper.SetPropertyOrField(obj2, "ignoreReversedGraphics", UnityHelper.GetPropertyOrField<GraphicRaycaster>(component, "ignoreReversedGraphics"));
					UnityHelper.SetPropertyOrField(obj2, "blockingObjects", UnityHelper.GetPropertyOrField<GraphicRaycaster>(component, "blockingObjects"));
					UnityHelper.SetPropertyOrField(obj2, "m_BlockingMask", UnityHelper.GetPropertyOrField<GraphicRaycaster>(component, "m_BlockingMask"));
				}
			}
		}
	}

	protected override void OnUpdate()
	{
		if (VR.Context.ConfineMouse)
		{
			Cursor.lockState = (CursorLockMode)2;
		}
		EnsureCameraTargets();
		if (_Listeners > 0)
		{
			CatchCanvas();
		}
		if (_Listeners < 0)
		{
			VRLog.Warn("Numbers don't add up!");
		}
	}

	protected override void OnLevel(int level)
	{
		base.OnLevel(level);
		_CheckedCameras.Clear();
		_CameraMappings.Clear();
	}

	internal void OnAfterGUI()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		if ((int)Event.current.type == 7)
		{
			RenderTexture.active = _PrevRT;
		}
	}

	internal void OnBeforeGUI()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		if ((int)Event.current.type == 7)
		{
			_PrevRT = RenderTexture.active;
			RenderTexture.active = IMGuiTexture;
			GL.Clear(true, true, Color.clear);
		}
	}

	public void AddGrabber(IScreenGrabber grabber)
	{
		if (!_ScreenGrabbers.Contains(grabber))
		{
			_ScreenGrabbers.Insert(0, grabber);
			RejudgeAll();
		}
	}

	public void RemoveGrabber(IScreenGrabber grabber)
	{
		_ScreenGrabbers.Remove(grabber);
		RejudgeAll();
	}

	private void RejudgeAll()
	{
		foreach (Camera item in _CheckedCameras.ToList())
		{
			AddCamera(item);
		}
	}

	public void AddCamera(Camera camera)
	{
		VRLog.Info("Trying to find a GUI mapping for camera {0}", ((Object)camera).name);
		IScreenGrabber screenGrabber = FindCameraMapping(camera);
		if (screenGrabber != null)
		{
			_CameraMappings[camera] = screenGrabber;
			screenGrabber.OnAssign(camera);
			VRLog.Info("Assigned camera {0} to {1}", ((Object)camera).name, screenGrabber);
		}
		_CheckedCameras.Add(camera);
	}

	private void EnsureCameraTargets()
	{
		List<Camera> list = new List<Camera>();
		foreach (KeyValuePair<Camera, IScreenGrabber> cameraMapping in _CameraMappings)
		{
			if (!Object.op_Implicit((Object)(object)cameraMapping.Key))
			{
				list.Add(cameraMapping.Key);
			}
			else if ((Object)(object)cameraMapping.Key.targetTexture != (Object)(object)cameraMapping.Value.GetTextures().FirstOrDefault())
			{
				cameraMapping.Key.targetTexture = cameraMapping.Value.GetTextures().FirstOrDefault();
			}
		}
		foreach (Camera item in list)
		{
			_CameraMappings.Remove(item);
		}
	}

	private IScreenGrabber FindCameraMapping(Camera camera)
	{
		foreach (IScreenGrabber screenGrabber in _ScreenGrabbers)
		{
			if (screenGrabber.Check(camera))
			{
				return screenGrabber;
			}
		}
		if (Check(camera))
		{
			return this;
		}
		return null;
	}

	public bool Check(Camera camera)
	{
		return VR.Interpreter.IsUICamera(camera);
	}

	public IEnumerable<RenderTexture> GetTextures()
	{
		yield return uGuiTexture;
		yield return IMGuiTexture;
	}

	public void OnAssign(Camera camera)
	{
		_VRGUICamera.depth = Mathf.Min(_VRGUICamera.depth, camera.depth - 1f);
	}
}

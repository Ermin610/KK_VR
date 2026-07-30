using System;
using System.Collections;
using Studio;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Events;
using UnityEngine.UI;
using VRGIN.Core;
using VRUtil;

namespace KKCharaStudioVR;

public class VRCameraMoveHelper : MonoBehaviour
{
	public bool showGUI = true;

	public RectTransform menuRect;

	private static VRCameraMoveHelper _instance;

	public bool keepY = true;

	public bool moveAlong;

	public Vector3 moveAlongBasePos;

	public Quaternion moveAlongBaseRot;

	private Studio.Studio studio;

	private GameObject moveDummy;

	private int windowID = 8752;

	private const int panelWidth = 400;

	private const int panelHeight = 100;

	private Rect windowRect = new Rect(-1f, -1f, 0f, 0f);

	private string windowTitle = "";

	private float DEFAULT_DISTANCE = 3f;

	private float DISTANCE_RATIO = 1f;

	public static VRCameraMoveHelper Instance => _instance;

	public static void Install(GameObject container)
	{
		if (_instance == null)
		{
			_instance = container.AddComponent<VRCameraMoveHelper>();
		}
	}

	private void Start()
	{
		StartCoroutine(StartupAlignCo());
	}

	private IEnumerator StartupAlignCo()
	{
		VRLog.Info("StartupAlignCo: Waiting for game to initialize...");
		
		// Wait a few seconds for SteamVR headset tracking to fully stabilize
		yield return new WaitForSeconds(3.0f);
		
		// Wait until the initial main scene loading is completely finished
		var sceneManager = Singleton<Manager.Scene>.Instance;
		if (sceneManager != null)
		{
			while (sceneManager.IsNowLoading || sceneManager.IsNowLoadingFade)
			{
				yield return null;
			}
		}

		// Wait an extra second to make sure VRGIN camera rigs are fully active and settled
		yield return new WaitForSeconds(1.0f);

		VRLog.Info("StartupAlignCo: Game initialized. Aligning VR camera and UI.");
		
		// Teleport player VR head to the initial scene camera position
		MoveToCurrent();
		
		// Reposition the main floating studio UI quad directly in front of the player's eyes
		float dist = 2.0f;
		var settings = VR.Manager.Context.Settings as KKCharaStudioVRSettings;
		if (settings != null)
		{
			dist = settings.UISpawnDistance;
		}
		RepositionMainUI(dist);
	}

	public static void RepositionMainUI(float dist)
	{
		try
		{
			Transform head = VR.Camera.Head;
			if (head == null) return;

			Type toolType = typeof(VRCameraMoveHelper).Assembly.GetType("KKCharaStudioVR.GripMoveKKCharaStudioTool");
			if (toolType != null)
			{
				var guiField = toolType.GetField("internalGui", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
				if (guiField != null)
				{
					var internalGui = guiField.GetValue(null) as VRGIN.Visuals.GUIQuad;
					if (internalGui != null)
					{
						VRLog.Info("RepositionMainUI: Repositioning internalGui directly!");
						((Component)internalGui).gameObject.SetActive(true);
						((Component)internalGui).transform.position = head.TransformPoint(new Vector3(0f, 0f, dist));
						((Component)internalGui).transform.rotation = Quaternion.LookRotation(head.TransformVector(new Vector3(0f, 0f, 1f)));
						((Component)internalGui).transform.localScale = Vector3.one * 0.8f;
						internalGui.UpdateAspect();
					}
				}
			}
		}
		catch (Exception ex)
		{
			VRLog.Error($"RepositionMainUI failed: {ex}");
		}
	}

	private void OnLevelWasLoaded(int level)
	{
		studio = Singleton<Studio.Studio>.Instance;
		if (!(studio == null))
		{
			Transform cameraMenuRootT = ((Component)studio).transform.Find("Canvas System Menu/02_Camera");
			_instance.Init(cameraMenuRootT);
		}
	}

	private void OnGUI()
	{
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Expected O, but got Unknown
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		if (!showGUI || !(menuRect != null) || !((Component)menuRect).gameObject.activeInHierarchy)
		{
			return;
		}
		GUISkin skin = GUI.skin;
		try
		{
			GUI.skin = VRIMGUIUtil.VRGUISkin;
			if (windowRect.x == -1f && windowRect.y == -1f)
			{
				windowRect = new Rect((float)(Screen.width / 2), 60f * ((Transform)menuRect).lossyScale.y, 400f, 100f);
			}
			windowRect = GUI.Window(windowID, windowRect, new GUI.WindowFunction(FuncWindowGUI), windowTitle);
		}
		finally
		{
			GUI.skin = skin;
		}
	}

	private void FuncWindowGUI(int winID)
	{
		try
		{
			GUI.enabled = true;
			GUILayout.BeginVertical((GUILayoutOption[])(object)new GUILayoutOption[0]);
			GUILayout.BeginHorizontal((GUILayoutOption[])(object)new GUILayoutOption[0]);
			GUILayoutOption[] array = (GUILayoutOption[])(object)new GUILayoutOption[2]
			{
				GUILayout.Width(80f),
				GUILayout.Height(35f)
			};
			if (GUILayout.Button("Back(1m)", array))
			{
				MoveForwardBackward(-1f);
			}
			if (GUILayout.Button("Back(2m)", array))
			{
				MoveForwardBackward(-2f);
			}
			if (GUILayout.Button("Jump", array))
			{
				MoveToSelectedObject(lockY: true);
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal((GUILayoutOption[])(object)new GUILayoutOption[0]);
			if (GUILayout.Button("Fwd(1m)", array))
			{
				MoveForwardBackward(1f);
			}
			if (GUILayout.Button("Fwd(2m)", array))
			{
				MoveForwardBackward(2f);
			}
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
			GUI.DragWindow();
		}
		catch (Exception value)
		{
			Console.WriteLine(value);
		}
	}

	public void SaveCamera(int slot)
	{
		if (!(VR.Camera.Head == null))
		{
			CurrentToCameraCtrl();
			studio.sceneInfo.cameraData[slot] = studio.cameraCtrl.Export();
		}
	}

	public void CurrentToCameraCtrl()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		VRCameraSyncController cameraSync = VRCameraSyncController.Instance;
		cameraSync?.Suspend();
		try
		{
			Studio.Studio activeStudio = studio ?? Singleton<Studio.Studio>.Instance;
			if (activeStudio == null || activeStudio.cameraCtrl == null || VR.Camera.Head == null)
			{
				return;
			}

			studio = activeStudio;
			GetCurrentLookDirAndRot(out var lookPoint, out var dir, out var rot);
			var val = new Studio.CameraControl.CameraData();
			VR.Camera.Head.TransformPoint(dir.normalized * DEFAULT_DISTANCE * DISTANCE_RATIO);
			Vector3 val2 = new Vector3(0f, 0f, -1f * DEFAULT_DISTANCE * DISTANCE_RATIO);

			Transform transformBase = activeStudio.cameraCtrl.transBase;
			if (transformBase != null)
			{
				lookPoint = transformBase.InverseTransformPoint(lookPoint);
				Quaternion localRotation =
					Quaternion.Inverse(transformBase.rotation) * Quaternion.Euler(rot);
				rot = localRotation.eulerAngles;
			}

			val.Set(lookPoint, rot, val2, activeStudio.cameraCtrl.fieldOfView);
			activeStudio.cameraCtrl.Import(val);
		}
		finally
		{
			cameraSync?.ResumeAndReset();
		}
	}

	private void GetCurrentLookDirAndRot(out Vector3 lookPoint, out Vector3 dir, out Vector3 rot)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		lookPoint = VR.Camera.Head.TransformPoint(Vector3.forward * DEFAULT_DISTANCE * DISTANCE_RATIO);
		Vector3 val = lookPoint;
		val.y = VR.Camera.Head.position.y;
		dir = val - VR.Camera.Head.position;
		if (dir == Vector3.zero)
		{
			dir = Vector3.forward;
		}
		Quaternion val2 = Quaternion.LookRotation(dir);
		rot = val2.eulerAngles;
	}

	public void MoveToCamera(int slot)
	{
		var val = studio.sceneInfo.cameraData[slot];
		studio.cameraCtrl.Import(val);
		MoveToCurrent();
	}

	public void MoveToCurrent()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (!TryGetCurrentStudioCameraPose(out Vector3 tobeHeadPos, out Quaternion tobeHeadRot))
			{
				return;
			}

			MoveTo(tobeHeadPos, tobeHeadRot);
		}
		finally
		{
			VRCameraSyncController.Instance?.CompleteNativeCameraReset();
		}
	}

	private bool TryGetCurrentStudioCameraPose(
		out Vector3 position,
		out Quaternion rotation)
	{
		position = Vector3.zero;
		rotation = Quaternion.identity;

		Studio.Studio activeStudio = studio ?? Singleton<Studio.Studio>.Instance;
		if (activeStudio == null || activeStudio.cameraCtrl == null)
		{
			return false;
		}

		studio = activeStudio;
		KKCharaStudioVRSettings settings =
			VR.Manager?.Context?.Settings as KKCharaStudioVRSettings;
		if (settings != null &&
		    settings.CameraSyncReadObjectCamera &&
		    activeStudio.ociCamera != null &&
		    activeStudio.ociCamera.objectItem != null)
		{
			Transform objectCamera = activeStudio.ociCamera.objectItem.transform;
			position = objectCamera.position;
			rotation = objectCamera.rotation;
			return true;
		}

		Studio.CameraControl.CameraData cameraData = activeStudio.cameraCtrl.Export();
		rotation = Quaternion.Euler(cameraData.rotate);
		Transform transformBase = activeStudio.cameraCtrl.transBase;
		if (transformBase != null)
		{
			rotation = transformBase.rotation * rotation;
			position =
				transformBase.TransformPoint(cameraData.pos) +
				rotation * cameraData.distance;
		}
		else
		{
			position = cameraData.pos + rotation * cameraData.distance;
		}

		return true;
	}

	public void MoveTo(Vector3 tobeHeadPos, Quaternion tobeHeadRot)
	{
		if (VR.Camera == null || VR.Camera.Head == null)
		{
			VRLog.Warn("VR.Camera or VR.Camera.Head is null, cannot MoveTo");
			return;
		}

		GameObject vROrigin = GetVROrigin();
		if (vROrigin != null)
		{
			if (moveDummy == null)
			{
				moveDummy = new GameObject("MoveDummy");
				UnityEngine.Object.DontDestroyOnLoad(moveDummy);
				moveDummy.transform.parent = ((Component)this).gameObject.transform;
			}

			Transform parent = vROrigin.transform.parent;
			moveDummy.transform.position = VR.Camera.Head.position;
			moveDummy.transform.rotation = GripMoveKKCharaStudioTool.RemoveXZRot(VR.Camera.Head.rotation);
			vROrigin.transform.parent = moveDummy.transform;
			moveDummy.transform.position = tobeHeadPos;
			moveDummy.transform.rotation = tobeHeadRot;
			vROrigin.transform.parent = parent;
			vROrigin.transform.rotation = GripMoveKKCharaStudioTool.RemoveXZRot(vROrigin.transform.rotation);
		}
	}

	private GameObject GetVROrigin()
	{
		if ((VR.Camera != null) && (VR.Camera.SteamCam != null) && (VR.Camera.SteamCam.origin != null))
		{
			return ((Component)VR.Camera.SteamCam.origin).gameObject;
		}
		return null;
	}

	public void MoveToSelectedObject(bool lockY)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		ObjectCtrlInfo[] selectObjectCtrl = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectObjectCtrl;
		if (selectObjectCtrl != null && selectObjectCtrl.Length != 0)
		{
			ObjectCtrlInfo val = selectObjectCtrl[0];
			Vector3 position = val.guideObject.transformTarget.position;
			if (val is OCIChar)
			{
				position = ((ChaInfo)((OCIChar)((val is OCIChar) ? val : null)).charInfo).objHead.transform.position;
			}
			MoveToPoint(position, lockY);
		}
	}

	public void MoveToPoint(Vector3 targetPos, bool lockY)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		GetCurrentLookDirAndRot(out var lookPoint, out var dir, out var rot);
		Vector3 val = targetPos - dir.normalized * 0.5f;
		if (lockY)
		{
			val.y = VR.Camera.Head.position.y;
		}
		else
		{
			val.y += VR.Camera.Head.position.y - lookPoint.y;
		}
		MoveTo(val, Quaternion.Euler(rot));
	}

	public void MoveForwardBackward(float distance)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		GetCurrentLookDirAndRot(out var _, out var dir, out var rot);
		Vector3 tobeHeadPos = VR.Camera.Head.position + dir * distance;
		tobeHeadPos.y = VR.Camera.Head.position.y;
		MoveTo(tobeHeadPos, Quaternion.Euler(rot));
	}

	private void Init(Transform cameraMenuRootT)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Expected O, but got Unknown
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Expected O, but got Unknown
		VRLog.Info("Initializing VRCameraMoveHelper");
		try
		{
			menuRect = ((Component)cameraMenuRootT).GetComponent<RectTransform>();
			if (moveDummy == null)
			{
				moveDummy = new GameObject("MoveDummy");
				UnityEngine.Object.DontDestroyOnLoad(moveDummy);
				moveDummy.transform.parent = ((Component)this).gameObject.transform;
			}
			for (int i = 0; i < ((Transform)menuRect).childCount; i++)
			{
				Transform child = ((Transform)menuRect).GetChild(i);
				int idx = -1;
				if (int.TryParse(((UnityEngine.Object)child).name, out idx))
				{
					((UnityEvent)((Component)child.Find("Button Save")).gameObject.GetComponent<Button>().onClick).AddListener((UnityAction)delegate
					{
						OnSaveButtonClick(idx);
					});
					((UnityEvent)((Component)child.Find("Button Load")).gameObject.GetComponent<Button>().onClick).AddListener((UnityAction)delegate
					{
						OnLoadButtonClick(idx);
					});
				}
				else
				{
					VRLog.Info("Not Found. {0}", ((UnityEngine.Object)child).name);
				}
			}
		}
		catch (Exception obj)
		{
			VRLog.Error(obj);
		}
		VRLog.Info("VR Camera Helper installed.");
	}

	private void OnSaveButtonClick(int idx)
	{
		SaveCamera(idx);
	}

	private void OnLoadButtonClick(int idx)
	{
		MoveToCamera(idx);
	}
}

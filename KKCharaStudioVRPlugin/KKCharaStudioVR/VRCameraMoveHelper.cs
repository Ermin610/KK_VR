using System;
using Studio;
using UnityEngine;
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

	private Studio studio;

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
		if ((Object)(object)_instance == (Object)null)
		{
			_instance = container.AddComponent<VRCameraMoveHelper>();
		}
	}

	private void Start()
	{
	}

	private void OnLevelWasLoaded(int level)
	{
		studio = Singleton<Studio>.Instance;
		if (!((Object)(object)studio == (Object)null))
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
		if (!showGUI || !((Object)(object)menuRect != (Object)null) || !((Component)menuRect).gameObject.activeInHierarchy)
		{
			return;
		}
		GUISkin skin = GUI.skin;
		try
		{
			GUI.skin = VRIMGUIUtil.VRGUISkin;
			if (((Rect)(ref windowRect)).x == -1f && ((Rect)(ref windowRect)).y == -1f)
			{
				windowRect = new Rect((float)(Screen.width / 2), 60f * ((Transform)menuRect).lossyScale.y, 400f, 100f);
			}
			windowRect = GUI.Window(windowID, windowRect, new WindowFunction(FuncWindowGUI), windowTitle);
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
		if (!((Object)(object)VR.Camera.Head == (Object)null))
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
		GetCurrentLookDirAndRot(out var lookPoint, out var dir, out var rot);
		CameraData val = new CameraData();
		VR.Camera.Head.TransformPoint(((Vector3)(ref dir)).normalized * DEFAULT_DISTANCE * DISTANCE_RATIO);
		Vector3 val2 = default(Vector3);
		((Vector3)(ref val2))._002Ector(0f, 0f, -1f * DEFAULT_DISTANCE * DISTANCE_RATIO);
		val.Set(lookPoint, rot, val2, studio.cameraCtrl.fieldOfView);
		studio.cameraCtrl.Import(val);
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
		rot = ((Quaternion)(ref val2)).eulerAngles;
	}

	public void MoveToCamera(int slot)
	{
		CameraData val = studio.sceneInfo.cameraData[slot];
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
		CameraData val = studio.cameraCtrl.Export();
		Vector3 tobeHeadPos = val.pos + Quaternion.Euler(val.rotate) * val.distance;
		Quaternion tobeHeadRot = Quaternion.Euler(val.rotate);
		MoveTo(tobeHeadPos, tobeHeadRot);
	}

	public void MoveTo(Vector3 tobeHeadPos, Quaternion tobeHeadRot)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		Transform val = null;
		GameObject vROrigin = GetVROrigin();
		if (!((Object)(object)vROrigin == (Object)null))
		{
			val = vROrigin.transform.parent;
			moveDummy.transform.position = VR.Camera.Head.position;
			moveDummy.transform.rotation = GripMoveKKCharaStudioTool.RemoveXZRot(VR.Camera.Head.rotation);
			vROrigin.transform.parent = moveDummy.transform;
			moveDummy.transform.position = tobeHeadPos;
			moveDummy.transform.rotation = tobeHeadRot;
			vROrigin.transform.parent = val;
			vROrigin.transform.rotation = GripMoveKKCharaStudioTool.RemoveXZRot(vROrigin.transform.rotation);
		}
	}

	private GameObject GetVROrigin()
	{
		if (Object.op_Implicit((Object)(object)VR.Camera) && Object.op_Implicit((Object)(object)VR.Camera.SteamCam) && Object.op_Implicit((Object)(object)VR.Camera.SteamCam.origin))
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
		ObjectCtrlInfo[] selectObjectCtrl = Singleton<Studio>.Instance.treeNodeCtrl.selectObjectCtrl;
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
		Vector3 val = targetPos - ((Vector3)(ref dir)).normalized * 0.5f;
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
			if ((Object)(object)moveDummy == (Object)null)
			{
				moveDummy = new GameObject("MoveDummy");
				Object.DontDestroyOnLoad((Object)(object)moveDummy);
				moveDummy.transform.parent = ((Component)this).gameObject.transform;
			}
			for (int i = 0; i < ((Transform)menuRect).childCount; i++)
			{
				Transform child = ((Transform)menuRect).GetChild(i);
				int idx = -1;
				if (int.TryParse(((Object)child).name, out idx))
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
					VRLog.Info("Not Found. {0}", ((Object)child).name);
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

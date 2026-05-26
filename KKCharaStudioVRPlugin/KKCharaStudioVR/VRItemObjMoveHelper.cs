using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Studio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using VRGIN.Core;
using VRGIN.Helpers;

namespace KKCharaStudioVR;

public class VRItemObjMoveHelper : MonoBehaviour
{
	public bool showGUI = true;

	public RectTransform menuRect;

	private Canvas workspaceCanvas;

	private static VRItemObjMoveHelper _instance;

	public bool keepY = true;

	public bool moveAlong;

	public Vector3 moveAlongBasePos;

	public Quaternion moveAlongBaseRot;

	private ObjMoveHelper helper = new ObjMoveHelper();

	private GameObject steamVRHeadOrigin;

	private Studio.Studio studio;

	private GameObject moveDummy;

	private int windowID = 8751;

	private const int panelWidth = 300;

	private const int panelHeight = 150;

	private Rect windowRect = new Rect(0f, 0f, 300f, 150f);

	private string windowTitle = "";

	private Texture2D bgTex;

	private Button callButton;

	private Button callXZButton;

	private static FieldInfo f_m_TreeNodeObject = typeof(TreeNodeCtrl).GetField("m_TreeNodeObject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField);

	private static MethodInfo m_AddSelectNode = typeof(TreeNodeCtrl).GetMethod("AddSelectNode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod);

	public static VRItemObjMoveHelper Instance => _instance;

	public static void Install(GameObject container)
	{
		if (_instance == null)
		{
			_instance = container.AddComponent<VRItemObjMoveHelper>();
		}
	}

	private void OnLevelWasLoaded(int level)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected O, but got Unknown
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		studio = Singleton<Studio.Studio>.Instance;
		if (!(studio == null))
		{
			Transform objectListCanvas = ((Component)studio).gameObject.transform.Find("Canvas Object List");
			_instance.Init(objectListCanvas);
			bgTex = new Texture2D(1, 1, (TextureFormat)5, false);
			bgTex.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.3f, 1f));
			bgTex.Apply();
		}
	}

	private Texture2D LoadImage(string path)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		Texture2D val = new Texture2D(1, 1, (TextureFormat)5, false);
		byte[] array = File.ReadAllBytes(path);
		val.LoadImage(array);
		return val;
	}

	public static Rect RectTransformToScreenSpace(RectTransform transform)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		Rect rect = transform.rect;
		Vector2 val = Vector2.Scale(rect.size, (Vector2)(((Transform)transform).lossyScale));
		Rect result = default(Rect);
		result = new Rect(((Transform)transform).position.x, (float)Screen.height - ((Transform)transform).position.y, val.x, val.y);
		result.x = result.x - transform.pivot.x * val.x;
		result.y = result.y - (1f - transform.pivot.y) * val.y;
		return result;
	}

	private void Init(Transform objectListCanvas)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Expected O, but got Unknown
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Expected O, but got Unknown
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Expected O, but got Unknown
		//IL_0286: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0302: Expected O, but got Unknown
		//IL_0310: Unknown result type (might be due to invalid IL or missing references)
		//IL_031a: Expected O, but got Unknown
		workspaceCanvas = ((Component)objectListCanvas).gameObject.GetComponent<Canvas>();
		menuRect = ((Component)objectListCanvas.Find("Image Bar/Scroll View")).gameObject.GetComponent<RectTransform>();
		steamVRHeadOrigin = ((Component)VR.Camera.SteamCam.origin).gameObject;
		if (moveDummy == null)
		{
			moveDummy = new GameObject("MoveDummy");
			UnityEngine.Object.DontDestroyOnLoad(moveDummy);
			moveDummy.transform.parent = ((Component)this).gameObject.transform;
		}
		Transform val = objectListCanvas.Find("Image Bar/Button Duplicate");
		Transform val2 = objectListCanvas.Find("Image Bar/Button Route");
		Transform val3 = objectListCanvas.Find("Image Bar/Button Camera");
		if (val != null)
		{
			float num = val3.localPosition.x - val2.localPosition.x;
			Sprite val4 = Sprite.Create(UnityHelper.LoadImage("KKCharaStudioVR/icon_call.png"), new Rect(0f, 0f, 32f, 32f), Vector2.zero);
			Sprite val5 = Sprite.Create(UnityHelper.LoadImage("KKCharaStudioVR/icon_call_xz.png"), new Rect(0f, 0f, 32f, 32f), Vector2.zero);
			if (callButton == null)
			{
				GameObject obj = UnityEngine.Object.Instantiate<GameObject>(((Component)val).gameObject);
				((UnityEngine.Object)obj).name = "Button Call";
				obj.transform.SetParent(((Component)val).transform.parent);
				obj.transform.localPosition = new Vector3(val2.localPosition.x - num * 2f, val2.localPosition.y, val2.localPosition.z);
				obj.transform.localScale = Vector3.one;
				Button component = obj.GetComponent<Button>();
				SpriteState spriteState = default(SpriteState);
				spriteState.disabledSprite = val4;
				spriteState.highlightedSprite = val4;
				spriteState.pressedSprite = val4;
				((Selectable)component).spriteState = spriteState;
				component.onClick = new Button.ButtonClickedEvent();
				((UnityEvent)component.onClick).AddListener(new UnityAction(OnCallClick));
				((Selectable)component).interactable = true;
				callButton = component;
				UnityEngine.Object.DestroyImmediate(obj.GetComponent<Image>());
				Image obj2 = obj.AddComponent<Image>();
				obj2.sprite = val4;
				obj2.type = (Image.Type)0;
				((Graphic)obj2).SetAllDirty();
			}
			if (callXZButton == null)
			{
				GameObject obj3 = UnityEngine.Object.Instantiate<GameObject>(((Component)val).gameObject);
				((UnityEngine.Object)obj3).name = "Button Call YLock";
				obj3.transform.SetParent(((Component)val).transform.parent);
				obj3.transform.localPosition = new Vector3(val2.localPosition.x - num, val2.localPosition.y, val2.localPosition.z);
				obj3.transform.localScale = Vector3.one;
				Button component2 = obj3.GetComponent<Button>();
				SpriteState spriteState2 = default(SpriteState);
				spriteState2.disabledSprite = val5;
				spriteState2.highlightedSprite = val5;
				spriteState2.pressedSprite = val5;
				((Selectable)component2).spriteState = spriteState2;
				component2.onClick = new Button.ButtonClickedEvent();
				((UnityEvent)component2.onClick).AddListener(new UnityAction(OnCallClickYLock));
				((Selectable)component2).interactable = true;
				callXZButton = component2;
				UnityEngine.Object.DestroyImmediate(obj3.GetComponent<Image>());
				Image obj4 = obj3.AddComponent<Image>();
				obj4.sprite = val5;
				obj4.type = (Image.Type)0;
				((Graphic)obj4).SetAllDirty();
			}
		}
		VRLog.Info("VR ItemObjMoveHelper installed");
	}

	private void OnCallClick()
	{
		MoveAllCharaAndItemsHere();
	}

	private void OnCallClickYLock()
	{
		MoveAllCharaAndItemsHere(keepY: true);
	}

	public void CallCurrentObject(bool keepY = false)
	{
		if (studio.treeNodeCtrl.selectObjectCtrl != null && studio.treeNodeCtrl.selectObjectCtrl.Length != 0)
		{
			ObjectCtrlInfo val = studio.treeNodeCtrl.selectObjectCtrl[0];
			if (val != null)
			{
				MoveObjectHere(val);
			}
		}
	}

	public void MoveAllCharaAndItemsHere(bool keepY = false)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		Vector3 newPos = VR.Camera.Head.TransformPoint(0f, 0f, 0.2f);
		ObjectCtrlInfo firstObject = helper.GetFirstObject();
		if (firstObject != null)
		{
			helper.moveAlongBasePos = firstObject.guideObject.transformTarget.position;
			helper.MoveAllCharaAndItemsHere(newPos, keepY);
			moveAlongBasePos = newPos;
		}
	}

	public void MoveObjectHere(ObjectCtrlInfo oci)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		Vector3 newPos = VR.Camera.Head.TransformPoint(0f, 0f, 0.2f);
		helper.MoveObject(oci, newPos, keepY);
	}

	public void VRToggleObjectSelectOnCursor()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		Studio.Studio instance = Singleton<Studio.Studio>.Instance;
		if (instance == null)
		{
			return;
		}
		PointerEventData val = new PointerEventData(EventSystem.current);
		val.position = (Vector2)(Input.mousePosition);
		List<RaycastResult> list = new List<RaycastResult>();
		EventSystem.current.RaycastAll(val, list);
		foreach (RaycastResult item in list)
		{
			RaycastResult current = item;
			if (current.gameObject != null && current.gameObject.transform.parent != null)
			{
				TreeNodeObject component = ((Component)current.gameObject.transform.parent).gameObject.GetComponent<TreeNodeObject>();
				if (component != null && instance.dicInfo.ContainsKey(component))
				{
					m_AddSelectNode.Invoke(instance.treeNodeCtrl, new object[2] { component, true });
					break;
				}
			}
		}
	}
}

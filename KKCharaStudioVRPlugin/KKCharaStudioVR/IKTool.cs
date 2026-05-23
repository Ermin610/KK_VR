using System;
using System.Collections;
using System.Collections.Generic;
using Studio;
using UnityEngine;

namespace KKCharaStudioVR;

public class IKTool : MonoBehaviour
{
	private bool markerShowOverlay = true;

	private float markerOverlayAlpha = 0.4f;

	private float markerSize = 0.06f;

	private float markerSizeBend = 0.05f;

	private Material markerSharedMaterial;

	private Material markerSharedMaterial_IKTarget;

	private Material markerSharedMaterial_IKBendTarget;

	private GameObject handle;

	public static IKTool instance;

	private const float DEFAUTL_SCALE_POS = 0.25f;

	private float DEFAULT_SCALE_POS_XYZ_DIST = Mathf.Sqrt(0.1875f);

	public static IKTool Create(GameObject container)
	{
		if ((Object)(object)instance != (Object)null)
		{
			return instance;
		}
		instance = container.AddComponent<IKTool>();
		return instance;
	}

	private void Awake()
	{
	}

	private void Start()
	{
		StartWatch();
	}

	private IEnumerator InstallMoveableObjectCo()
	{
		Studio studio = Singleton<Studio>.Instance;
		_ = Singleton<GuideObjectManager>.Instance;
		while (true)
		{
			yield return (object)new WaitForSeconds(1f);
			try
			{
				foreach (KeyValuePair<int, ObjectCtrlInfo> item in studio.dicObjectCtrl)
				{
					if (!((Object)(object)((Component)item.Value.guideObject).gameObject != (Object)null))
					{
						continue;
					}
					ObjectCtrlInfo value = item.Value;
					MakeObjectMoveable(value.guideObject, replaceMaterial: true, installToCenter: true);
					if (!(value is OCIChar))
					{
						continue;
					}
					OCIChar val = (OCIChar)(object)((value is OCIChar) ? value : null);
					foreach (IKInfo item2 in val.listIKTarget)
					{
						MakeObjectMoveable(item2.guideObject, replaceMaterial: true);
					}
					foreach (BoneInfo listBone in val.listBones)
					{
						MakeObjectMoveable(listBone.guideObject, replaceMaterial: true);
					}
				}
			}
			catch (Exception value2)
			{
				Console.WriteLine(value2);
			}
		}
	}

	private void OnLevelWasLoaded(int level)
	{
		StartWatch();
	}

	private void StartWatch()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected O, but got Unknown
		((MonoBehaviour)this).StopAllCoroutines();
		((MonoBehaviour)this).StartCoroutine(InstallMoveableObjectCo());
		if ((Object)(object)handle == (Object)null)
		{
			handle = new GameObject("handle");
			handle.transform.parent = ((Component)this).gameObject.transform;
		}
	}

	private void MakeObjectMoveable(GuideObject guideObject, bool replaceMaterial = false, bool installToCenter = false)
	{
		if (!((Object)(object)guideObject.transformTarget == (Object)null))
		{
			if (installToCenter)
			{
				InstallGripMoveMarker(((Component)guideObject).gameObject, OnObjectMove, guideObject, replaceMaterial, installToCenter);
			}
			else
			{
				GameObject gameObject = ((Component)((Component)guideObject).gameObject.transform.Find("Sphere")).gameObject;
				InstallGripMoveMarker(gameObject, OnObjectMove, guideObject, replaceMaterial, installToCenter);
			}
			if (guideObject.enableScale)
			{
				InstallScaleMoveMarker(guideObject);
			}
		}
	}

	private bool InstallGripMoveMarker(GameObject target, Action<MonoBehaviour> moveHandler, GuideObject guideObject, bool replaceMaterial, bool installToCenter)
	{
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Expected O, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Expected O, but got Unknown
		if ((Object)(object)target.transform.Find("_gripmovemarker") == (Object)null)
		{
			Renderer visibleReference = null;
			GameObject val;
			if (installToCenter)
			{
				val = GameObject.CreatePrimitive((PrimitiveType)0);
				((Object)val).name = "_gripmovemarker";
				val.layer = LayerMask.NameToLayer("Studio/Select");
				Renderer component = val.GetComponent<Renderer>();
				Material val2 = new Material(MaterialHelper.GetColorZOrderShader());
				val2.color = new Color(0f, 1f, 0f, 0.3f);
				val2.SetFloat("_AlphaRatio", 1.5f);
				val2.renderQueue = 3800;
				component.material = val2;
				Transform val3 = target.transform.Find("move/XYZ");
				if ((Object)(object)val3 != (Object)null)
				{
					visibleReference = ((Component)val3).gameObject.GetComponent<Renderer>();
				}
			}
			else
			{
				val = new GameObject("_gripmovemarker");
				if (replaceMaterial)
				{
					Renderer component2 = target.GetComponent<Renderer>();
					if ((Object)(object)component2 != (Object)null)
					{
						Material val4 = new Material(MaterialHelper.GetColorZOrderShader());
						val4.CopyPropertiesFromMaterial(component2.material);
						val4.SetFloat("_AlphaRatio", 1.5f);
						val4.renderQueue = 3800;
						component2.material = val4;
						visibleReference = component2;
					}
				}
			}
			SphereCollider val5 = val.AddComponent<SphereCollider>();
			Transform transform = val.transform;
			((Component)transform).transform.parent = target.transform;
			((Component)transform).transform.localPosition = Vector3.zero;
			((Component)transform).transform.rotation = guideObject.transformTarget.rotation;
			((Component)transform).transform.localScale = Vector3.one;
			((Collider)val5).isTrigger = true;
			MoveableGUIObject moveableGUIObject = val.AddComponent<MoveableGUIObject>();
			moveableGUIObject.guideObject = guideObject;
			moveableGUIObject.onMoveLister.Add(moveHandler);
			moveableGUIObject.visibleReference = visibleReference;
			if (installToCenter)
			{
				moveableGUIObject.isMoveObj = true;
			}
			return true;
		}
		return false;
	}

	private bool InstallScaleMoveMarker(GuideObject guideObject)
	{
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Expected O, but got Unknown
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		Transform val = ((Component)guideObject).gameObject.transform.Find("scale");
		if ((Object)(object)((Component)val).transform.Find("X/_gripmovemarker_scale") == (Object)null)
		{
			string[] array = new string[4] { "XYZ", "X", "Y", "Z" };
			foreach (string text in array)
			{
				Transform val2 = val.Find(text);
				GuideScale component = ((Component)val2).gameObject.GetComponent<GuideScale>();
				if ((Object)(object)component != (Object)null)
				{
					GameObject obj = GameObject.CreatePrimitive((PrimitiveType)0);
					((Object)obj).name = "_gripmovemarker_scale";
					obj.layer = LayerMask.NameToLayer("Studio/Select");
					Renderer component2 = obj.GetComponent<Renderer>();
					Material val3 = new Material(MaterialHelper.GetColorZOrderShader());
					val3.color = new Color(0f, 1f, 1f, 0.3f);
					val3.SetFloat("_AlphaRatio", 1.5f);
					val3.renderQueue = 3800;
					component2.material = val3;
					Renderer val4 = null;
					val4 = ((Component)val2).gameObject.GetComponent<Renderer>();
					SphereCollider val5 = obj.AddComponent<SphereCollider>();
					Transform transform = obj.transform;
					((Component)transform).transform.parent = val2;
					((Component)transform).transform.localPosition = CalcScaleHandleDefaultPos(component);
					((Component)transform).transform.rotation = guideObject.transformTarget.rotation;
					((Component)transform).transform.localScale = Vector3.one;
					((Collider)val5).isTrigger = true;
					MoveableGUIObject moveableGUIObject = obj.AddComponent<MoveableGUIObject>();
					moveableGUIObject.guideObject = guideObject;
					moveableGUIObject.guideScale = component;
					moveableGUIObject.onMoveLister.Add(OnScaleMove);
					moveableGUIObject.onReleasedLister.Add(OnScaleReleased);
					moveableGUIObject.visibleReference = val4;
				}
			}
			return true;
		}
		return false;
	}

	private void OnObjectMove(MonoBehaviour marker)
	{
		DoOnMove(marker, ((Component)marker).transform.parent);
	}

	private void OnObjectCubeMoveNoRotation(MonoBehaviour marker)
	{
		DoOnMove(marker, ((Component)marker).transform.parent.parent, rotation: false);
	}

	private void OnObjectRotationNoMove(MonoBehaviour marker)
	{
		DoOnMove(marker, ((Component)marker).transform.parent, rotation: true, pos: false);
	}

	private void OnRawObjectMove(MonoBehaviour marker)
	{
		DoOnMove(marker, ((Component)marker).transform.parent);
	}

	private void DoOnMove(MonoBehaviour marker, Transform target, bool rotation = true, bool pos = true)
	{
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		MoveableGUIObject component = ((Component)marker).GetComponent<MoveableGUIObject>();
		Transform parent = ((Component)marker).transform.parent;
		GuideObject guideObject = component.guideObject;
		pos &= guideObject.enablePos;
		rotation &= guideObject.enableRot;
		if (pos)
		{
			target.position += ((Component)marker).transform.position - ((Component)parent).transform.position;
			((Component)guideObject.transformTarget).transform.position = target.position;
			guideObject.changeAmount.pos = guideObject.transformTarget.localPosition;
		}
		((Component)marker).transform.localPosition = Vector3.zero;
		if (rotation)
		{
			guideObject.transformTarget.rotation = ((Component)marker).transform.rotation;
			guideObject.changeAmount.rot = guideObject.transformTarget.localEulerAngles;
		}
	}

	private void OnScaleMove(MonoBehaviour marker)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Expected I4, but got Unknown
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		MoveableGUIObject component = ((Component)marker).GetComponent<MoveableGUIObject>();
		_ = ((Component)marker).transform.parent;
		GuideObject guideObject = component.guideObject;
		GuideScale guideScale = component.guideScale;
		if (!guideObject.enableScale || !Object.op_Implicit((Object)(object)component.guideScale))
		{
			return;
		}
		Vector3 localPosition = ((Component)marker).transform.localPosition;
		float magnitude = ((Vector3)(ref localPosition)).magnitude;
		if (magnitude > 0f)
		{
			float num = magnitude / 0.25f;
			Vector3 val = component.oldScale;
			ScaleAxis axis = guideScale.axis;
			switch ((int)axis)
			{
			case 3:
				val *= num;
				break;
			case 0:
				val.x *= num;
				break;
			case 1:
				val.y *= num;
				break;
			case 2:
				val.z *= num;
				break;
			}
			val.x = Mathf.Max(val.x, 0.01f);
			val.y = Mathf.Max(val.y, 0.01f);
			val.z = Mathf.Max(val.z, 0.01f);
			guideObject.changeAmount.scale = val;
		}
	}

	private void OnScaleReleased(MonoBehaviour marker)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		MoveableGUIObject component = ((Component)marker).GetComponent<MoveableGUIObject>();
		GuideObject guideObject = component.guideObject;
		GuideScale guideScale = component.guideScale;
		if (guideObject.enableScale && Object.op_Implicit((Object)(object)guideScale))
		{
			((Component)marker).transform.localPosition = CalcScaleHandleDefaultPos(guideScale);
		}
	}

	private Vector3 CalcScaleHandleDefaultPos(GuideScale guideScale)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected I4, but got Unknown
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		ScaleAxis axis = guideScale.axis;
		return (Vector3)((int)axis switch
		{
			3 => new Vector3(0.25f, 0.25f, 0.25f) * 0.25f / DEFAULT_SCALE_POS_XYZ_DIST, 
			0 => new Vector3(0.25f, 0f, 0f), 
			1 => new Vector3(0f, 0.25f, 0f), 
			2 => new Vector3(0f, 0f, 0.25f), 
			_ => Vector3.zero, 
		});
	}

	private Material CreateForceDrawMaterial()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		try
		{
			Material val = new Material(MaterialHelper.GetColorZOrderShader());
			Color red = Color.red;
			red.a = markerOverlayAlpha;
			val.SetColor("_Color", red);
			return val;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
			return null;
		}
	}
}

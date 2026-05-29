using UnityEngine;
using UnityEngine.UI;
using VRGIN.Core;

namespace VRGIN.Controls;

public class HelpText : ProtectedBehaviour
{
	private Vector3 _TextOffset;

	private Vector3 _LineOffset;

	private Vector3 _HeightVector;

	private Vector3 _MovementVector;

	private Transform _Target;

	private string _Text;

	private static Material S_Material;

	private LineRenderer _Line;

	public static HelpText Create(string text, Transform target, Vector3 textOffset, Vector3? lineOffset = null)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		HelpText helpText = new GameObject().AddComponent<HelpText>();
		helpText._Text = text;
		helpText._Target = target;
		helpText._TextOffset = textOffset;
		helpText._LineOffset = (lineOffset.HasValue ? lineOffset.Value : Vector3.zero);
		Vector3 val = (lineOffset.HasValue ? (textOffset - lineOffset.Value) : textOffset);
		helpText._HeightVector = Vector3.Project(val, Vector3.up);
		helpText._MovementVector = Vector3.ProjectOnPlane(val, Vector3.up);
		return helpText;
	}

	protected override void OnStart()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0305: Unknown result type (might be due to invalid IL or missing references)
		//IL_0339: Unknown result type (might be due to invalid IL or missing references)
		base.OnStart();
		((Component)this).transform.SetParent(_Target, false);
		Canvas val = new GameObject().AddComponent<Canvas>();
		((Component)val).transform.SetParent(((Component)this).transform, false);
		val.renderMode = (RenderMode)2;
		((Component)val).GetComponent<RectTransform>().SetSizeWithCurrentAnchors((RectTransform.Axis)0, 300f);
		((Component)val).GetComponent<RectTransform>().SetSizeWithCurrentAnchors((RectTransform.Axis)1, 70f);
		((Component)this).transform.rotation = _Target.parent.rotation;
		((Component)val).transform.localScale = new Vector3(0.0001549628f, 0.0001549627f, 0f);
		((Component)val).transform.localPosition = _TextOffset;
		((Component)val).transform.localRotation = Quaternion.Euler(90f, 180f, 180f);
		Text obj = new GameObject().AddComponent<Text>();
		((Component)obj).transform.SetParent(((Component)val).transform, false);
		((Component)obj).GetComponent<RectTransform>().anchorMin = Vector2.zero;
		((Component)obj).GetComponent<RectTransform>().anchorMax = Vector2.one;
		obj.resizeTextForBestFit = true;
		obj.resizeTextMaxSize = 40;
		obj.resizeTextMinSize = 1;
		((Graphic)obj).color = Color.black;
		obj.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
		obj.horizontalOverflow = (HorizontalWrapMode)0;
		obj.verticalOverflow = (VerticalWrapMode)0;
		obj.alignment = (TextAnchor)4;
		obj.text = _Text;
		_Line = ((Component)this).gameObject.AddComponent<LineRenderer>();
		((Renderer)_Line).material = Resources.GetBuiltinResource<Material>("Sprites-Default.mat");
		_Line.SetColors(Color.cyan, Color.cyan);
		_Line.useWorldSpace = false;
		_Line.SetVertexCount(4);
		_Line.SetWidth(0.001f, 0.001f);
		Quaternion.Inverse(_Target.localRotation);
		_Line.SetPosition(0, _LineOffset + _HeightVector * 0.1f);
		_Line.SetPosition(1, _LineOffset + _HeightVector * 0.5f + _MovementVector * 0.2f);
		_Line.SetPosition(2, _TextOffset - _HeightVector * 0.5f - _MovementVector * 0.2f);
		_Line.SetPosition(3, _TextOffset - _HeightVector * 0.1f);
		GameObject obj2 = GameObject.CreatePrimitive((PrimitiveType)5);
		obj2.transform.SetParent(((Component)this).transform, false);
		obj2.transform.localPosition = _TextOffset - Vector3.up * 0.001f;
		obj2.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
		obj2.transform.localScale = new Vector3(0.05539737f, 0.009849964f, 0f);
		if (!(S_Material != null))
		{
			S_Material = VRManager.Instance.Context.Materials.Unlit;
			S_Material.color = Color.white;
		}
		((Component)obj2.transform).GetComponent<Renderer>().sharedMaterial = S_Material;
		obj2.GetComponent<Collider>().enabled = false;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
	}
}

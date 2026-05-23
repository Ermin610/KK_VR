using UnityEngine;
using VRGIN.Core;
using VRGIN.Helpers;

namespace VRGIN.Visuals;

public class SimulatedCursor : ProtectedBehaviour
{
	private Texture2D _Sprite;

	private Texture2D _DefaultSprite;

	private Vector2 _Scale;

	public static SimulatedCursor Create()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		return new GameObject("VRGIN_Cursor").AddComponent<SimulatedCursor>();
	}

	protected override void OnAwake()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		base.OnAwake();
		_DefaultSprite = UnityHelper.LoadImage("cursor.png");
		_Scale = new Vector2((float)((Texture)_DefaultSprite).width, (float)((Texture)_DefaultSprite).height) * 0.5f;
	}

	protected override void OnStart()
	{
		base.OnStart();
	}

	private void OnGUI()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		GUI.depth = -2147483647;
		if (Cursor.visible)
		{
			Vector2 val = default(Vector2);
			((Vector2)(ref val))._002Ector(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y);
			GUI.DrawTexture(new Rect(val.x, val.y, _Scale.x, _Scale.y), (Texture)(object)(_Sprite ?? _DefaultSprite));
		}
	}

	public void SetCursor(Texture2D texture)
	{
		_Sprite = texture;
	}
}

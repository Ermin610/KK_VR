using Leap.Unity;
using UnityEngine;
using VRGIN.Core;
using VRGIN.Native;
using VRGIN.Visuals;

namespace VRGIN.Controls.LeapMotion;

public class LeapMenuHandler : ProtectedBehaviour
{
	private enum RelativePosition
	{
		Out,
		Hover,
		Behind
	}

	private enum State
	{
		None,
		Hover,
		Press
	}

	private class AnalyzationResult
	{
		public RelativePosition Position;

		public Vector3 ClosestPoint = Vector3.zero;

		public Vector2 TextureCoords;
	}

	private HandModel _Hand;

	private const int MOUSE_STABILIZER_THRESHOLD = 50;

	private const float HOVER_HEIGHT = 0.05f;

	private const float MAX_DEPTH = 0.1f;

	private const int FINGER_INDEX = 1;

	private const int MAX_OVERLAP = 0;

	private GUIQuad _Current;

	private State _CurrentState;

	private Vector2? mouseDownPosition;

	private Vector3 _ScaleVector;

	private Vector3 TipPosition => _Hand.fingers[1].GetTipPosition();

	protected override void OnStart()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		_Hand = ((Component)this).GetComponent<HandModel>();
		_ScaleVector = ((Vector2)(new Vector2((float)VRGUI.Width / (float)Screen.width, (float)VRGUI.Height / (float)Screen.height)));
		if (!(_Hand != null))
		{
			VRLog.Error("Hand not found! Disabling...");
			((Behaviour)this).enabled = false;
		}
	}

	protected override void OnUpdate()
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		base.OnUpdate();
		if (!(_Current != null))
		{
			foreach (GUIQuad quad in GUIQuadRegistry.Quads)
			{
				if (AnalyzeQuad(quad).Position == RelativePosition.Hover)
				{
					_Current = quad;
					EnterState(State.Hover);
					break;
				}
			}
			return;
		}
		AnalyzationResult analyzationResult = AnalyzeQuad(_Current);
		if (analyzationResult.TextureCoords != Vector2.zero)
		{
			Vector2 val = default(Vector2);
			val = new Vector2(analyzationResult.TextureCoords.x * (float)VRGUI.Width, (1f - analyzationResult.TextureCoords.y) * (float)VRGUI.Height);
			if (!mouseDownPosition.HasValue || Vector2.Distance(mouseDownPosition.Value, val) > 50f)
			{
				MouseOperations.SetClientCursorPosition((int)val.x, (int)val.y);
				mouseDownPosition = null;
			}
		}
		if (_CurrentState == State.Press)
		{
			if (analyzationResult.Position == RelativePosition.Out)
			{
				EnterState(State.None);
			}
			else if (analyzationResult.Position == RelativePosition.Hover)
			{
				EnterState(State.Hover);
			}
		}
		else if (_CurrentState == State.Hover)
		{
			if (analyzationResult.Position == RelativePosition.Behind)
			{
				EnterState(State.Press);
			}
			else if (analyzationResult.Position == RelativePosition.Out)
			{
				EnterState(State.None);
			}
		}
	}

	private void EnterState(State newState)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		State currentState = _CurrentState;
		if (currentState == State.Press)
		{
			VR.Input.Mouse.LeftButtonUp();
			mouseDownPosition = null;
		}
		_CurrentState = newState;
		switch (_CurrentState)
		{
		case State.Press:
			mouseDownPosition = ((Vector2)(Vector3.Scale(((Vector2)(new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y))), _ScaleVector)));
			VR.Input.Mouse.LeftButtonDown();
			break;
		case State.None:
			_Current = null;
			break;
		}
	}

	private AnalyzationResult AnalyzeQuad(GUIQuad quad)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		AnalyzationResult analyzationResult = new AnalyzationResult();
		Collider component = ((Component)quad).GetComponent<Collider>();
		if (component == null)
		{
			return analyzationResult;
		}
		Vector3 val = -((Component)quad).transform.forward;
		Vector3 position = ((Component)quad).transform.position;
		Vector3 tipPosition = TipPosition;
		bool flag = Vector3.Dot(tipPosition - position, val) < 0f;
		Vector3 val2 = -val;
		Vector3 val3 = ((!flag) ? tipPosition : (position + Vector3.Reflect(tipPosition - position, val)));
		RaycastHit val4 = default(RaycastHit);
		if (component.Raycast(new Ray(val3, val2), out val4, 1.5f))
		{
			float num = (flag ? 0.1f : 0.05f);
			if (val4.distance <= num)
			{
				analyzationResult.Position = ((!flag) ? RelativePosition.Hover : RelativePosition.Behind);
			}
			analyzationResult.TextureCoords = val4.textureCoord;
		}
		else
		{
			analyzationResult.Position = RelativePosition.Out;
		}
		return analyzationResult;
	}
}

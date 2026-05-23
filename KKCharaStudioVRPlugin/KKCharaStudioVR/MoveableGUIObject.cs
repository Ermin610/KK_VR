using System;
using System.Collections.Generic;
using Studio;
using UnityEngine;

namespace KKCharaStudioVR;

public class MoveableGUIObject : MonoBehaviour
{
	public List<Action<MonoBehaviour>> onMoveLister = new List<Action<MonoBehaviour>>();

	public List<Action<MonoBehaviour>> onReleasedLister = new List<Action<MonoBehaviour>>();

	public GuideObject guideObject;

	public GuideScale guideScale;

	public ObjectCtrlInfo objectCtrlInfo;

	public Vector3 oldPos;

	public Vector3 oldRot;

	public Vector3 oldScale;

	public bool isMoveObj;

	private Renderer renderer;

	public Renderer visibleReference;

	private void Start()
	{
		renderer = ((Component)this).GetComponent<Renderer>();
	}

	public void OnMoveStart()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)guideObject != (Object)null)
		{
			oldPos = guideObject.changeAmount.pos;
			oldRot = guideObject.changeAmount.rot;
			oldScale = guideObject.changeAmount.scale;
		}
	}

	public void OnMoved()
	{
		foreach (Action<MonoBehaviour> item in onMoveLister)
		{
			try
			{
				item((MonoBehaviour)(object)this);
			}
			catch
			{
			}
		}
	}

	public void OnReleased()
	{
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Expected O, but got Unknown
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Expected O, but got Unknown
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Expected O, but got Unknown
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Expected O, but got Unknown
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Expected O, but got Unknown
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Expected O, but got Unknown
		if ((Object)(object)guideObject != (Object)null)
		{
			if ((Object)(object)guideScale == (Object)null)
			{
				if (guideObject.enablePos)
				{
					EqualsInfo val = new EqualsInfo
					{
						dicKey = guideObject.dicKey,
						oldValue = oldPos,
						newValue = guideObject.changeAmount.pos
					};
					Singleton<UndoRedoManager>.Instance.Push((ICommand)new MoveEqualsCommand((EqualsInfo[])(object)new EqualsInfo[1] { val }));
				}
				if (guideObject.enableRot)
				{
					EqualsInfo val2 = new EqualsInfo
					{
						dicKey = guideObject.dicKey,
						oldValue = oldRot,
						newValue = guideObject.changeAmount.rot
					};
					Singleton<UndoRedoManager>.Instance.Push((ICommand)new RotationEqualsCommand((EqualsInfo[])(object)new EqualsInfo[1] { val2 }));
				}
			}
			else if (guideObject.enableScale)
			{
				EqualsInfo[] array = (EqualsInfo[])(object)new EqualsInfo[1]
				{
					new EqualsInfo
					{
						dicKey = guideObject.dicKey,
						oldValue = oldScale,
						newValue = guideObject.changeAmount.scale
					}
				};
				Singleton<UndoRedoManager>.Instance.Push((ICommand)new ScaleEqualsCommand(array));
			}
		}
		foreach (Action<MonoBehaviour> item in onReleasedLister)
		{
			try
			{
				item((MonoBehaviour)(object)this);
			}
			catch
			{
			}
		}
	}

	private void Update()
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		if (isMoveObj)
		{
			((Component)this).transform.localScale = Vector3.one * 0.1f * Studio.optionSystem.manipulateSize;
		}
		if ((Object)(object)guideScale != (Object)null)
		{
			((Component)this).transform.localScale = Vector3.one * 0.05f * Studio.optionSystem.manipulateSize;
		}
		if ((Object)(object)visibleReference != (Object)null && (Object)(object)renderer != (Object)null)
		{
			((Component)renderer).gameObject.layer = ((Component)visibleReference).gameObject.layer;
		}
	}
}

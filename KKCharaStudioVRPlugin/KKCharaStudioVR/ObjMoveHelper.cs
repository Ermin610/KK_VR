using System.Collections.Generic;
using Studio;
using UnityEngine;

namespace KKCharaStudioVR;

internal class ObjMoveHelper
{
	public Vector3 moveAlongBasePos;

	public Quaternion moveAlongBaseRot;

	public void SetBasePos(Vector3 basePos)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		moveAlongBasePos = basePos;
	}

	public ObjectCtrlInfo GetFirstObject()
	{
		Studio instance = Singleton<Studio>.Instance;
		if ((Object)(object)instance != (Object)null)
		{
			ObjectCtrlInfo[] selectObjectCtrl = instance.treeNodeCtrl.selectObjectCtrl;
			if (selectObjectCtrl != null && selectObjectCtrl.Length != 0)
			{
				return selectObjectCtrl[0];
			}
		}
		return null;
	}

	public void MoveAllCharaAndItemsHere(Vector3 newPos, bool keepY = true)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Expected O, but got Unknown
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Expected O, but got Unknown
		Studio instance = Singleton<Studio>.Instance;
		if ((Object)(object)instance == (Object)null)
		{
			return;
		}
		Vector3 val = newPos - moveAlongBasePos;
		if (keepY)
		{
			val.y = 0f;
		}
		new Dictionary<Transform, Transform>();
		List<EqualsInfo> list = new List<EqualsInfo>();
		ObjectCtrlInfo[] selectObjectCtrl = instance.treeNodeCtrl.selectObjectCtrl;
		for (int i = 0; i < selectObjectCtrl.Length; i++)
		{
			GuideObject guideObject = selectObjectCtrl[i].guideObject;
			if ((Object)(object)guideObject != (Object)null)
			{
				Vector3 localPosition = guideObject.transformTarget.localPosition;
				guideObject.transformTarget.position = guideObject.transformTarget.position + val;
				guideObject.changeAmount.pos = guideObject.transformTarget.localPosition;
				if (guideObject.enablePos)
				{
					EqualsInfo item = new EqualsInfo
					{
						dicKey = guideObject.dicKey,
						oldValue = localPosition,
						newValue = guideObject.changeAmount.pos
					};
					list.Add(item);
				}
			}
			Singleton<UndoRedoManager>.Instance.Push((ICommand)new MoveEqualsCommand(list.ToArray()));
		}
	}

	public void MoveObject(ObjectCtrlInfo oci, Vector3 newPos, bool keepY)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Expected O, but got Unknown
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Expected O, but got Unknown
		if (keepY)
		{
			newPos.y = oci.guideObject.transformTarget.position.y;
		}
		GuideObject guideObject = oci.guideObject;
		if ((Object)(object)guideObject != (Object)null)
		{
			Vector3 localPosition = guideObject.transformTarget.localPosition;
			guideObject.transformTarget.position = newPos;
			guideObject.changeAmount.pos = guideObject.transformTarget.localPosition;
			if (guideObject.enablePos)
			{
				EqualsInfo val = new EqualsInfo
				{
					dicKey = guideObject.dicKey,
					oldValue = localPosition,
					newValue = guideObject.changeAmount.pos
				};
				Singleton<UndoRedoManager>.Instance.Push((ICommand)new MoveEqualsCommand((EqualsInfo[])(object)new EqualsInfo[1] { val }));
			}
		}
	}
}

using System;
using System.Collections.Generic;
using Studio;
using VRGIN.Core;

namespace KKCharaStudioVR;

internal sealed class VRVmdActorTarget
{
    public int ObjectKey;
    public OCIChar Character;
    public string DisplayName;
}

internal static class VRVmdTargetService
{
    public static int[] GetSelectedObjectKeys()
    {
        List<int> keys = new List<int>();
        try
        {
            Studio.Studio studio = Singleton<Studio.Studio>.Instance;
            ObjectCtrlInfo[] selected = studio?.treeNodeCtrl?.selectObjectCtrl;
            if (selected == null)
                return keys.ToArray();

            foreach (ObjectCtrlInfo item in selected)
            {
                OCIChar character = item as OCIChar;
                if (character?.objectInfo != null)
                    keys.Add(character.objectInfo.dicKey);
            }
        }
        catch (Exception ex)
        {
            VRLog.Warn("Unable to inspect selected VMD targets: " + ex.Message);
        }
        return keys.ToArray();
    }

    public static List<VRVmdActorTarget> GetAllTargets(out string status)
    {
        List<VRVmdActorTarget> targets = new List<VRVmdActorTarget>();
        try
        {
            Studio.Studio studio = Singleton<Studio.Studio>.Instance;
            if (studio == null || studio.dicObjectCtrl == null)
            {
                status = "工作室角色列表尚未就绪";
                return targets;
            }

            foreach (KeyValuePair<int, ObjectCtrlInfo> pair in studio.dicObjectCtrl)
            {
                OCIChar character = pair.Value as OCIChar;
                if (character == null)
                    continue;

                targets.Add(new VRVmdActorTarget
                {
                    ObjectKey = pair.Key,
                    Character = character,
                    DisplayName = VRCharacterClothingService.GetCharacterName(character)
                });
            }

            targets.Sort((left, right) =>
            {
                int nameComparison = StringComparer.CurrentCultureIgnoreCase.Compare(
                    left.DisplayName,
                    right.DisplayName);
                return nameComparison != 0
                    ? nameComparison
                    : left.ObjectKey.CompareTo(right.ObjectKey);
            });

            for (int index = 0; index < targets.Count; index++)
            {
                VRVmdActorTarget target = targets[index];
                string sex = target.Character.sex == 0 ? "男" : "女";
                target.DisplayName = (index + 1).ToString("00")
                    + "  "
                    + target.DisplayName
                    + "  ["
                    + sex
                    + "]";
            }

            status = targets.Count == 0 ? "当前场景没有角色" : null;
            return targets;
        }
        catch (Exception ex)
        {
            status = "无法读取场景角色：" + ex.Message;
            VRLog.Error(status);
            targets.Clear();
            return targets;
        }
    }

    public static bool TrySelectTarget(int objectKey, out string status)
    {
        try
        {
            Studio.Studio studio = Singleton<Studio.Studio>.Instance;
            ObjectCtrlInfo item;
            if (studio == null
                || studio.treeNodeCtrl == null
                || studio.dicObjectCtrl == null
                || !studio.dicObjectCtrl.TryGetValue(objectKey, out item))
            {
                status = "所选角色已不在当前场景";
                return false;
            }

            OCIChar character = item as OCIChar;
            if (character == null || character.treeNodeObject == null)
            {
                status = "所选对象不是角色";
                return false;
            }

            studio.treeNodeCtrl.SelectSingle(null, true);
            studio.treeNodeCtrl.SelectSingle(character.treeNodeObject, false);
            status = "动作目标：" + VRCharacterClothingService.GetCharacterName(character);
            return true;
        }
        catch (Exception ex)
        {
            status = "无法选择动作目标：" + ex.Message;
            VRLog.Error(status);
            return false;
        }
    }
}

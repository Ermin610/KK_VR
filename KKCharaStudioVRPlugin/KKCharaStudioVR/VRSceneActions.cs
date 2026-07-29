using System;
using Studio;
using VRGIN.Core;

namespace KKCharaStudioVR;

internal static class VRSceneActions
{
    public static bool OpenLoadScene(out string status)
    {
        try
        {
            Studio.Studio studio = Singleton<Studio.Studio>.Instance;
            if (studio == null || studio.systemButtonCtrl == null)
            {
                status = "场景读取界面尚未就绪";
                return false;
            }

            studio.systemButtonCtrl.OnClickLoad();
            status = "已打开场景读取";
            return true;
        }
        catch (Exception ex)
        {
            status = "无法打开场景读取：" + ex.Message;
            VRLog.Error(status);
            return false;
        }
    }

    public static bool SaveScene(out string status)
    {
        try
        {
            Studio.Studio studio = Singleton<Studio.Studio>.Instance;
            if (studio == null || studio.systemButtonCtrl == null)
            {
                status = "场景保存功能尚未就绪";
                return false;
            }

            studio.systemButtonCtrl.OnClickSave();
            status = "场景已保存";
            return true;
        }
        catch (Exception ex)
        {
            status = "场景保存失败：" + ex.Message;
            VRLog.Error(status);
            return false;
        }
    }
}

using System;
using System.Collections;
using System.IO;
using Studio;
using VRGIN.Core;

namespace KKCharaStudioVR;

internal static class VRSceneActions
{
    public static string SceneRoot => UserData.Create("studio/scene");

    public static bool IsSceneCard(string path)
    {
        try
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;

            using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            long pngSize = PngFile.GetPngSize(stream);
            if (pngSize <= 0 || pngSize >= stream.Length)
                return false;

            stream.Position = pngSize;
            using BinaryReader reader = new BinaryReader(stream);
            Version version = new Version(reader.ReadString());
            int objectCount = reader.ReadInt32();
            if (version.Major < 0 || objectCount < 0 || objectCount > 1000000)
                return false;

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static IEnumerator LoadScene(string path, Action<bool, string> completed)
    {
        if (!IsSceneCard(path))
        {
            completed?.Invoke(false, "所选文件不是有效的场景卡");
            yield break;
        }

        Studio.Studio studio = Singleton<Studio.Studio>.Instance;
        if (studio == null)
        {
            completed?.Invoke(false, "工作室场景功能尚未就绪");
            yield break;
        }

        string fileName = Path.GetFileNameWithoutExtension(path);
        LoadFixHook.PrepareSceneLoad();
        IEnumerator loadRoutine = studio.LoadSceneCoroutine(path);
        while (true)
        {
            bool hasNext;
            object current;
            try
            {
                hasNext = loadRoutine.MoveNext();
                current = hasNext ? loadRoutine.Current : null;
            }
            catch (Exception ex)
            {
                string error = "场景读取失败：" + ex.Message;
                VRLog.Error(error);
                completed?.Invoke(false, error);
                yield break;
            }

            if (!hasNext)
                break;
            yield return current;
        }

        LoadFixHook.CompleteSceneLoad(studio);
        completed?.Invoke(true, "已读取场景：" + fileName);
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

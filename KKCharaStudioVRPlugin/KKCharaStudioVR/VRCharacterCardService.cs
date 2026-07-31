using System;
using System.IO;
using Studio;
using UnityEngine;
using VRGIN.Core;

namespace KKCharaStudioVR;

internal enum VRCharacterCardMode
{
    AddFemale,
    AddMale
}

internal static class VRCharacterCardService
{
    public static string FemaleRoot => UserData.Create("chara/female");
    public static string MaleRoot => UserData.Create("chara/male");

    public static bool TryGetBrowserRoot(VRCharacterCardMode mode, out string root, out string status)
    {
        root = null;
        status = null;
        if (mode == VRCharacterCardMode.AddFemale)
        {
            root = FemaleRoot;
            return true;
        }

        if (mode == VRCharacterCardMode.AddMale)
        {
            root = MaleRoot;
            return true;
        }

        status = "未知的角色卡分类";
        return false;
    }

    public static bool ExecuteAdd(VRCharacterCardMode mode, string path, out string status)
    {
        try
        {
            if (!ValidateCardPath(path, out status))
                return false;

            Studio.Studio studio = Singleton<Studio.Studio>.Instance;
            if (studio == null)
            {
                status = "工作室角色功能尚未就绪";
                return false;
            }

            if (mode == VRCharacterCardMode.AddFemale)
            {
                if (!VRWristFileCatalog.IsInsideRoot(FemaleRoot, path))
                {
                    status = "请选择女性角色卡";
                    return false;
                }
                if (!ValidateCardData(path, 1, out status))
                    return false;

                studio.AddFemale(path);
                status = "已添加女角色：" + Path.GetFileNameWithoutExtension(path);
                return true;
            }

            if (mode == VRCharacterCardMode.AddMale)
            {
                if (!VRWristFileCatalog.IsInsideRoot(MaleRoot, path))
                {
                    status = "请选择男性角色卡";
                    return false;
                }
                if (!ValidateCardData(path, 0, out status))
                    return false;

                studio.AddMale(path);
                status = "已添加男角色：" + Path.GetFileNameWithoutExtension(path);
                return true;
            }

            status = "未知的角色卡读取方式";
            return false;
        }
        catch (Exception ex)
        {
            status = "角色卡读取失败：" + ex.Message;
            VRLog.Error(status);
            return false;
        }
    }

    public static bool TryLoadNativePreview(
        VRCharacterCardMode mode,
        string path,
        out Texture2D texture,
        out string characterName,
        out string status)
    {
        texture = null;
        characterName = null;
        try
        {
            if (!ValidateCardPath(path, out status))
                return false;

            byte sex = mode == VRCharacterCardMode.AddMale ? (byte)0 : (byte)1;
            string expectedRoot = sex == 0 ? MaleRoot : FemaleRoot;
            if (!VRWristFileCatalog.IsInsideRoot(expectedRoot, path))
            {
                status = sex == 0 ? "请选择男性角色卡" : "请选择女性角色卡";
                return false;
            }

            ChaFileControl card = new ChaFileControl();
            if (!card.LoadCharaFile(path, sex, noLoadPng: true))
            {
                status = sex == 0
                    ? "文件不是有效的男性角色卡"
                    : "文件不是有效的女性角色卡";
                return false;
            }

            // This is the same thumbnail loader used by Studio.CharaList.
            texture = PngAssist.LoadTexture(path);
            if (texture == null)
            {
                status = "工作室原生卡面接口未能读取图片";
                return false;
            }

            characterName = card.parameter != null
                && !string.IsNullOrEmpty(card.parameter.fullname)
                    ? card.parameter.fullname
                    : Path.GetFileNameWithoutExtension(path);
            status = null;
            return true;
        }
        catch (Exception ex)
        {
            if (texture != null)
            {
                UnityEngine.Object.Destroy(texture);
                texture = null;
            }
            status = "工作室原生卡面读取失败：" + ex.Message;
            VRLog.Warn(status);
            return false;
        }
    }

    public static bool ExecuteReplace(int objectKey, string path, out string status)
    {
        try
        {
            if (!ValidateCardPath(path, out status))
                return false;

            OCIChar character;
            if (!TryGetCharacter(objectKey, out character, out status))
                return false;

            string expectedRoot = character.sex == 0 ? MaleRoot : FemaleRoot;
            if (!VRWristFileCatalog.IsInsideRoot(expectedRoot, path))
            {
                status = character.sex == 0 ? "男性角色只能替换为男卡" : "女性角色只能替换为女卡";
                return false;
            }
            if (!ValidateCardData(path, (byte)character.sex, out status))
                return false;

            string previousName = VRCharacterClothingService.GetCharacterName(character);
            character.ChangeChara(path);
            TransientHead transientHead = ((UnityEngine.Component)character.charInfo)
                .GetComponent<TransientHead>();
            if (transientHead != null)
                transientHead.RefreshAfterCharacterChange();
            status = previousName + " 已替换为：" + VRCharacterClothingService.GetCharacterName(character);
            return true;
        }
        catch (Exception ex)
        {
            status = "角色替换失败：" + ex.Message;
            VRLog.Error(status);
            return false;
        }
    }

    public static bool TryGetCharacter(int objectKey, out OCIChar character, out string status)
    {
        character = null;
        try
        {
            Studio.Studio studio = Singleton<Studio.Studio>.Instance;
            ObjectCtrlInfo item;
            if (studio == null
                || studio.dicObjectCtrl == null
                || !studio.dicObjectCtrl.TryGetValue(objectKey, out item))
            {
                status = "所选角色已不在当前场景";
                return false;
            }

            character = item as OCIChar;
            if (character == null)
            {
                status = "所选对象不是角色";
                return false;
            }

            status = VRCharacterClothingService.GetCharacterName(character);
            return true;
        }
        catch (Exception ex)
        {
            status = "无法读取场景角色：" + ex.Message;
            return false;
        }
    }

    private static bool ValidateCardPath(string path, out string status)
    {
        if (string.IsNullOrEmpty(path)
            || !File.Exists(path)
            || !string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase))
        {
            status = "角色卡文件无效";
            return false;
        }

        status = null;
        return true;
    }

    private static bool ValidateCardData(string path, byte sex, out string status)
    {
        ChaFileControl card = new ChaFileControl();
        if (!card.LoadCharaFile(path, sex, noLoadPng: true))
        {
            status = sex == 0
                ? "文件不是有效的男性角色卡"
                : "文件不是有效的女性角色卡";
            return false;
        }

        status = null;
        return true;
    }
}

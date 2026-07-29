using System;
using System.IO;
using Studio;
using VRGIN.Core;

namespace KKCharaStudioVR;

internal enum VRCharacterCardMode
{
    AddFemale,
    AddMale,
    ReplaceSelected
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

        OCIChar character;
        if (!VRCharacterClothingService.TryGetSelectedCharacter(out character, out status))
            return false;

        root = character.sex == 0 ? MaleRoot : FemaleRoot;
        status = "替换：" + VRCharacterClothingService.GetCharacterName(character);
        return true;
    }

    public static bool Execute(VRCharacterCardMode mode, string path, out string status)
    {
        try
        {
            if (string.IsNullOrEmpty(path)
                || !File.Exists(path)
                || !string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase))
            {
                status = "角色卡文件无效";
                return false;
            }

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

                studio.AddMale(path);
                status = "已添加男角色：" + Path.GetFileNameWithoutExtension(path);
                return true;
            }

            OCIChar character;
            if (!VRCharacterClothingService.TryGetSelectedCharacter(out character, out status))
                return false;

            string expectedRoot = character.sex == 0 ? MaleRoot : FemaleRoot;
            if (!VRWristFileCatalog.IsInsideRoot(expectedRoot, path))
            {
                status = character.sex == 0 ? "男性角色只能替换为男卡" : "女性角色只能替换为女卡";
                return false;
            }

            string previousName = VRCharacterClothingService.GetCharacterName(character);
            character.ChangeChara(path);
            status = previousName + " 已替换为：" + VRCharacterClothingService.GetCharacterName(character);
            return true;
        }
        catch (Exception ex)
        {
            status = "角色卡操作失败：" + ex.Message;
            VRLog.Error(status);
            return false;
        }
    }
}

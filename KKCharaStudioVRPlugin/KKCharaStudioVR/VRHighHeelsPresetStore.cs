using System;
using System.IO;
using System.Xml.Serialization;
using Studio;
using VRGIN.Core;

namespace KKCharaStudioVR;

[Serializable]
public sealed class VRHighHeelsPreset
{
    public int Version = 1;
    public string CharacterCardName;
    public bool AutoMode;
    public bool ShoesDetect;
    public float Ankle;
    public float Heel;
    public float Toes;
    public bool ShoesOffsetEnabled;
    public float ShoesOnOffset;
    public float ShoesOffOffset;
}

internal static class VRHighHeelsPresetStore
{
    private const string DefaultCardName = "未命名角色";

    public static bool HasPreset(OCIChar character)
    {
        string path;
        string cardName;
        string status;
        return TryGetPresetPath(character, out path, out cardName, out status)
            && File.Exists(path);
    }

    public static bool Save(
        OCIChar character,
        VRHighHeelsPreset preset,
        out string status)
    {
        string path;
        string cardName;
        if (!TryGetPresetPath(character, out path, out cardName, out status))
            return false;
        if (preset == null)
        {
            status = "没有可保存的高跟鞋参数";
            return false;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            bool replacing = File.Exists(path);
            preset.Version = 1;
            preset.CharacterCardName = cardName;

            XmlSerializer serializer = new XmlSerializer(typeof(VRHighHeelsPreset));
            using (FileStream stream = new FileStream(
                path,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None))
            {
                serializer.Serialize(stream, preset);
            }

            status = cardName + "：高跟鞋参数已" + (replacing ? "覆盖" : "保存");
            return true;
        }
        catch (Exception ex)
        {
            status = "高跟鞋参数保存失败：" + ex.Message;
            VRLog.Error(status);
            return false;
        }
    }

    public static bool TryLoad(
        OCIChar character,
        out VRHighHeelsPreset preset,
        out string status)
    {
        preset = null;
        string path;
        string cardName;
        if (!TryGetPresetPath(character, out path, out cardName, out status))
            return false;
        if (!File.Exists(path))
        {
            status = cardName + "：没有已保存的高跟鞋参数";
            return false;
        }

        try
        {
            XmlSerializer serializer = new XmlSerializer(typeof(VRHighHeelsPreset));
            using (FileStream stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read))
            {
                preset = serializer.Deserialize(stream) as VRHighHeelsPreset;
            }

            if (!IsValid(preset))
            {
                preset = null;
                status = cardName + "：保存的高跟鞋参数无效";
                return false;
            }

            status = cardName + "：已读取高跟鞋参数";
            return true;
        }
        catch (Exception ex)
        {
            status = "高跟鞋参数读取失败：" + ex.Message;
            VRLog.Error(status);
            return false;
        }
    }

    private static bool TryGetPresetPath(
        OCIChar character,
        out string path,
        out string cardName,
        out string status)
    {
        path = null;
        cardName = GetCharacterCardName(character);
        if (character == null)
        {
            status = "请选择要保存高跟鞋参数的角色";
            return false;
        }

        try
        {
            string safeName = SanitizeFileName(cardName);
            string directory = Path.Combine(
                Path.Combine(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData"),
                    "VR"),
                "HighHeels");
            path = Path.Combine(directory, safeName + ".xml");
            status = null;
            return true;
        }
        catch (Exception ex)
        {
            status = "无法生成高跟鞋参数文件名：" + ex.Message;
            return false;
        }
    }

    private static string GetCharacterCardName(OCIChar character)
    {
        try
        {
            string fileName = character?.charInfo?.chaFile?.charaFileName;
            if (!string.IsNullOrEmpty(fileName))
            {
                string cardName = Path.GetFileNameWithoutExtension(fileName);
                if (!string.IsNullOrEmpty(cardName))
                    return cardName.Trim();
            }

            string fullName = character?.charInfo?.fileParam?.fullname;
            if (!string.IsNullOrEmpty(fullName))
                return fullName.Trim();
        }
        catch (Exception)
        {
            // Fall back to the Studio tree label below.
        }

        string displayName = VRCharacterClothingService.GetCharacterName(character);
        return string.IsNullOrEmpty(displayName) ? DefaultCardName : displayName.Trim();
    }

    private static string SanitizeFileName(string value)
    {
        string result = string.IsNullOrEmpty(value) ? DefaultCardName : value.Trim();
        foreach (char invalid in Path.GetInvalidFileNameChars())
            result = result.Replace(invalid, '_');
        result = result.Trim().TrimEnd('.');
        if (string.IsNullOrEmpty(result))
            result = DefaultCardName;

        string upper = result.ToUpperInvariant();
        if (upper == "CON"
            || upper == "PRN"
            || upper == "AUX"
            || upper == "NUL"
            || upper.StartsWith("COM", StringComparison.Ordinal)
            || upper.StartsWith("LPT", StringComparison.Ordinal))
        {
            result += "_";
        }
        return result;
    }

    private static bool IsValid(VRHighHeelsPreset preset)
    {
        return preset != null
            && preset.Version == 1
            && IsFinite(preset.Ankle)
            && IsFinite(preset.Heel)
            && IsFinite(preset.Toes)
            && IsFinite(preset.ShoesOnOffset)
            && IsFinite(preset.ShoesOffOffset);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

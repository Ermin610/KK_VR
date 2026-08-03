using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using BepInEx;
using VRGIN.Core;

namespace KKCharaStudioVR;

[Serializable]
public sealed class VRMmdCue
{
    // Percent remains the cue end point for backward compatibility with the
    // version-1 state-only XML format.
    public float Percent;
    public float FadeStartPercent = -1f;
    public float TargetTransparency = -1f;
    public bool ApplyState = true;
    public int PartId;
    public byte State;
    public int Order;
}

[Serializable]
[XmlRoot("MmdCueSheet")]
public sealed class VRMmdCueSheet
{
    public int Version = 2;
    public string PresetId;
    public string PresetName;
    public string VmdFingerprint;
    public string SourceFileName;

    [XmlArray("Cues")]
    [XmlArrayItem("Cue")]
    public List<VRMmdCue> Cues = new List<VRMmdCue>();
}

internal static class VRMmdCueSheetStore
{
    public const string StandardPresetId = "standard";
    public const string GentlePresetId = "gentle";
    public const string LayeredPresetId = "layered";
    public const string BeatPresetId = "beat";
    public const string TransparencyPresetId = "transparency";
    public const string DefaultPresetId = StandardPresetId;

    private static readonly string[] GlobalPresetIds =
    {
        StandardPresetId,
        GentlePresetId,
        LayeredPresetId,
        BeatPresetId,
        TransparencyPresetId
    };

    public static VRMmdCueSheet CreateGlobalPreset()
    {
        return CreateGlobalPreset(DefaultPresetId);
    }

    public static VRMmdCueSheet CreateGlobalPreset(string presetId)
    {
        string normalizedPresetId = NormalizePresetId(presetId);
        VRMmdCueSheet sheet = new VRMmdCueSheet
        {
            PresetId = normalizedPresetId,
            PresetName = GetGlobalPresetName(normalizedPresetId)
        };
        switch (normalizedPresetId)
        {
            case GentlePresetId:
                // Long-lived values stay inside the visually stable AZ range.
                AddFadeCue(sheet, 8f, 18f, 0, 25f, true, 1);
                AddFadeCue(sheet, 22f, 32f, 1, 22f, true, 1);
                AddFadeCue(sheet, 38f, 50f, 2, 18f, true, 1);
                AddFadeCue(sheet, 48f, 60f, 3, 20f, true, 1);
                AddFadeCue(sheet, 60f, 75f, 5, 15f, false, 0);
                break;

            case LayeredPresetId:
                AddFadeCue(sheet, 5f, 18f, 0, 40f, true, 1);
                AddFadeCue(sheet, 20f, 34f, 1, 38f, true, 1);
                AddFadeCue(sheet, 36f, 48f, 4, 65f, true, 3);
                AddFadeCue(sheet, 44f, 58f, 2, 28f, true, 1);
                AddFadeCue(sheet, 56f, 70f, 3, 32f, true, 1);
                AddFadeCue(sheet, 72f, 86f, 0, 70f, true, 3);
                AddFadeCue(sheet, 80f, 92f, 1, 65f, true, 3);
                break;

            case BeatPresetId:
                AddFadeCue(sheet, 9f, 12f, 0, 50f, true, 1);
                AddFadeCue(sheet, 19f, 22f, 1, 48f, true, 1);
                AddFadeCue(sheet, 29f, 32f, 2, 35f, true, 1);
                AddFadeCue(sheet, 29f, 32f, 3, 38f, true, 1);
                AddFadeCue(sheet, 39f, 42f, 0, 70f, true, 3);
                AddFadeCue(sheet, 39f, 42f, 1, 65f, true, 3);
                AddFadeCue(sheet, 49f, 52f, 4, 65f, true, 3);
                AddFadeCue(sheet, 55f, 60f, 5, 40f, false, 0);
                break;

            case TransparencyPresetId:
                AddFadeCue(sheet, 10f, 22f, 0, 45f, false, 0);
                AddFadeCue(sheet, 18f, 30f, 1, 42f, false, 0);
                AddFadeCue(sheet, 28f, 40f, 2, 30f, false, 0);
                AddFadeCue(sheet, 35f, 48f, 3, 35f, false, 0);
                AddFadeCue(sheet, 45f, 60f, 5, 40f, false, 0);
                AddFadeCue(sheet, 52f, 66f, 6, 30f, false, 0);
                AddFadeCue(sheet, 60f, 72f, 4, 45f, false, 0);
                break;

            default:
                AddFadeCue(sheet, 6f, 10f, 0, 42f, true, 1);
                AddFadeCue(sheet, 16f, 20f, 1, 40f, true, 1);
                AddFadeCue(sheet, 26f, 30f, 2, 28f, true, 1);
                AddFadeCue(sheet, 26f, 30f, 3, 32f, true, 1);
                AddFadeCue(sheet, 34f, 40f, 0, 70f, true, 3);
                AddFadeCue(sheet, 34f, 40f, 1, 65f, true, 3);
                AddFadeCue(sheet, 40f, 44f, 5, 25f, false, 0);
                break;
        }
        Normalize(sheet);
        return sheet;
    }

    public static string[] GetGlobalPresetIds()
    {
        return (string[])GlobalPresetIds.Clone();
    }

    public static string NormalizePresetId(string presetId)
    {
        presetId = string.IsNullOrEmpty(presetId) ? null : presetId.Trim();
        foreach (string candidate in GlobalPresetIds)
        {
            if (string.Equals(candidate, presetId, StringComparison.OrdinalIgnoreCase))
                return candidate;
        }
        return DefaultPresetId;
    }

    public static string GetGlobalPresetName(string presetId)
    {
        switch (NormalizePresetId(presetId))
        {
            case GentlePresetId:
                return "轻柔展示";
            case LayeredPresetId:
                return "慢速层次";
            case BeatPresetId:
                return "强节拍切换";
            case TransparencyPresetId:
                return "纯透明渐变";
            default:
                return "标准渐脱";
        }
    }

    public static bool TryGetEffective(
        string vmdPath,
        string presetId,
        out VRMmdCueSheet sheet,
        out bool custom,
        out string status)
    {
        sheet = null;
        custom = false;
        string fingerprint;
        if (!TryGetFingerprint(vmdPath, out fingerprint, out status))
            return false;

        string path = GetSheetPath(fingerprint);
        if (!File.Exists(path))
        {
            sheet = CreateGlobalPreset(presetId);
            sheet.VmdFingerprint = fingerprint;
            sheet.SourceFileName = Path.GetFileName(vmdPath);
            status = "使用全局预设";
            return true;
        }

        try
        {
            XmlSerializer serializer = new XmlSerializer(typeof(VRMmdCueSheet));
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                sheet = serializer.Deserialize(stream) as VRMmdCueSheet;

            if (!Validate(sheet, fingerprint))
            {
                sheet = null;
                status = "当前 VMD 的服装联动配置无效";
                return false;
            }

            Normalize(sheet);
            custom = true;
            status = "当前 VMD 自定义";
            return true;
        }
        catch (Exception ex)
        {
            status = "读取 VMD 服装联动失败：" + ex.Message;
            VRLog.Error(status);
            return false;
        }
    }

    public static bool TryGetEffective(
        string vmdPath,
        out VRMmdCueSheet sheet,
        out bool custom,
        out string status)
    {
        return TryGetEffective(
            vmdPath,
            DefaultPresetId,
            out sheet,
            out custom,
            out status);
    }

    public static bool HasCustom(string vmdPath)
    {
        string fingerprint;
        string status;
        return TryGetFingerprint(vmdPath, out fingerprint, out status)
            && File.Exists(GetSheetPath(fingerprint));
    }

    public static bool CreateCustomFromGlobal(
        string vmdPath,
        string presetId,
        out VRMmdCueSheet sheet,
        out string status)
    {
        sheet = CreateGlobalPreset(presetId);
        string fingerprint;
        if (!TryGetFingerprint(vmdPath, out fingerprint, out status))
            return false;
        sheet.VmdFingerprint = fingerprint;
        sheet.SourceFileName = Path.GetFileName(vmdPath);
        return SaveCustom(vmdPath, sheet, out status);
    }

    public static bool CreateCustomFromGlobal(
        string vmdPath,
        out VRMmdCueSheet sheet,
        out string status)
    {
        return CreateCustomFromGlobal(
            vmdPath,
            DefaultPresetId,
            out sheet,
            out status);
    }

    public static bool SaveCustom(string vmdPath, VRMmdCueSheet sheet, out string status)
    {
        string fingerprint;
        if (!TryGetFingerprint(vmdPath, out fingerprint, out status))
            return false;
        if (sheet == null)
        {
            status = "没有可保存的 VMD 服装联动配置";
            return false;
        }

        try
        {
            sheet.Version = 2;
            sheet.VmdFingerprint = fingerprint;
            sheet.SourceFileName = Path.GetFileName(vmdPath);
            Normalize(sheet);
            string path = GetSheetPath(fingerprint);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            XmlSerializer serializer = new XmlSerializer(typeof(VRMmdCueSheet));
            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                serializer.Serialize(stream, sheet);
            status = "当前 VMD 自定义已保存";
            return true;
        }
        catch (Exception ex)
        {
            status = "保存 VMD 服装联动失败：" + ex.Message;
            VRLog.Error(status);
            return false;
        }
    }

    public static bool DeleteCustom(string vmdPath, out string status)
    {
        string fingerprint;
        if (!TryGetFingerprint(vmdPath, out fingerprint, out status))
            return false;
        try
        {
            string path = GetSheetPath(fingerprint);
            if (File.Exists(path))
                File.Delete(path);
            status = "已恢复全局预设";
            return true;
        }
        catch (Exception ex)
        {
            status = "恢复全局预设失败：" + ex.Message;
            VRLog.Error(status);
            return false;
        }
    }

    public static VRMmdCueSheet Clone(VRMmdCueSheet source)
    {
        VRMmdCueSheet clone = new VRMmdCueSheet();
        if (source == null)
            return clone;
        clone.Version = source.Version;
        clone.PresetId = source.PresetId;
        clone.PresetName = source.PresetName;
        clone.VmdFingerprint = source.VmdFingerprint;
        clone.SourceFileName = source.SourceFileName;
        if (source.Cues != null)
        {
            foreach (VRMmdCue cue in source.Cues)
            {
                if (cue == null)
                    continue;
                clone.Cues.Add(new VRMmdCue
                {
                    Percent = cue.Percent,
                    FadeStartPercent = cue.FadeStartPercent,
                    TargetTransparency = cue.TargetTransparency,
                    ApplyState = cue.ApplyState,
                    PartId = cue.PartId,
                    State = cue.State,
                    Order = cue.Order
                });
            }
        }
        return clone;
    }

    private static VRMmdCue NewCue(float percent, int partId, byte state, int order)
    {
        return new VRMmdCue { Percent = percent, PartId = partId, State = state, Order = order };
    }

    private static void AddFadeCue(
        VRMmdCueSheet sheet,
        float startPercent,
        float endPercent,
        int partId,
        float targetTransparency,
        bool applyState,
        byte state)
    {
        sheet.Cues.Add(new VRMmdCue
        {
            FadeStartPercent = startPercent,
            Percent = endPercent,
            TargetTransparency = targetTransparency,
            ApplyState = applyState,
            PartId = partId,
            State = state,
            Order = sheet.Cues.Count
        });
    }

    private static void Normalize(VRMmdCueSheet sheet)
    {
        sheet.PresetId = NormalizePresetId(sheet.PresetId);
        sheet.PresetName = GetGlobalPresetName(sheet.PresetId);
        if (sheet.Cues == null)
            sheet.Cues = new List<VRMmdCue>();
        for (int index = sheet.Cues.Count - 1; index >= 0; index--)
        {
            VRMmdCue cue = sheet.Cues[index];
            if (cue == null)
            {
                sheet.Cues.RemoveAt(index);
                continue;
            }
            cue.Percent = NormalizeFinitePercent(cue.Percent, 0f);
            bool hasTransparency = IsFinite(cue.TargetTransparency)
                && cue.TargetTransparency >= 0f;
            if (hasTransparency)
            {
                cue.TargetTransparency = NormalizeFinitePercent(
                    cue.TargetTransparency,
                    0f);
                cue.FadeStartPercent = IsFinite(cue.FadeStartPercent)
                    && cue.FadeStartPercent >= 0f
                        ? Math.Max(0f, Math.Min(cue.Percent, cue.FadeStartPercent))
                        : cue.Percent;
            }
            else
            {
                cue.FadeStartPercent = -1f;
                cue.TargetTransparency = -1f;
            }
            cue.PartId = Math.Max(0, Math.Min(VRCharacterClothingService.PartCount - 1, cue.PartId));
            cue.State = cue.State > 3 ? (byte)3 : cue.State;
            if (!cue.ApplyState && !hasTransparency)
            {
                sheet.Cues.RemoveAt(index);
                continue;
            }
            if (!cue.ApplyState)
                cue.State = 0;
        }
        sheet.Cues.Sort(CompareCues);
        for (int index = 0; index < sheet.Cues.Count; index++)
            sheet.Cues[index].Order = index;
        sheet.Version = 2;
    }

    private static float NormalizeFinitePercent(float value, float fallback)
    {
        return IsFinite(value)
            ? Math.Max(0f, Math.Min(100f, value))
            : fallback;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static int CompareCues(VRMmdCue left, VRMmdCue right)
    {
        int percent = left.Percent.CompareTo(right.Percent);
        return percent != 0 ? percent : left.Order.CompareTo(right.Order);
    }

    private static bool Validate(VRMmdCueSheet sheet, string fingerprint)
    {
        return sheet != null
            && (sheet.Version == 1 || sheet.Version == 2)
            && string.Equals(sheet.VmdFingerprint, fingerprint, StringComparison.OrdinalIgnoreCase)
            && sheet.Cues != null;
    }

    private static bool TryGetFingerprint(string vmdPath, out string fingerprint, out string status)
    {
        fingerprint = null;
        if (string.IsNullOrEmpty(vmdPath) || !File.Exists(vmdPath))
        {
            status = "当前没有可识别的动作 VMD";
            return false;
        }

        try
        {
            using (FileStream stream = new FileStream(vmdPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (SHA256 hash = SHA256.Create())
            {
                byte[] bytes = hash.ComputeHash(stream);
                StringBuilder builder = new StringBuilder(bytes.Length * 2);
                foreach (byte value in bytes)
                    builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                fingerprint = builder.ToString();
            }
            status = null;
            return true;
        }
        catch (Exception ex)
        {
            status = "无法识别当前 VMD：" + ex.Message;
            return false;
        }
    }

    private static string GetSheetPath(string fingerprint)
    {
        string gameRoot = Paths.GameRootPath;
        if (string.IsNullOrEmpty(gameRoot) || !Directory.Exists(gameRoot))
            gameRoot = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(
            Path.Combine(Path.Combine(Path.Combine(gameRoot, "UserData"), "VR"), "MmdCueSheets"),
            fingerprint + ".xml");
    }
}

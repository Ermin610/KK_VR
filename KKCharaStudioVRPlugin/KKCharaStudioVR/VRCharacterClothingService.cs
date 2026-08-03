using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Studio;
using VRGIN.Core;

namespace KKCharaStudioVR;

internal sealed class VRClothingStateSnapshot
{
    public int ObjectKey;
    public byte[] States;
}

internal static class VRCharacterClothingService
{
    private static readonly string[] PartNames =
    {
        "上衣",
        "下装",
        "胸罩",
        "内裤",
        "手套",
        "裤袜",
        "袜子",
        "鞋子"
    };

    private static readonly string[] CoordinateNames =
    {
        "校服 1",
        "校服 2",
        "运动服",
        "泳装",
        "社团服",
        "便服",
        "睡衣"
    };

    private static bool _moreOutfitsSearched;
    private static MethodInfo _getMoreOutfitName;

    public static int PartCount => PartNames.Length;

    public static string GetPartName(int partId)
    {
        return partId >= 0 && partId < PartNames.Length ? PartNames[partId] : "服装";
    }

    public static string GetCharacterName(OCIChar character)
    {
        if (character?.treeNodeObject != null && !string.IsNullOrEmpty(character.treeNodeObject.textName))
            return character.treeNodeObject.textName;
        return "当前角色";
    }

    public static string GetCoordinateName(int coordinateIndex)
    {
        return coordinateIndex >= 0 && coordinateIndex < CoordinateNames.Length
            ? CoordinateNames[coordinateIndex]
            : "预设服装";
    }

    public static string GetCoordinateName(OCIChar character, int coordinateIndex)
    {
        if (coordinateIndex >= 0 && coordinateIndex < CoordinateNames.Length)
            return CoordinateNames[coordinateIndex];

        string customName = TryGetMoreOutfitName(character?.charInfo, coordinateIndex);
        string moreOutfitsDefault = "Outfit " + (coordinateIndex + 1);
        return !string.IsNullOrEmpty(customName)
            && !string.Equals(customName, moreOutfitsDefault, StringComparison.OrdinalIgnoreCase)
                ? customName
                : "服装 " + (coordinateIndex + 1);
    }

    public static int GetCoordinateCount(OCIChar character)
    {
        try
        {
            return character?.charInfo?.chaFile?.coordinate?.Length ?? 0;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    public static int GetCurrentCoordinateIndex(OCIChar character)
    {
        try
        {
            if (character?.charInfo?.fileStatus == null)
                return -1;
            int coordinateIndex = character.charInfo.fileStatus.coordinateType;
            return coordinateIndex >= 0 && coordinateIndex < GetCoordinateCount(character)
                ? coordinateIndex
                : -1;
        }
        catch (Exception)
        {
            return -1;
        }
    }

    public static bool TrySetCoordinate(int objectKey, int coordinateIndex, out string status)
    {
        if (coordinateIndex < 0)
        {
            status = "不支持的预设服装";
            return false;
        }

        OCIChar character;
        if (!TryGetCharacter(objectKey, out character, out status))
            return false;
        if (coordinateIndex >= GetCoordinateCount(character))
        {
            status = "不支持的预设服装";
            return false;
        }

        try
        {
            if (!character.charInfo.loadEnd)
            {
                status = "预设服装仍在读取，请稍后";
                return false;
            }
            if (character.charInfo?.chaFile?.coordinate == null
                || coordinateIndex >= character.charInfo.chaFile.coordinate.Length
                || character.charInfo.chaFile.coordinate[coordinateIndex] == null)
            {
                status = GetCharacterName(character) + "：角色没有这个预设服装";
                return false;
            }

            string coordinateName = GetCoordinateName(character, coordinateIndex);
            if (GetCurrentCoordinateIndex(character) == coordinateIndex)
            {
                status = GetCharacterName(character) + "：已是预设服装 " + coordinateName;
                return true;
            }

            character.SetCoordinateInfo((ChaFileDefine.CoordinateType)coordinateIndex);
            if (GetCurrentCoordinateIndex(character) != coordinateIndex)
            {
                status = "预设服装切换失败：工作室没有接受切换请求";
                return false;
            }
            status = GetCharacterName(character) + "：已切换到预设服装 " + coordinateName;
            return true;
        }
        catch (Exception ex)
        {
            status = "预设服装切换失败：" + ex.Message;
            VRLog.Error(status);
            return false;
        }
    }

    public static void RefreshStudioCharacterPanel(OCIChar character)
    {
        if (character == null)
            return;

        try
        {
            MPCharCtrl panel = UnityEngine.Object.FindObjectOfType<MPCharCtrl>();
            if (panel != null && object.ReferenceEquals(panel.ociChar, character))
                panel.ociChar = character;
        }
        catch (Exception ex)
        {
            VRLog.Warn("Unable to refresh Studio clothing panel: " + ex.Message);
        }
    }

    private static string TryGetMoreOutfitName(ChaControl chaControl, int coordinateIndex)
    {
        if (chaControl == null || coordinateIndex < CoordinateNames.Length)
            return null;

        try
        {
            if (!_moreOutfitsSearched)
            {
                _moreOutfitsSearched = true;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type pluginType = assembly.GetType("KK_Plugins.MoreOutfits.Plugin", false);
                    if (pluginType == null)
                        continue;

                    _getMoreOutfitName = pluginType.GetMethod(
                        "GetCoodinateName",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        new[] { typeof(ChaControl), typeof(int) },
                        null);
                    break;
                }
            }

            return _getMoreOutfitName?.Invoke(
                null,
                new object[] { chaControl, coordinateIndex }) as string;
        }
        catch (Exception)
        {
            // More Outfit Slots can be absent, still initializing, or a different
            // version. The caller will use a stable numbered fallback label.
            return null;
        }
    }

    public static bool TryGetCharacter(int objectKey, out OCIChar character, out string status)
    {
        character = null;
        VRVmdActorTarget target;
        if (!VRVmdTargetService.TryGetTarget(objectKey, out target, out status))
            return false;

        character = target.Character;
        status = target.DisplayName;
        return true;
    }

    public static bool TrySetAll(int objectKey, byte state, out string status)
    {
        if (state != 0 && state != 1 && state != 3)
        {
            status = "不支持的整套换装状态";
            return false;
        }

        OCIChar character;
        if (!TryGetCharacter(objectKey, out character, out status))
            return false;

        try
        {
            character.SetClothesStateAll(state);
            status = GetCharacterName(character) + "：全部服装 " + GetStateName(state);
            return true;
        }
        catch (Exception ex)
        {
            status = "整套换装失败：" + ex.Message;
            VRLog.Error(status);
            return false;
        }
    }

    public static bool TryCyclePart(int objectKey, int partId, out string status)
    {
        OCIChar character;
        if (!TryGetCharacter(objectKey, out character, out status))
            return false;
        if (!IsValidPart(character, partId))
        {
            status = GetCharacterName(character) + "：" + GetPartName(partId) + "不可用";
            return false;
        }

        try
        {
            byte currentState = character.charFileStatus.clothesState[partId];
            Dictionary<byte, string> supportedStates = character.charInfo.GetClothesStateKind(partId);
            if (supportedStates == null || supportedStates.Count == 0)
            {
                status = GetCharacterName(character) + "：" + GetPartName(partId) + "没有可切换状态";
                return false;
            }

            for (int offset = 1; offset < 4; offset++)
            {
                byte nextState = (byte)((currentState + offset) % 4);
                if (!supportedStates.ContainsKey(nextState))
                    continue;

                character.SetClothesState(partId, nextState);
                byte actualState = character.charFileStatus.clothesState[partId];
                status = GetCharacterName(character) + "：" + GetPartName(partId) + " " + GetStateName(actualState);
                return true;
            }

            status = GetCharacterName(character) + "：" + GetPartName(partId) + "没有其他状态";
            return false;
        }
        catch (Exception ex)
        {
            status = GetPartName(partId) + "切换失败：" + ex.Message;
            VRLog.Error(status);
            return false;
        }
    }

    public static bool TrySetPartState(int objectKey, int partId, byte state, out string status)
    {
        OCIChar character;
        if (!TryGetCharacter(objectKey, out character, out status))
            return false;
        if (!IsValidPart(character, partId))
        {
            status = GetCharacterName(character) + "：" + GetPartName(partId) + "不可用";
            return false;
        }

        try
        {
            Dictionary<byte, string> supportedStates = character.charInfo.GetClothesStateKind(partId);
            if (supportedStates == null || !supportedStates.ContainsKey(state))
            {
                status = GetCharacterName(character) + "：" + GetPartName(partId) + "不支持" + GetStateName(state);
                return false;
            }

            if (character.charFileStatus.clothesState[partId] != state)
                character.SetClothesState(partId, state);
            byte actualState = character.charFileStatus.clothesState[partId];
            if (actualState != state)
            {
                status = GetCharacterName(character) + "：" + GetPartName(partId) + "状态未生效";
                return false;
            }

            status = GetCharacterName(character) + "：" + GetPartName(partId) + " " + GetStateName(actualState);
            return true;
        }
        catch (Exception ex)
        {
            status = GetPartName(partId) + "设置失败：" + ex.Message;
            VRLog.Error(status);
            return false;
        }
    }

    public static bool TryCaptureStates(int objectKey, out VRClothingStateSnapshot snapshot, out string status)
    {
        snapshot = null;
        OCIChar character;
        if (!TryGetCharacter(objectKey, out character, out status))
            return false;

        try
        {
            if (character.charFileStatus?.clothesState == null)
            {
                status = GetCharacterName(character) + "：服装状态尚未就绪";
                return false;
            }

            int count = Math.Min(PartNames.Length, character.charFileStatus.clothesState.Length);
            byte[] states = new byte[count];
            Array.Copy(character.charFileStatus.clothesState, states, count);
            snapshot = new VRClothingStateSnapshot { ObjectKey = objectKey, States = states };
            status = null;
            return true;
        }
        catch (Exception ex)
        {
            status = "无法保存播放前服装状态：" + ex.Message;
            VRLog.Error(status);
            return false;
        }
    }

    public static bool TryRestoreStates(VRClothingStateSnapshot snapshot, out string status)
    {
        status = null;
        if (snapshot?.States == null)
        {
            status = "没有可恢复的服装状态";
            return false;
        }

        bool restoredAny = false;
        StringBuilder errors = new StringBuilder();
        for (int partId = 0; partId < snapshot.States.Length; partId++)
        {
            string partStatus;
            if (TrySetPartState(snapshot.ObjectKey, partId, snapshot.States[partId], out partStatus))
            {
                restoredAny = true;
                continue;
            }

            if (errors.Length > 0)
                errors.Append("；");
            errors.Append(partStatus);
        }

        status = errors.Length == 0 ? "已恢复播放前服装" : errors.ToString();
        return restoredAny && errors.Length == 0;
    }

    public static string GetPartStateLabel(OCIChar character, int partId)
    {
        try
        {
            if (!IsValidPart(character, partId))
                return "不可用";

            Dictionary<byte, string> supportedStates = character.charInfo.GetClothesStateKind(partId);
            if (supportedStates == null || supportedStates.Count == 0)
                return "无服装";

            return GetStateName(character.charFileStatus.clothesState[partId]);
        }
        catch (Exception)
        {
            return "不可用";
        }
    }

    private static bool IsValidPart(OCIChar character, int partId)
    {
        return character != null
            && character.charInfo != null
            && character.charFileStatus != null
            && character.charFileStatus.clothesState != null
            && partId >= 0
            && partId < PartNames.Length
            && partId < character.charFileStatus.clothesState.Length;
    }

    public static string GetStateName(byte state)
    {
        switch (state)
        {
            case 0:
                return "穿好";
            case 1:
                return "半脱";
            case 2:
                return "半脱 2";
            case 3:
                return "脱下";
            default:
                return "状态 " + state;
        }
    }

    public static bool HasPart(OCIChar character, int partId)
    {
        try
        {
            if (!IsValidPart(character, partId))
                return false;
            Dictionary<byte, string> supportedStates = character.charInfo.GetClothesStateKind(partId);
            return supportedStates != null && supportedStates.Count > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

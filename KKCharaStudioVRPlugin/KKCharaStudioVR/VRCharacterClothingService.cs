using System;
using System.Collections.Generic;
using Studio;
using VRGIN.Core;

namespace KKCharaStudioVR;

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

    private static string GetStateName(byte state)
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
}

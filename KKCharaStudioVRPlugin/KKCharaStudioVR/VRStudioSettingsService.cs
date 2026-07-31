using Config;
using Studio;
using UnityEngine;
using VRGIN.Core;

namespace KKCharaStudioVR;

internal enum VRAudioChannel
{
    Master,
    Bgm,
    Environment,
    SystemEffects,
    GameEffects
}

internal static class VRStudioSettingsService
{
    private static readonly Color[] BackgroundPresets =
    {
        new Color(0.035f, 0.045f, 0.055f, 1f),
        new Color(0.16f, 0.19f, 0.22f, 1f),
        new Color(0.32f, 0.38f, 0.42f, 1f),
        new Color(0.08f, 0.17f, 0.28f, 1f),
        new Color(0.82f, 0.84f, 0.84f, 1f)
    };

    private static readonly string[] BackgroundNames =
    {
        "墨黑", "深灰", "中灰", "深蓝", "浅灰"
    };

    private static readonly Color[] CharacterLightPresets =
    {
        new Color(1f, 0.82f, 0.68f, 1f),
        new Color(1f, 0.96f, 0.88f, 1f),
        new Color(0.78f, 0.9f, 1f, 1f),
        new Color(1f, 0.66f, 0.58f, 1f),
        new Color(0.72f, 1f, 0.82f, 1f)
    };

    private static readonly string[] CharacterLightNames =
    {
        "暖", "白", "冷", "红", "绿"
    };

    private static float _lastCharacterLightIntensity = 1f;

    public static int BackgroundPresetCount => BackgroundPresets.Length;
    public static int CharacterLightPresetCount => CharacterLightPresets.Length;

    public static Color GetBackgroundPresetColor(int index)
    {
        return BackgroundPresets[Mathf.Clamp(index, 0, BackgroundPresets.Length - 1)];
    }

    public static string GetBackgroundPresetName(int index)
    {
        return BackgroundNames[Mathf.Clamp(index, 0, BackgroundNames.Length - 1)];
    }

    public static Color GetCharacterLightPresetColor(int index)
    {
        return CharacterLightPresets[Mathf.Clamp(index, 0, CharacterLightPresets.Length - 1)];
    }

    public static string GetCharacterLightPresetName(int index)
    {
        return CharacterLightNames[Mathf.Clamp(index, 0, CharacterLightNames.Length - 1)];
    }

    public static bool SetBackgroundPreset(int index, out string status)
    {
        Color color = GetBackgroundPresetColor(index);
        bool applied = false;
        Studio.Studio studio = Singleton<Studio.Studio>.Instance;
        if (studio != null && studio.cameraCtrl != null && studio.cameraCtrl.mainCmaera != null)
        {
            studio.cameraCtrl.mainCmaera.backgroundColor = color;
            applied = true;
        }

        if (VR.Camera != null && VR.Camera.SteamCam != null && VR.Camera.SteamCam.camera != null)
        {
            VR.Camera.SteamCam.camera.backgroundColor = color;
            applied = true;
        }

        status = applied
            ? "背景色：" + GetBackgroundPresetName(index)
            : "当前没有可调节的场景相机";
        return applied;
    }

    public static int FindClosestBackgroundPreset()
    {
        Color current = BackgroundPresets[0];
        Studio.Studio studio = Singleton<Studio.Studio>.Instance;
        if (studio != null && studio.cameraCtrl != null && studio.cameraCtrl.mainCmaera != null)
            current = studio.cameraCtrl.mainCmaera.backgroundColor;

        int best = 0;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < BackgroundPresets.Length; i++)
        {
            Color delta = current - BackgroundPresets[i];
            float distance = delta.r * delta.r + delta.g * delta.g + delta.b * delta.b;
            if (distance < bestDistance)
            {
                best = i;
                bestDistance = distance;
            }
        }

        return best;
    }

    public static bool TryGetCharacterLight(out float intensity, out bool shadow, out Color color)
    {
        intensity = 0f;
        shadow = false;
        color = Color.white;
        CameraLightCtrl.LightInfo info;
        if (!TryGetCharacterLightInfo(out info))
            return false;

        intensity = info.intensity;
        shadow = info.shadow;
        color = info.color;
        return true;
    }

    public static bool ToggleCharacterLight(out string status)
    {
        CameraLightCtrl.LightInfo info;
        if (!TryGetCharacterLightInfo(out info))
        {
            status = "角色光尚未初始化";
            return false;
        }

        if (info.intensity > 0.01f)
        {
            _lastCharacterLightIntensity = info.intensity;
            info.intensity = 0f;
            status = "角色光已关闭";
        }
        else
        {
            info.intensity = Mathf.Max(0.1f, _lastCharacterLightIntensity);
            status = "角色光已开启";
        }

        ReflectCharacterLight();
        return true;
    }

    public static bool AdjustCharacterLight(float delta, out string status)
    {
        CameraLightCtrl.LightInfo info;
        if (!TryGetCharacterLightInfo(out info))
        {
            status = "角色光尚未初始化";
            return false;
        }

        info.intensity = Mathf.Clamp(info.intensity + delta, 0f, 2f);
        if (info.intensity > 0.01f)
            _lastCharacterLightIntensity = info.intensity;
        ReflectCharacterLight();
        status = "角色光强度 " + info.intensity.ToString("F1");
        return true;
    }

    public static bool SetCharacterLightColor(int index, out string status)
    {
        CameraLightCtrl.LightInfo info;
        if (!TryGetCharacterLightInfo(out info))
        {
            status = "角色光尚未初始化";
            return false;
        }

        info.color = GetCharacterLightPresetColor(index);
        ReflectCharacterLight();
        status = "角色光颜色：" + GetCharacterLightPresetName(index);
        return true;
    }

    public static bool ToggleCharacterLightShadow(out string status)
    {
        CameraLightCtrl.LightInfo info;
        if (!TryGetCharacterLightInfo(out info))
        {
            status = "角色光尚未初始化";
            return false;
        }

        info.shadow = !info.shadow;
        ReflectCharacterLight();
        status = info.shadow ? "角色光阴影已开启" : "角色光阴影已关闭";
        return true;
    }

    public static bool TryGetAudio(VRAudioChannel channel, out int volume, out bool enabled)
    {
        volume = 0;
        enabled = false;
        SoundData data = GetAudioData(channel);
        if (data == null)
            return false;
        volume = data.Volume;
        enabled = data.Switch;
        return true;
    }

    public static bool AdjustAudio(VRAudioChannel channel, int delta, out string status)
    {
        SoundData data = GetAudioData(channel);
        if (data == null)
        {
            status = "声音配置尚未初始化";
            return false;
        }

        data.Volume = Mathf.Clamp(data.Volume + delta, 0, 100);
        SaveAudioConfig();
        status = GetAudioName(channel) + " " + data.Volume + "%";
        return true;
    }

    public static bool ToggleAudio(VRAudioChannel channel, out string status)
    {
        SoundData data = GetAudioData(channel);
        if (data == null)
        {
            status = "声音配置尚未初始化";
            return false;
        }

        data.Switch = !data.Switch;
        SaveAudioConfig();
        status = GetAudioName(channel) + (data.Switch ? " 已开启" : " 已静音");
        return true;
    }

    public static string GetAudioName(VRAudioChannel channel)
    {
        switch (channel)
        {
            case VRAudioChannel.Master:
                return "主音量";
            case VRAudioChannel.Bgm:
                return "BGM";
            case VRAudioChannel.Environment:
                return "环境音";
            case VRAudioChannel.SystemEffects:
                return "系统音效";
            default:
                return "游戏音效";
        }
    }

    private static bool TryGetCharacterLightInfo(out CameraLightCtrl.LightInfo info)
    {
        info = null;
        Studio.Studio studio = Singleton<Studio.Studio>.Instance;
        if (studio == null || studio.sceneInfo == null || studio.sceneInfo.charaLight == null)
            return false;
        info = studio.sceneInfo.charaLight;
        return true;
    }

    private static void ReflectCharacterLight()
    {
        Studio.Studio studio = Singleton<Studio.Studio>.Instance;
        if (studio != null && studio.cameraLightCtrl != null)
            studio.cameraLightCtrl.Reflect();
    }

    private static SoundData GetAudioData(VRAudioChannel channel)
    {
        SoundSystem sound = Manager.Config.SoundData;
        if (sound == null)
            return null;

        switch (channel)
        {
            case VRAudioChannel.Master:
                return sound.Master;
            case VRAudioChannel.Bgm:
                return sound.BGM;
            case VRAudioChannel.Environment:
                return sound.ENV;
            case VRAudioChannel.SystemEffects:
                return sound.SystemSE;
            default:
                return sound.GameSE;
        }
    }

    private static void SaveAudioConfig()
    {
        Manager.Config config = Singleton<Manager.Config>.Instance;
        if (config != null)
            config.Save();
    }
}

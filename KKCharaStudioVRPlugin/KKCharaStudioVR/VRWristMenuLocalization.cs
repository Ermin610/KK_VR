using System;
using System.Collections;
using UnityEngine;
using VRGIN.Core;

namespace KKCharaStudioVR;

public sealed partial class VRWristMenuController
{
    private const string ChineseLanguage = "zh-CN";
    private const string JapaneseLanguage = "ja-JP";
    private const string EnglishLanguage = "en-US";

    private static readonly string[,] JapaneseStatusTranslations =
    {
        { "工作室角色列表尚未就绪", "スタジオのキャラ一覧はまだ準備できていません" },
        { "所选角色尚未初始化完成或已经失效", "選択したキャラは初期化中か無効です" },
        { "所选角色已不在当前场景", "選択したキャラは現在のシーンにいません" },
        { "当前场景没有可替换的男角色", "置き換え可能な男性キャラがいません" },
        { "当前场景没有可替换的女角色", "置き換え可能な女性キャラがいません" },
        { "当前场景没有可选角色", "選択できるキャラがいません" },
        { "当前场景没有角色", "現在のシーンにキャラがいません" },
        { "无法读取场景角色：", "シーンのキャラを読めません：" },
        { "请选择接收动作的角色", "モーションを適用するキャラを選択" },
        { "请选择要替换的场景角色", "置き換えるシーンのキャラを選択" },
        { "请选择要调整高跟鞋的角色", "ハイヒールを調整するキャラを選択" },
        { "请选择动作、镜头和音频所在的根目录", "モーション・カメラ・音声のルートフォルダーを選択" },
        { "场景角色已变化，请重新选择高跟鞋目标", "シーンのキャラが変わりました。ハイヒール対象を選び直してください" },
        { "场景角色已变化，请重新选择换装目标", "シーンのキャラが変わりました。服装対象を選び直してください" },
        { "请先给这个角色读取动作 VMD", "先にこのキャラへモーションVMDを読み込んでください" },
        { "请先切换到手动骨骼调节", "先に手動ボーン調整へ切り替えてください" },
        { "当前动作控制器不支持高跟鞋调节", "現在のモーション制御はハイヒール調整に対応していません" },
        { "MMDD 没有返回 Fixed FOV 状态", "MMDDから固定視野の状態が返されませんでした" },
        { "MMDD 没有返回高跟鞋状态", "MMDDからハイヒールの状態が返されませんでした" },
        { "动作数据目录不存在，请重新选择", "モーションデータフォルダーがありません。選び直してください" },
        { "没有选择动作或镜头 VMD", "モーションまたはカメラVMDが選択されていません" },
        { "VMD 文件不存在或格式无效", "VMDファイルがないか形式が無効です" },
        { "匹配到的音频文件不存在", "対応する音声ファイルがありません" },
        { "音频格式不受 MMDD 支持", "この音声形式はMMDDに対応していません" },
        { "角色卡与目标角色性别不一致", "キャラカードと対象キャラの性別が一致しません" },
        { "男性角色只能替换为男卡", "男性キャラは男性カードでのみ置換できます" },
        { "女性角色只能替换为女卡", "女性キャラは女性カードでのみ置換できます" },
        { "工作室原生卡面接口未能读取图片", "スタジオのカード画像を読み込めませんでした" },
        { "角色卡文件无效", "キャラカードファイルが無効です" },
        { "请选择女性角色卡", "女性キャラカードを選択してください" },
        { "请选择男性角色卡", "男性キャラカードを選択してください" },
        { "角色卡读取失败：", "キャラカードの読込に失敗：" },
        { "角色替换失败：", "キャラの置換に失敗：" },
        { "已添加女角色：", "女性キャラを追加：" },
        { "已添加男角色：", "男性キャラを追加：" },
        { "角色光阴影已开启", "キャラ光の影をオンにしました" },
        { "角色光阴影已关闭", "キャラ光の影をオフにしました" },
        { "角色光尚未初始化", "キャラ光が初期化されていません" },
        { "角色光已开启", "キャラ光をオンにしました" },
        { "角色光已关闭", "キャラ光をオフにしました" },
        { "角色光强度 ", "キャラ光の強さ " },
        { "角色光颜色：", "キャラ光の色：" },
        { "声音配置尚未初始化", "サウンド設定が初期化されていません" },
        { "当前没有可调节的场景相机", "調整できるシーンカメラがありません" },
        { "背景色：", "背景色：" },
        { "场景读取界面尚未就绪", "シーン読込画面はまだ準備できていません" },
        { "已打开大屏幕场景读取", "大画面のシーン読込を開きました" },
        { "无法打开场景读取：", "シーン読込を開けません：" },
        { "场景保存功能尚未就绪", "シーン保存はまだ準備できていません" },
        { "场景已保存", "シーンを保存しました" },
        { "场景保存失败：", "シーン保存に失敗：" },
        { "未检测到 Timeline 1.5.x", "Timeline 1.5.xが見つかりません" },
        { "Timeline 已暂停", "Timelineを一時停止しました" },
        { "Timeline 开始播放", "Timelineを再生しました" },
        { "Timeline 操作失败", "Timelineの操作に失敗しました" },
        { "MMDD 播放状态已切换", "MMDDの再生状態を切り替えました" },
        { "MMDD 无法控制：", "MMDDを操作できません：" },
        { "高跟鞋参数已应用", "ハイヒール設定を適用しました" },
        { "高跟鞋控制失败：", "ハイヒール操作に失敗：" },
        { "高跟鞋控制失败", "ハイヒール操作に失敗しました" },
        { "VMD 载入失败：", "VMDの読込に失敗：" },
        { "已载入", "読込完了" },
        { " 动作：", " モーション：" },
        { " 镜头：", " カメラ：" },
        { " 音频：", " 音声：" },
        { "动作目标：", "モーション対象：" },
        { "无法选择动作目标：", "モーション対象を選択できません：" },
        { "整套换装失败：", "一括着替えに失敗：" },
        { "没有可切换状态", "切り替え可能な状態がありません" },
        { "没有其他状态", "ほかの状態がありません" },
        { "切换失败：", "切り替えに失敗：" },
        { "全部服装", "すべての服装" },
        { "穿好", "着用" },
        { "半脱", "半脱ぎ" },
        { "脱下", "脱衣" },
        { " 已开启", " オン" },
        { " 已关闭", " オフ" },
        { " 已静音", " ミュート" },
        { " 自动", " 自動" },
        { " 手动", " 手動" }
    };

    private static readonly string[,] EnglishStatusTranslations =
    {
        { "工作室角色列表尚未就绪", "The Studio character list is not ready" },
        { "所选角色尚未初始化完成或已经失效", "The selected character is still initializing or is invalid" },
        { "所选角色已不在当前场景", "The selected character is no longer in the scene" },
        { "当前场景没有可替换的男角色", "There is no replaceable male character in the scene" },
        { "当前场景没有可替换的女角色", "There is no replaceable female character in the scene" },
        { "当前场景没有可选角色", "There are no selectable characters in the scene" },
        { "当前场景没有角色", "There are no characters in the scene" },
        { "无法读取场景角色：", "Could not read scene characters: " },
        { "请选择接收动作的角色", "Select the characters that will receive the motion" },
        { "请选择要替换的场景角色", "Select the scene character to replace" },
        { "请选择要调整高跟鞋的角色", "Select the character whose high heels you want to adjust" },
        { "请选择动作、镜头和音频所在的根目录", "Select the root folder containing motion, camera, and audio files" },
        { "场景角色已变化，请重新选择高跟鞋目标", "The scene characters changed. Select the high-heel target again" },
        { "场景角色已变化，请重新选择换装目标", "The scene characters changed. Select the clothing target again" },
        { "请先给这个角色读取动作 VMD", "Load a motion VMD for this character first" },
        { "请先切换到手动骨骼调节", "Switch to manual bone adjustment first" },
        { "当前动作控制器不支持高跟鞋调节", "The current motion controller does not support high-heel adjustment" },
        { "MMDD 没有返回 Fixed FOV 状态", "MMDD did not return the Fixed FOV state" },
        { "MMDD 没有返回高跟鞋状态", "MMDD did not return the high-heel state" },
        { "动作数据目录不存在，请重新选择", "The motion data folder does not exist. Select it again" },
        { "没有选择动作或镜头 VMD", "No motion or camera VMD was selected" },
        { "VMD 文件不存在或格式无效", "The VMD file is missing or invalid" },
        { "匹配到的音频文件不存在", "The matched audio file does not exist" },
        { "音频格式不受 MMDD 支持", "MMDD does not support this audio format" },
        { "角色卡与目标角色性别不一致", "The character card and target character have different sexes" },
        { "男性角色只能替换为男卡", "A male character can only be replaced with a male card" },
        { "女性角色只能替换为女卡", "A female character can only be replaced with a female card" },
        { "工作室原生卡面接口未能读取图片", "Studio could not read the native card image" },
        { "角色卡文件无效", "The character card file is invalid" },
        { "请选择女性角色卡", "Select a female character card" },
        { "请选择男性角色卡", "Select a male character card" },
        { "角色卡读取失败：", "Could not load character card: " },
        { "角色替换失败：", "Could not replace character: " },
        { "已添加女角色：", "Added female character: " },
        { "已添加男角色：", "Added male character: " },
        { "角色光阴影已开启", "Character-light shadows enabled" },
        { "角色光阴影已关闭", "Character-light shadows disabled" },
        { "角色光尚未初始化", "Character light is not initialized" },
        { "角色光已开启", "Character light enabled" },
        { "角色光已关闭", "Character light disabled" },
        { "角色光强度 ", "Character-light intensity " },
        { "角色光颜色：", "Character-light color: " },
        { "声音配置尚未初始化", "Audio settings are not initialized" },
        { "当前没有可调节的场景相机", "There is no adjustable scene camera" },
        { "背景色：", "Background: " },
        { "场景读取界面尚未就绪", "The scene-load screen is not ready" },
        { "已打开大屏幕场景读取", "Opened the large scene-load screen" },
        { "无法打开场景读取：", "Could not open scene loading: " },
        { "场景保存功能尚未就绪", "Scene saving is not ready" },
        { "场景已保存", "Scene saved" },
        { "场景保存失败：", "Could not save scene: " },
        { "未检测到 Timeline 1.5.x", "Timeline 1.5.x was not detected" },
        { "Timeline 已暂停", "Timeline paused" },
        { "Timeline 开始播放", "Timeline started" },
        { "Timeline 操作失败", "Timeline operation failed" },
        { "MMDD 播放状态已切换", "MMDD playback toggled" },
        { "MMDD 无法控制：", "Could not control MMDD: " },
        { "高跟鞋参数已应用", "High-heel preset applied" },
        { "高跟鞋控制失败：", "High-heel control failed: " },
        { "高跟鞋控制失败", "High-heel control failed" },
        { "VMD 载入失败：", "Could not load VMD: " },
        { "已载入", "Loaded" },
        { " 动作：", " Motion: " },
        { " 镜头：", " Camera: " },
        { " 音频：", " Audio: " },
        { "动作目标：", "Motion target: " },
        { "无法选择动作目标：", "Could not select motion target: " },
        { "整套换装失败：", "Could not change the full outfit: " },
        { "没有可切换状态", "No switchable state" },
        { "没有其他状态", "No other state" },
        { "切换失败：", "Could not switch: " },
        { "全部服装", "All clothing" },
        { "穿好", "Dressed" },
        { "半脱", "Half dressed" },
        { "脱下", "Undressed" },
        { " 已开启", " enabled" },
        { " 已关闭", " disabled" },
        { " 已静音", " muted" },
        { " 自动", " automatic" },
        { " 手动", " manual" }
    };

    private string CurrentLanguage
    {
        get
        {
            ResolveSettings();
            string language = _settings != null ? _settings.WristMenuLanguage : ChineseLanguage;
            if (string.Equals(language, JapaneseLanguage, StringComparison.OrdinalIgnoreCase))
                return JapaneseLanguage;
            if (string.Equals(language, EnglishLanguage, StringComparison.OrdinalIgnoreCase))
                return EnglishLanguage;
            return ChineseLanguage;
        }
    }

    private bool IsJapanese => CurrentLanguage == JapaneseLanguage;
    private bool IsEnglish => CurrentLanguage == EnglishLanguage;

    private string L(string chinese, string japanese, string english)
    {
        if (IsJapanese)
            return japanese;
        return IsEnglish ? english : chinese;
    }

    private string LocalizeStatus(string message)
    {
        if (string.IsNullOrEmpty(message) || CurrentLanguage == ChineseLanguage)
            return message;

        string[,] translations = IsJapanese ? JapaneseStatusTranslations : EnglishStatusTranslations;
        for (int i = 0; i < translations.GetLength(0); i++)
            message = message.Replace(translations[i, 0], translations[i, 1]);
        return message;
    }

    private string GetClothingPartLabel(int partId)
    {
        string[] chinese = { "上衣", "下装", "胸罩", "内裤", "手套", "裤袜", "袜子", "鞋子" };
        string[] japanese = { "トップス", "ボトムス", "ブラ", "ショーツ", "手袋", "ストッキング", "靴下", "靴" };
        string[] english = { "Top", "Bottom", "Bra", "Underwear", "Gloves", "Pantyhose", "Socks", "Shoes" };
        if (partId < 0 || partId >= chinese.Length)
            return L("服装", "服装", "Clothing");
        return L(chinese[partId], japanese[partId], english[partId]);
    }

    private string LocalizeClothingStateLabel(string state)
    {
        switch (state)
        {
            case "穿好":
                return L("穿好", "着用", "Dressed");
            case "半脱":
                return L("半脱", "半脱ぎ", "Half");
            case "半脱 2":
                return L("半脱 2", "半脱ぎ 2", "Half 2");
            case "脱下":
                return L("脱下", "脱衣", "Off");
            case "不可用":
                return L("不可用", "利用不可", "Unavailable");
            case "无服装":
                return L("无服装", "服装なし", "No clothing");
            default:
                return state;
        }
    }

    private void HandleSetLanguage(string language)
    {
        ResolveSettings();
        if (_settings == null)
        {
            SetStatus(
                L("VR 设置尚未初始化", "VR設定が初期化されていません", "VR settings are not initialized"),
                new Color(1f, 0.38f, 0.34f, 1f),
                6f);
            return;
        }

        string normalized = language == JapaneseLanguage
            ? JapaneseLanguage
            : language == EnglishLanguage
                ? EnglishLanguage
                : ChineseLanguage;
        if (CurrentLanguage == normalized)
        {
            RefreshLanguageSettings();
            return;
        }

        _settings.WristMenuLanguage = normalized;
        bool saved = true;
        try
        {
            _settings.Save();
            VRLog.Info("Wrist menu language saved: " + normalized);
        }
        catch (Exception ex)
        {
            saved = false;
            VRLog.Error("Unable to save wrist menu language: " + ex.Message);
        }

        StartCoroutine(RebuildMenuForLanguage(saved));
    }

    private IEnumerator RebuildMenuForLanguage(bool saved)
    {
        yield return null;

        bool reopen = _isOpen;
        ++_characterPreviewRequestId;
        _characterPreviewLoading = false;
        SetHoveredButton(null);
        DestroyPointerVisuals();
        ReleaseCharacterPreviewTexture();
        if (_menuRoot != null)
            Destroy(_menuRoot);
        ClearMenuReferences();

        yield return null;
        if (!EnsureMenu())
        {
            _isOpen = false;
            VRLog.Error("Unable to rebuild wrist menu after changing language.");
            yield break;
        }

        _isOpen = reopen;
        _poseInitialized = false;
        _menuRoot.SetActive(reopen);
        if (reopen)
        {
            ShowPage(WristMenuPage.Settings);
            ShowSettingsSection(WristSettingsSection.General);
            SetStatus(
                saved
                    ? L("界面语言已切换", "表示言語を変更しました", "Interface language changed")
                    : L(
                        "语言已切换，但无法保存到下次启动",
                        "言語を変更しましたが、次回起動用に保存できませんでした",
                        "Language changed, but it could not be saved for the next launch"),
                saved ? new Color(0.35f, 1f, 0.62f, 1f) : new Color(1f, 0.72f, 0.25f, 1f),
                saved ? 4f : 8f);
            EnsurePointer();
        }
    }

    private void ClearMenuReferences()
    {
        _menuRoot = null;
        _menuRect = null;
        _rootPage = null;
        _clothingPage = null;
        _browserPage = null;
        _sceneSavePage = null;
        _characterCardsPage = null;
        _characterPreviewPage = null;
        _mmdPage = null;
        _highHeelsPage = null;
        _settingsPage = null;
        _settingsGeneralPanel = null;
        _settingsVisualPanel = null;
        _settingsCharacterLightPanel = null;
        _settingsAudioPanel = null;
        _statusText = null;
        _statusIndicator = null;
        _hoveredButton = null;
        _clothingTargetButton = null;
        _loadVmdButton = null;
        _timelineButton = null;
        _vmdRootButton = null;
        _fixedFovToggleButton = null;
        _fixedFovValueText = null;
        _fixedFovCameraCountText = null;
        _highHeelsTargetButton = null;
        _highHeelsModeButton = null;
        _highHeelsShoesDetectButton = null;
        _shoesOffsetToggleButton = null;
        _highHeelsLoadPresetButton = null;
        _settingsGeneralTab = null;
        _settingsVisualTab = null;
        _settingsCharacterLightTab = null;
        _settingsAudioTab = null;
        _languageChineseButton = null;
        _languageJapaneseButton = null;
        _languageEnglishButton = null;
        _backgroundValueText = null;
        _characterLightToggle = null;
        _characterLightShadowToggle = null;
        _characterLightIntensityText = null;
        _browserTitleText = null;
        _browserHeaderActionButton = null;
        _browserPathText = null;
        _browserScrollText = null;
        _characterPreviewImage = null;
        _characterPreviewRect = null;
        _characterPreviewMessageText = null;
        _characterPreviewNameText = null;
        _characterPreviewLoadButton = null;
        _characterPreviewReplaceButton = null;
        for (int i = 0; i < _clothingPartButtons.Length; i++)
            _clothingPartButtons[i] = null;
        for (int i = 0; i < _browserEntryButtons.Length; i++)
            _browserEntryButtons[i] = null;
        for (int i = 0; i < _highHeelsAngleTexts.Length; i++)
            _highHeelsAngleTexts[i] = null;
        for (int i = 0; i < _shoesOffsetTexts.Length; i++)
            _shoesOffsetTexts[i] = null;
        for (int i = 0; i < _audioVolumeTexts.Length; i++)
        {
            _audioVolumeTexts[i] = null;
            _audioToggleButtons[i] = null;
        }
    }
}

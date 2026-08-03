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
        { "上一项操作尚未完成", "前の操作がまだ完了していません" },
        { "工作室主相机尚未就绪", "スタジオのメインカメラはまだ準備できていません" },
        { "无法打开服装卡目录：", "衣装カードフォルダーを開けません：" },
        { "服装卡文件无效", "衣装カードファイルが無効です" },
        { "文件不是有效的服装卡", "有効な衣装カードではありません" },
        { "服装卡面读取失败：", "衣装カード画像の読込に失敗：" },
        { "卡面预览失败：", "カード画像のプレビューに失敗：" },
        { "已有服装卡正在替换，请稍后", "別の衣装カードを適用中です。しばらくお待ちください" },
        { "所选角色仍在读取，请稍后再换装", "選択したキャラを読込中です。しばらくしてから着替えてください" },
        { "目标角色尚未初始化完成或已经失效", "対象キャラは初期化中か無効です" },
        { "Coordinate Load Option 正在处理其他换装，请稍后", "Coordinate Load Optionが別の着替えを処理中です" },
        { "Coordinate Load Option 正在处理其他换装", "Coordinate Load Optionが別の着替えを処理中です" },
        { "Coordinate Load Option 接口不可用", "Coordinate Load Optionのインターフェースを利用できません" },
        { "Coordinate Load Option 接口不兼容：", "Coordinate Load Optionのインターフェースに互換性がありません：" },
        { "Coordinate Load Option 启动失败，正在恢复角色状态", "Coordinate Load Optionの起動に失敗したため、キャラ状態を復元中です" },
        { "读取服装卡饰品槽失败：", "衣装カードのアクセサリースロットを読み込めません：" },
        { "无法准备整套换装的饰品追加：", "一式着替えのアクセ追加を準備できません：" },
        { "整套换装的饰品追加需要 Coordinate Load Option；未执行换装", "一式着替えでアクセを追加するにはCoordinate Load Optionが必要です。着替えは実行していません" },
        { "整套换装未执行：", "一式着替えを実行できません：" },
        { "整套换装未完成：目标角色没有足够的空饰品槽", "一式着替えを完了できません：対象キャラに十分な空きスロットがありません" },
        { "整套换装被中止：检测到现有饰品数据发生变化", "現在のアクセデータの変更を検出したため、一式着替えを中止しました" },
        { "正在完整替换衣服；源卡没有饰品，现有饰品保持不变", "衣服を一式変更中です。元カードにアクセがないため、現在のアクセは維持します" },
        { "正在完整替换衣服，并把全部源饰品追加到空槽", "衣服を一式変更し、元カードの全アクセを空き枠へ追加しています" },
        { "自选饰品需要 Coordinate Load Option；未执行换装", "アクセ選択にはCoordinate Load Optionが必要です。着替えは実行していません" },
        { "自选饰品未执行：", "アクセ選択を実行できません：" },
        { "自选饰品未完成：目标角色没有足够的空饰品槽", "アクセ選択を完了できません：対象キャラに十分な空きスロットがありません" },
        { "无法验证 Coordinate Load Option 的饰品绑定数据", "Coordinate Load Optionのアクセ連携データを確認できません" },
        { "无法读取服装卡的饰品绑定数据", "衣装カードのアクセ連携データを読み込めません" },
        { "服装卡含饰品绑定数据（", "衣装カードにアクセ連携データがあります（" },
        { "目标角色含饰品绑定数据（", "対象キャラにアクセ連携データがあります（" },
        { "检查饰品绑定数据失败：", "アクセ連携データの確認に失敗：" },
        { "已只替换衣服，现有饰品保持不变：", "服だけを変更し、現在のアクセを維持：" },
        { "已换衣服并把所选饰品追加到空槽：", "服を変更し、選択アクセを空き枠へ追加：" },
        { "已完整替换衣服；现有饰品保持不变：", "衣服を一式変更し、現在のアクセを維持：" },
        { "这个角色没有由本插件追加且仍可安全管理的饰品", "このキャラには、このプラグインが追加した安全に管理できるアクセがありません" },
        { "已有服装卡操作正在进行，请稍后再管理追加饰品", "衣装カード操作中です。完了後に追加アクセを管理してください" },
        { "场景角色已变化，请重新选择饰品管理目标", "シーンのキャラが変わりました。アクセ管理対象を選び直してください" },
        { "所选角色仍在读取，请稍后再管理追加饰品", "選択したキャラを読込中です。完了後に追加アクセを管理してください" },
        { "所选饰品已被修改或不再由本插件管理；未删除任何饰品", "選択アクセは変更済みか管理対象外です。何も削除していません" },
        { "饰品槽在删除前已变化，为避免误删已取消操作", "削除前にアクセ枠が変わったため、誤削除防止のため中止しました" },
        { "工作室没有接受饰品删除请求", "スタジオがアクセ削除要求を受け付けませんでした" },
        { "已移除本次追加饰品 ", "追加アクセを削除：" },
        { "正在移除本次追加饰品 ", "追加アクセを削除中：" },
        { "移除追加饰品没有启动", "追加アクセの削除を開始できませんでした" },
        { "移除追加饰品失败：", "追加アクセの削除に失敗：" },
        { "饰品操作失败：", "アクセ操作に失敗：" },
        { "已跳过重复饰品 ", "重複アクセをスキップ：" },
        { "只换衣服被中止：检测到饰品数据发生变化", "アクセデータの変更を検出したため、服だけの変更を中止しました" },
        { "已有服装卡操作正在进行，请稍后再撤销", "衣装カード操作中です。完了後に元に戻してください" },
        { "场景角色已变化，请重新选择撤销目标", "シーンのキャラが変わりました。元に戻す対象を選び直してください" },
        { "所选角色仍在读取，请稍后再撤销", "選択したキャラを読込中です。完了後に元に戻してください" },
        { "当前角色没有可撤销的换装记录", "このキャラには元に戻せる着替え記録がありません" },
        { "正在撤销到换装前状态", "着替え前の状態へ戻しています" },
        { "已撤销到换装前状态", "着替え前の状態へ戻しました" },
        { "撤销换装没有启动", "着替えの取り消しを開始できませんでした" },
        { "撤销换装失败：", "着替えの取り消しに失敗：" },
        { "无法同步目标角色的插件扩展状态：", "対象キャラのプラグイン拡張状態を同期できません：" },
        { "；已恢复基础角色状态，但插件扩展状态恢复失败：", "。基本キャラ状態は復元しましたが、プラグイン拡張状態の復元に失敗：" },
        { "插件扩展状态恢复失败：", "プラグイン拡張状態の復元に失敗：" },
        { "无法备份目标角色当前服装", "対象キャラの現在の衣装をバックアップできません" },
        { "无法读取换装前的角色服装备份", "着替え前のキャラ衣装バックアップを読み込めません" },
        { "目标角色当前服装备份为空", "対象キャラの現在の衣装バックアップが空です" },
        { "服装扩展数据快照接口不可用", "衣装拡張データのスナップショット機能を利用できません" },
        { "无法锁定目标角色现有饰品", "対象キャラの現在のアクセを保持できません" },
        { "服装卡已替换，并保护现有发饰：", "現在の髪アクセを保護して衣装カードを適用：" },
        { "已用兼容模式只替换衣服，并保留全部现有饰品：", "互換モードで衣服のみを適用し、現在の全アクセを保持：" },
        { "为保护发饰，已只替换衣服并保留全部现有饰品：", "髪アクセ保護のため衣服のみを適用し、現在の全アクセを保持：" },
        { "已完整替换衣服，并把全部源饰品追加到空槽；现有饰品保持不变：", "衣服を一式変更し、元カードの全アクセを空き枠へ追加しました。現在のアクセは維持：" },
        { "已完整替换衣服；源卡没有饰品，现有饰品保持不变：", "衣服を一式変更しました。元カードにアクセがないため、現在のアクセは維持：" },
        { "；角色恢复读取超时，已解除界面等待；请重载场景后再换装", "。キャラ復元がタイムアウトしたため画面の待機を解除しました。シーン再読込後に着替えてください" },
        { "；自动恢复角色状态失败：", "。キャラ状態の自動復元に失敗：" },
        { "；已恢复角色状态", "。キャラ状態を復元しました" },
        { "服装卡替换异常：", "衣装カード適用中に異常が発生：" },
        { "服装卡替换已取消", "衣装カードの適用をキャンセルしました" },
        { "ExtensibleSaveFormat 服装快照接口不兼容", "ExtensibleSaveFormatの衣装スナップショットに互換性がありません" },
        { "服装扩展数据快照为空", "衣装拡張データのスナップショットが空です" },
        { "目标角色服装快照不可用", "対象キャラの衣装スナップショットを利用できません" },
        { "无法读取换装前的服装扩展数据快照", "着替え前の衣装拡張データを読み込めません" },
        { "工作室没有接受服装快照恢复请求", "スタジオが衣装スナップショットの復元要求を受け付けませんでした" },
        { "服装卡替换失败：", "衣装カードの適用に失敗：" },
        { "完整替换失败：目标角色已经失效", "完全な着替えに失敗：対象キャラが無効です" },
        { "完整替换失败：", "完全な着替えに失敗：" },
        { "服装卡替换异常，已恢复角色状态：", "衣装カード適用中に異常が発生し、キャラ状態を復元：" },
        { "服装卡替换没有启动", "衣装カードの適用を開始できませんでした" },
        { "Coordinate Load Option 未完成加载", "Coordinate Load Optionが読込を完了できませんでした" },
        { "兼容模式换装失败：", "互換モードの着替えに失敗：" },
        { "服装卡替换超时，已恢复角色状态", "衣装カードの適用がタイムアウトしたため、キャラ状態を復元しました" },
        { "完整服装读取时间过长，已解除界面等待；完成或重载场景前不能再次换装", "完全な衣装の読込に時間がかかっているため、画面の待機を解除しました。読込完了またはシーン再読込までは再度着替えできません" },
        { "目标角色已经失效", "対象キャラが無効です" },
        { "场景角色已变化，换装结果未继续绑定到新角色", "シーンのキャラが変わったため、新しいキャラには適用していません" },
        { "场景角色已变化，后台换装结果未继续绑定到新角色", "シーンのキャラが変わったため、バックグラウンドの着替え結果を新しいキャラには適用していません" },
        { "当前场景没有可替换服装的角色", "着替え可能なキャラがいません" },
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
        { "角色替换前无法清理旧 VMD：", "キャラ置換前に古いVMDを解除できません：" },
        { "角色替换前无法确认旧 VMD 状态", "キャラ置換前に古いVMDの状態を確認できません" },
        { "VMD 重新绑定失败，旧绑定已清理：", "VMDの再接続に失敗しました。古い接続は解除済みです：" },
        { "VMD 重新绑定失败：", "VMDの再接続に失敗：" },
        { "VMD 已重新绑定：", "VMDを再接続：" },
        { "原 VMD 已恢复", "元のVMDを復元しました" },
        { "原 VMD 恢复失败：", "元のVMDを復元できません：" },
        { "新角色读取超时，旧 VMD 已安全清理但未重新绑定", "新しいキャラの読込がタイムアウトしました。古いVMDは安全に解除され、再接続されていません" },
        { "当前选中角色已经变化，请重新确认后再清空", "選択中のキャラが変わりました。もう一度確認してから消去してください" },
        { "清空 MMD 数据失败：", "MMDデータの消去に失敗：" },
        { "MMDD 没有返回可信的清空结果，未确认数据状态", "MMDDから信頼できる消去結果が返らず、データ状態を確認できません" },
        { "清空失败且回滚不完整，请勿保存场景并先重载备份：", "消去に失敗し、復元も不完全です。シーンを保存せず、先にバックアップを再読込してください：" },
        { "清空失败，原 MMDD 数据已恢复：", "消去に失敗しましたが、元のMMDDデータは復元されました：" },
        { "未执行清空：", "消去は実行されませんでした：" },
        { "已载入", "読込完了" },
        { " 动作：", " モーション：" },
        { " 镜头：", " カメラ：" },
        { " 音频：", " 音声：" },
        { "动作目标：", "モーション対象：" },
        { "无法选择动作目标：", "モーション対象を選択できません：" },
        { "整套换装失败：", "一括着替えに失敗：" },
        { "预设服装切换失败：", "衣装プリセットの切り替えに失敗：" },
        { "工作室没有接受切换请求", "スタジオが切り替え要求を受け付けませんでした" },
        { "预设服装仍在读取，请稍后", "衣装プリセットを読み込み中です。しばらくお待ちください" },
        { "角色没有这个预设服装", "このキャラにはこの衣装プリセットがありません" },
        { "不支持的预设服装", "対応していない衣装プリセットです" },
        { "已切换到预设服装 服装 ", "衣装プリセットを切り替えました：衣装 " },
        { "已是预设服装 服装 ", "現在の衣装プリセット：衣装 " },
        { "已切换到预设服装 ", "衣装プリセットを切り替えました：" },
        { "已是预设服装 ", "現在の衣装プリセット：" },
        { "校服 1", "制服 1" },
        { "校服 2", "制服 2" },
        { "运动服", "体操着" },
        { "泳装", "水着" },
        { "社团服", "部活着" },
        { "便服", "私服" },
        { "睡衣", "パジャマ" },
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
        { "上一项操作尚未完成", "The previous operation has not finished yet" },
        { "工作室主相机尚未就绪", "The Studio main camera is not ready" },
        { "无法打开服装卡目录：", "Could not open the outfit-card folder: " },
        { "服装卡文件无效", "The outfit-card file is invalid" },
        { "文件不是有效的服装卡", "The file is not a valid outfit card" },
        { "服装卡面读取失败：", "Could not load the outfit-card preview: " },
        { "卡面预览失败：", "Could not preview the card: " },
        { "已有服装卡正在替换，请稍后", "Another outfit card is being applied. Please wait" },
        { "所选角色仍在读取，请稍后再换装", "The selected character is still loading. Please wait before changing outfits" },
        { "目标角色尚未初始化完成或已经失效", "The target character is still initializing or is no longer valid" },
        { "Coordinate Load Option 正在处理其他换装，请稍后", "Coordinate Load Option is processing another outfit change" },
        { "Coordinate Load Option 正在处理其他换装", "Coordinate Load Option is processing another outfit change" },
        { "Coordinate Load Option 接口不可用", "The Coordinate Load Option interface is unavailable" },
        { "Coordinate Load Option 接口不兼容：", "The Coordinate Load Option interface is incompatible: " },
        { "Coordinate Load Option 启动失败，正在恢复角色状态", "Coordinate Load Option failed to start; restoring the character state" },
        { "读取服装卡饰品槽失败：", "Could not read outfit-card accessory slots: " },
        { "无法准备整套换装的饰品追加：", "Could not prepare full-outfit accessory appending: " },
        { "整套换装的饰品追加需要 Coordinate Load Option；未执行换装", "Adding accessories during a full outfit change requires Coordinate Load Option; no outfit change was made" },
        { "整套换装未执行：", "The full outfit change was not applied: " },
        { "整套换装未完成：目标角色没有足够的空饰品槽", "The full outfit change was not applied: the target has too few empty accessory slots" },
        { "整套换装被中止：检测到现有饰品数据发生变化", "The full outfit change was stopped because existing accessory data changed" },
        { "正在完整替换衣服；源卡没有饰品，现有饰品保持不变", "Replacing all clothes; the source card has no accessories, so current accessories will stay unchanged" },
        { "正在完整替换衣服，并把全部源饰品追加到空槽", "Replacing all clothes and adding every source accessory to empty slots" },
        { "自选饰品需要 Coordinate Load Option；未执行换装", "Picking accessories requires Coordinate Load Option; no outfit change was made" },
        { "自选饰品未执行：", "Picked accessories were not applied: " },
        { "自选饰品未完成：目标角色没有足够的空饰品槽", "Picked accessories were not applied: the target has too few empty accessory slots" },
        { "无法验证 Coordinate Load Option 的饰品绑定数据", "Could not validate Coordinate Load Option accessory binding data" },
        { "无法读取服装卡的饰品绑定数据", "Could not read outfit-card accessory binding data" },
        { "服装卡含饰品绑定数据（", "The outfit card contains accessory binding data (" },
        { "目标角色含饰品绑定数据（", "The target character contains accessory binding data (" },
        { "检查饰品绑定数据失败：", "Could not inspect accessory binding data: " },
        { "已只替换衣服，现有饰品保持不变：", "Changed clothing only and kept all current accessories: " },
        { "已换衣服并把所选饰品追加到空槽：", "Changed clothing and added picked accessories to empty slots: " },
        { "已完整替换衣服；现有饰品保持不变：", "Replaced all clothes and kept the current accessories: " },
        { "这个角色没有由本插件追加且仍可安全管理的饰品", "This character has no safely manageable accessories appended by this plugin" },
        { "已有服装卡操作正在进行，请稍后再管理追加饰品", "An outfit-card operation is still running; wait before managing appended accessories" },
        { "场景角色已变化，请重新选择饰品管理目标", "The scene character changed; select the accessory-management target again" },
        { "所选角色仍在读取，请稍后再管理追加饰品", "The selected character is still loading; wait before managing appended accessories" },
        { "所选饰品已被修改或不再由本插件管理；未删除任何饰品", "The selected accessories were modified or are no longer managed; nothing was removed" },
        { "饰品槽在删除前已变化，为避免误删已取消操作", "An accessory slot changed before deletion, so the operation was cancelled to prevent accidental removal" },
        { "工作室没有接受饰品删除请求", "Studio did not accept the accessory-removal request" },
        { "已移除本次追加饰品 ", "Removed appended accessories: " },
        { "正在移除本次追加饰品 ", "Removing appended accessories: " },
        { "移除追加饰品没有启动", "The appended-accessory removal did not start" },
        { "移除追加饰品失败：", "Could not remove appended accessories: " },
        { "饰品操作失败：", "Accessory operation failed: " },
        { "已跳过重复饰品 ", "Skipped duplicate accessories: " },
        { " 件", " items" },
        { "只换衣服被中止：检测到饰品数据发生变化", "Clothing-only replacement was stopped because accessory data changed" },
        { "已有服装卡操作正在进行，请稍后再撤销", "An outfit-card operation is still running; wait before undoing" },
        { "场景角色已变化，请重新选择撤销目标", "The scene character changed; select the undo target again" },
        { "所选角色仍在读取，请稍后再撤销", "The selected character is still loading; wait before undoing" },
        { "当前角色没有可撤销的换装记录", "This character has no outfit change to undo" },
        { "正在撤销到换装前状态", "Restoring the pre-change outfit state" },
        { "已撤销到换装前状态", "Restored the pre-change outfit state" },
        { "撤销换装没有启动", "The outfit undo did not start" },
        { "撤销换装失败：", "Could not undo the outfit change: " },
        { "无法同步目标角色的插件扩展状态：", "Could not synchronize the target character's plugin extension state: " },
        { "；已恢复基础角色状态，但插件扩展状态恢复失败：", ". Base character state was restored, but plugin extension state failed: " },
        { "插件扩展状态恢复失败：", "Could not restore plugin extension state: " },
        { "无法备份目标角色当前服装", "Could not back up the target character's current outfit" },
        { "无法读取换装前的角色服装备份", "Could not read the pre-change character outfit backup" },
        { "目标角色当前服装备份为空", "The target character's current outfit backup is empty" },
        { "服装扩展数据快照接口不可用", "The outfit extension-data snapshot interface is unavailable" },
        { "无法锁定目标角色现有饰品", "Could not preserve the target character's current accessories" },
        { "服装卡已替换，并保护现有发饰：", "Applied outfit card and protected current hair accessories: " },
        { "已用兼容模式只替换衣服，并保留全部现有饰品：", "Applied clothing only in compatibility mode and kept all current accessories: " },
        { "为保护发饰，已只替换衣服并保留全部现有饰品：", "Applied clothing only and kept all current accessories to protect hair: " },
        { "已完整替换衣服，并把全部源饰品追加到空槽；现有饰品保持不变：", "Replaced all clothes and added every source accessory to empty slots; current accessories were kept: " },
        { "已完整替换衣服；源卡没有饰品，现有饰品保持不变：", "Replaced all clothes; the source card had no accessories, so current accessories were kept: " },
        { "；角色恢复读取超时，已解除界面等待；请重载场景后再换装", ". Character recovery timed out, so the UI wait was released. Reload the scene before changing outfits again" },
        { "；自动恢复角色状态失败：", ". Automatic character-state recovery failed: " },
        { "；已恢复角色状态", ". The character state was restored" },
        { "服装卡替换异常：", "The outfit-card load failed unexpectedly: " },
        { "服装卡替换已取消", "The outfit-card load was cancelled" },
        { "ExtensibleSaveFormat 服装快照接口不兼容", "The ExtensibleSaveFormat outfit-snapshot interface is incompatible" },
        { "服装扩展数据快照为空", "The outfit extension-data snapshot is empty" },
        { "目标角色服装快照不可用", "The target character outfit snapshot is unavailable" },
        { "无法读取换装前的服装扩展数据快照", "Could not read the pre-change outfit extension-data snapshot" },
        { "工作室没有接受服装快照恢复请求", "Studio did not accept the outfit-snapshot recovery request" },
        { "服装卡替换失败：", "Could not apply outfit card: " },
        { "完整替换失败：目标角色已经失效", "Full outfit replacement failed: The target character is no longer valid" },
        { "完整替换失败：", "Full outfit replacement failed: " },
        { "服装卡替换异常，已恢复角色状态：", "The outfit load failed and the character state was recovered: " },
        { "服装卡替换没有启动", "The outfit-card load did not start" },
        { "Coordinate Load Option 未完成加载", "Coordinate Load Option did not finish loading" },
        { "兼容模式换装失败：", "Compatibility-mode outfit change failed: " },
        { "服装卡替换超时，已恢复角色状态", "The outfit-card load timed out and the character state was recovered" },
        { "完整服装读取时间过长，已解除界面等待；完成或重载场景前不能再次换装", "The full outfit is taking too long to load, so the UI wait was released. You cannot change outfits again until loading finishes or the scene is reloaded" },
        { "目标角色已经失效", "The target character is no longer valid" },
        { "场景角色已变化，换装结果未继续绑定到新角色", "The scene character changed; the result was not bound to the new character" },
        { "场景角色已变化，后台换装结果未继续绑定到新角色", "The scene character changed; the background outfit result was not bound to the new character" },
        { "当前场景没有可替换服装的角色", "There is no character whose outfit can be replaced" },
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
        { "角色替换前无法清理旧 VMD：", "Could not detach the old VMD before replacing the character: " },
        { "角色替换前无法确认旧 VMD 状态", "Could not confirm the old VMD state before replacing the character" },
        { "VMD 重新绑定失败，旧绑定已清理：", "Could not rebind VMD; the stale binding was removed: " },
        { "VMD 重新绑定失败：", "Could not rebind VMD: " },
        { "VMD 已重新绑定：", "Rebound VMD: " },
        { "原 VMD 已恢复", "Restored the original VMD" },
        { "原 VMD 恢复失败：", "Could not restore the original VMD: " },
        { "新角色读取超时，旧 VMD 已安全清理但未重新绑定", "The new character timed out while loading; the old VMD was safely detached and was not rebound" },
        { "当前选中角色已经变化，请重新确认后再清空", "The selected character changed; confirm again before clearing" },
        { "清空 MMD 数据失败：", "Could not clear MMD data: " },
        { "MMDD 没有返回可信的清空结果，未确认数据状态", "MMDD did not return a trustworthy clear result; the data state is unconfirmed" },
        { "清空失败且回滚不完整，请勿保存场景并先重载备份：", "Clearing failed and rollback was incomplete; do not save the scene, and reload a backup first: " },
        { "清空失败，原 MMDD 数据已恢复：", "Clearing failed; the original MMDD data was restored: " },
        { "未执行清空：", "Nothing was cleared: " },
        { "已载入", "Loaded" },
        { " 动作：", " Motion: " },
        { " 镜头：", " Camera: " },
        { " 音频：", " Audio: " },
        { "动作目标：", "Motion target: " },
        { "无法选择动作目标：", "Could not select motion target: " },
        { "整套换装失败：", "Could not change the full outfit: " },
        { "预设服装切换失败：", "Could not switch the outfit preset: " },
        { "工作室没有接受切换请求", "Studio did not accept the switch request" },
        { "预设服装仍在读取，请稍后", "The outfit preset is still loading. Please wait" },
        { "角色没有这个预设服装", "This character does not have that outfit preset" },
        { "不支持的预设服装", "Unsupported outfit preset" },
        { "已切换到预设服装 服装 ", "Switched to outfit preset: Outfit " },
        { "已是预设服装 服装 ", "Current outfit preset: Outfit " },
        { "已切换到预设服装 ", "Switched to outfit preset: " },
        { "已是预设服装 ", "Current outfit preset: " },
        { "校服 1", "School 1" },
        { "校服 2", "School 2" },
        { "运动服", "Gym clothes" },
        { "泳装", "Swimwear" },
        { "社团服", "Club outfit" },
        { "便服", "Casual" },
        { "睡衣", "Pajamas" },
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
        if (_operationInProgress)
        {
            SetStatus(
                L("换装仍在进行，请稍候", "着替え中です。しばらくお待ちください", "The outfit change is still running"),
                new Color(1f, 0.72f, 0.25f, 1f),
                4f);
            return;
        }

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
        ClearClothingSubmenuReferences();
        ClearCoordinateMenuReferences();
        _browserPage = null;
        _sceneSavePage = null;
        _characterCardsPage = null;
        _characterPreviewPage = null;
        _timelinePage = null;
        _mmdPage = null;
        ClearMmdExtendedMenuReferences();
        _highHeelsPage = null;
        _settingsPage = null;
        _settingsGeneralPanel = null;
        _settingsInteractionPanel = null;
        _settingsVisualPanel = null;
        _settingsReShadePanel = null;
        _settingsCharacterLightPanel = null;
        _settingsAudioPanel = null;
        _statusText = null;
        _statusIndicator = null;
        _hoveredButton = null;
        _clothingTargetButton = null;
        _rootLoadVmdButton = null;
        _loadVmdButton = null;
        _timelineButton = null;
        _timelineFovModeButton = null;
        _timelineFovValueText = null;
        _timelineFovStateText = null;
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
        _settingsReShadeTab = null;
        _settingsCharacterLightTab = null;
        _settingsAudioTab = null;
        _languageChineseButton = null;
        _languageJapaneseButton = null;
        _languageEnglishButton = null;
        for (int i = 0; i < _controllerLayoutButtons.Length; i++)
            _controllerLayoutButtons[i] = null;
        _mainGuiDistanceText = null;
        _mainGuiScaleText = null;
        _timelineCameraFollowButton = null;
        _reshadeToggleButton = null;
        _reshadePresetDropdownButton = null;
        _reshadePresetDropdownPanel = null;
        for (int i = 0; i < _reshadePresetOptionButtons.Length; i++)
            _reshadePresetOptionButtons[i] = null;
        _reshadePresetPreviousButton = null;
        _reshadePresetNextButton = null;
        _reshadePresetPageText = null;
        _reshadePresetOptions.Clear();
        _reshadePresetDropdownOpen = false;
        _reshadePresetPage = 0;
        _reshadeAvailabilityText = null;
        _backgroundValueText = null;
        _characterLightToggle = null;
        _characterLightShadowToggle = null;
        _characterLightIntensityText = null;
        _browserTitleText = null;
        _browserHeaderActionButton = null;
        _browserGenderSwitchButton = null;
        _browserPathText = null;
        _browserScrollText = null;
        _characterPreviewImage = null;
        _characterPreviewRect = null;
        _characterPreviewMessageText = null;
        _characterPreviewNameText = null;
        _characterPreviewTitleText = null;
        _characterPreviewLoadButton = null;
        _characterPreviewReplaceButton = null;
        _coordinateClothesOnlyButton = null;
        _coordinateCustomButton = null;
        _coordinateFullButton = null;
        _coordinateUndoButton = null;
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

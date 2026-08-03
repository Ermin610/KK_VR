========================================================================
        KK CharaStudio VR Plugin - Ermin Edition v0.4.0
========================================================================

[安装 / Installation]

将本压缩包根目录内的全部内容复制到游戏根目录，并允许覆盖旧文件。
Copy everything inside this package root to the game root directory and allow
overwrite.

重要 / Important:
- BepInEx\KKCharaStudioVRPlugin.dll 是核心插件。请替换旧文件，不要在
  BepInEx\plugins 中保留第二份副本。
- KKVRReShadeBridge.addon64 必须与 CharaStudio.exe 位于同一目录，并在
  Studio 启动前存在。安装或替换 bridge 后请彻底关闭再重新启动 Studio。
- ReShade bridge 随本包提供；ReShade 本体、Shader、纹理和个人预设不随包
  分发。未安装 ReShade 时，其余 KK VR 功能仍可使用。

[启动 / Launch]

VR 工作室 / VR Studio:
1. 启动 SteamVR / Start SteamVR.
2. 双击 StartCharaStudioVR.bat。
3. 启动参数固定为：CharaStudio.exe --studiovr -vrmode OpenVR

普通工作室 / Normal Studio without VR:
1. 双击 StartCharaStudioNoVR.bat。
2. 启动参数固定为：CharaStudio.exe --novr -vrmode None

请不要只依赖 --novr。整合包中的 globalgamemanagers 已启用 VR 支持，
-vrmode None 用于在 Unity 启动阶段阻止 OpenVR 进入普通工作室进程。

[腕表菜单 / Wrist Menu]

- 左手柄 X 短按：打开或关闭腕表菜单。
- 右手射线 + 扳机：指向并点击按钮。
- 文件浏览或换装滚动区域：右摇杆上下滚动；桌面也可使用鼠标滚轮。
- 左摇杆按下：播放或暂停 MMDD。
- Timeline 控制空间中按下右摇杆：播放或暂停 Timeline。
- Timeline 播放时右摇杆上下：调整前后构图。
- 按住右手 Grip 后推动右摇杆：上下调整垂直位置，左右线性调整水平旋转。
- 设置 > 通用：切换简体中文、日本語或 English。

[v0.4.0 主要功能 / Main Features]

角色与换装 / Characters and outfits:
- 角色卡与服装卡原生卡面预览；添加、替换或安全移除场景人物。
- 角色替换可保留当前服装，并在替换前后协调 MMDD 与 Timeline。
- 角色内置服装预设；只换衣服、向空槽追加饰品、自选饰品和撤销换装。
- 八部位穿脱状态直接显示在换装页；没有衣物的部位置灰。
- 分部位透明度支持 0/25/50/75/100%、±5% 和 ±1%，并可恢复原材质。
- AZ 材质优先使用同族 Alpha Shader；兼容处理失败时会回滚。
- 高跟鞋参数按角色卡保存，并可自动识别和应用缓存。

MMD / MMDD:
- 一级菜单直接载入 VMD；动作目录会保存，也可随时更换。
- 自动识别动作、表情和镜头，选择场景角色并匹配关联镜头/音频。
- 新动作成功载入后停在第一帧；支持播放、暂停、回到首帧和清理当前角色
  动作与 MMDD 镜头。
- Fixed FOV、镜头调整、展示时隐藏手柄与 UI。
- 五套内置服装透明渐变/穿脱联动预设，并支持按 VMD 保存自定义事件表。

Timeline:
- 独立一级菜单、播放/暂停和镜头构图控制空间。
- 前后构图、垂直偏移、线性水平旋转；参数可保存、读取或恢复默认值。
- 播放镜头期间隔离冲突的位移、抓取、缩放和快捷操作。

ReShade 与工作室 / ReShade and Studio:
- 腕表内真正开关 ReShade，并动态扫描 reshade-shaders 根目录的 .ini 预设。
- VR 与 NoVR 启动器明确隔离，后台运行 SteamVR 不再自动触发 VR 模式。
- 工作室背景、角色光、阴影、灯光颜色和音量可在 VR 内调整。

[外部依赖 / External Integrations]

以下项目均为独立第三方软件，不包含在本压缩包内，也没有合并进 KK VR
核心 DLL。只有在用户自行安装兼容版本后，对应入口才会工作：

- KK VR Camera Sync (optional Timeline camera-follow companion):
  https://github.com/YukyoMoe/KK_VR_CameraSync
- MMD Director / MMDD by Countd360:
  https://www.patreon.com/posts/mmd-director-pv-55604378
- MMDD 2.9.6:
  https://www.patreon.com/posts/mmdd-v2-9-6-160423639
- VN Game Engine / VNGE, originally by Keitaro / KeiPlugins and maintained with
  later contributions including Countd360:
  https://www.patreon.com/posts/vnge-release-92082611
- VNGE + MMDD installation guide:
  https://www.patreon.com/posts/vnge-and-mmdd-94912633
- Coordinate Load Option by Jim Chen:
  https://xn--jgy.tw/Koikatu/coordinate-load-option/
- Timeline, MaterialEditor, Stiletto/KK_Stiletto and their own upstream
  dependencies must also be installed separately when those functions are used.

[版本 / Version]

Core plugin: 0.4.0
Integration pack: v0.4.0
Maintainer and current edition author: Ermin
Repository: https://github.com/Ermin610/KK_VR

[原作者与许可 / Credits and Licenses]

This work is derived from the KoikatuVR plugin by vrhth:
https://github.com/vrhth/KoikatuVR

VRGIN_KKCS is derived from vrhth's VRGIN fork and Eusth's VRGIN:
https://github.com/Eusth/VRGIN

The original projects were published under the MIT License. Their full notices
are retained under ThirdPartyNotices in this package.

KKVRReShadeBridge uses the official ReShade add-on API headers from ReShade
6.7.1. ReShade is developed by crosire / Patrick Mours:
https://github.com/crosire/reshade
The corresponding BSD-3-Clause notice is retained under ThirdPartyNotices.

Ermin's work covers the modern VR interaction, wrist UI, native ReShade control
bridge, compatibility fixes, and Studio/MMD/Timeline workflows in this edition.
The external integrations listed above remain the work of their respective
authors and are not redistributed in this file 5 package.

[No Warranty]

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
========================================================================

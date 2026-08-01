### v0.3.0 - VR 腕表工作流 + MMD / 角色工具

在 v0.2.0 ReShade VR 修复整合包基础上，加入完整的腕表快捷菜单与工作室制作流程。核心插件内部版本为 `0.0.14`。

![KK VR 腕表主界面](https://github.com/Ermin610/KK_VR/blob/v0.3.0/post_assets/v0.0.14/KKVR_%E4%B8%BB%E7%95%8C%E9%9D%A2_UI.png?raw=1)

---

| 文件 | 说明 |
|------|------|
| 文件1 | BepInEx 5.4.23.2 |
| 文件2 | BepIn4Patcher |
| 文件3 | KK_MainGameVR 1.2.0 |
| 文件4 | 已修补 VR 支持的 globalgamemanagers |
| 文件5 | Ermin v0.0.14 插件、最新 VRGIN_KKCS、启动脚本与运行文件 |
| 教程 | 与上一版相同格式的中 / 英 / 日三语安装教程 |

### 更新日志

- 新增苹果风格腕表快捷菜单，默认简体中文，并支持日本語与 English。
- 在 VR 内载入/保存场景，预览、添加或替换角色卡，切换服装状态与 IK 控制器。
- 集中整理 MMD 二级菜单：MMDD、Timeline、VMD、固定视野和高跟鞋工具。
- VMD 根目录选择一次后自动保存，也可随时更换；修复场景重载后的读取失败。
- 高跟鞋支持角色选择、鞋子检测、角度/鞋高偏移，以及按角色卡名称保存和读取参数。
- 腕表表面显示射线落点并截断穿透点击；修复打开腕表后右手转向失效。
- 工作室背景、角色光、阴影、灯光颜色和声音可直接在 VR 内调整。
- 保留 v0.2.0 的 ReShade VR 引擎补丁、桌面遮挡、现代手部交互与移动控制。

### MMDD / VNGE 联动与原作者

本版新增的是供 VR 腕表使用的控制与兼容桥接层，并非重新实现 MMDD 或 VNGE。两者均为独立的第三方项目，**不包含在本发布包内**，需要用户另行安装对应的 KK 版本。

- **MMD Director（MMDD）**：由 [Countd360](https://www.patreon.com/c360plugins) 创作。请参阅 [MMD Director 官方介绍](https://www.patreon.com/posts/mmd-director-pv-55604378) 与 [MMDD 2.9.6 发布页](https://www.patreon.com/posts/mmdd-v2-9-6-160423639)。VMD 动作、表情、镜头和音频的载入与播放仍由 MMDD 完成；KK VR 增加了头显内文件浏览、场景角色选择、播放/暂停、固定视野与高跟鞋控制。
- **VN Game Engine（VNGE）**：原作者为 [Keitaro / KeiPlugins](https://www.patreon.com/KeiPlugins)；Countd360 是重要贡献者并维护后续发布。[Countd360 维护的 VNGE 发布说明](https://www.patreon.com/posts/vnge-release-92082611) 也明确记录了这一作者关系。KK VR 通过 VNGE 与 MMDD 联动，不会把两者嵌入自身插件。
- **安装与兼容性**：请按 Countd360 的 [VNGE / MMDD 安装教程](https://www.patreon.com/posts/vnge-and-mmdd-94912633) 安装适用于 KK 的版本及其上游依赖。当前开发与实测环境为 **MMDD 2.9.6 + VNGE 44.0**；MMDD 将 2.9.6 标为 3.0 前测试版，不代表兼容所有旧版或未来版本。
- **高跟鞋使用前提**：目标角色须先载入动作 VMD，形成 MMDD 动作控制器；同时 MMDD 须检测到 KK 支持的 Stiletto/KK_Stiletto，或 BonerStateSync + KKABMX。

### 快速开始

按压缩包内 `教程/恋活工作室VR化步骤.txt` 的顺序安装，然后运行 `StartCharaStudioVR.bat`。进入 VR 后短按左手柄 `X` 打开腕表菜单，使用右手射线与扳机操作。

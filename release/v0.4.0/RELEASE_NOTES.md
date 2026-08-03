# KK CharaStudio VR Pack (Ermin) v0.4.0

v0.4.0 在 v0.3.0 腕表菜单的基础上，集中完善了角色与服装工作流、MMD/Timeline 控制、ReShade 运行时控制，以及 VR/普通工作室的启动隔离。核心插件版本与整合包版本统一为 `0.4.0`。

本版本继续以 Koikatu/Koikatsu 的旧版 Unity 与 .NET 3.5 环境为目标，不要求新的游戏运行时。

## 发布包内容

| 文件 | 说明 |
|---|---|
| 文件1 | BepInEx 5.4.23.2 |
| 文件2 | BepIn4Patcher |
| 文件3 | KK_MainGameVR 1.2.0 |
| 文件4 | 已修补 VR 支持的 `globalgamemanagers` |
| 文件5 | Ermin v0.4.0 核心插件、VRGIN_KKCS、ReShade 控制桥、VR/NoVR 启动器及工作室 VR 运行文件 |
| 教程 | 简体中文、English、日本語三语安装教程 |

## 主要新增功能

### 角色与服装

- 角色卡浏览支持卡面预览、添加角色和替换场景中的同性角色；男性入口保留在角色卡二级页面。
- 角色替换可选择“保留当前服装”，以新角色的脸、身体和头发替换人物，同时恢复原角色当前衣服、鞋袜、服装材质和穿脱状态。
- 角色替换继续使用 Studio 原生 `ChangeChara` 路径，并在替换前后暂停、恢复 Timeline；MMDD 动作控制器会先解绑，待新骨架稳定后再恢复，减少旧动作直接套到新骨架造成的脸部或身体变形。
- 角色卡页面新增“移除人物”。操作前会显示不可撤销确认和随人物删除的子物体数量，并通过 Studio 原生删除链清理对应对象与 Timeline 轨道。
- 可直接切换角色卡内置的校服、运动服、泳装、社团服、便服、睡衣及扩展服装预设。
- 服装卡浏览提供原生卡面预览，并支持只换衣服、向空槽追加饰品、自选饰品、管理已追加饰品和撤销上次换装。
- 绑定型服装卡会检测 Coordinate Load Option、MaterialEditor 等扩展数据；需要覆盖槽位时先提示风险并确认，失败时使用快照回滚。
- 换装页直接显示八个服装部位的穿脱状态；没有对应衣物的部位会置灰，长页面可用鼠标滚轮或指向区域后的右摇杆滚动。
- 新增分部位服装透明度：提供 0/25/50/75/100% 快捷值、±5% 调整和 ±1% 微调，并可恢复本部位。AZ 材质优先使用同族 Alpha Shader，避免无条件降级为通用透明材质。
- 高跟鞋参数可按角色卡保存；检测到缓存时自动切换到手动模式并应用，没有缓存时恢复自动模式，避免上一角色的参数串入当前角色。

### MMD / MMDD

- “载入动作”成为腕表一级菜单快捷入口；动作目录选择一次后会保存，也可随时更换。
- VMD 浏览会识别动作、表情和镜头数据，按需选择场景角色、关联镜头，并匹配附近的音频文件。
- 新 VMD 成功载入后默认停止在第一帧；重复载入时先清理旧动作，载入失败则尽量恢复原状态。
- MMD 页面提供播放/暂停、回到首帧、Fixed FOV、镜头调整、高跟鞋，以及“清空当前角色动作与 MMDD 镜头”的确认操作。
- MMD 展示模式可在播放时隐藏手模型、原生控制器、手环、射线和交互界面；退出、暂停、异常或切换场景时会恢复交互状态。
- 新增服装联动与透明渐变。所有 VMD 可使用五套内置预设，也可按 VMD 内容指纹保存独立事件表；回到首帧、向后跳转、循环或关闭联动时会从播放前造型重新计算，避免重复触发。
- MMD 播放/暂停保留在左摇杆按下，和 Timeline 的右摇杆控制空间相互独立。

### Timeline 与镜头

- Timeline 独立为一级菜单入口，提供播放/暂停和单独的镜头构图页面。
- Timeline 控制空间中，右摇杆按下播放或暂停；上下连续调整前后构图，按住右手 Grip 后，上下改为垂直偏移，左右进行线性水平旋转。
- 当前 Timeline 构图参数可保存、读取或一键恢复默认值。
- Timeline 镜头播放期间会锁定普通位移、世界抓取、角色/IK 抓取、双手缩放和冲突快捷操作；暂停后恢复手动控制。
- 场景切换、相机初始化和控制器恢复由统一流程协调，减少 CameraSync、GripMove 和旧对齐流程同时写入 VR 原点造成的镜头漂移或乱飞。

### ReShade、界面与启动

- 新增原生 `KKVRReShadeBridge.addon64`，腕表可以真正开关 ReShade 效果并切换预设，而不只是修改界面状态。
- ReShade 预设列表会在打开时重新扫描 `reshade-shaders` 根目录中的有效 `.ini`；不会递归读取备份或实验子目录。
- 腕表界面支持简体中文、日本語和 English；设置立即保存，关闭腕表后会在本次运行内恢复最后一个稳定页面。
- 新增确定性的双启动器：
  - `StartCharaStudioVR.bat` 使用 `--studiovr -vrmode OpenVR`；
  - `StartCharaStudioNoVR.bat` 使用 `--novr -vrmode None`。
- 普通工作室模式从 Unity 启动阶段阻止 OpenVR 载入；仅在后台运行 SteamVR 不再使普通工作室误进入 VR。

## 重点修复

- 修复角色替换后 Timeline 轨道仍存在但人物不再跟随动作的问题。
- 修复替换或重新载入 VMD 时继承旧控制器、旧帧数或播放状态造成的骨骼和表情变形。
- 修复换装覆盖发型饰品、连续加载导致饰品重复叠加，以及失败后留下半套状态的问题。
- 修复高跟鞋缓存跨角色串用、控制器尚未建立时无法延迟应用的问题。
- 修复 ReShade 只能看到两个静态预设、关闭按钮无效和切换结果不持久的问题。
- 修复 Timeline 播放期间多套移动/相机逻辑竞争引起的漂移、异常 FOV 和镜头乱飞。
- 修复场景载入、控制器重连或展示模式异常退出后单手、双手、手环或射线无法恢复的问题。
- 修复文件浏览、翻页、返回和更换目录时可能出现的索引越界。
- 修复旧整合包仅使用 `--novr` 时仍会在 Unity 启动阶段载入 OpenVR 的问题。

## 安装与兼容说明

- 文件5中的 `KKCharaStudioVRPlugin.dll` 应放在游戏根目录的 `BepInEx` 下。请替换旧 DLL，不要同时在 `BepInEx/plugins` 中保留第二份副本。
- `KKVRReShadeBridge.addon64` 必须放在 `CharaStudio.exe` 同级目录，并在 Studio 启动前存在；ReShade 只会在初始化时发现原生 add-on，安装或替换后需要冷重启 Studio。
- ReShade bridge 随文件5提供，但 **ReShade 本体、Shader、纹理和个人预设不随包分发**。未安装 ReShade 时，其余 KK VR 功能仍可使用。
- **KK VR Camera Sync、Timeline、MMDD、VNGE、Coordinate Load Option、MaterialEditor 和高跟鞋插件均为独立第三方项目，不随本包分发。** 对应功能只会在检测到兼容安装后启用。

## 外部项目与原作者

- 本项目由 **Ermin** 维护并开发当前版本：[Ermin610/KK_VR](https://github.com/Ermin610/KK_VR)。
- 原始 KoikatuVR 项目由 **vrhth** 发布：[vrhth/KoikatuVR](https://github.com/vrhth/KoikatuVR)。
- VRGIN 原项目由 **Eusth** 发布：[Eusth/VRGIN](https://github.com/Eusth/VRGIN)。
- 可选 CameraSync companion 由 **YukyoMoe** 发布：[YukyoMoe/KK_VR_CameraSync](https://github.com/YukyoMoe/KK_VR_CameraSync)。
- MMD Director（MMDD）由 **Countd360** 创作：[MMD Director](https://www.patreon.com/posts/mmd-director-pv-55604378)。
- VN Game Engine（VNGE）原作者为 **Keitaro / KeiPlugins**，后续版本由 Countd360 等贡献者维护：[VNGE release](https://www.patreon.com/posts/vnge-release-92082611)。
- Coordinate Load Option 由 **Jim Chen** 开发：[Coordinate Load Option](https://xn--jgy.tw/Koikatu/coordinate-load-option/)。
- ReShade 由 **crosire / Patrick Mours** 开发：[crosire/ReShade](https://github.com/crosire/reshade)。本包附带其 API 头文件对应的 BSD-3-Clause 许可声明。

MMDD、VNGE、CameraSync 及上述其他插件的源码和二进制均未合并进 KK VR 核心 DLL。KK VR 提供的是 VR 内控制、流程编排与软兼容桥。

## 快速开始

按压缩包内 `教程/恋活工作室VR化步骤.txt` 的顺序安装。VR 模式运行 `StartCharaStudioVR.bat`；普通工作室运行 `StartCharaStudioNoVR.bat`。进入 VR 后短按左手柄 `X` 打开腕表菜单，使用右手射线和扳机操作。

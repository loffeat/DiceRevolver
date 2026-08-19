# DiceRevolver 项目状态

## 当前阶段

- 可玩原型阶段；测试机器人行为树、共享角色控制接缝和零伤害射击资源已经完成。

## 全局有效状态

- `main` 的当前工作树包含顶视角移动、骰子左轮、弹药 HUD、运行时构筑页和四个示例词条。
- 每个骰面现拥有基础、开火时、命中时、开火后四个互不占用的单事件槽位；抽面时生成不可变配置快照供延迟和命中事件继续使用。
- 构筑页会显示每面的四行槽位摘要；选择词条后点击骰面只替换该词条对应槽位。示例库包含基础射击、DoubleTap、BlastRound 和 LoadedFour 四类词条。
- 构筑页可在场景加载后自动创建，按 `E` 打开或关闭；瞄准系统使用镜像虚拟枪口、近距离稳定解算和开火前同帧姿态刷新。
- 子弹事件可通过 `BulletEventContext.Schedule` 使用轻量游戏时间调度；双重射击默认在第一发后 `0.25` 秒生成第二发。
- 弹丸运行时属性已迁移到 `ProjectileDefinition`；每次骰面触发使用独立 `DiceFaceActivation`，并以默认 `32` 次事件预算限制连锁。
- 六个骰面均绑定基础左轮子弹生成事件；基础子弹视觉通过独立包装引用 `fire_1.prefab`，附加弹默认不回触命中事件。
- `fire_1.prefab` 原材质引用的 Shader 资源实际缺失；`ProjectileVisualWrapper` 现在只对错误 Shader 使用项目内透明粒子 Shader 兼容包装，不修改原始特效资源。
- 玩家、测试靶、弹丸和 Ground 统一使用世界 `Y=0` 玩法平面；玩家移动不再依赖重力或地面 Collider。
- Ground 已改为 `Background` Sorting Layer 的平铺 SpriteRenderer；基础弹丸粒子包装默认使用 `projectile` Sorting Layer。
- AimRoot 运行时具有最低渲染平面保护，玩家根节点为 `Y=0` 时手臂不会落入 Ground 下方；基础左轮弹丸视觉缩放为 `0.4`。
- 换弹视觉只对 `ArmVisual` 做明暗闪烁，不再写入其位置或旋转。
- 核心战斗 Inspector 端口已提供中文名称，序列化字段名和既有数值保持不变。
- 场景包含一个无限生命测试靶；弹丸通过通用受伤接口提交伤害，每次命中生成独立的世界空间飘字。
- 场景另包含一个 `TestRobot.prefab` 实例；机器人按距离接近、后退或横移，持续瞄准玩家，并通过共享左轮路径发射六面均为 0 伤害的基础视觉弹丸。
- `TopDownCharacterController` 统一拥有移动、玩法平面约束和转向；玩家与机器人分别通过 `TopDownPlayerController` 和 `TestRobotController` 提供控制意图。
- 可移植上下文系统已集成 `main`；原功能分支和隔离工作树已清理。
- 项目上下文框架位于 `.project-context/framework/`，项目实例资料位于 `.project-context/project/`。

## 活跃工作流

- 无。

## 完成历史

- [2026-08-19 测试机器人行为树](workstreams/2026-08-19-test-robot-behavior-tree/STATE.md)（`completed`）
- [2026-08-19 骰面四槽位配置](workstreams/2026-08-19-dice-face-four-slots/STATE.md)（`completed`）
- [2026-08-19 手臂可见性与基础弹丸尺寸修复](workstreams/2026-08-19-arm-visibility-projectile-scale/STATE.md)（`completed`）
- [2026-08-18 零高度与渲染层级契约](workstreams/2026-08-18-zero-height-render-layers/STATE.md)（`completed`）
- [2026-08-18 基础射击可视与换弹手臂修复](workstreams/2026-08-18-base-shot-reload-visual-fix/STATE.md)（`completed`）
- [2026-08-18 子弹定义与模块化骰面事件管线](workstreams/2026-08-18-projectile-definition-event-pipeline/STATE.md)（`completed`）
- [2026-08-18 测试靶与伤害飘字](workstreams/2026-08-18-target-dummy-damage-numbers/STATE.md)（`completed`）
- [2026-08-18 子弹事件时间系统](workstreams/2026-08-18-bullet-event-time-system/STATE.md)（`completed`）
- [2026-08-17 额外弹丸互相碰撞修复](workstreams/2026-08-17-extra-shot-projectile-collision/STATE.md)（`completed`）
- [2026-08-16 骰面构筑系统](workstreams/2026-08-16-dice-face-build-system/STATE.md)（`completed`）
- [2026-08-17 可移植项目上下文系统](workstreams/2026-08-17-portable-project-context-system/STATE.md)（`completed`）

## 已知缺口

- `ExplosionOnHitEffect.asset` 尚未配置爆炸弹丸 Prefab，触发时只输出 warning。
- 已有通用受伤接口和无限生命测试靶，但没有正式敌人的有限生命、死亡与穿透消费逻辑。
- 骰面构筑仅存在于当前运行期，没有存档。
- 尚未执行完整 PlayMode 战斗流程验证。
- 测试机器人距离阈值、横移换向周期和视觉手感尚未在可见 Play Mode 中人工验收。
- 当前设备的 Git LFS clean filter 无法写入 `.git/lfs/tmp`，导致部分全仓库 Git 状态或 diff 检查失败。

## 最近项目级验证

- [passed] `2026-08-19`：测试机器人行为树 `8/8`、共享控制器 `2/2`、原有瞄准与枪械 `19/19`、机器人资源 `4/4`、场景资源 `7/7` 均通过。
- [passed] `2026-08-19`：Unity EditMode 完整回归 `121/121`，0 失败、0 跳过。
- [passed] `2026-08-19`：Player Prefab SHA256 `296F820634120FB2DCCC59FC268F8534E1C66D28DD3E0E9FE83383940B2ADBEB`、TargetDummy Prefab SHA256 `08BDFC2413576022A057C3E4A239DD490D3E6555D4E1D784DC3F481D5452FE33` 与任务前一致；Player 射速 `2`、换弹时间 `2`、持枪距离 `0.85`、持枪高度 `0.72` 未变化。
- [passed] `2026-08-19`：可移植上下文结构检查返回 `[context:ok]`。
- [not-run] `2026-08-19`：测试机器人在可见 Play Mode 中的距离维持、横移和连续射击手感尚未人工验收。
- [failed] `2026-08-19`：测试机器人行为树红灯按预期因缺少行为树生产类型而编译失败。
- [blocked] `2026-08-19`：行为树最小实现绿灯在隔离 Unity 首次导入期间按用户要求终止，没有测试结果；共享控制器、机器人资源和完整回归尚未运行。
- [passed] `2026-08-19`：四槽位数据、激活、枪械、UI 和资源均完成红绿循环；隔离 Unity 完整 EditMode 回归 `107/107`。
- [passed] `2026-08-19`：Player Prefab SHA256 保持 `296F820634120FB2DCCC59FC268F8534E1C66D28DD3E0E9FE83383940B2ADBEB`，射速 `2`、换弹时间 `2`，未由本事项重存或改写。
- [not-run] `2026-08-19`：四槽位构筑页在当前可见 Play Mode 中的最终排版和点击手感尚未人工验收。
- [passed] `2026-08-19`：手臂地面高度和基础弹丸双倍尺寸聚焦测试 `2/2`，完整 EditMode 回归 `104/104`。
- [passed] `2026-08-19`：Player Prefab 哈希在修复前后保持一致，未改写 AimRoot 子物体、sorting 或左轮调参。
- [not-run] `2026-08-19`：当前可见 Play Mode 中手臂最终对齐和放大后弹丸观感尚未人工验收。
- [passed] `2026-08-18`：零高度、Ground SpriteRenderer、角色/敌人非 Default 图层和基础弹丸 projectile 图层聚焦测试通过；完整 EditMode 回归 `102/102`。
- [passed] `2026-08-18`：Player 与 TargetDummy Prefab 哈希在场景迁移前后保持一致，没有改写用户角色 Transform、sorting 或左轮调参。
- [not-run] `2026-08-18`：当前可见 Play Mode 中 Ground 平铺与透明包装遮挡的最终观感尚未人工验收。
- [passed] `2026-08-18`：模拟真实左键确认 Player Prefab 会消耗一发并生成基础弹丸；缺失 Shader 兼容和换弹仅闪烁聚焦测试 `16/16`，完整 EditMode 回归 `98/98` 通过。
- [passed] `2026-08-18`：主 Unity 会话成功导入 `DiceRevolverGun.cs`、`ProjectileVisualWrapper.cs` 和新粒子 Shader，最新日志没有 C# 或 Shader 编译错误。
- [not-run] `2026-08-18`：修复后的 `fire_1` 最终尺寸、颜色和运动观感尚未在当前可见 Play Mode 中人工验收。
- [passed] `2026-08-18`：Unity 6000.3.10f1 许可证 IPC 恢复；修正测试框架数组计数断言后，完整 EditMode 回归 `94/94` 通过。
- [passed] `2026-08-18`：事件管线、基础弹丸资源、六面绑定、命中策略、场景测试靶和碰撞契约的 27 项确定性冒烟测试通过，续作未修改受保护资产或配置。
- [not-run] `2026-08-18`：`fire_1` 基础子弹、DoubleTap 和主弹/附加弹命中表现的 PlayMode 视觉与手感人工验证未运行。
- [passed] `2026-08-18`：本地 `main` 无独有提交或工作区改动，已快进到 `origin/main` 的 `6a33cf0`；上下文结构检查通过。
- [passed] `2026-08-18`：弹丸定义与模块化事件管线的运行时、Editor、EditMode 测试程序集通过 C# 编译；基础弹丸资源契约和 Player Prefab 限定差异检查通过。
- [passed] `2026-08-18`：测试靶、弹丸伤害、世界空间飘字和资产契约加入后，Unity EditMode 完整回归 `69/69` 通过。
- [passed] `2026-08-18`：场景静态渲染确认测试靶和伤害数字可见，且 Player Prefab、左轮参数、sorting 与项目设置保持不变。
- [not-run] `2026-08-18`：当前可见 Unity 会话中的测试靶连续受击 Play Mode 手感验证未运行。
- [passed] `2026-08-18`：子弹事件时间系统、双重射击延迟和核心战斗中文 Inspector 端口的 Unity EditMode 完整回归 `63/63` 通过。
- [not-run] `2026-08-18`：当前场景中双重射击 `0.25` 秒间隔的 PlayMode 视觉与手感验证未运行。
- [passed] `2026-08-17`：额外弹丸不再互相销毁或报告命中；Unity EditMode 完整回归 `31/31` 通过。
- [passed] `2026-08-17`：Unity `6000.3.10f1` 隔离临时工程 EditMode 测试 `29/29` 通过，覆盖 E 键开关、六面构筑与装备、Resources 示例库、左右镜像瞄准和近距离稳定解算。
- [passed] `2026-08-17`：只读复审未发现 Critical 或 Important 问题；当前 `main` 的 `Player.prefab` 与 HEAD 一致，受保护的 Body、AimRoot 子物体、sorting 和左轮调参未在上下文同步中改写。
- [not-run] `2026-08-17`：完整 PlayMode 战斗流程和当前场景的最终视觉对齐仍需人工验证。
- [passed] `2026-08-17`：检查器 10 项验收测试通过，包含 Unity、Web、Library、错误结构和默认根目录调用。
- [passed] `2026-08-17`：安装器 13 项验收测试通过，覆盖预览、幂等、安全合并、备份、重新绑定、升级和默认根目录调用。
- [passed] `2026-08-17`：`ExistingProject -Apply` 在当前仓库创建入口和项目资料后，结构检查返回成功。
- [passed] `2026-08-17`：真实仓库 `Repair` 预览返回 `no changes`，执行前后路径限定 Git 状态一致。
- [passed] `2026-08-17`：仅按 `AGENTS.md` 和上下文协议读取项目资料的冷启动演练，可以恢复项目结构、工作流、下一动作和已知限制。

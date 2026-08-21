# DiceRevolver 项目状态

## 当前阶段

- 可玩原型阶段；命中范围爆炸、测试机器人、五槽位骰面构筑、被动 Runtime 和雷电连锁构筑均已完成当前自动化范围。

## 全局有效状态

- 当前实现包含顶视角移动、骰子左轮、弹药 HUD、运行时构筑页、既有四个示例词条和六个雷电构筑词条。
- 十个公开骰面词条均已迁移为 Rule-backed 资源；Rule 模块以所属规则的 SubAsset 保存，运行时状态按 Gun/骰面/槽位隔离。
- Core 三个已迁移具体 Effect（DoubleTap、BlastRound、LoadedFour）与雷电五个具体 Effect（电磁共鸣、特斯拉、呼应协同、链式反应、收尾者）均已通过静态类型/GUID 门禁删除；雷电规则构建器已收敛为只创建缺失资产、保留既有值。
- 每个骰面现拥有基础、开火时、命中时、开火后和被动五个互不占用的单事件槽位；活动事件使用不可变激活快照，被动事件使用每枪每面的独立 Runtime。
- 构筑页会显示每面的五行槽位摘要；选择词条后点击骰面只替换该词条对应槽位。新词条只进入资源库，没有自动装备到 Player 或 TestRobot。
- 弹丸支持 ScriptableObject 类型和多标签身份、按不同受伤对象计算的通用穿透，以及按 Gun 隔离的存活弹丸 Registry。
- 雷电构筑已实现雷电球、收尾者、电磁共鸣、特斯拉、呼应协同和链式反应；所有参数通过对应 ScriptableObject 或 Prefab 暴露。
- 闪电链不视为攻击特效且不广播命中事件；呼应协同奖励弹允许同帧生成并带自然散布；链式反应不复制被动槽和空活动槽。
- 玩家 HUD 左上角会自动显示结构化战斗事件 Debug 文本，按真实执行顺序编号并用缩进表达普通射击、延迟结果和奖励射击的因果关系。
- Debug 显示通过 `CombatDebugSettings.asset` 调整启用状态、最大行数、停留时间、字号和面板尺寸，不修改 Player Prefab。
- 构筑页可在场景加载后自动创建，按 `E` 打开或关闭；瞄准系统使用镜像虚拟枪口、近距离稳定解算和开火前同帧姿态刷新。
- 子弹事件可通过 `BulletEventContext.Schedule` 使用轻量游戏时间调度；双重射击默认在第一发后 `0.25` 秒生成第二发。
- 弹丸运行时属性已迁移到 `ProjectileDefinition`；每次骰面触发使用独立 `DiceFaceActivation`，并以默认 `32` 次事件预算限制连锁。
- 左轮机械状态与骰面事件流程已分别收敛到 `DiceRevolverRuntime` 和 `DiceShotPipeline`；`DiceRevolverGun` 只保留 Unity 输入、姿态、实例化与事件适配，旧 `DiceChamber` 已删除。
- 六个骰面均绑定基础左轮子弹生成事件；基础子弹视觉通过独立包装引用 `fire_1.prefab`，附加弹默认不回触命中事件。
- `fire_1.prefab` 原材质引用的 Shader 资源实际缺失；`ProjectileVisualWrapper` 现在只对错误 Shader 使用项目内透明粒子 Shader 兼容包装，不修改原始特效资源。
- 玩家、测试靶和弹丸使用世界 `Y=0` 玩法平面，Ground 当前视觉高度为 `Y=-0.01`；玩家移动不再依赖重力或地面 Collider。
- Ground 已改为 `Background` Sorting Layer 的平铺 SpriteRenderer；基础弹丸粒子包装默认使用 `projectile` Sorting Layer。
- AimRoot 运行时具有最低渲染平面保护，玩家根节点为 `Y=0` 时手臂不会落入 Ground 下方；基础左轮弹丸视觉缩放为 `0.4`。
- 换弹视觉只对 `ArmVisual` 做明暗闪烁，不再写入其位置或旋转。
- 核心战斗 Inspector 端口已提供中文名称，序列化字段名和既有数值保持不变。
- 场景包含一个无限生命测试靶；弹丸通过通用受伤接口提交伤害，每次命中生成独立的世界空间飘字。
- 场景另包含一个 `TestRobot.prefab` 实例；机器人按距离接近、后退或横移，以移动 `0.7` 秒、站定攻击 `1.0` 秒的节奏循环，始终瞄准玩家，并通过共享左轮路径发射六面均为 0 伤害的基础视觉弹丸。
- 测试机器人禁用角色根节点朝瞄准方向旋转，保持与玩家一致的站立立绘，只由 Sprite 翻面和手臂瞄准表达方向。
- `TopDownCharacterController` 统一拥有移动、玩法平面约束和转向；玩家与机器人分别通过 `TopDownPlayerController` 和 `TestRobotController` 提供控制意图。
- `Projectile` 自身通过 `ShouldIgnoreCollision` 统一过滤碰撞并广播命中；`DiceShotPipeline` 处理合格 OnHit 后由 `Projectile` 提交直接伤害，旧 `GunController`、`RevolverGun` 与 `ProjectileHitReporter` 已删除。
- BlastRound 命中事件已绑定独立爆炸弹丸：默认半径 `2.5`、伤害 `3`、视觉持续 `0.35` 秒；直击目标会同时承受直击和爆炸伤害，发射者自身层级免疫爆炸，共享场景父节点下的兄弟角色仍可受伤。
- 可移植上下文系统已集成 `main`；原功能分支和隔离工作树已清理。
- 项目上下文框架位于 `.project-context/framework/`，项目实例资料位于 `.project-context/project/`。

## 活跃工作流

- 无。

## 完成历史

- [2026-08-21 事件配置页面](workstreams/2026-08-21-event-rule-editor/STATE.md)（`completed`；自动化实现与清理完成，三栏编辑器页面与战斗表现的可见 PlayMode 人工验收待做）

- [2026-08-21 战斗事件因果 Debug](workstreams/2026-08-21-combat-debug-trace/STATE.md)（`completed`；自动化完成，PlayMode 排版待人工验收）
- [2026-08-21 雷电构筑系统](workstreams/2026-08-20-lightning-build-backlog/STATE.md)（`completed`；自动化实现完成，PlayMode 视觉与手感待人工验收）
- [2026-08-20 左轮核心底层重构](workstreams/2026-08-20-dice-revolver-core-refactor/STATE.md)（`completed`；自动化结构重构完成，PlayMode 手感待人工验收）
- [2026-08-20 命中范围爆炸](workstreams/2026-08-20-area-explosion-on-hit/STATE.md)（`completed`）
- [2026-08-19 死代码清理与碰撞过滤收敛](workstreams/2026-08-19-dead-code-cleanup/STATE.md)（`completed`）
- [2026-08-19 测试机器人战斗节奏与站立姿态](workstreams/2026-08-19-test-robot-combat-rhythm/STATE.md)（`completed`）
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

- 事件规则编辑器与 Rule 运行时已完成自动化验收；三栏编辑器页面排版、交互与 Play Mode 战斗表现尚待人工验收。
- 已有通用受伤接口和无限生命测试靶，但没有正式敌人的有限生命、死亡与穿透消费逻辑。
- 骰面构筑仅存在于当前运行期，没有存档。
- 尚未执行完整 PlayMode 战斗流程验证。
- 测试机器人距离阈值、横移换向周期和视觉手感尚未在可见 Play Mode 中人工验收。
- 范围爆炸的圆环颜色、半径和持续时间尚未在可见 Play Mode 中人工验收。
- 雷电球尺寸、闪电链可读性、呼应协同同帧散布和六发构筑节奏尚未在可见 Play Mode 中人工验收。
- 当前设备的 Git LFS clean filter 无法写入 `.git/lfs/tmp`，导致部分全仓库 Git 状态或 diff 检查失败。

## 最近项目级验证

- [passed] `2026-08-21`：Task 9 fix 聚焦 `EventRuleLightningMigrationTests` 为 `12/12`、`0 skipped`，覆盖 custom legacy 参数复制与幂等。
- [passed] `2026-08-21`：Task 9 fix 十套联合 EditMode 回归为 `43/43`、`0 skipped`（含新增 custom-value 测试）。
- [failed] `2026-08-21`：清理前完整 EditMode `374/373`，唯一失败为已批准 Ground `Y=-0.01` 豁免项，无新增失败。
- [failed] `2026-08-21`：雷电五 Effect 清理后完整 EditMode `351/350`，唯一失败仍为已批准 Ground `Y=-0.01` 豁免项，`0 skipped`，无新增失败。
- [passed] `2026-08-21`：雷电五 Effect 的脚本类型名与五个资产 GUID 在 `Assets` 零引用；`BulletEventLibrary` 已修剪，十个骰面词条 `rule` 引用完整、legacy 字段为空。
- [passed] `2026-08-21`：Player、TestRobot、TargetDummy、场景、三个基础弹丸 Prefab、fire_1、BlastExplosion、LightningOrb、LightningChain 共十个受保护文件 SHA256 与清理前一致。
- [not-run] `2026-08-21`：事件规则编辑器三栏页面与雷电构筑的可见 PlayMode 人工验收。
- [passed] `2026-08-21`：Task 10 静态门禁确认 Core 三类型及六个 GUID 在 `Assets` 零引用；八个受保护文件 SHA256 与既有基线一致，Player/TestRobot 调参仍为 `2/2/32`。
- [passed] `2026-08-21`：战斗 Debug、枪械事件管线、被动和 UI 聚焦 EditMode `107/107`。
- [passed] `2026-08-21`：特斯拉、收尾者与战斗 Debug 联合 EditMode `20/20`。
- [failed] `2026-08-21`：战斗 Debug 完成后的完整 EditMode 为 `251/252`；唯一失败仍为既有 Ground `Y=-0.01` 契约差异，没有新增失败。
- [not-run] `2026-08-21`：左上角 Debug 文本的可见 PlayMode 排版与事件密度验收。
- [passed] `2026-08-21`：雷电资源、中文 Inspector、五槽位和 UI 联合 EditMode `65/65`，0 失败、0 跳过。
- [failed] `2026-08-21`：雷电构筑完成后的完整 EditMode 为 `233/234`，0 跳过；唯一失败为已获豁免的 `RenderingLayerContractTests.PrototypeSceneUsesZeroHeightSpriteGroundAndEntities`（Ground `-0.01`，测试期望 `0`），没有新增失败。
- [passed] `2026-08-21`：Player、TargetDummy、TestRobot Prefab SHA256 与实施前一致；Player 的持枪距离、持枪高度、射速、换弹时间保持 `0.85`、`0.72`、`2`、`2`。
- [not-run] `2026-08-21`：雷电球、闪电链、呼应协同散布和链式反应的可见 PlayMode 人工验收。
- [passed] `2026-08-20`：左轮核心重构终审修复后分层聚焦 EditMode `58/58`，0 失败、0 跳过。
- [failed] `2026-08-20`：终审修复并 fast-forward 合并到本地 `main` 后，完整 EditMode 复验为 `171/172`，0 跳过；唯一失败为已获用户豁免的 `RenderingLayerContractTests.PrototypeSceneUsesZeroHeightSpriteGroundAndEntities`（Ground `-0.01`，测试期望 `0`），没有新增失败。
- [not-run] `2026-08-20`：左轮核心重构的可见 PlayMode 六发抽取、换弹、DoubleTap、BlastRound 与 LoadedFour 手感尚未人工验收。
- [passed] `2026-08-20`：左轮 Gun/Runtime/Pipeline/Inspector 联合测试 `70/70`，0 失败、0 跳过。
- [failed] `2026-08-20`：完整 EditMode `165/166`；唯一失败为已知 Ground Y `-0.01` 契约差异，已获本任务豁免。
- [passed] `2026-08-20`：范围爆炸运行时 `4/4`、资源绑定 `3/3`；完整 EditMode 回归 `139/139`，0 失败、0 跳过。
- [passed] `2026-08-20`：Player、TargetDummy、TestRobot Prefab SHA256 与任务前一致；玩家射速、换弹时间、持枪距离、持枪高度保持 `2`、`2`、`0.85`、`0.72`。
- [not-run] `2026-08-20`：可见 Play Mode 中的爆炸圆环视觉和战斗手感尚未人工验收。
- [passed] `2026-08-19`：死代码清理前基线 `123/123`，碰撞策略聚焦测试 `3/3`，清理后完整 EditMode 回归 `126/126`。
- [passed] `2026-08-19`：被删 `GunController` 与 `RevolverGun` 的脚本 GUID 资源引用数均为 0；四个现有 DiceFaceEntry 均仍被词条库引用。
- [passed] `2026-08-19`：机器人移动/站定节奏、持续瞄准射击和站立姿态联合聚焦测试 `16/16`；完整 EditMode 回归 `123/123`，0 失败、0 跳过。
- [passed] `2026-08-19`：本次调整后 Player 与 TargetDummy Prefab SHA256 继续与任务前一致。
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

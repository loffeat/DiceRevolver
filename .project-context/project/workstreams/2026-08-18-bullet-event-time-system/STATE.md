# 子弹事件时间系统

- ID: `2026-08-18-bullet-event-time-system`
- Status: `completed`
- Branch: `main`
- Created: `2026-08-18`
- Updated: `2026-08-18`

## 目标

- 提供可供所有子弹事件复用的轻量游戏时间调度能力，并让双重射击默认延迟 `0.25` 秒生成第二发。
- 为核心战斗 Inspector 端口增加中文名称且保持现有序列化值。

## 非目标

- 不制作全局单例、循环计时器或复杂任务取消系统。
- 不修改角色、瞄准、镜头、UI、sorting、Prefab Layer 和既有枪械参数。

## 已确认事实

- 用户选择使用受 `Time.timeScale` 影响的游戏时间。
- 用户批准将原先的左轮内部延迟队列扩展为所有子弹事件可通过 Context 调用的精简时间系统。
- 时间调度器不读取 Unity 时间；所属左轮显式传入 `Time.time`。
- 双重射击资源已保存 `delaySeconds: 0.25`，可在 Inspector 的“第二发延迟（秒）”调整。

## 已完成

- 设计规格已写入 `docs/superpowers/specs/2026-08-18-bullet-event-time-system-design.md`。
- 实施计划已写入 `docs/superpowers/plans/2026-08-18-bullet-event-time-system.md`。
- 完成普通 C# 事件时间调度器、Context 调度接口和左轮时间驱动。
- 双重射击改为默认延迟 `0.25` 秒请求第二发，并保留原射击上下文与递归保护。
- 核心战斗相关 Inspector 端口增加中文名称，不重命名序列化字段。

## 当前正在进行

- 无

## 下一步

1. 在 Play Mode 中装备“双重射击”，调整 `ExtraShotOnFireEffect.asset` 的“第二发延迟（秒）”并确认手感。
2. 后续事件需要延迟时通过 `BulletEventContext.Schedule` 登记，不另建协程或全局计时器。

## 阻塞

- 无

## 涉及文件

- `docs/superpowers/specs/2026-08-18-bullet-event-time-system-design.md`
- `Assets/Scripts/Prototype/`
- `Assets/Tests/EditMode/`
- `Assets/Resources/DiceFacePrototype/`
- `Assets/Scripts/Prototype/BulletEventTimeScheduler.cs`
- `Assets/Scripts/Prototype/BulletEventContext.cs`
- `Assets/Scripts/Prototype/ExtraShotOnFireEffect.cs`
- `Assets/Scripts/Prototype/DiceRevolverGun.cs`
- `Assets/Tests/EditMode/BulletEventTimeSchedulerTests.cs`
- `Assets/Tests/EditMode/BulletEventEffectTests.cs`
- `Assets/Tests/EditMode/CombatInspectorLocalizationTests.cs`

## 验证记录

- [failed] `2026-08-18`：调度器 RED 因 `BulletEventTimeScheduler` 不存在而编译失败。
- [passed] `2026-08-18`：调度器 GREEN `6/6` 通过。
- [failed] `2026-08-18`：Context/双重射击 RED 因缺少 `Schedule`、8 参数构造和 `DelaySeconds` 而编译失败。
- [passed] `2026-08-18`：Context/双重射击 GREEN `6/6` 通过。
- [failed] `2026-08-18`：左轮接线 RED 中 Context 调度请求返回 `false`。
- [passed] `2026-08-18`：左轮集成 GREEN `5/5` 通过。
- [failed] `2026-08-18`：中文端口 RED 修正测试读取方式后为 `1/23` 通过、`22/23` 失败。
- [passed] `2026-08-18`：中文端口 GREEN `23/23` 通过。
- [passed] `2026-08-18`：Unity EditMode 完整回归 `63/63` 通过。
- [not-run] `2026-08-18`：PlayMode 延迟射击视觉与手感验证未运行。

## 相关资料

- [设计规格](../../../../docs/superpowers/specs/2026-08-18-bullet-event-time-system-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-18-bullet-event-time-system.md)
- [骰面构筑系统](../2026-08-16-dice-face-build-system/STATE.md)

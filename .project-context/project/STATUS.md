# DiceRevolver 项目状态

## 当前阶段

- 可玩原型阶段；骰面构筑、测试靶、模块化弹丸事件管线和上下文系统均已完成当前计划范围。

## 全局有效状态

- `main` 的当前基线包含顶视角移动、骰子左轮、弹药 HUD、运行时构筑页和三个示例词条。
- 构筑页可在场景加载后自动创建，按 `E` 打开或关闭；瞄准系统使用镜像虚拟枪口、近距离稳定解算和开火前同帧姿态刷新。
- 子弹事件可通过 `BulletEventContext.Schedule` 使用轻量游戏时间调度；双重射击默认在第一发后 `0.25` 秒生成第二发。
- 弹丸运行时属性已迁移到 `ProjectileDefinition`；每次骰面触发使用独立 `DiceFaceActivation`，并以默认 `32` 次事件预算限制连锁。
- 六个骰面均绑定基础左轮子弹生成事件；基础子弹视觉通过独立包装引用 `fire_1.prefab`，附加弹默认不回触命中事件。
- 核心战斗 Inspector 端口已提供中文名称，序列化字段名和既有数值保持不变。
- 场景包含一个无限生命测试靶；弹丸通过通用受伤接口提交伤害，每次命中生成独立的世界空间飘字。
- 可移植上下文系统已集成 `main`；原功能分支和隔离工作树已清理。
- 项目上下文框架位于 `.project-context/framework/`，项目实例资料位于 `.project-context/project/`。

## 活跃工作流

- 无

## 完成历史

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
- 当前设备的 Git LFS clean filter 无法写入 `.git/lfs/tmp`，导致部分全仓库 Git 状态或 diff 检查失败。

## 最近项目级验证

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

# 测试靶与伤害飘字

- ID: `2026-08-18-target-dummy-damage-numbers`
- Status: `completed`
- Branch: `main`
- Created: `2026-08-18`
- Updated: `2026-08-18`

## 目标

- 增加不会移动和攻击、拥有无限生命的测试靶。
- 每次受击在测试靶身旁显示独立的世界空间伤害飘字。
- 建立可供未来敌人复用且不耦合左轮的受伤协议。

## 非目标

- 不实现有限生命、死亡、AI、攻击、掉落或受击硬直。
- 不修改玩家、瞄准、镜头、sorting、骰面资源和左轮参数。

## 已确认事实

- 用户选择方案 1：弹丸通过通用受伤接口提交伤害。
- 用户选择世界空间飘字，每次伤害生成独立数字并上浮淡出。

## 已完成

- 新增不可变 `DamageInfo` 与 `IDamageReceiver` 受伤协议。
- `Projectile` 命中时向 Collider 父级接收者提交运行时伤害。
- `TargetDummy` 连续受击只广播事件，不维护生命或死亡状态。
- 世界空间伤害数字逐次独立生成、上浮、淡出并销毁。
- 创建 `TargetDummy.prefab`，并在原型场景玩家右侧放置一个实例。
- 使用专用幂等构建器，不调用完整场景重建入口。

## 当前正在进行

- 无

## 下一步

1. 在当前 Unity 编辑器中进入 Play Mode，连续射击测试靶，确认飘字节奏与偏移手感。
2. 后续正式敌人可复用 `IDamageReceiver`，在自己的生命组件中处理扣血和死亡。

## 阻塞

- 无

## 涉及文件

- `Assets/Scripts/Prototype/DamageInfo.cs`
- `Assets/Scripts/Prototype/IDamageReceiver.cs`
- `Assets/Scripts/Prototype/TargetDummy.cs`
- `Assets/Scripts/Prototype/WorldDamageNumber.cs`
- `Assets/Scripts/Prototype/WorldDamageNumberSpawner.cs`
- `Assets/Scripts/Prototype/Projectile.cs`
- `Assets/Scripts/Editor/TargetDummyPrototypeBuilder.cs`
- `Assets/Prefab/TargetDummy.prefab`
- `Assets/Materials/TargetDummy*.mat`
- `Assets/Scenes/TopDownShooterPrototype.unity`
- `Assets/Tests/EditMode/TargetDummyTests.cs`
- `Assets/Tests/EditMode/TargetDummyAssetTests.cs`

## 验证记录

- [failed] `2026-08-18`：伤害协议 RED 因 `TargetDummy` 与 `DamageInfo` 不存在而编译失败。
- [passed] `2026-08-18`：无限生命测试靶 GREEN `1/1` 通过。
- [failed] `2026-08-18`：弹丸伤害 RED 的实际命中次数为 `0`。
- [passed] `2026-08-18`：弹丸伤害传递 GREEN `2/2` 通过。
- [failed] `2026-08-18`：飘字 RED 因视图与生成器类型不存在而编译失败。
- [passed] `2026-08-18`：伤害协议、弹丸与飘字组合测试 `4/4` 通过。
- [failed] `2026-08-18`：资产 RED 中 Prefab 不存在、场景实例数为 `0`。
- [passed] `2026-08-18`：真实脚本 GUID、Prefab 与合并场景资产测试 `2/2` 通过。
- [passed] `2026-08-18`：完整 Unity EditMode 回归 `69/69` 通过。
- [passed] `2026-08-18`：场景静态渲染确认测试靶与 `12.5` 伤害数字清晰可见且不遮挡 HUD。
- [passed] `2026-08-18`：Player Prefab SHA-256 与此前一致，左轮参数、sorting、Packages 和 ProjectSettings 未改动。
- [not-run] `2026-08-18`：当前可见 Unity 会话中的 Play Mode 连续射击体验未人工运行。

## 相关资料

- [设计规格](../../../../docs/superpowers/specs/2026-08-18-target-dummy-damage-numbers-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-18-target-dummy-damage-numbers.md)

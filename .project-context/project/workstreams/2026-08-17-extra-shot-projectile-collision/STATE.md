# 额外弹丸互相碰撞修复

- ID: `2026-08-17-extra-shot-projectile-collision`
- Status: `completed`
- Branch: `main`
- Created: `2026-08-17`
- Updated: `2026-08-17`

## 目标

- 让“开火时额外发射一次当前骰面”稳定生成主弹和额外弹，两颗弹丸不会在枪口处互相判定命中并销毁。

## 非目标

- 不修改 `Player.prefab`、弹丸 Prefab Layer、sorting、AimRoot 子物体或左轮调参。
- 不改变敌人命中、伤害、穿透和额外射击禁止递归的既有规则。

## 已确认事实

- 主弹和额外弹使用相同位置与方向生成。
- `PrototypeProjectile.prefab` 位于 Default Layer，使用 Trigger SphereCollider。
- `Projectile` 和 `ProjectileHitReporter` 当前都会把另一个弹丸的 Collider 当作有效命中对象。
- 根因回归测试在修复前分别捕获到 `Projectile` 尝试销毁自身，以及 Reporter 错误上报 1 次命中。

## 已完成

- 完成事件调用链、资源引用和弹丸 Prefab 的只读根因调查。
- `Projectile` 忽略 Collider 层级中带有 `Projectile` 的对象。
- `ProjectileHitReporter` 同样忽略其他弹丸，不再产生伪命中事件。
- 新增两个真实碰撞处理器回归测试。

## 当前正在进行

- 无

## 下一步

1. 在 `TopDownShooterPrototype.unity` 的 Play Mode 中为骰面装备“双重射击”，人工确认可同时观察到两颗弹丸。

## 阻塞

- 无

## 涉及文件

- `Assets/Scripts/Prototype/Projectile.cs`
- `Assets/Scripts/Prototype/ProjectileHitReporter.cs`
- `Assets/Tests/EditMode/DiceRevolverGunIntegrationTests.cs`

## 验证记录

- [failed] `2026-08-17`：修复前 `DiceRevolverGunIntegrationTests` 为 `2/4`，两个新测试分别因 Destroy 调用和命中计数 `1` 失败。
- [passed] `2026-08-17`：修复后目标集成测试 `4/4` 通过。
- [passed] `2026-08-17`：Unity EditMode 完整回归 `31/31` 通过。
- [not-run] `2026-08-17`：当前场景 PlayMode 双重射击视觉验证未运行。

## 相关资料

- [骰面构筑系统](../2026-08-16-dice-face-build-system/STATE.md)

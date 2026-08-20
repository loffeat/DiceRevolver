# 额外弹丸互相碰撞修复交接

## 当前状态

- 修复与自动化回归已完成：主弹和额外弹不会再互相销毁或报告命中。

## 当前方案及原因

- 在 `Projectile` 和 `ProjectileHitReporter` 的碰撞入口统一忽略属于其他弹丸的 Collider，避免依赖全局 Physics Layer 配置，同时兼容 Collider 位于弹丸子物体的情况。

## 尚未完成

- 当前场景 PlayMode 的双重射击视觉验证。

## 下一步首个动作

- 打开 `TopDownShooterPrototype.unity`，进入 Play Mode，将“双重射击”装备到一个骰面并确认该面会生成两颗可见弹丸。

## 首先读取

- [工作流状态](STATE.md)
- [DiceRevolverGun.cs](../../../../Assets/Scripts/Prototype/DiceRevolverGun.cs)
- [Projectile.cs](../../../../Assets/Scripts/Prototype/Projectile.cs)
- `ProjectileHitReporter.cs`（后续核心重构已移除）

## 必须保护

- 不覆盖 `Player.prefab` 的 AimRoot、ArmVisual、GunBody、Muzzle、sorting layer 和枪械调参。
- 不修改用户填写的 `DiceRevolverGun` Inspector 数值。

## 最近验证

- [failed] `2026-08-17`：修复前目标集成测试 `2/4`，两个回归测试均按预期失败。
- [passed] `2026-08-17`：修复后目标集成测试 `4/4` 通过。
- [passed] `2026-08-17`：Unity EditMode 完整回归 `31/31` 通过。

## 风险与不可假定事项

- EditMode 回归不等于当前场景的两颗弹丸视觉表现已经人工确认。

# 死代码清理与碰撞过滤收敛

- ID: `2026-08-19-dead-code-cleanup`
- Status: `completed`
- Branch: `codex/cleanup-dead-code`
- Created: `2026-08-19`
- Updated: `2026-08-19`

## 目标

- 删除经资源 GUID 和生产引用核对后确认完全无用的旧枪械脚本。
- 在不修改 `DiceRevolverGun` 的前提下收敛弹丸重复碰撞过滤逻辑。

## 非目标

- 不拆分或重构 `DiceRevolverGun`。
- 不删除仍承担旧序列化迁移的隐藏字段。
- 不删除仍被词条库引用但尚未配置最终表现的词条。
- 不移除 `ProjectileHitReporter` 组件。

## 已确认事实

- `GunController` 与 `RevolverGun` 的脚本 GUID 在 Prefab、场景和其他生产资源中均为零引用。
- BasicShot、BlastRound、LoadedFour 和 DoubleTap 四个 `DiceFaceEntry` 均被 `DiceFaceLibrary` 引用，没有孤立词条。
- `Projectile` 与 `ProjectileHitReporter` 原先各自维护相同的弹丸和 Player 过滤判断。

## 当前正在进行

- 无

## 已完成

- 隔离工作树基线完整 EditMode 回归 `123/123`。
- 新增共享碰撞策略测试，红灯按预期因共享接口不存在而编译失败。
- `Projectile` 统一拥有碰撞过滤方法，`ProjectileHitReporter` 改为复用。
- 删除 `GunController.cs`、`RevolverGun.cs` 及对应 `.meta`。
- 聚焦碰撞策略测试 `3/3` 通过。
- 清理后完整 EditMode 回归 `126/126` 通过。
- 限定差异检查确认 `DiceRevolverGun.cs` 没有改动。

## 下一步

1. 未来允许修改 `DiceRevolverGun` 时，可让它直接订阅 `Projectile` 命中事件，再删除 `ProjectileHitReporter` 组件。

## 阻塞

- 无。

## 涉及文件

- `Assets/Scripts/Prototype/Projectile.cs`
- `Assets/Scripts/Prototype/ProjectileHitReporter.cs`
- `Assets/Tests/EditMode/ProjectileCollisionTests.cs`
- `Assets/Scripts/Prototype/GunController.cs`（删除）
- `Assets/Scripts/Prototype/RevolverGun.cs`（删除）

## 验证记录

- [passed] `2026-08-19`：清理前完整 EditMode 基线 `123/123`。
- [failed] `2026-08-19`：碰撞策略测试红灯因 `Projectile.ShouldIgnoreCollision` 不存在而编译失败，原因符合预期。
- [passed] `2026-08-19`：碰撞策略聚焦测试 `3/3`，0 失败、0 跳过。
- [passed] `2026-08-19`：两个被删脚本 GUID 的资源引用数均为 0；四个现有 DiceFaceEntry 均被词条库引用。
- [passed] `2026-08-19`：清理后完整 EditMode 回归 `126/126`，0 失败、0 跳过；`DiceRevolverGun.cs` 限定差异为空。

## 相关资料

- [项目状态](../../STATUS.md)

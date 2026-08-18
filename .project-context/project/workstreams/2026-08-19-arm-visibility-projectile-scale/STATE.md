# 手臂可见性与基础弹丸尺寸修复

- ID: `2026-08-19-arm-visibility-projectile-scale`
- Status: `completed`
- Branch: working tree
- Created: `2026-08-19`
- Updated: `2026-08-19`

## 目标

- 修复玩家根节点迁移到世界 `Y=0` 后手臂落到 Ground 以下而不可见的问题。
- 把基础左轮弹丸的视觉包装尺寸扩大一倍。

## 非目标

- 不修改 Player Prefab 的 AimRoot、ArmVisual、GunBody、Muzzle 局部 Transform 或 sorting。
- 不修改 `DiceRevolverGun` 的射速、换弹或其他用户调参数值。
- 不改变基础弹丸的伤害、速度、距离、碰撞体或事件行为。

## 已确认事实

- Player Prefab 中 ArmVisual 仍启用，Sprite 引用、Alpha 和 `Gun` Sorting Layer 均完整。
- Player Prefab 哈希与零高度迁移完成时一致，手臂引用没有被覆盖。
- 玩家根节点为 `Y=0` 时，`TopDownAimHandRig.RefreshAimPose` 按 `visualHeight=-0.58` 把手臂渲染中心放到 `Y=-0.58`，位于 Ground 下方。
- 基础弹丸 Prefab 的 `ProjectileVisualWrapper.visualScale` 为 `0.2`。

## 当前正在进行

- 无

## 已完成

- `TopDownAimHandRig` 保留原瞄准与视觉高度计算，但当 AimRoot 即将低于玩法平面时，将其世界高度限制在 `Y=0.01`。
- 未修改 Player Prefab，AimRoot 三个子物体及其已有 sorting 数据保持原样。
- `BasicRevolverBullet.prefab` 的视觉缩放从 `0.2` 调整为 `0.4`，其他弹丸数据未改。
- 新增真实 Player Prefab 手臂地面高度回归测试和基础弹丸尺寸资产测试。

## 下一步

1. 在当前场景重新进入 Play Mode，检查左右瞄准时手臂始终可见。
2. 连续射击并观察放大后的基础弹丸尺寸是否符合手感预期。

## 阻塞

- 无。

## 涉及文件

- `Assets/Scripts/Prototype/TopDownAimHandRig.cs`
- `Assets/Prefab/Projectiles/BasicRevolverBullet.prefab`
- `Assets/Tests/EditMode/TopDownAimSolverTests.cs`
- `Assets/Tests/EditMode/ProjectileDefinitionAssetTests.cs`

## 验证记录

- [failed] `2026-08-19`：修复前聚焦测试 `0/2`；手臂渲染中心为 `Y=-0.58`，基础弹丸视觉缩放为 `0.2`。
- [passed] `2026-08-19`：修复后聚焦测试 `2/2`。
- [passed] `2026-08-19`：隔离 Unity 工程完整 EditMode 回归 `104/104`。
- [passed] `2026-08-19`：Player Prefab SHA256 在修复前后保持一致。
- [not-run] `2026-08-19`：当前可见 Play Mode 的最终手臂位置和弹丸尺寸尚未人工验收。

## 相关资料

- [项目状态](../../STATUS.md)
- [零高度与渲染层级契约](../2026-08-18-zero-height-render-layers/STATE.md)

# 命中范围爆炸

- ID: `2026-08-20-area-explosion-on-hit`
- Status: `completed`
- Branch: `main` working tree
- Created: `2026-08-20`
- Updated: `2026-08-20`

## 目标

- 实装 `ExplosionOnHitEffect`，在弹丸命中点生成圆形爆炸。
- 对爆炸范围内的敌人各造成一次伤害，直击目标同时承受直击和爆炸伤害。
- 提供可独立配置的爆炸弹丸 Prefab 与定义。

## 非目标

- 不实现阵营系统、击退、持续伤害或连锁爆炸。
- 不修改玩家 3C、瞄准、枪械参数或骰面四槽位结构。
- 不重存 Player、TargetDummy 或 TestRobot Prefab。

## 已确认事实

- 用户确认直击目标需要额外承受一次爆炸范围伤害。
- 继续使用现有 `ExplosionOnHitEffect` 的爆炸弹丸定义端口。
- 默认爆炸半径 `2.5`、爆炸伤害 `3`、视觉持续 `0.35` 秒。
- 同一受伤对象的多个 Collider 只结算一次，发射者免疫自身爆炸。

## 当前正在进行

- 无

## 已完成

- 完成边界设计确认。
- 记录受保护 Prefab 的任务前 SHA256。
- 新增 `AreaExplosionProjectile`，在命中点按半径搜索 `IDamageReceiver` 并按接收者去重。
- 爆炸伤害包含直击目标，范围外目标不受伤，发射者自身层级免疫自身爆炸。
- 弹丸保存实际开火碰撞体作为所有者，避免共享场景父节点下的兄弟角色被误判为发射者。
- 新增从中心扩张并淡出的 XZ 平面圆环表现，使用 `projectile` Sorting Layer。
- 新增爆炸 Prefab、圆环材质和伤害为 `3` 的爆炸弹丸定义。
- 现有 `ExplosionOnHitEffect.asset` 已绑定爆炸定义，弹丸定义库只追加新定义。
- 定向构建器只创建缺失资源或补空引用，不覆盖已存在的用户调参。

## 下一步

1. 可选：在可见 Play Mode 中为骰面装备 BlastRound，人工验收圆环大小、颜色和持续时间。

## 阻塞

- 无。

## 尚未完成

- 可见 Play Mode 视觉与手感验收未运行。

## 必须保护

- `Assets/Prefab/Player.prefab` SHA256：`296F820634120FB2DCCC59FC268F8534E1C66D28DD3E0E9FE83383940B2ADBEB`。
- `Assets/Prefab/TargetDummy.prefab` SHA256：`08BDFC2413576022A057C3E4A239DD490D3E6555D4E1D784DC3F481D5452FE33`。
- `Assets/Prefab/TestRobot.prefab` SHA256：`A5A6A00F8367797E2AB70295E5B937F97F65194AC27708110CC93BC28136F438`。
- 不运行完整 `TopDownPrototypeSceneBuilder`。

## 验证记录

- [failed] `2026-08-20`：运行时红灯因缺少 `AreaExplosionProjectile` 类型而编译失败，符合预期。
- [passed] `2026-08-20`：范围伤害、重复 Collider 去重、直击位置、发射者免疫和共享父节点隔离聚焦测试 `4/4`。
- [failed] `2026-08-20`：资源红灯 `0/3`，缺少爆炸 Prefab、定义和事件绑定，符合预期。
- [passed] `2026-08-20`：爆炸 Prefab、定义、事件和定义库资源测试 `3/3`。
- [passed] `2026-08-20`：完整 EditMode 回归 `139/139`，0 失败、0 跳过。
- [passed] `2026-08-20`：Player、TargetDummy、TestRobot Prefab SHA256 与任务前一致；Player 射速、换弹时间、持枪距离和持枪高度保持 `2`、`2`、`0.85`、`0.72`。
- [not-run] `2026-08-20`：可见 Play Mode 圆环视觉和实际战斗手感未人工验收。

## 涉及文件

- `Assets/Scripts/Prototype/AreaExplosionProjectile.cs`
- `Assets/Scripts/Prototype/Projectile.cs`
- `Assets/Scripts/Editor/AreaExplosionPrototypeBuilder.cs`
- `Assets/Prefab/Projectiles/BlastExplosion.prefab`
- `Assets/Materials/ExplosionRing.mat`
- `Assets/Resources/DiceFacePrototype/Projectiles/BlastExplosion.asset`
- `Assets/Resources/DiceFacePrototype/BulletEvents/ExplosionOnHitEffect.asset`
- `Assets/Resources/DiceFacePrototype/Projectiles/ProjectileDefinitionLibrary.asset`
- `Assets/Tests/EditMode/AreaExplosionProjectileTests.cs`
- `Assets/Tests/EditMode/AreaExplosionAssetTests.cs`
- `Assets/Tests/EditMode/BulletEventEffectTests.cs`
- `Assets/Tests/EditMode/CombatInspectorLocalizationTests.cs`

## 相关资料

- [项目状态](../../STATUS.md)
- [既有弹丸事件管线](../2026-08-18-projectile-definition-event-pipeline/STATE.md)
